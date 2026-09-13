using SkiaSharp;

namespace Blossom.Core.Visual;

/// <summary>One styled run of text for <see cref="RichBox"/> / <see cref="TextLayout"/>.</summary>
public sealed class TextSpan
{
    public string Text { get; set; } = "";
    public SKColor? Color { get; set; }
    public float? Size { get; set; }
    public int? Weight { get; set; }
    public SKTypeface? Typeface { get; set; }

    public TextSpan() { }

    public TextSpan(string text)
    {
        Text = text ?? "";
    }

    public TextSpan(string text, SKColor color, float? size = null, int? weight = null)
    {
        Text = text ?? "";
        Color = color;
        Size = size;
        Weight = weight;
    }
}
