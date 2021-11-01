using System.Net.Mime;
using System.Threading;
using System;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkyNvg;
using SilkyNvg.Rendering.OpenGL;
using StbImageWriteSharp;
using System.Diagnostics;
using System.IO;
using Kara.Core;


namespace Kara
{
	public static class Browser
	{
		private static GL gl;
		private static Nvg nvg;

		private static double prevTime = 0;

		private static IWindow window;
		private static Stopwatch timer;

		internal static EventMap Events = new EventMap();
		internal static Application BrowserApplication = new Application();

		public static void Initialize()
		{
			Events.AddKeybind(new Key[] { Key.ControlLeft, Key.C }, () =>
			{
				Console.WriteLine("Copy pressed (Ctrl + C)");
			});

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

		private static void Closing()
		{
			timer.Stop();
			BrowserApplication.Dispose();
			nvg.Dispose();
			gl.Dispose();
		}

		private static void Load()
		{
			Events.GetFromWindow(window);

			gl = window.CreateOpenGL();

			OpenGLRenderer nvgRenderer = new(CreateFlags.Antialias | CreateFlags.StencilStrokes | CreateFlags.Debug, gl);
			nvg = Nvg.Create(nvgRenderer);

			BrowserApplication.Initialize(nvg);

			timer = Stopwatch.StartNew();

			timer.Restart();
			prevTime = timer.Elapsed.TotalMilliseconds;
		}

		private static void Render(double time)
		{
			double t = timer.Elapsed.TotalSeconds;
			double dt = t - prevTime;
			prevTime = t;

			Vector2D<float> winSize = window.Size.As<float>();
			Vector2D<float> fbSize = window.FramebufferSize.As<float>();

			float pxRatio = fbSize.X / winSize.X;

			gl.Viewport(0, 0, (uint)winSize.X, (uint)winSize.Y);
			gl.ClearColor(255, 255, 255, 128);
			gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

			nvg.BeginFrame(winSize.As<float>(), pxRatio);

			BrowserApplication.Render(prevTime);

			nvg.EndFrame();
		}
	}
}