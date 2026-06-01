using System.Numerics;
using System.Text;
using System;
using System.Collections.Generic;
using SkiaSharp;
using Blossom.Core;

namespace Blossom.Core.Visual;

public class VisualElement : IDisposable
{
    public virtual void AddedToView() { }

    public string Name { get; set; }

    #region User Interactions
    public bool HasFocus { get { return ParentView.FocusedElement == this; } }
    public bool Focusable { get; set; }
    public bool IsClickthrough { get; set; }
    private int _zIndex = 0;
    public int ZIndex
    {
        get => _zIndex;
        set
        {
            if (_zIndex != value)
            {
                _zIndex = value;
                ParentView?.MarkHierarchyDirty();
                ScheduleRender();
            }
        }
    }
    #endregion

    #region Events
    public ElementEvents Events { get; } = new();
    public Action<VisualElement> OnFocused = null!;
    public Action<VisualElement> OnFocusLost = null!;
    internal event ForDispose OnDisposing = null!;
    internal event Action<VisualElement, Transform> TransformChanged = null!;
    #endregion

    #region Rendering Related
    private readonly SKPaint paint = new();
    public Visibility ComputedVisibility { get; private set; }
    public SKRoundRect ComputedClipping { get; private set; } = null!;
    private bool _hasClippingAncestors = false;

    internal bool _visibilityClippingDirty = true;
    internal void MarkVisibilityClippingDirty()
    {
        if (_visibilityClippingDirty) return;
        _visibilityClippingDirty = true;
        foreach (var child in Children)
        {
            child?.MarkVisibilityClippingDirty();
        }
    }

    internal SKBitmap CachedRender = null!;
    internal bool HasCachedRender { get => CachedRender != null; }
    internal int CachedNestedElementCount = 0;

    private bool _IsDirty = false;
    private SKRect _lastRenderBounds = SKRect.Empty;
    private SKRect _cachedRenderBounds;
    internal bool _renderBoundsDirty = true;

    public SKRect RenderBounds
    {
        get
        {
            if (_renderBoundsDirty)
            {
                _cachedRenderBounds = GetRenderBounds();
                _renderBoundsDirty = false;
            }
            return _cachedRenderBounds;
        }
    }

    internal bool IsDirty
    {
        get => _IsDirty;
        set
        {
            _IsDirty = value;
            if (value && ParentView != null)
            {
                Transform.Evaluate(); // Ensure it's up to date
                
                _renderBoundsDirty = true;
                var currentRect = RenderBounds;
                
                ParentView.AddDirtyRect(currentRect);
                
                // If we have a previous render position that differs (e.g. from a move style change), mark that too
                if (!_lastRenderBounds.IsEmpty && _lastRenderBounds != currentRect)
                {
                     ParentView.AddDirtyRect(_lastRenderBounds);
                }
                
                _lastRenderBounds = currentRect;

                ParentView.RenderRequired = true;
            }
        }
    }

    private static SKPoint3 MapPoint3D(SKMatrix44 matrix, float x, float y, float z)
    {
        float[] result = matrix.MapScalars(x, y, z, 1f);
        float w = result[3];
        if (Math.Abs(w) > 1e-6f)
        {
            return new SKPoint3(result[0] / w, result[1] / w, result[2] / w);
        }
        return new SKPoint3(result[0], result[1], result[2]);
    }

    private SKRect GetLocalCombinedBounds()
    {
        var localRect = new SKRect(0, 0, Transform.Width, Transform.Height);

        // Include text bounds if text is rendered
        if (!string.IsNullOrEmpty(Text) && Style?.Text != null)
        {
            CalculateText(); // Ensure local TextPosition is updated
            var textRect = SKRect.Create(
                TextPosition.X + TextBounds.Left,
                TextPosition.Y + TextBounds.Top,
                TextBounds.Width,
                TextBounds.Height
            );
            localRect.Union(textRect);
        }

        // Include shadow bounds if shadow is valid
        if (Style?.Shadow?.HasValidValues() == true)
        {
            var s = Style.Shadow;
            var blurX = Math.Abs(s.SpreadX) * 3f;
            var blurY = Math.Abs(s.SpreadY) * 3f;
            
            var shadowRect = new SKRect(
                s.OffsetX - blurX,
                s.OffsetY - blurY,
                Transform.Width + s.OffsetX + blurX,
                Transform.Height + s.OffsetY + blurY
            );
            localRect.Union(shadowRect);
        }

        // Include border width inflation
        if (Style?.Border?.Width > 0)
        {
            var borderInflate = Style.Border.Width + 1.5f;
            localRect.Inflate(borderInflate, borderInflate);
        }

        return localRect;
    }

    private SKRect GetRenderBounds()
    {
        var localBounds = GetLocalCombinedBounds();
        
        SKRect rect;
        if (Transform.RotationX != 0 || Transform.RotationY != 0 || Transform.RotationZ != 0 ||
            Transform.ScaleX != 1 || Transform.ScaleY != 1 || Transform.ScaleZ != 1)
        {
            var globalMatrix = Transform.GetGlobalM44();
            var p1 = MapPoint3D(globalMatrix, localBounds.Left, localBounds.Top, 0);
            var p2 = MapPoint3D(globalMatrix, localBounds.Right, localBounds.Top, 0);
            var p3 = MapPoint3D(globalMatrix, localBounds.Left, localBounds.Bottom, 0);
            var p4 = MapPoint3D(globalMatrix, localBounds.Right, localBounds.Bottom, 0);

            float minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
            float maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
            float minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
            float maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));

            rect = new SKRect(minX, minY, maxX, maxY);
        }
        else
        {
            rect = localBounds;
            rect.Offset(Transform.Computed.X, Transform.Computed.Y);
        }

        return rect;
    }

    // ... (Visible/CanRender properties unchanged)

    // ...

    private void OnTransformChanged(Transform transform)
    {
        // IsDirty setter will handle invalidation of the old _lastRenderBounds
        
        Transform.Evaluate();
        CalculateText();
        TransformChanged?.Invoke(this, transform);

        // This triggers IsDirty setter, which will mark the NEW position
        ScheduleRender();
    }

    private bool _Visible = true;
    public bool Visible
    {
        get => _Visible;
        set
        {
            if (_Visible != value)
            {
                _Visible = value;
                MarkVisibilityClippingDirty();
                ScheduleRender();
            }
        }
    }

    public bool CanRender
    {
        get
        {
            if (Parent != null)
                return Visible && ParentView.Elements.ComponentsIntersect(this, Parent);

            return Visible;
        }
    }

    #endregion

    private View _ParentView = null!;
    internal View ParentView
    {
        get => Parent != null ? Parent.ParentView : _ParentView;
        set => _ParentView = value;
    }

    public int Layer
    {
        get => Parent != null ? Parent.Layer + 1 : 0;
    }

    internal ElementTree ChildElements { get; } = new();
    private VisualElement _Parent = null!;

    private VisualElement _RootParent = null!;
    public VisualElement RootParent
    {
        get => _RootParent ?? this;
        private set => _RootParent = value;
    }

    public VisualElement Parent
    {
        get => _Parent;
        set
        {
            if (_Parent != null)
                _Parent.TransformChanged -= ParentTransformChanged;

            _Parent = value;

            Transform.Parent = value.Transform;
            _Parent.TransformChanged += ParentTransformChanged;

            RootParent = value != null ? _Parent.RootParent : null;

            ScheduleRender();
        }
    }

    internal VisualElement[] Children { get => ChildElements.Items; }

    internal bool TransformIsChanged = false;
    internal SKPoint TextPosition;

    private Transform _Transform = new();
    public Transform Transform
    {
        get => _Transform;
        set
        {
            // Detach old
            if (_Transform != null)
                _Transform.OnChanged -= OnTransformChanged;

            // Attach new
            _Transform = value;
            _Transform.OnChanged += OnTransformChanged;

            _Transform.ParentElement = this;
        }
    }

    private ElementStyle _Style;
    public ElementStyle Style
    {
        get
        {
            if (_Style == null)
            {
                _Style = new ElementStyle();
                _Style.AssignElement(this);
            }
            return _Style;
        }
        set
        {
            _Style = value;
            _Style.AssignElement(this);
        }
    }

    private bool _IsClipping = false;
    public bool IsClipping
    {
        get => _IsClipping;
        set
        {
            if (_IsClipping != value)
            {
                _IsClipping = value;
                MarkVisibilityClippingDirty();
                ScheduleRender();
            }
        }
    }

    public void AddChild(VisualElement child)
    {
        child.Parent = this;
        ChildElements.AddElement(ref child);
        
        if (ParentView != null)
        {
            RegisterSubtree(child, ParentView);
            ParentView.MarkHierarchyDirty();
        }
    }

    private void RegisterSubtree(VisualElement element, View view)
    {
        view.TrackElement(ref element);
        
        // Recursively track children
        var children = element.Children;
        for (int i = 0; i < children.Length; i++)
        {
            var c = children[i];
            if (c != null)
            {
                RegisterSubtree(c, view);
            }
        }
    }

    public void RemoveChild(VisualElement child)
    {
        child.Parent = null!;
        ChildElements.RemoveElement(child);
        ParentView.UntrackElement(ref child);
        ParentView.MarkHierarchyDirty();
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

    internal int Render(SKCanvas renderTarget)
    {
        RenderSingle(renderTarget);
        return 0;
    }

    internal void RenderSingle(SKCanvas targetCanvas)
    {
        if (ParentView == null) return;

        // Ensure commands are recorded in the ledger
        if (IsDirty || ParentView.Ledger.GetCommands(Name) == null)
        {
            RecordDrawCommands(ParentView.Ledger);
            _IsDirty = false;
        }

        var cmds = ParentView.Ledger.GetCommands(Name);
        if (cmds == null) return;

        using (new SKAutoCanvasRestore(targetCanvas))
        {
            if (_hasClippingAncestors)
            {
                ApplyClippingHierarchy(targetCanvas);
            }

            var globalMatrix3D = Transform.GetGlobalM44();
            var globalMatrix2D = globalMatrix3D.Matrix;
            targetCanvas.Concat(ref globalMatrix2D);

            for (int i = 0; i < cmds.Count; i++)
            {
                cmds[i].Execute(targetCanvas);
            }
        }
    }

    private SKRoundRect? _cachedRoundRect;
    private SKRect _cachedRoundRectBounds;
    private float _cachedR1, _cachedR2, _cachedR3, _cachedR4;

    internal SKRoundRect GetOrCreateRoundRect()
    {
        var rect = new SKRect(
            0,
            0,
            Transform.Computed.Width,
            Transform.Computed.Height
        );
        // Translate the local bounds to global canvas space for clipping
        rect.Offset(Transform.Computed.X, Transform.Computed.Y);

        float r1 = Style?.Border?.RoundnessTopLeft ?? 0;
        float r2 = Style?.Border?.RoundnessTopRight ?? 0;
        float r3 = Style?.Border?.RoundnessBottomRight ?? 0;
        float r4 = Style?.Border?.RoundnessBottomLeft ?? 0;

        if (_cachedRoundRect == null || 
            _cachedRoundRectBounds != rect || 
            _cachedR1 != r1 || _cachedR2 != r2 || _cachedR3 != r3 || _cachedR4 != r4)
        {
            _cachedRoundRect?.Dispose();
            _cachedRoundRect = new SKRoundRect(rect);
            _cachedRoundRect.SetRectRadii(rect, new SKPoint[] {
                new(r1, r1),
                new(r2, r2),
                new(r3, r3),
                new(r4, r4)
            });
            _cachedRoundRectBounds = rect;
            _cachedR1 = r1;
            _cachedR2 = r2;
            _cachedR3 = r3;
            _cachedR4 = r4;
        }

        return _cachedRoundRect;
    }

    internal SKRoundRect GetLocalRoundRect()
    {
        var rect = new SKRect(
            0,
            0,
            Transform.Width,
            Transform.Height
        );
        float r1 = Style?.Border?.RoundnessTopLeft ?? 0;
        float r2 = Style?.Border?.RoundnessTopRight ?? 0;
        float r3 = Style?.Border?.RoundnessBottomRight ?? 0;
        float r4 = Style?.Border?.RoundnessBottomLeft ?? 0;
        var roundRect = new SKRoundRect(rect);
        roundRect.SetRectRadii(rect, new SKPoint[] {
            new(r1, r1),
            new(r2, r2),
            new(r3, r3),
            new(r4, r4)
        });
        return roundRect;
    }

    private void ApplyClippingHierarchy(SKCanvas canvas)
    {
        if (!_hasClippingAncestors) return;
        var ancestor = Parent;
        while (ancestor != null)
        {
            if (ancestor.IsClipping)
            {
                using var localRoundRect = ancestor.GetLocalRoundRect();
                using var path = new SKPath();
                path.AddRoundRect(localRoundRect);
                
                var globalMatrix3D = ancestor.Transform.GetGlobalM44();
                var matrix2D = globalMatrix3D.Matrix;
                path.Transform(matrix2D);
                
                canvas.ClipPath(path, SKClipOperation.Intersect, true);
            }
            ancestor = ancestor.Parent;
        }
    }

    public virtual void RecordDrawCommands(CommandLedger ledger)
    {
        var cmds = new List<DrawCommand>();

        // Local boundaries relative to element origin (0, 0)
        var rect = new SKRect(0, 0, Transform.Computed.Width, Transform.Computed.Height);

        // 1. Draw Shadow
        if (Style?.Shadow?.HasValidValues() == true)
        {
            var paint = Style.Shadow.Paint.Clone();
            paint.PathEffect = Style.BackgroundPathEffect;
            
            cmds.Add(new DrawRoundRectCommand(
                rect,
                Style.Border?.RoundnessTopLeft ?? 0,
                Style.Border?.RoundnessTopRight ?? 0,
                Style.Border?.RoundnessBottomRight ?? 0,
                Style.Border?.RoundnessBottomLeft ?? 0,
                paint
            ));
        }

        // 2. Draw Fill
        if ((Style != null && Style.BackColor.Alpha > 0) || (Style != null && Style.BackgroundPathEffect != null))
        {
            var fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                Color = Style.BackColor,
                PathEffect = Style.BackgroundPathEffect
            };
            cmds.Add(new DrawRoundRectCommand(
                rect,
                Style?.Border?.RoundnessTopLeft ?? 0,
                Style?.Border?.RoundnessTopRight ?? 0,
                Style?.Border?.RoundnessBottomRight ?? 0,
                Style?.Border?.RoundnessBottomLeft ?? 0,
                fillPaint
            ));
        }

        // 3. Draw Stroke/Border
        if (Style?.Border?.Width > 0 && Style.Border.Color.Alpha > 0)
        {
            var strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeWidth = Style.Border.Width,
                Color = Style.Border.Color,
                PathEffect = Style.Border.PathEffect
            };
            
            var borderRect = rect;
            borderRect.Inflate(Style.Border.Width / 2f, Style.Border.Width / 2f);
            
            cmds.Add(new DrawRoundRectCommand(
                borderRect,
                Style.Border.RoundnessTopLeft,
                Style.Border.RoundnessTopRight,
                Style.Border.RoundnessBottomRight,
                Style.Border.RoundnessBottomLeft,
                strokePaint
            ));
        }

        // 4. Draw Text
        if (!string.IsNullOrEmpty(Text) && Style?.Text != null)
        {
            CalculateText(); // calculates local TextPosition
            cmds.Add(new DrawTextCommand(Text, TextPosition, Style.Text.Paint));
        }

        ledger.Record(Name, cmds);
    }

    private SKRect TextBounds;
    private void CalculateTextBounds()
    {
        if (Browser.IsLoaded && Style?.Text?.Paint != null && Text.Length > 0)
            Style.Text.Paint.MeasureText(Text, ref TextBounds);
    }

    internal void CalculateText()
    {
        if (Style?.Text == null || Style.Text.Paint == null)
            return;

        // Local coordinate space (origin at 0, 0)
        var cx = 0f;
        var cy = 0f;
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

    public void EvaluateVisibilityAndClipping()
    {
        if (!Visible)
        {
            ComputedVisibility = Visibility.Hidden;
            _hasClippingAncestors = false;
            return;
        }

        SKRect? clipRect = null;
        var ancestor = Parent;
        _hasClippingAncestors = false;
        while (ancestor != null)
        {
            if (!ancestor.Visible)
            {
                ComputedVisibility = Visibility.Hidden;
                return;
            }

            if (ancestor.IsClipping)
            {
                _hasClippingAncestors = true;
                var bounds = new SKRect(
                    ancestor.Transform.Computed.X,
                    ancestor.Transform.Computed.Y,
                    ancestor.Transform.Computed.X + ancestor.Transform.Computed.Width,
                    ancestor.Transform.Computed.Y + ancestor.Transform.Computed.Height
                );
                
                if (clipRect.HasValue)
                {
                    var intersect = SKRect.Intersect(clipRect.Value, bounds);
                    if (intersect.Width <= 0 || intersect.Height <= 0)
                    {
                        ComputedVisibility = Visibility.Hidden;
                        return;
                    }
                    clipRect = intersect;
                }
                else
                {
                    clipRect = bounds;
                }
            }
            ancestor = ancestor.Parent;
        }

        if (clipRect.HasValue)
        {
            var myBounds = new SKRect(
                Transform.Computed.X,
                Transform.Computed.Y,
                Transform.Computed.X + Transform.Computed.Width,
                Transform.Computed.Y + Transform.Computed.Height
            );
            
            var intersect = SKRect.Intersect(clipRect.Value, myBounds);
            if (intersect.Width <= 0 || intersect.Height <= 0)
            {
                ComputedVisibility = Visibility.Hidden;
            }
            else if (clipRect.Value.Contains(myBounds))
            {
                ComputedVisibility = Visibility.Visible;
            }
            else
            {
                ComputedVisibility = Visibility.Clipped;
                ComputedClipping = new SKRoundRect(clipRect.Value);
            }
        }
        else
        {
            ComputedVisibility = Visibility.Visible;
        }
    }

    private void ParentTransformChanged(VisualElement e, Transform t)
    {
        Transform.Evaluate();
        CalculateText();

        ScheduleRender();
    }

    internal void ScheduleRender()
    {
        IsDirty = true;

        if (ParentView is not null)
            ParentView.RenderRequired = true;
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

    public void Dispose()
    {
        paint.Dispose();
        _cachedRoundRect?.Dispose();
        OnDisposing?.Invoke(this);

        ParentView.Elements.RemoveElement(this);

        foreach (var Child in Children)
        {
            Child.Dispose();
        }

        Parent?.ChildElements.RemoveElement(this);
    }
}

public enum Visibility
{
    Visible,
    Clipped,
    Hidden
}