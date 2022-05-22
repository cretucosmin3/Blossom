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
using Rux.Utils;

namespace Rux
{
    public static class Browser
    {
        internal static IWindow window;

        internal static TestingApplication BrowserApp = new TestingApplication();
        internal static RectangleF RenderRect = new(0, 0, 0, 0);

        public static event ForVoid OnLoaded;
        public static bool IsLoaded { get; private set; } = false;

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
            RenderRect = new RectangleF(0, 0, 1100, 700);

            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>((int)RenderRect.Width, (int)RenderRect.Height);
            options.Title = "Rux";
            options.VSync = false;
            options.IsEventDriven = true;

            GlfwWindowing.Use();

            window = Window.Create(options);

            window.Load += Load;
            window.Render += Render;
            window.Closing += Closing;

            window.Run();
        }

        public static void StartWindow()
        {
            int x = 0;
            while (!window.IsClosing)
            {
                if (x > 5)
                {
                    BrowserApp.ActiveView.TriggerLoop();
                    x = 0;
                }
                x++;
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

                // mouse.Click += (IMouse m, MouseButton btn, Vector2 pos) =>
                // {
                //     int mouseButton = (int)btn;
                //     BrowserApp.Events.HandleMouseClick(mouseButton, pos);
                //     BrowserApp.ActiveView?.Events.HandleMouseClick(mouseButton, pos);
                // };

                mouse.DoubleClick += (IMouse m, MouseButton btn, Vector2 pos) =>
                {
                    int mouseButton = (int)btn;
                    BrowserApp.Events.HandleMouseDoubleClick(mouseButton, pos);
                    BrowserApp.ActiveView?.Events.HandleMouseDoubleClick(mouseButton, pos);
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

        private static void Load()
        {
            window.Center();
            Renderer.SetCanvas(window);
            OnLoaded.Invoke();
            IsLoaded = true;

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
            Typeface = SKTypeface.FromFamilyName("Bitstream Charter", SKTypefaceStyle.Bold)
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