using System;
using Blossom.Core.Input;
using Blossom.Primitives;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>Horizontal slider on <see cref="Control"/>: drag with pointer capture, arrows to step.</summary>
public sealed class Slider : Control
{
    private float _value;
    private bool _dragging;

    public Slider(float min = 0f, float max = 100f, float value = 50f, float step = 1f)
    {
        Min = min;
        Max = max;
        Step = step;
        _value = Snap(value);
        Cursor = StandardCursor.Hand;
        ReceivesKeyboard = true;
        Transform.Height = 22;

        Events.OnMouseDown += (_, e) =>
        {
            if (e.Button != 0 || !Enabled) return;
            _dragging = true;
            CapturePointer();
            SetFromX(e.Relative.X);
            e.Handled = true;
        };
        Events.OnMouseMove += (_, e) =>
        {
            if (_dragging && HasPointerCapture) SetFromX(e.Relative.X);
        };
        Events.OnMouseUp += (_, e) =>
        {
            if (!_dragging) return;
            _dragging = false;
            ReleasePointer();
            InvalidatePaint();
        };
        Events.OnKeyDown += (KeyEvent e) =>
        {
            if (e.Key == Key.Left || e.Key == Key.Down) { Value -= Step; e.Handled = true; }
            if (e.Key == Key.Right || e.Key == Key.Up) { Value += Step; e.Handled = true; }
        };

        this.UseStyle("slider");
    }

    public float Min { get; }
    public float Max { get; }
    public float Step { get; }

    public SKColor TrackColor;
    public SKColor FillColor;
    public SKColor ThumbColor;

    public event Action<float>? ValueChanged;

    public float Value
    {
        get => _value;
        set
        {
            float v = Snap(value);
            if (v == _value) return;
            _value = v;
            InvalidatePaint();
            ValueChanged?.Invoke(v);
        }
    }

    private float Fraction => Max > Min ? (_value - Min) / (Max - Min) : 0f;

    private float Snap(float v)
    {
        v = Math.Clamp(v, Min, Max);
        return Step > 0 ? MathF.Round((v - Min) / Step) * Step + Min : v;
    }

    private void SetFromX(float x)
    {
        float f = Math.Clamp((x - 8f) / Math.Max(1f, W - 16f), 0f, 1f);
        Value = Min + f * (Max - Min);
    }

    protected override void OnStateChanged() => InvalidatePaint();

    protected override void Paint(SKCanvas c)
    {
        float cy = H / 2f;
        float left = 8f, right = W - 8f;
        float x = left + (right - left) * Fraction;
        using var paint = new SKPaint { IsAntialias = true, Color = TrackColor };
        c.DrawRoundRect(new SKRect(left, cy - 3f, right, cy + 3f), 3f, 3f, paint);
        paint.Color = FillColor;
        c.DrawRoundRect(new SKRect(left, cy - 3f, x, cy + 3f), 3f, 3f, paint);

        float r = _dragging || IsHovered ? 9f : 8f;
        if (_dragging || HasFocus)
        {
            paint.Color = FillColor.WithAlpha(50);
            c.DrawCircle(x, cy, r + 5f, paint);
        }
        paint.Color = ThumbColor;
        c.DrawCircle(x, cy, r, paint);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 2f;
        paint.Color = FillColor;
        c.DrawCircle(x, cy, r - 1f, paint);
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
        => new(maxWidth > 0 ? maxWidth : 200f, 22f);
}

/// <summary>Animated progress bar on <see cref="Control"/>; eases toward <see cref="Value"/>.</summary>
public sealed class ProgressBar : Control
{
    private float _target;
    private float _shown = -1f;

    public ProgressBar(float thickness = 8f)
    {
        IsClickthrough = true;
        Transform.Height = thickness;
        this.UseStyle("progress");
    }

    public SKColor TrackColor;
    public SKColor FillColor;

    /// <summary>0..1</summary>
    public float Value
    {
        get => _target;
        set
        {
            _target = Math.Clamp(value, 0f, 1f);
            InvalidatePaint();
        }
    }

    protected override void OnUpdate(float dt)
    {
        if (_shown < 0f) { _shown = _target; return; }
        if (Math.Abs(_shown - _target) < 0.002f)
        {
            if (_shown != _target) { _shown = _target; InvalidatePaint(); }
            return;
        }
        _shown += (_target - _shown) * Math.Min(1f, dt * 10f);
        InvalidatePaint();
    }

    protected override void Paint(SKCanvas c)
    {
        float v = _shown < 0f ? _target : _shown;
        float r = H / 2f;
        using var paint = new SKPaint { IsAntialias = true, Color = TrackColor };
        c.DrawRoundRect(new SKRect(0, 0, W, H), r, r, paint);
        if (v <= 0f) return;
        paint.Color = FillColor;
        c.DrawRoundRect(new SKRect(0, 0, Math.Max(H, W * v), H), r, r, paint);
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
        => new(maxWidth > 0 ? maxWidth : 120f, Transform.Height);
}
