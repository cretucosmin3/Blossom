using System;
using Blossom.Core.Visual;
using Blossom.Primitives;
using SkiaSharp;

namespace Blossom.Testing.Components;

public enum ButtonVariant
{
    Primary,
    Secondary,
    Outline,
    Ghost,
    Destructive
}

public enum ButtonSize
{
    Sm,
    Md,
    Lg
}

/// <summary>
/// <see cref="ButtonBase"/> with an optional leading icon and a label. Input, capture and
/// keyboard activation come from the primitive; colours come from the theme's "button" recipe,
/// which reads <see cref="Variant"/> and the hover/press state.
/// </summary>
public class Button : ButtonBase
{
    private readonly VisualElement _label;
    private Icon? _icon;
    private ButtonVariant _variant;
    private ButtonSize _size;
    private SKColor _content;

    public Button(string? text = null, string? icon = null, ButtonVariant variant = ButtonVariant.Primary, ButtonSize size = ButtonSize.Md)
    {
        Name = $"Button_{text ?? icon}";
        _variant = variant;
        _size = size;
        Transform.TransformOriginX = 0.5f;
        Transform.TransformOriginY = 0.5f;

        _label = new VisualElement { IsClickthrough = true, Text = text ?? "" };
        _label.Style.Text.Weight = 500;
        _label.Style.Text.Alignment = TextAlign.Center;
        _label.Visible = !string.IsNullOrEmpty(text);
        AddChild(_label);

        if (icon != null)
            IconName = icon;

        ApplySize();
        this.UseStyle("button");
    }

    /// <summary>Square, icon-only button.</summary>
    public static Button IconOnly(string icon, ButtonVariant variant = ButtonVariant.Ghost, ButtonSize size = ButtonSize.Md)
        => new(null, icon, variant, size);

    public string Label
    {
        get => _label.Text;
        set
        {
            _label.Text = value ?? "";
            _label.Visible = _label.Text.Length > 0;
            InvalidateLayout();
        }
    }

    public string? IconName
    {
        get => _icon?.IconName;
        set
        {
            if (value == null)
            {
                if (_icon != null) { RemoveChild(_icon); _icon.Dispose(); _icon = null; }
            }
            else if (_icon == null)
            {
                _icon = new Icon(value, IconSize) { Color = _content };
                InsertChild(0, _icon);
            }
            else
            {
                _icon.IconName = value;
            }
            InvalidateLayout();
        }
    }

    public ButtonVariant Variant
    {
        get => _variant;
        set
        {
            if (_variant == value) return;
            _variant = value;
            this.UseStyle("button");
        }
    }

    public ButtonSize Size
    {
        get => _size;
        set
        {
            if (_size == value) return;
            _size = value;
            ApplySize();
        }
    }

    /// <summary>Label + icon colour. Set by the theme recipe.</summary>
    public SKColor ContentColor
    {
        get => _content;
        set
        {
            _content = value;
            _label.Style.Text.Color = value;
            if (_icon != null) _icon.Color = value;
        }
    }

    private bool IsIconOnly => !_label.Visible && _icon != null;
    private float ControlHeight => _size switch { ButtonSize.Sm => 30f, ButtonSize.Lg => 42f, _ => 36f };
    private float HorizontalPadding => _size switch { ButtonSize.Sm => 10f, ButtonSize.Lg => 18f, _ => 14f };
    private float IconSize => _size == ButtonSize.Lg ? 18f : 16f;

    private void ApplySize()
    {
        _label.Style.Text.Size = _size switch { ButtonSize.Sm => 12.5f, ButtonSize.Lg => 14.5f, _ => 13.5f };
        Transform.Height = ControlHeight;
        InvalidateLayout();
    }

    protected override void OnStateChanged()
    {
        this.UseStyle("button");
        float press = IsPressed && Enabled ? 0.97f : 1f;
        Transform.ScaleX = press;
        Transform.ScaleY = press;
    }

    protected override void LayoutChildren()
    {
        float gap = 8f;
        float iconW = _icon != null ? IconSize : 0f;
        float labelW = _label.Visible ? _label.GetPreferredSize(0, 0).Width : 0f;
        float contentW = iconW + labelW + (iconW > 0 && labelW > 0 ? gap : 0f);

        float x = Transform.AbsoluteX + (Transform.Width - contentW) / 2f;
        float cy = Transform.AbsoluteY + Transform.Height / 2f;

        if (_icon != null)
        {
            _icon.Transform.SetAbsoluteFrame(MathF.Round(x), MathF.Round(cy - IconSize / 2f), IconSize, IconSize);
            x += iconW + gap;
        }
        if (_label.Visible)
            _label.Transform.SetAbsoluteFrame(x, Transform.AbsoluteY, labelW, Transform.Height);
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        float h = ControlHeight;
        float w;
        if (IsIconOnly)
            w = h;
        else
        {
            float labelW = _label.Visible ? _label.GetPreferredSize(0, 0).Width : 0f;
            float iconW = _icon != null ? IconSize + (labelW > 0 ? 8f : 0f) : 0f;
            w = labelW + iconW + HorizontalPadding * 2f;
        }
        if (MinWidth.HasValue) w = Math.Max(w, MinWidth.Value);
        if (maxWidth > 0) w = Math.Min(w, maxWidth);
        return new SKSize(w, h);
    }
}
