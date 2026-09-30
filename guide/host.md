# Host (`Shell`)

`Shell` is the static process host: GLFW window, input wiring, frame timing, clipboard, and the UI thread. It used to be named `Browser`. Do not also name a widget `Shell`.

Tree: **Shell → Application → View → VisualElement**.

Do not take Silk's `IWindow` or `IInputContext` by reflection. The public surface below is what other apps (and later Blossom primitives) are supposed to use.

## Window

Set policy on the application before `Shell.Initialize`:

```csharp
public abstract class Application
{
    public WindowOptions Window { get; } = new();
    public bool EnableStatsOverlay { get; set; } // default false
}

public sealed class WindowOptions
{
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 800;
    public int MinWidth { get; set; }
    public int MinHeight { get; set; }
    public bool CenterOnLoad { get; set; } = true;
    public bool Resizable { get; set; } = true;
}
```

At runtime:

```csharp
int w = Shell.ClientWidth;
int h = Shell.ClientHeight;
Shell.SetClientSize(w, h);
Shell.SetMinClientSize(800, 500);
Shell.SetMaxClientSize(0, 0); // 0,0 = no max
Shell.Center();
Shell.Title = "…";
nint hwndOrNative = Shell.NativeHandle;
Shell.ClientResized += (width, height) => { /* layout already forced */ };
```

`ClientResized` fires after `RenderRect` updates and the active view is laid out.

**GLFW size limits** must run after the native window exists (`Load`, `window.Handle != 0`). Calling `glfwSetWindowSizeLimits` right after `Window.Create` asserts `window != NULL`. `Shell` applies min/max in `Load`. If you wrap GLFW yourself, wait for the same point.

## Stats overlay (F12)

| `EnableStatsOverlay` | F12 | Overlay |
|---|---|---|
| `false` (default) | Delivered like any other key | Stays hidden; host does not toggle it |
| `true` | Host handles it **first**; app/field F12 binds do not run | Toggles on each F12 |

`Shell.ShowDebugOverlay` is visibility only. CLI `--fps` should set both the flag and the visibility.

## UI thread

The element tree is UI-thread only.

```csharp
Shell.CheckAccess();
Shell.Post(() => { /* marshal from a worker */ });
await Shell.PostAsync(() => { /* same, as a Task */ });

IDisposable blink = Shell.PostPeriodic(TimeSpan.FromMilliseconds(500), ToggleCaret);
IDisposable later = Shell.PostDelayed(TimeSpan.FromSeconds(1), HideToast);
// dispose to cancel
```

On window `Load`, `Shell` installs a `SynchronizationContext`. `await` from that thread resumes on it; `Post`/`Send` drain the existing queue and wake GLFW with `PostEmptyEvent`.

## Idle loop

When there is nothing to paint, the host waits with `glfwWaitEventsTimeout` until input, a posted work item, the next frame (if `MaxFps` / continuous shaders), or the next delayed timer. Do not spin with `Sleep(1)`. Apps keep calling `InvalidatePaint` / `Post` as usual.

## Clipboard and cursor

```csharp
string? text = Shell.GetClipboardText();
Shell.SetClipboardText("copied");
Shell.SetCursor(StandardCursor.Hand);
```
