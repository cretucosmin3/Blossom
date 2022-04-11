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

        private ElementStyle _Style = new ElementStyle();
        public ElementStyle Style { 
            get => _Style;
            set {
                _Style = value;
                _Style.ElementRef = this;
            }
        }

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

            Transform.ClearRenderData();
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

            SKRoundRect roundRect = new SKRoundRect();

            roundRect.SetRectRadii(rect,new SKPoint[] {
                new SKPoint(Style.Roundness,Style.Roundness),
                new SKPoint(Style.Roundness,Style.Roundness),
                new SKPoint(Style.Roundness,Style.Roundness),
                new SKPoint(Style.Roundness,Style.Roundness),
            });

            paint.Style = SKPaintStyle.Fill;
            paint.Color = Style.BackColor;
            paint.IsAntialias = true;

            Renderer.Canvas.DrawRoundRect(roundRect, paint);

            if (Style.BorderWidth > 0)
            {
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = Style.BorderWidth;
                paint.Color = Style.BorderColor;
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
            Color = SKColors.SkyBlue,
            TextSize = 75f,
            TextAlign = SKTextAlign.Left,
            StrokeJoin = SKStrokeJoin.Miter,
            IsStroke = true,
            StrokeWidth = 1f,
            Typeface = SKTypeface.FromFamilyName("Bitstream Charter", SKTypefaceStyle.Normal)
        };

        float advance = 0;
        internal void DrawText()
        {
            // Early return if there's no text or color
            if (string.IsNullOrEmpty(Text))
                return;

            advance += 0.030f;
            var cx = Transform.Computed.X;
            var cy = Transform.Computed.Y;
            var cw = Transform.Computed.Width;
            var ch = Transform.Computed.Height;

            var halfBorder = (Style.BorderWidth / 2);

            CalculateTextBounds();

            float textX = Style.Text.Alignment switch
            {
                var x when
                    x == TextAlign.Left ||
                    x == TextAlign.TopLeft ||
                    x == TextAlign.BottomLeft
                    => cx + Style.Text.Padding,
                var x when
                    x == TextAlign.Right ||
                    x == TextAlign.TopRight ||
                    x == TextAlign.BottomRight
                    => cx + cw - TextBounds.Width - Style.Text.Padding,
                _ => cx + (cw / 2f) - TextBounds.MidX // Center, other
            };

            float textY = Style.Text.Alignment switch
            {
                var x when
                    x == TextAlign.Top ||
                    x == TextAlign.TopLeft ||
                    x == TextAlign.TopRight
                    => (cy + TextBounds.Height + Style.Text.Padding) - TextBounds.Bottom,
                var x when
                    x == TextAlign.Bottom ||
                    x == TextAlign.BottomLeft ||
                    x == TextAlign.BottomRight
                    => cy + ch - Style.Text.Padding,
                _ => cy + (ch / 2f) - TextBounds.MidY // Center, other
            };

            var TextPoint = new SKPoint(textX, textY);

            TextPaint.StrokeWidth = 0;
            TextPaint.IsStroke = false;
            TextPaint.Color = Style.Text.Color;
            TextPaint.PathEffect = null;
            TextPaint.TextSize = Style.Text.Size;

            Renderer.Canvas.DrawText(Text, TextPoint, TextPaint);

            // TextPaint.Color = SKColors.DimGray;
            // TextPaint.IsStroke = true;
            // TextPaint.StrokeWidth = 3;
            // TextPaint.PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, advance);

            // Renderer.Canvas.DrawText(Text, TextPoint, TextPaint);
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