using System;
using System.Diagnostics;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Hosting;

/// <summary>
/// Host-neutral lifetime for one <see cref="Application"/> tree.
/// Desktop <c>Shell.Initialize</c> constructs this; games construct it without a GLFW window.
/// </summary>
public sealed class UiSession : IDisposable
{
    readonly UiSessionOptions _options;
    bool _disposed;
    bool _installedSyncContext;
    int? _maxFps;
    long _lastPaintTicks;

    public UiSession(IUiHost host, Application application, UiSessionOptions? options = null)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
        Application = application ?? throw new ArgumentNullException(nameof(application));
        if (application.Session != null && !ReferenceEquals(application.Session, this))
            throw new InvalidOperationException("Application is already attached to a UiSession.");

        _options = options ?? new UiSessionOptions();
        PaintMode = _options.PaintMode;
        ClearColor = _options.ClearColor;
        ClearCanvas = _options.ClearCanvas;
        _maxFps = _options.MaxFps is > 0 ? _options.MaxFps : null;
        Input = new UiInput(this);
        application.Session = this;
    }

    /// <summary>
    /// Session for a single view, without an <see cref="Application"/> subclass (a world-space panel,
    /// a nameplate). The view is wrapped in a private one-view application; <see cref="Application"/>
    /// still exposes it for <c>Time</c> and app-level <c>Events</c>.
    /// </summary>
    public UiSession(IUiHost host, View view, UiSessionOptions? options = null)
        : this(host, new SingleViewApplication(view), options)
    {
    }

    public Application Application { get; }
    public IUiHost Host { get; }
    public UiInput Input { get; }
    public bool IsLoaded { get; private set; }
    public PaintMode PaintMode { get; }
    public SKColor ClearColor { get; }
    public bool ClearCanvas { get; }
    public UiSize LogicalSize { get; private set; }
    public FramebufferSize FramebufferSize { get; private set; }

    /// <summary>
    /// Gets or sets the maximum UI frames per second.
    /// When set to a positive value (e.g. 30 or 60), <see cref="NeedsPaint"/> will throttle
    /// and return false until the internal frame time has elapsed, allowing Blossom to accumulate
    /// changes without premature rendering. Set to null or &lt;= 0 to uncap (ready as soon as dirty).
    /// </summary>
    public int? MaxFps
    {
        get => _maxFps;
        set => _maxFps = value is > 0 ? value : null;
    }

    /// <summary>
    /// Computes the seconds until the next paint is allowed or due.
    /// Returns 0 if paint is needed immediately.
    /// Returns a positive duration if changes are pending but throttled by <see cref="MaxFps"/>.
    /// Returns null if there are no pending changes.
    /// </summary>
    public double? SecondsUntilNextPaint()
    {
        ThrowIfDisposed();

        bool hasChanges = Application.ActiveView?.ComputeNeedsPaint() == true || UiDispatcher.HasDueTimer();
        if (!hasChanges)
            return null;

        if (_maxFps is > 0)
        {
            long now = Stopwatch.GetTimestamp();
            long minTicks = Stopwatch.Frequency / _maxFps.Value;
            long elapsed = now - _lastPaintTicks;
            if (_lastPaintTicks > 0 && elapsed < minTicks)
            {
                long remainTicks = minTicks - elapsed;
                return Math.Max(0.0, (double)remainTicks / Stopwatch.Frequency);
            }
        }

        return 0.0;
    }

    /// <summary>Computed; does not consume dirty flags. Respects <see cref="MaxFps"/> if configured.</summary>
    public bool NeedsPaint
    {
        get
        {
            ThrowIfDisposed();
            return SecondsUntilNextPaint() == 0.0;
        }
    }

    public void SetSize(float logicalW, float logicalH, int framebufferW, int framebufferH)
    {
        ThrowIfDisposed();
        logicalW = Math.Max(0, logicalW);
        logicalH = Math.Max(0, logicalH);
        framebufferW = Math.Max(0, framebufferW);
        framebufferH = Math.Max(0, framebufferH);

        var prev = LogicalSize;
        var prevFb = FramebufferSize;
        bool changed =
            Math.Abs(prev.Width - logicalW) > 0.01f ||
            Math.Abs(prev.Height - logicalH) > 0.01f ||
            prevFb.Width != framebufferW ||
            prevFb.Height != framebufferH;
        if (!changed)
            return;

        LogicalSize = new UiSize(logicalW, logicalH);
        FramebufferSize = new FramebufferSize(framebufferW, framebufferH);
        Application.ActiveView?.ForceLayoutEvaluation();
        Host.Wake();
    }

    public void Init()
    {
        ThrowIfDisposed();
        if (IsLoaded)
            return;

        try
        {
            Blossom.Utils.Fonts.EnsureFallbacks();
        }
        catch { }

        if (_options.InstallSynchronizationContext)
        {
            _installedSyncContext = UiDispatcher.TryInstallSynchronizationContext();
        }
        else
        {
            UiDispatcher.BindUiThread();
        }

        var view = Application.ActiveView;
        if (view != null)
        {
            if (ClearColor.Alpha == 0 && view.BackColor == SKColors.White)
                view.BackColor = SKColors.Transparent;

            if (!view.IsLoaded)
            {
                try
                {
                    view.Init();
                }
                catch (Exception ex)
                {
                    Log.Fatal("Exception during ActiveView.Init():\n" + ex);
                    throw;
                }
                view.IsLoaded = true;
            }
        }
        else
        {
            Log.Warning("No active view");
        }

        IsLoaded = true;
    }

    /// <summary>CPU only: dispatcher, timers, TriggerLoop, SKSL clocks. Does not touch GRContext.</summary>
    public void Tick(float deltaSeconds)
    {
        ThrowIfDisposed();
        EnsureUiThread();
        UiHost.Enter();
        try
        {
            UiDispatcher.DrainPostQueue();
            UiDispatcher.DrainTimers();

            float dt = deltaSeconds < 0 ? 0 : deltaSeconds;
            Application.AdvanceTime(dt);

            Application.ActiveView?.TriggerLoop();

            UiDispatcher.DrainPostQueue();
            UiDispatcher.DrainTimers();
        }
        finally
        {
            UiHost.Exit();
        }
    }

    public void Paint(in RenderFrame frame, bool force = false)
    {
        ThrowIfDisposed();
        EnsureUiThread();

        if (frame.Canvas is null)
        {
            Log.Warning("UiSession.Paint skipped: canvas is null.");
            return;
        }

        if (LogicalSize.Width <= 0 || LogicalSize.Height <= 0 ||
            frame.LogicalSize.Width <= 0 || frame.LogicalSize.Height <= 0)
        {
            Log.Warning("UiSession.Paint skipped: logical size is zero.");
            return;
        }

        if (!force && !NeedsPaint)
            return;

        _lastPaintTicks = Stopwatch.GetTimestamp();

        var view = Application.ActiveView;
        if (force || view?.HasContinuousKeepAlive == true)
        {
            if (view != null)
                view.RenderRequired = true;
        }

        UiHost.Enter();
        try
        {
            GpuPass.InGpuPass = true;
            GpuPass.PaintMode = PaintMode;
            var surface = frame.Surface ?? frame.Canvas.Surface;
            GpuPass.Frame = new RenderFrame(
                canvas: frame.Canvas,
                logicalSize: frame.LogicalSize,
                framebufferSize: frame.FramebufferSize,
                gpuContext: frame.GpuContext,
                surface: surface,
                clearCanvas: frame.ClearCanvas,
                clearColor: frame.ClearColor);

            Application.Render();
        }
        finally
        {
            GpuPass.InGpuPass = false;
            GpuPass.PaintMode = default;
            GpuPass.Frame = default;
            UiHost.Exit();
        }
    }

    public VisualElement? HitTest(float logicalX, float logicalY)
    {
        ThrowIfDisposed();
        return Application.ActiveView?.HitTest(logicalX, logicalY);
    }

    /// <summary>
    /// True if a geometric hit exists at the logical point, including a blocking
    /// overlay layer (miss on empty glass still wants the pointer). Does not
    /// consider pointer capture. Opacity 0 still hits unless Interactive is false
    /// or IsClickthrough is true.
    /// </summary>
    public bool WantsPointer(float logicalX, float logicalY) => HitTest(logicalX, logicalY) != null;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        IsLoaded = false;

        if (ReferenceEquals(Application.Session, this))
            Application.Session = null;

        Application.Dispose();

        if (_installedSyncContext)
            UiDispatcher.RestoreSynchronizationContext();
    }

    internal void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UiSession));
    }

    void EnsureUiThread()
    {
        if (UiDispatcher.HasUiThread && !Host.CheckAccess())
            throw new InvalidOperationException("UiSession.Tick/Paint must run on the UI thread.");
    }

    sealed class SingleViewApplication : Application
    {
        public SingleViewApplication(View view)
        {
            ArgumentNullException.ThrowIfNull(view);
            if (view.Application != null)
                throw new InvalidOperationException("View already belongs to an Application.");
            AddView(view);
            SetActiveView(view);
        }
    }
}
