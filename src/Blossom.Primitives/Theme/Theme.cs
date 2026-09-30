using System;
using System.Collections.Generic;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Primitives;

/// <summary>
/// Named colour/number tokens plus reusable style recipes.
/// Recipes take the theme being applied so a Current-theme switch re-resolves tokens.
/// </summary>
public sealed class Theme
{
    private readonly Dictionary<string, SKColor> _colours = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, float> _numbers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Action<VisualElement, Theme>> _styles = new(StringComparer.OrdinalIgnoreCase);

    public Theme(string name)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "theme" : name;
    }

    public string Name { get; }

    public void SetColour(string token, SKColor colour)
    {
        _colours[RequireToken(token)] = colour;
    }

    public SKColor Colour(string token)
    {
        if (!_colours.TryGetValue(RequireToken(token), out var colour))
            throw new KeyNotFoundException($"Theme '{Name}' has no colour token '{token}'.");
        return colour;
    }

    public bool TryColour(string token, out SKColor colour)
        => _colours.TryGetValue(token ?? "", out colour);

    public void SetNumber(string token, float value)
    {
        _numbers[RequireToken(token)] = value;
    }

    public float Number(string token, float fallback = 0f)
        => _numbers.TryGetValue(token ?? "", out var n) ? n : fallback;

    public void SetStyle(string name, Action<VisualElement, Theme> apply)
    {
        if (apply == null) throw new ArgumentNullException(nameof(apply));
        _styles[RequireToken(name)] = apply;
    }

    public bool HasStyle(string name) => _styles.ContainsKey(name ?? "");

    /// <summary>Runs named recipes in order against <paramref name="element"/>'s own <see cref="VisualElement.Style"/>.</summary>
    public void Apply(VisualElement element, params string[] styleNames)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (styleNames == null || styleNames.Length == 0) return;

        for (int i = 0; i < styleNames.Length; i++)
        {
            var name = styleNames[i];
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!_styles.TryGetValue(name, out var apply))
                throw new KeyNotFoundException($"Theme '{Name}' has no style '{name}'.");
            apply(element, this);
        }
    }

    /// <summary>Like <see cref="Apply"/>, but unknown names are skipped (used when swapping <see cref="Themes.Current"/>).</summary>
    internal void ApplyAvailable(VisualElement element, string[] styleNames)
    {
        if (element == null || styleNames == null) return;
        for (int i = 0; i < styleNames.Length; i++)
        {
            var name = styleNames[i];
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (_styles.TryGetValue(name, out var apply))
                apply(element, this);
        }
    }

    private static string RequireToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token name is required.", nameof(token));
        return token.Trim();
    }
}
