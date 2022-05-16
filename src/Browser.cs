using System.ComponentModel;
using System;
using System.Drawing;
using System.Numerics;
using System.Diagnostics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Kara.Core;
using Kara.Core.Input;
using Kara.Core.Delegates.Common;
using Kara.Testing;
using Silk.NET.Windowing.Glfw;
using SkiaSharp;

namespace Kara
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
        public static float FontSize = 100;

        public static void Initialize()
        {
            OnLoaded = () =>
            {
                ManageInputEvents();
                BrowserApp.ActiveView.Main();
            };

            SetWindow();

            Renderer.SetCanvas(window);

            StartWindow();
        }

        private static void SetWindow()
        {
            RenderRect = new RectangleF(0, 0, 800, 800);

            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(800, 800);
            options.Title = "UI";
            options.PreferredDepthBufferBits = 24;
            options.PreferredStencilBufferBits = 8;
            options.VSync = false;
            options.PreferredBitDepth = new Vector4D<int>(4, 4, 4, 4);
            options.IsEventDriven = true;

            GlfwWindowing.Use();

            window = Window.Create(options);

            window.Load += Load;
            window.Render += Render;
            window.Closing += Closing;

            window.Initialize();
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
                    var ViewHandled = BrowserApp.ActiveView.Events.HandleKeyDown(key, i);
                };

                keyboard.KeyUp += (IKeyboard _, Key key, int i) =>
                {
                    BrowserApp.Events.HandleKeyUp(key, i);
                    BrowserApp.ActiveView.Events.HandleKeyUp(key, i);
                };

                keyboard.KeyChar += (IKeyboard _, char ch) =>
                {
                    BrowserApp.Events.HandleKeyChar(ch);
                    BrowserApp.ActiveView.Events.HandleKeyChar(ch);
                };
            }

            // Register mouse events
            foreach (IMouse mouse in input.Mice)
            {
                mouse.MouseMove += (IMouse _, Vector2 pos) =>
                {
                    BrowserApp.Events.Handle_Mouse_Move((int)pos.X, (int)pos.Y);
                    BrowserApp.ActiveView.Events.Handle_Mouse_Move((int)pos.X, (int)pos.Y);
                };

                mouse.Scroll += (IMouse _, ScrollWheel wheel) =>
                {
                    FontSize += wheel.Y;
                };

                //            mouse.Click += (IMouse m, MouseButton btn, Vector2 pos) =>
                //{
                //	BrowserApp.Events.Handle_Mouse_Click()
                //};
                //            mouse.DoubleClick += Handle_Mouse_Double_Click;

                //            mouse.MouseDown += Handle_Mouse_Down;
                //            mouse.MouseUp += Handle_Mouse_Up;

                //            mouse.Scroll += Handle_Mouse_Scroll;
            }
        }

        private static void Closing()
        {
            BrowserApp.Dispose();
        }

        private static void Load()
        {
            OnLoaded.Invoke();
            timer.Start();
            d.Start();
            IsLoaded = true;
        }

        private static float frames = 0;
        private static double fps_avg = 0;
        private static float fps = 0;
        private static Stopwatch timer = new Stopwatch();
        private static Stopwatch d = new Stopwatch();
        private static SKPaint fpsPaint = new SKPaint()
        {
            Color = SKColors.White,
            TextSize = 20,
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
            // float pxRatio = fbSize.X / winSize.X;

            // float x = 250;
            // float y = 200;

            // SKPaint testPaint = new SKPaint()
            // {
            //     Color = SKColors.Black,
            //     TextSize = FontSize,
            //     IsAntialias = true,
            //     IsStroke = false,
            //     TextAlign = SKTextAlign.Center,
            // };

            // string text = "Hello world!";

            // var textMetrics = Fonts.Measure(testPaint, text);

            // Renderer.Canvas.DrawText(text, x, y, testPaint);

            // testPaint.Color = SKColors.Red;
            // testPaint.StrokeWidth = 5;

            // Renderer.Canvas.DrawPoint(new SKPoint(x, y), testPaint);

            // testPaint.Color = new SKColor(0, 255, 0, 25);
            // Renderer.Canvas.DrawRect(x, y - (textMetrics.Y / 2f), textMetrics.X, textMetrics.Y, testPaint);

            BrowserApp.Render();

            // SKPaint crossPaint = new SKPaint()
            // {
            //     Color = SKColors.Black,
            //     IsAntialias = true,
            //     IsStroke = true,
            //     StrokeWidth = 2,
            //     TextSize = 35,
            // };

            // var outer = new SKRoundRect(new (100, 100, 300, 550), 3, 3);
            // var inner = new SKRoundRect(new (150, 150, 500, 500), 30, 30);

            // Renderer.Canvas.DrawRoundRect(outer, crossPaint);
            // Renderer.Canvas.DrawRoundRect(inner, crossPaint);

            // using(new SKAutoCanvasRestore(Renderer.Canvas))
            // {
            //     if(d.ElapsedMilliseconds <= 800){
            //         Renderer.Canvas.ClipRoundRect(outer, SKClipOperation.Intersect);
            //     }
            //     else if (d.ElapsedMilliseconds > 1600) {
            //         d.Restart();
            //     }

            //     Renderer.Canvas.DrawRoundRect(inner, crossPaint);

            //     crossPaint.IsStroke = false;
            //     Renderer.Canvas.DrawText("Hello World", x - 25, y, crossPaint);
            // }

            if (FpsVisible)
            {
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