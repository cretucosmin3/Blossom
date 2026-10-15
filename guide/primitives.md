# Primitives package

[`Blossom.Primitives`](../src/Blossom.Primitives/README.md) is the element library, parallel to [`Blossom.Reactive`](../src/Blossom.Reactive/README.md). All three ship in the **`Blossom`** NuGet package.

| Assembly | Job |
|---|---|
| `Blossom` | Platform: window, tree, draw, input |
| `Blossom.Reactive` | Signals / memos / `For.Each` |
| `Blossom.Primitives` | Layout hosts, `Colours` scales, `Theme`, and future widgets |

Core does not gain Flexbox or Grid as `VisualElement` properties. You add a `Stack`, `Grid`, or `Split` node; that node assigns child frames.

See the package README for API examples. `Layout.SetManual` opts a child out so a 3D stage (or any hand-placed node) can live beside flowed siblings.

The Gallery demo builds its frame with these hosts: a `Grid` for sidebar / content, `Stack`s for bars and cards, a `Grid` page slot and Tasks board. `Grid` fills its slot (flowed rows are stars), so content that must size to its children inside a scroller uses the demo's `ColumnLayout` / `WrapPanel`, written on the same `LayoutElement` hook. Widget internals that need custom geometry (caret, slider thumb, 3D stage) still place their own children.

Named colour scales live on `Colours` (`Colours.Zinc[800]`, Tailwind 50–950). Values are `SKColor`.

Reusable styles and theme swap live on `Theme` / `Themes.Current` (`UseStyle("card")`). Blossom does not ship a look: apps define a theme or set `ElementStyle` on each node. The Gallery demo builds Dark / Light × six accents from signals in `demos/ApplicationDemo/Theme/Look.cs`; all recipes live in `Theme/Recipes.cs`. Recipes write each node's `ElementStyle`; switching `Themes.Current` re-applies bound names.

Tests: `tests/Blossom.Primitives.Tests`.
