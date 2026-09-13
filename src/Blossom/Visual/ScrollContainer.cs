using System;
using System.Collections.Generic;
using SkiaSharp;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual.Enums;

namespace Blossom.Core.Visual;

public class ScrollContainer : VisualElement
{
    private float _scrollX = 0f;
    private float _scrollY = 0f;
    private float _targetScrollX = 0f;
    private float _targetScrollY = 0f;
    private bool _isAnimatingX = false;
    private bool _isAnimatingY = false;

    public bool SmoothScroll { get; set; } = true;
    public float ScrollDuration { get; set; } = 0.22f;
    public float ScrollDamping { get; set; } = 22f;
    public float ScrollStepY { get; set; } = 108f;
    public float ScrollStepX { get; set; } = 108f;

    public float ScrollX
    {
        get => _scrollX;
        set
        {
            float clamped = (OverflowX == OverflowMode.Scroll) ? Math.Clamp(value, 0, MaxScrollX) : 0f;
            _targetScrollX = clamped;
            _isAnimatingX = false;
            if (_scrollX != clamped)
            {
                _scrollX = clamped;
                OnScrollOffsetChanged();
            }
        }
    }

    public float ScrollY
    {
        get => _scrollY;
        set
        {
            float clamped = (OverflowY == OverflowMode.Scroll) ? Math.Clamp(value, 0, MaxScrollY) : 0f;
            _targetScrollY = clamped;
            _isAnimatingY = false;
            if (_scrollY != clamped)
            {
                _scrollY = clamped;
                OnScrollOffsetChanged();
            }
        }
    }

    protected virtual void OnScrollOffsetChanged()
    {
        MarkChildrenTransformDirty();
        MarkVisibilityClippingDirty();
        InvalidatePaint();

        VScrollbar?.InvalidatePaint();
        HScrollbar?.InvalidatePaint();
    }

    private float? _customContentWidth;
    public float? CustomContentWidth
    {
        get => _customContentWidth;
        set
        {
            if (_customContentWidth != value)
            {
                _customContentWidth = value;
                ClampScroll();
                InvalidateLayout();
            }
        }
    }

    private float? _customContentHeight;
    public float? CustomContentHeight
    {
        get => _customContentHeight;
        set
        {
            if (_customContentHeight != value)
            {
                _customContentHeight = value;
                ClampScroll();
                InvalidateLayout();
            }
        }
    }

    public void SetContentSize(float? width, float? height)
    {
        if (_customContentWidth == width && _customContentHeight == height)
            return;

        _customContentWidth = width;
        _customContentHeight = height;
        ClampScroll();
        InvalidateLayout();
    }

    public float MaxScrollX => (OverflowX == OverflowMode.Scroll) ? Math.Max(0, ContentWidth - Transform.Computed.Width) : 0f;
    public float MaxScrollY => (OverflowY == OverflowMode.Scroll) ? Math.Max(0, ContentHeight - Transform.Computed.Height) : 0f;

    public float ContentWidth
    {
        get
        {
            if (_customContentWidth.HasValue)
                return _customContentWidth.Value;

            float max = 0f;
            var children = Children;
            float containerX = Transform.Computed.X;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null || !child.Visible) continue;
                float childLocalRight = (child.Transform.Computed.X - containerX + ScrollX) + child.Transform.Computed.Width;
                if (childLocalRight > max) max = childLocalRight;
            }
            return max;
        }
    }

    public float ContentHeight
    {
        get
        {
            if (_customContentHeight.HasValue)
                return _customContentHeight.Value;

            float max = 0f;
            var children = Children;
            float containerY = Transform.Computed.Y;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null || !child.Visible) continue;
                float childLocalBottom = (child.Transform.Computed.Y - containerY + ScrollY) + child.Transform.Computed.Height;
                if (childLocalBottom > max) max = childLocalBottom;
            }
            return max;
        }
    }

    public ScrollbarVisibility ScrollbarVisibilityX { get; set; } = ScrollbarVisibility.Auto;
    public ScrollbarVisibility ScrollbarVisibilityY { get; set; } = ScrollbarVisibility.Auto;
    public ScrollbarVisibility ScrollbarVisibility
    {
        get => ScrollbarVisibilityY;
        set
        {
            ScrollbarVisibilityX = value;
            ScrollbarVisibilityY = value;
        }
    }

    public float ScrollbarThickness { get; set; } = 8f;
    public float ScrollbarRadius { get; set; } = 4f;
    public float ScrollbarPadding { get; set; } = 1.5f;
    public SKColor ScrollbarTrackColor { get; set; } = SKColors.Transparent;
    public SKColor ScrollbarThumbColor { get; set; } = new SKColor(160, 160, 160, 140);
    public SKColor ScrollbarThumbHoverColor { get; set; } = new SKColor(200, 200, 200, 200);
    public SKColor ScrollbarThumbDragColor { get; set; } = new SKColor(230, 230, 230, 240);

    internal ScrollbarV VScrollbar { get; }
    internal ScrollbarH HScrollbar { get; }

    public ScrollContainer()
    {
        OverflowX = OverflowMode.Scroll;
        OverflowY = OverflowMode.Scroll;

        // Unique names: command ledger keys by Name; empty container Name would collide as "_VScrollbar".
        string idSuffix = Id.ToString("N")[..8];
        VScrollbar = new ScrollbarV(this)
        {
            Name = $"ScrollV_{idSuffix}",
            Transform =
            {
                Anchor = Anchor.Top | Anchor.Right,
                FixedWidth = true,
                FixedHeight = false
            }
        };
        HScrollbar = new ScrollbarH(this)
        {
            Name = $"ScrollH_{idSuffix}",
            Transform =
            {
                Anchor = Anchor.Bottom | Anchor.Left,
                FixedWidth = false,
                FixedHeight = true
            }
        };
        // Parent after construct so transform Parent links correctly without AddChild (chrome only).
        VScrollbar.Parent = this;
        HScrollbar.Parent = this;

        Events.OnScroll += (sender, args) =>
        {
            bool isShift = ParentView?.Events.IsShiftDown ?? Browser.BrowserApp?.Events.IsShiftDown ?? false;

            float deltaX = args.Offset.X;
            float deltaY = args.Offset.Y;

            if ((isShift || OverflowY != OverflowMode.Scroll) && deltaX == 0f && deltaY != 0f)
            {
                deltaX = deltaY;
                deltaY = 0f;
            }

            if (OverflowY == OverflowMode.Scroll && deltaY != 0f && MaxScrollY > 0)
            {
                AnimateScrollBy(0, -deltaY * ScrollStepY);
            }

            if (OverflowX == OverflowMode.Scroll && deltaX != 0f && MaxScrollX > 0)
            {
                AnimateScrollBy(-deltaX * ScrollStepX, 0);
            }

            // Always mark handled when this scroller is the target so parents do not double-scroll (v1 nested policy).
            if (OverflowX == OverflowMode.Scroll || OverflowY == OverflowMode.Scroll)
            {
                args.Handled = true;
            }
        };
    }

    public void AnimateScrollTo(float targetX, float targetY)
    {
        if (!SmoothScroll)
        {
            ScrollX = targetX;
            ScrollY = targetY;
            return;
        }

        if (OverflowY == OverflowMode.Scroll && MaxScrollY > 0)
        {
            float clamped = Math.Clamp(targetY, 0, MaxScrollY);
            if (Math.Abs(clamped - _scrollY) > 0.1f)
            {
                _targetScrollY = clamped;
                _isAnimatingY = true;
                ScheduleRender();
            }
            else
            {
                _targetScrollY = clamped;
                _isAnimatingY = false;
            }
        }

        if (OverflowX == OverflowMode.Scroll && MaxScrollX > 0)
        {
            float clamped = Math.Clamp(targetX, 0, MaxScrollX);
            if (Math.Abs(clamped - _scrollX) > 0.1f)
            {
                _targetScrollX = clamped;
                _isAnimatingX = true;
                ScheduleRender();
            }
            else
            {
                _targetScrollX = clamped;
                _isAnimatingX = false;
            }
        }
    }

    public void AnimateScrollBy(float deltaX, float deltaY)
    {
        if (!SmoothScroll)
        {
            if (deltaX != 0f) ScrollX += deltaX;
            if (deltaY != 0f) ScrollY += deltaY;
            return;
        }

        if (OverflowY == OverflowMode.Scroll && deltaY != 0f && MaxScrollY > 0)
        {
            // If currently moving and user scrolls in the OPPOSITE direction:
            // immediately cancel momentum and reverse direction from the current position!
            float currentMoveDir = _isAnimatingY ? Math.Sign(_targetScrollY - _scrollY) : 0;
            float inputDir = Math.Sign(deltaY);

            float currentBase;
            if (_isAnimatingY && currentMoveDir != 0 && inputDir != 0 && inputDir != currentMoveDir)
            {
                currentBase = _scrollY;
            }
            else
            {
                currentBase = _isAnimatingY ? _targetScrollY : _scrollY;
            }

            float newTarget = Math.Clamp(currentBase + deltaY, 0, MaxScrollY);

            // Cap the lead ahead of current position to keep response crisp and avoid runaway scrolling
            float maxLead = ScrollStepY * 4f;
            if (newTarget - _scrollY > maxLead)
                newTarget = Math.Clamp(_scrollY + maxLead, 0, MaxScrollY);
            else if (_scrollY - newTarget > maxLead)
                newTarget = Math.Clamp(_scrollY - maxLead, 0, MaxScrollY);

            if (Math.Abs(newTarget - _scrollY) > 0.1f)
            {
                _targetScrollY = newTarget;
                _isAnimatingY = true;
                ScheduleRender();
            }
            else
            {
                _targetScrollY = newTarget;
                _isAnimatingY = false;
            }
        }

        if (OverflowX == OverflowMode.Scroll && deltaX != 0f && MaxScrollX > 0)
        {
            float currentMoveDir = _isAnimatingX ? Math.Sign(_targetScrollX - _scrollX) : 0;
            float inputDir = Math.Sign(deltaX);

            float currentBase;
            if (_isAnimatingX && currentMoveDir != 0 && inputDir != 0 && inputDir != currentMoveDir)
            {
                currentBase = _scrollX;
            }
            else
            {
                currentBase = _isAnimatingX ? _targetScrollX : _scrollX;
            }

            float newTarget = Math.Clamp(currentBase + deltaX, 0, MaxScrollX);

            float maxLead = ScrollStepX * 4f;
            if (newTarget - _scrollX > maxLead)
                newTarget = Math.Clamp(_scrollX + maxLead, 0, MaxScrollX);
            else if (_scrollX - newTarget > maxLead)
                newTarget = Math.Clamp(_scrollX - maxLead, 0, MaxScrollX);

            if (Math.Abs(newTarget - _scrollX) > 0.1f)
            {
                _targetScrollX = newTarget;
                _isAnimatingX = true;
                ScheduleRender();
            }
            else
            {
                _targetScrollX = newTarget;
                _isAnimatingX = false;
            }
        }
    }

    protected override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);

        if (_isAnimatingY)
        {
            float stepDt = (dt > 0.0001f && dt < 0.1f) ? dt : 0.016f;
            float blend = 1f - MathF.Exp(-ScrollDamping * stepDt);
            float nextY = _scrollY + (_targetScrollY - _scrollY) * blend;

            if (Math.Abs(_targetScrollY - nextY) < 0.25f)
            {
                nextY = _targetScrollY;
                _isAnimatingY = false;
            }

            if (Math.Abs(_scrollY - nextY) > 0.01f)
            {
                _scrollY = nextY;
                OnScrollOffsetChanged();
            }

            // Always schedule render on both moving frames and the settling frame so final position is drawn
            ScheduleRender();
        }

        if (_isAnimatingX)
        {
            float stepDt = (dt > 0.0001f && dt < 0.1f) ? dt : 0.016f;
            float blend = 1f - MathF.Exp(-ScrollDamping * stepDt);
            float nextX = _scrollX + (_targetScrollX - _scrollX) * blend;

            if (Math.Abs(_targetScrollX - nextX) < 0.25f)
            {
                nextX = _targetScrollX;
                _isAnimatingX = false;
            }

            if (Math.Abs(_scrollX - nextX) > 0.01f)
            {
                _scrollX = nextX;
                OnScrollOffsetChanged();
            }

            ScheduleRender();
        }
    }

    public void MarkChildrenTransformDirty()
    {
        var children = Children;
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child != null)
            {
                child.InvalidateSubtreeBounds();
            }
        }
    }

    private void ClampScroll()
    {
        float newX = (OverflowX == OverflowMode.Scroll) ? Math.Clamp(_scrollX, 0, MaxScrollX) : 0f;
        float newY = (OverflowY == OverflowMode.Scroll) ? Math.Clamp(_scrollY, 0, MaxScrollY) : 0f;
        _targetScrollX = (OverflowX == OverflowMode.Scroll) ? Math.Clamp(_targetScrollX, 0, MaxScrollX) : 0f;
        _targetScrollY = (OverflowY == OverflowMode.Scroll) ? Math.Clamp(_targetScrollY, 0, MaxScrollY) : 0f;

        if (newX != _scrollX || newY != _scrollY)
        {
            _scrollX = newX;
            _scrollY = newY;
            _isAnimatingX = false;
            _isAnimatingY = false;
            OnScrollOffsetChanged();
            ScheduleRender();
        }
    }

    protected override void OnSizeChanged(float width, float height)
    {
        base.OnSizeChanged(width, height);
        ClampScroll();
        InvalidateLayout();
    }

    protected override void LayoutChildren()
    {
        base.LayoutChildren();

        ClampScroll();

        float viewW = Transform.Computed.Width;
        float viewH = Transform.Computed.Height;
        // Transform.X/Y setters expect absolute (computed) coordinates, not parent-local.
        float originX = Transform.Computed.X;
        float originY = Transform.Computed.Y;

        bool showV = (OverflowY == OverflowMode.Scroll) && (
            ScrollbarVisibilityY == ScrollbarVisibility.Always ||
            (ScrollbarVisibilityY == ScrollbarVisibility.Auto && MaxScrollY > 0)
        );

        bool showH = (OverflowX == OverflowMode.Scroll) && (
            ScrollbarVisibilityX == ScrollbarVisibility.Always ||
            (ScrollbarVisibilityX == ScrollbarVisibility.Auto && MaxScrollX > 0)
        );

        VScrollbar.Visible = showV;
        HScrollbar.Visible = showH;

        float thickness = ScrollbarThickness;

        if (showV)
        {
            // Right edge of the viewport, absolute coords
            VScrollbar.Transform.Width = thickness;
            VScrollbar.Transform.Height = Math.Max(0, viewH - (showH ? thickness : 0f));
            VScrollbar.Transform.X = originX + viewW - thickness;
            VScrollbar.Transform.Y = originY;
            VScrollbar.Transform.Anchor = Anchor.Top | Anchor.Right;
            VScrollbar.Transform.FixedWidth = true;
            VScrollbar.Transform._transformDirty = true;
            VScrollbar.InvalidatePaint();
        }

        if (showH)
        {
            // Bottom edge of the viewport, absolute coords
            HScrollbar.Transform.Width = Math.Max(0, viewW - (showV ? thickness : 0f));
            HScrollbar.Transform.Height = thickness;
            HScrollbar.Transform.X = originX;
            HScrollbar.Transform.Y = originY + viewH - thickness;
            HScrollbar.Transform.Anchor = Anchor.Bottom | Anchor.Left;
            HScrollbar.Transform.FixedHeight = true;
            HScrollbar.Transform._transformDirty = true;
            HScrollbar.InvalidatePaint();
        }
    }

    internal override IEnumerable<VisualElement> GetVisualChildren()
    {
        foreach (var child in Children)
        {
            if (child != null)
                yield return child;
        }

        if (VScrollbar != null && VScrollbar.Visible)
            yield return VScrollbar;

        if (HScrollbar != null && HScrollbar.Visible)
            yield return HScrollbar;
    }

    public override void Dispose()
    {
        VScrollbar?.Dispose();
        HScrollbar?.Dispose();
        base.Dispose();
    }
}

internal class ScrollbarV : VisualElement
{
    private readonly ScrollContainer _container;
    private bool _isDragging = false;
    private float _dragStartMouseY;
    private float _dragStartScroll;
    private bool _isHovered = false;

    public ScrollbarV(ScrollContainer container)
    {
        _container = container;
        ZIndex = 100000;
        IsClipping = false;

        Events.OnMouseEnter += (el) =>
        {
            _isHovered = true;
            InvalidatePaint();
        };

        Events.OnMouseLeave += (el) =>
        {
            _isHovered = false;
            InvalidatePaint();
        };

        Events.OnMouseDown += (s, args) =>
        {
            if (args.Button != 0) return;

            float localY = args.Relative.Y;
            var (thumbY, thumbH) = GetThumbBounds();

            if (localY >= thumbY && localY <= thumbY + thumbH)
            {
                _isDragging = true;
                _dragStartMouseY = args.Global.Y;
                _dragStartScroll = _container.ScrollY;
                CapturePointer();
                args.Handled = true;
                InvalidatePaint();
            }
            else
            {
                float pageStep = _container.Transform.Computed.Height * 0.8f;
                if (localY < thumbY)
                {
                    _container.ScrollY -= pageStep;
                }
                else
                {
                    _container.ScrollY += pageStep;
                }
                args.Handled = true;
                InvalidatePaint();
            }
        };

        Events.OnMouseMove += (s, args) =>
        {
            if (_isDragging)
            {
                float deltaY = args.Global.Y - _dragStartMouseY;
                var (_, thumbH) = GetThumbBounds();
                float scrollable = Transform.Computed.Height - thumbH;
                if (scrollable > 0 && _container.MaxScrollY > 0)
                {
                    float scrollDelta = (deltaY / scrollable) * _container.MaxScrollY;
                    _container.ScrollY = _dragStartScroll + scrollDelta;
                }
                args.Handled = true;
                InvalidatePaint();
            }
        };

        Events.OnMouseUp += (s, args) =>
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleasePointer();
                args.Handled = true;
                InvalidatePaint();
            }
        };
    }

    internal (float Y, float Height) GetThumbBounds()
    {
        float trackH = Transform.Computed.Height;
        float contentH = _container.ContentHeight;
        float viewH = _container.Transform.Computed.Height;

        if (contentH <= 0 || viewH <= 0 || trackH <= 0)
            return (0, trackH);

        float thumbH = Math.Max(20f, trackH * (viewH / Math.Max(viewH, contentH)));
        if (thumbH > trackH) thumbH = trackH;

        float scrollable = trackH - thumbH;
        float thumbY = _container.MaxScrollY > 0 ? (_container.ScrollY / _container.MaxScrollY) * scrollable : 0f;

        return (thumbY, thumbH);
    }

    public override void RecordDrawCommands(CommandLedger ledger)
    {
        var cmds = new List<DrawCommand>();
        float w = Transform.Computed.Width;
        float h = Transform.Computed.Height;

        if (w <= 0 || h <= 0) return;

        var trackRect = new SKRect(0, 0, w, h);

        if (_container.ScrollbarTrackColor.Alpha > 0)
        {
            var trackPaint = new SKPaint
            {
                Color = _container.ScrollbarTrackColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            cmds.Add(new DrawRoundRectCommand(trackRect, _container.ScrollbarRadius, _container.ScrollbarRadius, _container.ScrollbarRadius, _container.ScrollbarRadius, trackPaint));
        }

        var (thumbY, thumbH) = GetThumbBounds();
        var pad = _container.ScrollbarPadding;
        var thumbRect = new SKRect(pad, thumbY, Math.Max(pad, w - pad), thumbY + thumbH);

        var thumbColor = _isDragging ? _container.ScrollbarThumbDragColor : (_isHovered ? _container.ScrollbarThumbHoverColor : _container.ScrollbarThumbColor);

        var thumbPaint = new SKPaint
        {
            Color = thumbColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        float r = Math.Min(_container.ScrollbarRadius, (w - pad * 2) / 2f);
        cmds.Add(new DrawRoundRectCommand(thumbRect, r, r, r, r, thumbPaint));

        ledger.Record(DrawCommandKey, cmds);
    }

    public override void RemovedFromView()
    {
        base.RemovedFromView();
        _isDragging = false;
        if (HasPointerCapture) ReleasePointer();
    }
}

internal class ScrollbarH : VisualElement
{
    private readonly ScrollContainer _container;
    private bool _isDragging = false;
    private float _dragStartMouseX;
    private float _dragStartScroll;
    private bool _isHovered = false;

    public ScrollbarH(ScrollContainer container)
    {
        _container = container;
        ZIndex = 100000;
        IsClipping = false;

        Events.OnMouseEnter += (el) =>
        {
            _isHovered = true;
            InvalidatePaint();
        };

        Events.OnMouseLeave += (el) =>
        {
            _isHovered = false;
            InvalidatePaint();
        };

        Events.OnMouseDown += (s, args) =>
        {
            if (args.Button != 0) return;

            float localX = args.Relative.X;
            var (thumbX, thumbW) = GetThumbBounds();

            if (localX >= thumbX && localX <= thumbX + thumbW)
            {
                _isDragging = true;
                _dragStartMouseX = args.Global.X;
                _dragStartScroll = _container.ScrollX;
                CapturePointer();
                args.Handled = true;
                InvalidatePaint();
            }
            else
            {
                float pageStep = _container.Transform.Computed.Width * 0.8f;
                if (localX < thumbX)
                {
                    _container.ScrollX -= pageStep;
                }
                else
                {
                    _container.ScrollX += pageStep;
                }
                args.Handled = true;
                InvalidatePaint();
            }
        };

        Events.OnMouseMove += (s, args) =>
        {
            if (_isDragging)
            {
                float deltaX = args.Global.X - _dragStartMouseX;
                var (_, thumbW) = GetThumbBounds();
                float scrollable = Transform.Computed.Width - thumbW;
                if (scrollable > 0 && _container.MaxScrollX > 0)
                {
                    float scrollDelta = (deltaX / scrollable) * _container.MaxScrollX;
                    _container.ScrollX = _dragStartScroll + scrollDelta;
                }
                args.Handled = true;
                InvalidatePaint();
            }
        };

        Events.OnMouseUp += (s, args) =>
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleasePointer();
                args.Handled = true;
                InvalidatePaint();
            }
        };
    }

    internal (float X, float Width) GetThumbBounds()
    {
        float trackW = Transform.Computed.Width;
        float contentW = _container.ContentWidth;
        float viewW = _container.Transform.Computed.Width;

        if (contentW <= 0 || viewW <= 0 || trackW <= 0)
            return (0, trackW);

        float thumbW = Math.Max(20f, trackW * (viewW / Math.Max(viewW, contentW)));
        if (thumbW > trackW) thumbW = trackW;

        float scrollable = trackW - thumbW;
        float thumbX = _container.MaxScrollX > 0 ? (_container.ScrollX / _container.MaxScrollX) * scrollable : 0f;

        return (thumbX, thumbW);
    }

    public override void RecordDrawCommands(CommandLedger ledger)
    {
        var cmds = new List<DrawCommand>();
        float w = Transform.Computed.Width;
        float h = Transform.Computed.Height;

        if (w <= 0 || h <= 0) return;

        var trackRect = new SKRect(0, 0, w, h);

        if (_container.ScrollbarTrackColor.Alpha > 0)
        {
            var trackPaint = new SKPaint
            {
                Color = _container.ScrollbarTrackColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            cmds.Add(new DrawRoundRectCommand(trackRect, _container.ScrollbarRadius, _container.ScrollbarRadius, _container.ScrollbarRadius, _container.ScrollbarRadius, trackPaint));
        }

        var (thumbX, thumbW) = GetThumbBounds();
        var pad = _container.ScrollbarPadding;
        var thumbRect = new SKRect(thumbX, pad, thumbX + thumbW, Math.Max(pad, h - pad));

        var thumbColor = _isDragging ? _container.ScrollbarThumbDragColor : (_isHovered ? _container.ScrollbarThumbHoverColor : _container.ScrollbarThumbColor);

        var thumbPaint = new SKPaint
        {
            Color = thumbColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        float r = Math.Min(_container.ScrollbarRadius, (h - pad * 2) / 2f);
        cmds.Add(new DrawRoundRectCommand(thumbRect, r, r, r, r, thumbPaint));

        ledger.Record(DrawCommandKey, cmds);
    }

    public override void RemovedFromView()
    {
        base.RemovedFromView();
        _isDragging = false;
        if (HasPointerCapture) ReleasePointer();
    }
}
