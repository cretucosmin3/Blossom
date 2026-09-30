using System;
using Blossom.Core.Visual;
using Blossom.Primitives;
using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>
/// Applies <see cref="Themes.Current"/> to demo widgets and re-paints when the theme changes.
/// </summary>
internal static class ThemeChrome
{
    public static void Bind(VisualElement element, Action apply)
    {
        apply();
        Action<Theme> onTheme = _ => apply();
        Themes.CurrentChanged += onTheme;
        element.Disposed += _ => Themes.CurrentChanged -= onTheme;
    }

    public static SKColor Colour(string token, SKColor fallback)
        => Themes.Current.TryColour(token, out var c) ? c : fallback;

    public static float Number(string token, float fallback)
        => Themes.Current.Number(token, fallback);

    public static void Square(VisualElement el)
    {
        float r = Number("radius", 0f);
        el.Style.Border.Roundness = r;
        el.Style.Border.RoundnessTopLeft = r;
        el.Style.Border.RoundnessTopRight = r;
        el.Style.Border.RoundnessBottomLeft = r;
        el.Style.Border.RoundnessBottomRight = r;
    }
}
