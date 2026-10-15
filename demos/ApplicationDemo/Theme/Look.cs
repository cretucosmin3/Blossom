using System;
using Blossom.Primitives;
using Blossom.Reactive;
using SkiaSharp;
using static Blossom.Reactive.ReactiveEngine;

namespace Blossom.Testing.Styling;

public enum ThemeMode
{
    Dark,
    Light
}

/// <summary>
/// The demo's design system. Two signals (mode + accent) drive one <see cref="Theme"/>:
/// an effect rebuilds the sheet and assigns <see cref="Themes.Current"/>, which re-runs every
/// <c>UseStyle</c> recipe in the tree. Components stay headless; all look lives in <see cref="Recipes"/>.
/// </summary>
public static class Look
{
    public static readonly (string Name, ColourScale Scale)[] Accents =
    {
        ("Indigo", Colours.Indigo),
        ("Violet", Colours.Violet),
        ("Rose", Colours.Rose),
        ("Amber", Colours.Amber),
        ("Emerald", Colours.Emerald),
        ("Sky", Colours.Sky),
    };

    public static readonly Signal<ThemeMode> Mode = CreateSignal(ThemeMode.Dark);
    public static readonly Signal<string> Accent = CreateSignal("Indigo");

    /// <summary>The active sheet as a signal, so <c>Bind</c> effects can depend on theme tokens.</summary>
    public static readonly Signal<Theme> Current = CreateSignal(Build(ThemeMode.Dark, "Indigo"));

    private static Effect? _driver;

    public static void Initialize()
    {
        if (_driver != null) return;
        _driver = CreateEffect(() =>
        {
            var theme = Build(Mode.Value, Accent.Value);
            Themes.Current = theme;
            Current.Value = theme;
        });
    }

    public static ColourScale AccentScale(string name)
    {
        foreach (var (n, scale) in Accents)
            if (n == name) return scale;
        return Colours.Indigo;
    }

    private static Theme Build(ThemeMode mode, string accent)
    {
        var a = AccentScale(accent);
        var z = Colours.Zinc;
        var th = new Theme($"{mode} {accent}");
        bool dark = mode == ThemeMode.Dark;

        if (dark)
        {
            th.SetColour("bg", z[950]);
            th.SetColour("sidebar", new SKColor(0x0C, 0x0C, 0x0F));
            th.SetColour("surface", new SKColor(0x13, 0x13, 0x16));
            th.SetColour("surface-raised", z[900]);
            th.SetColour("muted", z[900]);
            th.SetColour("hover", z[800]);
            th.SetColour("press", z[700]);
            th.SetColour("border", new SKColor(0x23, 0x23, 0x27));
            th.SetColour("border-strong", z[700]);
            th.SetColour("input", z[800]);
            th.SetColour("text", z[50]);
            th.SetColour("text-muted", z[400]);
            th.SetColour("text-subtle", z[500]);
            th.SetColour("primary", a[500]);
            th.SetColour("primary-hover", a[400]);
            th.SetColour("primary-press", a[600]);
            th.SetColour("primary-text", a[300]);
            th.SetColour("on-primary", accent is "Amber" ? z[950] : Colours.White);
            th.SetColour("primary-soft", a[500].WithAlpha(38));
            th.SetColour("danger", Colours.Red[500]);
            th.SetColour("danger-hover", Colours.Red[400]);
            th.SetColour("danger-text", Colours.Red[400]);
            th.SetColour("danger-soft", Colours.Red[500].WithAlpha(36));
            th.SetColour("success", Colours.Emerald[500]);
            th.SetColour("success-text", Colours.Emerald[400]);
            th.SetColour("success-soft", Colours.Emerald[500].WithAlpha(36));
            th.SetColour("warning", Colours.Amber[500]);
            th.SetColour("warning-text", Colours.Amber[400]);
            th.SetColour("warning-soft", Colours.Amber[500].WithAlpha(36));
            th.SetColour("info", Colours.Sky[500]);
            th.SetColour("info-text", Colours.Sky[400]);
            th.SetColour("info-soft", Colours.Sky[500].WithAlpha(36));
            th.SetColour("track", z[800]);
            th.SetColour("thumb", Colours.White);
            th.SetColour("selection", a[500].WithAlpha(90));
            th.SetColour("overlay", new SKColor(0, 0, 0, 150));
            th.SetColour("shadow", new SKColor(0, 0, 0, 110));
            th.SetColour("scrollbar", z[50].WithAlpha(40));
            th.SetColour("scrollbar-hover", z[50].WithAlpha(90));
        }
        else
        {
            th.SetColour("bg", z[50]);
            th.SetColour("sidebar", Colours.White);
            th.SetColour("surface", Colours.White);
            th.SetColour("surface-raised", Colours.White);
            th.SetColour("muted", z[100]);
            th.SetColour("hover", z[100]);
            th.SetColour("press", z[200]);
            th.SetColour("border", z[200]);
            th.SetColour("border-strong", z[300]);
            th.SetColour("input", z[300]);
            th.SetColour("text", z[950]);
            th.SetColour("text-muted", z[500]);
            th.SetColour("text-subtle", z[400]);
            th.SetColour("primary", a[600]);
            th.SetColour("primary-hover", a[500]);
            th.SetColour("primary-press", a[700]);
            th.SetColour("primary-text", a[700]);
            th.SetColour("on-primary", accent is "Amber" ? z[950] : Colours.White);
            th.SetColour("primary-soft", a[500].WithAlpha(26));
            th.SetColour("danger", Colours.Red[600]);
            th.SetColour("danger-hover", Colours.Red[500]);
            th.SetColour("danger-text", Colours.Red[600]);
            th.SetColour("danger-soft", Colours.Red[500].WithAlpha(24));
            th.SetColour("success", Colours.Emerald[600]);
            th.SetColour("success-text", Colours.Emerald[700]);
            th.SetColour("success-soft", Colours.Emerald[500].WithAlpha(26));
            th.SetColour("warning", Colours.Amber[500]);
            th.SetColour("warning-text", Colours.Amber[700]);
            th.SetColour("warning-soft", Colours.Amber[500].WithAlpha(30));
            th.SetColour("info", Colours.Sky[600]);
            th.SetColour("info-text", Colours.Sky[700]);
            th.SetColour("info-soft", Colours.Sky[500].WithAlpha(26));
            th.SetColour("track", z[200]);
            th.SetColour("thumb", Colours.White);
            th.SetColour("selection", a[500].WithAlpha(60));
            th.SetColour("overlay", new SKColor(9, 9, 11, 90));
            th.SetColour("shadow", new SKColor(0, 0, 0, 28));
            th.SetColour("scrollbar", z[950].WithAlpha(40));
            th.SetColour("scrollbar-hover", z[950].WithAlpha(90));
        }

        th.SetNumber("radius-sm", 6f);
        th.SetNumber("radius", 8f);
        th.SetNumber("radius-lg", 12f);
        th.SetNumber("dark", dark ? 1f : 0f);

        Recipes.Register(th);
        return th;
    }

    /// <summary>Linear blend between two colours (used by animated controls).</summary>
    public static SKColor Mix(SKColor from, SKColor to, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new SKColor(
            (byte)(from.Red + (to.Red - from.Red) * t),
            (byte)(from.Green + (to.Green - from.Green) * t),
            (byte)(from.Blue + (to.Blue - from.Blue) * t),
            (byte)(from.Alpha + (to.Alpha - from.Alpha) * t));
    }
}
