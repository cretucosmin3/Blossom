using System;
using System.Collections.Generic;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual.Enums;
using SkiaSharp;

namespace Blossom.Core.Visual;

/// <summary>
/// Text box with clipping / ellipsis, word wrap, emoji font fallback, and optional scrolling.
/// Off-screen runs are culled. Toggle <see cref="Scrollable"/> to switch between ellipsis and scroll.
/// </summary>
public class RichBox : VisualElement
{
    private const float ScrollbarW = 5f;
    private const float ScrollStep = 54f;

    private readonly List<TextSpan> _spans = new();
    private bool _scrollable;
    private float _scrollY;
    private TextOverflow _savedOverflow = TextOverflow.Ellipsis;
    private int _savedMaxLines = 1;
    private bool _draggingBar;
    private float _dragThumbOffset;
    private SKRect _thumbRect;

    public RichBox()
    {
        Name = "RichBox";
        Overflow = OverflowMode.Clip;
        Style = new ElementStyle
        {
            BackColor = SKColors.Transparent,
            Text = new TextStyle
            {
                Color = SKColors.White,
                Size = 13,
                Weight = 400,
                Alignment = TextAlign.Left,
                Overflow = TextOverflow.Ellipsis,
                MaxLines = 1,
                Padding = 0
            }
        };

        Events.OnScroll += OnWheel;
        Events.OnMouseDown += OnBarDown;
        Events.OnMouseMove += OnBarMove;
        Events.OnMouseUp += OnBarUp;
    }

    public IReadOnlyList<TextSpan> Spans => _spans;

    /// <summary>
    /// When true, text wraps to the box width and the overflow scrolls instead of ellipsizing.
    /// Off-screen lines are not drawn.
    /// </summary>
    public bool Scrollable
    {
        get => _scrollable;
        set
        {
            if (_scrollable == value)
                return;
            _scrollable = value;
            if (value)
            {
                _savedOverflow = Style?.Text?.Overflow ?? TextOverflow.Ellipsis;
                _savedMaxLines = Style?.Text?.MaxLines ?? 1;
                Overflow = OverflowMode.Clip;
                if (Style?.Text != null)
                {
                    Style.Text.Overflow = TextOverflow.Wrap;
                    Style.Text.MaxLines = int.MaxValue;
                }
            }
            else
            {
                _scrollY = 0f;
                _draggingBar = false;
                if (Style?.Text != null)
                {
                    Style.Text.Overflow = _savedOverflow;
                    Style.Text.MaxLines = Math.Max(1, _savedMaxLines);
                }
            }
            InvalidateTextLayout();
        }
    }

    public float ScrollY
    {
        get => _scrollY;
        set
        {
            float next = Math.Clamp(value, 0f, MaxScrollY);
            if (Math.Abs(_scrollY - next) < 0.05f)
                return;
            _scrollY = next;
            InvalidatePaint();
        }
    }

    public float MaxScrollY
    {
        get
        {
            if (!_scrollable)
                return 0f;
            float view = ViewportHeight;
            float content = EnsureTextLayout().Height;
            return Math.Max(0f, content - view);
        }
    }

    public bool CanScroll => _scrollable && MaxScrollY > 0.5f;

    public TextOverflow TextOverflow
    {
        get => Style?.Text?.Overflow ?? TextOverflow.Ellipsis;
        set
        {
            if (Style?.Text == null) return;
            if (!_scrollable)
                _savedOverflow = value;
            Style.Text.Overflow = _scrollable ? TextOverflow.Wrap : value;
            InvalidateTextLayout();
        }
    }

    public int MaxLines
    {
        get => _scrollable ? _savedMaxLines : (Style?.Text?.MaxLines ?? 1);
        set
        {
            if (Style?.Text == null) return;
            int v = Math.Clamp(value, 1, int.MaxValue);
            _savedMaxLines = v;
            if (!_scrollable)
                Style.Text.MaxLines = v;
            InvalidateTextLayout();
        }
    }

    protected override bool ScrollsTextContent => _scrollable;
    protected override float TextScrollY => _scrollY;

    public RichBox Add(string text, SKColor? color = null, float? size = null, int? weight = null)
    {
        _spans.Add(new TextSpan
        {
            Text = text ?? "",
            Color = color,
            Size = size,
            Weight = weight
        });
        InvalidateTextLayout();
        return this;
    }

    public void ClearSpans()
    {
        _spans.Clear();
        InvalidateTextLayout();
    }

    public void SetText(string text)
    {
        _spans.Clear();
        Text = text ?? "";
    }

    protected override IReadOnlyList<TextSpan> EnumerateTextSpans()
    {
        if (_spans.Count > 0)
            return _spans;
        return base.EnumerateTextSpans();
    }

    protected override void OnAfterStyleDraw(List<DrawCommand> cmds)
    {
        if (!CanScroll)
            return;
        cmds.Add(new DrawCallbackCommand(DrawScrollbar));
    }

    private float ViewportHeight
    {
        get
        {
            float pad = (Style?.Text?.Padding ?? 0f);
            return Math.Max(1f, Transform.Computed.Height - Padding.Vertical - pad * 2f);
        }
    }

    private void OnWheel(object sender, MouseScrollEventArgs e)
    {
        if (!_scrollable || MaxScrollY <= 0f)
            return;
        ScrollY -= e.Offset.Y * ScrollStep;
        e.Handled = true;
    }

    private SKRect TrackRect()
    {
        float w = Transform.Computed.Width;
        float h = Transform.Computed.Height;
        return new SKRect(w - ScrollbarW - 1f, 2f, w - 1f, h - 2f);
    }

    private SKRect ThumbRect()
    {
        var track = TrackRect();
        float content = Math.Max(ViewportHeight + MaxScrollY, 1f);
        float thumbH = Math.Clamp(track.Height * (ViewportHeight / content), 16f, track.Height);
        float travel = Math.Max(0f, track.Height - thumbH);
        float t = MaxScrollY > 0f ? _scrollY / MaxScrollY : 0f;
        float y = track.Top + travel * t;
        return new SKRect(track.Left, y, track.Right, y + thumbH);
    }

    private void OnBarDown(object sender, MouseEventArgs e)
    {
        if (!_scrollable || e.Button != 0 || MaxScrollY <= 0f)
            return;
        var thumb = ThumbRect();
        _thumbRect = thumb;
        if (thumb.Contains(e.Relative.X, e.Relative.Y))
        {
            _draggingBar = true;
            _dragThumbOffset = e.Relative.Y - thumb.Top;
            CapturePointer();
            e.Handled = true;
            return;
        }
        var track = TrackRect();
        if (track.Contains(e.Relative.X, e.Relative.Y))
        {
            float t = (e.Relative.Y - track.Top) / Math.Max(1f, track.Height);
            ScrollY = t * MaxScrollY;
            e.Handled = true;
        }
    }

    private void OnBarMove(object sender, MouseEventArgs e)
    {
        if (!_draggingBar)
            return;
        var track = TrackRect();
        float thumbH = _thumbRect.Height;
        float travel = Math.Max(1f, track.Height - thumbH);
        float y = e.Relative.Y - _dragThumbOffset - track.Top;
        ScrollY = (y / travel) * MaxScrollY;
        e.Handled = true;
    }

    private void OnBarUp(object sender, MouseEventArgs e)
    {
        if (!_draggingBar)
            return;
        _draggingBar = false;
        ReleasePointer();
        e.Handled = true;
    }

    private void DrawScrollbar(SKCanvas canvas)
    {
        if (!CanScroll)
            return;
        var track = TrackRect();
        var thumb = ThumbRect();
        _thumbRect = thumb;

        using var trackPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 18),
            IsAntialias = true
        };
        canvas.DrawRoundRect(track, 2f, 2f, trackPaint);

        using var thumbPaint = new SKPaint
        {
            Color = _draggingBar ? new SKColor(255, 255, 255, 160) : new SKColor(255, 255, 255, 90),
            IsAntialias = true
        };
        canvas.DrawRoundRect(thumb, 2f, 2f, thumbPaint);
    }
}
