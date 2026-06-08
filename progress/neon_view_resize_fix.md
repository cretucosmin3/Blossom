# Bug Fix: View Sizing and Alignment Mismatch After Window Resize

Fixed an issue where elements in views instantiated after a window resize (like the Neon Showcase View) appeared smaller, incorrectly placed, or offset.

## 1. Root Cause Analysis
During layout initialization of views (e.g. inside `Init()`), coordinates are calculated based on the view's layout resolution. Under the hood:
- The view's dimensions during `Init()` correctly use the active viewport size `Browser.RenderRect.Width` and `Browser.RenderRect.Height` (which is updated immediately when the window is resized).
- However, the design coordinate constants in the view's layout code (like `ctrlBtnW = 180f`, `containerW = 800f`, and positioning offsets) are designed specifically for the reference resolution of 1280x800.
- Furthermore, during construction of a `Transform` (which occurs before the parent view link is established), the parent resolution fallback defaulted to the logical window coordinates via `Browser.window.Size.X` and `Browser.window.Size.Y`.

If the window was resized before navigating to the view (e.g., to 1600x900):
1. The view's `Init()` runs with the resized dimensions (e.g., `Width = 1600`).
2. The elements' anchors and relative positions (`RelativeLeft`, `RelativeRight`, etc.) are calculated relative to `1600` instead of the view's reference base of `1280`.
3. However, because the absolute coordinates passed into the elements are designed for `1280` (e.g. `Width = 180`), the layout evaluation treats these elements as already scaled for a `1600` screen width.
4. Consequently, the elements are not scaled up to fit the larger resolution (keeping their base sizes like 800 and 180 instead of scaling to 1000 and 225), resulting in them looking odd and smaller.

## 2. Modifications
*   **[src/Visual/Transform.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Visual/Transform.cs)**:
    *   Replaced fallback references to `Browser.window.Size.X` / `Browser.window.Size.Y` with `(float)Browser.RenderRect.Width` / `(float)Browser.RenderRect.Height` to maintain consistency with active render coordinates.
*   **[src/Core/Application.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Core/Application.cs)**:
    *   Wrapped the view's `Init()` call in `SetActiveView` with a temporary override of `Browser.RenderRect` to the view's `ReferenceWidth` and `ReferenceHeight`.
    *   This forces all elements created in `Init()` to calculate their relative layout anchors and positions with respect to the view's designed reference size (e.g. 1280x800).
    *   Once `Init()` finishes, the actual window size is restored, and `ForceLayoutEvaluation()` is called to scale the layout dynamically to the actual window size.
*   **[src/Browser.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Browser.cs)**:
    *   Wrapped the active view's `Init()` call during startup loading with the same temporary `Browser.RenderRect` reference resolution override.

## 3. Verification & Results
The build succeeds and the benchmark FPS was verified with no regressions:
*   **Static Grid View:** **316.1 FPS** (Baseline: $\ge 160$ FPS)
*   **Dynamic Mutation View:** **91.4 FPS** (Baseline: $\ge 80$ FPS)
