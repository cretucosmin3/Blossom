using System;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing.Components;
using SkiaSharp;
using static Blossom.Reactive.ReactiveEngine;

namespace Blossom.Testing;

/// <summary>
/// Demo-only themes. Blossom.Primitives does not ship a look.
/// Dark/Light follow shadcn zinc (squared). Retro is paper/ink, 2px rules.
/// </summary>
public static class DemoThemes
{
    public const string DarkName = "Dark";
    public const string LightName = "Light";
    public const string RetroName = "Retro";

    public static readonly string[] Names = { DarkName, LightName, RetroName };
    public static readonly Signal<string> Active = CreateSignal(DarkName);

    public static Theme Dark { get; } = BuildDark();
    public static Theme Light { get; } = BuildLight();
    public static Theme Retro { get; } = BuildRetro();

    public static void Select(string name)
    {
        Theme theme = name switch
        {
            LightName => Light,
            RetroName => Retro,
            _ => Dark
        };
        string resolved = theme.Name;
        Themes.Current = theme;
        if (Active.Value != resolved)
            Active.Value = resolved;
    }

    public static void Ensure()
    {
        if (Themes.Current.Name is DarkName or LightName or RetroName)
            return;
        Select(DarkName);
    }

    /// <summary>Re-runs when the demo theme changes; use inside <c>Bind</c>.</summary>
    public static Theme Current
    {
        get
        {
            _ = Active.Value;
            return Themes.Current;
        }
    }

    public static void TintButton(Button button, string colourToken, string textToken = "on-accent")
    {
        button.Bind(b =>
        {
            var th = Current;
            b.NormalColor = th.Colour(colourToken);
            b.Style.Text.Color = th.Colour(textToken);
            Square(b, th);
            bool solid = colourToken is "accent" or "danger" or "success";
            b.Style.Border.Width = solid ? 0 : th.Number("stroke", 1f);
            b.Style.Border.Color = th.Colour("border");
            b.Style.Shadow = null!;
        });
    }

    private static void Square(VisualElement el, Theme th)
    {
        float r = th.Number("radius");
        el.Style.Border.Roundness = r;
        el.Style.Border.RoundnessTopLeft = r;
        el.Style.Border.RoundnessTopRight = r;
        el.Style.Border.RoundnessBottomLeft = r;
        el.Style.Border.RoundnessBottomRight = r;
    }

    // shadcn zinc dark — primary is foreground, not a hue
    private static Theme BuildDark() => Fill(new Theme(DarkName),
        app: new SKColor(0x09, 0x09, 0x0B),
        chrome: new SKColor(0x09, 0x09, 0x0B),
        surface: new SKColor(0x09, 0x09, 0x0B),
        header: new SKColor(0x18, 0x18, 0x1B),
        well: new SKColor(0x18, 0x18, 0x1B),
        field: new SKColor(0x09, 0x09, 0x0B),
        border: new SKColor(0x27, 0x27, 0x2A),
        borderStrong: new SKColor(0x3F, 0x3F, 0x46),
        text: new SKColor(0xFA, 0xFA, 0xFA),
        muted: new SKColor(0xA1, 0xA1, 0xAA),
        accent: new SKColor(0xFA, 0xFA, 0xFA),
        ghost: new SKColor(0x27, 0x27, 0x2A),
        danger: new SKColor(0xEF, 0x44, 0x44),
        success: new SKColor(0x22, 0xC5, 0x5E),
        onAccent: new SKColor(0x18, 0x18, 0x1B),
        radius: 0f,
        stroke: 1f);

    // shadcn zinc light
    private static Theme BuildLight() => Fill(new Theme(LightName),
        app: new SKColor(0xFA, 0xFA, 0xFA),
        chrome: new SKColor(0xFF, 0xFF, 0xFF),
        surface: new SKColor(0xFF, 0xFF, 0xFF),
        header: new SKColor(0xF4, 0xF4, 0xF5),
        well: new SKColor(0xF4, 0xF4, 0xF5),
        field: new SKColor(0xFF, 0xFF, 0xFF),
        border: new SKColor(0xE4, 0xE4, 0xE7),
        borderStrong: new SKColor(0xA1, 0xA1, 0xAA),
        text: new SKColor(0x09, 0x09, 0x0B),
        muted: new SKColor(0x71, 0x71, 0x7A),
        accent: new SKColor(0x18, 0x18, 0x1B),
        ghost: new SKColor(0xF4, 0xF4, 0xF5),
        danger: new SKColor(0xDC, 0x26, 0x26),
        success: new SKColor(0x16, 0xA3, 0x4A),
        onAccent: new SKColor(0xFA, 0xFA, 0xFA),
        radius: 0f,
        stroke: 1f);

    // Paper, ink, oxblood. Heavy square rules.
    private static Theme BuildRetro() => Fill(new Theme(RetroName),
        app: new SKColor(0xC8, 0xBF, 0xA8),
        chrome: new SKColor(0xB7, 0xAD, 0x94),
        surface: new SKColor(0xE6, 0xDC, 0xC4),
        header: new SKColor(0xD2, 0xC6, 0xA8),
        well: new SKColor(0xA8, 0x9B, 0x80),
        field: new SKColor(0xEF, 0xE6, 0xD0),
        border: new SKColor(0x2A, 0x24, 0x1C),
        borderStrong: new SKColor(0x1C, 0x16, 0x10),
        text: new SKColor(0x1C, 0x16, 0x10),
        muted: new SKColor(0x5C, 0x53, 0x48),
        accent: new SKColor(0x6B, 0x2D, 0x2D),
        ghost: new SKColor(0xD4, 0xC9, 0xAE),
        danger: new SKColor(0x6B, 0x2D, 0x2D),
        success: new SKColor(0x3E, 0x5A, 0x3A),
        onAccent: new SKColor(0xEF, 0xE6, 0xD0),
        radius: 0f,
        stroke: 2f);

    private static Theme Fill(
        Theme t,
        SKColor app, SKColor chrome, SKColor surface, SKColor header, SKColor well, SKColor field,
        SKColor border, SKColor borderStrong, SKColor text, SKColor muted, SKColor accent,
        SKColor ghost, SKColor danger, SKColor success, SKColor onAccent, float radius, float stroke)
    {
        t.SetColour("app", app);
        t.SetColour("chrome", chrome);
        t.SetColour("surface", surface);
        t.SetColour("header", header);
        t.SetColour("well", well);
        t.SetColour("field", field);
        t.SetColour("border", border);
        t.SetColour("border-strong", borderStrong);
        t.SetColour("text", text);
        t.SetColour("muted", muted);
        t.SetColour("accent", accent);
        t.SetColour("ghost", ghost);
        t.SetColour("danger", danger);
        t.SetColour("success", success);
        t.SetColour("on-accent", onAccent);
        t.SetNumber("radius", radius);
        t.SetNumber("radius-sm", radius);
        t.SetNumber("stroke", stroke);

        t.SetStyle("app", (el, th) =>
        {
            el.Style.BackColor = th.Colour("app");
            el.Style.Border.Width = 0;
            Square(el, th);
        });
        t.SetStyle("chrome", (el, th) =>
        {
            el.Style.BackColor = th.Colour("chrome");
            el.Style.Border.Color = th.Colour("border");
            el.Style.Border.Width = th.Number("stroke", 1f);
            Square(el, th);
            el.Style.Shadow = null!;
        });
        t.SetStyle("nav", (el, th) =>
        {
            el.Style.BackColor = th.Colour("app");
            el.Style.Border.Color = th.Colour("border");
            el.Style.Border.Width = th.Number("stroke", 1f);
            Square(el, th);
        });
        t.SetStyle("surface", (el, th) =>
        {
            el.Style.BackColor = th.Colour("surface");
            el.Style.Border.Color = th.Colour("border");
            el.Style.Border.Width = th.Number("stroke", 1f);
            Square(el, th);
            el.Style.Shadow = null!;
        });
        t.SetStyle("header", (el, th) =>
        {
            el.Style.BackColor = th.Colour("header");
            el.Style.Border.Color = th.Colour("border");
            el.Style.Border.Width = 0;
            Square(el, th);
            el.Style.Shadow = null!;
        });
        t.SetStyle("well", (el, th) =>
        {
            el.Style.BackColor = th.Colour("well");
            el.Style.Border.Color = th.Colour("border");
            el.Style.Border.Width = th.Number("stroke", 1f);
            Square(el, th);
        });
        t.SetStyle("field", (el, th) =>
        {
            el.Style.BackColor = th.Colour("field");
            el.Style.Border.Color = th.Colour("border");
            el.Style.Border.Width = th.Number("stroke", 1f);
            Square(el, th);
        });
        t.SetStyle("card", (el, th) =>
        {
            el.Style.BackColor = th.Colour("surface");
            el.Style.Border.Color = th.Colour("border");
            el.Style.Border.Width = th.Number("stroke", 1f);
            Square(el, th);
        });
        t.SetStyle("card-done", (el, th) =>
        {
            el.Style.BackColor = th.Colour("header");
            el.Style.Border.Color = th.Colour("border");
            el.Style.Border.Width = th.Number("stroke", 1f);
            Square(el, th);
        });
        t.SetStyle("badge", (el, th) =>
        {
            el.Style.BackColor = th.Colour("ghost");
            el.Style.Border.Color = th.Colour("border");
            el.Style.Border.Width = th.Number("stroke", 1f);
            Square(el, th);
        });
        t.SetStyle("split-bar", (el, th) =>
        {
            el.Style.BackColor = th.Colour("border");
            Square(el, th);
        });
        t.SetStyle("progress-track", (el, th) =>
        {
            el.Style.BackColor = th.Colour("ghost");
            el.Style.Border.Width = 0;
            Square(el, th);
        });
        t.SetStyle("progress-fill", (el, th) =>
        {
            el.Style.BackColor = th.Colour("accent");
            el.Style.Border.Width = 0;
            Square(el, th);
        });
        t.SetStyle("logo", (el, th) => Text(el, th.Colour("text"), 18f, 700, TextAlign.Left));
        t.SetStyle("display", (el, th) =>
        {
            Text(el, th.Colour("text"), 20f, 700, TextAlign.Left);
            el.Style.Text.Overflow = TextOverflow.Ellipsis;
            el.Style.Text.MaxLines = 2;
        });
        t.SetStyle("heading", (el, th) => Text(el, th.Colour("text"), 13f, 600, TextAlign.Left));
        t.SetStyle("muted", (el, th) => Text(el, th.Colour("muted"), 12f, 500, TextAlign.Left));
        t.SetStyle("kicker", (el, th) => Text(el, th.Colour("muted"), 11f, 600, TextAlign.Left));
        t.SetStyle("body", (el, th) => Text(el, th.Colour("muted"), 12f, 400, TextAlign.Left));
        t.SetStyle("status", (el, th) => Text(el, th.Colour("muted"), 11f, 400, TextAlign.Left));
        t.SetStyle("status-end", (el, th) => Text(el, th.Colour("muted"), 11f, 400, TextAlign.Right));
        t.SetStyle("empty", (el, th) => Text(el, th.Colour("muted"), 12f, 400, TextAlign.Center));
        t.SetStyle("card-title", (el, th) =>
        {
            Text(el, th.Colour("text"), 13f, 500, TextAlign.Left);
            el.Style.Text.Overflow = TextOverflow.Ellipsis;
            el.Style.Text.MaxLines = 2;
        });
        t.SetStyle("card-title-done", (el, th) =>
        {
            Text(el, th.Colour("muted"), 13f, 500, TextAlign.Left);
            el.Style.Text.Overflow = TextOverflow.Ellipsis;
            el.Style.Text.MaxLines = 2;
        });
        return t;
    }

    private static void Text(VisualElement el, SKColor color, float size, int weight, TextAlign align)
    {
        el.Style.Text.Color = color;
        el.Style.Text.Size = size;
        el.Style.Text.Weight = weight;
        el.Style.Text.Alignment = align;
    }
}
