using System;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Hosting;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Utils;
using SkiaSharp;

namespace ReactiveLab.Ui;

/// <summary>Builders so pages read like markup. Every colour is bound to <see cref="Look.Current"/>.</summary>
public static class UiKit
{
    /// <summary>Same family as element text (<c>TextStyle</c>'s default), for painted controls.</summary>
    public const string FontFamily = "Liberation Sans, Noto Sans, sans-serif";

    private static SKFont? _font;
    public static SKFont FieldFont => _font ??= Fonts.CreateFont(Fonts.GetTypeface(FontFamily, 400), 13f);

    public static VisualElement Text(string text, float size = 13f, int weight = 400, bool muted = false)
    {
        var el = new VisualElement { IsClickthrough = true };
        StyleText(el, size, weight, muted, null);
        el.Text = text; // after the font is set, so the text is measured once
        return el;
    }

    /// <summary>Reactive text: re-renders when signals read inside <paramref name="text"/> change.</summary>
    /// <param name="colour">Optional reactive colour; signals it reads re-colour the text.</param>
    public static VisualElement Text(Func<string> text, float size = 13f, int weight = 400, bool muted = false, Func<Palette, SKColor>? colour = null)
    {
        var el = new VisualElement { IsClickthrough = true };
        StyleText(el, size, weight, muted, colour);
        el.BindText(text);
        return el;
    }

    private static void StyleText(VisualElement el, float size, int weight, bool muted, Func<Palette, SKColor>? colour)
    {
        el.Style.Text.Size = size;
        el.Style.Text.Weight = weight;
        el.Style.Text.Alignment = TextAlign.Left;
        el.Bind(e =>
        {
            var p = Look.Current.Value;
            e.Style.Text.Color = colour?.Invoke(p) ?? (muted ? p.Muted : p.Text);
        });
    }

    public static Stack Row(float gap, params VisualElement[] children)
    {
        var row = new Stack { Orientation = Orientation.Horizontal, Gap = gap, Align = LayoutAlign.Center };
        foreach (var c in children) row.AddChild(c);
        return row;
    }

    public static Stack Column(float gap, params VisualElement[] children)
    {
        var col = new Stack { Orientation = Orientation.Vertical, Gap = gap };
        foreach (var c in children) col.AddChild(c);
        return col;
    }

    public static VisualElement Spacer()
    {
        var spacer = new VisualElement { IsClickthrough = true };
        Stack.SetGrow(spacer, 1f);
        return spacer;
    }

    /// <summary>Rounded surface with a border; children stack vertically.</summary>
    public static Stack Panel(float gap = 10f, float padding = 14f)
    {
        var panel = new Stack { Orientation = Orientation.Vertical, Gap = gap, Padding = new Thickness(padding) };
        panel.Style.Border.Width = 1;
        panel.Style.Border.Roundness = 10;
        panel.Bind(p =>
        {
            var pal = Look.Current.Value;
            p.Style.BackColor = pal.Panel;
            p.Style.Border.Color = pal.Border;
        });
        return panel;
    }

    public static T Grow<T>(this T element, float weight = 1f) where T : VisualElement
    {
        Stack.SetGrow(element, weight);
        return element;
    }

    public static T Width<T>(this T element, float width) where T : VisualElement
    {
        element.Transform.Width = width;
        element.MinWidth = width;
        element.MaxWidth = width;
        return element;
    }

    public static T Height<T>(this T element, float height) where T : VisualElement
    {
        element.Transform.Height = height;
        element.MinHeight = height;
        element.MaxHeight = height;
        return element;
    }

    /// <summary>Two-way: field edits write the signal; signal writes update the field.</summary>
    public static Field BindValue(this Field field, Signal<string> value)
    {
        field.Changed += v => value.Value = v;
        field.Bind(f =>
        {
            if (f.Text != value.Value)
                f.Text = value.Value;
        });
        return field;
    }

    public static Check BindChecked(this Check check, Signal<bool> value)
    {
        check.CheckedChanged += v => value.Value = v;
        check.Bind(c => c.IsChecked = value.Value);
        return check;
    }

    /// <summary>A row of buttons acting as one choice; the selected one is highlighted.</summary>
    public static Stack Choice(Signal<int> selected, params string[] labels)
        => Choice(() => selected.Value, i => selected.Value = i, labels);

    public static Stack Choice(Func<int> selected, Action<int> select, params string[] labels)
    {
        var row = new Stack { Orientation = Orientation.Horizontal, Gap = 4, Align = LayoutAlign.Center };
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            var b = new Btn(labels[i], small: true);
            b.Clicked += () => select(index);
            b.Bind(x => x.Kind = selected() == index ? BtnKind.Primary : BtnKind.Normal);
            row.AddChild(b);
        }
        return row;
    }

    /// <summary>Section heading with an optional muted hint line under it.</summary>
    public static Stack Section(string title, string? hint = null)
    {
        var col = Column(2, Text(title, 14f, 700).Height(20));
        if (hint != null)
            col.AddChild(Paragraph(hint));
        return col;
    }

    public static VisualElement Paragraph(string text, float size = 12f)
    {
        var el = new Paragraph { Text = text, IsClickthrough = true };
        el.Style.Text.Size = size;
        el.Bind(e => e.Style.Text.Color = Look.Current.Value.Muted);
        return el;
    }

    /// <summary>Equal-width buttons in one row.</summary>
    public static Stack Buttons(params Btn[] buttons)
    {
        var row = new Stack { Orientation = Orientation.Horizontal, Gap = 6 };
        foreach (var b in buttons) row.AddChild(b.Grow());
        return row;
    }
}

/// <summary>Wrapping text whose preferred height follows the offered width.</summary>
public sealed class Paragraph : VisualElement
{
    public Paragraph()
    {
        Style.Text.Overflow = TextOverflow.Wrap;
        Style.Text.MaxLines = int.MaxValue;
        Style.Text.Alignment = TextAlign.Left;
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        float w = maxWidth > 0 ? maxWidth : Transform.Width;
        if (w <= 0 || string.IsNullOrEmpty(Text))
            return base.GetPreferredSize(maxWidth, maxHeight);
        var layout = TextLayout.Build(Text, Style.Text.SkFont, Style.Text.Color,
            Math.Max(1f, w - Padding.Horizontal), float.MaxValue, TextOverflow.Wrap, int.MaxValue);
        return new SKSize(w, layout.Height + Padding.Vertical);
    }
}

/// <summary>Horizontal progress bar; <see cref="Fraction"/> is 0..1.</summary>
public sealed class Bar : VisualElement
{
    private readonly VisualElement _fill;
    private float _fraction;

    public Bar()
    {
        IsClickthrough = true;
        Style.Border.Roundness = 4;
        _fill = new VisualElement { IsClickthrough = true };
        _fill.Style.Border.Roundness = 4;
        AddChild(_fill);
        this.Height(8);
        this.Bind(b =>
        {
            var p = Look.Current.Value;
            b.Style.BackColor = p.Surface;
            _fill.Style.BackColor = p.Success;
        });
    }

    public float Fraction
    {
        get => _fraction;
        set
        {
            value = Math.Clamp(value, 0f, 1f);
            if (_fraction == value) return;
            _fraction = value;
            InvalidateLayout();
        }
    }

    protected override void LayoutChildren()
    {
        _fill.Transform.SetAbsoluteFrame(Transform.AbsoluteX, Transform.AbsoluteY, Transform.Width * _fraction, Transform.Height);
    }
}

public enum BtnKind
{
    Normal,
    Primary,
    Ghost,
    Danger
}

/// <summary>Text button on <see cref="ButtonBase"/>. Colours follow the palette, kind and hover/press state.</summary>
public sealed class Btn : ButtonBase
{
    private readonly VisualElement _label;
    private readonly bool _small;
    private BtnKind _kind;

    public Btn(string text, BtnKind kind = BtnKind.Normal, bool small = false)
    {
        _kind = kind;
        _small = small;
        Name = $"Btn_{text}";
        _label = new VisualElement { IsClickthrough = true, Text = text };
        _label.Style.Text.Size = small ? 12f : 13f;
        _label.Style.Text.Weight = 500;
        _label.Style.Text.Alignment = TextAlign.Center;
        AddChild(_label);
        Style.Border.Roundness = 6;
        Transform.Height = Size;
        this.Bind(_ => Apply(Look.Current.Value));
    }

    private float Size => _small ? 26f : 32f;

    public string Label
    {
        get => _label.Text;
        set
        {
            if (_label.Text == value) return;
            _label.Text = value;
            InvalidateLayout();
            (Parent as VisualElement)?.InvalidateLayout();
        }
    }

    public BtnKind Kind
    {
        get => _kind;
        set
        {
            if (_kind == value) return;
            _kind = value;
            Apply(Look.Current.Peek());
        }
    }

    protected override void OnStateChanged() => Apply(Look.Current.Peek());

    private void Apply(Palette p)
    {
        if (_label == null) return;
        bool hover = IsHovered && Enabled;
        bool press = IsPressed && Enabled;
        SKColor back, fore, border = SKColors.Transparent;
        switch (_kind)
        {
            case BtnKind.Primary:
                back = hover ? p.AccentHover : p.Accent;
                fore = p.OnAccent;
                break;
            case BtnKind.Danger:
                back = p.Danger.WithAlpha((byte)(press ? 255 : hover ? 230 : 190));
                fore = SKColors.White;
                break;
            case BtnKind.Ghost:
                back = hover ? p.SurfaceHover : SKColors.Transparent;
                fore = hover ? p.Text : p.Muted;
                break;
            default:
                back = press ? p.Border : hover ? p.SurfaceHover : p.Surface;
                fore = p.Text;
                border = p.Border;
                break;
        }
        Style.BackColor = back;
        Style.Border.Color = border;
        Style.Border.Width = border.Alpha > 0 ? 1f : 0f;
        _label.Style.Text.Color = fore;
        Opacity = Enabled ? 1f : 0.45f;
        InvalidatePaint();
    }

    protected override void LayoutChildren()
    {
        _label.Transform.SetAbsoluteFrame(Transform.AbsoluteX, Transform.AbsoluteY, Transform.Width, Transform.Height);
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        float w = _label.GetPreferredSize(0, 0).Width + (_small ? 18f : 26f);
        w = Math.Max(w, Size);
        if (MinWidth.HasValue) w = Math.Max(w, MinWidth.Value);
        if (maxWidth > 0) w = Math.Min(w, maxWidth);
        return new SKSize(w, Size);
    }
}

/// <summary>Checkbox with a label, on <see cref="ToggleBase"/>. The box is painted; the label is a child.</summary>
public sealed class Check : ToggleBase
{
    private readonly VisualElement _label;

    public Check(string text)
    {
        _label = UiKit.Text(text, 12.5f);
        AddChild(_label);
        Transform.Height = 24;
        this.Bind(c => { _ = Look.Current.Value; c.InvalidatePaint(); });
    }

    protected override void OnStateChanged() => InvalidatePaint();

    protected override void Paint(SKCanvas c)
    {
        var p = Look.Current.Peek();
        using var paint = new SKPaint { IsAntialias = true };
        var box = new SKRect(1, H / 2 - 8, 17, H / 2 + 8);
        paint.Color = IsChecked ? p.Accent : IsHovered ? p.SurfaceHover : p.Surface;
        c.DrawRoundRect(box, 4, 4, paint);
        if (!IsChecked)
        {
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 1;
            paint.Color = p.Muted;
            c.DrawRoundRect(box, 4, 4, paint);
            return;
        }
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 2;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.Color = p.OnAccent;
        using var path = new SKPath();
        path.MoveTo(5, H / 2);
        path.LineTo(8, H / 2 + 3);
        path.LineTo(13.5f, H / 2 - 3.5f);
        c.DrawPath(path, paint);
    }

    protected override void LayoutChildren()
    {
        _label.Transform.SetAbsoluteFrame(Transform.AbsoluteX + 24, Transform.AbsoluteY, Math.Max(0, Transform.Width - 24), Transform.Height);
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
        => new(24 + _label.GetPreferredSize(0, 0).Width + 4, 24);
}

/// <summary>Single-line input on <see cref="TextInputBase"/>; this class only measures and paints.</summary>
public sealed class Field : TextInputBase
{
    private const float Pad = 10f;
    private float _scrollX;

    public Field(string placeholder = "", float width = 200f)
    {
        Placeholder = placeholder;
        Transform.Height = 32;
        MinWidth = width;
        Style.Border.Width = 1;
        Style.Border.Roundness = 6;
        this.Bind(_ => Apply(Look.Current.Value));
    }

    protected override void OnStateChanged() => Apply(Look.Current.Peek());

    private void Apply(Palette p)
    {
        Style.BackColor = p.Surface;
        Style.Border.Color = IsFocused ? p.Accent : IsHovered ? p.Muted : p.Border;
        InvalidatePaint();
    }

    private static SKFont Font => UiKit.FieldFont;
    private float InnerWidth => Math.Max(1f, W - Pad * 2);

    protected override float PrefixWidth(int charCount)
    {
        charCount = Math.Clamp(charCount, 0, Text.Length);
        return charCount == 0 ? 0f : Blossom.Utils.Text.MeasureWidth(Text[..charCount], Font);
    }

    protected override int IndexAt(float localX)
    {
        float x = localX - Pad + _scrollX;
        float prev = 0f;
        for (int i = 1; i <= Text.Length; i++)
        {
            float w = PrefixWidth(i);
            if (x < (prev + w) / 2f) return i - 1;
            prev = w;
        }
        return Text.Length;
    }

    protected override void OnCaretMoved() => EnsureCaretVisible();
    protected override void OnTextModified() => EnsureCaretVisible();

    private void EnsureCaretVisible()
    {
        float caret = PrefixWidth(CaretIndex);
        float total = PrefixWidth(Text.Length);
        if (caret - _scrollX > InnerWidth) _scrollX = caret - InnerWidth;
        if (caret - _scrollX < 0) _scrollX = caret;
        _scrollX = Math.Clamp(_scrollX, 0f, Math.Max(0f, total - InnerWidth));
    }

    protected override void Paint(SKCanvas c)
    {
        var p = Look.Current.Peek();
        float baseline = Blossom.Utils.Text.Baseline(Font, H / 2f);
        c.Save();
        c.ClipRect(new SKRect(Pad - 1, 0, W - Pad + 1, H));
        float originX = Pad - _scrollX;
        using var paint = new SKPaint { IsAntialias = true };

        if (IsFocused && HasSelection)
        {
            float x0 = originX + PrefixWidth(SelectionStart);
            float x1 = originX + PrefixWidth(SelectionStart + SelectionLength);
            paint.Color = p.Accent.WithAlpha(90);
            c.DrawRect(new SKRect(x0, H / 2f - 9f, x1, H / 2f + 9f), paint);
        }

        paint.Color = Text.Length == 0 ? p.Muted : p.Text;
        c.DrawText(Text.Length == 0 ? Placeholder : Text, Text.Length == 0 ? Pad : originX, baseline, SKTextAlign.Left, Font, paint);

        if (IsFocused && IsCaretShown && !HasSelection)
        {
            float cx = MathF.Round(originX + PrefixWidth(CaretIndex)) + 0.5f;
            paint.Color = p.Text;
            paint.StrokeWidth = 1.5f;
            c.DrawLine(cx, H / 2f - 8f, cx, H / 2f + 8f, paint);
        }
        c.Restore();
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        float w = MinWidth ?? 200f;
        if (maxWidth > 0) w = Math.Min(w, maxWidth);
        return new SKSize(w, 32f);
    }
}

/// <summary>Vertical scroller around a <see cref="Stack"/>; content height follows its preferred size.</summary>
public sealed class Scroller : ScrollContainer
{
    public Scroller(float gap = 6f, Thickness? padding = null)
    {
        OverflowX = OverflowMode.Clip;
        OverflowY = OverflowMode.Scroll;
        ScrollbarVisibilityX = ScrollbarVisibility.Hidden;
        ScrollbarThickness = 6f;
        ScrollbarRadius = 3f;
        Content = new Stack { Orientation = Orientation.Vertical, Gap = gap, Padding = padding ?? new Thickness(0) };
        AddChild(Content);
        this.Bind(s =>
        {
            var p = Look.Current.Value;
            s.ScrollbarThumbColor = p.Muted.WithAlpha(110);
            s.ScrollbarThumbHoverColor = p.Muted.WithAlpha(200);
            s.ScrollbarThumbDragColor = p.Muted;
        });
    }

    public Stack Content { get; }

    protected override void LayoutChildren()
    {
        float w = Math.Max(1f, Transform.Computed.Width);
        float h = Math.Max(1f, Content.GetPreferredSize(w, 0).Height);
        Content.Transform.SetLocalFrame(0, 0, w, h);
        SetContentSize(w, h);
        base.LayoutChildren();
    }
}

/// <summary>
/// A row of small glyph buttons painted by one element. Board cards use it instead of one
/// <see cref="Btn"/> per action: thousands of cards would otherwise mean thousands more elements.
/// </summary>
public sealed class ActionStrip : Control
{
    public const float Slot = 22f;
    private static SKFont? _glyphFont;
    private readonly string[] _glyphs;
    private readonly Action[] _actions;
    private readonly bool[] _enabled;
    private int _hover = -1;
    private int _pressed = -1;

    public ActionStrip(params (string Glyph, Action Run)[] actions)
    {
        _glyphs = Array.ConvertAll(actions, a => a.Glyph);
        _actions = Array.ConvertAll(actions, a => a.Run);
        _enabled = Array.ConvertAll(actions, _ => true);
        Cursor = UiCursor.Hand;
        Transform.Width = Slot * actions.Length;
        Transform.Height = Slot;

        Events.OnMouseMove += (_, e) => SetHover(SlotAt(e.Relative.X));
        Events.OnMouseLeave += _ => SetHover(-1);
        Events.OnMouseDown += (_, e) =>
        {
            if (e.Button != 0) return;
            _pressed = SlotAt(e.Relative.X);
            CapturePointer();
            InvalidatePaint();
            e.Handled = true;
        };
        Events.OnMouseUp += (_, e) =>
        {
            if (_pressed < 0) return;
            int pressed = _pressed;
            _pressed = -1;
            ReleasePointer();
            InvalidatePaint();
            e.Handled = true;
            bool inside = e.Relative.Y >= 0 && e.Relative.Y <= H;
            if (inside && SlotAt(e.Relative.X) == pressed && _enabled[pressed])
                _actions[pressed]();
        };
        this.Bind(s => { _ = Look.Current.Value; s.InvalidatePaint(); });
    }

    private static SKFont GlyphFont => _glyphFont ??= Fonts.CreateFont(Fonts.GetTypeface(UiKit.FontFamily, 600), 15f);

    public void SetEnabled(int index, bool enabled)
    {
        if (_enabled[index] == enabled) return;
        _enabled[index] = enabled;
        InvalidatePaint();
    }

    private int SlotAt(float x) => x < 0 || x >= Slot * _glyphs.Length ? -1 : (int)(x / Slot);

    private void SetHover(int slot)
    {
        if (_hover == slot) return;
        _hover = slot;
        InvalidatePaint();
    }

    protected override void Paint(SKCanvas c)
    {
        var p = Look.Current.Peek();
        using var paint = new SKPaint { IsAntialias = true };
        float baseline = Blossom.Utils.Text.Baseline(GlyphFont, H / 2f);
        for (int i = 0; i < _glyphs.Length; i++)
        {
            float x = i * Slot;
            if (_enabled[i] && (i == _hover || i == _pressed))
            {
                paint.Color = i == _pressed ? p.Border : p.SurfaceHover;
                c.DrawRoundRect(new SKRect(x + 1, 1, x + Slot - 1, H - 1), 5, 5, paint);
            }
            paint.Color = !_enabled[i] ? p.Muted.WithAlpha(70) : i == _hover ? p.Text : p.Muted;
            c.DrawText(_glyphs[i], x + Slot / 2f, baseline, SKTextAlign.Center, GlyphFont, paint);
        }
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight) => new(Slot * _glyphs.Length, Slot);
}
