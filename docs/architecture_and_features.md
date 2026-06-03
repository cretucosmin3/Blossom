# Blossom UI Framework Architecture & Feature Summary

This document provides a comprehensive overview of the Blossom UI framework's core rendering pipeline, layout solver, advanced GPU features, codebase directory structure, and performance benchmarks.

---

## 1. Core Architecture & Rendering Pipeline

Blossom utilizes **Silk.NET** (GLFW/OpenGL binding) for window and input lifecycle management and **SkiaSharp** for layout-driven hardware-accelerated 2D/3D graphics rendering.

### A. Retained-Mode Pipeline & Dirty Rect Tracking
Rather than redrawing the entire screen every frame, Blossom is built as a retained-mode engine:
1. **Damage mapping (Dirty Rects):** Modifying visual properties invalidates the element's bounding box and registers it with the view's dirty list.
2. **Path union-merging:** The layout engine batches dirty boundaries. If the number of invalidation regions is small, they are union-merged to minimize complex clipping.
3. **Canvas Scissor clipping:** Skia clips rendering to the dirty region before iterating the scene graph.
4. **Targeted Redraws:** Elements are sorted back-to-front. Only elements that intersect the dirty region are drawn. This automatically fixes overlap clipping issues (where bottom elements might otherwise paint over top elements).

### B. Ordering (Painter's Algorithm)
Before drawing, components are sorted to ensure correct depth ordering:
* **Priority 1:** Z-Index (ascending).
* **Priority 2:** Hierarchical tree depth (parents must always render before children).
* **Priority 3:** Temporal registration order (the element added to the view last draws last/on top).

### C. Layout & Anchors System
Every visual element holds a `Transform` defining its 2D local space. The layout solver resolves:
* **Anchor Flags:** Edge alignments (`Top`, `Bottom`, `Left`, `Right`, `Horizontal`, `Vertical`, `None`).
* **Stretch vs Proportional scaling:** Elements scale relative to parent bounds or maintain proportional coordinates.
* **Fixed Dimensions:** When `FixedWidth` or `FixedHeight` is enabled with `Anchor.None`, components remain centered inside their proportional bounds without stretching.

### D. Reference Resolution & Viewport Mapping
Views support virtual reference resolutions (e.g., 1280x800). 
* **Matrix Projection:** An aspect-ratio preserving scale matrix fits the virtual bounds within the physical window (letterboxing/pillarboxing).
* **Cramer's Rule Input Mapping:** Mouse coordinates are reverse-mapped back into local 3D-transformed design coordinates using the inverse global transform matrix solved via Cramer's Rule:
  $$localX = \frac{C_1 B_2 - B_1 C_2}{A_1 B_2 - B_1 A_2}, \quad localY = \frac{A_1 C_2 - C_1 A_2}{A_1 B_2 - B_1 A_2}$$

---

## 2. Advanced GPU & Shader Features

*   **GPU SKSL Shaders:** Fragment shaders (Liquid Plasma, Synthwave Grid, CRT Scanlines) written in Skia Shading Language (SKSL). Dynamic uniforms (`u_time`, `u_resolution`, `u_color`, `u_hover`) are bound to drive real-time interactive transitions.
*   **True Backdrop Blur:** Snapshots the backbuffer texture under the element, clips it to the rounded bounds, and redraws it with blur filters, achieving high-fidelity glassmorphism.
*   **Marching Ants & Jitter Borders:** Custom stroke effects that animate discrete geometric jitter or dashed border offsets based on elapsed time.
*   **Halftone Transition Shader:** A GPU-backed grid reveal/hide shader that uses a custom halftone dots pattern to fade components out or transition them in.
*   **3D Transform Showcase:** Rotations on all 3 axes (X, Y, Z), scale, translation, and perspective. A 3D Ray-Plane Intersection algorithm maps screen pointer raycasts onto the local plane ($z=0$) to keep mouse states fully interactive.

---

## 3. Project Directory Map

```
Blossom/
├── docs/                     # Architectural documents & design specs
├── explore/                  # In-depth math breakdowns (e.g. 3D projection formulas)
├── progress/                 # Chronological log of optimizations and feature updates
├── glfw/                     # Custom GLFW library dependencies for Linux & Windows
├── Obsidian/                 # Obsidian workspace vaults containing the roadmap
└── src/
    ├── Browser.cs            # Main engine runloop, input listeners, and frame timing
    ├── Program.cs            # Native process entry point & shader validations
    ├── Core/                 # Layout solvers, commands ledger, and view managers
    ├── Visual/               # VisualElement definitions, styles, and transform math
    └── Testing/
        ├── Components/       # Reusable components (Buttons, charts, cards)
        └── Views/            # Active dashboard and benchmark view configurations
```

---

## 4. Benchmark Performance Results

System performance verified on this hardware using `./Blossom --benchmark`:

*   **Static Grid Benchmark:** **451.1 FPS** (Target baseline: $\ge 160$ FPS)
*   **Dynamic Mutation Benchmark:** **99.4 FPS** (Target baseline: $\ge 80$ FPS)

No heap allocations occur in standard update loops, layout solves, or rendering ticks, minimizing garbage collection overhead.
