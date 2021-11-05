using System.Threading;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkyNvg;
using SilkyNvg.Rendering.OpenGL;
using Kara.Core;
using Kara.Core.Input;

namespace Kara
{
	public static class Browser
	{
		private static GL gl;
		internal static Nvg Renderer;
		internal static IWindow window;

		/// <summary>
		/// Application of the browser
		/// </summary>
		internal static Application KaraApp = new Application();

		public static void Initialize()
		{
			WindowOptions windowOptions = WindowOptions.Default;
			windowOptions.FramesPerSecond = -1;
			windowOptions.ShouldSwapAutomatically = true;
			windowOptions.Size = new Vector2D<int>(1000, 600);
			windowOptions.Title = "Kara";
			windowOptions.VSync = false;
			windowOptions.PreferredDepthBufferBits = 12;
			windowOptions.PreferredStencilBufferBits = 8;

			window = Window.Create(windowOptions);
			window.Load += Load;
			window.Render += Render;
			window.Closing += Closing;
			window.IsEventDriven = true;

			window.Initialize();

			while (!window.IsClosing)
			{
				window.DoRender();
				window.DoEvents();
				window.ContinueEvents();
				Thread.Sleep(1);
			}

			window.Dispose();
		}

		private static void ManageInputEvents()
		{
			KaraApp.Events.Access = EventAccess.Keyboard;
			IInputContext input = window.CreateInput();

			// Register keyboard events
			foreach (IKeyboard keyboard in input.Keyboards)
			{
				keyboard.KeyDown += (IKeyboard _, Key key, int i) =>
				{
					var BrowserHandled = KaraApp.Events.HandleKeyDown(key, i);
				};

				keyboard.KeyUp += (IKeyboard _, Key key, int i) =>
				{
					KaraApp.Events.HandleKeyUp(key, i);
				};

				keyboard.KeyChar += (IKeyboard _, char ch) =>
				{
					KaraApp.Events.HandleKeyChar(ch);
				};
			}

			// Register mouse events
			// foreach (IMouse mouse in input.Mice)
			// {
			// 	mouse.MouseMove += Handle_Mouse_Move;

			// 	mouse.Click += Handle_Mouse_Click;
			// 	mouse.DoubleClick += Handle_Mouse_Double_Click;

			// 	mouse.MouseDown += Handle_Mouse_Down;
			// 	mouse.MouseUp += Handle_Mouse_Up;

			// 	mouse.Scroll += Handle_Mouse_Scroll;
			// }
		}

		private static void Closing()
		{
			KaraApp.Dispose();
			Renderer.Dispose();
			gl.Dispose();
		}

		private static void Load()
		{
			ManageInputEvents();
			gl = window.CreateOpenGL();

			OpenGLRenderer nvgRenderer = new(CreateFlags.Antialias | CreateFlags.StencilStrokes | CreateFlags.Debug, gl);
			Renderer = Nvg.Create(nvgRenderer);

			KaraApp.Initialize(Renderer);
		}

		private static void Render(double time)
		{
			Vector2D<float> winSize = window.Size.As<float>();
			Vector2D<float> fbSize = window.FramebufferSize.As<float>();

			float pxRatio = fbSize.X / winSize.X;

			gl.Viewport(0, 0, (uint)winSize.X, (uint)winSize.Y);
			gl.ClearColor(255, 255, 255, 128);
			gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

			Renderer.BeginFrame(winSize.As<float>(), pxRatio);
			KaraApp.Render();
			Renderer.EndFrame();
		}
	}
}