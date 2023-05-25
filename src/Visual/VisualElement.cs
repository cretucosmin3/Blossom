using System.Numerics;
using System.Text;
using System;
using SkiaSharp;

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

    public bool IsClickthrough { get; set; } = false;

    public ElementEvents Events { get; } = new();

    #region Render

    internal SKPicture CachedRender;
    internal bool HasCachedRender { get => CachedRender != null; }
    internal bool IsDirty { get; set; } = false;

    internal void MarkTreePathDirty(int nestedElements = 0)
    {
        if (IsDirty) return;

        // Destroy any cached render
        CachedRender?.Dispose();

        // Calculate nested elements
        TotalNestedElements = nestedElements + Children.Length;

        IsDirty = true;
        Parent?.MarkTreePathDirty(TotalNestedElements);
    }

    #endregion

    private View _ParentView;
    internal View ParentView
    {
        get => Parent != null ? Parent.ParentView : _ParentView;
        set => _ParentView = value;
    }

    public int Layer
    {
        get => Parent != null ? Parent.Layer + 1 : 0;
    }

    public int TotalNestedElements { get; private set; } = 0;

    internal ElementTree ChildElements { get; } = new();

    public VisualElement RootParent
    {
        get
        {
            if (Parent == null)
                return this;

            return Parent.RootParent;
        }
    }

    private VisualElement _Parent;
    private readonly SKPaint paint = new();
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

            ScheduleRender();
        }
    }

    internal VisualElement[] Children { get => ChildElements.Items; }

    internal event ForDispose OnDisposing;
    internal event Action<VisualElement, Transform> TransformChanged;
    internal SKPoint TextPosition;

    private Transform _Transform = new();
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

            _Transform.ParentElement = this;
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
        ChildElements.AddElement(ref child);
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

    public Rect BoundingRect
    {
        get
        {
            if (Children.Length == 0)
                return Transform.Computed;

            var elementsRect = ChildElements.BoundAxis.GetBoundingRect();

            return Rect.Max(elementsRect, Transform.Computed);
        }
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

    internal void Render(SKCanvas targetCanvas)
    {
        Transform.Evaluate();

        ComputedVisibility = Visibility.Visible;
        bool isWithinParent = true;
        bool isClipped = false;

        if (Parent != null)
        {
            isClipped = Parent.IsClipping;
            bool isInsideParent = Parent.Transform.Computed.RectF.Contains(Transform.Computed.RectF);
            isWithinParent = isInsideParent || Transform.Computed.RectF.IntersectsWith(Parent.Transform.Computed.RectF);
        }

        if (Parent?.ComputedVisibility == Visibility.Hidden || (isClipped && !isWithinParent))
        {
            ComputedVisibility = Visibility.Hidden;
            return;
        }

        using (new SKAutoCanvasRestore(targetCanvas))
        {
            if (isClipped || Parent?.ComputedVisibility == Visibility.Clipped)
            {
                ComputedVisibility = Visibility.Clipped;
                ApplyClipping(targetCanvas);
            }

            DrawBase(targetCanvas);
            DrawText(targetCanvas);
        }
    }

    private void ApplyClipping(SKCanvas targetCanvas)
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

        // compClippingRect.SetRectRadii(clippingRect, new SKPoint[] {
        //     new(15,5),
        //     new(Parent.Style.Border.Roundness, Parent.Style.Border.Roundness),
        //     new(Parent.Style.Border.Roundness, Parent.Style.Border.Roundness),
        //     new(Parent.Style.Border.Roundness, Parent.Style.Border.Roundness),
        // });

        targetCanvas.ClipRoundRect(compClippingRect, SKClipOperation.Intersect, true);

        ComputedClipping = compClippingRect;
    }

    internal void DrawBase(SKCanvas targetCanvas)
    {
        SKRect rect = new(
            Transform.Computed.X,
            Transform.Computed.Y,
            Transform.Computed.X + Transform.Computed.Width,
            Transform.Computed.Y + Transform.Computed.Height
        );

        SKRoundRect roundRect = new();

        if (Style.Border != null)
        {
            roundRect.SetRectRadii(rect, new SKPoint[] {
                new SKPoint(Style.Border.RoundnessTopLeft, Style.Border.RoundnessTopLeft),
                new SKPoint(Style.Border.RoundnessTopRight, Style.Border.RoundnessTopRight),
                new SKPoint(Style.Border.RoundnessBottomRight, Style.Border.RoundnessBottomRight),
                new SKPoint(Style.Border.RoundnessBottomLeft, Style.Border.RoundnessBottomLeft),
            });
        }

        if (Style.Shadow?.HasValidValues() == true)
        {
            Style.Shadow.Paint.PathEffect = Style.BackgroundPathEffect;
            targetCanvas.DrawRoundRect(roundRect, Style.Shadow.Paint);
        }

        paint.Style = SKPaintStyle.Fill;
        paint.Color = Style.BackColor;
        paint.IsAntialias = true;
        paint.PathEffect = Style.BackgroundPathEffect;

        targetCanvas.DrawRoundRect(roundRect, paint);

        if (Style.Border?.Width > 0)
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
            targetCanvas.DrawRoundRect(roundRect, paint);

            // paint.Shader?.Dispose();
            // paint.Shader = null;
        }

        roundRect.Dispose();
    }

    private void DrawText(SKCanvas targetCanvas)
    {
        if (string.IsNullOrEmpty(Text) || Style.Text is null)
            return;

        CalculateText();
        DrawTextShadow(targetCanvas);
    }

    internal void DrawTextShadow(SKCanvas targetCanvas)
    {
        targetCanvas.DrawText(Text, TextPosition, Style.Text.Paint);
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
        Transform.Evaluate();
        CalculateText();
        TransformChanged?.Invoke(this, transform);

        ScheduleRender();
    }

    private void ParentTransformChanged(VisualElement e, Transform t)
    {
        Transform.Evaluate();
        CalculateText();
        ScheduleRender();
    }

    internal void ScheduleRender()
    {
        MarkTreePathDirty();

        if (ParentView != null)
            ParentView.RenderRequired = true;

        if (Children.Length > 0)
        {
            foreach (var child in Children)
            {
                child.ScheduleRender();
            }
        }
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
        paint.Dispose();
        OnDisposing?.Invoke(this);

        ParentView.Elements.RemoveElement(this);

        foreach (var Child in Children)
        {
            Child.Dispose();
        }

        Parent?.ChildElements.RemoveElement(this);
    }
}