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
using Blossom.Core.Delegates.Common;
using System;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using System.Runtime.InteropServices;
using SkiaSharp;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Silk.NET.Windowing.Sdl;
using Silk.NET.GLFW;

namespace Blossom;

public static class Browser
{
    private static IInputContext input;

    internal static IWindow window;
    internal static Application BrowserApp = null!;
    internal static RectangleF RenderRect = new(0, 0, 0, 0);
    internal static bool WasResized;

    internal static Action OnRenderRequired;

    public static event ForVoid OnLoaded;

    /// <summary>OS file drop onto the window. Also forwarded to <c>Application.Events</c> and the active view.</summary>
    public static event Action<string[]> FilesDropped;

    public static IntPtr Window_handle => window.Native.Win32.Value.Hwnd;
    public static bool IsLoaded { get; private set; } = false;
    public static bool IsRunning { get; } = false;
    public static int TotalRenders { get; private set; }
    public static bool SkipCountingNextRender { get; set; } = false;

    private static readonly System.Collections.Concurrent.ConcurrentQueue<Action> _postQueue = new();

    public static void Post(Action action)
    {
        if (action == null) return;
        _postQueue.Enqueue(action);
        if (IsLoaded)
        {
            try
            {
                GlfwProvider.GLFW.Value.PostEmptyEvent();
            }
            catch { }
        }
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

    static readonly SKColor DefaultBackColor = new(255, 255, 255, 255);
    private static readonly List<(SKRect, SKColor)> PostMarkers = new();
    /// <summary>Frame-time overlay (top-left). Off by default; toggle with F12 or <c>--fps</c>.</summary>
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
        BrowserApp = application ?? throw new ArgumentNullException(nameof(application));

        try
        {
            Blossom.Utils.Fonts.EnsureFallbacks();
        }
        catch { }

        OnLoaded = () =>
        {
            ManageInputEvents();

            if (BrowserApp.ActiveView != null)
            {
                var actualRect = Browser.RenderRect;
                Browser.RenderRect = new System.Drawing.RectangleF(0, 0, BrowserApp.ActiveView.ReferenceWidth, BrowserApp.ActiveView.ReferenceHeight);
                try
                {
                    BrowserApp.ActiveView.Init();
                }
                catch (Exception ex)
                {
                    Log.Fatal("Exception during ActiveView.Init():\n" + ex.ToString());
                    throw;
                }
                finally
                {
                    Browser.RenderRect = actualRect;
                }
                BrowserApp.ActiveView.IsLoaded = true;
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
        RenderRect = new System.Drawing.Rectangle(0, 0, 1280, 800);

        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>((int)RenderRect.Width, (int)RenderRect.Height);
        options.Title = string.IsNullOrWhiteSpace(BrowserApp.Title) ? "Blossom" : BrowserApp.Title;
        options.VSync = false;
        options.TransparentFramebuffer = false;
        options.WindowBorder = WindowBorder.Resizable;
        options.IsEventDriven = true;
        options.PreferredDepthBufferBits = null;

        options.API = new GraphicsAPI(
    ContextAPI.OpenGL,
    ContextProfile.Core,
    ContextFlags.ForwardCompatible,
    new APIVersion(3, 2));

        GlfwWindowing.Use();
        // SdlWindowing.Use();

        _glfw = Glfw.GetApi();
        _glfw = GlfwProvider.GLFW.Value;

        // SetGlfwWindowHints();

        window = Window.Create(options);

        window.Load += Load;
        window.Render += Render;
        window.Closing += Closing;
        window.FileDrop += OnFileDrop;

        window.StateChanged += (state) =>
        {
            if (state != WindowState.Minimized && BrowserApp.ActiveView != null)
            {
                Browser.WasResized = true;
                BrowserApp.ActiveView.FullRenderRequired = true;
                BrowserApp.ActiveView.RenderRequired = true;
                BrowserApp.ActiveView.ForceLayoutEvaluation();
                try { GlfwProvider.GLFW.Value.PostEmptyEvent(); } catch { }
            }
        };

        window.Run();
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
                BrowserApp.ActiveView?.TriggerLoop();
                window.DoEvents();
                window.ContinueEvents();
                DrainPostQueue();

                if (BrowserApp.ActiveView?.RenderRequired == true)
                {
                    WaitForFrameSlot();
                    if (window.IsClosing)
                        break;
                    if (BrowserApp.ActiveView?.RenderRequired != true)
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
                    Thread.Sleep(1);
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
    /// Pumps events during the wait so input stays responsive.
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
            double remainMs = (target - now) * 1000.0 / freq;
            if (remainMs > 1.5)
            {
                DrainPostQueue();
                window.DoEvents();
                window.ContinueEvents();
                Thread.Sleep(1);
            }
            else
            {
                Thread.SpinWait(128);
            }
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
        BrowserApp.Events.Access = EventAccess.Keyboard;
        input = window.CreateInput();

        // Register keyboard events
        foreach (IKeyboard keyboard in input.Keyboards)
        {
            keyboard.KeyDown += (IKeyboard _, Key key, int i) =>
            {
                if (i == 0) return;
                if (key == Key.F12)
                {
                    ShowDebugOverlay = !ShowDebugOverlay;
                    if (BrowserApp.ActiveView != null)
                    {
                        BrowserApp.ActiveView.FullRenderRequired = true;
                        BrowserApp.ActiveView.RenderRequired = true;
                    }
                    return;
                }

                bool browserHandled = BrowserApp.Events.HandleKeyDown(key, i);
                if (!browserHandled && BrowserApp.ActiveView != null)
                {
                    bool elementHandled = false;
                    if (BrowserApp.ActiveView.ActiveKeyboardElement != null)
                    {
                        elementHandled = BrowserApp.ActiveView.ActiveKeyboardElement.Events.HandleKeyDown(key, i);
                    }

                    if (!elementHandled)
                    {
                        BrowserApp.ActiveView.Events.HandleKeyDown(key, i);
                    }
                }
            };

            keyboard.KeyUp += (IKeyboard _, Key key, int i) =>
            {
                BrowserApp.Events.HandleKeyUp(key, i);
                BrowserApp.ActiveView?.Events.HandleKeyUp(key, i);
                BrowserApp.ActiveView?.ActiveKeyboardElement?.Events.HandleKeyUp(key, i);
            };

            keyboard.KeyChar += (IKeyboard _, char ch) =>
            {
                BrowserApp.Events.HandleKeyChar(ch);
                BrowserApp.ActiveView?.Events.HandleKeyChar(ch);
                BrowserApp.ActiveView?.ActiveKeyboardElement?.Events.HandleKeyChar(ch);
            };
        }

        // Register mouse events
        foreach (IMouse mouse in input.Mice)
        {
            mouse.MouseMove += (IMouse m, Vector2 pos) =>
            {
                if (BrowserApp.ActiveView?.PointerCaptureElement != null
                    && !m.IsButtonPressed(Silk.NET.Input.MouseButton.Left)
                    && !m.IsButtonPressed(Silk.NET.Input.MouseButton.Right))
                {
                    BrowserApp.ActiveView.ReleasePointerCapture();
                }
                BrowserApp.Events.HandleMouseMove(pos);
                BrowserApp.ActiveView?.Events.HandleMouseMove(pos);
            };

            mouse.Scroll += (IMouse _, ScrollWheel wheel) =>
            {
                var pos = new Vector2(wheel.X, wheel.Y);
                BrowserApp.Events.HandleMouseScroll(pos);
                BrowserApp.ActiveView?.Events.HandleMouseScroll(pos);
            };

            mouse.MouseDown += (IMouse m, Silk.NET.Input.MouseButton btn) =>
            {
                int mouseButton = (int)btn;
                BrowserApp.Events.HandleMouseDown(mouseButton, m.Position);
                BrowserApp.ActiveView?.Events.HandleMouseDown(mouseButton, m.Position);
            };

            mouse.MouseUp += (IMouse m, Silk.NET.Input.MouseButton btn) =>
            {
                int mouseButton = (int)btn;
                BrowserApp.Events.HandleMouseUp(mouseButton, m.Position);
                BrowserApp.ActiveView?.Events.HandleMouseUp(mouseButton, m.Position);
            };
        }
    }

    private static void Closing()
    {
        BrowserApp.Dispose();
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
            BrowserApp?.Events.HandleFilesDropped(paths);
            BrowserApp?.ActiveView?.Events.HandleFilesDropped(paths);
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
        IsLoaded = true;

        window.Center();
        Renderer.SetCanvas(window);
        OnLoaded.Invoke();

        LoadLogo();

        Browser.WasResized = true; // Ensure full render on startup
        StartWindow();
    }

    private static readonly Stopwatch frameTimer = new();
    private static int frameCounter = 0;
    private static readonly double[] frameTimes = new double[20];

    static Browser()
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

    private static readonly SKPaint HudHeaderTitlePaint = new()
    {
        TextSize = 13.5f,
        Color = new SKColor(255, 255, 255, 255), // Pure white #FFFFFF
        Style = SKPaintStyle.Fill,
        Typeface = Blossom.Utils.Fonts.GetTypeface("Noto Sans, Inter, Roboto, DejaVu Sans, Liberation Sans, sans-serif", weight: 600),
        IsAntialias = true,
        SubpixelText = true,
        LcdRenderText = true,
        HintingLevel = SKPaintHinting.Normal,
    };

    private static readonly SKPaint HudSeparatorPaint = new()
    {
        StrokeWidth = 1f,
        Color = new SKColor(55, 55, 55, 255), // Pure neutral gray #373737
        Style = SKPaintStyle.Stroke,
        IsAntialias = true,
    };

    private static readonly SKPaint HudMetricLabelPaint = new()
    {
        TextSize = 10f,
        Color = new SKColor(150, 150, 150, 255), // Pure neutral medium gray #969696
        Style = SKPaintStyle.Fill,
        Typeface = Blossom.Utils.Fonts.GetTypeface("Noto Sans, Inter, Roboto, DejaVu Sans, Liberation Sans, sans-serif", weight: 600),
        IsAntialias = true,
        SubpixelText = true,
        LcdRenderText = true,
        HintingLevel = SKPaintHinting.Normal,
    };

    private static readonly SKPaint HudFpsValuePaint = new()
    {
        TextSize = 26f,
        Color = new SKColor(16, 185, 129, 255), // Emerald-500
        Style = SKPaintStyle.Fill,
        Typeface = Blossom.Utils.Fonts.GetTypeface("Noto Sans, Inter, Roboto, DejaVu Sans, Liberation Sans, sans-serif", weight: 700),
        IsAntialias = true,
        SubpixelText = true,
        LcdRenderText = true,
        HintingLevel = SKPaintHinting.Normal,
    };

    private static readonly SKPaint HudFrameTimeValuePaint = new()
    {
        TextSize = 26f,
        Color = new SKColor(255, 255, 255, 255), // Crisp pure white #FFFFFF
        Style = SKPaintStyle.Fill,
        Typeface = Blossom.Utils.Fonts.GetTypeface("Noto Sans, Inter, Roboto, DejaVu Sans, Liberation Sans, sans-serif", weight: 700),
        IsAntialias = true,
        SubpixelText = true,
        LcdRenderText = true,
        HintingLevel = SKPaintHinting.Normal,
    };

    private static readonly SKPaint HudMetricUnitPaint = new()
    {
        TextSize = 12f,
        Color = new SKColor(130, 130, 130, 255), // Pure neutral gray #828282
        Style = SKPaintStyle.Fill,
        Typeface = Blossom.Utils.Fonts.GetTypeface("Noto Sans, Inter, Roboto, DejaVu Sans, Liberation Sans, sans-serif", weight: 600),
        IsAntialias = true,
        SubpixelText = true,
        LcdRenderText = true,
        HintingLevel = SKPaintHinting.Normal,
    };

    private static readonly SKPaint HudDiagLabelPaint = new()
    {
        TextSize = 10.5f,
        Color = new SKColor(140, 140, 140, 255), // Pure neutral gray #8C8C8C
        Style = SKPaintStyle.Fill,
        Typeface = Blossom.Utils.Fonts.GetTypeface("Noto Sans, Inter, Roboto, DejaVu Sans, Liberation Sans, sans-serif", weight: 600),
        IsAntialias = true,
        SubpixelText = true,
        LcdRenderText = true,
        HintingLevel = SKPaintHinting.Normal,
    };

    private static readonly SKPaint HudDiagValuePaint = new()
    {
        TextSize = 11.5f,
        Color = new SKColor(235, 235, 235, 255), // Crisp pure light gray #EBEBEB
        Style = SKPaintStyle.Fill,
        Typeface = Blossom.Utils.Fonts.GetTypeface("Noto Sans, Inter, Roboto, DejaVu Sans, Liberation Sans, sans-serif", weight: 500),
        IsAntialias = true,
        SubpixelText = true,
        LcdRenderText = true,
        HintingLevel = SKPaintHinting.Normal,
    };

    private static void DrawDebugOverlay(double avgDrawMs, double theoreticalFps)
    {
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
        Renderer.Canvas.DrawText("Stats", panelX + 26, headerY + 22, HudHeaderTitlePaint);

        // Separator line under header flush with panel borders
        float div1Y = headerY + 32.5f;
        Renderer.Canvas.DrawLine(panelX + 1f, div1Y, panelX + panelW - 1f, div1Y, HudSeparatorPaint);

        // 3. Primary metrics — stacked vertically
        // Block 1: FPS
        float fpsBlockY = headerY + 32f;
        Renderer.Canvas.DrawText("FPS", panelX + 14, fpsBlockY + 18, HudMetricLabelPaint);
        HudFpsValuePaint.Color = perfColor;
        string fpsStr = theoreticalFps >= 1000.0 ? $"{theoreticalFps:0}" : (theoreticalFps > 0 ? $"{theoreticalFps:0.0}" : "--.-");
        Renderer.Canvas.DrawText(fpsStr, panelX + 14, fpsBlockY + 45, HudFpsValuePaint);

        // Separator between metrics
        float div2Y = fpsBlockY + 54.5f;
        Renderer.Canvas.DrawLine(panelX + 1f, div2Y, panelX + panelW - 1f, div2Y, HudSeparatorPaint);

        // Block 2: Frame Draw Time
        float ftBlockY = fpsBlockY + 54f;
        Renderer.Canvas.DrawText("FRAME DRAW", panelX + 14, ftBlockY + 18, HudMetricLabelPaint);
        string ftStr = $"{avgDrawMs:0.00}";
        Renderer.Canvas.DrawText(ftStr, panelX + 14, ftBlockY + 45, HudFrameTimeValuePaint);
        float ftValWidth = HudFrameTimeValuePaint.MeasureText(ftStr);
        Renderer.Canvas.DrawText(" ms", panelX + 14 + ftValWidth, ftBlockY + 45, HudMetricUnitPaint);

        // Separator between metrics and diagnostics
        float div3Y = ftBlockY + 54.5f;
        Renderer.Canvas.DrawLine(panelX + 1f, div3Y, panelX + panelW - 1f, div3Y, HudSeparatorPaint);

        // 4. Secondary Diagnostics — each metric on its own line with generous vertical gap
        float diagBlockY = ftBlockY + 54f;
        float scale = Browser.RenderRect.Width > 0 ? (float)Renderer.FramebufferWidth / Browser.RenderRect.Width : 1f;
        int elementCount = BrowserApp.ActiveView?.Elements.Count ?? 0;

        float rowY = diagBlockY + 20f;
        const float rowGap = 21f;
        const float valColX = panelX + 68f;

        // Row 1: Resolution & Scale
        Renderer.Canvas.DrawText("RES", panelX + 14, rowY, HudDiagLabelPaint);
        Renderer.Canvas.DrawText($"{Renderer.FramebufferWidth}x{Renderer.FramebufferHeight} ({scale:0.0}x)", valColX, rowY, HudDiagValuePaint);
        rowY += rowGap;

        // Row 2: Node Count
        Renderer.Canvas.DrawText("NODES", panelX + 14, rowY, HudDiagLabelPaint);
        Renderer.Canvas.DrawText($"{elementCount}", valColX, rowY, HudDiagValuePaint);
        rowY += rowGap;

        // Row 3: Active View
        string viewName = BrowserApp.ActiveView?.Name ?? "None";
        if (viewName.Length > 15) viewName = viewName[..14] + "…";
        Renderer.Canvas.DrawText("VIEW", panelX + 14, rowY, HudDiagLabelPaint);
        Renderer.Canvas.DrawText(viewName, valColX, rowY, HudDiagValuePaint);
        rowY += rowGap;

        // Row 4: Memory Usage
        double memMb = (double)GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        Renderer.Canvas.DrawText("MEM", panelX + 14, rowY, HudDiagLabelPaint);
        Renderer.Canvas.DrawText($"{memMb:0.0} MB", valColX, rowY, HudDiagValuePaint);
        rowY += rowGap;

        // Row 5: Frame Counter
        Renderer.Canvas.DrawText("FRAME", panelX + 14, rowY, HudDiagLabelPaint);
        Renderer.Canvas.DrawText($"#{TotalRenders}", valColX, rowY, HudDiagValuePaint);

        // Clean-up
        PostMarkers.Clear();
    }

    private static void Render(double time)
    {
        DrainPostQueue();
        Blossom.Core.Visual.SKSLShaderTimeTracker.DeltaTime = (float)time;
        Blossom.Core.Visual.SKSLShaderTimeTracker.ElapsedSeconds += (float)time;

        Renderer.ResetContext();
        // Renderer.Canvas.Clear(BrowserApp.ActiveView?.BackColor ?? DefaultBackColor);
        
        if (Blossom.Core.BenchmarkManager.IsBenchmarkMode)
        {
            Blossom.Core.BenchmarkManager.StartFrame(BrowserApp.ActiveView?.Name ?? "Unknown");
        }

        frameTimer.Restart();
        BrowserApp.Render();
        frameTimer.Stop();

        if (Blossom.Core.BenchmarkManager.IsBenchmarkMode)
        {
            Blossom.Core.BenchmarkManager.EndFrame();
        }

        string title = !string.IsNullOrWhiteSpace(BrowserApp.Title)
            ? BrowserApp.Title
            : (BrowserApp.ActiveView?.Name ?? "Blossom");
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