# Blossom guide

Blossom is a **retained-mode UI framework for C#**. It owns a native window, a tree of `VisualElement` nodes, Skia drawing, and input. It is not an HTML/CSS engine.

**Mental model:** `VisualElement` is the platform. Buttons, fields, terminals, and charts are subclasses of that node (userland today, Blossom primitives later). Core invests in host contracts so those authors do not need reflection or a private event pump.

```text
Shell (static process host)
  └── Application
        └── View (one active)
              └── VisualElement tree
                    ├── Transform (anchors + 3D matrix)
                    ├── ElementStyle
                    └── ElementEvents / EventMap
```

## Who this is for

- App authors who reference the `Blossom` package (or `src/Blossom/Blossom.csproj`) and call `Shell.Initialize`.
- Anyone building a custom primitive (field, terminal, chart) on `VisualElement`.
- Contributors who need the current public surface.

## Pages

| Page | Contents |
|---|---|
| [Getting started](getting-started.md) | Stack, build, first `Application` |
| [Host](host.md) | `Shell`, window size, threading, idle loop, stats overlay |
| [Elements and layout](elements-and-layout.md) | Tree, anchors, `LayoutChildren`, stretch reseed |
| [Input](input.md) | Keyboard, mouse, overlays, hit-testing |
| [Drawing and 3D](drawing-and-3d.md) | Retained paint, shaders, `SKMatrix44`, unprojection |
| [Fonts and text](fonts-and-text.md) | Register faces, `SKFont`, `TextLayout` caret |
| [Primitives](primitives.md) | `Blossom.Primitives`: Stack, Grid, Split, Colours, Theme |

Reactive signals live in [`src/Blossom.Reactive/README.md`](../src/Blossom.Reactive/README.md). Layout hosts and future widgets live in [`src/Blossom.Primitives/README.md`](../src/Blossom.Primitives/README.md).

## Product rules

| Invest in | Leave out of core unless asked |
|---|---|
| Element primitives (transform, style, clip, opacity, hit-test, draw) | Full web/CSS parity |
| Input correctness (capture, `Handled`, click policy) | Focus rings / `TabIndex` / focus traps |
| Tree lifecycle + layout **hooks** | Built-in Flexbox / Grid |
| Scrollable region + scrollbars | `ScrollIntoView` |
| Host contracts every primitive reuses | A large shipped control library **in this pass** |

Demo widgets under `src/Blossom.Demo/Components` are samples, not a versioned control API.
