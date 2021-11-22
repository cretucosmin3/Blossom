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
		internal static Nvg RenderPipeline;
		internal static IWindow window;

		internal static int RenderOffsetX;
		internal static int RenderOffsetY;

		/// <summary>
		/// Application of the browser
		/// </summary>
		internal static Application BrowserApp = new WebApplication();

		public static void Initialize()
		{
			BrowserApp.RenderOffsetX = 0;
			BrowserApp.RenderOffsetY = 0;

			WindowOptions windowOptions = WindowOptions.Default;
			windowOptions.FramesPerSecond = -1;
			windowOptions.ShouldSwapAutomatically = true;
			windowOptions.Size = new Vector2D<int>(1000, 600);
			windowOptions.Title = "Kara";
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
				window.DoRender();
				window.DoEvents();
				window.ContinueEvents();
				Thread.Sleep(1);
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

					Log.Debug($"Key down {key}");
					var BrowserHandled = BrowserApp.Events.HandleKeyDown(key, i);
				};

				keyboard.KeyUp += (IKeyboard _, Key key, int i) =>
				{
					BrowserApp.Events.HandleKeyUp(key, i);
				};

				keyboard.KeyChar += (IKeyboard _, char ch) =>
				{
					BrowserApp.Events.HandleKeyChar(ch);
				};
			}

			// Register mouse events
			foreach (IMouse mouse in input.Mice)
			{

				// mouse.MouseMove += (IMouse _, Vector2 pos) =>
				// {
				// 	KaraApp.Events.(pos.X, pos.Y);
				// };

				// mouse.Click += Handle_Mouse_Click;
				// mouse.DoubleClick += Handle_Mouse_Double_Click;

				// mouse.MouseDown += Handle_Mouse_Down;
				// mouse.MouseUp += Handle_Mouse_Up;

				// mouse.Scroll += Handle_Mouse_Scroll;
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
			ManageInputEvents();
			gl = window.CreateOpenGL();

			OpenGLRenderer nvgRenderer = new(CreateFlags.Antialias | CreateFlags.StencilStrokes | CreateFlags.Debug, gl);
			RenderPipeline = Nvg.Create(nvgRenderer);

			Renderer.Initialize(RenderPipeline);
		}

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
			RenderPipeline.EndFrame();
		}
	}

	public class MainView : View
	{
		public MainView()
		{
			Log.Debug("MainView created");
		}
	}

	public class WebApplication : Application
	{
		public WebApplication()
		{
			this.Events.Access = EventAccess.Keyboard;

			AddView(new MainView());
		}
	}
}