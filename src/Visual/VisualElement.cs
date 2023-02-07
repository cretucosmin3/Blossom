using System.Numerics;
using System.Text;
using System;
using SkiaSharp;
using Blossom.Core.Visual;
using System.Diagnostics.CodeAnalysis;

namespace Blossom.Core.Visual;

public enum Visibility
{
    Visible,
    Clipped,
    Hidden
}

public class VisualElement : IDisposable
{
    public virtual void AddedToView() { }

    public string Name { get; set; }
    public bool HasFocus { get { return ParentView.FocusedElement == this; } }

    private bool _IsClickThrough = false;
    public bool IsClickthrough { get => _IsClickThrough || Style?.BackColor.Alpha == 0; set => _IsClickThrough = value; }
    public ElementEvents Events { get; } = new();

    private View _ParentView;
    internal View ParentView
    {
        get => Parent != null ? Parent.ParentView : _ParentView;
        set => _ParentView = value;
    }

    internal ElementTree ChildElements { get; } = new();

    private VisualElement _Parent;
    private readonly SKPaint paint = new();
    private readonly SKPaint shadowPaint = new();
    public Visibility ComputedVisibility { get; private set; }
    public SKRoundRect ComputedClipping { get; private set; }

    public VisualElement Parent
    {
        get => _Parent;
        set
        {
            if (_Parent != null)
                _Parent.TransformChanged -= ParentTransformChanged;

            _Parent = value;
            Transform.Parent = value.Transform;
            value.TransformChanged += ParentTransformChanged;
        }
    }

    internal VisualElement[] Children { get => ChildElements.Items; }

    internal event ForDispose OnDisposing;
    internal event Action<VisualElement, Transform> TransformChanged;
    internal SKPoint TextPosition;

    private Transform _Transform = new Transform();
    public Transform Transform
    {
        get => _Transform;
        set
        {
            // Detach old
            _Transform.OnChanged -= ChangedTransform;

            // Attach new
            _Transform = value;
            _Transform.OnChanged += ChangedTransform;
        }
    }

    private ElementStyle _Style;
    public ElementStyle Style
    {
        get => _Style;
        set
        {
            _Style = value;
            _Style.AssignElement(this);
        }
    }

    private bool _IsClipping = true;
    public bool IsClipping
    {
        get => _IsClipping;
        set
        {
            _IsClipping = value;
            ScheduleRender();
        }
    }

    public void AddChild(VisualElement child)
    {
        child.Parent = this;
        ChildElements.AddElement(ref child, ParentView);

        ParentView.TrackElement(ref child);
    }

    public void RemoveChild(VisualElement child)
    {
        child.Parent = null;
        ChildElements.RemoveElement(child);


        ParentView.UntrackElement(ref child);
    }

    public bool Visible { get; set; } = true;

    public bool CanRender
    {
        get
        {
            if (Parent != null)
                return Visible && ParentView.Elements.ComponentsIntersect(this, Parent);

            return Visible;
        }
    }

    public int Layer
    {
        get => Parent != null ? Parent.Layer + 1 : 0;
    }

    private readonly StringBuilder _Text = new("");
    public string Text
    {
        get => _Text.ToString();
        set
        {
            if (_Text.ToString() != value)
            {
                _Text.Clear();
                _Text.Append(value);

                ScheduleRender();
            }
        }
    }

    internal void Render()
    {
        Transform.Evaluate();

        ComputedVisibility = Visibility.Visible;
        bool isWithinParent = true;
        bool isInsideParent = true;
        bool isClipped = false;

        if (Parent != null)
        {
            isClipped = Parent.IsClipping;
            isInsideParent = Parent.Transform.Computed.RectF.Contains(Transform.Computed.RectF);
            isWithinParent = isInsideParent || Transform.Computed.RectF.IntersectsWith(Parent.Transform.Computed.RectF);
        }

        if (Parent?.ComputedVisibility == Visibility.Hidden || (isClipped && !isWithinParent))
        {
            ComputedVisibility = Visibility.Hidden;
            return;
        }

        if (isClipped || Parent?.ComputedVisibility == Visibility.Clipped)
        {
            ComputedVisibility = Visibility.Clipped;
            ApplyClipping();
        }

        DrawBase();
        DrawText();

        foreach (var child in Children)
        {
            child.Render();
        }
    }

    private void ApplyClipping()
    {
        var prevClipping = Parent.GetPreviousClippingRect();

        var clippingRect = Parent?.IsClipping == true ? new SKRect(
            Parent.Transform.Computed.X,
            Parent.Transform.Computed.Y,
            Parent.Transform.Computed.RectF.Right,
            Parent.Transform.Computed.RectF.Bottom
        ) : prevClipping.Rect;

        if (prevClipping?.Rect != clippingRect && prevClipping != null)
            clippingRect = SKRect.Intersect(clippingRect, prevClipping.Rect);

        var compClippingRect = new SKRoundRect(
                clippingRect,
                Parent.Style.Border.Roundness, Parent.Style.Border.Roundness
            );

        compClippingRect.SetRectRadii(clippingRect, new SKPoint[] {
            new(15,5),
            new(Parent.Style.Border.Roundness, Parent.Style.Border.Roundness),
            new(Parent.Style.Border.Roundness, Parent.Style.Border.Roundness),
            new(Parent.Style.Border.Roundness, Parent.Style.Border.Roundness),
        });

        Renderer.Canvas.ClipRoundRect(compClippingRect, SKClipOperation.Intersect, true);

        ComputedClipping = compClippingRect;
    }

    internal void DrawBase()
    {
        SKRect rect = new(
            Transform.Computed.X,
            Transform.Computed.Y,
            Transform.Computed.X + Transform.Computed.Width,
            Transform.Computed.Y + Transform.Computed.Height
        );

        SKRoundRect roundRect = new SKRoundRect();

        roundRect.SetRectRadii(rect, new SKPoint[] {
                new SKPoint(Style.Border.RoundnessTopLeft, Style.Border.RoundnessTopLeft),
                new SKPoint(Style.Border.RoundnessTopRight, Style.Border.RoundnessTopRight),
                new SKPoint(Style.Border.RoundnessBottomRight, Style.Border.RoundnessBottomRight),
                new SKPoint(Style.Border.RoundnessBottomLeft, Style.Border.RoundnessBottomLeft),
            });

        if (Style.Shadow?.HasValidValues() == true)
        {
            shadowPaint.Style = SKPaintStyle.Fill;
            shadowPaint.Color = Style.Shadow.Color;
            shadowPaint.IsAntialias = true;
            shadowPaint.ImageFilter = Style.Shadow.Filter;
            shadowPaint.PathEffect = Style.BackgroundPathEffect;
            Renderer.Canvas.DrawRoundRect(roundRect, shadowPaint);
        }

        paint.Style = SKPaintStyle.Fill;
        paint.Color = Style.BackColor;
        paint.IsAntialias = true;
        paint.PathEffect = Style.BackgroundPathEffect;

        Renderer.Canvas.DrawRoundRect(roundRect, paint);

        if (Style.Border.Width > 0)
        {
            paint.Style = SKPaintStyle.Stroke;

            if (Style.Border.PathEffect != null)
                paint.PathEffect = Style.Border.PathEffect;

            paint.StrokeWidth = Style.Border.Width;
            paint.Color = Style.Border.Color;
            // paint.Shader = SKShader.CreateRadialGradient(
            //     new SKPoint(rect.MidX, rect.MidY), rect.Width / 1.7f,
            //     new SKColor[] { SKColors.Transparent, new(0, 0, 0, 70) },
            //     SKShaderTileMode.Clamp);

            roundRect.Inflate(new SKSize(Style.Border.Width / 2f, Style.Border.Width / 2f));
            Renderer.Canvas.DrawRoundRect(roundRect, paint);

            // paint.Shader?.Dispose();
            // paint.Shader = null;
        }
    }

    private void DrawText()
    {
        if (string.IsNullOrEmpty(Text) || Style.Text is null)
            return;

        CalculateText();
        DrawTextShadow();
    }

    internal void DrawTextShadow()
    {
        Renderer.Canvas.DrawText(Text, TextPosition, Style.Text.Paint);
    }

    private SKRect TextBounds;
    private void CalculateTextBounds()
    {
        if (Browser.IsLoaded && Style != null && Text.Length > 0)
            Style.Text.Paint.MeasureText(Text, ref TextBounds);
    }

    internal void CalculateText()
    {
        if (Style.Text == null || Style.Text.Paint == null)
            return;

        var cx = Transform.Computed.X;
        var cy = Transform.Computed.Y;
        var cw = Transform.Computed.Width;
        var ch = Transform.Computed.Height;

        CalculateTextBounds();

        TextPosition.X = Style.Text.Alignment switch
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
            _ => cx + (cw / 2f) - TextBounds.MidX // Center
        };

        TextPosition.Y = Style.Text.Alignment switch
        {
            var x when
                x == TextAlign.Top ||
                x == TextAlign.TopLeft ||
                x == TextAlign.TopRight
                => cy + TextBounds.Height + Style.Text.Padding - TextBounds.Bottom,
            var x when
                x == TextAlign.Bottom ||
                x == TextAlign.BottomLeft ||
                x == TextAlign.BottomRight
                => cy + ch - Style.Text.Padding,
            _ => cy + (ch / 2f) - TextBounds.MidY // Center
        };
    }

    private void ChangedTransform(Transform transform)
    {
        this.Transform.Evaluate();
        CalculateText();
        TransformChanged?.Invoke(this, transform);
    }

    private void ParentTransformChanged(VisualElement e, Transform t)
    {
        this.Transform.Evaluate();
        CalculateText();
    }

    internal void ScheduleRender()
    {
        Browser.BrowserApp.ActiveView.RenderRequired = true;
    }

    public void GetFocus()
    {
        if (ParentView != null)
            ParentView.FocusedElement = this;
    }

    public Vector2 PointToClient(float x, float y)
    {
        return new Vector2(
            x - Transform.Computed.X,
            y - Transform.Computed.Y
        );
    }

    internal SKRoundRect GetPreviousClippingRect()
    {
        if (ComputedVisibility == Visibility.Clipped)
            return ComputedClipping;

        if (Parent != null)
            return Parent.GetPreviousClippingRect();

        return null;
    }

    public void Dispose()
    {
        OnDisposing?.Invoke(this);

        ParentView.Elements.RemoveElement(this);

        foreach (var Child in Children)
        {
            Child.Dispose();
        }

        if (Parent != null)
            Parent.ChildElements.RemoveElement(this);
    }
}