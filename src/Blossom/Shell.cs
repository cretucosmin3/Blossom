using System.Diagnostics;
using System.Threading;
using System.Numerics;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using Blossom.Core.Delegates.Common;
using Blossom.Platform;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using System.Runtime.InteropServices;
using SkiaSharp;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.GLFW;

namespace Blossom;

public static class Shell
{
    private static IInputContext input;

    internal static IWindow window;
    internal static Application ShellApp = null!;
    internal static RectangleF RenderRect = new(0, 0, 0, 0);
    internal static bool WasResized;

    internal static Action OnRenderRequired;

    public static event ForVoid OnLoaded;

    /// <summary>OS file drop onto the window. Also forwarded to <c>Application.Events</c> and the active view.</summary>
    public static event Action<string[]> FilesDropped;

    /// <summary>Raised after a real client-area size change (width, height in screen coordinates).</summary>
    public static event Action<int, int>? ClientResized;

    internal static void RaiseClientResized(int width, int height) =>
        ClientResized?.Invoke(width, height);

    /// <summary>
    /// Native OS window handle. Never throws; 0 if the window is not created.
    /// Windows: HWND. Linux X11: X window id when available, else the GLFW window pointer.
    /// Linux Wayland: surface pointer when available, else the GLFW window pointer.
    /// macOS: NSWindow pointer when available, else the GLFW window pointer.
    /// </summary>
    public static nint NativeHandle
    {
        get
        {
            if (window is null)
                return 0;

            try
            {
                var native = window.Native;
                if (native?.Win32 is { } win32 && win32.Hwnd != 0)
                    return win32.Hwnd;
                if (native?.X11 is { } x11 && x11.Window != 0)
                    return (nint)x11.Window;
                if (native?.Wayland is { } wayland && wayland.Surface != 0)
                    return wayland.Surface;
                if (native?.Cocoa is nint cocoa && cocoa != 0)
                    return cocoa;
            }
            catch
            {
                // Platform tuple missing; fall through to GLFW handle.
            }

            return window.Handle;
        }
    }

    /// <summary>
    /// Current display scale factor (logical-to-physical ratio) applied to UI layout (1.0 = standard 100%).
    /// </summary>
    public static float ScaleFactor => DisplayScale.Factor;

    /// <summary>
    /// Raised when the active display scale factor changes (e.g. window moved to another monitor with a different scale).
    /// </summary>
    public static event Action<float>? ScaleChanged
    {
        add => DisplayScale.ScaleChanged += value;
        remove => DisplayScale.ScaleChanged -= value;
    }

    public static int ClientWidth => (int)Math.Max(0, RenderRect.Width);
    public static int ClientHeight => (int)Math.Max(0, RenderRect.Height);

    public static void SetClientSize(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (window is not null)
        {
            int physicalW = (int)MathF.Ceiling(width * ScaleFactor);
            int physicalH = (int)MathF.Ceiling(height * ScaleFactor);
            window.Size = new Vector2D<int>(physicalW, physicalH);
        }
        else
            HandleClientSize(width, height);
    }

    public static void SetMinClientSize(int width, int height)
    {
        _minClientWidth = Math.Max(0, width);
        _minClientHeight = Math.Max(0, height);
        ApplySizeLimits();
    }

    /// <summary>Pass 0, 0 to clear the maximum (no limit).</summary>
    public static void SetMaxClientSize(int width, int height)
    {
        _maxClientWidth = Math.Max(0, width);
        _maxClientHeight = Math.Max(0, height);
        ApplySizeLimits();
    }

    public static void Center()
    {
        window?.Center();
    }

    public static string Title
    {
        get
        {
            if (window is not null && !string.IsNullOrEmpty(window.Title))
                return window.Title;
            if (ShellApp is not null && !string.IsNullOrWhiteSpace(ShellApp.Title))
                return ShellApp.Title;
            return "Blossom";
        }
        set
        {
            value ??= "";
            if (ShellApp is not null)
                ShellApp.Title = value;
            if (window is not null)
                window.Title = string.IsNullOrWhiteSpace(value) ? "Blossom" : value;
        }
    }

    public static bool IsLoaded { get; private set; } = false;
    public static bool IsRunning { get; } = false;
    public static int TotalRenders { get; private set; }
    public static bool SkipCountingNextRender { get; set; } = false;

    private static readonly System.Collections.Concurrent.ConcurrentQueue<Action> _postQueue = new();
    private static int _uiThreadId;
    private static SynchronizationContext? _previousSyncContext;
    private static readonly object _timerLock = new();
    private static readonly List<PostedTimer> _timerHeap = new();

    /// <summary>True when the caller is already on the window / UI thread.</summary>
    public static bool CheckAccess() =>
        _uiThreadId != 0 && Environment.CurrentManagedThreadId == _uiThreadId;

    public static void Post(Action action)
    {
        if (action == null) return;
        _postQueue.Enqueue(action);
        WakeWait();
    }

    /// <summary>
    /// Queue <paramref name="action"/> on the UI thread and return a task that completes after it runs.
    /// Always queues (does not run inline). Do not <c>Wait</c> this task on the UI thread.
    /// </summary>
    public static Task PostAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try
            {
                action();
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

    /// <summary>Run <paramref name="action"/> on the UI thread after <paramref name="delay"/>. Dispose to cancel.</summary>
    public static IDisposable PostDelayed(TimeSpan delay, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay));
        return ScheduleTimer(delay, TimeSpan.Zero, periodic: false, action);
    }

    /// <summary>
    /// Run <paramref name="action"/> on the UI thread every <paramref name="period"/> (first fire after one period).
    /// Dispose to cancel.
    /// </summary>
    public static IDisposable PostPeriodic(TimeSpan period, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (period <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(period));
        return ScheduleTimer(period, period, periodic: true, action);
    }

    public static string GetClipboardText()
    {
        try
        {
            unsafe
            {
                if (window != null)
                {
                    var glfw = GlfwProvider.GLFW.Value;
                    var glfwWin = (Silk.NET.GLFW.WindowHandle*)window.Handle;
                    return glfw.GetClipboardString(glfwWin) ?? "";
                }
            }
        }
        catch { }
        return "";
    }

    public static void SetClipboardText(string text)
    {
        try
        {
            unsafe
            {
                if (window != null)
                {
                    var glfw = GlfwProvider.GLFW.Value;
                    var glfwWin = (Silk.NET.GLFW.WindowHandle*)window.Handle;
                    glfw.SetClipboardString(glfwWin, text ?? "");
                }
            }
        }
        catch { }
    }

    internal static void DrainPostQueue()
    {
        while (_postQueue.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.Error($"Exception executing posted action:\n{ex}");
            }
        }
    }

    private static void WakeWait()
    {
        if (!IsLoaded)
            return;
        try
        {
            GlfwProvider.GLFW.Value.PostEmptyEvent();
        }
        catch { }
    }

    private static void InstallSynchronizationContext()
    {
        _uiThreadId = Environment.CurrentManagedThreadId;
        if (SynchronizationContext.Current is ShellSynchronizationContext)
            return;
        _previousSyncContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new ShellSynchronizationContext());
    }

    private static IDisposable ScheduleTimer(TimeSpan delay, TimeSpan period, bool periodic, Action action)
    {
        var timer = new PostedTimer
        {
            DueTicks = Stopwatch.GetTimestamp() + ToStopwatchTicks(delay),
            Period = period,
            Action = action,
            IsPeriodic = periodic
        };
        lock (_timerLock)
            TimerPush(timer);
        WakeWait();
        return timer;
    }

    private static long ToStopwatchTicks(TimeSpan span) =>
        (long)(span.TotalSeconds * Stopwatch.Frequency);

    private static void DrainTimers()
    {
        while (true)
        {
            PostedTimer? due;
            long now = Stopwatch.GetTimestamp();
            lock (_timerLock)
            {
                CompactCancelledTimers();
                if (_timerHeap.Count == 0 || _timerHeap[0].DueTicks > now)
                    return;
                due = TimerPop();
            }

            if (due.Cancelled)
                continue;

            try
            {
                due.Action();
            }
            catch (Exception ex)
            {
                Log.Error($"Exception executing timed action:\n{ex}");
            }

            if (!due.IsPeriodic || due.Cancelled)
                continue;

            due.DueTicks = Stopwatch.GetTimestamp() + ToStopwatchTicks(due.Period);
            lock (_timerLock)
            {
                if (!due.Cancelled)
                    TimerPush(due);
            }
        }
    }

    /// <summary>
    /// Seconds until the next scheduled timer, 0 if one is already due, or null to wait indefinitely.
    /// </summary>
    private static double? SecondsUntilNextTimer()
    {
        long due;
        lock (_timerLock)
        {
            CompactCancelledTimers();
            if (_timerHeap.Count == 0)
                return null;
            due = _timerHeap[0].DueTicks;
        }

        long now = Stopwatch.GetTimestamp();
        if (due <= now)
            return 0;
        return (due - now) / (double)Stopwatch.Frequency;
    }

    private static long? NextTimerDueTicks()
    {
        lock (_timerLock)
        {
            CompactCancelledTimers();
            if (_timerHeap.Count == 0)
                return null;
            return _timerHeap[0].DueTicks;
        }
    }

    private static void CompactCancelledTimers()
    {
        while (_timerHeap.Count > 0 && _timerHeap[0].Cancelled)
            TimerPop();
    }

    private static void TimerPush(PostedTimer timer)
    {
        _timerHeap.Add(timer);
        int i = _timerHeap.Count - 1;
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (_timerHeap[parent].DueTicks <= timer.DueTicks)
                break;
            _timerHeap[i] = _timerHeap[parent];
            i = parent;
        }
        _timerHeap[i] = timer;
    }

    private static PostedTimer TimerPop()
    {
        int last = _timerHeap.Count - 1;
        var min = _timerHeap[0];
        var x = _timerHeap[last];
        _timerHeap.RemoveAt(last);
        if (last == 0)
            return min;

        int i = 0;
        int n = _timerHeap.Count;
        while (true)
        {
            int left = i * 2 + 1;
            if (left >= n)
                break;
            int right = left + 1;
            int smallest = (right < n && _timerHeap[right].DueTicks < _timerHeap[left].DueTicks) ? right : left;
            if (x.DueTicks <= _timerHeap[smallest].DueTicks)
                break;
            _timerHeap[i] = _timerHeap[smallest];
            i = smallest;
        }
        _timerHeap[i] = x;
        return min;
    }

    /// <summary>
    /// Block until input, a posted action, or <paramref name="timeoutSeconds"/>.
    /// Null timeout waits indefinitely (no timer and no frame due).
    /// </summary>
    private static void WaitForEvents(double? timeoutSeconds)
    {
        try
        {
            var glfw = GlfwProvider.GLFW.Value;
            if (timeoutSeconds is null)
                glfw.WaitEvents();
            else if (timeoutSeconds.Value <= 0)
                glfw.PollEvents();
            else
                glfw.WaitEventsTimeout(timeoutSeconds.Value);
        }
        catch
        {
            try { window.DoEvents(); } catch { }
        }
    }

    private sealed class PostedTimer : IDisposable
    {
        public long DueTicks;
        public TimeSpan Period;
        public Action Action = null!;
        public bool IsPeriodic;
        public bool Cancelled;

        public void Dispose()
        {
            lock (_timerLock)
            {
                if (Cancelled)
                    return;
                Cancelled = true;
            }
            WakeWait();
        }
    }

    /// <summary>
    /// Marshals <see cref="SynchronizationContext.Post"/> / <see cref="SynchronizationContext.Send"/>
    /// through <see cref="Shell.Post"/> so <c>await</c> after I/O resumes on the window thread.
    /// </summary>
    private sealed class ShellSynchronizationContext : SynchronizationContext
    {
        public override SynchronizationContext CreateCopy() => this;

        public override void Post(SendOrPostCallback d, object? state)
        {
            if (d == null)
                return;
            Shell.Post(() => d(state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (d == null)
                return;

            // Inline on the UI thread. Also inline before the window thread exists so
            // Send during startup does not wait on a pump that has not started.
            if (CheckAccess() || _uiThreadId == 0)
            {
                d(state);
                return;
            }

            Exception? error = null;
            using var done = new ManualResetEventSlim(false);
            Shell.Post(() =>
            {
                try
                {
                    d(state);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    done.Set();
                }
            });
            done.Wait();
            if (error != null)
                ExceptionDispatchInfo.Throw(error);
        }
    }

    static readonly SKColor DefaultBackColor = new(255, 255, 255, 255);
    private static readonly List<(SKRect, SKColor)> PostMarkers = new();
    /// <summary>
    /// Frame-time overlay visibility (top-left). Independent of <see cref="Application.EnableStatsOverlay"/>,
    /// which is what lets F12 toggle this flag. Default false.
    /// </summary>
    public static bool ShowDebugOverlay { get; set; }

    /// <summary>
    /// Hard cap on presented frames per second, independent of the display refresh rate.
    /// Default is 120. Set to 0 to disable the cap (used by throughput benchmarks).
    /// </summary>
    public static int MaxFps
    {
        get => _maxFps;
        set => _maxFps = Math.Max(0, value);
    }

    private static int _maxFps = 120;
    private static long _nextPresentTicks;

    private static Glfw _glfw = null!;
    private static int _minClientWidth;
    private static int _minClientHeight;
    private static int _maxClientWidth;
    private static int _maxClientHeight;

    // GLFW callbacks must be stored so the GC does not collect them (native function pointers).
    private static GlfwCallbacks.KeyCallback? _glfwKeyCallback;
    private static GlfwCallbacks.KeyCallback? _prevGlfwKeyCallback;
    private static GlfwCallbacks.CharCallback? _glfwCharCallback;
    private static GlfwCallbacks.CharCallback? _prevGlfwCharCallback;
    private static bool _glfwCharHooked;
    private static char _pendingHighSurrogate;



    internal static void AddVisualMarker(SKRect marker, SKColor color)
    {
        if (!ShowDebugOverlay) return;

        marker.Inflate(3, 3);
        PostMarkers.Add((marker, color));
    }

    /// <summary>
    /// Hosts <paramref name="application"/> in a native window and runs until the window closes.
    /// </summary>
    public static void Initialize(Application application)
    {
        _uiThreadId = Environment.CurrentManagedThreadId;
        ShellApp = application ?? throw new ArgumentNullException(nameof(application));

        try
        {
            Blossom.Utils.Fonts.EnsureFallbacks();
        }
        catch { }

        OnLoaded = () =>
        {
            ManageInputEvents();

            if (ShellApp.ActiveView != null)
            {
                try
                {
                    ShellApp.ActiveView.Init();
                }
                catch (Exception ex)
                {
                    Log.Fatal("Exception during ActiveView.Init():\n" + ex.ToString());
                    throw;
                }
                ShellApp.ActiveView.IsLoaded = true;
            }
            else
            {
                Log.Warning("No active view");
            }
        };

        SetWindow();
    }

    private static void SetWindow()
    {
        var appWindow = ShellApp.Window;
        int width = Math.Max(1, appWindow.Width);
        int height = Math.Max(1, appWindow.Height);
        if (appWindow.MinWidth > 0)
            width = Math.Max(width, appWindow.MinWidth);
        if (appWindow.MinHeight > 0)
            height = Math.Max(height, appWindow.MinHeight);

        RenderRect = new System.Drawing.RectangleF(0, 0, width, height);
        _minClientWidth = Math.Max(0, appWindow.MinWidth);
        _minClientHeight = Math.Max(0, appWindow.MinHeight);

        GlfwWindowing.Use();
        // SdlWindowing.Use();

        _glfw = Glfw.GetApi();
        _glfw = GlfwProvider.GLFW.Value;

        float initialScale = DisplayScale.GetInitialScale(out int maxWorkW, out int maxWorkH);

        int initialPhysicalW = (int)Math.Min(MathF.Ceiling(width * initialScale), maxWorkW);
        int initialPhysicalH = (int)Math.Min(MathF.Ceiling(height * initialScale), maxWorkH);

        var options = Silk.NET.Windowing.WindowOptions.Default;
        options.Size = new Vector2D<int>(initialPhysicalW, initialPhysicalH);
        options.Title = string.IsNullOrWhiteSpace(ShellApp.Title) ? "Blossom" : ShellApp.Title;
        options.VSync = false;
        options.TransparentFramebuffer = false;
        options.WindowBorder = appWindow.Resizable ? WindowBorder.Resizable : WindowBorder.Fixed;
        options.IsEventDriven = true;
        options.PreferredDepthBufferBits = null;

        options.API = new GraphicsAPI(
    ContextAPI.OpenGL,
    ContextProfile.Core,
    ContextFlags.ForwardCompatible,
    new APIVersion(3, 2));

        DisplayScale.SetupGlfwHints();

        window = Window.Create(options);

        window.Load += Load;
        window.Render += Render;
        window.Closing += Closing;
        window.FileDrop += OnFileDrop;

        DisplayScale.HookWindow(window);

        window.StateChanged += (state) =>
        {
            if (state != WindowState.Minimized)
            {
                HandleClientSize(window.Size.X, window.Size.Y);
                try { GlfwProvider.GLFW.Value.PostEmptyEvent(); } catch { }
            }
        };

        window.Run();
    }

    /// <summary>
    /// Track a live client-area size. Updates <see cref="RenderRect"/>, relayouts the active view,
    /// and raises <see cref="ClientResized"/> only when the size actually changed.
    /// </summary>
    internal static void HandleClientSize(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        if (DisplayScale.CheckScaleChange(clientWidth: width, clientHeight: height))
            return;

        float logicalW = width / DisplayScale.Factor;
        float logicalH = height / DisplayScale.Factor;

        bool changed = Math.Abs(RenderRect.Width - logicalW) > 0.01f || Math.Abs(RenderRect.Height - logicalH) > 0.01f;
        if (!changed)
            return;

        RenderRect = new System.Drawing.RectangleF(0, 0, logicalW, logicalH);
        WasResized = true;

        if (ShellApp?.ActiveView != null)
        {
            ShellApp.ActiveView.FullRenderRequired = true;
            ShellApp.ActiveView.RenderRequired = true;
            ShellApp.ActiveView.ForceLayoutEvaluation();
        }

        try { GlfwProvider.GLFW.Value.PostEmptyEvent(); } catch { }

        ClientResized?.Invoke((int)logicalW, (int)logicalH);
    }

    internal static void ApplySizeLimits()
    {
        // Silk's IWindow exists after Window.Create; GLFW's native window does not
        // until Load. glfwSetWindowSizeLimits asserts window != NULL.
        if (window is null || window.Handle == 0)
            return;

        try
        {
            unsafe
            {
                var glfw = GlfwProvider.GLFW.Value;
                var handle = (Silk.NET.GLFW.WindowHandle*)window.Handle;
                if (handle == null)
                    return;

                float factor = ScaleFactor > 0f ? ScaleFactor : 1f;
                int minW = _minClientWidth > 0 ? (int)MathF.Ceiling(_minClientWidth * factor) : Glfw.DontCare;
                int minH = _minClientHeight > 0 ? (int)MathF.Ceiling(_minClientHeight * factor) : Glfw.DontCare;
                int maxW = _maxClientWidth > 0 ? (int)MathF.Ceiling(_maxClientWidth * factor) : Glfw.DontCare;
                int maxH = _maxClientHeight > 0 ? (int)MathF.Ceiling(_maxClientHeight * factor) : Glfw.DontCare;
                glfw.SetWindowSizeLimits(handle, minW, minH, maxW, maxH);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not apply window size limits: {ex.Message}");
        }
    }

    private static void SetGlfwWindowHints()
    {
        _glfw.DefaultWindowHints();

        if (true)
        {
            _glfw.WindowHint(WindowHintContextApi.ContextCreationApi, ContextApi.NativeContextApi); // ContextApi.EglContextApi
            _glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGL);
            _glfw.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);

            _glfw.WindowHint(WindowHintInt.ContextVersionMajor, 3);
            _glfw.WindowHint(WindowHintInt.ContextVersionMinor, 2);
        }
        // else
        // {
        //     _glfw.WindowHint(WindowHintContextApi.ContextCreationApi,
        //       _automaticFallback || _useEgl ? ContextApi.EglContextApi : ContextApi.NativeContextApi);
        //     _glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGLES);

        //     _glfw.WindowHint(WindowHintInt.ContextVersionMajor, _majOES);
        //     _glfw.WindowHint(WindowHintInt.ContextVersionMinor, 0);
        // }

        _glfw.WindowHint(WindowHintInt.RedBits, 8);
        _glfw.WindowHint(WindowHintInt.GreenBits, 8);
        _glfw.WindowHint(WindowHintInt.BlueBits, 8);
        _glfw.WindowHint(WindowHintInt.DepthBits, 24);
        _glfw.WindowHint(WindowHintInt.StencilBits, 8);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // osx graphics switching
            _glfw.WindowHint((WindowHintBool)0x00023003, true);
        }
    }

    internal static void StartWindow()
    {
        try
        {
            while (!window.IsClosing)
            {
                DrainPostQueue();
                DrainTimers();
                ShellApp.ActiveView?.TriggerLoop();
                DrainPostQueue();
                DrainTimers();

                if (ShellApp.ActiveView?.RenderRequired == true)
                {
                    WaitForFrameSlot();
                    if (window.IsClosing)
                        break;
                    DrainPostQueue();
                    DrainTimers();
                    if (ShellApp.ActiveView?.RenderRequired != true)
                        continue;

                    window.DoRender();

                    if (!SkipCountingNextRender)
                    {
                        TotalRenders++;
                        OnRenderRequired?.Invoke();
                    }
                    else
                    {
                        SkipCountingNextRender = false;
                    }
                }
                else
                {
                    WaitForEvents(SecondsUntilNextTimer());
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal("Exception in StartWindow main loop:\n" + ex.ToString());
            throw;
        }
        finally
        {
            window.Dispose();
        }
    }

    /// <summary>
    /// Block until the next present slot so presented FPS never exceeds <see cref="MaxFps"/>.
    /// Wait timeout is min(next frame, next timer); input and <see cref="Post"/> still wake the wait.
    /// </summary>
    private static void WaitForFrameSlot()
    {
        int maxFps = _maxFps;
        if (maxFps <= 0)
            return;

        long freq = Stopwatch.Frequency;
        long period = Math.Max(1L, freq / maxFps);
        long now = Stopwatch.GetTimestamp();
        long target = _nextPresentTicks;

        if (target <= 0)
        {
            _nextPresentTicks = now + period;
            return;
        }

        while (now < target && !window.IsClosing)
        {
            long waitUntil = target;
            long? timerDue = NextTimerDueTicks();
            if (timerDue is long due && due < waitUntil)
                waitUntil = due;

            double remainSec = (waitUntil - now) / (double)freq;
            if (remainSec <= 0)
            {
                DrainPostQueue();
                DrainTimers();
                now = Stopwatch.GetTimestamp();
                continue;
            }

            WaitForEvents(remainSec);
            DrainPostQueue();
            DrainTimers();
            now = Stopwatch.GetTimestamp();
        }

        now = Stopwatch.GetTimestamp();
        _nextPresentTicks += period;
        if (now - _nextPresentTicks > period)
            _nextPresentTicks = now + period;
    }

    internal static void ChangeCursor(StandardCursor cursor)
    {
        SetCursor(cursor);
    }

    /// <summary>Set a platform standard mouse cursor.</summary>
    private static StandardCursor _appliedStandardCursor = (StandardCursor)(-1);

    public static void SetCursor(StandardCursor cursor)
    {
        if (input == null || input.Mice == null) return;
        if (cursor == _appliedStandardCursor)
            return;
        _appliedStandardCursor = cursor;
        foreach (IMouse mouse in input.Mice)
        {
            try
            {
                var c = mouse.Cursor;
                c.Type = CursorType.Standard;
                c.StandardCursor = cursor;
            }
            catch { }
        }
    }

    /// <summary>Set a custom RGBA mouse cursor (32-bit non-premultiplied little-endian).</summary>
    public static void SetCustomCursor(RawImage image, int hotspotX, int hotspotY)
    {
        if (input == null || input.Mice == null) return;
        _appliedStandardCursor = (StandardCursor)(-1);
        foreach (IMouse mouse in input.Mice)
        {
            try
            {
                var c = mouse.Cursor;
                c.HotspotX = hotspotX;
                c.HotspotY = hotspotY;
                c.Image = image;
                c.Type = CursorType.Custom;
            }
            catch { }
        }
    }

    private static void ManageInputEvents()
    {
        ShellApp.Events.Access = EventAccess.Keyboard;
        input = window.CreateInput();

        // Register keyboard events. Silk's GLFW backend drops InputAction.Repeat;
        // HookGlfwKeyboard wraps the native key callback so we still get IsRepeat.
        HookGlfwKeyboard();

        foreach (IKeyboard keyboard in input.Keyboards)
        {
            keyboard.KeyDown += (IKeyboard _, Key key, int scanCode) =>
                DispatchKeyDown(key, scanCode, isRepeat: false);

            keyboard.KeyUp += (IKeyboard _, Key key, int scanCode) =>
                DispatchKeyUp(key, scanCode);

            if (!_glfwCharHooked)
            {
                keyboard.KeyChar += (IKeyboard _, char ch) =>
                    HandleUtf16Char(ch);
            }
        }

        // Register mouse events
        foreach (IMouse mouse in input.Mice)
        {
            mouse.MouseMove += (IMouse m, Vector2 pos) =>
            {
                if (ShellApp.ActiveView?.PointerCaptureElement != null
                    && !m.IsButtonPressed(Silk.NET.Input.MouseButton.Left)
                    && !m.IsButtonPressed(Silk.NET.Input.MouseButton.Right))
                {
                    ShellApp.ActiveView.ReleasePointerCapture();
                }
                Vector2 logicalPos = ScaleFactor > 0f ? pos / ScaleFactor : pos;
                ShellApp.Events.HandleMouseMove(logicalPos);
                ShellApp.ActiveView?.Events.HandleMouseMove(logicalPos);
            };

            mouse.Scroll += (IMouse _, ScrollWheel wheel) =>
            {
                var pos = new Vector2(wheel.X, wheel.Y);
                ShellApp.Events.HandleMouseScroll(pos);
                ShellApp.ActiveView?.Events.HandleMouseScroll(pos);
            };

            mouse.MouseDown += (IMouse m, Silk.NET.Input.MouseButton btn) =>
            {
                int mouseButton = (int)btn;
                Vector2 logicalPos = ScaleFactor > 0f ? m.Position / ScaleFactor : m.Position;
                ShellApp.Events.HandleMouseDown(mouseButton, logicalPos);
                ShellApp.ActiveView?.Events.HandleMouseDown(mouseButton, logicalPos);
            };

            mouse.MouseUp += (IMouse m, Silk.NET.Input.MouseButton btn) =>
            {
                int mouseButton = (int)btn;
                Vector2 logicalPos = ScaleFactor > 0f ? m.Position / ScaleFactor : m.Position;
                ShellApp.Events.HandleMouseUp(mouseButton, logicalPos);
                ShellApp.ActiveView?.Events.HandleMouseUp(mouseButton, logicalPos);
            };
        }
    }

    private static unsafe void HookGlfwKeyboard()
    {
        _glfwCharHooked = false;
        if (window is null)
            return;

        try
        {
            var glfw = GlfwProvider.GLFW.Value;
            var handle = (WindowHandle*)window.Handle;
            if (handle is null)
                return;

            _glfwKeyCallback = OnGlfwKey;
            _prevGlfwKeyCallback = glfw.SetKeyCallback(handle, _glfwKeyCallback);

            _glfwCharCallback = OnGlfwChar;
            _prevGlfwCharCallback = glfw.SetCharCallback(handle, _glfwCharCallback);
            _glfwCharHooked = true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to hook GLFW keyboard callbacks (key repeat / Unicode text may be limited):\n{ex}");
            _glfwCharHooked = false;
        }
    }

    private static unsafe void OnGlfwKey(
        WindowHandle* glfwWindow,
        Keys glfwKey,
        int scanCode,
        InputAction action,
        KeyModifiers mods)
    {
        _prevGlfwKeyCallback?.Invoke(glfwWindow, glfwKey, scanCode, action, mods);

        // Silk already maps Press → KeyDown and Release → KeyUp. Repeat is dropped there.
        if (action != InputAction.Repeat)
            return;

        var key = (Key)glfwKey;
        DispatchKeyDown(key, scanCode, isRepeat: true);
    }

    private static unsafe void OnGlfwChar(WindowHandle* glfwWindow, uint codepoint)
    {
        _prevGlfwCharCallback?.Invoke(glfwWindow, codepoint);

        if (codepoint > 0x10FFFF || (codepoint >= 0xD800 && codepoint <= 0xDFFF))
            return;

        DispatchText(char.ConvertFromUtf32((int)codepoint));
    }

    private static void HandleUtf16Char(char ch)
    {
        if (char.IsHighSurrogate(ch))
        {
            _pendingHighSurrogate = ch;
            return;
        }

        if (_pendingHighSurrogate != '\0' && char.IsLowSurrogate(ch))
        {
            DispatchText(new string(new[] { _pendingHighSurrogate, ch }));
            _pendingHighSurrogate = '\0';
            return;
        }

        if (_pendingHighSurrogate != '\0')
        {
            DispatchText(_pendingHighSurrogate.ToString());
            _pendingHighSurrogate = '\0';
        }

        DispatchText(ch.ToString());
    }

    private static void DispatchKeyDown(Key key, int scanCode, bool isRepeat)
    {
        if (ShellApp is null)
            return;

        if (ShellApp.EnableStatsOverlay && key == Key.F12)
        {
            if (!isRepeat)
            {
                ShowDebugOverlay = !ShowDebugOverlay;
                if (ShellApp.ActiveView != null)
                {
                    ShellApp.ActiveView.FullRenderRequired = true;
                    ShellApp.ActiveView.RenderRequired = true;
                }
            }
            return;
        }

        var appEvents = ShellApp.Events;
        var view = ShellApp.ActiveView;
        var focused = view?.ActiveKeyboardElement;

        if (!isRepeat)
        {
            appEvents.TrackKeyDown(key);
            view?.Events.TrackKeyDown(key);
            for (var el = focused; el != null; el = el.Parent)
                el.Events.TrackKeyDown(key);
        }

        var e = new KeyEvent
        {
            Key = key,
            ScanCode = scanCode,
            IsRepeat = isRepeat,
            Control = appEvents.IsControlDown,
            Alt = appEvents.IsAltDown,
            Shift = appEvents.IsShiftDown,
            Super = appEvents.IsSuperDown
        };

        for (var el = focused; el != null && !e.Handled; el = el.Parent)
            el.Events.RaiseKeyDown(e);

        if (!e.Handled)
            view?.Events.RaiseKeyDown(e);

        if (!e.Handled)
            appEvents.RaiseKeyDown(e);

        if (!e.Handled && !isRepeat)
        {
            for (var el = focused; el != null && !e.Handled; el = el.Parent)
            {
                if (el.Events.TryInvokeHotkeys())
                    e.Handled = true;
            }

            if (!e.Handled && view != null && view.Events.TryInvokeHotkeys())
                e.Handled = true;

            if (!e.Handled && appEvents.TryInvokeHotkeys())
                e.Handled = true;
        }

        if (!e.Handled && key == Key.Tab && !e.Control && !e.Alt && !e.Super && view != null)
        {
            if (view.TryHandleDefaultTab(e.Shift))
                e.Handled = true;
        }
    }

    private static void DispatchKeyUp(Key key, int scanCode)
    {
        if (ShellApp is null)
            return;

        var appEvents = ShellApp.Events;
        var view = ShellApp.ActiveView;
        var focused = view?.ActiveKeyboardElement;

        var e = new KeyEvent
        {
            Key = key,
            ScanCode = scanCode,
            IsRepeat = false,
            Control = appEvents.IsControlDown,
            Alt = appEvents.IsAltDown,
            Shift = appEvents.IsShiftDown,
            Super = appEvents.IsSuperDown
        };

        appEvents.TrackKeyUp(key);
        view?.Events.TrackKeyUp(key);
        for (var el = focused; el != null; el = el.Parent)
            el.Events.TrackKeyUp(key);

        for (var el = focused; el != null && !e.Handled; el = el.Parent)
            el.Events.RaiseKeyUp(e);

        if (!e.Handled)
            view?.Events.RaiseKeyUp(e);

        if (!e.Handled)
            appEvents.RaiseKeyUp(e);
    }

    private static void DispatchText(string text)
    {
        if (ShellApp is null || string.IsNullOrEmpty(text))
            return;

        var e = new TextEvent { Text = text };
        var view = ShellApp.ActiveView;
        var focused = view?.ActiveKeyboardElement;

        for (var el = focused; el != null && !e.Handled; el = el.Parent)
            el.Events.RaiseTextInput(e);

        if (!e.Handled)
            view?.Events.RaiseTextInput(e);

        if (!e.Handled)
            ShellApp.Events.RaiseTextInput(e);
    }

    private static void Closing()
    {
        DisplayScale.Shutdown();

        ShellApp.Dispose();
        if (SynchronizationContext.Current is ShellSynchronizationContext)
            SynchronizationContext.SetSynchronizationContext(_previousSyncContext);
        _uiThreadId = 0;
    }

    private static void OnFileDrop(string[] paths)
    {
        if (paths == null || paths.Length == 0)
            return;

        try
        {
            FilesDropped?.Invoke(paths);
        }
        catch (Exception ex)
        {
            Log.Error($"FilesDropped handler failed:\n{ex}");
        }

        try
        {
            ShellApp?.Events.HandleFilesDropped(paths);
            ShellApp?.ActiveView?.Events.HandleFilesDropped(paths);
        }
        catch (Exception ex)
        {
            Log.Error($"View/application file-drop handler failed:\n{ex}");
        }
    }

    private static void LoadLogo()
    {
        try
        {
            unsafe
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icon.png");
                if (!File.Exists(iconPath))
                    iconPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "icon.png");
                if (!File.Exists(iconPath))
                    throw new FileNotFoundException("assets/icon.png not found next to the app or in the working directory.");

                using var image = Image.Load<Rgba32>(iconPath);
                var memoryGroup = image.GetPixelMemoryGroup();
                Memory<byte> array = new byte[memoryGroup.TotalLength * sizeof(Rgba32)];
                var block = MemoryMarshal.Cast<byte, Rgba32>(array.Span);
                foreach (var memory in memoryGroup)
                {
                    memory.Span.CopyTo(block);
                    block = block[memory.Length..];
                }

                var icon = new RawImage(image.Width, image.Height, array);
                window.SetWindowIcon(ref icon);
                Log.Info("Logo loaded");
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not load window icon: {ex.Message}");
        }
    }

    private static void Load()
    {
        InstallSynchronizationContext();
        IsLoaded = true;

        DisplayScale.Initialize(window);
        ApplySizeLimits();

        if (ShellApp.Window.CenterOnLoad)
            window.Center();
        Renderer.SetCanvas(window);
        OnLoaded.Invoke();

        LoadLogo();

        HandleClientSize(window.Size.X, window.Size.Y);
        WasResized = true; // Ensure full render on startup
        StartWindow();
    }

    private static readonly Stopwatch frameTimer = new();
    private static int frameCounter = 0;
    private static readonly double[] frameTimes = new double[20];

    static Shell()
    {
        Array.Fill(frameTimes, 1.0);
    }

    private static readonly SKPaint PostMarkerPaint = new()
    {
        StrokeWidth = 3f,
        Color = SKColors.Red,
        Style = SKPaintStyle.Stroke,
    };

    private static readonly SKPaint HudBgPaint = new()
    {
        Color = new SKColor(24, 24, 24, 255), // Pure neutral dark gray #181818
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint HudBorderPaint = new()
    {
        StrokeWidth = 1f,
        Color = new SKColor(255, 255, 255, 255), // Crisp pure white #FFFFFF
        Style = SKPaintStyle.Stroke,
        IsAntialias = true,
    };

    private static readonly SKPaint HudStatusPipPaint = new()
    {
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private static SKFont HudFont(float size, int weight) =>
        Blossom.Utils.Fonts.CreateFont(
            Blossom.Utils.Fonts.GetTypeface("Noto Sans, Inter, Roboto, DejaVu Sans, Liberation Sans, sans-serif", weight: weight),
            size);

    private static readonly SKFont HudHeaderTitleFont = HudFont(13.5f, 600);
    private static readonly SKPaint HudHeaderTitlePaint = new()
    {
        Color = new SKColor(255, 255, 255, 255), // Pure white #FFFFFF
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private static readonly SKPaint HudSeparatorPaint = new()
    {
        StrokeWidth = 1f,
        Color = new SKColor(55, 55, 55, 255), // Pure neutral gray #373737
        Style = SKPaintStyle.Stroke,
        IsAntialias = true,
    };

    private static readonly SKFont HudMetricLabelFont = HudFont(10f, 600);
    private static readonly SKPaint HudMetricLabelPaint = new()
    {
        Color = new SKColor(150, 150, 150, 255), // Pure neutral medium gray #969696
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private static readonly SKFont HudFpsValueFont = HudFont(26f, 700);
    private static readonly SKPaint HudFpsValuePaint = new()
    {
        Color = new SKColor(16, 185, 129, 255), // Emerald-500
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private static readonly SKFont HudFrameTimeValueFont = HudFont(26f, 700);
    private static readonly SKPaint HudFrameTimeValuePaint = new()
    {
        Color = new SKColor(255, 255, 255, 255), // Crisp pure white #FFFFFF
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private static readonly SKFont HudMetricUnitFont = HudFont(12f, 600);
    private static readonly SKPaint HudMetricUnitPaint = new()
    {
        Color = new SKColor(130, 130, 130, 255), // Pure neutral gray #828282
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private static readonly SKFont HudDiagLabelFont = HudFont(10.5f, 600);
    private static readonly SKPaint HudDiagLabelPaint = new()
    {
        Color = new SKColor(140, 140, 140, 255), // Pure neutral gray #8C8C8C
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private static readonly SKFont HudDiagValueFont = HudFont(11.5f, 500);
    private static readonly SKPaint HudDiagValuePaint = new()
    {
        Color = new SKColor(235, 235, 235, 255), // Crisp pure light gray #EBEBEB
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private static void DrawDebugOverlay(double avgDrawMs, double theoreticalFps)
    {
        float overlayScale = Shell.RenderRect.Width > 0 ? (float)Renderer.FramebufferWidth / Shell.RenderRect.Width : 1f;
        using var _ = new SKAutoCanvasRestore(Renderer.Canvas);
        if (overlayScale > 0f && overlayScale != 1f)
            Renderer.Canvas.Scale(overlayScale, overlayScale);

        // Draw informational markers
        foreach (var (rect, color) in PostMarkers)
        {
            PostMarkerPaint.Color = color;
            PostMarkerPaint.PathEffect?.Dispose();
            PostMarkerPaint.PathEffect = SKPathEffect.CreateDash(new float[] { 3, 10 }, Random.Shared.Next(0, 1000));
            Renderer.Canvas.DrawRect(rect, PostMarkerPaint);
        }

        const float panelX = 14f;
        const float panelY = 14f;
        const float panelW = 240f;
        const float panelH = 258f;

        var bgRect = SKRect.Create(panelX, panelY, panelW, panelH);
        var borderRect = SKRect.Create(panelX + 0.5f, panelY + 0.5f, panelW - 1f, panelH - 1f);

        // 1. Pure dark gray panel with crisp white border
        // Insetting stroke by 0.5px aligns the 1px line precisely to the pixel grid within bgRect,
        // preventing fractional anti-aliasing spillover outside the panel bounds that causes flicker during re-renders.
        Renderer.Canvas.DrawRect(bgRect, HudBgPaint);
        Renderer.Canvas.DrawRect(borderRect, HudBorderPaint);

        // Performance color coding based on theoretical FPS / draw latency
        SKColor perfColor;
        if (theoreticalFps >= 60.0 || avgDrawMs <= 16.667)
            perfColor = new SKColor(16, 185, 129, 255); // Emerald green
        else if (theoreticalFps >= 30.0 || avgDrawMs <= 33.333)
            perfColor = new SKColor(245, 158, 11, 255); // Amber
        else
            perfColor = new SKColor(239, 68, 68, 255);  // Crimson red

        // 2. Header — Clean "Stats" with live square indicator pip
        float headerY = panelY;
        HudStatusPipPaint.Color = perfColor;
        Renderer.Canvas.DrawRect(SKRect.Create(panelX + 14, headerY + 13, 6, 6), HudStatusPipPaint);
        Renderer.Canvas.DrawText("Stats", panelX + 26, headerY + 22, SKTextAlign.Left, HudHeaderTitleFont, HudHeaderTitlePaint);

        // Separator line under header flush with panel borders
        float div1Y = headerY + 32.5f;
        Renderer.Canvas.DrawLine(panelX + 1f, div1Y, panelX + panelW - 1f, div1Y, HudSeparatorPaint);

        // 3. Primary metrics — stacked vertically
        // Block 1: FPS
        float fpsBlockY = headerY + 32f;
        Renderer.Canvas.DrawText("FPS", panelX + 14, fpsBlockY + 18, SKTextAlign.Left, HudMetricLabelFont, HudMetricLabelPaint);
        HudFpsValuePaint.Color = perfColor;
        string fpsStr = theoreticalFps >= 1000.0 ? $"{theoreticalFps:0}" : (theoreticalFps > 0 ? $"{theoreticalFps:0.0}" : "--.-");
        Renderer.Canvas.DrawText(fpsStr, panelX + 14, fpsBlockY + 45, SKTextAlign.Left, HudFpsValueFont, HudFpsValuePaint);

        // Separator between metrics
        float div2Y = fpsBlockY + 54.5f;
        Renderer.Canvas.DrawLine(panelX + 1f, div2Y, panelX + panelW - 1f, div2Y, HudSeparatorPaint);

        // Block 2: Frame Draw Time
        float ftBlockY = fpsBlockY + 54f;
        Renderer.Canvas.DrawText("FRAME DRAW", panelX + 14, ftBlockY + 18, SKTextAlign.Left, HudMetricLabelFont, HudMetricLabelPaint);
        string ftStr = $"{avgDrawMs:0.00}";
        Renderer.Canvas.DrawText(ftStr, panelX + 14, ftBlockY + 45, SKTextAlign.Left, HudFrameTimeValueFont, HudFrameTimeValuePaint);
        float ftValWidth = HudFrameTimeValueFont.MeasureText(ftStr);
        Renderer.Canvas.DrawText(" ms", panelX + 14 + ftValWidth, ftBlockY + 45, SKTextAlign.Left, HudMetricUnitFont, HudMetricUnitPaint);

        // Separator between metrics and diagnostics
        float div3Y = ftBlockY + 54.5f;
        Renderer.Canvas.DrawLine(panelX + 1f, div3Y, panelX + panelW - 1f, div3Y, HudSeparatorPaint);

        // 4. Secondary Diagnostics — each metric on its own line with generous vertical gap
        float diagBlockY = ftBlockY + 54f;
        float scale = Shell.RenderRect.Width > 0 ? (float)Renderer.FramebufferWidth / Shell.RenderRect.Width : 1f;
        int elementCount = ShellApp.ActiveView?.Elements.Count ?? 0;

        float rowY = diagBlockY + 20f;
        const float rowGap = 21f;
        const float valColX = panelX + 68f;

        // Row 1: Resolution & Scale
        Renderer.Canvas.DrawText("RES", panelX + 14, rowY, SKTextAlign.Left, HudDiagLabelFont, HudDiagLabelPaint);
        Renderer.Canvas.DrawText($"{Renderer.FramebufferWidth}x{Renderer.FramebufferHeight} ({scale:0.0}x)", valColX, rowY, SKTextAlign.Left, HudDiagValueFont, HudDiagValuePaint);
        rowY += rowGap;

        // Row 2: Node Count
        Renderer.Canvas.DrawText("NODES", panelX + 14, rowY, SKTextAlign.Left, HudDiagLabelFont, HudDiagLabelPaint);
        Renderer.Canvas.DrawText($"{elementCount}", valColX, rowY, SKTextAlign.Left, HudDiagValueFont, HudDiagValuePaint);
        rowY += rowGap;

        // Row 3: Active View
        string viewName = ShellApp.ActiveView?.Name ?? "None";
        if (viewName.Length > 15) viewName = viewName[..14] + "…";
        Renderer.Canvas.DrawText("VIEW", panelX + 14, rowY, SKTextAlign.Left, HudDiagLabelFont, HudDiagLabelPaint);
        Renderer.Canvas.DrawText(viewName, valColX, rowY, SKTextAlign.Left, HudDiagValueFont, HudDiagValuePaint);
        rowY += rowGap;

        // Row 4: Memory Usage
        double memMb = (double)GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        Renderer.Canvas.DrawText("MEM", panelX + 14, rowY, SKTextAlign.Left, HudDiagLabelFont, HudDiagLabelPaint);
        Renderer.Canvas.DrawText($"{memMb:0.0} MB", valColX, rowY, SKTextAlign.Left, HudDiagValueFont, HudDiagValuePaint);
        rowY += rowGap;

        // Row 5: Frame Counter
        Renderer.Canvas.DrawText("FRAME", panelX + 14, rowY, SKTextAlign.Left, HudDiagLabelFont, HudDiagLabelPaint);
        Renderer.Canvas.DrawText($"#{TotalRenders}", valColX, rowY, SKTextAlign.Left, HudDiagValueFont, HudDiagValuePaint);

        // Clean-up
        PostMarkers.Clear();
    }

    private static void Render(double time)
    {
        DrainPostQueue();
        Blossom.Core.Visual.SKSLShaderTimeTracker.DeltaTime = (float)time;
        Blossom.Core.Visual.SKSLShaderTimeTracker.ElapsedSeconds += (float)time;

        Renderer.ResetContext();
        // Renderer.Canvas.Clear(ShellApp.ActiveView?.BackColor ?? DefaultBackColor);
        
        if (Blossom.Core.BenchmarkManager.IsBenchmarkMode)
        {
            Blossom.Core.BenchmarkManager.StartFrame(ShellApp.ActiveView?.Name ?? "Unknown");
        }

        frameTimer.Restart();
        ShellApp.Render();
        frameTimer.Stop();

        if (Blossom.Core.BenchmarkManager.IsBenchmarkMode)
        {
            Blossom.Core.BenchmarkManager.EndFrame();
        }

        string title = !string.IsNullOrWhiteSpace(ShellApp.Title)
            ? ShellApp.Title
            : (ShellApp.ActiveView?.Name ?? "Blossom");
        if (window.Title != title)
            window.Title = title;

        double drawMs = frameTimer.Elapsed.TotalMilliseconds;
        frameTimes[frameCounter] = drawMs;
        frameCounter = (frameCounter + 1) % frameTimes.Length;

        double totalMs = 0;
        int count = 0;
        foreach (double t in frameTimes)
        {
            if (t > 0)
            {
                totalMs += t;
                count++;
            }
        }
        double avgDrawMs = count > 0 ? totalMs / count : drawMs;

        // Theoretical FPS: calculated directly from frame draw time (1000.0 / avgDrawMs)
        double theoreticalFps = avgDrawMs > 0.05 ? 1000.0 / avgDrawMs : 9999.0;

        if (ShowDebugOverlay)
        {
            DrawDebugOverlay(avgDrawMs, theoreticalFps);
        }
        else if (PostMarkers.Count > 0)
        {
            PostMarkers.Clear();
        }

        WasResized = false;

        Renderer.FlushToScreen();
    }
}