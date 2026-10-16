using Blossom.Primitives;
using Blossom.Reactive;
using ReactiveLab.State;
using SkiaSharp;
using static Blossom.Reactive.ReactiveEngine;

namespace ReactiveLab.Ui;

/// <summary>All colours the lab uses. A record, so the palette memo only notifies when a colour actually differs.</summary>
public sealed record Palette(
    bool Dark,
    SKColor Bg,
    SKColor Panel,
    SKColor Surface,
    SKColor SurfaceHover,
    SKColor Border,
    SKColor Text,
    SKColor Muted,
    SKColor Accent,
    SKColor AccentHover,
    SKColor OnAccent,
    SKColor Danger,
    SKColor Success,
    SKColor Warning,
    SKColor Info)
{
    public SKColor PriorityColour(Priority p) => p switch
    {
        Priority.Urgent => Danger,
        Priority.High => Warning,
        Priority.Medium => Info,
        _ => Muted,
    };

    public SKColor StageColour(Stage s) => s switch
    {
        Stage.Doing => Warning,
        Stage.Review => Info,
        Stage.Done => Success,
        _ => Muted,
    };
}

/// <summary>
/// Theme as plain reactive state: every element binds its colours to <see cref="Current"/>, so a
/// theme switch is one signal write fanning out to thousands of effects (one per bound element).
/// </summary>
public static class Look
{
    public static readonly Signal<bool> Dark = CreateSignal(true);
    public static readonly Signal<int> Accent = CreateSignal(0);

    public static readonly string[] AccentNames = { "Indigo", "Emerald", "Rose", "Amber" };
    private static readonly ColourScale[] Accents = { Colours.Indigo, Colours.Emerald, Colours.Rose, Colours.Amber };

    public static readonly Memo<Palette> Current = CreateMemo(() =>
    {
        var a = Accents[Accent.Value];
        return Dark.Value
            ? new Palette(true,
                Bg: Colours.Zinc[950], Panel: Colours.Zinc[900], Surface: Colours.Zinc[800], SurfaceHover: Colours.Zinc[700],
                Border: Colours.Zinc[700], Text: Colours.Zinc[50], Muted: Colours.Zinc[400],
                Accent: a[500], AccentHover: a[400], OnAccent: SKColors.White,
                Danger: Colours.Red[500], Success: Colours.Emerald[500], Warning: Colours.Amber[500], Info: Colours.Sky[500])
            : new Palette(false,
                Bg: Colours.Zinc[100], Panel: Colours.White, Surface: Colours.White, SurfaceHover: Colours.Zinc[100],
                Border: Colours.Zinc[200], Text: Colours.Zinc[900], Muted: Colours.Zinc[500],
                Accent: a[600], AccentHover: a[500], OnAccent: SKColors.White,
                Danger: Colours.Red[600], Success: Colours.Emerald[600], Warning: Colours.Amber[600], Info: Colours.Sky[600]);
    });
}
