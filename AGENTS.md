# AGENTS.md — Blossom

General guidance for anyone (human or AI) working in this repository.  
**Design plans and feature roadmaps live under `plans/`** — open those when implementing a specific initiative; do not assume this file lists every active project.

Unattended implementer workflow (Antigravity / `agy`): see **`AGY_SUBAGENT.md`** and **`plans/agy-runs/`**.

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

See also: `findings/element-view-gap-analysis.md`, `plans/visual-element-platform-must-have.md`.

---

## Repository layout

```text
Blossom/
├── Program.cs              # Entry
├── Blossom.csproj          # Single executable project (net10.0)
├── src/
│   ├── Browser.cs          # Window loop, input wiring, frame timing
│   ├── Core/               # Application, View, ElementTree, EventMap, Renderer, ledger
│   ├── Visual/             # VisualElement, Transform, Style, ScrollContainer, shaders
│   ├── Testing/            # Demo app, showcase views, sample components
│   ├── Utils/              # Fonts, imaging helpers
│   └── External/           # Vendored quadtree helpers
├── assets/                 # Fonts, images, icons (copied to output on build)
├── glfw/                   # Native GLFW libs for packaging
├── docs/                   # Architecture / layout / rendering notes
├── plans/                  # Implementation plans (+ agy-runs/)
├── findings/               # Analysis notes (gaps, research)
├── progress/               # Chronological work logs (may be historical)
├── explore/                # Deep dives (e.g. 3D math)
└── AGY_SUBAGENT.md         # How to run agy as implementer
```

Ignore `bin/`, `obj/`, and large build outputs under `dist/` if present.

---

## Stack

| Piece | Tech |
|---|---|
| Language | C# |
| TFM | **.NET 10** (`net10.0`) |
| Window / input | Silk.NET 2.15 (GLFW) |
| Graphics | SkiaSharp 2.88 (+ SVG, ImageSharp) |
| Solution | Single `Blossom.csproj` / `Blossom.sln` |

---

## Core architecture (where to look)

### Runtime hierarchy

```text
Browser (static host)
  └── Application
        └── View (one active)
              └── VisualElement tree
                    ├── Transform (layout anchors + 3D matrix)
                    ├── ElementStyle (fill, border, shadow, text, effects)
                    └── ElementEvents / EventMap (input)
```

| Concern | Primary files |
|---|---|
| Window + input entry | `src/Browser.cs` |
| App / multi-view | `src/Core/Application.cs` |
| View render + mouse routing | `src/Core/View.cs` |
| Hit-test tree | `src/Core/ElementsMap.cs` |
| Events | `src/Core/EventMap.cs`, `src/Visual/ElementEvents.cs` |
| Element node | `src/Visual/VisualElement.cs` |
| Layout / anchors / matrices | `src/Visual/Transform.cs`, `src/Visual/Enums/Anchor.cs` |
| Style | `src/Visual/ElementStyle.cs`, `src/Visual/Style/*` |
| Scroll | `src/Visual/ScrollContainer.cs` |
| Draw commands | `src/Core/CommandLedger.cs`, `VisualElement.RecordDrawCommands` |
| Shaders / effects | `src/Visual/Style/SKSLShaders.cs` |
| Demo shell | `src/Testing/TestingApp.cs`, `src/Testing/Views/*` |
| Sample widgets | `src/Testing/Components/*` (not formal public controls) |

### Layout today

- Anchors: `Left` / `Right` / `Top` / `Bottom` (stretch vs fixed vs proportional)
- Optional **reference resolution** on `View` (virtual design size + letterbox scale)
- `ScrollContainer` applies scroll offsets into child computed positions
- Custom layout engines should plug in via planned hooks (`LayoutChildren`, layout vs paint dirty) — see platform plan; do **not** add Flexbox as a core system unless the plan explicitly says so

### Rendering

- Retained mode: dirty rects, scissor clip, painter’s order (ZIndex, tree depth, registration order)
- Advanced: SKSL backgrounds, backdrop blur, 3D transforms with inverse hit-test

Background reading: `docs/architecture_and_features.md`, `docs/layout_system.md`, `docs/bulletproof rendering.md`.

---

## Testing / demos

`src/Testing` hosts the primary manual test harness and sample test components (not the framework API boundary):

- Single active application view: **Todo Kanban Board** (`KanbanView`), exercising pointer capture, whole-card drag-and-drop, scrollable columns, modals, and input fields.
- Components under `Testing/Components`: **Button**, **Switch**, **Checkbox**, **Container**, **Modal**, **InputField**, **StackPanel**, and **TodoCard**.
- Benchmarks: `./Blossom --benchmark` (isolated benchmark views).

When hardening core, update demos only as needed for compile/regression — do not expand the control library as the main deliverable unless asked.

---

## Plans and findings

| Path | Role |
|---|---|
| `plans/` | Implementation plans (e.g. VisualElement platform must-haves) |
| `plans/agy-runs/` | Short AGY result notes + prompt/workflow templates |
| `findings/` | Gap analyses and research write-ups |
| `progress/` | Historical progress notes |
| `docs/` | Architecture and design documentation |

If a task names a plan, **read and follow that plan** (and any progress checkboxes). Do not invent large redesigns when a plan already scopes the work.

---

## Conventions for agents

- **Minimal, focused diffs**; match surrounding style and naming.
- Prefer **primitives and contracts on `VisualElement` / `View`** over new widget types in core.
- **Do not** expand a focus system (no TabIndex, focus rings, traps) unless the user overrides product direction.
- **Do not** add `ScrollIntoView` or built-in flex/grid unless explicitly requested.
- Sample/layout proofs can live under `Testing` or docs; keep core free of “app chrome” widgets when possible.
- No secrets in the repo.
- Don’t treat `bin/` / `obj/` as source of truth.
- Nullable is enabled; respect existing patterns for `null!` / events.

---

## Commands

Agents may **build** (and run short verification) to check changes. Prefer not starting long-lived UI processes unless the user asks; the app is interactive.

```bash
# From repo root
dotnet build Blossom.csproj

# Or packaging script (Linux; see build.sh for flags)
./build.sh
```

Run (after build), from output or via project:

```bash
dotnet run --project Blossom.csproj
# optional: --builder | --benchmark
```

---

## Entry points by task

| Task | Start here |
|---|---|
| Element behavior / tree | `docs/lifecycle.md`, `src/Visual/VisualElement.cs` |
| Anchors / size / 3D matrix | `src/Visual/Transform.cs` |
| Mouse / hover / view render | `docs/input_model.md`, `src/Core/View.cs` |
| Hit-testing | `docs/input_model.md`, `src/Core/ElementsMap.cs` |
| Keyboard / mouse event maps | `docs/input_model.md`, `src/Core/EventMap.cs`, `src/Browser.cs` |
| Scrolling & scrollbars | `docs/scroll_container.md`, `src/Visual/ScrollContainer.cs` |
| Layout contracts & panels | `docs/layout_contract.md`, `docs/custom_layout.md` |
| Custom control recipes & samples | `docs/custom_controls.md`, `docs/platform_samples.md` |
| Styles / shaders | `src/Visual/ElementStyle.cs`, `src/Visual/Style/` |
| Multi-view app shell | `src/Core/Application.cs`, `src/Testing/TestingApp.cs` |
| Platform roadmap | `plans/visual-element-platform-must-have.md` |
| Known gaps | `findings/element-view-gap-analysis.md` |
| AGY implementer runs | `AGY_SUBAGENT.md`, `plans/agy-runs/` |

---

## When unsure

1. Prefer reading `VisualElement` + `View` + the relevant plan under `plans/`.  
2. Treat demos in `Testing` as consumers of the API, not as the definition of core.  
3. Ask the user before large product-direction changes (focus model, layout engine shape, new packages).  
4. For unattended implementation, use the AGY workflow with `--add-dir` on the real repo path.
