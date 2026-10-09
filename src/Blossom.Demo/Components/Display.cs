using System;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Primitives;
using SkiaSharp;

namespace Blossom.Testing.Components;

public enum Tone
{
    Neutral,
    Primary,
    Success,
    Warning,
    Danger,
    Info
}

/// <summary>Small status pill. Colours come from the "badge" recipe per <see cref="Tone"/>.</summary>
public sealed class Badge : VisualElement
{
    private Tone _tone;

    public Badge(string text, Tone tone = Tone.Neutral)
    {
        Text = text;
        _tone = tone;
        IsClickthrough = true;
        Padding = new Thickness(8, 2);
        Transform.Height = 20;
        this.UseStyle("badge");
    }

    public Tone Tone
    {
        get => _tone;
        set
        {
            if (_tone == value) return;
            _tone = value;
            this.UseStyle("badge");
        }
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        var s = base.GetPreferredSize(0, 0);
        return new SKSize(Math.Max(20f, s.Width), 20f);
    }
}

/// <summary>Round avatar with initials; hue is derived from the name.</summary>
public sealed class Avatar : VisualElement
{
    private static readonly ColourScale[] Hues =
    {
        Colours.Indigo, Colours.Rose, Colours.Emerald, Colours.Amber, Colours.Sky, Colours.Violet, Colours.Teal
    };

    private readonly float _size;

    public Avatar(string name, float size = 32f)
    {
        _size = size;
        IsClickthrough = true;
        Transform.Width = size;
        Transform.Height = size;
        Style.Text.Size = size * 0.38f;
        Style.Text.Weight = 600;
        Style.Text.Alignment = TextAlign.Center;
        Name = name;
        this.UseStyle("avatar");
    }

    public new string Name
    {
        get => base.Name ?? "";
        set
        {
            base.Name = value;
            Text = Initials(value);
            int hash = 0;
            foreach (char ch in value ?? "") hash = hash * 31 + ch;
            Hue = Hues[Math.Abs(hash) % Hues.Length][500];
            this.UseStyle("avatar");
        }
    }

    public SKColor Hue { get; private set; }

    private static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string a = parts[0][..1];
        string b = parts.Length > 1 ? parts[^1][..1] : "";
        return (a + b).ToUpperInvariant();
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight) => new(_size, _size);
}

/// <summary>Surface with optional title/description header; children go into <see cref="Stack"/> order.</summary>
public class Card : Stack
{
    public Card(string? title = null, string? description = null, string style = "card")
    {
        Orientation = Orientation.Vertical;
        Gap = 16;
        Padding = new Thickness(20);
        this.UseStyle(style);

        if (title != null)
        {
            var header = Ui.Column(4, Ui.Text(title, "text.heading"));
            if (description != null)
                header.AddChild(Ui.Paragraph(description, "text.muted"));
            AddChild(header);
        }
    }
}

/// <summary>Vertical scroller around a <see cref="Stack"/>; content height follows its preferred size.</summary>
public class ScrollView : ScrollContainer
{
    public ScrollView(float gap = 20f, Thickness? padding = null)
    {
        OverflowX = OverflowMode.Clip;
        OverflowY = OverflowMode.Scroll;
        ScrollbarVisibilityX = ScrollbarVisibility.Hidden;
        Content = new Stack
        {
            Orientation = Orientation.Vertical,
            Gap = gap,
            Padding = padding ?? new Thickness(0)
        };
        AddChild(Content);
        this.UseStyle("scroll");
    }

    public Stack Content { get; }

    protected override void LayoutChildren()
    {
        base.LayoutChildren();
        float w = Math.Max(1f, Transform.Computed.Width);
        float h = Math.Max(1f, Content.GetPreferredSize(w, 0).Height);
        Content.Transform.SetLocalFrame(0, 0, w, h);
        SetContentSize(w, h);
    }
}

/// <summary>
/// Flow layout written against Blossom's layout hook (<see cref="VisualElement.LayoutChildren"/>):
/// children keep their preferred size and wrap onto the next line. Core ships no flex/wrap; apps write their own.
/// </summary>
public sealed class WrapPanel : LayoutElement
{
    public float Gap { get; set; } = 8f;
    public float LineGap { get; set; } = 8f;

    protected override void LayoutChildren()
    {
        float maxW = Transform.Width;
        float y = 0;
        var line = new System.Collections.Generic.List<(VisualElement El, SKSize Size)>();
        float x = 0, lineH = 0;

        void Flush()
        {
            // Centre each item vertically within its line (mixed button sizes line up on their middles).
            float lx = 0;
            foreach (var (el, size) in line)
            {
                Place(el, Transform.AbsoluteX + lx, Transform.AbsoluteY + y + MathF.Round((lineH - size.Height) / 2f), size.Width, size.Height);
                lx += size.Width + Gap;
            }
            y += lineH + LineGap;
            line.Clear();
            x = 0;
            lineH = 0;
        }

        foreach (var child in ParticipatingChildren())
        {
            var size = child.GetPreferredSize(0, 0);
            if (x > 0 && x + size.Width > maxW)
                Flush();
            line.Add((child, size));
            x += size.Width + Gap;
            lineH = Math.Max(lineH, size.Height);
        }
        if (line.Count > 0) Flush();
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        float limit = maxWidth > 0 ? maxWidth : Transform.Width;
        float x = 0, y = 0, line = 0, widest = 0;
        foreach (var child in ParticipatingChildren())
        {
            var size = child.GetPreferredSize(0, 0);
            if (x > 0 && limit > 0 && x + size.Width > limit)
            {
                x = 0;
                y += line + LineGap;
                line = 0;
            }
            x += size.Width + Gap;
            widest = Math.Max(widest, x - Gap);
            line = Math.Max(line, size.Height);
        }
        return new SKSize(limit > 0 ? limit : widest, y + line);
    }
}

/// <summary>
/// Content-sized column grid for scrolling pages. <see cref="Grid"/> fills its slot (rows default to
/// stars), so inside a scroller it has no height to give; this host measures each row instead.
/// Tracks use the same syntax as Grid (<c>"2*, *"</c>, <c>"*, 260"</c>); children flow row-major
/// and every cell in a row is stretched to the tallest one.
/// </summary>
public sealed class ColumnLayout : LayoutElement
{
    private readonly System.Collections.Generic.List<LayoutLength> _tracks;

    public ColumnLayout(string tracks, float gap = 20f)
    {
        _tracks = LayoutLength.ParseList(tracks);
        if (_tracks.Count == 0) _tracks.Add(LayoutLength.Star(1f));
        Gap = gap;
        RowGap = gap;
    }

    public float Gap { get; set; }
    public float RowGap { get; set; }

    private float[] Widths(float total)
    {
        int n = _tracks.Count;
        var w = new float[n];
        float free = Math.Max(0f, total - Padding.Horizontal - Gap * (n - 1));
        float stars = 0f;
        for (int i = 0; i < n; i++)
        {
            if (_tracks[i].Unit == LayoutUnit.Pixel) { w[i] = _tracks[i].Value; free -= w[i]; }
            else stars += _tracks[i].Unit == LayoutUnit.Star ? _tracks[i].Value : 1f;
        }
        free = Math.Max(0f, free);
        for (int i = 0; i < n; i++)
            if (_tracks[i].Unit != LayoutUnit.Pixel)
                w[i] = stars > 0 ? free * (_tracks[i].Unit == LayoutUnit.Star ? _tracks[i].Value : 1f) / stars : 0f;
        return w;
    }

    private float Arrange(float width, bool place)
    {
        var widths = Widths(width);
        var kids = new System.Collections.Generic.List<VisualElement>(ParticipatingChildren());
        float y = Padding.Top;
        for (int start = 0; start < kids.Count; start += widths.Length)
        {
            float rowH = 0f;
            for (int c = 0; c < widths.Length && start + c < kids.Count; c++)
                rowH = Math.Max(rowH, PreferredHeight(kids[start + c], widths[c], 0f));
            if (place)
            {
                float x = Padding.Left;
                for (int c = 0; c < widths.Length && start + c < kids.Count; c++)
                {
                    Place(kids[start + c], Transform.AbsoluteX + x, Transform.AbsoluteY + y, widths[c], rowH);
                    x += widths[c] + Gap;
                }
            }
            y += rowH + RowGap;
        }
        return kids.Count == 0 ? Padding.Vertical : y - RowGap + Padding.Bottom;
    }

    protected override void LayoutChildren() => Arrange(Transform.Width, place: true);

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        float w = maxWidth > 0 ? maxWidth : Transform.Width;
        return new SKSize(w, Arrange(w, place: false));
    }
}
