# Input

## Keyboard

```csharp
public sealed class KeyEvent
{
    public Key Key { get; init; }
    public int ScanCode { get; init; }
    public bool IsRepeat { get; init; }
    public bool Control { get; init; }
    public bool Alt { get; init; }
    public bool Shift { get; init; }
    public bool Super { get; init; }
    public bool Handled { get; set; }
}

public sealed class TextEvent
{
    public string Text { get; init; } // UTF-16 chunk; surrogate pairs allowed
    public bool Handled { get; set; }
}
```

Subscribe on `ElementEvents` / `EventMap`: `OnKeyDown`, `OnKeyUp`, `OnTextInput`. There is no `OnKeyType(char)`.

**Dispatch:** focused `VisualElement` first, then parents until `Handled`, then the view, then the application. Registered hotkeys run only if the event is still unhandled. A terminal that marks Ctrl+C handled keeps the byte; an app-level Ctrl+W still runs when nothing ate it.

**Repeat:** Silk's GLFW backend still drops `InputAction.Repeat`. `Shell` fills `KeyEvent.IsRepeat` from the GLFW key callback.

**Text:** high surrogates are buffered and emitted as one `TextEvent`. Consumers can `Text.EnumerateRunes()`.

**Modifiers:** `EventMap.IsKeyDown`, `IsControlDown`, `IsAltDown`, `IsShiftDown`, `IsSuperDown`. Do not reflect into Silk `IInputContext`.

**F12:** see [Host](host.md) — reserved only when `EnableStatsOverlay` is true.

Give a primitive keyboard by setting `ReceivesKeyboard = true` and handling the events. Call `GetFocus()` / `View.SetActiveKeyboardElement` to become the target.

## Tab without TabIndex

`View.KeyboardTargets()` lists `Visible && EffectiveInteractive && ReceivesKeyboard` in tree order (overlay layers after app roots, bottom to top).

Default unhandled Tab / Shift+Tab moves `ActiveKeyboardElement` along that list. Handle the key and set `Handled` to supply a custom order or to ignore Tab. Core does not ship focus rings or focus traps; a widget that wants a ring paints one.

## Mouse

`MouseEventArgs` has `Global`, `Relative`, `Button`, and `Handled`. Pointer capture: `View.SetPointerCapture` / `ReleasePointerCapture`.

`Relative` comes from `VisualElement.PointToClient`, which unprojects onto the element's local z=0 plane (required for rotated/perspective nodes). Do not subtract `AbsoluteX`/`AbsoluteY` by hand on a 3D element.

Click policy and hover use `ElementTree.Hits` (see below). `IsClickthrough` walks to the nearest ancestor that can receive pointer events (icon/label on a button).

## Hit-testing

1. Painter order (front to back), overlays first.
2. Conservative `RenderBounds` AABB (corners mapped through the 4×4, including perspective divide).
3. `Transform.TryUnproject` onto local z=0, then `HitTestLocal` (rect + corner radii; override for custom shapes).
4. Clipping ancestors: the same screen point is unprojected into each clipping ancestor.

**Do not** invert the 4×4 and `MapPoint(screenX, screenY, z: 0)`. After `RotationX` / `RotationY`, the painted pixel is not at z=0 in that space, so the hit region slides off the drawing. `TryUnproject` solves the row-vector homography for `(lx, ly, 0, 1) * M`.

Override `HitTestLocal` for circles, paths, etc.

## Overlays (modals, menus, toasts)

Overlays are siblings of the view root: painted last, hit-tested first, not clipped by the opener's overflow.

```csharp
public sealed class OverlayOptions
{
    public bool BlockHitsUnderneath { get; set; } = true;  // modal
    public bool CloseOnPointerOutside { get; set; }        // menu
    public VisualElement? RestoreKeyboardTo { get; set; }
}

IDisposable lease = view.PushOverlay(dialog, new OverlayOptions
{
    BlockHitsUnderneath = true,
    CloseOnPointerOutside = false,
});
// dispose or view.PopOverlay(dialog) — restores keyboard
```

A menu typically uses `BlockHitsUnderneath = false` and `CloseOnPointerOutside = true`. The demo `Modal` uses this stack.
