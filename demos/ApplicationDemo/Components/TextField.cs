using System;
using Blossom.Primitives;
using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>
/// Single-line input on <see cref="TextInputBase"/> (caret, selection, clipboard, word jumps come
/// from the primitive). This class only measures and paints: text, selection, caret, placeholder,
/// and keeps the caret scrolled into view.
/// </summary>
public sealed class TextField : TextInputBase
{
    private const float FontSize = 13.5f;
    private readonly Icon? _icon;
    private float _scrollX;
    private bool _invalid;

    public TextField(string placeholder = "", string? icon = null)
    {
        Placeholder = placeholder;
        Transform.Height = 36;
        if (icon != null)
        {
            _icon = new Icon(icon, 16f);
            AddChild(_icon);
        }
        this.UseStyle("field");
    }

    public SKColor TextColor;
    public SKColor PlaceholderColor;
    public SKColor SelectionColor;
    public SKColor CaretColor;

    public SKColor IconColor
    {
        set { if (_icon != null) _icon.Color = value; }
    }

    /// <summary>Shows the error border; driven by validation memos.</summary>
    public bool Invalid
    {
        get => _invalid;
        set
        {
            if (_invalid == value) return;
            _invalid = value;
            this.UseStyle("field");
        }
    }

    private SKFont Font => Ui.Font(FontSize);
    private float PadLeft => _icon != null ? 36f : 12f;
    private const float PadRight = 12f;
    private float InnerWidth => Math.Max(1f, W - PadLeft - PadRight);

    private string Display => IsPassword ? new string('•', Text.Length) : Text;

    protected override void OnStateChanged() => this.UseStyle("field");

    protected override float PrefixWidth(int charCount)
    {
        var s = Display;
        charCount = Math.Clamp(charCount, 0, s.Length);
        return charCount == 0 ? 0f : Blossom.Utils.Text.MeasureWidth(s[..charCount], Font);
    }

    protected override int IndexAt(float localX)
    {
        float x = localX - PadLeft + _scrollX;
        var s = Display;
        float prev = 0f;
        for (int i = 1; i <= s.Length; i++)
        {
            float w = PrefixWidth(i);
            if (x < (prev + w) / 2f) return i - 1;
            prev = w;
        }
        return s.Length;
    }

    protected override void OnCaretMoved() => EnsureCaretVisible();
    protected override void OnTextModified() => EnsureCaretVisible();

    private void EnsureCaretVisible()
    {
        float caret = PrefixWidth(CaretIndex);
        float total = PrefixWidth(Display.Length);
        if (caret - _scrollX > InnerWidth) _scrollX = caret - InnerWidth;
        if (caret - _scrollX < 0) _scrollX = caret;
        _scrollX = Math.Clamp(_scrollX, 0f, Math.Max(0f, total - InnerWidth));
    }

    protected override void LayoutChildren()
    {
        _icon?.Transform.SetAbsoluteFrame(
            Transform.AbsoluteX + 12f,
            MathF.Round(Transform.AbsoluteY + (Transform.Height - 16f) / 2f), 16f, 16f);
    }

    protected override void Paint(SKCanvas c)
    {
        var font = Font;
        float baseline = Blossom.Utils.Text.Baseline(font, H / 2f);
        var clip = new SKRect(PadLeft - 1f, 0, W - PadRight + 1f, H);

        c.Save();
        c.ClipRect(clip);
        float originX = PadLeft - _scrollX;
        using var paint = new SKPaint { IsAntialias = true };

        if (IsFocused && HasSelection)
        {
            float x0 = originX + PrefixWidth(SelectionStart);
            float x1 = originX + PrefixWidth(SelectionStart + SelectionLength);
            paint.Color = SelectionColor;
            c.DrawRoundRect(new SKRect(x0, H / 2f - 10f, x1, H / 2f + 10f), 3f, 3f, paint);
        }

        if (Text.Length == 0)
        {
            paint.Color = PlaceholderColor;
            c.DrawText(Placeholder, PadLeft, baseline, SKTextAlign.Left, font, paint);
        }
        else
        {
            paint.Color = Enabled ? TextColor : TextColor.WithAlpha(120);
            c.DrawText(Display, originX, baseline, SKTextAlign.Left, font, paint);
        }

        if (IsFocused && IsCaretShown && !HasSelection)
        {
            float cx = MathF.Round(originX + PrefixWidth(CaretIndex)) + 0.5f;
            paint.Color = CaretColor;
            paint.StrokeWidth = 1.5f;
            c.DrawLine(cx, H / 2f - 9f, cx, H / 2f + 9f, paint);
        }
        c.Restore();
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        float w = MinWidth ?? 200f;
        if (maxWidth > 0) w = Math.Min(w, maxWidth);
        return new SKSize(w, 36f);
    }
}
