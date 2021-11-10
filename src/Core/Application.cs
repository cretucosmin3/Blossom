using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System;
using SilkyNvg;
using SilkyNvg.Images;
using SilkyNvg.Text;
using Silk.NET.Input;
using Kara.Core.Visual;
using Kara.Core.Input;
using Kara.Utils;

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

		public EventMap Events = new EventMap();
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
		public VisualElement Element;

		internal void Initialize(Nvg RenderPipeline)
		{
			Rp = RenderPipeline;

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Keypad1 }).Handle(
				(e) => Element.Roundness += 5
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Keypad2 }).Handle(
				(e) => Element.Roundness -= 5
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Keypad3 }).Handle(
				(e) => Element.FontSize += 5
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Keypad4 }).Handle(
				(e) => Element.FontSize -= 5
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Keypad5 }).Handle(
				(e) =>
                {
					Element.BackColor = Color.White;
					Element.FontColor = Color.Black;
				}
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Keypad6 }).Handle(
				(e) =>
                {
					Element.BackColor = Color.Black;
					Element.FontColor = Color.White;
                }
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Up }).Handle(
				(e) => Element.TextAlignment = TextAlign.Top
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Down }).Handle(
				(e) => Element.TextAlignment = TextAlign.Bottom
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Left }).Handle(
				(e) => Element.TextAlignment = TextAlign.Left
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Right }).Handle(
				(e) => Element.TextAlignment = TextAlign.Right
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Enter }).Handle(
				(e) => Element.TextAlignment = TextAlign.Center
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Keypad7}).Handle(
				(e) =>
                {
					Element.TextShadow = new System.Numerics.Vector2(1, 1);
					Element.TextShadowColor = Color.Red;
					Element.TextShadowSpread = 2;
					Element.FontColor = Color.White;
                }
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Keypad8 }).Handle(
				(e) => Element.TextShadowColor = Color.Transparent
			);

			Events.AddHotkey(new[] { Key.ControlLeft, Key.Keypad9 }).Handle(
				(e) => Element.BorderWidth = Element.BorderWidth == 6 ? 2 : 6
			);

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