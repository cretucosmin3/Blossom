using System.Diagnostics.Tracing;
using System.Drawing;
using System;
using Silk.NET.Maths;
using SilkyNvg;
using SilkyNvg.Graphics;
using SilkyNvg.Images;
using SilkyNvg.Paths;
using SilkyNvg.Scissoring;
using SilkyNvg.Text;
using Silk.NET.Input;
using Kara.Core.Delegates.Inputs;
using Kara.Core.Delegates.Common;
using Kara.Core.Visual;
using Kara.Core.Input;

namespace Kara.Core
{
	public class Application : IDisposable
	{
		#region Rendering
		private const int ICON_SEARCH = 0x1F50D;
		private const int ICON_CIRCLED_CROSS = 0x2716;
		private const int ICON_CHEVRON_RIGHT = 0xE75E;
		private const int ICON_CHECK = 0x2713;
		private const int ICON_LOGIN = 0xE740;
		private const int ICON_TRASH = 0xE729;

		private int _fontNormal, _fontBold, _fontIcons, _fontEmoji;
		private int[] _images = new int[12];

		/// <summary>
		/// Render Pipeline
		/// </summary>
		internal Nvg Rp;
		#endregion

		#region Events

		public EventMap Events = new EventMap();

		#endregion

		private string _title = "";
		public string Title
		{
			get => _title;
			set
			{
				_title = value;
				//! #render
			}
		}

		public VisualElement FocusedElement { get; set; }
		public VisualElement Button;

		internal void Initialize(Nvg RenderPipeline)
		{
			Rp = RenderPipeline;

			// _fontIcons = Rp.CreateFont("icons", "./fonts/entypo.ttf");
			// if (_fontIcons == -1)
			// {
			//     Console.Error.WriteLine("Could not add font icons.");
			//     Environment.Exit(-1);
			// }
			_fontNormal = Rp.CreateFont("sans", "./fonts/Roboto-Regular.ttf");
			// if (_fontIcons == -1)
			// {
			//     Console.Error.WriteLine("Could not add font regular.");
			//     Environment.Exit(-1);
			// }
			// _fontBold = Rp.CreateFont("sans-bold", "./fonts/Roboto-Bold.ttf");
			// if (_fontIcons == -1)
			// {
			//     Console.Error.WriteLine("Could not add font bold.");
			//     Environment.Exit(-1);
			// }
			// _fontEmoji = Rp.CreateFont("emoji", "./fonts/NotoEmoji-Regular.ttf");
			// if (_fontIcons == -1)
			// {
			//     Console.Error.WriteLine("Could not add font emoji.");
			//     Environment.Exit(-1);
			// }

			// _ = Rp.AddFallbackFontId(_fontNormal, _fontEmoji);
			// _ = Rp.AddFallbackFontId(_fontBold, _fontEmoji);

			//! Element sample
			Button = new VisualElement()
			{
				BackColor = Color.FromArgb(200, 0, 0, 0),
				BorderColor = Color.Red,
				FontColor = Color.White,
				TextShadowColor = Color.Red,
				Transform = new RectangleF(50, 50, 200, 100),
				Roundness = 10f,
				BorderWidth = 5f,
				Text = "Click me!",
				FontSize = 25,
				TextAlignment = TextAlign.Top,
				TextPadding = 20f,
				TextShadow = new System.Numerics.Vector2(-1f, 1f),
				TextShadowSpread = 2f,
			};
		}

		internal void Render()
		{
			Button.Draw();
		}

		public void Dispose()
		{
			for (uint i = 0; i < 12; i++)
			{
				Rp.DeleteImage(_images[i]);
			}
		}
	}
}