# Blossom.Primitives

Optional **custom elements** for Blossom, in the same role as [`Blossom.Reactive`](../Blossom.Reactive/README.md) for state: a library on top of `VisualElement`, not a second platform.

This package is where layout hosts and (later) widgets live. Core stays hooks; you opt in with a project reference.

```xml
<ProjectReference Include="..\Blossom.Primitives\Blossom.Primitives.csproj" />
```

```csharp
using Blossom.Primitives;
```

## Layout hosts

These **place and size** their children. You `AddChild`; they write `SetAbsoluteFrame`. Skip a child with `Layout.SetManual(el, true)` if you still want to position it yourself.

Lengths: `Auto` (preferred size), pixels (`200`), stars (`*` / `2*`) for leftover space.

### Stack

Row or column, gap, cross-axis align (default **Stretch**).

```csharp
var bar = new Stack { Orientation = Orientation.Horizontal, Gap = 8 };
bar.AddChild(logo);
bar.AddChild(search);
bar.AddChild(user);
Stack.SetGrow(search, 1f); // search eats leftover width
```

### Grid

Tracks define size; tree order **flows** row-major into empty cells. `Grid.SetCell` pins a child. Default align is **Stretch** (the grid sizes the button).

```csharp
var g = new Grid
{
    Columns = "200, *, 120",
    Rows = "Auto, *",
    ColumnGap = 12,
    RowGap = 12,
};
g.AddChild(sidebar);
g.AddChild(header);
g.AddChild(main);
g.AddChild(ok);
Grid.SetCell(sidebar, column: 0, row: 0, rowSpan: 2);
Grid.SetCell(header, column: 1, row: 0, columnSpan: 2);
```

### Split

First two participating children become panes. `Ratio` is the first pane’s share until the user drags the bar.

```csharp
var split = new Split { Orientation = Orientation.Horizontal, Ratio = 0.3f, BarSize = 6 };
split.AddChild(nav);
split.AddChild(stage);

split.IsResizable = false;                 // fixed panes; bar is a divider only
split.BarSize = 4;
split.Bar.Style.BackColor = Colours.Zinc[700];
split.Bar.Style.Border.Roundness = 2;
```

`BarSize = 0` hides the divider and places the panes flush. Style the drag line through `Bar` (`VisualElement`): colour, opacity, roundness, or your own children.

## Colours

Tailwind’s default scales as `SKColor` values. Shades are `50` (light) through `950` (dark).

```csharp
el.Style.BackColor = Colours.Zinc[800];
el.Style.Border.Color = Colours.Zinc[700];
el.Style.Text.Color = Colours.Indigo[500].WithAlpha(80);
```

Families: `Slate`, `Gray` (`Grey` is the same scale), `Zinc`, `Neutral`, `Stone`, `Red`, `Orange`, `Amber`, `Yellow`, `Lime`, `Green`, `Emerald`, `Teal`, `Cyan`, `Sky`, `Blue`, `Indigo`, `Violet`, `Purple`, `Fuchsia`, `Pink`, `Rose`, plus `White`, `Black`, and `Transparent`. A bad shade throws `ArgumentOutOfRangeException`; `TryGet` is the quiet path.

## Themes

A <c>Theme</c> stores colour/number tokens and named recipes that write an element's own <c>ElementStyle</c>. You can also assign one <c>ElementStyle</c> instance to many nodes (`a.Style = b.Style = card`) if you want shared mutation.

```csharp
var dark = new Theme("dark");
dark.SetColour("surface", Colours.Zinc[800]);
dark.SetColour("text", Colours.Zinc[50]);
dark.SetNumber("radius", 8f);
dark.SetStyle("card", (el, th) =>
{
    el.Style.BackColor = th.Colour("surface");
    el.Style.Border.Roundness = th.Number("radius");
});
dark.SetStyle("title", (el, th) =>
{
    el.Style.Text.Color = th.Colour("text");
    el.Style.Text.Size = 18f;
    el.Style.Text.Weight = 700;
});

Themes.Current = dark;
panel.UseStyle("card");
heading.UseStyle("title");

Themes.Current = light; // every UseStyle element re-runs its recipes against the new theme
```

Blossom does not ship a look. Skip `Theme` and set `ElementStyle` on each node, or build your own sheet and assign `Themes.Current`. Use the `th` argument inside recipes so a theme switch re-reads tokens. Later names in `UseStyle("card", "accent")` overwrite earlier fields.

The Studio demo defines Dark / Light / Retro in `src/Blossom.Demo/Themes/DemoThemes.cs` and switches them from the header.

## Later

Buttons, fields, and charts belong in this project as further `VisualElement` subclasses, on the same host contracts as app-authored primitives. Demo widgets under `Blossom.Demo/Components` stay samples until they move here.
