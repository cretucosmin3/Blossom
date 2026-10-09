using System;
using Blossom.Primitives;
using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>Colours a theme recipe hands to custom-painted toggles.</summary>
public struct TogglePalette
{
    public SKColor On;
    public SKColor Off;
    public SKColor OffBorder;
    public SKColor Thumb;
    public SKColor Mark;
    public SKColor Label;
    public SKColor Hover;
}

/// <summary>Shared painting for <see cref="Switch"/> and <see cref="Checkbox"/>: animated progress + optional label.</summary>
public abstract class PaintedToggle : ToggleBase
{
    protected float Progress;
    private string _label;

    protected PaintedToggle(string? label, bool isChecked, string styleName) : base(isChecked)
    {
        _label = label ?? "";
        Progress = isChecked ? 1f : 0f;
        StyleName = styleName;
        Transform.Height = 22;
        this.UseStyle(styleName);
    }

    protected string StyleName { get; }

    public TogglePalette Palette;

    public string Label
    {
        get => _label;
        set
        {
            _label = value ?? "";
            InvalidateLayout();
            InvalidatePaint();
        }
    }

    protected abstract float GlyphWidth { get; }

    protected override void OnStateChanged() => this.UseStyle(StyleName);

    protected override void OnCheckedChanged(bool isChecked) => InvalidatePaint();

    protected override void OnUpdate(float dt)
    {
        float target = IsChecked ? 1f : 0f;
        if (Progress == target) return;
        float step = dt / 0.14f;
        Progress = target > Progress ? Math.Min(target, Progress + step) : Math.Max(target, Progress - step);
        InvalidatePaint();
    }

    protected void PaintLabel(SKCanvas c)
    {
        if (_label.Length == 0) return;
        var font = Ui.Font(13.5f);
        using var paint = new SKPaint { Color = Enabled ? Palette.Label : Palette.Label.WithAlpha(110), IsAntialias = true };
        float baseline = Blossom.Utils.Text.Baseline(font, H / 2f);
        c.DrawText(_label, GlyphWidth + 10f, baseline, SKTextAlign.Left, font, paint);
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        float w = GlyphWidth;
        if (_label.Length > 0)
            w += 10f + Blossom.Utils.Text.MeasureWidth(_label, Ui.Font(13.5f));
        return new SKSize(w, 22f);
    }
}

public sealed class Switch : PaintedToggle
{
    public Switch(string? label = null, bool isChecked = false) : base(label, isChecked, "toggle") { }

    protected override float GlyphWidth => 36f;

    protected override void Paint(SKCanvas c)
    {
        float t = Ease(Progress);
        float top = (H - 20f) / 2f;
        var track = new SKRect(0, top, 36f, top + 20f);
        using var paint = new SKPaint { IsAntialias = true };

        var off = IsHovered ? Palette.Hover : Palette.Off;
        paint.Color = Styling.Look.Mix(off, Palette.On, t);
        if (!Enabled) paint.Color = paint.Color.WithAlpha(110);
        c.DrawRoundRect(track, 10f, 10f, paint);

        float cx = 10f + 16f * t;
        paint.Color = new SKColor(0, 0, 0, 40);
        c.DrawCircle(cx, top + 10.6f, 8f, paint);
        paint.Color = Palette.Thumb;
        c.DrawCircle(cx, top + 10f, 8f, paint);

        PaintLabel(c);
    }

    private static float Ease(float t) => t * t * (3f - 2f * t);
}

public sealed class Checkbox : PaintedToggle
{
    public Checkbox(string? label = null, bool isChecked = false) : base(label, isChecked, "toggle") { }

    protected override float GlyphWidth => 18f;

    protected override void Paint(SKCanvas c)
    {
        float top = (H - 18f) / 2f;
        var box = new SKRect(0.5f, top + 0.5f, 17.5f, top + 17.5f);
        using var paint = new SKPaint { IsAntialias = true };

        float t = Progress;
        paint.Color = Styling.Look.Mix(Palette.On.WithAlpha(0), Palette.On, t);
        c.DrawRoundRect(box, 5f, 5f, paint);

        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1.2f;
        paint.Color = Styling.Look.Mix(IsHovered ? Palette.Hover : Palette.OffBorder, Palette.On, t);
        c.DrawRoundRect(box, 5f, 5f, paint);

        if (t > 0.01f)
        {
            paint.Color = Palette.Mark.WithAlpha((byte)(255 * t));
            paint.StrokeWidth = 2f;
            paint.StrokeCap = SKStrokeCap.Round;
            paint.StrokeJoin = SKStrokeJoin.Round;
            using var path = new SKPath();
            path.MoveTo(4.5f, top + 9.2f);
            path.LineTo(7.6f, top + 12.3f);
            path.LineTo(13.5f, top + 5.8f);
            c.DrawPath(path, paint);
        }

        PaintLabel(c);
    }
}

/// <summary>
/// Pill-shaped single choice (Dark | Light, Week | Month). Custom painted on <see cref="Control"/>:
/// the active pill slides between segments.
/// </summary>
public sealed class Segmented : Control
{
    private readonly string[] _options;
    private int _selected;
    private int _hover = -1;
    private float _pill = -1f;

    public Segmented(params string[] options)
    {
        _options = options;
        Cursor = Silk.NET.Input.StandardCursor.Hand;
        Transform.Height = 32;

        Events.OnMouseMove += (_, e) => SetHoverIndex(IndexAt(e.Relative.X));
        Events.OnMouseLeave += _ => SetHoverIndex(-1);
        Events.OnMouseDown += (_, e) =>
        {
            if (e.Button != 0 || !Enabled) return;
            int i = IndexAt(e.Relative.X);
            if (i >= 0) SelectedIndex = i;
            e.Handled = true;
        };

        this.UseStyle("segmented");
    }

    public event Action<int>? SelectionChanged;

    public SKColor PillColor;
    public SKColor TextColor;
    public SKColor ActiveTextColor;

    public int SelectedIndex
    {
        get => _selected;
        set
        {
            value = Math.Clamp(value, 0, _options.Length - 1);
            if (_selected == value) return;
            _selected = value;
            InvalidatePaint();
            SelectionChanged?.Invoke(value);
        }
    }

    public string SelectedOption => _options[_selected];

    private void SetHoverIndex(int i)
    {
        if (_hover == i) return;
        _hover = i;
        InvalidatePaint();
    }

    private float SegmentWidth => _options.Length == 0 ? 0 : (W - 6f) / _options.Length;

    private int IndexAt(float x)
    {
        if (x < 0 || x > W || _options.Length == 0) return -1;
        return Math.Clamp((int)((x - 3f) / SegmentWidth), 0, _options.Length - 1);
    }

    protected override void OnUpdate(float dt)
    {
        float target = 3f + _selected * SegmentWidth;
        if (_pill < 0f) { _pill = target; return; }
        if (Math.Abs(_pill - target) < 0.25f)
        {
            if (_pill != target) { _pill = target; InvalidatePaint(); }
            return;
        }
        _pill += (target - _pill) * Math.Min(1f, dt * 16f);
        InvalidatePaint();
    }

    protected override void Paint(SKCanvas c)
    {
        float segW = SegmentWidth;
        float pillX = _pill < 0f ? 3f + _selected * segW : _pill;
        using var paint = new SKPaint { IsAntialias = true, Color = PillColor };
        float r = Math.Max(0f, Style.Border.Roundness - 3f);
        c.DrawRoundRect(new SKRect(pillX, 3f, pillX + segW, H - 3f), r, r, paint);

        var font = Ui.Font(12.5f, 500);
        float baseline = Blossom.Utils.Text.Baseline(font, H / 2f);
        for (int i = 0; i < _options.Length; i++)
        {
            paint.Color = i == _selected || i == _hover ? ActiveTextColor : TextColor;
            float cx = 3f + segW * i + segW / 2f;
            c.DrawText(_options[i], MathF.Round(cx), baseline, SKTextAlign.Center, font, paint);
        }
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        var font = Ui.Font(12.5f, 500);
        float widest = 0f;
        foreach (var o in _options)
            widest = Math.Max(widest, Blossom.Utils.Text.MeasureWidth(o, font));
        float w = (widest + 24f) * _options.Length + 6f;
        if (MinWidth.HasValue) w = Math.Max(w, MinWidth.Value);
        if (maxWidth > 0) w = Math.Min(w, maxWidth);
        return new SKSize(w, 32f);
    }
}
