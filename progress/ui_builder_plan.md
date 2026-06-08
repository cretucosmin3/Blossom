# Implementation Plan: UI Builder Application

This plan details the architecture, component design, and incremental roadmap for building a fully interactive **UI Builder** application inside the Blossom framework.

---

## 1. Architectural Components

### A. Viewport Canvas (Figma-Like Zoom & Pan)
*   **Target Component:** `BuilderCanvas : VisualElement`
*   **Transform Matrix:** Will host the active design view. The canvas will apply a translation (pan) and scaling (zoom) matrix to its local coordinate space during rendering:
    $$M = T(OffsetX, OffsetY) \cdot S(Zoom, Zoom)$$
*   **Limits:** Zoom range limited between `0.15x` and `4.0x`.
*   **Controls:**
    *   `MouseScroll` $\to$ Zoom in/out at the current mouse cursor location.
    *   `Middle Mouse Drag` (or `Space + Left Mouse Drag`) $\to$ Pan the canvas (`PanOffset`).
*   **Hit-Testing Integration:** By scaling and translating via `Transform.ScaleX`, `Transform.ScaleY`, `Transform.X`, and `Transform.Y`, Blossom’s native Cramer's Rule ray-plane intersection will automatically map screen-space clicks to the correct transformed coordinates.

### B. Dynamic Component Property Inspection (Reflection & Attributes)
*   **Attribute Definition:** Create `[BuilderProperty(string label, string category)]` in `src/Core/Attributes.cs`.
*   **Inspector Panel:** `src/UiBuilder/Components/PropertiesPanel.cs`
*   **Dynamic Generation:** When an element is selected on the canvas, the Properties Panel uses reflection to read properties decorated with `[BuilderProperty]`:
    *   `float`/`int` $\to$ Slider or Numeric field controls.
    *   `bool` $\to$ Checkbox/Switch toggles.
    *   `SKColor` $\to$ Hex/RGB sliders or palette dropdowns.
    *   `string` $\to$ Text input fields.
*   **Selection Feedback:** Selected elements will render a dynamic marching-ants neon border to signify focus.

### C. Available Components Palette (Tab Modal Overlay)
*   **Target Control:** `ComponentPaletteModal : VisualElement`
*   **Trigger:** Pressing the `Tab` key toggles the modal's visibility.
*   **Grid Content:** Displays cards representing all spawnable widgets:
    *   `VisualElement` (Standard Box Container)
    *   `Button` & `NeonButton`
    *   `Label` (Text field)
    *   `Checkbox` (New Control)
    *   `Slider` (New Control)
    *   `ProgressBar`
*   **Placement Logic (Click-to-Place):** Selecting a widget closes the modal and activates a "Placement Mode." A ghost outline of the element follows the mouse. Left-clicking the canvas places/spawns the element at that local coordinate.

### D. Extra Core Components (To be Developed)
*   **Checkbox (`Checkbox : VisualElement`):** Toggle box displaying checkmarks with an `OnCheckedChanged` callback.
*   **Slider (`Slider : VisualElement`):** Grab-handle track that maps mouse drag offsets to a value range with an `OnValueChanged` callback.
*   **Switch (`Switch : VisualElement`):** Sliding toggle pill-button.
*   **TextField/InputField (`InputField : VisualElement`):** Basic text typing handler for editing string properties in-builder.

---

## 2. Program Launch Integration

Add a command-line parser switch `--builder` inside `Program.cs` and `Browser.cs` to spin up `UiBuilderApplication` instead of `TestingApplication`:

```bash
# How to run the builder:
./Blossom --builder
```

---

## 3. Step-by-Step Task List

### Phase 1: Core Framework Support & Extra Components
- [x] Create `[BuilderProperty]` attribute inside a new file `src/Core/Attributes.cs`.
- [x] Decorate existing base properties (`X`, `Y`, `Width`, `Height`, `Text`, `BackColor`, `Border.Roundness`, etc.) with `[BuilderProperty]`.
- [x] Implement the `Checkbox` component with hover scaling and toggle state logic.
- [x] Implement the `Slider` component with coordinate-to-value dragging logic.
- [x] Implement the `Switch` component with smooth sliding transitions.
- [x] Implement the `InputField` component to support basic editing/typing.

### Phase 2: Canvas Viewport & Figma-like Scaling
- [x] Implement the `BuilderCanvas` container component.
- [x] Register scroll and mouse move hooks to manage `Zoom` and `PanOffset`.
- [x] Verify that children inside `BuilderCanvas` receive mouse hover and click coordinates accurately at all zoom scales.

### Phase 3: Left Navigation & Right Inspector
- [x] Create `UiBuilderView : View` as the main editing workspace layout.
- [x] Implement the Left Sidebar displaying a list of "Views under design".
- [x] Implement the Right Properties Inspector rendering controls dynamically based on the selected element's reflection attributes.
- [x] Build the selection outline using the dynamic Jitter/Marching Ants border styles.

### Phase 4: Component Palette Tab Modal
- [x] Create the Overlay Modal component.
- [x] Hook the global `Tab` key-up trigger to toggle modal state.
- [x] Implement the component placement/spawner cursor hook.

### Phase 5: Verification & Log Updates
- [x] Verify compilation using `./build.sh`.
- [x] Validate benchmark FPS has no regressions.
- [x] Update progress log and present finished UI Builder app.
