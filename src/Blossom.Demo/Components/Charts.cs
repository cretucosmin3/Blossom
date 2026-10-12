using System;
using Blossom.Primitives;
using SkiaSharp;

namespace Blossom.Testing.Components;

public struct ChartPalette
{
    public SKColor Bar;
    public SKColor BarHover;
    public SKColor Grid;
    public SKColor Label;
    public SKColor TooltipBack;
    public SKColor TooltipText;
}

/// <summary>
/// Bar chart drawn straight to Skia on a <see cref="Control"/>. Bars ease to new data,
/// and hovering shows a value tooltip: hit-testing is plain math on local coordinates.
/// </summary>
public sealed class BarChart : Control
{
    private float[] _values = Array.Empty<float>();
    private float[] _shown = Array.Empty<float>();
    private string[] _labels = Array.Empty<string>();
    private int _hover = -1;

    public BarChart()
    {
        Transform.Height = 220;
        Events.OnMouseMove += (_, e) => SetHover(IndexAt(e.Relative.X, e.Relative.Y));
        Events.OnMouseLeave += _ => SetHover(-1);
        this.UseStyle("chart");
    }

    public ChartPalette Palette;
    public Func<float, string> Format { get; set; } = v => v.ToString("0");

    public void SetData(float[] values, string[] labels)
    {
        _labels = labels;
        _values = values;
        // A different bar count is a different series (days vs months): grow the new bars from zero
        // instead of easing Monday out of January's value.
        if (_shown.Length != values.Length)
            _shown = new float[values.Length];
        InvalidatePaint();
    }

    private const float AxisH = 24f;
    private const float TopPad = 28f;

    /// <summary>Axis top. Includes the values still easing out, so bars never outgrow the plot mid-animation.</summary>
    private float Peak
    {
        get
        {
            float max = 1f;
            foreach (var v in _values) max = Math.Max(max, v);
            foreach (var v in _shown) max = Math.Max(max, v);
            return NiceCeiling(max);
        }
    }

    private static float NiceCeiling(float v)
    {
        float mag = MathF.Pow(10, MathF.Floor(MathF.Log10(v)));
        float n = v / mag;
        float nice = n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10;
        return nice * mag;
    }

    private (float x, float w) Slot(int i)
    {
        float slot = W / Math.Max(1, _values.Length);
        float bw = Math.Min(42f, slot * 0.58f);
        return (slot * i + (slot - bw) / 2f, bw);
    }

    private int IndexAt(float x, float y)
    {
        if (_values.Length == 0 || y < 0 || y > H) return -1;
        int i = (int)(x / (W / _values.Length));
        return i >= 0 && i < _values.Length ? i : -1;
    }

    private void SetHover(int i)
    {
        if (_hover == i) return;
        _hover = i;
        InvalidatePaint();
    }

    protected override void OnUpdate(float dt)
    {
        bool moving = false;
        for (int i = 0; i < _shown.Length; i++)
        {
            float d = _values[i] - _shown[i];
            if (d == 0f) continue;
            if (Math.Abs(d) < 0.01f) { _shown[i] = _values[i]; moving = true; continue; }
            _shown[i] += d * Math.Min(1f, dt * 9f);
            moving = true;
        }
        if (moving) InvalidatePaint();
    }

    protected override void Paint(SKCanvas c)
    {
        float plotH = H - AxisH - TopPad;
        float peak = Peak;
        // Never paint outside the element: the engine only repaints our bounds, so stray pixels would linger.
        c.ClipRect(new SKRect(0, 0, W, H));
        using var paint = new SKPaint { IsAntialias = true };
        var small = Ui.Font(11f);

        paint.Color = Palette.Grid;
        paint.StrokeWidth = 1f;
        for (int g = 0; g <= 4; g++)
        {
            float y = MathF.Round(TopPad + plotH * g / 4f) + 0.5f;
            c.DrawLine(0, y, W, y, paint);
        }

        for (int i = 0; i < _values.Length; i++)
        {
            var (x, bw) = Slot(i);
            float h = Math.Clamp(plotH * (_shown[i] / peak), 2f, plotH);
            float top = TopPad + plotH - h;
            paint.Color = i == _hover ? Palette.BarHover : Palette.Bar;
            using var rr = new SKRoundRect();
            rr.SetRectRadii(new SKRect(x, top, x + bw, TopPad + plotH), new[]
            {
                new SKPoint(6, 6), new SKPoint(6, 6), new SKPoint(0, 0), new SKPoint(0, 0)
            });
            c.DrawRoundRect(rr, paint);

            if (i < _labels.Length)
            {
                paint.Color = Palette.Label;
                c.DrawText(_labels[i], MathF.Round(x + bw / 2f), H - 6f, SKTextAlign.Center, small, paint);
            }
        }

        if (_hover >= 0 && _hover < _values.Length)
        {
            var (x, bw) = Slot(_hover);
            float h = Math.Clamp(plotH * (_shown[_hover] / peak), 2f, plotH);
            string text = Format(_values[_hover]);
            var font = Ui.Font(12f, 600);
            float tw = Blossom.Utils.Text.MeasureWidth(text, font) + 16f;
            float tx = Math.Clamp(x + bw / 2f - tw / 2f, 0f, W - tw);
            float ty = Math.Max(0f, TopPad + plotH - h - 30f);
            paint.Color = Palette.TooltipBack;
            c.DrawRoundRect(new SKRect(tx, ty, tx + tw, ty + 24f), 6f, 6f, paint);
            paint.Color = Palette.TooltipText;
            c.DrawText(text, MathF.Round(tx + tw / 2f), Blossom.Utils.Text.Baseline(font, ty + 12f), SKTextAlign.Center, font, paint);
        }
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
        => new(maxWidth > 0 ? maxWidth : 400f, Transform.Height);
}

/// <summary>Tiny trend line with a soft gradient fill.</summary>
public sealed class Sparkline : Control
{
    private float[] _values = Array.Empty<float>();

    public Sparkline()
    {
        IsClickthrough = true;
        Transform.Height = 36;
    }

    public SKColor LineColor { get; set; }

    public float[] Values
    {
        get => _values;
        set
        {
            _values = value ?? Array.Empty<float>();
            InvalidatePaint();
        }
    }

    protected override void Paint(SKCanvas c)
    {
        if (_values.Length < 2) return;
        c.ClipRect(new SKRect(0, 0, W, H));
        float min = float.MaxValue, max = float.MinValue;
        foreach (var v in _values) { min = Math.Min(min, v); max = Math.Max(max, v); }
        float range = Math.Max(0.001f, max - min);

        using var line = new SKPath();
        for (int i = 0; i < _values.Length; i++)
        {
            float x = W * i / (_values.Length - 1);
            float y = 3f + (H - 6f) * (1f - (_values[i] - min) / range);
            if (i == 0) line.MoveTo(x, y); else line.LineTo(x, y);
        }

        using var fill = new SKPath(line);
        fill.LineTo(W, H);
        fill.LineTo(0, H);
        fill.Close();

        using var paint = new SKPaint { IsAntialias = true };
        paint.Shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(0, H),
            new[] { LineColor.WithAlpha(70), LineColor.WithAlpha(0) }, null, SKShaderTileMode.Clamp);
        c.DrawPath(fill, paint);
        paint.Shader = null;

        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 2f;
        paint.StrokeJoin = SKStrokeJoin.Round;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.Color = LineColor;
        c.DrawPath(line, paint);
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
        => new(maxWidth > 0 ? maxWidth : 100f, Transform.Height);
}
