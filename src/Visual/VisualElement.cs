using System.Threading.Tasks.Dataflow;
namespace Rux.Core.Visual;
using System;
using System.Numerics;
using System.Collections.Generic;
using Rux.Core.Delegates.Common;
using SkiaSharp;

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
            if (_Parent != null)
                _Parent.TransformChanged -= ParentTransformChanged;

            _Parent = value;
            Transform.Parent = value.Transform;
            value.TransformChanged += ParentTransformChanged;
        }
    }

    public List<VisualElement> Children { get; set; } = new List<VisualElement>();

    internal event ForDispose OnDisposing;
    internal event Action<Transform> TransformChanged;

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
        this.Transform.Evaluate();

        SKRoundRect prect = null;
        if (Parent != null && Transform.Computed.RectF.IntersectsWith(Parent.Transform.Computed.RectF))
        {
            prect = new SKRoundRect(
                new SKRect(
                    Parent.Transform.Computed.X,
                    Parent.Transform.Computed.Y,
                    Parent.Transform.Computed.X + Parent.Transform.Computed.Width,
                    Parent.Transform.Computed.Y + Parent.Transform.Computed.Height
                ),
                Style.Border.Roundness, Style.Border.Roundness
            );

            using (new SKAutoCanvasRestore(Renderer.Canvas))
            {
                Renderer.Canvas.ClipRoundRect(prect, SKClipOperation.Intersect, true);
                DrawBase();
                DrawText();
            }
        }
        else
        {
            DrawBase();
            DrawText();
        }

        foreach (var child in Children)
        {
            child.Render();
        }
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

        roundRect.SetRectRadii(rect, new SKPoint[] {
                new SKPoint(Style.Border.Roundness, Style.Border.Roundness),
                new SKPoint(Style.Border.Roundness,Style.Border.Roundness),
                new SKPoint(Style.Border.Roundness,Style.Border.Roundness),
                new SKPoint(Style.Border.Roundness,Style.Border.Roundness),
            });

        paint.Style = SKPaintStyle.Fill;
        paint.Color = Style.BackColor;
        paint.IsAntialias = true;

        Renderer.Canvas.DrawRoundRect(roundRect, paint);

        if (Style.Border.Width > 0)
        {
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = Style.Border.Width;
            paint.Color = Style.Border.Color;
        }

        Renderer.Canvas.DrawRoundRect(roundRect, paint);
    }

    private SKRect TextBounds;
    private void CalculateTextBounds()
    {
        if (Browser.IsLoaded)
            TextPaint.MeasureText(Text, ref TextBounds);
    }

    SKPaint TextPaint = new SKPaint()
    {
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = SKTypeface.FromFamilyName("DejaVu Sans Mono", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
    };

    SKPoint TextPoint;

    private float textX = 0;
    private float textY = 0;

    internal void CalculateText()
    {
        if (Style.Text == null)
            return;

        TextPaint.TextSize = Style.Text.Size;
        TextPaint.Color = Style.Text.Color;

        var cx = Transform.Computed.X;
        var cy = Transform.Computed.Y;
        var cw = Transform.Computed.Width;
        var ch = Transform.Computed.Height;

        CalculateTextBounds();

        textX = Style.Text.Alignment switch
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

        textY = Style.Text.Alignment switch
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
            _ => cy + (ch / 2f) - TextBounds.MidY // Center
        };

        TextPoint = new SKPoint(textX, textY);
    }

    private void ChangedTransform(Transform x)
    {
        this.Transform.Evaluate();
        CalculateText();
        TransformChanged.Invoke(x);
    }

    private void ParentTransformChanged(Transform _)
    {
        this.Transform.Evaluate();
        CalculateText();
    }

    internal void DrawText()
    {
        // Early return if there's no text or color
        if (string.IsNullOrEmpty(Text) || Style.Text is null)
            return;

        // if (Style.Text.HasChanged)
        CalculateText();

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

    internal void ScheduleRender()
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