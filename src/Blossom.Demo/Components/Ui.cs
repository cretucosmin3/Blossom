using System;
using System.Collections.Generic;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Utils;
using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>Small builders so pages read like markup: rows, columns, text, spacers.</summary>
public static class Ui
{
    public const string FontFamily = "Liberation Sans, Noto Sans, sans-serif";

    private static readonly Dictionary<(float, int), SKFont> FontCache = new();

    /// <summary>Shared font for custom-painted controls (<see cref="Control.Paint"/>).</summary>
    public static SKFont Font(float size, int weight = 400)
    {
        if (!FontCache.TryGetValue((size, weight), out var font))
        {
            font = Fonts.CreateFont(Fonts.GetTypeface(FontFamily, weight), size);
            FontCache[(size, weight)] = font;
        }
        return font;
    }

    /// <summary>Text node styled by a theme recipe (e.g. "text.title", "text.muted").</summary>
    public static VisualElement Text(string text, string style = "text.body")
    {
        var el = new VisualElement { Text = text, IsClickthrough = true };
        el.UseStyle(style);
        return el;
    }

    /// <summary>Reactive text node: re-renders when signals read inside <paramref name="text"/> change.</summary>
    public static VisualElement Text(Func<string> text, string style = "text.body")
    {
        var el = new VisualElement { IsClickthrough = true };
        el.UseStyle(style);
        el.BindText(text);
        return el;
    }

    /// <summary>Wrapping paragraph; height follows the available width.</summary>
    public static VisualElement Paragraph(string text, string style = "text.muted")
    {
        var el = new Paragraph { Text = text, IsClickthrough = true };
        el.UseStyle(style);
        return el;
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

    public static VisualElement Divider()
    {
        var line = new VisualElement { IsClickthrough = true }.Height(1);
        line.UseStyle("divider");
        return line;
    }

    /// <summary>Two-way binding: field edits write the signal; signal writes update the field.</summary>
    public static TextField BindValue(this TextField field, Signal<string> value)
    {
        field.Changed += v => value.Value = v;
        field.Bind(f =>
        {
            if (f.Text != value.Value)
                f.Text = value.Value;
        });
        return field;
    }

    public static Slider BindValue(this Slider slider, Signal<float> value)
    {
        slider.ValueChanged += v => value.Value = v;
        slider.Bind(s => s.Value = value.Value);
        return slider;
    }

    public static T BindChecked<T>(this T toggle, Signal<bool> value) where T : ToggleBase
    {
        toggle.CheckedChanged += v => value.Value = v;
        toggle.Bind(t => t.IsChecked = value.Value);
        return toggle;
    }

    public static Segmented BindSelected(this Segmented segmented, Signal<int> index)
    {
        segmented.SelectionChanged += i => index.Value = i;
        segmented.Bind(s => s.SelectedIndex = index.Value);
        return segmented;
    }
}

/// <summary>
/// Wrapping text whose preferred height follows the offered width. The base element measures
/// against its current height (one line before first layout), so this lays out with no height cap.
/// </summary>
public sealed class Paragraph : VisualElement
{
    public Paragraph()
    {
        Style.Text.Overflow = Core.Visual.Enums.TextOverflow.Wrap;
        Style.Text.MaxLines = int.MaxValue;
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        float w = maxWidth > 0 ? maxWidth : Transform.Width;
        if (w <= 0 || string.IsNullOrEmpty(Text))
            return base.GetPreferredSize(maxWidth, maxHeight);
        var layout = TextLayout.Build(Text, Style.Text.SkFont, Style.Text.Color,
            Math.Max(1f, w - Padding.Horizontal), float.MaxValue, Core.Visual.Enums.TextOverflow.Wrap, int.MaxValue);
        return new SKSize(w, layout.Height + Padding.Vertical);
    }
}
