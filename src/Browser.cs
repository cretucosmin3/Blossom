using System;
using System.Drawing;
using System.Numerics;
using System.Diagnostics;
using System.Threading;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkyNvg;
using SilkyNvg.Rendering.OpenGL;
using SilkyNvg.Text;
using SilkyNvg.Graphics;
using Kara.Utils;
using Kara.Core;
using Kara.Core.Input;
using Kara.Core.Delegates.Common;
using Kara.Testing;

namespace Kara
{
	public static class Browser
	{
		private static GL gl;
		internal static Nvg RenderPipeline;
		internal static IWindow window;

		internal static Application BrowserApp = new TestingApplication();
		internal static RectangleF RenderRect = new(0, 0, 0, 0);

		public static event ForVoid OnLoaded;
		public static bool IsLoaded { get; private set; } = false;

		private static bool FpsVisible = false;
		public static void ShowFps() => FpsVisible = true;
		public static void HideFps() => FpsVisible = false;

		public static void Initialize()
		{
			RenderRect = new RectangleF(0, 0, 1000, 600);

			WindowOptions windowOptions = WindowOptions.Default;
			windowOptions.FramesPerSecond = -1;
			windowOptions.ShouldSwapAutomatically = true;
			windowOptions.Size = new Vector2D<int>(1000, 600);
			windowOptions.Title = "UI";
			windowOptions.VSync = false;
			windowOptions.PreferredDepthBufferBits = 24;
			windowOptions.PreferredStencilBufferBits = 8;

			window = Window.Create(windowOptions);
			window.Load += Load;
			window.Render += Render;
			window.Closing += Closing;
			window.IsEventDriven = true;

			window.Initialize();

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
			RenderPipeline.Dispose();
			gl.Dispose();
		}

		private static void Load()
		{
			gl = window.CreateOpenGL();

			OpenGLRenderer nvgRenderer = new(CreateFlags.Antialias | CreateFlags.StencilStrokes | CreateFlags.Debug, gl);
			RenderPipeline = Nvg.Create(nvgRenderer);
			Renderer.Initialize(RenderPipeline);

			ManageInputEvents();
			IsLoaded = true;
			OnLoaded?.Invoke();
			timer.Start();
		}

		private static float frames = 0;
		private static double fps_avg = 0;
		private static float fps = 0;
		private static Stopwatch timer = new Stopwatch();
		private static void Render(double time)
		{
			Vector2D<float> winSize = window.Size.As<float>();
			Vector2D<float> fbSize = window.FramebufferSize.As<float>();

			float pxRatio = fbSize.X / winSize.X;

			gl.Viewport(0, 0, (uint)winSize.X, (uint)winSize.Y);
			gl.ClearColor(255, 255, 255, 255);
			gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

			RenderPipeline.BeginFrame(winSize.X, winSize.Y, pxRatio);
			BrowserApp.Render();

			if (FpsVisible)
			{
				RenderPipeline.FillColour(Conversion.fromColor(Color.Red));
				RenderPipeline.Text(15, window.Size.Y - 15, $"FPS {fps:0}");


				frames++;
				fps_avg += time;

				if (frames == 100)
				{
					fps = 1f / (float)(fps_avg / 100d);
					fps_avg = 0;
					frames = 0;
				}
			}

			RenderPipeline.EndFrame();
		}
	}
}