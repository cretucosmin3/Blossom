using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Testing.Components;
using Blossom.Testing.Frame;
using SkiaSharp;

namespace Blossom.Testing.Styling;

/// <summary>
/// Every visual decision in the demo. Components call <c>UseStyle("name")</c> (again on hover / press /
/// focus); a theme switch re-runs the same recipes against new tokens. Recipes read component state
/// (variant, hover, checked) but never change layout or behaviour.
/// </summary>
internal static class Recipes
{
    public static void Register(Theme th)
    {
        Surfaces(th);
        Typography(th);
        Controls(th);
        Inputs(th);
        Data(th);
        Overlays(th);
    }

    private static void Surfaces(Theme th)
    {
        th.SetStyle("app", (el, t) => el.Style.BackColor = t.Colour("bg"));

        th.SetStyle("sidebar", (el, t) =>
        {
            el.Style.BackColor = t.Colour("sidebar");
        });

        th.SetStyle("sidebar-edge", (el, t) => el.Style.BackColor = t.Colour("border"));

        th.SetStyle("topbar", (el, t) => el.Style.BackColor = t.Colour("bg"));

        th.SetStyle("divider", (el, t) => el.Style.BackColor = t.Colour("border"));

        th.SetStyle("card", (el, t) =>
        {
            el.Style.BackColor = t.Colour("surface");
            Border(el, t.Colour("border"), t.Number("radius-lg"));
            Shadow(el, 0, 1, 2, t.Colour("shadow").WithAlpha((byte)(t.Number("dark") > 0 ? 60 : 14)));
        });

        th.SetStyle("card-muted", (el, t) =>
        {
            el.Style.BackColor = t.Colour("muted");
            Border(el, t.Colour("border"), t.Number("radius"));
        });

        th.SetStyle("column", (el, t) =>
        {
            el.Style.BackColor = t.Colour("muted").WithAlpha((byte)(t.Number("dark") > 0 ? 140 : 255));
            Border(el, t.Colour("border"), t.Number("radius-lg"));
        });

        th.SetStyle("task-card", (el, t) =>
        {
            el.Style.BackColor = t.Colour("surface-raised");
            Border(el, t.Colour("border"), t.Number("radius"));
            Shadow(el, 0, 1, 2, t.Colour("shadow").WithAlpha((byte)(t.Number("dark") > 0 ? 70 : 16)));
        });

        th.SetStyle("icon-tile", (el, t) =>
        {
            el.Style.BackColor = t.Colour("primary-soft");
            Border(el, SKColors.Transparent, t.Number("radius"), 0);
            if (el is Icon icon) icon.Color = t.Colour("primary-text");
        });

        th.SetStyle("scroll", (el, t) =>
        {
            if (el is not ScrollContainer s) return;
            s.ScrollbarThumbColor = t.Colour("scrollbar");
            s.ScrollbarThumbHoverColor = t.Colour("scrollbar-hover");
            s.ScrollbarThumbDragColor = t.Colour("scrollbar-hover");
            s.ScrollbarThickness = 6f;
            s.ScrollbarRadius = 3f;
        });

        th.SetStyle("stage", (el, t) =>
        {
            Border(el, t.Colour("border"), t.Number("radius-lg"));
        });

        th.SetStyle("glass", (el, t) =>
        {
            bool dark = t.Number("dark") > 0;
            el.Style.BackColor = (dark ? SKColors.Black : SKColors.White).WithAlpha((byte)(dark ? 80 : 150));
            Border(el, SKColors.White.WithAlpha(90), 20f);
            Shadow(el, 0, 18, 24, new SKColor(0, 0, 0, 90));
        });
    }

    private static void Typography(Theme th)
    {
        Text(th, "text.display", 26f, 700, "text");
        Text(th, "text.title", 18f, 600, "text");
        Text(th, "text.heading", 15f, 600, "text");
        Text(th, "text.strong", 13.5f, 600, "text");
        Text(th, "text.body", 13.5f, 400, "text");
        Text(th, "text.muted", 13f, 400, "text-muted");
        Text(th, "text.small", 12f, 400, "text-muted");
        Text(th, "text.caption", 11f, 600, "text-subtle");
        Text(th, "text.label", 12.5f, 500, "text");
        Text(th, "text.value", 28f, 700, "text");
        Text(th, "text.brand", 16f, 700, "text");
        Text(th, "text.error", 12f, 500, "danger-text");
        Text(th, "text.ok", 12f, 500, "success-text");
        Text(th, "text.accent", 12.5f, 600, "primary-text");
        Text(th, "text.glass-title", 22f, 700, "text");
        Text(th, "text.glass", 13f, 400, "text");
    }

    private static void Controls(Theme th)
    {
        th.SetStyle("button", (el, t) =>
        {
            if (el is not Button b) return;
            bool hover = b.IsHovered && b.Enabled;
            bool press = b.IsPressed && b.Enabled;
            float radius = t.Number("radius");

            SKColor back, content, border = SKColors.Transparent;
            switch (b.Variant)
            {
                case ButtonVariant.Primary:
                    back = t.Colour(press ? "primary-press" : hover ? "primary-hover" : "primary");
                    content = t.Colour("on-primary");
                    break;
                case ButtonVariant.Destructive:
                    back = t.Colour(press ? "danger" : hover ? "danger-hover" : "danger");
                    content = SKColors.White;
                    break;
                case ButtonVariant.Secondary:
                    back = t.Colour(press ? "press" : hover ? "border-strong" : "hover");
                    content = t.Colour("text");
                    break;
                case ButtonVariant.Outline:
                    back = press ? t.Colour("press") : hover ? t.Colour("hover") : t.Colour("surface");
                    content = t.Colour("text");
                    border = t.Colour("input");
                    break;
                default:
                    back = press ? t.Colour("press") : hover ? t.Colour("hover") : SKColors.Transparent;
                    content = hover ? t.Colour("text") : t.Colour("text-muted");
                    break;
            }

            b.Style.BackColor = back;
            Border(b, border, radius, border.Alpha > 0 ? 1f : 0f);
            b.ContentColor = content;
            b.Opacity = b.Enabled ? 1f : 0.45f;
            bool lifted = b.Variant is ButtonVariant.Primary or ButtonVariant.Destructive;
            Shadow(b, 0, 1, 2, lifted && !press ? t.Colour("shadow").WithAlpha(70) : SKColors.Transparent);
        });

        th.SetStyle("nav-item", (el, t) =>
        {
            if (el is not NavItem n) return;
            el.Style.BackColor = n.Active ? t.Colour("primary-soft") : n.IsHovered ? t.Colour("hover") : SKColors.Transparent;
            Border(el, SKColors.Transparent, t.Number("radius"), 0);
            n.Colour(
                n.Active ? t.Colour("primary-text") : n.IsHovered ? t.Colour("text") : t.Colour("text-muted"),
                n.Active ? t.Colour("on-primary") : t.Colour("text-muted"),
                n.Active ? t.Colour("primary") : t.Colour("hover"));
        });

        th.SetStyle("toggle", (el, t) =>
        {
            if (el is not PaintedToggle p) return;
            p.Palette = new TogglePalette
            {
                On = t.Colour("primary"),
                Off = t.Colour("track"),
                OffBorder = t.Colour("input"),
                Hover = t.Colour("border-strong"),
                Thumb = t.Colour("thumb"),
                Mark = t.Colour("on-primary"),
                Label = t.Colour("text"),
            };
            p.InvalidatePaint();
        });

        th.SetStyle("segmented", (el, t) =>
        {
            if (el is not Segmented s) return;
            el.Style.BackColor = t.Colour("muted");
            Border(el, t.Colour("border"), t.Number("radius"));
            s.PillColor = t.Number("dark") > 0 ? t.Colour("press") : t.Colour("surface");
            s.TextColor = t.Colour("text-muted");
            s.ActiveTextColor = t.Colour("text");
            s.InvalidatePaint();
        });
    }

    private static void Inputs(Theme th)
    {
        th.SetStyle("field", (el, t) =>
        {
            if (el is not TextField f) return;
            SKColor border = f.Invalid ? t.Colour("danger")
                : f.IsFocused ? t.Colour("primary")
                : f.IsHovered ? t.Colour("border-strong")
                : t.Colour("input");
            el.Style.BackColor = t.Number("dark") > 0 ? t.Colour("surface") : SKColors.White;
            Border(el, border, t.Number("radius"));
            var ring = f.Invalid ? t.Colour("danger") : t.Colour("primary");
            Shadow(el, 0, 0, 3, f.IsFocused ? ring.WithAlpha(70) : SKColors.Transparent);
            f.TextColor = t.Colour("text");
            f.PlaceholderColor = t.Colour("text-subtle");
            f.SelectionColor = t.Colour("selection");
            f.CaretColor = t.Colour("primary");
            f.IconColor = f.IsFocused ? t.Colour("primary-text") : t.Colour("text-subtle");
            f.InvalidatePaint();
        });

        th.SetStyle("slider", (el, t) =>
        {
            if (el is not Slider s) return;
            s.TrackColor = t.Colour("track");
            s.FillColor = t.Colour("primary");
            s.ThumbColor = t.Colour("thumb");
            s.InvalidatePaint();
        });
    }

    private static void Data(Theme th)
    {
        th.SetStyle("progress", (el, t) =>
        {
            if (el is not ProgressBar p) return;
            p.TrackColor = t.Colour("track");
            p.FillColor = t.Colour("primary");
            p.InvalidatePaint();
        });

        th.SetStyle("badge", (el, t) =>
        {
            if (el is not Badge b) return;
            string key = b.Tone switch
            {
                Tone.Primary => "primary",
                Tone.Success => "success",
                Tone.Warning => "warning",
                Tone.Danger => "danger",
                Tone.Info => "info",
                _ => ""
            };
            el.Style.BackColor = key == "" ? t.Colour("hover") : t.Colour(key + "-soft");
            el.Style.Text.Color = key == "" ? t.Colour("text-muted") : t.Colour(key + "-text");
            el.Style.Text.Size = 11f;
            el.Style.Text.Weight = 600;
            el.Style.Text.Alignment = TextAlign.Center;
            Border(el, SKColors.Transparent, 10f, 0);
        });

        th.SetStyle("avatar", (el, t) =>
        {
            if (el is not Avatar a) return;
            el.Style.BackColor = a.Hue;
            el.Style.Text.Color = SKColors.White;
            Border(el, t.Colour("bg"), 999f, 2f);
        });

        th.SetStyle("chart", (el, t) =>
        {
            if (el is not BarChart c) return;
            c.Palette = new ChartPalette
            {
                Bar = t.Colour("primary"),
                BarHover = t.Colour("primary-hover"),
                Grid = t.Colour("border"),
                Label = t.Colour("text-subtle"),
                TooltipBack = t.Colour("text"),
                TooltipText = t.Colour("bg"),
            };
            c.InvalidatePaint();
        });

        th.SetStyle("dot", (el, t) => Border(el, SKColors.Transparent, 4f, 0));
    }

    private static void Overlays(Theme th)
    {
        th.SetStyle("dialog-backdrop", (el, t) =>
        {
            el.Style.BackColor = t.Colour("overlay");
            el.Style.BackdropBlur = 6f;
        });

        th.SetStyle("dialog", (el, t) =>
        {
            el.Style.BackColor = t.Colour("surface");
            Border(el, t.Colour("border"), t.Number("radius-lg"));
            Shadow(el, 0, 16, 24, new SKColor(0, 0, 0, 90));
        });

        th.SetStyle("toast", (el, t) =>
        {
            el.Style.BackColor = t.Colour("surface-raised");
            Border(el, t.Colour("border-strong"), t.Number("radius-lg"));
            Shadow(el, 0, 8, 16, new SKColor(0, 0, 0, (byte)(t.Number("dark") > 0 ? 120 : 40)));
        });
    }

    private static void Text(Theme th, string name, float size, int weight, string colour)
    {
        th.SetStyle(name, (el, t) =>
        {
            el.Style.Text.Size = size;
            el.Style.Text.Weight = weight;
            el.Style.Text.Color = t.Colour(colour);
            el.Style.Text.Alignment = TextAlign.Left;
        });
    }

    private static void Border(VisualElement el, SKColor colour, float radius, float width = 1f)
    {
        el.Style.Border.Width = width;
        el.Style.Border.Color = colour;
        el.Style.Border.Roundness = radius;
    }

    private static void Shadow(VisualElement el, float x, float y, float blur, SKColor colour)
    {
        var s = el.Style.Shadow;
        s.OffsetX = x;
        s.OffsetY = y;
        s.SpreadX = blur;
        s.SpreadY = blur;
        s.Color = colour;
    }
}
