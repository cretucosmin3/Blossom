using System.Threading;
using System.Diagnostics;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Delegates.Common;
using Blossom.Testing;
using Silk.NET.Windowing.Glfw;
using System;
using Silk.NET.Core;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using System.Runtime.InteropServices;

namespace Blossom;

public static class Browser
{
    internal static IWindow window;
    public static IntPtr window_handle { get => window.Native.Win32.Value.Hwnd; }
    private static IInputContext input;

    internal static TestingApplication BrowserApp = new TestingApplication();
    internal static System.Drawing.RectangleF RenderRect = new(0, 0, 0, 0);
    internal static Action OnRenderRequired;

    public static event ForVoid OnLoaded;

    public static bool IsLoaded { get; private set; } = false;
    public static bool IsRunning { get; private set; } = false;
    public static int TotalRenders { get; private set; }
    public static bool SkipCountingNextRender { get; set; } = false;

    internal static void Initialize()
    {
        OnLoaded = () =>
        {
            ManageInputEvents();

            if (BrowserApp.ActiveView != null)
                BrowserApp.ActiveView.Main();
            else Console.WriteLine("-- No active view --");

        };

        SetWindow();
    }

    private static void SetWindow()
    {
        RenderRect = new System.Drawing.RectangleF(0, 0, 1100, 700);

        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>((int)RenderRect.Width, (int)RenderRect.Height);
        options.Title = "Blossom";
        options.VSync = false;
        options.TransparentFramebuffer = true;
        options.WindowBorder = WindowBorder.Resizable;
        options.IsEventDriven = true;

        GlfwWindowing.Use();

        window = Window.Create(options);

        window.Load += Load;
        window.Render += Render;
        window.Closing += Closing;

        window.Run();
    }

    internal static void StartWindow()
    {
        while (!window.IsClosing)
        {
            Thread.Sleep(1);
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
            };

            keyboard.KeyUp += (IKeyboard _, Key key, int i) =>
            {
                BrowserApp.Events.HandleKeyUp(key, i);
                BrowserApp.ActiveView?.Events.HandleKeyUp(key, i);
            };

            keyboard.KeyChar += (IKeyboard _, char ch) =>
            {
                BrowserApp.Events.HandleKeyChar(ch);
                BrowserApp.ActiveView?.Events.HandleKeyChar(ch);
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

            mouse.MouseDown += (IMouse m, MouseButton btn) =>
            {
                int mouseButton = (int)btn;
                BrowserApp.Events.HandleMouseDown(mouseButton, m.Position);
                BrowserApp.ActiveView?.Events.HandleMouseDown(mouseButton, m.Position);
            };

            mouse.MouseUp += (IMouse m, MouseButton btn) =>
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
                block = block.Slice(memory.Length);
            }

            var icon = new RawImage(image.Width, image.Height, array);
            window.SetWindowIcon(ref icon);
            Console.WriteLine("Logo loaded");
        };
    }

    private static void Load()
    {
        var winHandle = window.Native.Win32.Value.Hwnd;

        IsLoaded = true;
        window.Center();
        Renderer.SetCanvas(window);
        OnLoaded.Invoke();
        LoadLogo();

        User32.makeWindowBorderless(winHandle);
        StartWindow();
    }

    private static void Render(double time)
    {
        Renderer.ResetContext();
        Renderer.Canvas.Clear(new(255, 255, 255, 255));
        BrowserApp.Render();
        Renderer.Canvas.Flush();
    }
}