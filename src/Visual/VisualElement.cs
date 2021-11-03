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
using Kara.Core.Delegates.Inputs;
using Kara.Core.Delegates.Common;
using QuadTrees;
using Silk.NET.Input;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

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
			get => Visible && X >= 0 && Y >= 0 && Y < Parent.Height && X < Parent.Width;
		}

		private int _Layer;
		public int Layer
		{
			get => _Layer;
			set
			{
				_Layer = value;
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

		private Color _BorderColor = Color.Transparent;
		public Color BorderColor
		{
			get => _BorderColor;
			set => _BorderColor = value;
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

		private Color _BackColor = Color.Transparent;
		public Color BackColor
		{
			get => _BackColor;
			set => _BackColor = value;
		}

		private string _Text = "";
		public string Text
		{
			get => _Text;
			set
			{
				_Text = value;
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
				//! #render
			}
		}

		private Color _FontColor = Color.Black;
		public Color FontColor
		{
			get => _FontColor;
			set
			{
				_FontColor = value;
				//! #render
			}
		}

		private string _FontName = "sans";

		public string FontName
		{
			get => _FontName;
			set
			{
				_FontName = value;
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
			}
		}

		private Color _TextShadowColor = Color.Black;
		public Color TextShadowColor
		{
			get { return _TextShadowColor; }
			set
			{
				_TextShadowColor = value;
			}
		}

		internal System.Drawing.RectangleF Transform = new System.Drawing.RectangleF(0, 0, 0, 0);

		public float X
		{
			get => Transform.X;
			set
			{
				Transform.X = value;
				//! OnSizeChanged?.Invoke(new Vector4(X, Y, Width, Height));
			}
		}
		public float Y
		{
			get => Transform.Y;
			set
			{
				Transform.Y = value;
				//! OnSizeChanged?.Invoke(new Vector4(X, Y, Width, Height));
			}
		}
		public float Width
		{
			get => Transform.Width;
			set
			{
				Transform.Width = value;
				//! OnSizeChanged?.Invoke(new Vector4(X, Y, Width, Height));
			}
		}
		public float Height
		{
			get => Transform.Height;
			set
			{
				Transform.Height = value;
				//! OnSizeChanged?.Invoke(new Vector4(X, Y, Width, Height));
			}
		}

		#region Events

		#endregion

		internal void Draw()
		{
			DrawBase();
			DrawText();
			Renderer.Reset();
		}

		internal void DrawBase()
		{
			Renderer.BeginPath();
			Renderer.RoundedRect(X, Y, Width, Height, Roundness);

			Renderer.FillColour(
				Renderer.Rgba(
					BackColor.R,
					BackColor.G,
					BackColor.B,
					BackColor.A
				)
			);

			Renderer.Fill();

			Renderer.BeginPath();

			Renderer.RoundedRect(X, Y, Width, Height, Roundness);

			Renderer.StrokeWidth(BorderWidth);
			Renderer.StrokeColour(
				Renderer.Rgba(
					BorderColor.R,
					BorderColor.G,
					BorderColor.B,
					BorderColor.A
				)
			);

			Renderer.Stroke();
			Renderer.ClosePath();
		}

		internal void DrawText()
		{
			Renderer.FontSize(FontSize);
			Renderer.FontFace("sans");
			Renderer.TextAlign(Align.Centre | Align.Centre);

			Silk.NET.Maths.Rectangle<float> bounds;
			float tw = Renderer.TextBounds(X + (Width / 2f), Y + (Height / 2f) + 8, Text, out bounds);

			// Renderer.BeginPath();
			// Renderer.FillColour(Renderer.Rgba(10, 10, 10, 125));
			// Renderer.RoundedRect(bounds.Origin.X, bounds.Origin.Y, bounds.Size.X, bounds.Size.Y, 1f);
			// Renderer.Fill();

			var halfBorder = (BorderWidth / 2);
			Renderer.Scissor(X + halfBorder, Y + halfBorder, Width - BorderWidth, Height - BorderWidth);
			Renderer.FillColour(Renderer.Rgba(FontColor.R, FontColor.G, FontColor.B, FontColor.A));
			Renderer.Text(bounds.Origin.X + bounds.HalfSize.X, bounds.Origin.Y + bounds.HalfSize.Y + 14, Text);
		}

		public void GetFocus()
		{
			if (ApplicationParent != null)
				ApplicationParent.FocusedElement = this;
		}

		public void Dispose()
		{
			//! OnDisposing?.Invoke(this);

			if (Parent != null)
				Parent.Children.Remove(this);

			foreach (var Child in Children)
			{
				Child.Dispose();
			}
		}
	}
}