using System.Numerics;
using System.Text;
using System;
using SkiaSharp;
using Blossom.Core.Visual;
using System.Diagnostics.CodeAnalysis;

namespace Blossom.Core.Visual;

public class VisualElement : IDisposable
{
    public virtual void AddedToView() { }

    public string Name { get; set; }
    public bool HasFocus { get { return ParentView.FocusedElement == this; } }
    public bool IsClickthrough { get; set; } = false;
    public ElementEvents Events { get; } = new();

    internal Application ParentApplication { get; set; }
    internal View ParentView { get; set; }
    private ElementTree ChildElements { get; } = new();

    private VisualElement _Parent;
    private readonly SKPaint paint = new SKPaint();
    private readonly SKPaint shadowPaint = new SKPaint();

    public VisualElement Parent
    {
        get => _Parent;
        set
        {
            Console.WriteLine($"Parent '{value.Name}' added to '{this.Name}'");
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

    public void AddChild(VisualElement child)
    {
        child.Parent = this;
        ChildElements.AddElement(ref child, ParentView);
    }

    public void RemoveChild(VisualElement child)
    {
        child.Parent = null;
        ChildElements.RemoveElement(child);
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
        this.Transform.Evaluate();

        bool isWithin = true;

        if (Parent != null)
            isWithin = Parent.Transform.Computed.RectF.Contains(Transform.Computed.RectF) || Transform.Computed.RectF.IntersectsWith(Parent.Transform.Computed.RectF);

        if (Parent?.Style.IsClipping == true)
        {
            if (!isWithin) return;

            WithClipping(() =>
            {
                DrawBase();
                DrawText();
            });
        }
        else
        {
            using (new SKAutoCanvasRestore(Renderer.Canvas))
            {
                DrawBase();
                DrawText();
            }
        }

        foreach (var child in Children)
        {
            child.Render();
        }
    }

    private void WithClipping(Action action)
    {
        var prect = new SKRoundRect(
                new SKRect(
                    Parent.Transform.Computed.X + 1,
                    Parent.Transform.Computed.Y + 1,
                    Parent.Transform.Computed.X + Parent.Transform.Computed.Width - 2,
                    Parent.Transform.Computed.Y + Parent.Transform.Computed.Height - 2
                ),
                Style.Border.Roundness, Style.Border.Roundness
            );

        Renderer.Canvas.ClipRoundRect(prect, SKClipOperation.Intersect, true);

        using (new SKAutoCanvasRestore(Renderer.Canvas))
        {
            action();
        }
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
            shadowPaint.Color = Style.BackColor;
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

            if (this.Style.Border.PathEffect != null)
                paint.PathEffect = this.Style.Border.PathEffect;

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
        // Early return if there's no text or color
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

        // TextPosition.Y *= 0.995f;

        // TextPosition.Y += 2;
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
        Vector2 relative = new Vector2(
            x - Transform.Computed.X,
            y - Transform.Computed.Y
        );

        return relative;
    }

    public void Dispose()
    {
        //Parent.OnComputedSize -= HandleParentResized;
        OnDisposing?.Invoke(this);

        if (Parent != null)
            Parent.ChildElements.RemoveElement(this);

        foreach (var Child in Children)
        {
            Child.Dispose();
        }
    }
}