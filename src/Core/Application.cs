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
		// Keyboard
		public event ForKey OnKeyboardDown;
		public event ForKey OnKeyboardUp;
		public event ForChar OnKeyboardType;
		public event ForKeybind OnKeyboardKeybind;

		// Mouse
		public event ForPosition OnMouseMove;
		public event ForPosition OnMouseScroll;
		public event ForKey OnMouseDown;
		public event ForKey OnMouseUp;
		public event ForKey OnMouseClick;
		public event ForKey OnMouseDoubleClick;

		// Application
		public event ForString OnTitleChanged;
		#endregion

		#region Event Invokers
		// Keyboard
		public void DoKeyDown(int val) => OnKeyboardDown?.Invoke(val);
		public void DoKeyUp(int val) => OnKeyboardUp?.Invoke(val);
		public void DoKeyChar(char k) => OnKeyboardType?.Invoke(k);
		public void DoKeybind(int[] k) => OnKeyboardKeybind?.Invoke(k);

		// Mouse
		public void DoMouseDown(int key) => OnMouseDown?.Invoke(key);
		public void DoMouseUp(int key) => OnMouseUp?.Invoke(key);
		public void DoMouseClick(int key) => OnMouseClick?.Invoke(key);
		public void DoMouseDoubleClick(int key) => OnMouseDoubleClick?.Invoke(key);
		public void DoMouseScroll(int x, int y) => OnMouseScroll?.Invoke(x, y);

		public void DoMouseMove(int x, int y) => OnMouseMove?.Invoke(x, y);
		#endregion

		#region Properties
		// Privates
		private string _title = "";

		// Publics
		public string Title
		{
			get
			{
				return _title;
			}
			set
			{
				_title = value;
				OnTitleChanged?.Invoke(value);
			}
		}
		public VisualElement FocusedElement { get; set; }
		#endregion

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

			_ = Rp.AddFallbackFontId(_fontNormal, _fontEmoji);
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
				TextAlignment = TextAlign.Center,
				TextShadow = new System.Numerics.Vector2(-1f, 1f),
			};
		}

		private Random rnd = new Random();
		private void DrawSearchBox(string text, float x, float y, float w, float h, float fontSize)
		{
			Rp.FontSize(fontSize);
			Rp.FontFace("sans");
			Rp.FillColour(new Colour(0, 0, 0, 255));

			float tw = Rp.TextBounds(0.0f, 0.0f, text, out _);
			Rp.TextAlign(Align.Left | Align.Middle);
			Rp.Text(x + (w / 2f) - (tw / 2f), y + h * 0.5f, text);
		}

		bool up = true;
		float value = 1f;
		float max = 1.5f;
		float min = -1.5f;
		float increment = 0.2f;
		internal void Render(double time)
		{
			DrawSearchBox($"{(time).ToString("0.00")} FPS", 400f, 250f, 200f, 80f, 12f);
			Button.Draw();

            if (up)
            {
				value += increment;
				if (value >= max)
                {
					up = false;
                }
            }
			else
            {
				value -= increment;
				if (value <= min)
				{
					up = true;
				}
			}

			Button.TextShadow = new System.Numerics.Vector2(value, value);
		}

		private void WindowLoad()
		{

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