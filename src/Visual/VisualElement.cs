using System;
using System.Numerics;
using System.Collections.Generic;
using Kara.Core.Delegates.Common;
using SkiaSharp;

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
                Transform.Parent = value.Transform;
            }
        }

        public List<VisualElement> Children { get; set; } = new List<VisualElement>();

        internal event ForDispose OnDisposing;
        public event ForV4 OnResized;

        public Transform Transform { get; set; } = new Transform();

        public void AddChild(VisualElement child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public void RemoveChild(VisualElement child)
        {
            child.Parent = null;
            Children.Remove(child);
        }

        private bool _Visible = true;
        public bool Visible
        {
            get => _Visible;
            set => _Visible = value;
        }

        public bool CanRender
        {
            get
            {
                if (Parent != null)
                    return Visible ? ParentView.Elements.ComponentsIntersect(this, Parent) : false;

                return Visible;
            }
        }

        public int Layer
        {
            get => Parent != null ? Parent.Layer + 1 : 0;
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

        private SKColor _BorderColor = new(0, 0, 0, 0);
        public SKColor BorderColor
        {
            get => _BorderColor;
            set
            {
                _BorderColor = value;
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

        private SKColor _BackColor = new(0, 0, 0, 0);
        public SKColor BackColor
        {
            get => _BackColor;
            set
            {
                _BackColor = value;
                //! #render
            }
        }

        private string _Text = "";
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

        private SKColor _FontColor = new(0, 0, 0, 255);
        public SKColor FontColor
        {
            get => _FontColor;
            set
            {
                _FontColor = value;
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

        private SKColor _TextShadowColor = new(0, 0, 0, 0);
        public SKColor TextShadowColor
        {
            get { return _TextShadowColor; }
            set
            {
                _TextShadowColor = value;
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

        internal void Render()
        {
            // Validate transform
            this.Transform.Evaluate();

            //if (CanRender)
            //{
            DrawBase();

            if (!String.IsNullOrEmpty(Text))
                DrawText();

            foreach (var child in Children)
            {
                child.Render();
            }
            //}
        }

        SKPaint paint = new SKPaint();

        internal void DrawBase()
        {
            SKRect rect = new SKRect(
                Transform.Computed.X,
                Transform.Computed.Y,
                Transform.Computed.X + Transform.Computed.Width,
                Transform.Computed.Y + Transform.Computed.Height
            );

            SKRoundRect roundRect = new SKRoundRect(rect, Roundness);

            paint.Style = SKPaintStyle.Fill;
            paint.Color = BackColor;
            paint.IsAntialias = true;

            Renderer.Canvas.DrawRoundRect(roundRect, paint);

            if (BorderWidth > 0)
            {
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = BorderWidth;
                paint.Color = BorderColor;
            }

            Renderer.Canvas.DrawRoundRect(roundRect, paint);
        }

        private float TextWidth = 0;
        private SKRect TextBounds;
        private void CalculateTextBounds()
        {
            if (Browser.IsLoaded)
                TextPaint.MeasureText(Text, ref TextBounds);
        }


        SKPaint TextPaint = new SKPaint()
        {
            IsAntialias = true,
            Color = SKColors.White,
            TextSize = 30f,
            TextAlign = SKTextAlign.Center,
        };

        internal void DrawText()
        {
            var cx = Transform.Computed.X;
            var cy = Transform.Computed.Y;
            var cw = Transform.Computed.Width;
            var ch = Transform.Computed.Height;

            var halfBorder = (BorderWidth / 2);

            CalculateTextBounds();

            float textX = TextAlignment switch
            {
                var x when
                    x == TextAlign.Left ||
                    x == TextAlign.TopLeft ||
                    x == TextAlign.BottomLeft
                    => cx + TextPadding + TextBounds.MidX,
                var x when
                    x == TextAlign.Right ||
                    x == TextAlign.TopRight ||
                    x == TextAlign.BottomRight
                    => (Transform.X + cw - TextBounds.Width) - TextPadding,
                _ => cx + cw - (TextBounds.Width / 2), // Center, other
            };

            float textY = TextAlignment switch
            {
                var x when
                    x == TextAlign.Top ||
                    x == TextAlign.TopLeft ||
                    x == TextAlign.TopRight
                    => cy + TextBounds.Height + TextPadding,
                var x when
                    x == TextAlign.Bottom ||
                    x == TextAlign.BottomLeft ||
                    x == TextAlign.BottomRight
                    => (cy + ch) - (TextBounds.Height - TextPadding),
                _ => (cy + ch * 0.5f) + TextBounds.Height / 2f, // Center, other
            };

            // Early return if there's no text or color
            if (string.IsNullOrEmpty(Text) || TextShadowColor.Alpha > 0)
                return;

            // Draw shadow
            // if (TextShadow != Vector2.Zero)
            // {
            //     if (TextShadowSpread > 0) Renderer.Pipe.FontBlur(TextShadowSpread);

            //     var aria = TextBounds.Size.X + TextBounds.Size.Y;
            //     Renderer.Pipe.FillColour(Conversion.fromColor(TextShadowColor));
            //     Renderer.Pipe.Text(textX + aria * (TextShadow.X / 100f), textY + (aria * (TextShadow.Y / 100f)), Text);

            //     if (TextShadowSpread > 0) Renderer.Pipe.FontBlur(0);
            // }

            var TextPoint = new SKPoint(textX, textY);
            // set paint

            Renderer.Canvas.DrawText(Text, TextPoint, TextPaint);
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
            //Parent.OnComputedSize -= HandleParentResized;
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