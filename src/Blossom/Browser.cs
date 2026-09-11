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

                Thread.Sleep(1);
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

    internal static void ChangeCursor(StandardCursor cursor)
    {
        SetCursor(cursor);
    }

    /// <summary>Set a platform standard mouse cursor.</summary>
    public static void SetCursor(StandardCursor cursor)
    {
        if (input == null || input.Mice == null) return;
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
                    bool viewHandled = BrowserApp.ActiveView.Events.HandleKeyDown(key, i);
                    if (!viewHandled)
                    {
                        BrowserApp.ActiveView.ActiveKeyboardElement?.Events?.HandleKeyDown(key, i);
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
    private static readonly double[] frameTimes = new double[10];

    private static readonly SKPaint PostMarkerPaint = new()
    {
        StrokeWidth = 3f,
        Color = SKColors.Red,
        Style = SKPaintStyle.Stroke,
    };

    private static readonly SKPaint InfoTextPaint = new()
    {
        TextSize = 18,
        FakeBoldText = true,
        Color = SKColors.IndianRed,
        Style = SKPaintStyle.Fill,
        Typeface = Blossom.Utils.Fonts.GetTypeface("Roboto", 500),
    };

    private static readonly SKPaint InfoBackgroundPaint = new()
    {
        Color = new SKColor(15, 15, 20, 255), // Solid dark cyberpunk background
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint InfoBorderPaint = new()
    {
        StrokeWidth = 1f,
        Color = new SKColor(205, 92, 92, 180), // IndianRed matching border
        Style = SKPaintStyle.Stroke,
    };

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

        frameTimes[frameCounter] = frameTimer.ElapsedMilliseconds;

        frameCounter++;
        if (frameCounter == frameTimes.Length)
            frameCounter = 0;

        double AverageFrame = 0;
        foreach (double t in frameTimes)
            AverageFrame += t;

        if (ShowDebugOverlay)
        {
            // Draw informational markers
            foreach (var (rect, color) in PostMarkers)
            {
                PostMarkerPaint.Color = color;
                PostMarkerPaint.PathEffect?.Dispose();
                PostMarkerPaint.PathEffect = SKPathEffect.CreateDash(new float[] { 3, 10 }, Random.Shared.Next(0, 1000));

                Renderer.Canvas.DrawRect(rect, PostMarkerPaint);
            }

            double avgMs = AverageFrame / frameTimes.Length;
            string msText = $"FT {avgMs:0.00} ms";
            
            float boxWidth = 130f; // Fixed width to prevent size jittering and smears
            float boxHeight = 36f;
            SKRect bgRect = new SKRect(10, 10, 10 + boxWidth, 10 + boxHeight);
            
            Renderer.Canvas.DrawRoundRect(bgRect, 6, 6, InfoBackgroundPaint);
            Renderer.Canvas.DrawRoundRect(bgRect, 6, 6, InfoBorderPaint);
            
            float textWidth = InfoTextPaint.MeasureText(msText);
            float textX = 10f + (boxWidth - textWidth) / 2f;
            float textY = 10f + 25f; // Baseline aligned inside the 36px box
            Renderer.Canvas.DrawText(msText, textX, textY, InfoTextPaint);
            
            // Clean-up
            PostMarkers.Clear();
        }
        else if (PostMarkers.Count > 0)
        {
            PostMarkers.Clear();
        }

        WasResized = false;
        
        // Final blit to screen
        Renderer.FlushToScreen();
    }
}