# Input

## Keyboard

```csharp
public sealed class KeyEvent
{
    public UiKey Key { get; init; }
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

`UiKey` is Blossom-owned. Names and numeric values match Silk.NET.Input.Key / GLFW (`ShiftLeft`, `ControlLeft`, `Enter = 257`, arrows `262–265`). Desktop casts with `(UiKey)(int)silkKey`. MonoGame `Keys` uses different numbers (`Enter = 13`); that mapper is not in core.

Subscribe on `ElementEvents` / `EventMap`: `OnKeyDown`, `OnKeyUp`, `OnTextInput`. There is no `OnKeyType(char)`.

**Dispatch:** focused `VisualElement` first, then parents until `Handled`, then the view, then the application. Registered hotkeys run only if the event is still unhandled. A terminal that marks Ctrl+C handled keeps the byte; an app-level Ctrl+W still runs when nothing ate it.

**Repeat:** Silk's GLFW backend still drops `InputAction.Repeat`. `Shell` fills `KeyEvent.IsRepeat` from the GLFW key callback, then calls `session.Input.KeyDown`.

**Text:** high surrogates are buffered in `Shell` and emitted as one `TextEvent`. Consumers can `Text.EnumerateRunes()`.

**Modifiers:** `EventMap.IsKeyDown`, `IsControlDown`, `IsAltDown`, `IsShiftDown`, `IsSuperDown`. Do not reflect into Silk `IInputContext`.

**F12:** see [Host](host.md) — reserved only when `EnableStatsOverlay` is true (desktop Shell intercepts before `UiInput`).

Give a primitive keyboard by setting `ReceivesKeyboard = true` and handling the events. Call `GetFocus()` / `View.SetActiveKeyboardElement` to become the target.

## Host injection (`UiInput`)

Core never reads Silk, GLFW, or engine devices. Every host calls `UiSession.Input`:

```csharp
session.Input.PointerMove(x, y);
session.Input.PointerDown(x, y, button);   // 0=left, 1=right, 2=middle
session.Input.PointerUp(x, y, button);
session.Input.PointerLeave();              // hover only; keep capture if button 0/1 down
session.Input.Scroll(dx, dy);
session.Input.KeyDown(UiKey.Enter, scanCode: 0, isRepeat: false);
session.Input.KeyUp(UiKey.Enter);
session.Input.Text(utf16);                 // UTF-16 chunk
```

Coordinates are **logical viewport** (origin top-left of the Blossom surface), same space as desktop mouse after `pos / ScaleFactor`.

Call `PointerMove` when the pointer moves, not on a timer. Each call runs a hit test and hover pass, so a game loop that polls the mouse should compare against last frame's position first.

`Shell.Initialize` keeps mapping Silk `IInputContext` onto these methods. Games inject from their own `Update`. Surrogate pairing stays in Shell; game `Text` comes from the engine IME if you subscribe.

## Tab without TabIndex

`View.KeyboardTargets()` lists `Visible && EffectiveInteractive && ReceivesKeyboard` in tree order (overlay layers after app roots, bottom to top).

Default unhandled Tab / Shift+Tab moves `ActiveKeyboardElement` along that list. Handle the key and set `Handled` to supply a custom order or to ignore Tab. Core does not ship focus rings or focus traps; a widget that wants a ring paints one.

## Mouse

`MouseEventArgs` has `Global`, `Relative`, `Button`, and `Handled`. Pointer capture: `View.SetPointerCapture` / `ReleasePointerCapture`.

`Relative` comes from `VisualElement.PointToClient`, which unprojects onto the element's local z=0 plane (required for rotated/perspective nodes). Do not subtract `AbsoluteX`/`AbsoluteY` by hand on a 3D element.

Click policy and hover use `ElementTree.Hits` (see below). `IsClickthrough` walks to the nearest ancestor that can receive pointer events (icon/label on a button).

`VisualElement.Cursor` is `UiCursor?` (`Hand`, `IBeam`, `HResize`, …). Null inherits from ancestors; otherwise `UiCursor.Default`. The host applies it via `IUiHost.SetCursor`.

**PointerLeave vs capture.** Leaving the HUD/window:

1. Keep delivering `PointerMove` / `PointerUp` to the capture target (coords may be outside the logical viewport).
2. `PointerLeave` clears hover only; it does **not** `ReleasePointerCapture` if button 0 or 1 is down.
3. World click-through: if `!session.WantsPointer(lx, ly)` **and** `PointerCaptureElement == null`, the game keeps the event.

## Hit-testing

1. Painter order (front to back), overlays first.
2. Conservative `RenderBounds` AABB (corners mapped through the 4×4, including perspective divide).
3. `Transform.TryUnproject` onto local z=0, then `HitTestLocal` (rect + corner radii; override for custom shapes).
4. Clipping ancestors: the same screen point is unprojected into each clipping ancestor.

**Do not** invert the 4×4 and `MapPoint(screenX, screenY, z: 0)`. After `RotationX` / `RotationY`, the painted pixel is not at z=0 in that space, so the hit region slides off the drawing. `TryUnproject` solves the row-vector homography for `(lx, ly, 0, 1) * M`.

Override `HitTestLocal` for circles, paths, etc.

Hits are **geometric, not alpha**. Opacity 0 still hits unless `Interactive` is false or `IsClickthrough` is true. A full-window transparent root **steals world clicks**.

`View.HitTest` is internal. Public query:

```csharp
VisualElement? hit = session.HitTest(x, y);
bool wants = session.WantsPointer(x, y); // hit != null
```

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

If the top overlay has `BlockHitsUnderneath` (default **true**), a miss still returns the overlay **layer**. `WantsPointer` is then true for the **entire HUD rect**, including empty glass — correct for a modal, **not** world click-through. Use `IsClickthrough` on large empty panels, or a bottom-bar HUD with no full-size hittable backdrop, if the world must receive clicks.
