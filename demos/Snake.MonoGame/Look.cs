using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Primitives;
using SkiaSharp;

namespace SnakeDemo;

/// <summary>
/// The sample's visual language: a hard-edged terminal look. Monospace type, square corners,
/// flat fills, and hard offset shadows instead of soft glows.
/// </summary>
internal static class Look
{
    public const string Mono = "JetBrains Mono, Cascadia Mono, Consolas, DejaVu Sans Mono, Menlo, Liberation Mono, monospace";

    public static readonly SKColor Ink = Colours.Zinc[950];
    public static readonly SKColor Panel = new(9, 11, 9);
    public static readonly SKColor Line = Colours.Zinc[800];
    public static readonly SKColor Text = Colours.Zinc[100];
    public static readonly SKColor Dim = Colours.Zinc[500];
    public static readonly SKColor Accent = Colours.Lime[400];
    public static readonly SKColor Gold = Colours.Amber[300];
    public static readonly SKColor Danger = Colours.Rose[500];

    public static ShadowStyle HardShadow(float offset = 6) => new(offset, offset, 0, 0, new SKColor(0, 0, 0, 220));

    public static VisualElement Label(string text, float x, float y, float w, float h, float size, SKColor color,
        TextAlign align = TextAlign.Left, int weight = 400)
    {
        var label = new VisualElement { Name = "Label" };
        Place(label, x, y, w, h);
        label.Text = text;
        label.Style.Text.Font = Mono;
        label.Style.Text.Size = size;
        label.Style.Text.Color = color;
        label.Style.Text.Alignment = align;
        label.Style.Text.Weight = weight;
        return label;
    }

    public static VisualElement Box(float x, float y, float w, float h, SKColor color)
    {
        var box = new VisualElement { Name = "Box" };
        Place(box, x, y, w, h);
        box.Style.BackColor = color;
        box.Style.Border = new BorderStyle { Width = 0, Roundness = 0 };
        return box;
    }

    public static T Place<T>(T el, float x, float y, float w, float h) where T : VisualElement
    {
        el.Transform.Anchor = Anchor.Left | Anchor.Top;
        el.Transform.LocalX = x;
        el.Transform.LocalY = y;
        el.Transform.Width = w;
        el.Transform.Height = h;
        return el;
    }
}
