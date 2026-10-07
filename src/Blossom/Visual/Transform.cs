using System;
using System.Numerics;
using SkiaSharp;
using Blossom.Core;
using Blossom.Core.Design;
using Blossom.Core.Visual.Enums;

namespace Blossom.Core.Visual;

public class Transform : IDisposable
{
    private SKMatrix44 _cachedLocalM44 = SKMatrix44.Identity;
    private SKMatrix44 _cachedGlobalM44 = SKMatrix44.Identity;
    internal bool _matrixDirty = true;

    private float _rotationX = 0f;
    private float _rotationY = 0f;
    private float _rotationZ = 0f;
    private float _scaleX = 1f;
    private float _scaleY = 1f;
    private float _scaleZ = 1f;
    private float _perspective = 0f;
    private float _originX = 0.5f;
    private float _originY = 0.5f;
    private bool _has3DTransforms = false;
    public bool Has3DTransforms => _has3DTransforms;

    private void UpdateHas3DTransforms()
    {
        _has3DTransforms = _rotationX != 0f || _rotationY != 0f || _rotationZ != 0f || _scaleX != 1f || _scaleY != 1f || _scaleZ != 1f || _perspective != 0f;
    }

    public float RotationX
    {
        get => _rotationX;
        set { if (_rotationX != value) { _rotationX = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.InvalidateSubtreeBounds(); ParentElement?.ScheduleRender(); } }
    }
    public float RotationY
    {
        get => _rotationY;
        set { if (_rotationY != value) { _rotationY = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.InvalidateSubtreeBounds(); ParentElement?.ScheduleRender(); } }
    }
    public float RotationZ
    {
        get => _rotationZ;
        set { if (_rotationZ != value) { _rotationZ = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.InvalidateSubtreeBounds(); ParentElement?.ScheduleRender(); } }
    }
    public float ScaleX
    {
        get => _scaleX;
        set { if (_scaleX != value) { _scaleX = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.InvalidateSubtreeBounds(); ParentElement?.ScheduleRender(); } }
    }
    public float ScaleY
    {
        get => _scaleY;
        set { if (_scaleY != value) { _scaleY = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.InvalidateSubtreeBounds(); ParentElement?.ScheduleRender(); } }
    }
    public float ScaleZ
    {
        get => _scaleZ;
        set { if (_scaleZ != value) { _scaleZ = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.InvalidateSubtreeBounds(); ParentElement?.ScheduleRender(); } }
    }
    public float Perspective
    {
        get => _perspective;
        set { if (_perspective != value) { _perspective = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.InvalidateSubtreeBounds(); ParentElement?.ScheduleRender(); } }
    }
    public float TransformOriginX
    {
        get => _originX;
        set { if (_originX != value) { _originX = value; _matrixDirty = true; ParentElement?.InvalidateSubtreeBounds(); ParentElement?.ScheduleRender(); } }
    }
    public float TransformOriginY
    {
        get => _originY;
        set { if (_originY != value) { _originY = value; _matrixDirty = true; ParentElement?.InvalidateSubtreeBounds(); ParentElement?.ScheduleRender(); } }
    }

    public SKMatrix44 GetLocalM44()
    {
        if (_matrixDirty)
        {
            var m = SKMatrix44.Identity;

            float localX = AbsoluteX - (Parent != null ? Parent.AbsoluteX : 0);
            float localY = AbsoluteY - (Parent != null ? Parent.AbsoluteY : 0);

            // Skia 4 SKMatrix44 is System.Numerics.Matrix4x4 (row-vector).
            // PostConcat(A) => A * this, which keeps transform-origin at the layout center.
            m = m.PostConcat(SKMatrix44.CreateTranslation(localX, localY, 0));

            float originPxX = Width * TransformOriginX;
            float originPxY = Height * TransformOriginY;
            m = m.PostConcat(SKMatrix44.CreateTranslation(originPxX, originPxY, 0));

            if (Perspective > 0)
            {
                var persp = SKMatrix44.Identity;
                // Last column so Vector4/Skia divide by w' = 1 - z/perspective.
                persp[2, 3] = -1f / Perspective;
                m = m.PostConcat(persp);
            }

            if (RotationX != 0)
                m = m.PostConcat(SKMatrix44.CreateRotationDegrees(1f, 0f, 0f, RotationX));
            if (RotationY != 0)
                m = m.PostConcat(SKMatrix44.CreateRotationDegrees(0f, 1f, 0f, RotationY));
            if (RotationZ != 0)
                m = m.PostConcat(SKMatrix44.CreateRotationDegrees(0f, 0f, 1f, RotationZ));

            if (ScaleX != 1 || ScaleY != 1 || ScaleZ != 1)
                m = m.PostConcat(SKMatrix44.CreateScale(ScaleX, ScaleY, ScaleZ));

            m = m.PostConcat(SKMatrix44.CreateTranslation(-originPxX, -originPxY, 0));

            _cachedLocalM44 = m;
            _matrixDirty = false;
        }
        return _cachedLocalM44;
    }

    public SKMatrix44 GetGlobalM44()
    {
        var local = GetLocalM44();
        if (Parent != null)
        {
            // p_world = p_local * local * parent
            _cachedGlobalM44 = local * Parent.GetGlobalM44();
            return _cachedGlobalM44;
        }

        return local;
    }

    /// <summary>
    /// Maps a local point through a 4x4, including perspective divide when w != 1.
    /// </summary>
    public static SKPoint3 MapPoint3D(SKMatrix44 matrix, float x, float y, float z = 0f)
    {
        var v = Vector4.Transform(new Vector4(x, y, z, 1f), matrix);
        if (v.W != 1f && Math.Abs(v.W) > 1e-8f)
            return new SKPoint3(v.X / v.W, v.Y / v.W, v.Z / v.W);
        return new SKPoint3(v.X, v.Y, v.Z);
    }

    public static SKPoint MapPoint(SKMatrix44 matrix, float x, float y)
    {
        var p = MapPoint3D(matrix, x, y, 0f);
        return new SKPoint(p.X, p.Y);
    }

    /// <summary>
    /// Unprojects a screen point onto the element's local z=0 plane.
    /// Invert+MapPoint(z=0) is wrong after rotateX/Y: the projected point does not lie at z=0.
    /// </summary>
    public static bool TryUnproject(SKMatrix44 matrix, float screenX, float screenY, out float localX, out float localY)
    {
        // Row-vector: (lx, ly, 0, 1) * M, then divide by w.
        float m00 = matrix[0, 0], m01 = matrix[0, 1], m03 = matrix[0, 3];
        float m10 = matrix[1, 0], m11 = matrix[1, 1], m13 = matrix[1, 3];
        float m30 = matrix[3, 0], m31 = matrix[3, 1], m33 = matrix[3, 3];

        float a1 = screenX * m03 - m00;
        float b1 = screenX * m13 - m10;
        float c1 = m30 - screenX * m33;

        float a2 = screenY * m03 - m01;
        float b2 = screenY * m13 - m11;
        float c2 = m31 - screenY * m33;

        float d = a1 * b2 - b1 * a2;
        if (Math.Abs(d) <= 1e-6f)
        {
            localX = 0f;
            localY = 0f;
            return false;
        }

        localX = (c1 * b2 - b1 * c2) / d;
        localY = (a1 * c2 - c1 * a2) / d;
        return true;
    }

    public static SKRect MapRect(SKMatrix44 matrix, SKRect rect)
    {
        var p1 = MapPoint3D(matrix, rect.Left, rect.Top);
        var p2 = MapPoint3D(matrix, rect.Right, rect.Top);
        var p3 = MapPoint3D(matrix, rect.Left, rect.Bottom);
        var p4 = MapPoint3D(matrix, rect.Right, rect.Bottom);
        float minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
        float maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
        float minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
        float maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));
        return new SKRect(minX, minY, maxX, maxY);
    }

    /// <summary>
    /// Maps a path through a 4x4 (SKPath.Transform is 3x3 and drops perspective).
    /// </summary>
    public static SKPath MapPath(SKPath source, SKMatrix44 matrix)
    {
        var dest = new SKPath();
        using var it = source.CreateRawIterator();
        var pts = new SKPoint[4];
        SKPathVerb verb;
        while ((verb = it.Next(pts)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    dest.MoveTo(MapPoint(matrix, pts[0].X, pts[0].Y));
                    break;
                case SKPathVerb.Line:
                    dest.LineTo(MapPoint(matrix, pts[1].X, pts[1].Y));
                    break;
                case SKPathVerb.Quad:
                    dest.QuadTo(MapPoint(matrix, pts[1].X, pts[1].Y), MapPoint(matrix, pts[2].X, pts[2].Y));
                    break;
                case SKPathVerb.Conic:
                    dest.ConicTo(MapPoint(matrix, pts[1].X, pts[1].Y), MapPoint(matrix, pts[2].X, pts[2].Y), it.ConicWeight());
                    break;
                case SKPathVerb.Cubic:
                    dest.CubicTo(
                        MapPoint(matrix, pts[1].X, pts[1].Y),
                        MapPoint(matrix, pts[2].X, pts[2].Y),
                        MapPoint(matrix, pts[3].X, pts[3].Y));
                    break;
                case SKPathVerb.Close:
                    dest.Close();
                    break;
            }
        }
        return dest;
    }

    public void Dispose()
    {
    }

    internal VisualElement ParentElement;
    internal bool HasChanged;
    private bool _anchorsInitialized = false;
    internal bool _transformDirty = true;
    private Transform _Parent = null;
    public Transform Parent
    {
        get => _Parent;
        set
        {
            _Parent = value;
            _transformDirty = true;
            _matrixDirty = true;
            SetAnchorValues();
        }
    }

    private float ParentWidth => Parent != null ? Parent.Width : (ParentElement?.ParentView != null ? ParentElement.ParentView.Width : DesignCanvas.DefaultDesignWidth);
    private float ParentHeight => Parent != null ? Parent.Height : (ParentElement?.ParentView != null ? ParentElement.ParentView.Height : DesignCanvas.DefaultDesignHeight);

    /// <summary>
    /// True when stretch anchors can be captured against a real parent or view size
    /// (not the design-canvas fallback used when the host is still 0×0 / unattached).
    /// </summary>
    private bool HasRealParentSize
    {
        get
        {
            if (Parent != null)
                return Parent.Width > 0 || Parent.Height > 0;
            var view = ParentElement?.ParentView;
            return view != null && (view.Width > 0 || view.Height > 0);
        }
    }

    internal float FixedLeft;
    internal float FixedRight;
    internal float FixedTop;
    internal float FixedBottom;

    internal float RelativeLeft;
    internal float RelativeRight;
    internal float RelativeTop;
    internal float RelativeBottom;

    private readonly Rect ComputedTransform = new(0, 0, 0, 0);
    // TODO: Change into SKRect for simplicity and to avoid casting
    public Rect Computed => ComputedTransform;
    public Rect Local { get; } = new Rect(0, 0, 0, 0);


    public bool FixedHeight { get; set; } = false;
    public bool FixedWidth { get; set; } = false;

    public bool FixedSize
    {
        set
        {
            FixedHeight = value;
            FixedWidth = value;
        }
    }

    /// <summary>
    /// Called when the transform is updated. (x, y, w, h)
    /// Suppressed while <see cref="VisualElement.LayoutMutationDepth"/> &gt; 0 to avoid layout loops.
    /// </summary>
    public Action<Transform> OnChanged;

    [BuilderProperty("Absolute X", "Layout", min: -1000f, max: 3000f, step: 1f)]
    public float AbsoluteX
    {
        get => ComputedTransform.X;
        set
        {
            if (Math.Abs(ComputedTransform.X - value) < 0.01f && !_transformDirty) return;

            // Do not copy Computed size into Local — that clobbers a Width/Height set earlier
            // in the same layout pass before Evaluate() has run.
            float parentX = Parent != null ? Parent.ComputedTransform.X : 0;
            Local.X = value - parentX;
            CalculateLeftAnchor();
            CalculateRighAnchor();

            ComputedTransform.X = value;
            CenterX = value + (ComputedTransform.Width / 2f);

            _transformDirty = true;
            _matrixDirty = true;
            ParentElement?.InvalidateSubtreeBounds();
            NotifyChangedIfOutsideLayout(invalidateLayout: false);
        }
    }

    [BuilderProperty("Absolute Y", "Layout", min: -1000f, max: 3000f, step: 1f)]
    public float AbsoluteY
    {
        get => ComputedTransform.Y;
        set
        {
            if (Math.Abs(ComputedTransform.Y - value) < 0.01f && !_transformDirty) return;

            float parentY = Parent != null ? Parent.ComputedTransform.Y : 0;
            Local.Y = value - parentY;
            CalculateTopAnchor();
            CalculateBottomAnchor();

            ComputedTransform.Y = value;
            CenterY = value + (ComputedTransform.Height / 2f);

            _transformDirty = true;
            _matrixDirty = true;
            ParentElement?.InvalidateSubtreeBounds();
            NotifyChangedIfOutsideLayout(invalidateLayout: false);
        }
    }

    [BuilderProperty("Local X", "Layout", min: -1000f, max: 3000f, step: 1f)]
    public float LocalX
    {
        get => Local.X;
        set
        {
            if (Math.Abs(Local.X - value) < 0.01f && !_transformDirty) return;
            float parentX = Parent != null ? Parent.ComputedTransform.X : 0;
            AbsoluteX = parentX + value;
        }
    }

    [BuilderProperty("Local Y", "Layout", min: -1000f, max: 3000f, step: 1f)]
    public float LocalY
    {
        get => Local.Y;
        set
        {
            if (Math.Abs(Local.Y - value) < 0.01f && !_transformDirty) return;
            float parentY = Parent != null ? Parent.ComputedTransform.Y : 0;
            AbsoluteY = parentY + value;
        }
    }

    [BuilderProperty("Width", "Layout", min: 0f, max: 3000f, step: 1f)]
    public float Width
    {
        get => ComputedTransform.Width;
        set
        {
            value = Math.Max(0, value);
            if (Math.Abs(ComputedTransform.Width - value) < 0.01f && Math.Abs(Local.Width - value) < 0.01f && !_transformDirty)
                return;

            float prevW = ComputedTransform.Width;
            float prevH = ComputedTransform.Height;

            Local.Width = value;
            CalculateLeftAnchor();
            CalculateRighAnchor();

            ComputedTransform.Width = value;
            CenterX = ComputedTransform.X + (ComputedTransform.Width / 2f);

            _transformDirty = true;
            _matrixDirty = true;
            ParentElement?.InvalidateSubtreeBounds();
            NotifyChangedIfOutsideLayout(invalidateLayout: true);
            NotifySizeChangedIfNeeded(prevW, prevH);
        }
    }

    [BuilderProperty("Height", "Layout", min: 0f, max: 3000f, step: 1f)]
    public float Height
    {
        get => ComputedTransform.Height;
        set
        {
            value = Math.Max(0, value);
            if (Math.Abs(ComputedTransform.Height - value) < 0.01f && Math.Abs(Local.Height - value) < 0.01f && !_transformDirty)
                return;

            float prevW = ComputedTransform.Width;
            float prevH = ComputedTransform.Height;

            Local.Height = value;
            CalculateTopAnchor();
            CalculateBottomAnchor();

            ComputedTransform.Height = value;
            CenterY = ComputedTransform.Y + (ComputedTransform.Height / 2f);

            _transformDirty = true;
            _matrixDirty = true;
            ParentElement?.InvalidateSubtreeBounds();
            NotifyChangedIfOutsideLayout(invalidateLayout: true);
            NotifySizeChangedIfNeeded(prevW, prevH);
        }
    }

    /// <summary>
    /// Sets absolute (parent-space / screen) frame and eagerly updates <see cref="Computed"/>
    /// so nested layout in the same pass can read a correct parent origin.
    /// Prefer this over setting AbsoluteX/AbsoluteY/Width/Height separately during layout.
    /// </summary>
    public void SetAbsoluteFrame(float absX, float absY, float width, float height)
    {
        width = Math.Max(0, width);
        height = Math.Max(0, height);

        float prevW = ComputedTransform.Width;
        float prevH = ComputedTransform.Height;

        float parentX = Parent != null ? Parent.ComputedTransform.X : 0;
        float parentY = Parent != null ? Parent.ComputedTransform.Y : 0;

        float targetLocalX = absX - parentX;
        float targetLocalY = absY - parentY;

        float scrollX = 0f;
        float scrollY = 0f;
        if (Parent?.ParentElement is ScrollContainer sc
            && ParentElement != sc.VScrollbar
            && ParentElement != sc.HScrollbar)
        {
            if (sc.OverflowX == OverflowMode.Scroll) scrollX = sc.ScrollX;
            if (sc.OverflowY == OverflowMode.Scroll) scrollY = sc.ScrollY;
        }

        float computedX = absX - scrollX;
        float computedY = absY - scrollY;

        // Skip no-ops so LayoutChildren can re-apply the same frames without scheduling another frame
        const float eps = 0.01f;
        if (_anchorsInitialized
            && Math.Abs(Local.X - targetLocalX) < eps
            && Math.Abs(Local.Y - targetLocalY) < eps
            && Math.Abs(Local.Width - width) < eps
            && Math.Abs(Local.Height - height) < eps
            && Math.Abs(ComputedTransform.X - computedX) < eps
            && Math.Abs(ComputedTransform.Y - computedY) < eps)
        {
            return;
        }

        Local.X = targetLocalX;
        Local.Y = targetLocalY;
        Local.Width = width;
        Local.Height = height;

        // Prefer left/top fixed anchors for manually placed frames
        if (_Anchor == Anchor.None)
            _Anchor = Anchor.Left | Anchor.Top;

        CalculateLeftAnchor();
        CalculateRighAnchor();
        CalculateTopAnchor();
        CalculateBottomAnchor();

        ComputedTransform.X = computedX;
        ComputedTransform.Y = computedY;
        ComputedTransform.Width = Local.Width;
        ComputedTransform.Height = Local.Height;
        CenterX = computedX + Local.Width / 2f;
        CenterY = computedY + Local.Height / 2f;

        _anchorsInitialized = true;
        _transformDirty = true;
        _matrixDirty = true;

        if (ParentElement != null)
        {
            ParentElement.InvalidateSubtreeBounds();
            ParentElement.InvalidateLayout();
            ParentElement.ClearRenderCache();
            ParentElement.MarkVisibilityClippingDirty();

            if (ParentElement.ParentView != null)
            {
                if (!ParentElement._lastRenderBounds.IsEmpty)
                {
                    ParentElement.ParentView.AddDirtyRect(ParentElement._lastRenderBounds);
                }
                var newBounds = ParentElement.RenderBounds;
                ParentElement.ParentView.AddDirtyRect(newBounds);
                ParentElement._lastRenderBounds = newBounds;
                ParentElement._isPaintDirty = true;
                ParentElement.ParentView.RenderRequired = true;
            }
        }

        // OnChanged is suppressed during LayoutChildren (would loop). SizeChanged still fires.
        NotifyChangedIfOutsideLayout(invalidateLayout: false);
        NotifySizeChangedIfNeeded(prevW, prevH);
    }

    /// <summary>
    /// Sets frame in parent-local coordinates (ignores scroll). Eagerly updates Computed using parent Computed origin.
    /// </summary>
    public void SetLocalFrame(float localX, float localY, float width, float height)
    {
        float parentX = Parent != null ? Parent.ComputedTransform.X : 0;
        float parentY = Parent != null ? Parent.ComputedTransform.Y : 0;
        SetAbsoluteFrame(parentX + localX, parentY + localY, width, height);
    }

    public float CenterX { get; private set; }
    public float CenterY { get; private set; }

    public float Left { get => AbsoluteX; }
    public float Right { get => AbsoluteX + Width; }
    public float Top { get => AbsoluteY; }
    public float Bottom { get => AbsoluteY + Height; }

    public bool ValidateOnAnchor { get; set; } = false;

    private Anchor _Anchor;
    public Anchor Anchor
    {
        get => _Anchor;
        set
        {
            _Anchor = value;

            // Before the first real Evaluate, Local is the authored frame — do not clobber it
            // with a still-zero Computed (object initializers set Anchor right after the ctor).
            if (_anchorsInitialized)
            {
                var parentX = Parent != null ? Parent.ComputedTransform.X : 0;
                var parentY = Parent != null ? Parent.ComputedTransform.Y : 0;

                Local.X = Computed.X - parentX;
                Local.Y = Computed.Y - parentY;
                Local.Width = Computed.Width;
                Local.Height = Computed.Height;
            }

            if (ValidateOnAnchor)
                SetAnchorValues();

            _transformDirty = true;
            OnChanged?.Invoke(this);
            ParentElement?.InvalidateLayout();
        }
    }

    public Transform() { }

    public Transform(float x, float y, float width, float height)
    {
        Local = new Rect(x, y, width, height);
        SetAnchorValues();
    }

    public void DetachParent()
    {
        Parent = null;
        SetAnchorValues();
    }

    internal void SetAnchorValues()
    {
        // 0×0 or design-only (no real parent/view yet): keep Local as authored and wait to reseed.
        if (!HasRealParentSize)
        {
            _anchorsInitialized = false;
            return;
        }

        bool firstRealSize = !_anchorsInitialized;

        // First real parent/view size: stretch L|R / T|B fills remaining space. Local may still
        // be a design-canvas size (e.g. 1000) captured before the window client size was known.
        if (firstRealSize)
        {
            if (_Anchor.HasFlag(Anchor.Left) && _Anchor.HasFlag(Anchor.Right))
                Local.Width = Math.Max(0, ParentWidth - Local.X);
            if (_Anchor.HasFlag(Anchor.Top) && _Anchor.HasFlag(Anchor.Bottom))
                Local.Height = Math.Max(0, ParentHeight - Local.Y);
        }
        else
        {
            if (_Anchor.HasFlag(Anchor.Left) && _Anchor.HasFlag(Anchor.Right) && Local.Width <= 0)
                Local.Width = ParentWidth;
            if (_Anchor.HasFlag(Anchor.Top) && _Anchor.HasFlag(Anchor.Bottom) && Local.Height <= 0)
                Local.Height = ParentHeight;
        }

        // Horizontal anchors.
        CalculateLeftAnchor();
        CalculateRighAnchor();

        // Vertical anchors.
        CalculateTopAnchor();
        CalculateBottomAnchor();

        ComputeHorizontalTransform();
        ComputeVerticalTransform();

        _anchorsInitialized = true;
    }

    private void CalculateLeftAnchor()
    {
        FixedLeft = Local.X;
        RelativeLeft = ParentWidth == 0 ? 0 : FixedLeft / ParentWidth;
    }

    private void CalculateRighAnchor()
    {
        FixedRight = ParentWidth - (Local.X + Local.Width);
        RelativeRight = ParentWidth == 0 ? 0 : FixedRight / ParentWidth;
    }

    private void CalculateTopAnchor()
    {
        FixedTop = Local.Y;
        RelativeTop = ParentHeight == 0 ? 0 : FixedTop / ParentHeight;
    }

    private void CalculateBottomAnchor()
    {
        FixedBottom = ParentHeight - (Local.Y + Local.Height);
        RelativeBottom = ParentHeight == 0 ? 0 : FixedBottom / ParentHeight;
    }

    private void ComputeHorizontalTransform()
    {
        float ParentWidth = Parent is not null
            ? Parent.ComputedTransform.Width
            : (ParentElement?.ParentView != null ? ParentElement.ParentView.Width : DesignCanvas.DefaultDesignWidth);

        if (_Anchor.HasFlag(Anchor.Left) && !_Anchor.HasFlag(Anchor.Right))
        {
            ComputedTransform.X = FixedLeft;
            ComputedTransform.Width = Local.Width;
        }
        else if (_Anchor.HasFlag(Anchor.Right) && !_Anchor.HasFlag(Anchor.Left))
        {
            ComputedTransform.X = ParentWidth - FixedRight - Local.Width;
            ComputedTransform.Width = Local.Width;
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
                ComputedTransform.X = centerX - (Local.Width / 2f);
                ComputedTransform.Width = Local.Width;
            }
        }

        if (ParentElement?.MinWidth != null && ComputedTransform.Width < ParentElement.MinWidth.Value)
        {
            ComputedTransform.Width = ParentElement.MinWidth.Value;
        }
        if (ParentElement?.MaxWidth != null && ComputedTransform.Width > ParentElement.MaxWidth.Value)
        {
            ComputedTransform.Width = ParentElement.MaxWidth.Value;
        }

        if (ComputedTransform.Width < 0)
        {
            ComputedTransform.Width = 0;
        }

        // Add parent X (scroll chrome is not content — do not apply scroll offset)
        float scrollX = 0f;
        if (Parent?.ParentElement is ScrollContainer sc
            && sc.OverflowX == OverflowMode.Scroll
            && ParentElement != sc.VScrollbar
            && ParentElement != sc.HScrollbar)
        {
            scrollX = sc.ScrollX;
        }
        ComputedTransform.X += Parent is null ? 0 : (Parent.ComputedTransform.X - scrollX);
    }

    private void ComputeVerticalTransform()
    {
        float ParentHeight = Parent != null
            ? Parent.Computed.Height
            : (ParentElement?.ParentView != null ? ParentElement.ParentView.Height : DesignCanvas.DefaultDesignHeight);

        bool bottomAnchored = _Anchor.HasFlag(Anchor.Bottom);
        bool topAnchored = _Anchor.HasFlag(Anchor.Top);

        if (topAnchored && !bottomAnchored)
        {
            ComputedTransform.Y = FixedTop;
            ComputedTransform.Height = Local.Height;
        }
        else if (bottomAnchored && !topAnchored)
        {
            ComputedTransform.Y = ParentHeight - FixedBottom - Local.Height;
            ComputedTransform.Height = Local.Height;
        }
        else if (topAnchored && bottomAnchored)
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
                ComputedTransform.Y = centerY - (Local.Height / 2f);
                ComputedTransform.Height = Local.Height;
            }
        }

        if (ParentElement?.MinHeight != null && ComputedTransform.Height < ParentElement.MinHeight.Value)
        {
            ComputedTransform.Height = ParentElement.MinHeight.Value;
        }
        if (ParentElement?.MaxHeight != null && ComputedTransform.Height > ParentElement.MaxHeight.Value)
        {
            ComputedTransform.Height = ParentElement.MaxHeight.Value;
        }

        if (ComputedTransform.Height < 0)
        {
            ComputedTransform.Height = 0;
        }

        // Add parent Y (scroll chrome is not content — do not apply scroll offset)
        float scrollY = 0f;
        if (Parent?.ParentElement is ScrollContainer sc2
            && sc2.OverflowY == OverflowMode.Scroll
            && ParentElement != sc2.VScrollbar
            && ParentElement != sc2.HScrollbar)
        {
            scrollY = sc2.ScrollY;
        }
        ComputedTransform.Y += Parent != null ? (Parent.ComputedTransform.Y - scrollY) : 0;
    }

    internal bool Evaluate()
    {
        if (Shell.WasResized)
        {
            _transformDirty = true;
        }

        if (!_anchorsInitialized)
        {
            if (!HasRealParentSize)
            {
                // Stay dirty so the first non-zero parent/view size reseeds stretch anchors.
                _transformDirty = true;
                return false;
            }
            SetAnchorValues();
        }

        if (!_transformDirty && _anchorsInitialized)
        {
            return false;
        }

        _matrixDirty = true;

        float prevX = Computed.X;
        float prevY = Computed.Y;
        float prevW = Computed.Width;
        float prevH = Computed.Height;

        ComputeHorizontalTransform();
        ComputeVerticalTransform();

        _transformDirty = false;

        const float eps = 0.01f;
        bool changed =
            Math.Abs(prevX - Computed.X) > eps ||
            Math.Abs(prevY - Computed.Y) > eps ||
            Math.Abs(prevW - Computed.Width) > eps ||
            Math.Abs(prevH - Computed.Height) > eps;

        if (changed)
        {
            bool sizeChanged =
                Math.Abs(prevW - Computed.Width) > eps ||
                Math.Abs(prevH - Computed.Height) > eps;

            if (ParentElement != null)
            {
                ParentElement.InvalidateSubtreeBounds();

                if (ParentElement.ParentView != null)
                {
                    if (!ParentElement._lastRenderBounds.IsEmpty)
                    {
                        ParentElement.ParentView.AddDirtyRect(ParentElement._lastRenderBounds);
                    }
                    var newBounds = ParentElement.RenderBounds;
                    ParentElement.ParentView.AddDirtyRect(newBounds);
                    ParentElement._lastRenderBounds = newBounds;
                    ParentElement._isPaintDirty = true;
                    ParentElement.ParentView.RenderRequired = true;
                }
            }

            if (sizeChanged && ParentElement != null)
            {
                // SizeChanged must fire even inside LayoutChildren; OnChanged must not (loops).
                ParentElement.NotifySizeChanged(Computed.Width, Computed.Height);
                if (VisualElement.LayoutMutationDepth == 0)
                    ParentElement.InvalidateLayout();

                if (prevW <= 0 || prevH <= 0)
                    MarkChildrenAwaitingAnchorReseed();
            }

            if (sizeChanged)
            {
                ParentElement?.ClearRenderCache();
            }

            if (VisualElement.LayoutMutationDepth == 0)
            {
                ParentElement?.InvalidatePaint();
            }
        }

        return changed;
    }

    private void NotifyChangedIfOutsideLayout(bool invalidateLayout)
    {
        if (VisualElement.LayoutMutationDepth != 0)
            return;

        OnChanged?.Invoke(this);
        if (invalidateLayout)
            ParentElement?.InvalidateLayout();
        else
            ParentElement?.InvalidatePaint();
    }

    private void NotifySizeChangedIfNeeded(float prevW, float prevH)
    {
        if (ParentElement == null)
            return;

        const float eps = 0.01f;
        bool sizeChanged =
            Math.Abs(prevW - ComputedTransform.Width) > eps ||
            Math.Abs(prevH - ComputedTransform.Height) > eps;
        if (!sizeChanged)
            return;

        ParentElement.NotifySizeChanged(ComputedTransform.Width, ComputedTransform.Height);

        if (prevW <= 0 || prevH <= 0)
            MarkChildrenAwaitingAnchorReseed();
    }

    private void MarkChildrenAwaitingAnchorReseed()
    {
        if (ParentElement == null)
            return;

        var children = ParentElement.Children;
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child?.Transform == null)
                continue;
            if (!child.Transform._anchorsInitialized)
                child.Transform._transformDirty = true;
        }
    }
}