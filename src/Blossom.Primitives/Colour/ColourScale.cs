using System;
using SkiaSharp;

namespace Blossom.Primitives;

/// <summary>
/// One named hue with Tailwind-style shades 50 (light) through 950 (dark).
/// Use <c>Colours.Zinc[800]</c>.
/// </summary>
public sealed class ColourScale
{
    /// <summary>Legal shade keys: 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950.</summary>
    public static readonly int[] Shades = { 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950 };

    private readonly SKColor[] _values;

    internal ColourScale(params uint[] rgb)
    {
        if (rgb == null || rgb.Length != Shades.Length)
            throw new ArgumentException("A colour scale needs 11 RGB values (50–950).", nameof(rgb));
        _values = new SKColor[Shades.Length];
        for (int i = 0; i < rgb.Length; i++)
            _values[i] = FromRgb(rgb[i]);
    }

    /// <summary>Shade lookup. Throws if <paramref name="shade"/> is not a Tailwind step.</summary>
    public SKColor this[int shade]
    {
        get
        {
            if (!TryGet(shade, out var colour))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(shade),
                    shade,
                    "Shade must be 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, or 950.");
            }
            return colour;
        }
    }

    public bool TryGet(int shade, out SKColor colour)
    {
        int index = shade switch
        {
            50 => 0,
            100 => 1,
            200 => 2,
            300 => 3,
            400 => 4,
            500 => 5,
            600 => 6,
            700 => 7,
            800 => 8,
            900 => 9,
            950 => 10,
            _ => -1
        };
        if (index < 0)
        {
            colour = default;
            return false;
        }
        colour = _values[index];
        return true;
    }

    internal static SKColor FromRgb(uint rgb) => new(
        (byte)((rgb >> 16) & 0xFF),
        (byte)((rgb >> 8) & 0xFF),
        (byte)(rgb & 0xFF));
}
