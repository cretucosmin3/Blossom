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
using Blossom.Testing;
using System;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using System.Runtime.InteropServices;
using SkiaSharp;
using System.Collections.Generic;
using System.Drawing;
using Silk.NET.Windowing.Sdl;
using Silk.NET.GLFW;

namespace Blossom;

public static class Browser
{
    private static IInputContext input;

    internal static IWindow window;
    internal static TestingApplication BrowserApp = new();
    internal static RectangleF RenderRect = new(0, 0, 0, 0);
    internal static bool WasResized;

    internal static Action OnRenderRequired;

    public static event ForVoid OnLoaded;

    public static IntPtr Window_handle => window.Native.Win32.Value.Hwnd;
    public static bool IsLoaded { get; private set; } = false;
    public static bool IsRunning { get; } = false;
    public static int TotalRenders { get; private set; }
    public static bool SkipCountingNextRender { get; set; } = false;

    static readonly SKColor DefaultBackColor = new(255, 255, 255, 255);
    private static readonly List<(SKRect, SKColor)> PostMarkers = new();
    private static bool DrawDebugMarkers = true;

    private static Glfw _glfw = null!;

    internal static void AddVisualMarker(SKRect marker, SKColor color)
    {
        if (!DrawDebugMarkers) return;

        marker.Inflate(3, 3);
        PostMarkers.Add((marker, color));
    }

    internal static void Initialize()
    {
        OnLoaded = () =>
        {
            ManageInputEvents();

            if (BrowserApp.ActiveView != null)
            {
                BrowserApp.ActiveView.Init();
                BrowserApp.ActiveView.IsLoaded = true;
            }
            else
            {
                Console.WriteLine("-- No active view --");
            }
        };

        SetWindow();
    }

    private static void SetWindow()
    {
        RenderRect = new System.Drawing.Rectangle(0, 0, 1100, 700);

        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>((int)RenderRect.Width, (int)RenderRect.Height);
        options.Title = "Blossom";
        options.VSync = false;
        options.TransparentFramebuffer = false;
        options.WindowBorder = WindowBorder.Resizable;
        options.IsEventDriven = true;
        options.PreferredDepthBufferBits = null;

        options.API = new GraphicsAPI(
           ContextAPI.OpenGL,
           ContextProfile.Core,
           ContextFlags.ForwardCompatible,
           new APIVersion(3, 2)
        );

        GlfwWindowing.Use();
        // SdlWindowing.Use();

        _glfw = Glfw.GetApi();
        _glfw = GlfwProvider.GLFW.Value;

        window = Window.Create(options);

        window.Load += Load;
        window.Render += Render;
        window.Closing += Closing;

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
        while (!window.IsClosing)
        {
            BrowserApp.ActiveView?.TriggerLoop();
            window.DoEvents();
            window.ContinueEvents();

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

        window.Dispose();
    }

    internal static void ChangeCursor(StandardCursor cursor)
    {
        foreach (IMouse mouse in input.Mice)
        {
            mouse.Cursor.StandardCursor = cursor;
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
                var BrowserHandled = BrowserApp.Events.HandleKeyDown(key, i);
                var ViewHandled = BrowserApp.ActiveView?.Events.HandleKeyDown(key, i);
                BrowserApp.ActiveView?.FocusedElement?.Events?.HandleKeyDown(key, i);
            };

            keyboard.KeyUp += (IKeyboard _, Key key, int i) =>
            {
                BrowserApp.Events.HandleKeyUp(key, i);
                BrowserApp.ActiveView?.Events.HandleKeyUp(key, i);
                BrowserApp.ActiveView?.FocusedElement?.Events.HandleKeyUp(key, i);
            };

            keyboard.KeyChar += (IKeyboard _, char ch) =>
            {
                BrowserApp.Events.HandleKeyChar(ch);
                BrowserApp.ActiveView?.Events.HandleKeyChar(ch);
                BrowserApp.ActiveView?.FocusedElement?.Events.HandleKeyChar(ch);
            };
        }

        // Register mouse events
        foreach (IMouse mouse in input.Mice)
        {
            mouse.MouseMove += (IMouse _, Vector2 pos) =>
            {
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

    private static void LoadLogo()
    {
        unsafe
        {
            using var image = Image.Load<Rgba32>("assets/icon.png");
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
            Console.WriteLine("Logo loaded");
        }
    }

    private static void Load()
    {
        IsLoaded = true;

        window.Center();
        Renderer.SetCanvas(window);
        OnLoaded.Invoke();

        LoadLogo();

        Console.Clear();
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
        Typeface = SKTypeface.FromFamilyName("Arimo", 100, 2, SKFontStyleSlant.Upright),
    };

    private static void Render(double time)
    {
        Renderer.ResetContext();
        Renderer.Canvas.Clear(BrowserApp.ActiveView?.BackColor ?? DefaultBackColor);
        
        frameTimer.Restart();
        BrowserApp.Render();
        frameTimer.Stop();

        if (window.Title != BrowserApp.ActiveView.Name)
        {
            window.Title = BrowserApp.ActiveView.Name;
        }

        frameTimes[frameCounter] = frameTimer.ElapsedMilliseconds;

        frameCounter++;
        if (frameCounter == frameTimes.Length)
            frameCounter = 0;

        double AverageFrame = 0;
        foreach (double t in frameTimes)
            AverageFrame += t;

        if (DrawDebugMarkers)
        {
            // Draw informational markers
            foreach (var (rect, color) in PostMarkers)
            {
                PostMarkerPaint.Color = color;
                PostMarkerPaint.PathEffect?.Dispose();
                PostMarkerPaint.PathEffect = SKPathEffect.CreateDash(new float[] { 3, 10 }, Random.Shared.Next(0, 1000));

                Renderer.Canvas.DrawRect(rect, PostMarkerPaint);
            }

            Renderer.Canvas.DrawText($"{AverageFrame / frameTimes.Length:0.00} ms", 5, 20, InfoTextPaint);

            // Clean-up
            PostMarkers.Clear();
        }

        WasResized = false;

        Renderer.Canvas.Flush();
    }
}