using System.Drawing;
using System;
using SilkyNvg;
using SilkyNvg.Images;
using SilkyNvg.Text;
using Silk.NET.Input;
using Kara.Core.Visual;
using Kara.Core.Input;
using System.Collections.Generic;

namespace Kara.Core
{
	public abstract class Application : IDisposable
	{
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

		public EventMap Events = new();
		public ElementsMap Elements = new();

		private string _title = "";
		public string Title
		{
			get => _title;
			set
			{
				_title = value;
				//! #render title only
			}
		}

		public VisualElement FocusedElement { get; set; }
		public VisualElement Element;

		public void AddElement(VisualElement element)
		{
			Elements.AddElement(ref element, this);
		}

		public void RemoveElement(VisualElement element)
		{
			Elements.RemoveElement(element);
		}

		internal void Initialize(Nvg RenderPipeline)
		{
			Rp = RenderPipeline;

			// _fontIcons = Rp.CreateFont("icons", "./fonts/entypo.ttf");
			// if (_fontIcons == -1)
			// {
			//     Console.Error.WriteLine("Could not add font icons.");
			//     Environment.Exit(-1);
			// }
			_fontNormal = Rp.CreateFont("sans", "./fonts/roboto.medium.ttf");
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

			Element = new VisualElement()
			{
				BackColor = Color.FromArgb(2, 0, 0, 0),
				BorderColor = Color.FromArgb(255, 0, 0, 0),
				FontColor = Color.Black,
				Transform = new RectangleF(20, 20, 200, 80),
				BorderWidth = 2f,
				Roundness = 5f,
				Text = "Hello",
				FontSize = 30,
				TextAlignment = TextAlign.Right,
				TextShadow = new System.Numerics.Vector2(1, 1),
				TextShadowSpread = 0f,
			};
		}

		internal void Render()
		{
			Element.Draw();
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