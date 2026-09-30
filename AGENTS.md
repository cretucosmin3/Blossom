# AGENTS.md — Blossom

Working rules for anyone (human or AI) editing this repository. **Public capability docs** live in [`guide/`](guide/README.md) (getting started, host contracts, 3D/hit-test). Do not recreate `docs/`, `plans/`, `findings/`, or `progress/` trees.

Unattended `agy` runs must `cd` to the repo root and pass `--add-dir` with that absolute path. Print mode otherwise writes into a scratch workspace, and the files never land in this repo.

---

## What Blossom is

Blossom is a **retained-mode UI framework for C#** — a native desktop “browser-like” host for rich 2D/3D UI, not an HTML/CSS engine.

- Window + input: **Silk.NET** (GLFW)
- Drawing: **SkiaSharp** (GPU-accelerated, dirty-rect retained pipeline)
- Apps are C# object graphs of **`VisualElement`** nodes inside **`View`s**, owned by an **`Application`**

**Mental model:** `VisualElement` is the platform. Buttons, lists, flex-like panels, and other widgets are **userland** (or later packages) built on top of the element tree — not the primary investment in core.

### Product philosophy (short)

| Invest in | Do not treat as core product |
|---|---|
| Element primitives (transform, style, clip, opacity, hit-test, draw) | Full web/CSS parity |
| Input correctness (capture, event handled, click policy) | Focus rings / tab order / focus traps |
| Tree lifecycle + layout **hooks** (so others can build layout engines) | Built-in Flexbox / Grid |
| Scrollable region + **scrollbars** | `ScrollIntoView` |
| Extensibility contracts | Shipping a large control library in core |

---

## Repository layout

```text
Blossom/
├── Blossom.sln             # Library + demo
├── .github/workflows/      # NuGet pack / release
├── guide/                  # Public guide (capabilities, APIs, pitfalls)
├── src/
│   ├── Blossom/            # Framework library (class lib)
│   │   ├── Blossom.csproj
│   │   ├── Shell.cs        # Window loop, input wiring, frame timing
│   │   ├── Core/           # Application, View, ElementsMap, EventMap, Renderer, ledger, Gpu, Design
│   │   ├── Visual/         # VisualElement, Transform, Style, ScrollContainer, shaders
│   │   ├── Utils/          # Fonts, imaging helpers
│   │   └── External/       # Vendored quadtree helpers
│   ├── Blossom.Reactive/   # SolidJS-inspired signals, Bind*, For.Each, Show.When
│   ├── Blossom.Primitives/ # Layout hosts, Colours, Theme, future widgets
│   ├── Blossom.Pack/       # One NuGet id `Blossom` (all three libraries)
│   └── Blossom.Demo/       # Sample host app (not the public API)
│       ├── Blossom.Demo.csproj
│       ├── Program.cs
│       ├── TestingApp.cs
│       ├── Views/
│       ├── Components/
│       └── Models/
├── assets/                 # Fonts, images, icons (copied to demo output on build)
├── glfw/                   # Native GLFW libs for packaging
└── README.md
```

Ignore `bin/`, `obj/`, and large build outputs under `dist/` or `production/` if present.

---

## Stack

| Piece | Tech |
|---|---|
| Language | C# |
| TFM | **.NET 10** (`net10.0`) |
| Window / input | Silk.NET 2.23 (GLFW) |
| Graphics | SkiaSharp 4.153 (+ SVG, ImageSharp) |
| Solution | `Blossom.sln` — libraries + pack (`Blossom` nupkg) + demo |

---

## Core architecture (where to look)

### Runtime hierarchy

```text
Shell (static host)
  └── Application
        └── View (one active)
              └── VisualElement tree
                    ├── Transform (layout anchors + 3D matrix)
                    ├── ElementStyle (fill, border, shadow, text, effects)
                    └── ElementEvents / EventMap (input)
```

| Concern | Primary files |
|---|---|
| Window + input entry | `src/Blossom/Shell.cs` |
| App / multi-view | `src/Blossom/Core/Application.cs` |
| View render + mouse routing | `src/Blossom/Core/View.cs` |
| Hit-test tree | `src/Blossom/Core/ElementsMap.cs` |
| Events | `src/Blossom/Core/EventMap.cs`, `src/Blossom/Visual/ElementEvents.cs` |
| Element node | `src/Blossom/Visual/VisualElement.cs` |
| Layout / anchors / matrices | `src/Blossom/Visual/Transform.cs`, `src/Blossom/Visual/Enums/Anchor.cs` |
| Style | `src/Blossom/Visual/ElementStyle.cs`, `src/Blossom/Visual/Style/*` |
| Scroll | `src/Blossom/Visual/ScrollContainer.cs` |
| Draw commands | `src/Blossom/Core/CommandLedger.cs`, `VisualElement.RecordDrawCommands` |
| GPU context / offscreen surfaces | `src/Blossom/Core/Gpu.cs` |
| Shaders / effects | `src/Blossom/Visual/Style/SKSLShaders.cs` |
| Design canvas, units, plugin embed | `src/Blossom/Core/Design/` |
| Demo shell | `src/Blossom.Demo/TestingApp.cs`, `src/Blossom.Demo/Views/*` |
| Sample widgets | `src/Blossom.Demo/Components/*` (not formal public controls) |

### Layout today

- **Product model:** apps fill the window; components reflow with anchors relative to their parent/slot.
- Host views: layout width/height track the window client size so root content with `Left|Right|Top|Bottom` fills the app. No host scale/letterbox matrix.
- Components / plugins: author against a design size, then embed into a slot; `PluginEmbed` sets the root to the slot size and **anchors reflow**.
- Isolation workflow: `ComponentDesignView` previews plugins against different slot sizes (anchor reflow).
- Anchors: `Left` / `Right` / `Top` / `Bottom` (stretch vs fixed vs proportional)
- `ScrollContainer` applies scroll offsets into child computed positions
- Custom layout engines plug in through `VisualElement.LayoutChildren` (layout vs paint dirty). Do **not** add Flexbox as a core system unless explicitly requested.

### Rendering

- Retained mode: dirty rects, scissor clip, painter’s order (ZIndex, tree depth, registration order)
- Advanced: SKSL backgrounds, backdrop blur, 3D transforms with inverse hit-test
- Event-driven idle: when no dirty rects or render flags are active, the engine sleeps to preserve CPU/power; only invalidate when state actually changes.

---

## Testing / demos

`src/Blossom.Demo` hosts the primary manual test harness and sample test components (not the framework API boundary):

- Single active application view: **Blossom Studio** (`StudioView`), exercising fine-grained reactive signals (`Blossom.Reactive`), keyed list reconciliation (`For.Each`), two-way and derived form bindings.
- Tabs under `Blossom.Demo/Tabs`: **TasksTab** (reactive 3-column Kanban board with keyed list reconciliation, search, and progress metrics), and **ControlsTab** (two-way signal inputs, sliders, switches, live preview, and batch updates).
- Components under `Blossom.Demo/Components`: **Button**, **Switch**, **Checkbox**, **Container**, **Modal**, **InputField**, **Slider**, **StackPanel**.
- Benchmarks: `./Blossom --benchmark` (isolated benchmark views).

When hardening core, update demos only as needed for compile/regression — do not expand the control library as the main deliverable unless asked.

---

## Conventions for agents

- **Minimal, focused diffs**; match surrounding style and naming.
- Prefer **primitives and contracts on `VisualElement` / `View`** over new widget types in core.
- **Do not** expand a focus system (no TabIndex, focus rings, traps) unless the user overrides product direction.
- **Do not** add `ScrollIntoView` or built-in flex/grid unless explicitly requested.
- Sample/layout proofs live under `Blossom.Demo`; keep core free of “app chrome” widgets when possible.
- No secrets in the repo.
- Don’t treat `bin/` / `obj/` as source of truth.
- Nullable is enabled; respect existing patterns for `null!` / events.

---

## Commands

Agents may **build** (and run short verification) to check changes. Prefer not starting long-lived UI processes unless the user asks; the app is interactive.

```bash
# From repo root
dotnet build Blossom.sln

# One NuGet package (artifacts/Blossom.*.nupkg) with all three libraries
dotnet pack src/Blossom.Pack/Blossom.Pack.csproj -c Release
# CI: .github/workflows/nuget.yml (pack + artifact; tag v*.*.* → GitHub Release;
#     nuget.org Trusted Publishing: policy workflow nuget.yml)
# Version: Nerdbank.GitVersioning + version.json (`0.1` → 0.1.N per commit;
#     bump the first numbers only for minor/major)

# Or packaging script (Linux; see build.sh for flags)
./build.sh
```

Run (after build), from output or via project:

```bash
dotnet run --project src/Blossom.Demo/Blossom.Demo.csproj
# optional: --builder | --benchmark
```

Other apps consume the framework with `dotnet add package Blossom`, or with project references to `src/Blossom`, `src/Blossom.Reactive`, and `src/Blossom.Primitives`, then `Shell.Initialize(new MyApplication())`.

---

## Entry points by task

| Task | Start here |
|---|---|
| Element behavior / tree | `src/Blossom/Visual/VisualElement.cs` |
| Anchors / size / 3D matrix | `src/Blossom/Visual/Transform.cs`, `src/Blossom/Visual/Enums/Anchor.cs` |
| Mouse / hover / view render | `src/Blossom/Core/View.cs` |
| Hit-testing | `src/Blossom/Core/ElementsMap.cs` |
| Keyboard / mouse event maps | `src/Blossom/Core/EventMap.cs`, `src/Blossom/Shell.cs`, `src/Blossom/Visual/ElementEvents.cs` |
| Scrolling & scrollbars | `src/Blossom/Visual/ScrollContainer.cs` |
| Layout hooks | `src/Blossom/Visual/VisualElement.cs` (`LayoutChildren`), `src/Blossom.Primitives/` (`Stack`, `Grid`, `Split`) |
| Design canvas & units | `src/Blossom/Core/Design/` |
| Plugin embed & isolation | `src/Blossom/Core/Design/PluginEmbed.cs`, `src/Blossom/Core/Design/PluginRoot.cs`, `src/Blossom.Demo/Views/ComponentDesignView.cs` |
| Custom control samples | `src/Blossom.Demo/Components/` |
| GPU surfaces / app shaders | `src/Blossom/Core/Gpu.cs` |
| Styles / shaders | `src/Blossom/Visual/ElementStyle.cs`, `src/Blossom/Visual/Style/` |
| Multi-view app shell | `src/Blossom/Core/Application.cs`, `src/Blossom.Demo/TestingApp.cs` |
| Reactive signals / flow | `src/Blossom.Reactive/` (see that project's README) |
| Layout hosts, Colours, Theme | `src/Blossom.Primitives/` |

---

## When unsure

1. Read `VisualElement` and `View` in `src/Blossom`. The behavior is defined there.
2. Treat demos in `Blossom.Demo` as consumers of the API, not as the definition of core.
3. Ask the user before large product-direction changes (focus model, layout engine shape, new packages).
4. For an unattended `agy -p` run, pass `--add-dir` set to the absolute repo root, and confirm results with `git status` in this repo.
5. For public APIs and Skia 4 3D/hit-test, read `guide/` rather than expanding this file.
