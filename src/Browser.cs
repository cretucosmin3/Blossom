using System.Drawing;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Rux.Core;
using Rux.Core.Input;
using Rux.Core.Delegates.Common;
using Rux.Testing;
using Silk.NET.Windowing.Glfw;
using SkiaSharp;
using System;
using Silk.NET.Core;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using System.Runtime.InteropServices;

namespace Rux
{
    public static class Browser
    {
        internal static IWindow window;

        internal static TestingApplication BrowserApp = new TestingApplication();
        internal static System.Drawing.RectangleF RenderRect = new(0, 0, 0, 0);

        public static event ForVoid OnLoaded;
        public static bool IsLoaded { get; private set; } = false;
        public static bool IsRunning { get; private set; } = false;

        private static bool FpsVisible = false;
        public static void ShowFps() => FpsVisible = true;
        public static void HideFps() => FpsVisible = false;

        public static void Initialize()
        {
            OnLoaded = () =>
            {
                ManageInputEvents();
                BrowserApp.ActiveView.Main();
            };

            SetWindow();
        }

        private static void SetWindow()
        {
            RenderRect = new System.Drawing.RectangleF(0, 0, 1100, 700);

            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>((int)RenderRect.Width, (int)RenderRect.Height);
            options.Title = "Rux";
            options.VSync = false;
            options.IsEventDriven = true;

            GlfwWindowing.Use();
            // SdlWindowing.Use();

            window = Window.Create(options);

            window.Load += Load;
            window.Render += Render;
            window.Closing += Closing;

            window.Run();
        }

        public static void StartWindow()
        {
            while (!window.IsClosing)
            {
                BrowserApp.ActiveView.TriggerLoop();
                window.DoRender();
                window.DoEvents();
                window.ContinueEvents();
            }

            window.Dispose();
        }

        private static void ManageInputEvents()
        {
            BrowserApp.Events.Access = EventAccess.Keyboard;
            IInputContext input = window.CreateInput();

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
                using var image = Image.Load<Rgba32>("rux-logo.png");
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
                Console.WriteLine("Finished loading");
            };

            // var logo = new RawImage(128, 128,
            //     new Memory<byte>(File.ReadAllBytes("example.ico"))
            // );

            // window.SetWindowIcon(ref logo);
        }

        private static void Load()
        {
            IsLoaded = true;
            window.Center();
            Renderer.SetCanvas(window);
            OnLoaded.Invoke();

            LoadLogo();
            StartWindow();
        }

        private static float frames = 0;
        private static double fps_avg = 0;
        private static float fps = 0;
        private static SKPaint fpsPaint = new SKPaint()
        {
            Color = SKColors.White,
            TextSize = 20,
            StrokeWidth = 4,
            IsAntialias = false,
            IsStroke = false,
            Typeface = SKTypeface.FromFamilyName("Bitstream Charter", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
        };

        private static void Render(double time)
        {
            Vector2D<float> winSize = window.Size.As<float>();
            Vector2D<float> fbSize = window.FramebufferSize.As<float>();

            // Renderer.ResetContext();
            Renderer.Canvas.Clear(new(255, 255, 255, 45));
            float pxRatio = fbSize.X / winSize.X;

            BrowserApp.Render();

            if (FpsVisible)
            {
                fpsPaint.IsStroke = true;
                fpsPaint.Color = SKColors.Black;
                fpsPaint.StrokeWidth = 4;
                Renderer.Canvas.DrawText($"FPS {fps:0}", 10, 20, fpsPaint);

                fpsPaint.IsStroke = false;
                fpsPaint.Color = SKColors.IndianRed;
                Renderer.Canvas.DrawText($"FPS {fps:0}", 10, 20, fpsPaint);

                frames++;
                fps_avg += time;

                if (frames == 100)
                {
                    fps = 1f / (float)(fps_avg / 100d);
                    fps_avg = 0;
                    frames = 0;
                }
            }

            Renderer.Canvas.Flush();
        }
    }
}