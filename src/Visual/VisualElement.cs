using System.Security.Cryptography;
using System.ComponentModel;
using System;
using System.Net.Mime;
using System.Text;
using SilkyNvg;
using SilkyNvg.Graphics;
using SilkyNvg.Images;
using SilkyNvg.Paths;
using SilkyNvg.Scissoring;
using SilkyNvg.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Kara.Utils;

namespace Kara.Core.Visual
{
	public class VisualElement : IDisposable
	{
		private Nvg Renderer { get => Browser.Renderer; }
		public string Name { get; set; }
		public bool HasFocus { get { return ApplicationParent.FocusedElement == this; } }
		public bool IsHovered { get; set; } = false;

		internal Application ApplicationParent { get; set; }
		public VisualElement Parent { get; set; } = null;
		public List<VisualElement> Children { get; set; } = new List<VisualElement>();

		internal event ForDispose OnDisposing;

		private bool _Visible = true;
		public bool Visible
		{
			get => _Visible;
			set
			{
				if (value != _Visible) { } // Application render
				else _Visible = value;
			}
		}

		public bool CanRender
		{
			get
			{
				var pw = Parent != null ? Parent.Width : Browser.window.Size.X;
				var ph = Parent != null ? Parent.Width : Browser.window.Size.X;
				return Visible && X >= 0 && Y >= 0 && Y < ph && X < pw;
			}

		}

		private int _Layer;
		public int Layer
		{
			get => _Layer;
			set
			{
				_Layer = value;
				//! #render
				//! OnLayerChanged?.Invoke(this, value);
			}
		}

		private float _BorderWidth = 0f;
		public float BorderWidth
		{
			get => _BorderWidth;
			set
			{
				_BorderWidth = value;
				//! #render
			}
		}

		private Colour _BorderColor = new(0, 0, 0, 0);
		public Color BorderColor
		{
			get => Conversion.toColor(_BorderColor);
			set
			{
				_BorderColor = Conversion.fromColor(value);
				//! #render
			}
		}

		private float _Roundness = 0f;
		public float Roundness
		{
			get => _Roundness;
			set
			{
				_Roundness = value;
				//! #render
			}
		}

		private Colour _BackColor = new(255, 255, 255, 255);
		public Color BackColor
		{
			get => Conversion.toColor(_BackColor);
			set
			{
				_BackColor = Conversion.fromColor(value);
				//! #render
			}
		}

		private string _Text;
		public string Text
		{
			get => _Text;
			set
			{
				_Text = value;
				CalculateTextBounds();
				//! #render
			}
		}

		private int _Text_Spacing = 2;
		public int TextSpacing
		{
			get => _Text_Spacing;
			set
			{
				_Text_Spacing = value;
				//! #render
			}
		}

		private float _FontSize = 18f;
		public float FontSize
		{
			get => _FontSize;
			set
			{
				_FontSize = value;
				CalculateTextBounds();
				//! #render
			}
		}

		private Colour _FontColor = new(0, 0, 0, 255);
		public Color FontColor
		{
			get => Conversion.toColor(_FontColor);
			set
			{
				_FontColor = Conversion.fromColor(value);
				//! #render
			}
		}

		private string _FontName = "sans";
		public string Font
		{
			get => _FontName;
			set
			{
				_FontName = value;
				//! #render
				//! TextFont = Fonts.Get(value);
			}
		}

		private Vector2 _TextShadow = Vector2.Zero;
		public Vector2 TextShadow
		{
			get => _TextShadow;
			set
			{
				_TextShadow = value;
				//! #render
			}
		}

		private Colour _TextShadowColor = new(0, 0, 0, 0);
		public Color TextShadowColor
		{
			get { return Conversion.toColor(_TextShadowColor); }
			set
			{
				_TextShadowColor = Conversion.fromColor(value);
				//! #render
			}
		}

		private float _TextShadowSpread = 0f;
		public float TextShadowSpread
		{
			get => _TextShadowSpread;
			set
			{
				_TextShadowSpread = value;
				//! #render
			}
		}

		private TextAlign _TextAlignment = TextAlign.Center;
		public TextAlign TextAlignment
		{
			get => _TextAlignment;
			set
			{
				_TextAlignment = value;
				//! #render
			}
		}

		private float _TextPadding = 0f;
		public float TextPadding
		{
			get => _TextPadding;
			set
			{
				_TextPadding = value;
				//! #render
			}
		}

		private Silk.NET.Maths.Rectangle<float> TextBounds;
		internal System.Drawing.RectangleF Transform = new System.Drawing.RectangleF(0, 0, 0, 0);

		public float X
		{
			get => Transform.X;
			set
			{
				Transform.X = value;
				//! #render
				//! OnSizeChanged?.Invoke(new Vector4(X, Y, Width, Height));
			}
		}
		public float Y
		{
			get => Transform.Y;
			set
			{
				Transform.Y = value;
				//! #render
				//! OnSizeChanged?.Invoke(new Vector4(X, Y, Width, Height));
			}
		}
		public float Width
		{
			get => Transform.Width;
			set
			{
				Transform.Width = value;
				//! #render
				//! OnSizeChanged?.Invoke(new Vector4(X, Y, Width, Height));
			}
		}
		public float Height
		{
			get => Transform.Height;
			set
			{
				Transform.Height = value;
				//! #render
				//! OnSizeChanged?.Invoke(new Vector4(X, Y, Width, Height));
			}
		}

		internal void Draw()
		{
			if (CanRender)
			{
				DrawBase();
				DrawText();
				Renderer.Reset();
			}
		}

		internal void DrawBase()
		{
			Renderer.BeginPath();
			Renderer.RoundedRect(X, Y, Width, Height, Roundness);

			if (_BackColor.A > 0)
			{
				Renderer.FillColour(_BackColor);
				Renderer.Fill();
			}

			Renderer.BeginPath();
			Renderer.RoundedRect(X, Y, Width, Height, Roundness);

			if (_BorderColor.A > 0f && BorderWidth > 0f)
			{
				Renderer.StrokeWidth(BorderWidth);
				Renderer.StrokeColour(_BorderColor);
				Renderer.Stroke();
			}

			Renderer.ClosePath();
		}

		private float TextWidth = 0;
		private void CalculateTextBounds()
		{
			TextWidth = Renderer.TextBounds(0, 0, Text, out TextBounds);
		}

		internal void DrawText()
		{
			Renderer.FontSize(FontSize);
			Renderer.FontFace("sans");

			var halfBorder = (BorderWidth / 2);
			Renderer.Scissor(X + halfBorder, Y + halfBorder, Width - BorderWidth, Height - BorderWidth);

			Renderer.TextAlign(Align.Middle | Align.Middle);
			CalculateTextBounds();

			float textX = TextAlignment switch
			{
				var x when
					x == TextAlign.Left ||
					x == TextAlign.TopLeft ||
					x == TextAlign.BottomLeft => X + TextPadding,
				var x when
					x == TextAlign.Right ||
					x == TextAlign.TopRight ||
					x == TextAlign.BottomRight => (X + Width - TextWidth) - TextPadding,
				_ => X + Width * 0.5f - TextWidth * 0.5f, // Center, other
			};

			float textY = TextAlignment switch
			{
				var x when x == TextAlign.Top ||
					x == TextAlign.TopLeft ||
					x == TextAlign.TopRight => Y + TextBounds.HalfSize.Y + TextPadding,
				var x when x == TextAlign.Bottom ||
					x == TextAlign.BottomLeft ||
					x == TextAlign.BottomRight => Y + Height - TextBounds.HalfSize.Y - TextPadding,
				_ => Y + Height * 0.5f, // Center, other
			};

			textY += 4f;

			// Early return if there's no text
			if (string.IsNullOrEmpty(Text))
				return;

			if (TextShadowColor.A > 0 && TextShadow != Vector2.Zero)
			{
				if (TextShadowSpread > 0) Renderer.FontBlur(TextShadowSpread);

				var aria = TextBounds.Size.X + TextBounds.Size.Y;
				Renderer.FillColour(Conversion.fromColor(TextShadowColor));
				Renderer.Text(textX + aria * (TextShadow.X / 100f), textY + (aria * (TextShadow.Y / 100f)), Text);

				if (TextShadowSpread > 0) Renderer.FontBlur(0);
			}

			Renderer.FillColour(Conversion.fromColor(FontColor));
			Renderer.Text(textX, textY, Text);
		}

		internal void DrawTextShadow()
		{

		}

		public void GetFocus()
		{
			if (ApplicationParent != null)
				ApplicationParent.FocusedElement = this;
		}

		public void Dispose()
		{
			OnDisposing?.Invoke(this);

			if (Parent != null)
				Parent.Children.Remove(this);

			foreach (var Child in Children)
			{
				Child.Dispose();
			}
		}
	}
}