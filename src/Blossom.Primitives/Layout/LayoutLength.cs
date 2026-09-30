using System;
using System.Collections.Generic;
using System.Globalization;

namespace Blossom.Primitives;

public enum LayoutUnit
{
    Auto,
    Pixel,
    Star
}

/// <summary>
/// Track or child size: <c>Auto</c> (preferred), pixel, or star (<c>*</c> / <c>2*</c>) sharing leftover space.
/// </summary>
public readonly struct LayoutLength : IEquatable<LayoutLength>
{
    public LayoutUnit Unit { get; }
    public float Value { get; }

    private LayoutLength(LayoutUnit unit, float value)
    {
        Unit = unit;
        Value = value;
    }

    public static LayoutLength Auto { get; } = new(LayoutUnit.Auto, 0f);

    public static LayoutLength Pixel(float pixels) => new(LayoutUnit.Pixel, Math.Max(0f, pixels));

    public static LayoutLength Star(float weight = 1f) => new(LayoutUnit.Star, Math.Max(0f, weight));

    public static implicit operator LayoutLength(float pixels) => Pixel(pixels);

    public static implicit operator LayoutLength(int pixels) => Pixel(pixels);

    public static LayoutLength Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Auto;

        text = text.Trim();
        if (text.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return Auto;

        if (text == "*")
            return Star(1f);

        if (text.EndsWith("*", StringComparison.Ordinal))
        {
            var num = text[..^1].Trim();
            if (num.Length == 0)
                return Star(1f);
            if (float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out float weight))
                return Star(weight);
            throw new FormatException($"Invalid star length '{text}'.");
        }

        if (text.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            text = text[..^2].Trim();

        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float px))
            return Pixel(px);

        throw new FormatException($"Invalid layout length '{text}'.");
    }

    public static List<LayoutLength> ParseList(string spec)
    {
        var list = new List<LayoutLength>();
        if (string.IsNullOrWhiteSpace(spec))
            return list;

        foreach (var part in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            list.Add(Parse(part));
        return list;
    }

    public override string ToString()
    {
        return Unit switch
        {
            LayoutUnit.Auto => "Auto",
            LayoutUnit.Star => Value == 1f ? "*" : string.Create(CultureInfo.InvariantCulture, $"{Value}*"),
            _ => string.Create(CultureInfo.InvariantCulture, $"{Value}"),
        };
    }

    public bool Equals(LayoutLength other) => Unit == other.Unit && Value == other.Value;
    public override bool Equals(object? obj) => obj is LayoutLength other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Unit, Value);
    public static bool operator ==(LayoutLength a, LayoutLength b) => a.Equals(b);
    public static bool operator !=(LayoutLength a, LayoutLength b) => !a.Equals(b);
}
