using System.Diagnostics;
using System.Net;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Drawing;
using System;
using SilkyNvg;
using SilkyNvg.Images;
using SilkyNvg.Text;
using Silk.NET.Input;
using Kara.Core.Visual;
using Kara.Core.Input;
using System.Collections.Generic;
using Kara.Utils;
using SilkyNvg.Graphics;
using SilkyNvg.Paths;
using StbImageSharp;

namespace Kara.Core
{
	internal static class Renderer
	{
		private static Nvg _renderPipeline;
		private static readonly object _lock = new();
		private static int DefaultFont;

		internal static Nvg Pipe
		{
			get => _renderPipeline;
		}

		internal static void Initialize(Nvg renderPipeline)
		{
			_renderPipeline = renderPipeline;
			DefaultFont = Renderer.Pipe.CreateFont("sans", "./fonts/roboto.medium.ttf");
		}
	}
}