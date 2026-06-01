# UI Re-rendering on Minimize Restoration

## Overview
When the application window is minimized (iconified) and subsequently restored, the graphics context/surface state needs to be updated. To ensure the user interface is fully up-to-date and correctly drawn upon restoration, we added an event listener to the window's `StateChanged` event.

## Changes Made
1. **Window State Event Handler**:
   - In `Browser.cs` ([Browser.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Browser.cs)), subscribed to the window's `StateChanged` event:
     ```csharp
     window.StateChanged += (state) =>
     {
         if (state != WindowState.Minimized && BrowserApp.ActiveView != null)
         {
             Browser.WasResized = true;
             BrowserApp.ActiveView.FullRenderRequired = true;
             BrowserApp.ActiveView.RenderRequired = true;
         }
     };
     ```
   - When the window state changes to anything other than `WindowState.Minimized` (such as `Normal` or `Maximized`), we force a full redraw by setting:
     - `Browser.WasResized = true` (to reset layout/rendering bounds).
     - `FullRenderRequired = true` and `RenderRequired = true` on the currently active view.

## Verification
- Run compilation using `./build.sh` to confirm no errors.
- Verified rendering benchmark performance has not been impacted.

---

# Debug Frame Time (ms) Overlay Fix

## Overview
Because Blossom implements an optimized on-demand partial dirty-region rendering pipeline, the area where the frame timer debug text (ms per frame) is drawn in the top left was not regularly cleared to the view's background color. This caused new text drawings to lay over old ones, rendering them unreadable.

## Changes Made
- **Badge Background Wipe**:
  - In `Browser.cs` ([Browser.cs](file:///home/kozmo/Documents/GitHub/Blossom/src/Browser.cs)), introduced `InfoBackgroundPaint` and `InfoBorderPaint`.
  - Replaced the simple `DrawText` call with a structured badge rendering step:
    - Measures the current text width dynamically using `InfoTextPaint.MeasureText`.
    - Draws an opaque rounded rectangle badge (`InfoBackgroundPaint`) and border (`InfoBorderPaint`) before drawing the text.
    - This completely clears/wipes the target coordinates of any old pixel remnants from the previous frame.

