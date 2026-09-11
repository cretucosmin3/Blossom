using System;
using SkiaSharp;
using Blossom.Core;
using Blossom.Core.Design;
using Blossom.Core.Visual.Enums;

namespace Blossom.Core.Visual;

public class Transform : IDisposable
{
    private readonly SKMatrix44 _cachedLocalM44 = new SKMatrix44();
    private readonly SKMatrix44 _cachedGlobalM44 = new SKMatrix44();
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
        set { if (_rotationX != value) { _rotationX = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.ScheduleRender(); } }
    }
    public float RotationY
    {
        get => _rotationY;
        set { if (_rotationY != value) { _rotationY = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.ScheduleRender(); } }
    }
    public float RotationZ
    {
        get => _rotationZ;
        set { if (_rotationZ != value) { _rotationZ = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.ScheduleRender(); } }
    }
    public float ScaleX
    {
        get => _scaleX;
        set { if (_scaleX != value) { _scaleX = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.ScheduleRender(); } }
    }
    public float ScaleY
    {
        get => _scaleY;
        set { if (_scaleY != value) { _scaleY = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.ScheduleRender(); } }
    }
    public float ScaleZ
    {
        get => _scaleZ;
        set { if (_scaleZ != value) { _scaleZ = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.ScheduleRender(); } }
    }
    public float Perspective
    {
        get => _perspective;
        set { if (_perspective != value) { _perspective = value; _matrixDirty = true; UpdateHas3DTransforms(); ParentElement?.ScheduleRender(); } }
    }
    public float TransformOriginX
    {
        get => _originX;
        set { if (_originX != value) { _originX = value; _matrixDirty = true; ParentElement?.ScheduleRender(); } }
    }
    public float TransformOriginY
    {
        get => _originY;
        set { if (_originY != value) { _originY = value; _matrixDirty = true; ParentElement?.ScheduleRender(); } }
    }

    public SKMatrix44 GetLocalM44()
    {
        if (_matrixDirty)
        {
            _cachedLocalM44.SetIdentity();

            float localX = X - (Parent != null ? Parent.X : 0);
            float localY = Y - (Parent != null ? Parent.Y : 0);

            _cachedLocalM44.PreTranslate(localX, localY, 0);

            float originPxX = Width * TransformOriginX;
            float originPxY = Height * TransformOriginY;
            _cachedLocalM44.PreTranslate(originPxX, originPxY, 0);

            if (Perspective > 0)
            {
                using var persp = SKMatrix44.CreateIdentity();
                persp[3, 2] = -1f / Perspective;
                _cachedLocalM44.PreConcat(persp);
            }

            if (RotationX != 0)
            {
                using var rot = SKMatrix44.CreateRotationDegrees(1f, 0f, 0f, RotationX);
                _cachedLocalM44.PreConcat(rot);
            }
            if (RotationY != 0)
            {
                using var rot = SKMatrix44.CreateRotationDegrees(0f, 1f, 0f, RotationY);
                _cachedLocalM44.PreConcat(rot);
            }
            if (RotationZ != 0)
            {
                using var rot = SKMatrix44.CreateRotationDegrees(0f, 0f, 1f, RotationZ);
                _cachedLocalM44.PreConcat(rot);
            }

            if (ScaleX != 1 || ScaleY != 1 || ScaleZ != 1)
            {
                _cachedLocalM44.PreScale(ScaleX, ScaleY, ScaleZ);
            }

            _cachedLocalM44.PreTranslate(-originPxX, -originPxY, 0);

            _matrixDirty = false;
        }
        return _cachedLocalM44;
    }

    public SKMatrix44 GetGlobalM44()
    {
        var local = GetLocalM44();
        if (Parent != null)
        {
            var parentGlobal = Parent.GetGlobalM44();
            _cachedGlobalM44.SetIdentity();
            _cachedGlobalM44.PreConcat(parentGlobal);
            _cachedGlobalM44.PreConcat(local);
            return _cachedGlobalM44;
        }

        return local;
    }

    public void Dispose()
    {
        _cachedLocalM44.Dispose();
        _cachedGlobalM44.Dispose();
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
    /// </summary>
    public Action<Transform> OnChanged;

    [BuilderProperty("Position X", "Layout", min: -1000f, max: 3000f, step: 1f)]
    public float X
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
            if (VisualElement.LayoutMutationDepth == 0)
            {
                OnChanged?.Invoke(this);
                ParentElement?.InvalidatePaint();
            }
        }
    }

    [BuilderProperty("Position Y", "Layout", min: -1000f, max: 3000f, step: 1f)]
    public float Y
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
            if (VisualElement.LayoutMutationDepth == 0)
            {
                OnChanged?.Invoke(this);
                ParentElement?.InvalidatePaint();
            }
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

            Local.Width = value;
            CalculateLeftAnchor();
            CalculateRighAnchor();

            ComputedTransform.Width = value;
            CenterX = ComputedTransform.X + (ComputedTransform.Width / 2f);

            _transformDirty = true;
            _matrixDirty = true;
            ParentElement?.InvalidateSubtreeBounds();
            if (VisualElement.LayoutMutationDepth == 0)
            {
                OnChanged?.Invoke(this);
                // Size change may require LayoutChildren
                ParentElement?.InvalidateLayout();
            }
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

            Local.Height = value;
            CalculateTopAnchor();
            CalculateBottomAnchor();

            ComputedTransform.Height = value;
            CenterY = ComputedTransform.Y + (ComputedTransform.Height / 2f);

            _transformDirty = true;
            _matrixDirty = true;
            ParentElement?.InvalidateSubtreeBounds();
            if (VisualElement.LayoutMutationDepth == 0)
            {
                OnChanged?.Invoke(this);
                ParentElement?.InvalidateLayout();
            }
        }
    }

    /// <summary>
    /// Sets absolute (parent-space / screen) frame and eagerly updates <see cref="Computed"/>
    /// so nested layout in the same pass can read a correct parent origin.
    /// Prefer this over setting X/Y/Width/Height separately during layout.
    /// </summary>
    public void SetAbsoluteFrame(float absX, float absY, float width, float height)
    {
        width = Math.Max(0, width);
        height = Math.Max(0, height);

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

        // During LayoutChildren, notifications are suppressed (see VisualElement.LayoutMutationDepth)
        if (VisualElement.LayoutMutationDepth == 0)
        {
            OnChanged?.Invoke(this);
            // Paint only — caller that needs re-layout should call InvalidateLayout explicitly
            ParentElement?.InvalidatePaint();
        }
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

    public float Left { get => X; }
    public float Right { get => X + Width; }
    public float Top { get => Y; }
    public float Bottom { get => Y + Height; }

    public bool ValidateOnAnchor { get; set; } = false;

    private Anchor _Anchor;
    public Anchor Anchor
    {
        get => _Anchor;
        set
        {
            _Anchor = value;

            var parentX = Parent != null ? Parent.ComputedTransform.X : 0;
            var parentY = Parent != null ? Parent.ComputedTransform.Y : 0;

            Local.X = Computed.X - parentX;
            Local.Y = Computed.Y - parentY;
            Local.Width = Computed.Width;
            Local.Height = Computed.Height;

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
        if (Parent != null && ParentWidth == 0)
        {
            _anchorsInitialized = false;
            return;
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
        if (Browser.WasResized)
        {
            _transformDirty = true;
        }

        if (!_transformDirty && _anchorsInitialized)
        {
            return false;
        }

        _matrixDirty = true;

        if (!_anchorsInitialized && (Parent == null || ParentWidth > 0))
        {
            SetAnchorValues();
        }

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

            if (sizeChanged && ParentElement != null && VisualElement.LayoutMutationDepth == 0)
            {
                ParentElement.NotifySizeChanged(Computed.Width, Computed.Height);
                // Custom LayoutChildren (Kanban, Modal, etc.) must re-run when size changes
                ParentElement.InvalidateLayout();
            }

            ParentElement?.ClearRenderCache();

            if (VisualElement.LayoutMutationDepth == 0)
            {
                ParentElement?.InvalidatePaint();
            }
        }

        return changed;
    }
}