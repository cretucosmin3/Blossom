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

		#region Style
		private Vector4 _Borders = Vector4.Zero;

		public Vector4 Borders
		{
			get => _Borders;
			set
			{
				_Borders = value;

				// Reposition Borders Panel
				BorderTransform.X = X - value.X;
				BorderTransform.Y = Y - value.Y;
				BorderTransform.Width = Width + value.X + value.Z;
				BorderTransform.Height = Height + value.Y + value.W;
			}
		}

		private System.Drawing.RectangleF BorderTransform = new System.Drawing.RectangleF(0, 0, 0, 0);
		public float BorderLeft
		{
			get => _Borders.X;
			set
			{
				_Borders.X = value;
				BorderTransform.X = X - value;
				BorderTransform.Width = Width + _Borders.Z + _Borders.X;
			}
		}
		public float BorderTop
		{
			get => _Borders.Y;
			set
			{
				_Borders.Y = value;
				BorderTransform.Y = Y - value;
				BorderTransform.Height = Height + _Borders.Y + _Borders.W;
			}
		}
		public float BorderRight
		{
			get => _Borders.Z;
			set
			{
				_Borders.Z = value;
				BorderTransform.Width = Width + Borders.X + _Borders.Z;
			}
		}
		public float BorderBottom
		{
			get => _Borders.W;
			set
			{
				_Borders.W = value;
				BorderTransform.Height = Height + Borders.Y + _Borders.W;
			}
		}

		public float BorderWidth
		{
			set => Borders = new Vector4(value);
		}

		private Color _BorderColor = Color.Transparent;
		public Color BorderColor
		{
			get => _BorderColor;
			set => _BorderColor = value;
		}

		private Color _BackColor = Color.Transparent;
		public Color BackColor
		{
			get => _BackColor;
			set => _BackColor = value;
		}
		#endregion

		private string _Text = "";
		public string Text
		{
			get => _Text;
			set => _Text = value;
		}

		private int _Text_Spacing = 2;
		public int TextSpacing
		{
			get => _Text_Spacing;
			set => _Text_Spacing = value;
		}

		private int _FontSize = 18;
		public int FontSize
		{
			get => _FontSize;
			set => _FontSize = value;
		}

		private Color _FontColor = Color.Black;
		public Color FontColor
		{
			get => _FontColor;
			set => _FontColor = value;
		}

		private string _FontName = "arial";

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

		#region Transform
		internal System.Drawing.RectangleF Transform = new System.Drawing.RectangleF(0, 0, 0, 0);

		public float X
		{
			get => Transform.X;
			set
			{
				Transform.X = value;
				BorderTransform.X = value - _Borders.X;
				//! OnSizeChanged?.Invoke(new Vector4(X, Y, Width, Height));
			}
		}
		public float Y
		{
			get => Transform.Y;
			set
			{
				Transform.Y = value;
				BorderTransform.Y = value - _Borders.Y;
				//! OnSizeChanged?.Invoke(new Vector4(X, Y, Width, Height));
			}
		}
		public float Width
		{
			get => Transform.Width;
			set
			{
				Transform.Width = value;
				BorderTransform.Width = value + _Borders.X + _Borders.Z;
				//! OnSizeChanged?.Invoke(new Vector4(X, Y, Width, Height));
			}
		}
		public float Height
		{
			get => Transform.Height;
			set
			{
				Transform.Height = value;
				BorderTransform.Height = value + _Borders.Y + _Borders.W;
				//! OnSizeChanged?.Invoke(new Vector4(X, Y, Width, Height));
			}
		}
		#endregion

		#region Events

		#endregion

		#region Internal Only
		internal void Draw()
		{

		}
		#endregion

		public void GetFocus()
		{
			if (ApplicationParent != null)
				ApplicationParent.FocusedElement = this;
		}

		internal void DrawText()
		{

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