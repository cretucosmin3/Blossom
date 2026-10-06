using System;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Blossom.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.GLFW;
using Silk.NET.Windowing;

namespace Blossom.Platform;

/// <summary>
/// Manages platform-specific DPI scaling, window content scale detection, and live display scale changes.
/// </summary>
public static class DisplayScale
{
    public const float MinFactor = 1f;
    public const float MaxFactor = 4f;

    /// <summary>
    /// Current display scale factor (logical-to-physical ratio) applied to UI layout (1.0 = standard 100%).
    /// </summary>
    public static float Factor { get; internal set; } = 1f;

    /// <summary>
    /// Raised when the active display scale factor changes (e.g. display settings changed or window moved across monitors).
    /// </summary>
    public static event Action<float>? ScaleChanged;

    internal static IWindow? Window { get; set; }

    private static nint _getContentScalePtr;
    private static nint _setContentScaleCallbackPtr;
    private static nint _prevScaleCallback;

    private static bool _x11Initialized;
    private static nint _x11Lib;
    private static unsafe delegate* unmanaged[Cdecl]<nint, nint> _xOpenDisplay;
    private static unsafe delegate* unmanaged[Cdecl]<nint, nint> _xResourceManagerString;
    private static unsafe delegate* unmanaged[Cdecl]<nint, int> _xCloseDisplay;

    private static bool _win32Initialized;
    private static nint _user32Lib;
    private static unsafe delegate* unmanaged[Stdcall]<nint, uint> _getDpiForWindow;
    private static unsafe delegate* unmanaged[Cdecl]<WindowHandle*, nint> _glfwGetWin32Window;

    private static System.Threading.Timer? _scalePollTimer;

    #region Platform Interop (X11 & Win32)

    private static unsafe void EnsureX11Interop()
    {
        if (_x11Initialized) return;
        _x11Initialized = true;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

        try
        {
            if (!NativeLibrary.TryLoad("libX11.so.6", out _x11Lib) &&
                !NativeLibrary.TryLoad("libX11.so", out _x11Lib))
                return;

            if (NativeLibrary.TryGetExport(_x11Lib, "XOpenDisplay", out var openPtr))
                _xOpenDisplay = (delegate* unmanaged[Cdecl]<nint, nint>)openPtr;
            if (NativeLibrary.TryGetExport(_x11Lib, "XResourceManagerString", out var resPtr))
                _xResourceManagerString = (delegate* unmanaged[Cdecl]<nint, nint>)resPtr;
            if (NativeLibrary.TryGetExport(_x11Lib, "XCloseDisplay", out var closePtr))
                _xCloseDisplay = (delegate* unmanaged[Cdecl]<nint, int>)closePtr;
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not initialize X11 interop: {ex.Message}");
        }
    }

    private static unsafe float QueryLinuxX11Dpi()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return 0f;

        EnsureX11Interop();
        if (_xOpenDisplay == null || _xResourceManagerString == null || _xCloseDisplay == null)
            return 0f;

        try
        {
            nint dpy = _xOpenDisplay(0);
            if (dpy == 0) return 0f;
            try
            {
                nint strPtr = _xResourceManagerString(dpy);
                if (strPtr == 0) return 0f;
                string? rm = Marshal.PtrToStringAnsi(strPtr);
                if (string.IsNullOrEmpty(rm)) return 0f;

                foreach (var line in rm.Split('\n'))
                {
                    if (line.StartsWith("Xft.dpi:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(':', 2);
                        if (parts.Length == 2 && float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float dpi) && dpi > 0f)
                        {
                            return dpi / 96f;
                        }
                    }
                }
            }
            finally
            {
                _xCloseDisplay(dpy);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to query X11 DPI: {ex.Message}");
        }
        return 0f;
    }

    private static unsafe void EnsureWin32DpiInterop()
    {
        if (_win32Initialized) return;
        _win32Initialized = true;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        try
        {
            if (NativeLibrary.TryLoad("user32.dll", out _user32Lib))
            {
                if (NativeLibrary.TryGetExport(_user32Lib, "GetDpiForWindow", out var dpiPtr))
                    _getDpiForWindow = (delegate* unmanaged[Stdcall]<nint, uint>)dpiPtr;
            }

            var glfw = GlfwProvider.GLFW.Value;
            var ctx = typeof(NativeApiContainer).GetField("_ctx", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(glfw) as INativeContext;
            if (ctx != null && ctx.TryGetProcAddress("glfwGetWin32Window", out var win32Ptr))
            {
                _glfwGetWin32Window = (delegate* unmanaged[Cdecl]<WindowHandle*, nint>)win32Ptr;
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not initialize Win32 DPI interop: {ex.Message}");
        }
    }

    private static unsafe float QueryWindowsDpi(WindowHandle* handle)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return 0f;

        EnsureWin32DpiInterop();
        if (_getDpiForWindow != null && _glfwGetWin32Window != null && handle != null)
        {
            try
            {
                nint hwnd = _glfwGetWin32Window(handle);
                if (hwnd != 0)
                {
                    uint dpi = _getDpiForWindow(hwnd);
                    if (dpi > 0)
                        return dpi / 96f;
                }
            }
            catch { }
        }
        return 0f;
    }

    #endregion

    #region Monitor & GLFW Interop

    private static unsafe Silk.NET.GLFW.Monitor* GetCurrentMonitor(WindowHandle* handle)
    {
        var glfw = GlfwProvider.GLFW.Value;
        if (handle == null) return glfw.GetPrimaryMonitor();
        try
        {
            glfw.GetWindowPos(handle, out int x, out int y);
            glfw.GetWindowSize(handle, out int width, out int height);
            int cx = x + width / 2, cy = y + height / 2;
            Silk.NET.GLFW.Monitor** monitors = glfw.GetMonitors(out int count);
            Silk.NET.GLFW.Monitor* chosen = glfw.GetPrimaryMonitor();
            for (int i = 0; i < count; i++)
            {
                glfw.GetMonitorWorkarea(monitors[i], out int mx, out int my, out int mw, out int mh);
                if (cx >= mx && cx < mx + mw && cy >= my && cy < my + mh)
                {
                    chosen = monitors[i];
                    break;
                }
            }
            return chosen != null ? chosen : glfw.GetPrimaryMonitor();
        }
        catch
        {
            return glfw.GetPrimaryMonitor();
        }
    }

    private static void EnsureGlfwScaleInterop()
    {
        if (_getContentScalePtr != 0)
            return;

        try
        {
            var glfw = GlfwProvider.GLFW.Value;
            var ctx = typeof(NativeApiContainer).GetField("_ctx", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(glfw) as INativeContext;
            if (ctx != null)
            {
                ctx.TryGetProcAddress("glfwGetWindowContentScale", out _getContentScalePtr);
                ctx.TryGetProcAddress("glfwSetWindowContentScaleCallback", out _setContentScaleCallbackPtr);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not resolve glfwGetWindowContentScale: {ex.Message}");
        }
    }

    private static unsafe float QueryWindowContentScale(WindowHandle* handle)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            float winDpi = QueryWindowsDpi(handle);
            if (winDpi > 0f) return winDpi;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            float linuxDpi = QueryLinuxX11Dpi();
            if (linuxDpi > 0f) return linuxDpi;
        }

        EnsureGlfwScaleInterop();
        if (_getContentScalePtr != 0 && handle != null)
        {
            float xScale = 1f, yScale = 1f;
            ((delegate* unmanaged[Cdecl]<WindowHandle*, float*, float*, void>)_getContentScalePtr)(handle, &xScale, &yScale);
            if (xScale > 0f && float.IsFinite(xScale))
                return xScale;
        }

        try
        {
            var monitor = GetCurrentMonitor(handle);
            if (monitor != null)
            {
                GlfwProvider.GLFW.Value.GetMonitorContentScale(monitor, out float mx, out _);
                if (mx > 0f && float.IsFinite(mx))
                    return mx;
            }
        }
        catch { }

        return 1f;
    }

    #endregion

    #region Factor Computation & Scale Changes

    /// <summary>
    /// Computes the layout scale factor for the given GLFW window handle.
    /// </summary>
    public static unsafe float ComputeFactor(WindowHandle* handle, float? forcedContentScale = null)
    {
        if (handle == null)
            return Factor;

        var glfw = GlfwProvider.GLFW.Value;
        glfw.GetWindowSize(handle, out int width, out int height);
        glfw.GetFramebufferSize(handle, out int fbWidth, out int fbHeight);
        if (width <= 0 || height <= 0 || fbWidth <= 0 || fbHeight <= 0)
            return Factor;

        float contentScale = forcedContentScale ?? QueryWindowContentScale(handle);

        // The portion of the content scale the framebuffer does not already carry:
        // - On Windows and X11: width == fbWidth, so factor == contentScale (e.g. 1.75).
        // - On macOS (and Wayland): fbWidth == contentScale * width, so factor == 1.0 (framebuffer already scaled).
        float factor = Math.Clamp(MathF.Round(contentScale * width / fbWidth * 100f) / 100f, MinFactor, MaxFactor);
        return float.IsFinite(factor) && factor > 0f ? factor : 1f;
    }

    /// <summary>
    /// Checks whether the display scale factor has changed. If so, updates layout bounds,
    /// clears render caches, re-evaluates the active view, and raises scale change events.
    /// </summary>
    public static unsafe bool CheckScaleChange(float? forcedContentScale = null, int? clientWidth = null, int? clientHeight = null)
    {
        var win = Window ?? Shell.window;
        if (win is null || win.Handle == 0)
            return false;

        float newFactor = ComputeFactor((WindowHandle*)win.Handle, forcedContentScale);
        if (Math.Abs(Factor - newFactor) <= 0.001f)
            return false;

        Log.Info($"Display scale changed: {Factor:0.##} -> {newFactor:0.##}");
        Factor = newFactor;

        Shell.ApplySizeLimits();

        int winW = clientWidth ?? (win.Size.X > 0 ? win.Size.X : 1);
        int winH = clientHeight ?? (win.Size.Y > 0 ? win.Size.Y : 1);
        float logicalW = winW / Factor;
        float logicalH = winH / Factor;

        Shell.RenderRect = new RectangleF(0, 0, logicalW, logicalH);
        Shell.WasResized = true;

        if (Shell.ShellApp?.ActiveView is { } view)
        {
            view.InvalidateAll();
        }

        ScaleChanged?.Invoke(Factor);
        Shell.RaiseClientResized((int)logicalW, (int)logicalH);

        try { GlfwProvider.GLFW.Value.PostEmptyEvent(); } catch { }
        return true;
    }

    #endregion

    #region Window & Lifecycle Setup

    /// <summary>
    /// Computes the initial scale and maximum working area for window creation before the window handle exists.
    /// </summary>
    public static unsafe float GetInitialScale(out int maxWorkW, out int maxWorkH)
    {
        float initialScale = 1f;
        maxWorkW = int.MaxValue;
        maxWorkH = int.MaxValue;

        try
        {
            var glfw = GlfwProvider.GLFW.Value;
            var primary = glfw.GetPrimaryMonitor();
            if (primary != null)
            {
                glfw.GetMonitorWorkarea(primary, out _, out _, out int workW, out int workH);
                if (workW > 0 && workH > 0)
                {
                    maxWorkW = (int)(workW * 0.9f);
                    maxWorkH = (int)(workH * 0.9f);
                }

                // On macOS, Cocoa window dimensions are already in points, so do not multiply window.Size
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        float linuxDpi = QueryLinuxX11Dpi();
                        if (linuxDpi > 0f)
                            initialScale = Math.Clamp(linuxDpi, MinFactor, MaxFactor);
                    }

                    if (initialScale <= 1f)
                    {
                        glfw.GetMonitorContentScale(primary, out float mx, out _);
                        if (mx > 0f && float.IsFinite(mx))
                            initialScale = Math.Clamp(mx, MinFactor, MaxFactor);
                    }
                }
            }
        }
        catch { }

        return initialScale;
    }

    /// <summary>
    /// Sets GLFW window hints relevant to display scaling (e.g. GLFW_SCALE_TO_MONITOR).
    /// </summary>
    public static void SetupGlfwHints()
    {
        try
        {
            var glfw = GlfwProvider.GLFW.Value;
            glfw.WindowHint((WindowHintBool)0x0002200C, true); // GLFW_SCALE_TO_MONITOR
        }
        catch { }
    }

    /// <summary>
    /// Hooks window focus and movement events to detect display scale transitions across monitors and settings changes.
    /// </summary>
    public static void HookWindow(IWindow window)
    {
        Window = window;

        window.FocusChanged += focused =>
        {
            if (focused)
                CheckScaleChange();
        };

        window.Move += _ => CheckScaleChange();
    }

    /// <summary>
    /// Initializes display scale callbacks and background polling when the window has loaded.
    /// </summary>
    public static unsafe void Initialize(IWindow window)
    {
        Window = window;

        HookGlfwScaleCallback();
        CheckScaleChange();

        _scalePollTimer?.Dispose();
        _scalePollTimer = new System.Threading.Timer(_ =>
        {
            var win = Window ?? Shell.window;
            if (win is null || win.Handle == 0 || win.IsClosing)
                return;

            try
            {
                float current = Factor;
                float candidate = ComputeFactor((WindowHandle*)win.Handle);
                if (Math.Abs(current - candidate) > 0.001f)
                {
                    Shell.Post(() => CheckScaleChange());
                }
            }
            catch { }
        }, null, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Cleans up display scale callbacks and background polling timers when shutting down.
    /// </summary>
    public static unsafe void Shutdown()
    {
        _scalePollTimer?.Dispose();
        _scalePollTimer = null;

        UnhookGlfwScaleCallback();
        Window = null;
    }

    private static unsafe void HookGlfwScaleCallback()
    {
        var win = Window ?? Shell.window;
        if (win is null || win.Handle == 0)
            return;

        EnsureGlfwScaleInterop();
        if (_setContentScaleCallbackPtr == 0)
            return;

        try
        {
            var handle = (WindowHandle*)win.Handle;
            var set = (delegate* unmanaged[Cdecl]<WindowHandle*, delegate* unmanaged[Cdecl]<WindowHandle*, float, float, void>, nint>)_setContentScaleCallbackPtr;
            _prevScaleCallback = set(handle, &OnGlfwContentScale);
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not hook glfwSetWindowContentScaleCallback: {ex.Message}");
        }
    }

    private static unsafe void UnhookGlfwScaleCallback()
    {
        var win = Window ?? Shell.window;
        if (win is not null && win.Handle != 0 && _setContentScaleCallbackPtr != 0)
        {
            try
            {
                var handle = (WindowHandle*)win.Handle;
                var set = (delegate* unmanaged[Cdecl]<WindowHandle*, delegate* unmanaged[Cdecl]<WindowHandle*, float, float, void>, nint>)_setContentScaleCallbackPtr;
                set(handle, null);
            }
            catch { }
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnGlfwContentScale(WindowHandle* handle, float xScale, float yScale)
    {
        try
        {
            if (_prevScaleCallback != 0)
            {
                ((delegate* unmanaged[Cdecl]<WindowHandle*, float, float, void>)_prevScaleCallback)(handle, xScale, yScale);
            }

            CheckScaleChange(xScale > 0f && float.IsFinite(xScale) ? xScale : null);
        }
        catch (Exception ex)
        {
            Log.Error($"Display scale callback failed: {ex}");
        }
    }

    #endregion
}
