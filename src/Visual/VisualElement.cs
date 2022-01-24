using System;
using System.Drawing;
using System.Numerics;
using System.Collections.Generic;
using Kara.Core.Delegates.Common;
using Kara.Utils;
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
                SetAnchorValues();
            }
        }

        public List<VisualElement> Children { get; set; } = new List<VisualElement>();

        internal event ForDispose OnDisposing;
        public event ForV4 OnResized;

        public void AddChild(VisualElement child)
        {
            child.Parent = this;

            SetAnchorValues();
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
                    return Visible ? ParentView.Elements.ComponentsIntersect(this, Parent) : false;
                else
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

        private Anchor _Anchor;

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

        public bool FixedHeight { get; set; } = false;
        public bool FixedWidth { get; set; } = false;

        internal void SetAnchorValues()
        {
            if (!Browser.IsLoaded) return;

            CalculateHorizontalAnchors();
            CalculateVerticalAnchors();

            ComputeHorizontalTransform();
            ComputeVerticalTransform();
        }

        internal RectangleF LocalTransform = new RectangleF(0, 0, 0, 0);
        internal RectangleF ComputedTransform = new RectangleF(0, 0, 0, 0);
        public RectangleF GlobalTransform { get => ComputedTransform; }

        internal void HandleParentHorizontalResized() => ComputeHorizontalTransform();
        internal void HandleParentVerticalResized() => ComputeVerticalTransform();

        private bool XChanged = false;
        private bool YChanged = false;
        private bool WidthChanged = false;
        private bool HeightChanged = false;

        private void CalculateHorizontalAnchors()
        {
            var ParentWidth = Parent != null ? Parent.LocalTransform.Width : Browser.RenderRect.Width;

            FixedLeft = X;
            RelativeLeft = FixedLeft / ParentWidth;

            FixedRight = ParentWidth - (X + Width);
            RelativeRight = FixedRight / ParentWidth;
        }

        private void ComputeHorizontalTransform()
        {
            var ParentWidth = Parent != null ? Parent.ComputedTransform.Width : Browser.RenderRect.Width;

            if (_Anchor.HasFlag(Anchor.Left) && !_Anchor.HasFlag(Anchor.Right))
            {
                ComputedTransform.X = FixedLeft;
                ComputedTransform.Width = Width;
            }
            else if (_Anchor.HasFlag(Anchor.Right) && !_Anchor.HasFlag(Anchor.Left))
            {
                ComputedTransform.X = ParentWidth - FixedRight - Width;
                ComputedTransform.Width = Width;
            }
            else if (_Anchor.HasFlag(Anchor.Left) && _Anchor.HasFlag(Anchor.Right))
            {
                ComputedTransform.X = FixedLeft;
                ComputedTransform.Width = ParentWidth - FixedLeft - FixedRight;
            }
            else
            {
                ComputedTransform.X = RelativeLeft * ParentWidth;
                ComputedTransform.Width = ParentWidth - (RelativeRight * ParentWidth) - ComputedTransform.X;

                if (FixedWidth)
                {
                    var centerX = ComputedTransform.X + (ComputedTransform.Width / 2f);
                    ComputedTransform.X = centerX - (Width / 2f);
                    ComputedTransform.Width = Width;
                }
            }

            if (ComputedTransform.Width < 0)
            {
                ComputedTransform.Width = 0;
            }

            // Add parent X
            ComputedTransform.X += Parent != null ? Parent.ComputedTransform.X : Browser.RenderRect.X;
        }

        private void CalculateVerticalAnchors()
        {
            var ParentHeight = Parent != null ? Parent.LocalTransform.Height : Browser.RenderRect.Height;

            FixedTop = Y;
            RelativeTop = FixedTop / ParentHeight;

            FixedBottom = ParentHeight - (Y + Height);
            RelativeBottom = FixedBottom / ParentHeight;
        }

        private void ComputeVerticalTransform()
        {
            var ParentHeight = Parent != null ? Parent.ComputedTransform.Height : Browser.RenderRect.Height;

            bool bottomAnchored = _Anchor.HasFlag(Anchor.Bottom);
            bool topAnchored = _Anchor.HasFlag(Anchor.Top);

            if (_Anchor.HasFlag(Anchor.Top) && !_Anchor.HasFlag(Anchor.Bottom))
            {
                ComputedTransform.Y = FixedTop;
                ComputedTransform.Height = Height;
            }
            else if (bottomAnchored && !topAnchored)
            {
                ComputedTransform.Y = ParentHeight - FixedBottom - Height;
                ComputedTransform.Height = Height;
            }
            else if (_Anchor.HasFlag(Anchor.Top) && _Anchor.HasFlag(Anchor.Bottom))
            {
                ComputedTransform.Y = FixedTop;
                ComputedTransform.Height = ParentHeight - FixedTop - FixedBottom;
            }
            else
            {
                ComputedTransform.Y = RelativeTop * ParentHeight;
                ComputedTransform.Height = ParentHeight - (RelativeBottom * ParentHeight) - ComputedTransform.Y;

                if (FixedHeight)
                {
                    var centerY = ComputedTransform.Y + (ComputedTransform.Height / 2f);
                    ComputedTransform.Y = centerY - (Height / 2f);
                    ComputedTransform.Height = Height;
                }
            }

            if (ComputedTransform.Height < 0)
            {
                ComputedTransform.Height = 0;
            }

            // Add parent Y
            ComputedTransform.Y += Parent != null ? Parent.ComputedTransform.Y : Browser.RenderRect.Y;
        }

        public float X
        {
            get => LocalTransform.X;
            set
            {
                XChanged = true;
                LocalTransform.X = value;
                CalculateHorizontalAnchors();

                OnResized?.Invoke(
                    LocalTransform.X,
                    LocalTransform.Y,
                    LocalTransform.Width,
                    LocalTransform.Height
                );

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
                CalculateVerticalAnchors();

                OnResized?.Invoke(
                    LocalTransform.X,
                    LocalTransform.Y,
                    LocalTransform.Width,
                    LocalTransform.Height
                );

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
                CalculateHorizontalAnchors();

                OnResized?.Invoke(
                    LocalTransform.X,
                    LocalTransform.Y,
                    LocalTransform.Width,
                    LocalTransform.Height
                );

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
                CalculateVerticalAnchors();

                OnResized?.Invoke(
                    LocalTransform.X,
                    LocalTransform.Y,
                    LocalTransform.Width,
                    LocalTransform.Height
                );

                //! queue render
            }
        }

        internal void PreRender()
        {
            if (XChanged || WidthChanged) CalculateHorizontalAnchors();
            if (YChanged || HeightChanged) CalculateVerticalAnchors();

            if (Parent != null)
            {
                if (Parent.XChanged || Parent.WidthChanged)
                    ComputeHorizontalTransform();

                if (Parent.YChanged || Parent.HeightChanged)
                    ComputeVerticalTransform();
            }
            else
            {
                ComputeHorizontalTransform();
                ComputeVerticalTransform();
            }
        }

        internal void Render()
        {
            PreRender();

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

            XChanged = false;
            YChanged = false;
            WidthChanged = false;
            HeightChanged = false;
        }

        internal void DrawBase()
        {
            SKRect rect = new SKRect(
                ComputedTransform.X,
                ComputedTransform.Y,
                ComputedTransform.X + ComputedTransform.Width,
                ComputedTransform.Y + ComputedTransform.Height
            );

            SKRoundRect roundRect = new SKRoundRect(rect, Roundness);

            // if (Parent != null)
            // {
            //     SKRect parentRect = new SKRect(
            //         Parent.ComputedTransform.X,
            //         Parent.ComputedTransform.Y,
            //         Parent.ComputedTransform.X + Parent.ComputedTransform.Width,
            //         Parent.ComputedTransform.Y + Parent.ComputedTransform.Height
            //     );

            //     SKRoundRect parentRoundRect = new SKRoundRect(parentRect, Parent.Roundness);

            //     Renderer.Canvas.ClipRoundRect(roundRect, SKClipOperation.Intersect, true);
            // }

            SKPaint paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = BackColor,
                IsAntialias = true,
            };

            if (BorderWidth > 0)
            {
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = BorderWidth;
                paint.Color = BorderColor;
            }

            Renderer.Canvas.DrawRoundRect(roundRect, paint);
        }

        private float TextWidth = 0;
        private Silk.NET.Maths.Rectangle<float> TextBounds;
        private void CalculateTextBounds()
        {
            // if (Browser.IsLoaded)
            //     TextWidth = Renderer.Pipe.TextBounds(0, 0, Text, out TextBounds);
        }

        internal void DrawText()
        {
            var cx = ComputedTransform.X;
            var cy = ComputedTransform.Y;
            var cw = ComputedTransform.Width;
            var ch = ComputedTransform.Height;

            var halfBorder = (BorderWidth / 2);

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

            textY += 2f;

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

            var TextPoint = new SKPoint(textX, 15);
            var TextPaint = new SKPaint()
            {
                IsAntialias = true,
                Color = FontColor,
                TextSize = FontSize,
                TextAlign = SKTextAlign.Center,
            };

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