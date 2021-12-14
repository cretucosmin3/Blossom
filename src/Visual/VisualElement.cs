using System;
using System.Drawing;
using System.Numerics;
using System.Collections.Generic;
using SilkyNvg;
using SilkyNvg.Graphics;
using SilkyNvg.Paths;
using SilkyNvg.Scissoring;
using SilkyNvg.Text;
using Kara.Core.Delegates.Common;
using Kara.Utils;

namespace Kara.Core.Visual
{
	public class VisualElement : IDisposable
	{
		public string Name { get; set; }
		public bool HasFocus { get { return ParentView.FocusedElement == this; } }

		internal Application ParentApplication { get; set; }
		internal View ParentView { get; set; }

		private VisualElement _Parent = null;
		public VisualElement Parent
		{
			get => _Parent;
			set
			{
				_Parent = value;
				SetAnchorValues();
			}
		}

		public List<VisualElement> Children { get; set; } = new List<VisualElement>();

		internal event ForDispose OnDisposing;
		public event ForV4 OnResized;
		internal event ForResizeType OnComputedSize;

		public void AddChild(VisualElement child)
		{
			child.Parent = this;
			OnComputedSize += child.HandleParentResized;
			Children.Add(child);
		}

		public void RemoveChild(VisualElement child)
		{
			child.Parent = null;
			OnComputedSize -= child.HandleParentResized;
			Children.Remove(child);
		}

		private bool _Visible = true;
		public bool Visible
		{
			get => _Visible;
			set
			{
				if (value != _Visible)
				{

				}
				else _Visible = value;
			}
		}

		public bool CanRender
		{
			get
			{
				if (Parent != null)
				{
					return Visible ? ParentView.Elements.ComponentsIntersect(this, Parent) : false;
				}
				else
				{
					return Visible;
				}
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

		private Anchor _Anchor = Anchor.Left | Anchor.Top;

		internal float FixedLeft = 0f;
		internal float FixedRight = 0f;
		internal float FixedTop = 0f;
		internal float FixedBottom = 0f;

		internal float RelativeLeft = 0f;
		internal float RelativeRight = 0f;
		internal float RelativeTop = 0f;
		internal float RelativeBottom = 0f;

		public Anchor Anchor
		{
			get => _Anchor;
			set
			{
				_Anchor = value;
				SetAnchorValues();
			}
		}

		private void SetAnchorValues()
		{
			if (!Browser.IsLoaded) return;

			ComputeX();
			ComputeY();
			ComputeWidth();
			ComputeHeight();
		}

		internal RectangleF LocalTransform = new RectangleF(0, 0, 0, 0);
		internal RectangleF ComputedTransform = new RectangleF(0, 0, 0, 0);
		public RectangleF GlobalTransform { get => ComputedTransform; }

		internal void HandleParentResized(ResizeType prop)
		{
			switch (prop)
			{
				case ResizeType.X:
					ComputeX();
					break;
				case ResizeType.Y:
					ComputeY();
					break;
				case ResizeType.Width:
					ComputeWidth();
					break;
				case ResizeType.Height:
					ComputeHeight();
					break;
			}
		}

		private bool XChanged = false;
		private bool YChanged = false;
		private bool WidthChanged = false;
		private bool HeightChanged = false;

		private void ComputeX()
		{
			if (_Anchor.HasFlag(Anchor.Left))
			{
				FixedLeft = X;
				ComputedTransform.X = FixedLeft;
			}
			else if (!_Anchor.HasFlag(Anchor.Right))
			{
				if (XChanged)
				{
					RelativeLeft = (X + (Width / 2f)) / Parent.Width;
					XChanged = false;
				}

				ComputedTransform.X = (Parent.Width * RelativeLeft) - (Width / 2f);
			}

			// Add parent X
			if (Parent != null) ComputedTransform.X += Parent.X;

			// Add app offset render
			if (Parent != null)
				ComputedTransform.X += ParentApplication.OffsetX;
		}

		private void ComputeY()
		{
			if (_Anchor.HasFlag(Anchor.Top))
			{
				FixedTop = Y;
				ComputedTransform.Y = FixedTop;
			}
			else if (!_Anchor.HasFlag(Anchor.Bottom))
			{
				if (YChanged)
				{
					RelativeTop = (Y + (Height / 2f)) / Parent.Height;
					YChanged = false;
				}

				ComputedTransform.Y = (Parent.Height * RelativeTop) - (Height / 2f);
			}

			// Add parent Y
			if (Parent != null) ComputedTransform.Y += Parent.Y;

			// Add app offset render
			if (Parent != null)
				ComputedTransform.Y += ParentApplication.OffsetY;
		}

		private void ComputeWidth()
		{
			if (_Anchor.HasFlag(Anchor.Left))
			{
				ComputedTransform.Width = Width;
			}
			else if (!_Anchor.HasFlag(Anchor.Right))
			{
				if (WidthChanged)
				{
					RelativeRight = (Parent.Width - (X + Width)) / Parent.Width;
					WidthChanged = false;
				}

				ComputedTransform.Width = Parent.Width * RelativeRight;
			}

			// Add parent X
			// if (Parent != null) ComputedTransform.Width += Parent.X;

			// Add app offset render
			// if (Parent != null)
			// 	ComputedTransform.Width += ParentApplication.OffsetX;
		}

		private void ComputeHeight()
		{
			if (_Anchor.HasFlag(Anchor.Top))
			{
				FixedTop = Y;
				ComputedTransform.Height = Height;
			}
			else if (!_Anchor.HasFlag(Anchor.Bottom))
			{
				if (HeightChanged)
				{
					RelativeBottom = (Parent.Height - (Y + Height)) / Parent.Height;
					HeightChanged = false;
				}

				ComputedTransform.Height = Parent.Height * RelativeBottom;
			}

			// Add parent Y
			// if (Parent != null) ComputedTransform.Height += Parent.Y;

			// Add app offset render
			// if (Parent != null)
			// 	ComputedTransform.Height += ParentApplication.OffsetY;
		}

		public float X
		{
			get => LocalTransform.X;
			set
			{
				XChanged = true;
				LocalTransform.X = value;
				ComputeX();
				ComputeWidth();

				OnResized?.Invoke(
					LocalTransform.X,
					LocalTransform.Y,
					LocalTransform.Width,
					LocalTransform.Height
				);

				OnComputedSize?.Invoke(ResizeType.X);

				//! queue render
			}
		}

		public float Y
		{
			get => LocalTransform.Y;
			set
			{
				YChanged = true;
				LocalTransform.Y = value;
				ComputeY();
				ComputeHeight();

				OnResized?.Invoke(
					LocalTransform.X,
					LocalTransform.Y,
					LocalTransform.Width,
					LocalTransform.Height
				);

				OnComputedSize?.Invoke(ResizeType.Y);

				//! queue render
			}
		}

		public float Width
		{
			get => LocalTransform.Width;
			set
			{
				WidthChanged = true;
				LocalTransform.Width = value;
				ComputeWidth();
				ComputeX();

				OnResized?.Invoke(
					LocalTransform.X,
					LocalTransform.Y,
					LocalTransform.Width,
					LocalTransform.Height
				);

				OnComputedSize?.Invoke(ResizeType.Width);

				//! queue render
			}
		}

		public float Height
		{
			get => LocalTransform.Height;
			set
			{
				HeightChanged = true;
				LocalTransform.Height = value;
				ComputeHeight();
				ComputeX();

				OnResized?.Invoke(
					LocalTransform.X,
					LocalTransform.Y,
					LocalTransform.Width,
					LocalTransform.Height
				);

				OnComputedSize?.Invoke(ResizeType.Height);

				//! queue render
			}
		}

		internal void Draw()
		{
			if (CanRender)
			{
				DrawBase();
				DrawText();
				Renderer.Pipe.Reset();

				foreach (var child in Children)
				{
					child.Draw();
				}
			}
		}

		internal void DrawBase()
		{
			if (Parent != null)
			{
				Renderer.Pipe.Scissor(
					Parent.GlobalTransform.X + (Parent.BorderWidth / 2f),
					Parent.GlobalTransform.Y + (Parent.BorderWidth / 2f),
					Parent.GlobalTransform.Width - Parent.BorderWidth,
					Parent.GlobalTransform.Height - Parent.BorderWidth
				);
			}

			Renderer.Pipe.BeginPath();
			Renderer.Pipe.RoundedRect(
				ComputedTransform.X,
				ComputedTransform.Y,
				ComputedTransform.Width,
				ComputedTransform.Height,
				Roundness
			);

			if (_BackColor.A > 0)
			{
				Renderer.Pipe.FillColour(_BackColor);
				Renderer.Pipe.Fill();
			}

			Renderer.Pipe.BeginPath();
			Renderer.Pipe.RoundedRect(
				ComputedTransform.X,
				ComputedTransform.Y,
				ComputedTransform.Width,
				ComputedTransform.Height,
				Roundness
			);

			if (_BorderColor.A > 0f && BorderWidth > 0f)
			{
				Renderer.Pipe.StrokeWidth(BorderWidth);
				Renderer.Pipe.StrokeColour(_BorderColor);
				Renderer.Pipe.Stroke();
			}

			Renderer.Pipe.ClosePath();
		}

		private float TextWidth = 0;
		private void CalculateTextBounds()
		{
			if (Browser.IsLoaded)
				TextWidth = Renderer.Pipe.TextBounds(0, 0, Text, out TextBounds);
		}

		internal void DrawText()
		{
			var cx = ComputedTransform.X;
			var cy = ComputedTransform.Y;
			var cw = ComputedTransform.Width;
			var ch = ComputedTransform.Height;

			Renderer.Pipe.FontSize(FontSize);
			Renderer.Pipe.FontFace("sans");

			var halfBorder = (BorderWidth / 2);

			if (Parent != null)
			{
				Renderer.Pipe.Scissor(
					Parent.GlobalTransform.X + (Parent.BorderWidth / 2f),
					Parent.GlobalTransform.Y + (Parent.BorderWidth / 2f),
					Parent.GlobalTransform.Width - Parent.BorderWidth,
					Parent.GlobalTransform.Height - Parent.BorderWidth
				);
			}
			else Renderer.Pipe.Scissor(cx + halfBorder, cy + halfBorder, cw - BorderWidth, ch - BorderWidth);

			Renderer.Pipe.TextAlign(Align.Middle | Align.Middle);
			CalculateTextBounds();

			float textX = TextAlignment switch
			{
				var x when
					x == TextAlign.Left ||
					x == TextAlign.TopLeft ||
					x == TextAlign.BottomLeft => cx + TextPadding,
				var x when
					x == TextAlign.Right ||
					x == TextAlign.TopRight ||
					x == TextAlign.BottomRight => (X + cw - TextWidth) - TextPadding,
				_ => cx + cw * 0.5f - TextWidth * 0.5f, // Center, other
			};

			float textY = TextAlignment switch
			{
				var x when x == TextAlign.Top ||
					x == TextAlign.TopLeft ||
					x == TextAlign.TopRight => cy + TextBounds.HalfSize.Y + TextPadding,
				var x when x == TextAlign.Bottom ||
					x == TextAlign.BottomLeft ||
					x == TextAlign.BottomRight => cy + ch - TextBounds.HalfSize.Y - TextPadding,
				_ => cy + ch * 0.5f, // Center, other
			};

			textY += 4f;

			// Early return if there's no text
			if (string.IsNullOrEmpty(Text))
				return;

			if (TextShadowColor.A > 0 && TextShadow != Vector2.Zero)
			{
				if (TextShadowSpread > 0) Renderer.Pipe.FontBlur(TextShadowSpread);

				var aria = TextBounds.Size.X + TextBounds.Size.Y;
				Renderer.Pipe.FillColour(Conversion.fromColor(TextShadowColor));
				Renderer.Pipe.Text(textX + aria * (TextShadow.X / 100f), textY + (aria * (TextShadow.Y / 100f)), Text);

				if (TextShadowSpread > 0) Renderer.Pipe.FontBlur(0);
			}

			Renderer.Pipe.FillColour(Conversion.fromColor(FontColor));
			Renderer.Pipe.Text(textX, textY, Text);
		}

		internal void DrawTextShadow()
		{

		}

		public void GetFocus()
		{
			if (ParentView != null)
				ParentView.FocusedElement = this;
		}

		public void Dispose()
		{
			Parent.OnComputedSize -= HandleParentResized;
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