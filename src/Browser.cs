using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkyNvg;
using SilkyNvg.Rendering.OpenGL;
using Kara.Core;
using Kara.Core.Input;
using Kara.Core.Visual;
using System.Drawing;
using Kara.Core.Delegates.Common;
using System;

namespace Kara
{
	public static class Browser
	{
		private static GL gl;
		internal static Nvg RenderPipeline;
		internal static IWindow window;

		internal static int RenderOffsetX = 0;
		internal static int RenderOffsetY = 0;

		internal static Application BrowserApp = new BrowserApplication();

		public static event ForVoid OnLoaded;
		public static bool IsLoaded { get; private set; } = false;

		public static void Initialize()
		{
			// RenderOffsetX = 0;
			// RenderOffsetY = 0;

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
			gl = window.CreateOpenGL();

			OpenGLRenderer nvgRenderer = new(CreateFlags.Antialias | CreateFlags.StencilStrokes | CreateFlags.Debug, gl);
			RenderPipeline = Nvg.Create(nvgRenderer);
			Renderer.Initialize(RenderPipeline);

			ManageInputEvents();
			IsLoaded = true;
			OnLoaded?.Invoke();
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

	public class Button : VisualElement
	{

	}

	public class MainView : View
	{
		public override void Main()
		{
			VisualElement parent = new VisualElement()
			{
				Name = "parent",
				Text = "Parent",
				X = 50,
				Y = 50,
				Width = 400,
				Height = 400,
				FontSize = 20,
				BorderWidth = 2,
				BorderColor = Color.Black,
				TextAlignment = TextAlign.Bottom,
				TextPadding = 10,
				Anchor = Anchor.Top | Anchor.Left,
			};

			VisualElement child = new VisualElement()
			{
				Name = "child",
				Text = "Child",
				X = 10,
				Y = 10,
				Width = 80,
				Height = 50,
				FontSize = 20,
				BorderWidth = 2,
				BorderColor = Color.Red,
				Anchor = Anchor.Bottom | Anchor.Left,
			};

			Name = "MainView";
			Events.OnKeyDown += (int K) =>
			{
				Log.Debug($"{K}");

				if (K == 114) parent.X += 5;
				if (K == 113) parent.X -= 5;
				if (K == 116) parent.Y += 5;
				if (K == 111) parent.Y -= 5;

				if (K == 38) parent.Width -= 5;
				if (K == 40) parent.Width += 5;
			};

			Elements.AddElement(ref parent, this);
			Elements.AddElement(ref child, this);

			parent.AddChild(child);

			new Thread(() =>
			{
				var from = 50;
				var to = 200;
				var oscilateDirectiton = true;

				// oscilate between from and to
				while (true)
				{
					if (oscilateDirectiton)
					{
						if (parent.X < to)
						{
							parent.X += 1;
							parent.Width = parent.X * 2.3f;
							parent.Height = parent.X * 2.3f;
						}
						else
							oscilateDirectiton = false;
					}
					else
					{
						if (parent.X > from)
						{
							parent.X -= 1;
							parent.Width = parent.X * 2.3f;
							parent.Height = parent.X * 2.3f;
						}
						else
							oscilateDirectiton = true;
					}

					Thread.Sleep(15);
				}
			}).Start();
		}
	}

	public class BrowserApplication : Application
	{
		private View mainView = new MainView();
		public BrowserApplication()
		{
			this.Events.Access = EventAccess.Keyboard;

			AddView(mainView);
			SetActiveView(mainView);
		}
	}
}