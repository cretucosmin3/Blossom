# Elements and layout

## Tree

Apps are object graphs of `VisualElement` nodes. Each node has:

- `Transform` — local/absolute frame, anchors, 3D
- `ElementStyle` — fill, border, shadow, text, shaders (share one instance across nodes, or name recipes on `Theme` in Primitives)
- `Events` — pointer and keyboard

Parents own children. `Parent = x` calls `AddChild`. For chrome that must not participate in the parent's layout (scrollbar thumbs), use `SetParentInternal`.

`GetVisualChildren` yields content then visible chrome. `OverflowMode.Clip` (not `Hidden`) clips drawing and hit-testing.

Lifecycle hooks you can use when writing a primitive: `ParentChanged`, `Disposed`, `IndexOfChild`, `Tag`, `OnSizeChanged` / `SizeChanged`, `LayoutChildren`.

## Coordinates

`new Transform(x, y, w, h)` is **local**. Getters are honest about space:

```csharp
el.Transform.LocalX / LocalY
el.Transform.AbsoluteX / AbsoluteY
el.Transform.SetLocalFrame(x, y, w, h);
el.Transform.SetAbsoluteFrame(absX, absY, w, h);
```

Do not mix constructor locals with a mental model of “X is always screen”.

## Anchors

`Left` / `Right` / `Top` / `Bottom` — stretch vs fixed vs proportional relative to the parent (or the view, for a root).

Host views: `View.Width` / `Height` follow the client size so a root with `Left|Right|Top|Bottom` fills the window. There is no host scale/letterbox matrix.

**Stretch reseed:** the first time a parent (or view) has a real non-zero size, stretch `L|R` / `T|B` captures remaining width/height from that size. If you attach against the design-canvas fallback (1000×1000) while the window is still 0×0, insets freeze (a 280 px gap on a 1280-wide window is `1280 - 1000`). `Shell` no longer swaps `RenderRect` to the design canvas at init; stretch still reseeds on first real parent size.

Components authored at a design size embed into a slot with `PluginEmbed`: the plugin root is set to the slot size and **anchors reflow**.

## Custom layout

Override `VisualElement.LayoutChildren` and position children with `SetAbsoluteFrame` / `SetLocalFrame`. That is the extension point. Core does not ship Flexbox or Grid as element properties.

For row/column/grid/split **hosts** that place and size children, use [`Blossom.Primitives`](../src/Blossom.Primitives/README.md) (`Stack`, `Grid`, `Split`), included in the `Blossom` package. See [Primitives](primitives.md).

While `LayoutMutationDepth` is non-zero, `Transform.OnChanged` is suppressed (avoids a layout loop). **Width/height changes still raise `SizeChanged`**, so a nested primitive can relayout itself.

`GetPreferredSize` is the measure hook for userland layout engines.

## Scroll

`ScrollContainer` applies scroll offsets into child computed positions and paints scrollbar chrome. Chrome must use `SetParentInternal` so `For.Each` / column stacks do not lay the thumb out as an extra child.

## Sample widgets

`src/Blossom.Demo/Components` (`Button`, `InputField`, `Slider`, `StackPanel`, `Modal`, …) show how to subclass `VisualElement`. They are not the public control library. Until Blossom ships primitives, copy the pattern, do not take a package dependency on the demo.
