using System.Numerics;
using System.Text;
using System;
using System.Collections.Generic;
using SkiaSharp;
using Blossom.Core;
using Blossom.Core.Design;
using Blossom.Core.Visual.Enums;
using Silk.NET.Input;

namespace Blossom.Core.Visual;

public class VisualElement : IDisposable
{
    public Guid Id { get; } = Guid.NewGuid();
    public string? Name { get; set; }

    /// <summary>Stable key for the command ledger (never null). Prefer Id so unnamed elements still render.</summary>
    internal string DrawCommandKey => Id.ToString("N");

    public virtual void AddedToView() { }
    public virtual void RemovedFromView() { }

    public event Action<VisualElement, float, float>? SizeChanged;
    protected virtual void OnSizeChanged(float width, float height) { }
    internal void NotifySizeChanged(float width, float height)
    {
        OnSizeChanged(width, height);
        SizeChanged?.Invoke(this, width, height);
    }

    #region Layout & Box Model
    private Thickness _padding = new(0);
    public Thickness Padding
    {
        get => _padding;
        set
        {
            if (_padding != value)
            {
                _padding = value;
                _localBoundsDirty = true;
                InvalidateLayout();
            }
        }
    }

    private Thickness _margin = new(0);
    public Thickness Margin
    {
        get => _margin;
        set
        {
            if (_margin != value)
            {
                _margin = value;
                _localBoundsDirty = true;
                InvalidateLayout();
            }
        }
    }

    private float? _minWidth;
    public float? MinWidth
    {
        get => _minWidth;
        set
        {
            if (_minWidth != value)
            {
                _minWidth = value;
                InvalidateLayout();
            }
        }
    }

    private float? _maxWidth;
    public float? MaxWidth
    {
        get => _maxWidth;
        set
        {
            if (_maxWidth != value)
            {
                _maxWidth = value;
                InvalidateLayout();
            }
        }
    }

    private float? _minHeight;
    public float? MinHeight
    {
        get => _minHeight;
        set
        {
            if (_minHeight != value)
            {
                _minHeight = value;
                InvalidateLayout();
            }
        }
    }

    private float? _maxHeight;
    public float? MaxHeight
    {
        get => _maxHeight;
        set
        {
            if (_maxHeight != value)
            {
                _maxHeight = value;
                InvalidateLayout();
            }
        }
    }

    private bool _isLayoutDirty = true;
    public bool IsLayoutDirty => _isLayoutDirty;

    internal bool _isPaintDirty = true;
    public bool IsPaintDirty => _isPaintDirty;

    /// <summary>
    /// While &gt; 0, transform mutations (SetAbsoluteFrame, X/Y/…) must not re-enter layout/paint
    /// invalidation — LayoutChildren is already running. Prevents continuous render loops.
    /// </summary>
    internal static int LayoutMutationDepth { get; private set; }

    public void InvalidatePaint()
    {
        if (LayoutMutationDepth > 0)
        {
            // Defer: layout will request paint once if bounds actually changed
            _isPaintDirty = true;
            _localBoundsDirty = true;
            _renderBoundsDirty = true;
            return;
        }

        _isPaintDirty = true;
        IsDirty = true;
        _localBoundsDirty = true;
        _renderBoundsDirty = true;

        if (ParentView is not null)
        {
            ParentView.RenderRequired = true;
            try { Silk.NET.GLFW.GlfwProvider.GLFW.Value.PostEmptyEvent(); } catch { }
        }
    }

    public void InvalidateLayout()
    {
        // Nested invalidation during LayoutChildren would schedule infinite frames
        if (LayoutMutationDepth > 0)
        {
            _isLayoutDirty = true;
            Transform._transformDirty = true;
            return;
        }

        _isLayoutDirty = true;
        Transform._transformDirty = true;

        var current = Parent;
        while (current != null)
        {
            current._isLayoutDirty = true;
            current = current.Parent;
        }

        if (ParentView is not null)
        {
            ParentView.LayoutRequired = true;
        }

        InvalidatePaint();
    }

    /// <summary>
    /// Called during the layout pass after this element's computed size is known
    /// (or when this node is layout-dirty). Override to position/size children
    /// (e.g. stack, wrap, flex-like algorithms). Default: no-op (anchors only).
    /// </summary>
    protected virtual void LayoutChildren() { }

    internal void PerformLayout()
    {
        if (!_isLayoutDirty) return;

        LayoutMutationDepth++;
        try
        {
            LayoutChildren();
            _isLayoutDirty = false;
        }
        finally
        {
            LayoutMutationDepth--;
        }
    }

    /// <summary>
    /// Immediately re-run <see cref="LayoutChildren"/> on this element and descendants.
    /// Use after programmatic moves (e.g. drag) so children track the parent in the same frame.
    /// </summary>
    public void ForceLayoutSubtree()
    {
        LayoutMutationDepth++;
        try
        {
            _isLayoutDirty = true;
            LayoutChildren();
            _isLayoutDirty = false;

            for (int i = 0; i < _children.Count; i++)
            {
                _children[i]?.ForceLayoutSubtreeCore();
            }

            foreach (var visual in GetVisualChildren())
            {
                if (visual == null || _children.Contains(visual)) continue;
                visual.ForceLayoutSubtreeCore();
            }
        }
        finally
        {
            LayoutMutationDepth--;
        }

        // One paint request for the whole subtree after layout settles
        InvalidateSubtreePaint();
    }

    private void ForceLayoutSubtreeCore()
    {
        _isLayoutDirty = true;
        LayoutChildren();
        _isLayoutDirty = false;

        for (int i = 0; i < _children.Count; i++)
            _children[i]?.ForceLayoutSubtreeCore();

        foreach (var visual in GetVisualChildren())
        {
            if (visual == null || _children.Contains(visual)) continue;
            visual.ForceLayoutSubtreeCore();
        }
    }

    /// <summary>
    /// Mark this element and all descendants paint-dirty, including previous and current bounds
    /// (needed when a whole subtree moves together).
    /// </summary>
    public void InvalidateSubtreePaint()
    {
        InvalidatePaint();
        for (int i = 0; i < _children.Count; i++)
        {
            _children[i]?.InvalidateSubtreePaint();
        }
        foreach (var visual in GetVisualChildren())
        {
            if (visual == null || _children.Contains(visual)) continue;
            visual.InvalidateSubtreePaint();
        }
    }

    /// <summary>
    /// Recursively marks render bounds, transform matrix, and layout transform dirty for this element and all descendants.
    /// Used when this element or an ancestor moves or resizes.
    /// </summary>
    public void InvalidateSubtreeBounds()
    {
        _renderBoundsDirty = true;
        _localBoundsDirty = true;
        Transform._matrixDirty = true;
        Transform._transformDirty = true;

        for (int i = 0; i < _children.Count; i++)
        {
            _children[i]?.InvalidateSubtreeBounds();
        }
        foreach (var visual in GetVisualChildren())
        {
            if (visual == null || _children.Contains(visual)) continue;
            visual.InvalidateSubtreeBounds();
        }
    }

    /// <summary>
    /// Returns the size this element would like given max constraints.
    /// Default: current Width/Height (or Local width/height).
    /// Override for text, images, or custom content.
    /// </summary>
    public virtual SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        float w = Transform.Width;
        float h = Transform.Height;

        if (!string.IsNullOrEmpty(Text) && Style?.Text?.Paint != null)
        {
            var layout = EnsureTextLayout(maxWidth);
            float textW = layout.Width;
            float textH = layout.Height > 0 ? layout.Height : (Style.Text.Paint.FontMetrics.Descent - Style.Text.Paint.FontMetrics.Ascent);

            w = textW + Padding.Horizontal + (Style.Text.Padding * 2);
            h = textH + Padding.Vertical + (Style.Text.Padding * 2);
        }

        if (MinWidth.HasValue) w = Math.Max(w, MinWidth.Value);
        if (MaxWidth.HasValue) w = Math.Min(w, MaxWidth.Value);
        if (MinHeight.HasValue) h = Math.Max(h, MinHeight.Value);
        if (MaxHeight.HasValue) h = Math.Min(h, MaxHeight.Value);

        if (maxWidth > 0) w = Math.Min(w, maxWidth);
        if (maxHeight > 0) h = Math.Min(h, maxHeight);

        return new SKSize(Math.Max(0, w), Math.Max(0, h));
    }
    #endregion

    #region Transitions
    public float EffectiveTransitionProgress
    {
        get
        {
            if (Style != null && Style.TransitionType != TransitionEffectType.None)
                return Style.TransitionProgress;
            if (Parent != null)
                return Parent.EffectiveTransitionProgress;
            return 1.0f;
        }
    }

    public TransitionEffectType EffectiveTransitionType
    {
        get
        {
            if (Style != null && Style.TransitionType != TransitionEffectType.None)
                return Style.TransitionType;
            if (Parent != null)
                return Parent.EffectiveTransitionType;
            return TransitionEffectType.None;
        }
    }

    public VisualElement TransitionHost
    {
        get
        {
            if (Style != null && Style.TransitionType != TransitionEffectType.None)
                return this;
            if (Parent != null)
                return Parent.TransitionHost;
            return this;
        }
    }
    #endregion

    #region User Interactions
    private bool _interactive = true;
    /// <summary>
    /// Whether this element responds to user interaction (hit-testing, mouse events, pointer capture, keyboard).
    /// When false (or when any ancestor is false), this element is excluded from hit-testing and cannot receive pointer capture or active keyboard element.
    /// Default is true.
    /// </summary>
    public bool Interactive
    {
        get => _interactive;
        set
        {
            if (_interactive != value)
            {
                _interactive = value;
                if (!_interactive)
                {
                    if (HasPointerCapture) ReleasePointer();
                    if (ParentView?.ActiveKeyboardElement == this) ParentView.SetActiveKeyboardElement(null);
                }
            }
        }
    }

    /// <summary>
    /// Cumulative visible state: true only if this element and all ancestors have <see cref="Visible"/> set to true.
    /// </summary>
    public bool EffectiveVisible => Visible && (Parent == null || Parent.EffectiveVisible);

    /// <summary>
    /// Cumulative interactive state: true only if this element and all ancestors are visible and have <see cref="Interactive"/> set to true.
    /// </summary>
    public bool EffectiveInteractive => Interactive && EffectiveVisible && (Parent == null || Parent.EffectiveInteractive);

    private float _opacity = 1f;
    /// <summary>
    /// Opacity factor between 0.0 (fully transparent) and 1.0 (fully opaque).
    /// Subtrees render with cumulative <see cref="EffectiveOpacity"/>.
    /// Note: Elements with Opacity 0 still participate in hit-testing unless <see cref="Interactive"/> is false or <see cref="IsClickthrough"/> is true.
    /// Default is 1.0.
    /// </summary>
    public float Opacity
    {
        get => _opacity;
        set
        {
            float clamped = Math.Clamp(value, 0f, 1f);
            if (_opacity != clamped)
            {
                _opacity = clamped;
                InvalidatePaint();
            }
        }
    }

    /// <summary>
    /// Cumulative effective opacity multiplying this element's opacity by all ancestor opacities.
    /// </summary>
    public float EffectiveOpacity => Opacity * (Parent?.EffectiveOpacity ?? 1f);

    /// <summary>
    /// Standard mouse cursor displayed when hovering over this element.
    /// If null, inherits cursor from the nearest ancestor with a non-null Cursor, or standard default.
    /// </summary>
    public StandardCursor? Cursor { get; set; }

    /// <summary>
    /// Opt-in flag: if true, this element can become the active keyboard target on mouse-down.
    /// </summary>
    public bool ReceivesKeyboard { get; set; }

    /// <summary>
    /// Legacy compatibility alias for <see cref="ReceivesKeyboard"/>.
    /// </summary>
    public bool Focusable
    {
        get => ReceivesKeyboard;
        set => ReceivesKeyboard = value;
    }
    public bool HasFocus => ParentView != null && ParentView.ActiveKeyboardElement == this;

    public void CapturePointer()
    {
        if (EffectiveInteractive)
        {
            ParentView?.SetPointerCapture(this);
        }
    }

    public void ReleasePointer()
    {
        if (HasPointerCapture)
        {
            ParentView?.ReleasePointerCapture(this);
        }
    }

    public bool HasPointerCapture => ParentView != null && ParentView.PointerCaptureElement == this;

    /// <summary>
    /// When true, this element is skipped as a hit-test target (events pass through to elements below).
    /// </summary>
    public bool IsClickthrough { get; set; }

    private int _zIndex = 0;
    /// <summary>
    /// Rendering and hit-test stacking order. Higher ZIndex values are drawn on top and receive hit events first among siblings.
    /// </summary>
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

    public float HoverProgress { get; internal set; } = 0f;
    public Func<SKBitmap?>? GetShaderBitmapResource;
    public float ShaderMixingRate { get; set; } = 0.25f;

    internal void UpdateHover(float dt)
    {
        bool usesAnimatedVisual = Style != null && (
            (Style.BackgroundShader != BackgroundShaderType.None && Style.ShaderRenderMode == EffectRenderMode.Continuous) ||
            (Style.BorderEffect != BorderEffectType.None && Style.ShaderRenderMode == EffectRenderMode.Continuous) ||
            (EffectiveTransitionType != TransitionEffectType.None && EffectiveTransitionProgress < 1.0f));

        bool isHovered = ParentView != null && ParentView.HoveredElement == this;
        float target = isHovered ? 1f : 0f;
        if (HoverProgress != target)
        {
            if (usesAnimatedVisual)
            {
                float speed = 8f;
                if (isHovered)
                    HoverProgress = Math.Min(1f, HoverProgress + dt * speed);
                else
                    HoverProgress = Math.Max(0f, HoverProgress - dt * speed);
                ScheduleRender();
            }
            else
            {
                // Chrome hover is applied instantly via BackColor; don't keep the view dirty for 125ms.
                HoverProgress = target;
            }
        }

        if (usesAnimatedVisual)
            ScheduleRender();

        OnUpdate(dt);
    }

    /// <summary>
    /// Called each frame during the view update pass. Override to perform per-frame animations.
    /// </summary>
    protected virtual void OnUpdate(float dt) { }
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
    public bool HasClippingAncestors => _hasClippingAncestors;

    internal bool _visibilityClippingDirty = true;
    public void MarkVisibilityClippingDirty()
    {
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
    internal SKRect _lastRenderBounds = SKRect.Empty;
    private SKRect _cachedRenderBounds;
    internal bool _renderBoundsDirty = true;
    private SKRect _cachedLocalBounds;
    internal bool _localBoundsDirty = true;

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
        if (_localBoundsDirty)
        {
            _cachedLocalBounds = GetLocalContentBounds();
            _localBoundsDirty = false;
        }

        return _cachedLocalBounds;
    }

    /// <summary>
    /// Returns the bounding rectangle of this element's local content and style
    /// (0, 0, Width, Height + text, shadow, border inflation).
    /// Override in subclasses to expand dirty/render bounds for custom drawing or visual effects.
    /// </summary>
    protected virtual SKRect GetLocalContentBounds()
    {
        var localRect = new SKRect(0, 0, Transform.Width, Transform.Height);

        // Include text bounds if text is rendered
        if (!string.IsNullOrEmpty(Text) && Style?.Text != null)
        {
            CalculateText();
            var textRect = SKRect.Create(
                TextPosition.X,
                TextPosition.Y,
                Math.Max(TextBounds.Width, 0),
                Math.Max(TextBounds.Height, 0)
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
        bool useGlobalMapping = ParentView != null;
        if (!useGlobalMapping)
        {
            var curr = this;
            while (curr != null)
            {
                if (curr.Transform.Has3DTransforms)
                {
                    useGlobalMapping = true;
                    break;
                }
                curr = curr.Parent;
            }
        }

        if (useGlobalMapping)
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
        if (LayoutMutationDepth > 0)
            return; // LayoutChildren is applying frames — do not re-enter layout/paint here

        // Paint only: re-layout is requested explicitly (parent size change, ForceLayoutSubtree, etc.)
        CalculateText();
        MarkVisibilityClippingDirty();
        TransformChanged?.Invoke(this, transform);
        InvalidateSubtreeBounds();
        InvalidatePaint();
    }

    private bool _Visible = true;
    /// <summary>
    /// Gets or sets whether this element is visible. When false, the element and its subtree are hidden from rendering and hit-testing.
    /// </summary>
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
    public View ParentView
    {
        get => Parent != null ? Parent.ParentView : _ParentView;
        internal set => _ParentView = value;
    }

    public int Layer
    {
        get => Parent != null ? Parent.Layer + 1 : 0;
    }

    private readonly List<VisualElement> _children = new();
    public IReadOnlyList<VisualElement> Children => _children;

    private VisualElement? _Parent;

    private VisualElement _RootParent = null!;
    public VisualElement RootParent
    {
        get => _RootParent ?? this;
        private set => _RootParent = value;
    }

    public VisualElement? Parent
    {
        get => _Parent;
        set
        {
            if (_Parent == value) return;

            if (_Parent != null)
            {
                _Parent.RemoveChild(this);
            }

            if (value != null)
            {
                value.AddChild(this);
            }
        }
    }

    internal void SetParentInternal(VisualElement? value)
    {
        if (_Parent != null)
            _Parent.TransformChanged -= ParentTransformChanged;

        _Parent = value;

        if (value != null)
        {
            Transform.Parent = value.Transform;
            _Parent.TransformChanged += ParentTransformChanged;
            RootParent = _Parent.RootParent;
        }
        else
        {
            Transform.DetachParent();
            RootParent = this;
        }

        ScheduleRender();
    }

    internal bool TransformIsChanged = false;
    internal SKPoint TextPosition;

    private Transform _Transform;
    public Transform Transform
    {
        get => _Transform;
        set
        {
            // Detach old
            if (_Transform != null)
                _Transform.OnChanged -= OnTransformChanged;

            // Attach new
            _Transform = value ?? new Transform();
            _Transform.OnChanged += OnTransformChanged;
            _Transform.ParentElement = this;

            // Update parent of children's transforms
            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                if (child != null && child.Transform != null)
                {
                    child.Transform.Parent = _Transform;
                }
            }
        }
    }

    public VisualElement()
    {
        // Critical: default Transform must own this element and fire OnChanged.
        // Without ParentElement, SetAbsoluteFrame/X/Y never InvalidateLayout — children
        // stay put when a parent moves (empty drag shells, dirty-rect holes).
        _Transform = new Transform();
        _Transform.ParentElement = this;
        _Transform.OnChanged += OnTransformChanged;
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

    private OverflowMode _overflowX = OverflowMode.Visible;
    /// <summary>
    /// Gets or sets horizontal content overflow behavior (<see cref="OverflowMode.Visible"/>, <see cref="OverflowMode.Clip"/>, or <see cref="OverflowMode.Scroll"/>).
    /// </summary>
    public OverflowMode OverflowX
    {
        get => _overflowX;
        set
        {
            if (_overflowX != value)
            {
                _overflowX = value;
                _IsClipping = (_overflowX == OverflowMode.Clip || _overflowX == OverflowMode.Scroll || _overflowY == OverflowMode.Clip || _overflowY == OverflowMode.Scroll);
                MarkVisibilityClippingDirty();
                InvalidateLayout();
            }
        }
    }

    private OverflowMode _overflowY = OverflowMode.Visible;
    /// <summary>
    /// Gets or sets vertical content overflow behavior (<see cref="OverflowMode.Visible"/>, <see cref="OverflowMode.Clip"/>, or <see cref="OverflowMode.Scroll"/>).
    /// </summary>
    public OverflowMode OverflowY
    {
        get => _overflowY;
        set
        {
            if (_overflowY != value)
            {
                _overflowY = value;
                _IsClipping = (_overflowX == OverflowMode.Clip || _overflowX == OverflowMode.Scroll || _overflowY == OverflowMode.Clip || _overflowY == OverflowMode.Scroll);
                MarkVisibilityClippingDirty();
                InvalidateLayout();
            }
        }
    }

    /// <summary>
    /// Uniform shorthand for setting both <see cref="OverflowX"/> and <see cref="OverflowY"/>.
    /// </summary>
    public OverflowMode Overflow
    {
        get => _overflowY;
        set
        {
            OverflowX = value;
            OverflowY = value;
        }
    }

    private bool _IsClipping = false;
    /// <summary>
    /// Gets or sets whether child elements and content are clipped to this element's rounded bounds.
    /// Setting this aligns with <see cref="Overflow"/> modes (<see cref="OverflowMode.Clip"/> when true, <see cref="OverflowMode.Visible"/> when false).
    /// </summary>
    public bool IsClipping
    {
        get => _IsClipping;
        set
        {
            if (_IsClipping != value)
            {
                _IsClipping = value;
                if (!_IsClipping)
                {
                    _overflowX = OverflowMode.Visible;
                    _overflowY = OverflowMode.Visible;
                }
                else if (_overflowX == OverflowMode.Visible && _overflowY == OverflowMode.Visible)
                {
                    _overflowX = OverflowMode.Clip;
                    _overflowY = OverflowMode.Clip;
                }
                MarkVisibilityClippingDirty();
                ScheduleRender();
            }
        }
    }

    private SKBitmap? _BackgroundImage;
    public SKBitmap? BackgroundImage
    {
        get => _BackgroundImage;
        set
        {
            if (_BackgroundImage != value)
            {
                _BackgroundImage?.Dispose();
                _BackgroundImage = value;
                ScheduleRender();
            }
        }
    }

    private ImageScaleMode _BackgroundImageScale = ImageScaleMode.Stretch;
    public ImageScaleMode BackgroundImageScale
    {
        get => _BackgroundImageScale;
        set
        {
            if (_BackgroundImageScale != value)
            {
                _BackgroundImageScale = value;
                ScheduleRender();
            }
        }
    }

    private float _BackgroundImageBlur = 0f;
    public float BackgroundImageBlur
    {
        get => _BackgroundImageBlur;
        set
        {
            if (_BackgroundImageBlur != value)
            {
                _BackgroundImageBlur = value;
                ScheduleRender();
            }
        }
    }

    private float _BackgroundImageGrayscale = 0f;
    public float BackgroundImageGrayscale
    {
        get => _BackgroundImageGrayscale;
        set
        {
            if (_BackgroundImageGrayscale != value)
            {
                _BackgroundImageGrayscale = value;
                ScheduleRender();
            }
        }
    }

    private SKColor _BackgroundImageTintColor = SKColors.Transparent;
    public SKColor BackgroundImageTintColor
    {
        get => _BackgroundImageTintColor;
        set
        {
            if (_BackgroundImageTintColor != value)
            {
                _BackgroundImageTintColor = value;
                ScheduleRender();
            }
        }
    }

    private SKBlendMode _BackgroundImageTintBlendMode = SKBlendMode.SrcATop;
    public SKBlendMode BackgroundImageTintBlendMode
    {
        get => _BackgroundImageTintBlendMode;
        set
        {
            if (_BackgroundImageTintBlendMode != value)
            {
                _BackgroundImageTintBlendMode = value;
                ScheduleRender();
            }
        }
    }

    private static readonly System.Net.Http.HttpClient _httpClient = new();

    public void LoadImageFromFile(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                BackgroundImage = null;
                return;
            }
            if (System.IO.File.Exists(filePath))
            {
                BackgroundImage = SKBitmap.Decode(filePath);
            }
            else
            {
                Log.Error($"File not found: {filePath}");
                BackgroundImage = null;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load image from file '{filePath}': {ex.Message}");
            BackgroundImage = null;
        }
    }

    public void LoadImageFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            BackgroundImage = null;
            return;
        }

        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url);
                var bmp = SKBitmap.Decode(bytes);
                if (bmp != null)
                {
                    Browser.Post(() =>
                    {
                        if (_isDisposed)
                        {
                            bmp.Dispose();
                            return;
                        }
                        BackgroundImage = bmp;
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load image from URL '{url}': {ex.Message}");
            }
        });
    }

    private SkiaSharp.Extended.Svg.SKSvg? _BackgroundSvg;
    public SkiaSharp.Extended.Svg.SKSvg? BackgroundSvg
    {
        get => _BackgroundSvg;
        set
        {
            if (_BackgroundSvg != value)
            {
                _BackgroundSvg?.Picture?.Dispose();
                _BackgroundSvg = value;
                ScheduleRender();
            }
        }
    }

    public void LoadSvgFromFile(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                BackgroundSvg = null;
                return;
            }
            if (System.IO.File.Exists(filePath))
            {
                var svg = new SkiaSharp.Extended.Svg.SKSvg();
                svg.Load(filePath);
                BackgroundSvg = svg;
            }
            else
            {
                Log.Error($"Svg file not found: {filePath}");
                BackgroundSvg = null;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load SVG from file '{filePath}': {ex.Message}");
            BackgroundSvg = null;
        }
    }

    public void LoadSvgFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            BackgroundSvg = null;
            return;
        }

        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url);
                using (var ms = new System.IO.MemoryStream(bytes))
                {
                    var svg = new SkiaSharp.Extended.Svg.SKSvg();
                    svg.Load(ms);
                    Browser.Post(() =>
                    {
                        if (_isDisposed)
                        {
                            svg.Picture?.Dispose();
                            return;
                        }
                        BackgroundSvg = svg;
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load SVG from URL '{url}': {ex.Message}");
            }
        });
    }

    public void AddChild(VisualElement child)
    {
        if (child == null) throw new ArgumentNullException(nameof(child));
        if (child == this) throw new InvalidOperationException("Cannot add an element as a child of itself.");
        if (child.ContainsElement(this)) throw new InvalidOperationException("Cannot add an ancestor as a child.");

        if (child._Parent != null)
        {
            if (child._Parent == this) return;
            child._Parent.RemoveChild(child);
        }

        _children.Add(child);
        child.SetParentInternal(this);

        if (ParentView != null)
        {
            RegisterSubtree(child, ParentView);
            ParentView.MarkHierarchyDirty();
        }
    }

    public void InsertChild(int index, VisualElement child)
    {
        if (child == null) throw new ArgumentNullException(nameof(child));
        if (child == this) throw new InvalidOperationException("Cannot add an element as a child of itself.");
        if (child.ContainsElement(this)) throw new InvalidOperationException("Cannot add an ancestor as a child.");

        if (child._Parent != null)
        {
            if (child._Parent == this)
            {
                SetChildIndex(child, index);
                return;
            }
            child._Parent.RemoveChild(child);
        }

        if (index < 0) index = 0;
        if (index > _children.Count) index = _children.Count;

        _children.Insert(index, child);
        child.SetParentInternal(this);

        if (ParentView != null)
        {
            RegisterSubtree(child, ParentView);
            ParentView.MarkHierarchyDirty();
        }

        InvalidateLayout();
    }

    public void RemoveChild(VisualElement child)
    {
        if (child == null) return;
        int index = _children.IndexOf(child);
        if (index < 0) return;

        _children.RemoveAt(index);

        var view = ParentView;
        if (view != null)
        {
            UnregisterSubtree(child, view);
            view.MarkHierarchyDirty();
        }

        child.SetParentInternal(null);
        InvalidateLayout();
    }

    public void ClearChildren()
    {
        var list = _children.ToArray();
        _children.Clear();

        var view = ParentView;
        foreach (var child in list)
        {
            if (view != null)
            {
                UnregisterSubtree(child, view);
            }
            child.SetParentInternal(null);
        }

        if (view != null)
        {
            view.MarkHierarchyDirty();
        }

        InvalidateLayout();
    }

    public void SetChildIndex(VisualElement child, int index)
    {
        if (child == null) return;
        int currentIndex = _children.IndexOf(child);
        if (currentIndex < 0) return;

        if (index < 0) index = 0;
        if (index >= _children.Count) index = _children.Count - 1;
        if (currentIndex == index) return;

        _children.RemoveAt(currentIndex);
        _children.Insert(index, child);

        ParentView?.MarkHierarchyDirty();
        InvalidateLayout();
    }

    /// <summary>
    /// Embeds a plugin root into this element as a host slot, executing synthetic resize reflow (Strategy S2).
    /// </summary>
    public void Embed(PluginRoot plugin) => PluginEmbed.Attach(plugin, this);

    /// <summary>
    /// Detaches an embedded plugin root from this element.
    /// </summary>
    public void Unembed(PluginRoot plugin) => PluginEmbed.Detach(plugin);

    internal virtual IEnumerable<VisualElement> GetVisualChildren() => Children;

    internal static void RegisterSubtree(VisualElement element, View view)
    {
        element._ParentView = view;
        view.TrackElement(ref element);

        foreach (var child in element.GetVisualChildren())
        {
            RegisterSubtree(child, view);
        }
    }

    internal static void UnregisterSubtree(VisualElement element, View view)
    {
        foreach (var child in element.GetVisualChildren())
        {
            UnregisterSubtree(child, view);
        }

        view.UntrackElement(ref element);
        element._ParentView = null!;
    }

    public Rect BoundingRect
    {
        get
        {
            if (_children.Count == 0)
                return Transform.Computed;

            float minX = Transform.Computed.X;
            float minY = Transform.Computed.Y;
            float maxX = minX + Transform.Computed.Width;
            float maxY = minY + Transform.Computed.Height;

            foreach (var child in _children)
            {
                var childBounds = child.BoundingRect;
                minX = Math.Min(minX, childBounds.X);
                minY = Math.Min(minY, childBounds.Y);
                maxX = Math.Max(maxX, childBounds.X + childBounds.Width);
                maxY = Math.Max(maxY, childBounds.Y + childBounds.Height);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }

    private readonly StringBuilder _Text = new("");
    [BuilderProperty("Text", "Content")]
    public string Text
    {
        get => _Text.ToString();
        set
        {
            if (_Text.ToString() != value)
            {
                _Text.Clear();
                _Text.Append(value);

                _localBoundsDirty = true;
                _textLayout = null;
                CalculateText();
                InvalidateLayout();
            }
        }
    }

    internal int Render(SKCanvas renderTarget)
    {
        RenderSingle(renderTarget);
        return 0;
    }

    private float _lastRecordedWidth = float.NaN;
    private float _lastRecordedHeight = float.NaN;

    internal void RenderSingle(SKCanvas targetCanvas)
    {
        if (ParentView == null) return;
        if (!Visible || !EffectiveVisible || ComputedVisibility == Visibility.Hidden) return;

        // Ensure commands are recorded in the ledger (key by Id — Name may be null).
        // Re-record when size changes so fills/borders match current bounds (scrolled cards
        // often first recorded at 0×0 or stale size and kept empty backgrounds).
        string drawKey = DrawCommandKey;
        float w = Transform.Computed.Width;
        float h = Transform.Computed.Height;
        bool sizeChanged =
            float.IsNaN(_lastRecordedWidth) ||
            Math.Abs(_lastRecordedWidth - w) > 0.5f ||
            Math.Abs(_lastRecordedHeight - h) > 0.5f;

        if (IsDirty || sizeChanged || ParentView.Ledger.GetCommands(drawKey) == null)
        {
            RecordDrawCommands(ParentView.Ledger);
            _lastRecordedWidth = w;
            _lastRecordedHeight = h;
            _IsDirty = false;
        }

        var cmds = ParentView.Ledger.GetCommands(drawKey);
        if (cmds == null) return;

        float opacity = EffectiveOpacity;
        if (opacity <= 0.0001f) return;

        float transitionProgress = EffectiveTransitionProgress;
        TransitionEffectType transitionType = EffectiveTransitionType;

        using (new SKAutoCanvasRestore(targetCanvas))
        {
            if (_hasClippingAncestors)
            {
                ApplyClippingHierarchy(targetCanvas);
            }

            var globalMatrix3D = Transform.GetGlobalM44();
            var globalMatrix2D = globalMatrix3D.Matrix;
            
            bool hasTransition = transitionType == TransitionEffectType.HalftoneDots && transitionProgress < 1.0f;
            bool hasOpacity = opacity < 0.999f;
            int saveCount = -1;

            if (hasTransition || hasOpacity)
            {
                float margin = 32f;
                var localRect = new SKRect(-margin, -margin, Transform.Computed.Width + margin, Transform.Computed.Height + margin);
                targetCanvas.Concat(ref globalMatrix2D);

                if (hasOpacity)
                {
                    byte alpha = (byte)Math.Clamp((int)(opacity * 255f), 0, 255);
                    using var opacityPaint = new SKPaint { Color = new SKColor(255, 255, 255, alpha) };
                    saveCount = targetCanvas.SaveLayer(localRect, opacityPaint);
                }
                else
                {
                    saveCount = targetCanvas.SaveLayer(localRect, null);
                }
            }
            else
            {
                targetCanvas.Concat(ref globalMatrix2D);
            }

            // Clip this element's own draw (custom images, children paint) to its box.
            // Ancestor clip alone does not clip Overflow=Clip content that draws past local bounds.
            if (IsClipping)
            {
                float cw = Transform.Computed.Width;
                float ch = Transform.Computed.Height;
                if (cw > 0 && ch > 0)
                    targetCanvas.ClipRect(new SKRect(0, 0, cw, ch), SKClipOperation.Intersect, true);
            }

            for (int i = 0; i < cmds.Count; i++)
            {
                cmds[i].Execute(targetCanvas);
            }

            if (saveCount != -1)
            {
                if (hasTransition)
                {
                    var host = TransitionHost;
                    float hostW = host.Transform.Computed.Width;
                    float hostH = host.Transform.Computed.Height;
                    float screenX = globalMatrix2D.TransX;
                    float screenY = globalMatrix2D.TransY;

                    using var halftoneShader = SKSLShaderManager.CreateHalftoneShader(transitionProgress, hostW, hostH, screenX, screenY);
                    if (halftoneShader != null)
                    {
                        using var maskPaint = new SKPaint
                        {
                            Shader = halftoneShader,
                            BlendMode = SKBlendMode.DstIn,
                            IsAntialias = true
                        };
                        float margin = 32f;
                        var localRect = new SKRect(-margin, -margin, Transform.Computed.Width + margin, Transform.Computed.Height + margin);
                        targetCanvas.DrawRect(localRect, maskPaint);
                    }
                }
                targetCanvas.RestoreToCount(saveCount);
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

    /// <summary>
    /// Hit-tests a coordinate in this element's local space (0..Width, 0..Height).
    /// Default implementation tests within the rectangle bounds taking border corner radii into account.
    /// Override in subclasses for custom hit geometry (circles, ellipses, polygons, paths).
    /// </summary>
    public virtual bool HitTestLocal(float localX, float localY)
    {
        float w = Transform.Width;
        float h = Transform.Height;
        
        if (localX < 0 || localX > w || localY < 0 || localY > h)
            return false;

        float r1 = Style?.Border?.RoundnessTopLeft ?? 0;
        float r2 = Style?.Border?.RoundnessTopRight ?? 0;
        float r3 = Style?.Border?.RoundnessBottomRight ?? 0;
        float r4 = Style?.Border?.RoundnessBottomLeft ?? 0;

        // Top-Left corner
        if (localX < r1 && localY < r1)
        {
            float dx = localX - r1;
            float dy = localY - r1;
            return (dx * dx + dy * dy) <= r1 * r1;
        }
        // Top-Right corner
        if (localX > w - r2 && localY < r2)
        {
            float dx = localX - (w - r2);
            float dy = localY - r2;
            return (dx * dx + dy * dy) <= r2 * r2;
        }
        // Bottom-Right corner
        if (localX > w - r3 && localY > h - r3)
        {
            float dx = localX - (w - r3);
            float dy = localY - (h - r3);
            return (dx * dx + dy * dy) <= r3 * r3;
        }
        // Bottom-Left corner
        if (localX < r4 && localY > h - r4)
        {
            float dx = localX - r4;
            float dy = localY - (h - r4);
            return (dx * dx + dy * dy) <= r4 * r4;
        }

        return true;
    }

    /// <summary>
    /// Legacy compatibility alias for <see cref="HitTestLocal(float, float)"/>.
    /// </summary>
    public bool IsPointInside(float x, float y) => HitTestLocal(x, y);

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

        // 1.5 Draw Backdrop Blur (Glassmorphism)
        if (Style != null && Style.BackdropBlur > 0)
        {
            cmds.Add(new DrawBackdropBlurCommand(
                Style.BackdropBlur,
                Style.Border?.RoundnessTopLeft ?? 0,
                Style.Border?.RoundnessTopRight ?? 0,
                Style.Border?.RoundnessBottomRight ?? 0,
                Style.Border?.RoundnessBottomLeft ?? 0,
                this,
                Style.ShaderRenderMode
            ));
        }

        // 2. Draw Fill / Background Shader
        if (Style != null && Style.BackgroundShader != BackgroundShaderType.None)
        {
            cmds.Add(new DrawShaderBackgroundCommand(
                Style.BackgroundShader,
                Style.BackgroundShaderColor,
                Style.Border?.RoundnessTopLeft ?? 0,
                Style.Border?.RoundnessTopRight ?? 0,
                Style.Border?.RoundnessBottomRight ?? 0,
                Style.Border?.RoundnessBottomLeft ?? 0,
                this,
                Style.ShaderRenderMode
            ));
        }
        else if ((Style != null && Style.BackColor.Alpha > 0) || (Style != null && Style.BackgroundPathEffect != null))
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

        // 2.5 Draw Background Image / SVG
        if (BackgroundImage != null)
        {
            cmds.Add(new DrawImageCommand(
                BackgroundImage,
                rect,
                BackgroundImageScale,
                Style?.Border?.RoundnessTopLeft ?? 0,
                Style?.Border?.RoundnessTopRight ?? 0,
                Style?.Border?.RoundnessBottomRight ?? 0,
                Style?.Border?.RoundnessBottomLeft ?? 0,
                BackgroundImageBlur,
                BackgroundImageGrayscale,
                BackgroundImageTintColor,
                BackgroundImageTintBlendMode
            ));
        }
        else if (BackgroundSvg != null)
        {
            cmds.Add(new DrawSvgCommand(
                BackgroundSvg,
                rect,
                BackgroundImageScale,
                Style?.Border?.RoundnessTopLeft ?? 0,
                Style?.Border?.RoundnessTopRight ?? 0,
                Style?.Border?.RoundnessBottomRight ?? 0,
                Style?.Border?.RoundnessBottomLeft ?? 0,
                BackgroundImageBlur,
                BackgroundImageGrayscale,
                BackgroundImageTintColor,
                BackgroundImageTintBlendMode
            ));
        }

        // 3. Draw Stroke/Border
        if (Style?.Border?.Width > 0 && Style.Border.Color.Alpha > 0)
        {
            if (Style.BorderEffect != BorderEffectType.None)
            {
                cmds.Add(new DrawBorderCommand(
                    Style.BorderEffect,
                    Style.Border.Width,
                    Style.BorderEffectSpeed,
                    Style.BorderEffectAmount,
                    Style.Border.Color,
                    Style.Border.RoundnessTopLeft,
                    Style.Border.RoundnessTopRight,
                    Style.Border.RoundnessBottomRight,
                    Style.Border.RoundnessBottomLeft,
                    this,
                    Style.ShaderRenderMode
                ));
            }
            else
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
        }

        // 4. Draw Text (rich layout: wrap, ellipsis, emoji fallback, optional scroll culling)
        if (HasTextContent && Style?.Text != null)
        {
            CalculateText();
            var layout = EnsureTextLayout();
            SKRect? clip = null;
            var overflow = Style.Text.Overflow;
            bool scrolling = ScrollsTextContent;
            if (scrolling || overflow == TextOverflow.Clip || overflow == TextOverflow.Ellipsis || overflow == TextOverflow.Wrap)
            {
                clip = new SKRect(0, 0, Math.Max(0, Transform.Computed.Width), Math.Max(0, Transform.Computed.Height));
            }
            var origin = TextPosition;
            if (scrolling)
            {
                origin.X -= TextScrollX;
                origin.Y -= TextScrollY;
            }
            cmds.Add(new DrawRichTextCommand(layout, origin, Style.Text.Paint, clip));
        }

        OnAfterStyleDraw(cmds);

        ledger.Record(DrawCommandKey, cmds);
    }

    /// <summary>
    /// Extension hook called during <see cref="RecordDrawCommands"/> after standard styling commands
    /// (shadow, backdrop blur, background fill/shader/image/svg, border, text) have been generated.
    /// Override in subclasses to append or prepend custom <see cref="DrawCommand"/> instances.
    /// Alternatively, override <see cref="RecordDrawCommands"/> entirely for full control over command recording.
    /// </summary>
    /// <param name="cmds">The list of draw commands to be submitted to the ledger.</param>
    protected virtual void OnAfterStyleDraw(List<DrawCommand> cmds) { }

    private SKRect TextBounds;
    private TextLayout? _textLayout;
    private float _textLayoutW = float.NaN, _textLayoutH = float.NaN;
    private string? _textLayoutKey;

    /// <summary>Override to feed mixed-style runs. Default is a single span from <see cref="Text"/>.</summary>
    protected virtual IReadOnlyList<TextSpan> EnumerateTextSpans()
    {
        if (string.IsNullOrEmpty(Text))
            return Array.Empty<TextSpan>();
        return new[] { new TextSpan(Text) };
    }

    protected bool HasTextContent => EnumerateTextSpans().Count > 0;

    protected void InvalidateTextLayout()
    {
        _textLayout = null;
        _textLayoutKey = null;
        _localBoundsDirty = true;
        InvalidateLayout();
        InvalidatePaint();
    }

    /// <summary>When true, text is laid out in full and offset by <see cref="TextScrollX"/> / <see cref="TextScrollY"/>.</summary>
    protected virtual bool ScrollsTextContent => false;
    protected virtual float TextScrollX => 0f;
    protected virtual float TextScrollY => 0f;

    protected TextLayout EnsureTextLayout(float constraintWidth = 0)
    {
        var paint = Style?.Text?.Paint;
        if (paint == null)
            return _textLayout ??= new TextLayout();

        float cw = Transform.Computed.Width;
        float ch = Transform.Computed.Height;
        if (cw <= 0 && constraintWidth > 0)
            cw = constraintWidth;

        float padLeft = Padding.Left + Style!.Text.Padding;
        float padRight = Padding.Right + Style.Text.Padding;
        float padTop = Padding.Top + Style.Text.Padding;
        float padBottom = Padding.Bottom + Style.Text.Padding;
        float innerW = Math.Max(1f, cw - padLeft - padRight);
        float innerH = Math.Max(1f, ch - padTop - padBottom);
        if (constraintWidth > 0)
            innerW = Math.Max(1f, Math.Min(innerW, constraintWidth - padLeft - padRight));

        var overflow = Style.Text.Overflow;
        int maxLines = Style.Text.MaxLines;
        bool scrolling = ScrollsTextContent;
        if (scrolling)
        {
            overflow = TextOverflow.Wrap;
            maxLines = int.MaxValue;
        }
        float maxW = (overflow == TextOverflow.Visible && maxLines <= 1 && !scrolling) ? float.MaxValue : innerW;
        float maxH = (overflow == TextOverflow.Visible && maxLines <= 1 && !scrolling) ? float.MaxValue : (scrolling ? float.MaxValue : innerH);

        string key = $"{Text}|{overflow}|{maxLines}|{innerW:0.#}|{(scrolling ? 0 : innerH):0.#}|{paint.TextSize:0.#}|{paint.Color}|{paint.Typeface?.FamilyName}|s{(scrolling ? 1 : 0)}";
        if (_textLayout != null && _textLayoutKey == key && _textLayoutW == innerW && _textLayoutH == innerH)
            return _textLayout;

        _textLayout = TextLayout.Build(EnumerateTextSpans(), paint, maxW, maxH, overflow, maxLines);
        _textLayoutKey = key;
        _textLayoutW = innerW;
        _textLayoutH = innerH;
        TextBounds = new SKRect(0, 0, _textLayout.Width, _textLayout.Height);
        return _textLayout;
    }

    private void CalculateTextBounds()
    {
        EnsureTextLayout();
    }

    internal void CalculateText()
    {
        if (Style?.Text == null || Style.Text.Paint == null)
            return;

        var cx = 0f;
        var cy = 0f;
        var cw = Transform.Computed.Width;
        var ch = Transform.Computed.Height;

        var layout = EnsureTextLayout();
        TextBounds = new SKRect(0, 0, layout.Width, layout.Height);

        float padLeft = Padding.Left + Style.Text.Padding;
        float padRight = Padding.Right + Style.Text.Padding;
        float padTop = Padding.Top + Style.Text.Padding;
        float padBottom = Padding.Bottom + Style.Text.Padding;
        float innerW = Math.Max(0f, cw - padLeft - padRight);
        float innerH = Math.Max(0f, ch - padTop - padBottom);

        if (ScrollsTextContent)
        {
            TextPosition.X = cx + padLeft;
            TextPosition.Y = cy + padTop;
            return;
        }

        TextPosition.X = Style.Text.Alignment switch
        {
            var x when
                x == TextAlign.Left ||
                x == TextAlign.TopLeft ||
                x == TextAlign.BottomLeft
                => cx + padLeft,
            var x when
                x == TextAlign.Right ||
                x == TextAlign.TopRight ||
                x == TextAlign.BottomRight
                => cx + cw - layout.Width - padRight,
            _ => cx + padLeft + (innerW - layout.Width) * 0.5f
        };

        TextPosition.Y = Style.Text.Alignment switch
        {
            var x when
                x == TextAlign.Top ||
                x == TextAlign.TopLeft ||
                x == TextAlign.TopRight
                => cy + padTop,
            var x when
                x == TextAlign.Bottom ||
                x == TextAlign.BottomLeft ||
                x == TextAlign.BottomRight
                => cy + ch - padBottom - layout.Height,
            _ => cy + padTop + (innerH - layout.Height) * 0.5f
        };
    }

    public void EvaluateVisibilityAndClipping()
    {
        var previous = ComputedVisibility;

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
                        // Became hidden: still dirty the last drawn area so it is erased cleanly
                        if (previous != Visibility.Hidden)
                            InvalidatePaint();
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

        // Scrolled into view: must repaint (including BackColor). Previously Hidden cards
        // were skipped and never re-dirtied when becoming visible again.
        if (previous == Visibility.Hidden && ComputedVisibility != Visibility.Hidden)
        {
            ClearRenderCache();
            InvalidatePaint();
        }
        else if (previous != Visibility.Hidden && ComputedVisibility == Visibility.Hidden)
        {
            InvalidatePaint();
        }
    }

    private void ParentTransformChanged(VisualElement e, Transform t)
    {
        if (LayoutMutationDepth > 0)
            return;

        Transform._transformDirty = true;
        CalculateText();
        MarkVisibilityClippingDirty();
        // Parent moved/resized: our LayoutChildren may need to re-run
        InvalidateLayout();
    }

    internal void ScheduleRender()
    {
        InvalidatePaint();
    }

    public void GetFocus()
    {
        if (ParentView != null && EffectiveInteractive)
            ParentView.SetActiveKeyboardElement(this);
    }

    public bool ContainsElement(VisualElement? element)
    {
        var cur = element;
        while (cur != null)
        {
            if (cur == this) return true;
            cur = cur.Parent;
        }
        return false;
    }

    public Vector2 PointToClient(float x, float y)
    {
        var globalMatrix = Transform.GetGlobalM44();

        float m00 = globalMatrix[0, 0];
        float m01 = globalMatrix[0, 1];
        float m03 = globalMatrix[0, 3];

        float m10 = globalMatrix[1, 0];
        float m11 = globalMatrix[1, 1];
        float m13 = globalMatrix[1, 3];

        float m30 = globalMatrix[3, 0];
        float m31 = globalMatrix[3, 1];
        float m33 = globalMatrix[3, 3];

        float A1 = x * m30 - m00;
        float B1 = x * m31 - m01;
        float C1 = m03 - x * m33;

        float A2 = y * m30 - m10;
        float B2 = y * m31 - m11;
        float C2 = m13 - y * m33;

        float D = A1 * B2 - B1 * A2;
        if (Math.Abs(D) > 1e-6f)
        {
            float localX = (C1 * B2 - B1 * C2) / D;
            float localY = (A1 * C2 - C1 * A2) / D;
            return new Vector2(localX, localY);
        }

        return new Vector2(
            x - Transform.Computed.X,
            y - Transform.Computed.Y
        );
    }

    internal SKImage? CachedBackdropBlur;
    internal SKRect CachedBackdropBlurBounds;
    internal SKImage? CachedShaderBackground;
    internal SKImage? CachedBorder;
    internal SKRect CachedBorderBounds;

    internal void ClearRenderCache()
    {
        CachedBackdropBlur?.Dispose();
        CachedBackdropBlur = null;
        CachedShaderBackground?.Dispose();
        CachedShaderBackground = null;
        CachedBorder?.Dispose();
        CachedBorder = null;
    }

    private bool _isDisposed = false;
    public bool IsDisposed => _isDisposed;

    public virtual void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        OnDisposing?.Invoke(this);

        if (HasPointerCapture)
        {
            ReleasePointer();
        }
        if (ParentView?.ActiveKeyboardElement == this)
        {
            ParentView.SetActiveKeyboardElement(null);
        }

        var childList = _children.ToArray();
        _children.Clear();
        foreach (var child in childList)
        {
            child.Dispose();
        }

        if (_Parent != null)
        {
            _Parent._children.Remove(this);
            _Parent = null;
        }

        if (ParentView != null)
        {
            ParentView.Elements.RemoveElement(this);
            RemovedFromView();
            _ParentView = null!;
        }

        ClearRenderCache();
        Transform.Dispose();
        paint.Dispose();
        _cachedRoundRect?.Dispose();
        _BackgroundImage?.Dispose();
        _BackgroundSvg?.Picture?.Dispose();
    }
}

public enum Visibility
{
    Visible,
    Clipped,
    Hidden
}