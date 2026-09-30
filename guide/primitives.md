# Primitives package

[`Blossom.Primitives`](../src/Blossom.Primitives/README.md) is the optional element library, parallel to [`Blossom.Reactive`](../src/Blossom.Reactive/README.md):

| Package | Job |
|---|---|
| `Blossom` | Platform: window, tree, draw, input |
| `Blossom.Reactive` | Signals / memos / `For.Each` |
| `Blossom.Primitives` | Layout hosts, `Colours` scales, `Theme`, and future widgets |

Core does not gain Flexbox or Grid as `VisualElement` properties. You add a `Stack`, `Grid`, or `Split` node; that node assigns child frames.

See the package README for API examples. `Layout.SetManual` opts a child out so a 3D stage (or any hand-placed node) can live beside flowed siblings.

The Studio demo now builds its chrome with these hosts: header/status `Stack`, tab slot `Grid`, Tasks board `Grid` of column `Stack`s, Controls `Split` plus form `Stack`. Widget internals that need custom geometry (caret, slider thumb, 3D stage well) still place their own children.

Named colour scales live on `Colours` (`Colours.Zinc[800]`, Tailwind 50–950). Values are `SKColor`.

Reusable styles and theme swap live on `Theme` / `Themes.Current` (`UseStyle("card")`). Blossom does not ship a look: apps define a theme or set `ElementStyle` on each node. The Studio demo ships Dark / Light / Retro under `src/Blossom.Demo/Themes/` and a header switcher. Recipes write each node's `ElementStyle`; switching `Themes.Current` re-applies bound names.

Tests: `tests/Blossom.Primitives.Tests`.
