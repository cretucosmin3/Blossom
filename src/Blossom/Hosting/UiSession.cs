using System;
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
        Input = new UiInput(this);
        application.Session = this;
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

    /// <summary>Computed; does not consume dirty flags.</summary>
    public bool NeedsPaint
    {
        get
        {
            ThrowIfDisposed();
            if (Application.ActiveView?.ComputeNeedsPaint() == true)
                return true;
            return UiDispatcher.HasDueTimer();
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
            Blossom.Core.Visual.SKSLShaderTimeTracker.DeltaTime = dt;
            Blossom.Core.Visual.SKSLShaderTimeTracker.ElapsedSeconds += dt;

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
}
