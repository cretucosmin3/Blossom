# Completion Log: UI Builder Application & Crash Resolutions

This log documents the final implementation details, testing results, and the diagnostic/resolution steps taken to fix the runtime crashes, layout/highlight/name bugs, event propagation issues, and drag performance when running the UI Builder (`./Blossom --builder`).

---

## 1. Feature Set Implemented

The **UI Builder** has been successfully built and integrated into the Blossom framework. The key features delivered include:

1. **Left Sidebar View Navigation:** Allows switching between virtual designed screens/views.
2. **Figma-Like Zoomable Canvas:** Pan and zoom grid canvas with boundary limits and precise ray-plane mouse coordinates mapping.
3. **Right Properties Inspector:** Reflection-based editor for modifying component parameters marked with `[BuilderProperty]`.
4. **Spawnable Components Palette:** Triggered via the `Tab` key, enabling component selection and placement onto the canvas.
5. **New Core Components:** Added `Checkbox`, `Slider`, `Switch`, and `InputField`.

---

## 2. Crash Diagnostics and Resolutions

Several significant runtime issues and bugs were diagnosed and resolved:

### A. NullReferenceException in `VisualElement.Dispose()` (Views Switching)
* **Problem:** In `ViewsSidebar.UpdateViewsList()`, children were first detached using `parent.RemoveChild(child)` and then disposed using `child.Dispose()`. Because `child.Parent` was set to `null` before `Dispose()`, and nested subtrees did not have `_ParentView` populated during construction, `child.ParentView` evaluated to `null`. This caused `ParentView.Elements.RemoveElement(this)` to crash.
* **Resolution:** 
  * Updated `RemoveChild()` in [VisualElement.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Visual/VisualElement.cs) to cache the parent's `ParentView` and explicitly assign it to `child.ParentView` before setting `child.Parent = null!`. This ensures the detached child and its entire nested subtree retain their pointer to the `ParentView` during disposal.
  * Modified `RegisterSubtree()` to explicitly assign `element.ParentView = view;` upon tracking new subtrees.
  * Added null-safe checking (`ParentView?.Elements.RemoveElement(this)`) in `Dispose()`.

### B. ArgumentOutOfRangeException in `SortedAxis.RemoveElement()`
* **Problem:** When removing multiple elements (like updating the sidebar list), removing an element from the `Lefts`, `Rights`, `Tops`, or `Bottoms` lists shifted the indices of all subsequent elements. However, `SortedAxis.RemoveElement()` did not update the cached index values stored in `SortIndexes` for those shifted elements. This caused subsequent removals of other elements to query incorrect/out-of-range indices.
* **Resolution:**
  * Updated `RemoveElement()` in [SortedAxis.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Core/SortedAxis.cs) to decrement the cached `Left`, `Right`, `Top`, and `Bottom` index values for all subsequent elements in the lists when a preceding element is removed.

### C. Duplicate Component Name Registration Errors
* **Problem:** Multiple custom components like `Slider`, `InputField`, `Switch`, and `Checkbox` had hardcoded internal names (e.g. `SliderTrack`, `InputFieldBg`, `SwitchKnob`, `CheckboxBox`). When the Properties Panel spawned multiple property controls, they conflicted in the view's unique component name `Map`, generating errors (`[ERROR] A component with name SwitchKnob already exists`) and preventing inputs/controls from receiving mouse events.
* **Resolution:**
  * Updated [Slider.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Testing/Components/Slider.cs), [InputField.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Testing/Components/InputField.cs), [Switch.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Testing/Components/Switch.cs), and [Checkbox.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Testing/Components/Checkbox.cs) constructors to assign unique Guid-based names to themselves and append suffixes for their internal sub-components (e.g., `$"Slider_{guid}"`, `$"Slider_{guid}_Track"`), resolving registration conflicts.
  * Added Guid suffixes to header and label items dynamically created in [PropertiesPanel.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Testing/Components/PropertiesPanel.cs) (e.g., `$"Label_{meta.Prop.Name}_{guid}"`), resolving potential overlaps from elements and their transforms having properties with the same name (like `Width`).

---

## 3. UI Layout & Selection Highlighting Improvements

1. **Inspector Panel & Sidebar Positioning Fix:**
   * **Problem:** The Right Inspector Panel content was showing up on the left side of the screen. This happened because the parent elements' `Transform` instances were completely overwritten via C# object initializers in `UiBuilderView.Init()`, leaving children attached to the old constructed transforms positioned on the left.
   * **Fix:** Updated the `Transform` setter in [VisualElement.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Visual/VisualElement.cs) to automatically re-parent the transforms of all existing children to the new assigned parent transform.
2. **Marching Ants Selection Outline Positioning & Visibility:**
   * **Problem:** The selection outline did not track correctly when elements were selected, and was hidden behind elements because it was rendered at incorrect coordinates and drawn behind designed components.
   * **Fix:** Set the selection outline's `ZIndex` to `9999` to ensure it is drawn on top of all canvas objects, and updated `UpdateSelectionOutline()` in [UiBuilderView.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Testing/Views/UiBuilderView.cs) to calculate coordinates relative to the canvas origin (`selectedElement.Transform.X - _canvas.Transform.X`).
3. **Child Coordinate Settings at Runtime:**
   * **Problem:** The handle of the trackbar (Slider), the knob of the Switch, and the caret cursor of the InputField were drawn all over the place (often off-screen or on the far left). This was because the `Transform.X` and `Transform.Y` setters in the Blossom framework interpret values as *global coordinates* (subtracting the parent's global position to compute the internal local coordinates). Thus, assigning a local value directly (like `knob.Transform.X = 23;`) incorrectly treated `23` as a global screen coordinate.
   * **Fix:** Modified [Slider.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Testing/Components/Slider.cs), [Switch.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Testing/Components/Switch.cs), and [InputField.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Testing/Components/InputField.cs) to assign runtime coordinate changes relative to the parent element's global position (`Transform.X + offset`).

---

## 4. Input & Drag-and-Drop Enhancements

1. **Artboard Parent Level Select/Drag targeting:**
   * **Problem:** Clicking on sub-components (such as a slider handle or a label on a switch) selected and dragged only that specific leaf element, breaking the component integrity during layout edits.
   * **Fix:** Modified [UiBuilderView.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Testing/Views/UiBuilderView.cs) click handler to walk up the parent chain until the parent is the `activeArtboard`, ensuring the top-level custom component itself is always targeted for selection and dragging.
2. **Drag Performance Optimization (Inspector Refresh Deferral):**
   * **Problem:** Dragging elements was very slow and lagged, taking 1-2 seconds to reflect layout moves. This was because `_inspector.InspectElement()` was triggered on every single pixel of mouse move, forcing expensive reflection and complete recreation/relayout of the properties panel UI.
   * **Fix:** Removed the properties panel rebuild call from the mouse move drag loop in [UiBuilderView.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Testing/Views/UiBuilderView.cs). Rebuilding is now deferred and called exactly once inside `OnMouseUp` when the drag operation finishes, resulting in buttery-smooth, 60+ FPS real-time element dragging.
3. **Slider Drag Coordinate Mapping (`PointToClient`):**
   * **Problem:** The trackbar dots (handles) were all over the place when dragging them under canvas zoom levels other than `1.0x`. This was due to `Slider` calculating local values using screen-space coordinate offsets without accounting for scaling.
   * **Fix:** Updated `UpdateValueFromMouse()` in [Slider.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Testing/Components/Slider.cs) to map coordinates using the built-in `PointToClient()` method, which performs matrix-based ray-plane mapping of the global mouse coordinate to the slider's local space.
4. **Standard Mouse Event Bubbling:**
   * **Problem:** Clicking on custom components did not trigger their events if the click landed on sub-components (such as labels or knobs), as the framework dispatched events exclusively to the leaf node returned by hit-testing.
   * **Fix:** Updated event handlers (`OnMouseDown`, `OnMouseUp`, `OnMouseMove`) in [View.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Core/View.cs) to propagate events up the parent element hierarchy recursively. Also updated focus logic to walk up and target the closest `Focusable` parent.
5. **Hierarchical Hover State Transitioning:**
   * **Problem:** Hover states would flicker or turn off when moving the mouse from the component background onto a child sub-component (e.g. from the slider track onto the handle).
   * **Fix:** Implemented parent-chain comparisons in [View.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Core/View.cs) to only trigger `MouseLeave`/`MouseEnter` for elements entering or leaving the active hover ancestor stack.

---

## 5. Verification & Benchmark Performance

* **Build Validation:** Compilation via `./build.sh` is 100% clean.
* **UI Builder Launch:** Tested via `./Blossom --builder` and runs successfully with no exceptions/crashes, properly aligned sidebars, and functional inputs.
* **Benchmark Execution:** Validated via `./Blossom --benchmark`. The performance metrics exceed baseline limits:
  * **Static Grid View:** `379.1 FPS` (Target: $\ge 160$)
  * **Dynamic Mutation View:** `90.1 FPS` (Target: $\ge 80$)
