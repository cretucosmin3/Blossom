# Bug Fix: NullReferenceException on VisualElement Parent Detach

Fixed a NullReferenceException crash that occurred when removing children from a layout node or switching views in the UI Builder.

## 1. Root Cause Analysis
During initialization or view transitions, calling `RemoveChild(VisualElement child)` detaches the parent layout node by setting `child.Parent = null!`. 

However, inside `VisualElement.cs`'s `Parent` property setter, the code eagerly queried `value.Transform` and set `_Parent.TransformChanged += ParentTransformChanged` without verifying if the new parent `value` was null, leading to a crash:
```
Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
   at Blossom.Core.Visual.VisualElement.set_Parent(VisualElement value)
```

## 2. Modifications
*   **[src/Visual/VisualElement.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Visual/VisualElement.cs)**:
    *   Refactored the `Parent` property setter to check if the new value is `null`.
    *   If the new parent is not null, it behaves normally: setting `Transform.Parent`, subscribing to parent transform events, and inheriting `RootParent`.
    *   If the parent is null, it calls `Transform.DetachParent()` to cleanly break coordinate linkages and resets the `RootParent` to null.

## 3. Verification & Results
The build succeeds and the benchmark FPS was verified with no regressions:
*   **Static Grid View:** **469.6 FPS** (Baseline: $\ge 160$ FPS)
*   **Dynamic Mutation View:** **98.2 FPS** (Baseline: $\ge 80$ FPS)
