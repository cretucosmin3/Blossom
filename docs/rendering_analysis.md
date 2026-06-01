# Rendering Pipeline Analysis and Recommendations

## 1. Current Implementation Analysis

The current rendering engine operates largely in an immediate-mode style with some retained-mode characteristics (component tree).

-   **Rendering Loop**: The `View.Render()` method iterates through all elements in `Elements.Items`.
-   **Caching**: `VisualElement` attempts to cache its content to an `SKBitmap` (`CachedRender`) when it has many cached nested elements. This is an element-level optimization, not a pipeline-level one.
-   **Dirty State**: `VisualElement` has an `IsDirty` flag, but `View.Render()` currently iterates all elements. If an element is dirty, it re-renders itself, but there is no mechanism to handle the *impact* of that dirty area on *other* overlapping elements.
-   **Clipping**: Clipping is primarily handled via `ApplyClipping` which clips to the parent's bounds. This handles hierarchy but not arbitrary overlapping of sibling components.
-   **Ordering**: Rendering order is determined by the order in `Elements.Items`. There is no explicit Z-Index sorting or "Tree Depth" prioritization logic visible in the main render loop.

**Major Issues identified:**
1.  **Overlap Glitches**: When a top-level component changes (is dirty), the system might redraw it. However, if a *bottom* component changes, it might draw *over* the top component if the order isn't strictly enforced every frame. Conversely, if a top component moves, the "hole" left behind on the bottom component isn't automatically repaired because the bottom component might not know it needs to redraw that specific region.
2.  **Inefficient Redraws**: `Renderer.Canvas.Clear` is called every frame in `Browser.Render`, clearing the entire screen. This defeats the purpose of "Dirty Rects" where you only want to update changed pixels.

## 2. Gap Analysis: Current vs. "Bulletproof" Requirements

| Feature | Current Implementation | "Bulletproof" Requirement |
| :--- | :--- | :--- |
| **Geometry** | `Transform.Computed` exists. | **Match.** Continue using World Bounds. |
| **Ordering** | List order (Implicit). | **Z-Index + Tree Depth + Temporal.** Needs a dedicated sorting step before every draw. |
| **Dirty Tracking** | `IsDirty` flag on Element. | **Damage Map.** Need to collect `World Bounds` of all dirty elements into a list. |
| **Dirty Union** | None. | **Region Union.** Merge all dirty rects into a single `SKRegion` or complex path. |
| **Clipping** | Parent-bound clipping. | **Dirty Region Clipping.** Clip the *Canvas* to the Dirty Region before drawing *any* component. |
| **Overlap Fix** | None. | **Stack Redraw.** For the dirty region, find *all* intersecting components (even non-dirty ones) and draw them Bottom-to-Top. |

## 3. Recommendations (How it should be done)

To implement the requested pipeline, we need to restructure `Browser.cs` loop and `View.cs` rendering logic.

### A. The Optimization Controller (New Concept)
Introduce a `RenderPipeline` or `SceneGraph` class that manages the frame lifecycle.

### B. The New Render Loop
1.  **Input/Update Phase**: Components handle events. If a visual change occurs (color, position, text), the component calls `Invalidate()`.
    -   `Invalidate()` implementation: Adds the component's **current** `WorldBounds` to a global `DirtyList`.
    -   *Crucial*: If a component *moves*, it must invalidate its **old** position AND its **new** position.

2.  **Pre-Render Sort**:
    -   Flatten the component tree or maintain a flat list.
    -   Sort this list by: `Z-Index` (Ascending) -> `Tree Depth` (Parents first) -> `Creation Time`.

3.  **Calculate Dirty Region**:
    -   Take all rects in `DirtyList`.
    -   Union them into a single `SKPath` or `SKRegion` (Let's call it `DamageRegion`).
    -   *Optimization*: If `DamageRegion` covers a significant % of screen, just redraw everything.

4.  **Render Phase**:
    -   **Do NOT** clear the whole screen (remove `Canvas.Clear` from the loop unless we are doing a full redraw).
    -   Set `Canvas.ClipPath(DamageRegion)`.
    -   Iterate through the **Sorted Component List**:
        -   Perform Intersection Test: `if (Component.WorldBounds.Intersects(DamageRegion))`
        -   If yes, Draw the component.
        -   If no, Skip.
    -   *Result*: This automatically solves the overlap issue. Since we clipped to the `DamageRegion` and we are drawing the *entire stack* of intersecting components from Bottom-to-Top, the background gets filled in, and the top layers get drawn over it correctly.

### C. Coordinate Systems
-   Keep using `Transform` stack.
-   Ensure `Canvas.Translate(x, y)` is called before element draw and `Canvas.Translate(-x, -y)` (or `Restore()`) after.

## 4. Implementation Steps

1.  **Modify `VisualElement`**:
    -   Update `IsDirty` to register itself with the `View`'s dirty list instead of just setting a bool.
    -   Ensure `IsDirty` captures the bounds at the moment of invalidation.

2.  **Modify `View`**:
    -   Add `List<SKRect> DirtyRects`.
    -   Add `List<VisualElement> RenderQueue`.
    -   Implement `SortRenderQueue()`.
    -   Rewrite `Render()` to follow the logic in Section 3B.

3.  **Modify `Browser.cs`**:
    -   Stop clearing the screen every frame.
    -   Let `View.Render()` handle the clearing (only clearing the dirty region if needed, typically by drawing the root background).
