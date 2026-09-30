using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Primitives;

/// <summary>
/// Two panes with a divider. The first two participating children are the panes;
/// the host sizes them. <see cref="Ratio"/> is the first pane's share until the user drags
/// (when <see cref="IsResizable"/> is true). Style the divider through <see cref="Bar"/>.
/// </summary>
public class Split : LayoutElement
{
    private readonly SplitBar _bar;
    private float _ratio = 0.5f;
    private float? _firstPixels;
    private float _barSize = 6f;
    private float _minPane = 40f;
    private bool _resizable = true;
    private Orientation _orientation = Orientation.Horizontal;

    public Split()
    {
        _bar = new SplitBar(this);
        AddChild(_bar);
    }

    /// <summary>The divider between panes. Set <c>Bar.Style</c> for colour, roundness, etc.</summary>
    public VisualElement Bar => _bar;

    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation == value) return;
            _orientation = value;
            InvalidateLayout();
        }
    }

    /// <summary>First pane fraction of leftover space (0–1) when the bar has not been dragged.</summary>
    public float Ratio
    {
        get => _ratio;
        set
        {
            _ratio = Math.Clamp(value, 0f, 1f);
            _firstPixels = null;
            InvalidateLayout();
        }
    }

    /// <summary>Divider thickness in pixels. 0 hides the bar and places panes flush.</summary>
    public float BarSize
    {
        get => _barSize;
        set
        {
            if (_barSize == value) return;
            _barSize = Math.Max(0f, value);
            InvalidateLayout();
        }
    }

    public float MinPaneSize
    {
        get => _minPane;
        set
        {
            if (_minPane == value) return;
            _minPane = Math.Max(0f, value);
            InvalidateLayout();
        }
    }

    /// <summary>When false, panes keep <see cref="Ratio"/> (or last drag) and the bar does not capture the pointer.</summary>
    public bool IsResizable
    {
        get => _resizable;
        set
        {
            if (_resizable == value) return;
            _resizable = value;
            InvalidateLayout();
        }
    }

    internal void DragTo(float firstPixels)
    {
        _firstPixels = firstPixels;
        InvalidateLayout();
        ForceLayoutSubtree();
    }

    internal float InnerMainSize
    {
        get
        {
            return Orientation == Orientation.Horizontal
                ? Math.Max(0f, Transform.Width - Padding.Horizontal)
                : Math.Max(0f, Transform.Height - Padding.Vertical);
        }
    }

    protected override void LayoutChildren()
    {
        if (_bar.Parent != this)
            AddChild(_bar);

        VisualElement? a = null;
        VisualElement? b = null;
        foreach (var child in ParticipatingChildren())
        {
            if (child == _bar) continue;
            if (a == null) a = child;
            else if (b == null) { b = child; break; }
        }

        float innerW = Math.Max(0f, Transform.Width - Padding.Horizontal);
        float innerH = Math.Max(0f, Transform.Height - Padding.Vertical);
        float ox = Transform.AbsoluteX + Padding.Left;
        float oy = Transform.AbsoluteY + Padding.Top;
        bool horizontal = Orientation == Orientation.Horizontal;
        float main = horizontal ? innerW : innerH;
        float bar = _barSize <= 0f ? 0f : Math.Min(_barSize, Math.Max(0f, main));
        float leftover = Math.Max(0f, main - bar);
        float min = Math.Min(_minPane, leftover * 0.5f);
        float first = _firstPixels ?? leftover * _ratio;
        first = Math.Clamp(first, min, Math.Max(min, leftover - min));
        float second = Math.Max(0f, leftover - first);
        _firstPixels = first;

        if (horizontal)
        {
            if (a != null) Place(a, ox, oy, first, innerH);
            Place(_bar, ox + first, oy, bar, innerH);
            if (b != null) Place(b, ox + first + bar, oy, second, innerH);
        }
        else
        {
            if (a != null) Place(a, ox, oy, innerW, first);
            Place(_bar, ox, oy + first, innerW, bar);
            if (b != null) Place(b, ox, oy + first + bar, innerW, second);
        }

        bool showBar = b != null && bar > 0f;
        _bar.Visible = showBar;
        _bar.IsClickthrough = !IsResizable;
        if (showBar && IsResizable)
            _bar.Cursor = horizontal ? StandardCursor.HResize : StandardCursor.VResize;
        else
            _bar.Cursor = StandardCursor.Default;
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        float w = Transform.Width > 0 ? Transform.Width : (maxWidth > 0 ? maxWidth : 0);
        float h = Transform.Height > 0 ? Transform.Height : (maxHeight > 0 ? maxHeight : 0);
        return new SKSize(w + Padding.Horizontal, h + Padding.Vertical);
    }

    private sealed class SplitBar : VisualElement
    {
        private readonly Split _host;
        private bool _dragging;
        private float _startFirst;
        private float _startPos;

        public SplitBar(Split host)
        {
            _host = host;
            Name = "SplitBar";
            Style.BackColor = new SKColor(60, 60, 68);
            Events.OnMouseDown += (_, e) =>
            {
                if (e.Button != 0 || !EffectiveInteractive || !_host.IsResizable) return;
                _dragging = true;
                _startFirst = _host._firstPixels ?? (_host.InnerMainSize * _host.Ratio);
                _startPos = _host.Orientation == Orientation.Horizontal ? e.Global.X : e.Global.Y;
                CapturePointer();
                e.Handled = true;
            };
            Events.OnMouseMove += (_, e) =>
            {
                if (!_dragging) return;
                float now = _host.Orientation == Orientation.Horizontal ? e.Global.X : e.Global.Y;
                _host.DragTo(_startFirst + (now - _startPos));
                e.Handled = true;
            };
            Events.OnMouseUp += (_, e) =>
            {
                if (e.Button != 0 || !_dragging) return;
                _dragging = false;
                ReleasePointer();
                e.Handled = true;
            };
        }
    }
}
