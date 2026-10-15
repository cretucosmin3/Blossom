# Host (`Shell` / `Blossom.Desktop`)

`Shell` is the static desktop process host (located in `Blossom.Desktop`): GLFW window, input wiring, frame timing, clipboard, and the UI thread. It used to be named `Browser`. Do not also name a widget `Shell`. Core `Blossom` is Silk-free and hosts can also run headlessly via `UiSession`.

Tree: **Shell → Application → View → VisualElement**.

Do not take Silk's `IWindow` or `IInputContext` by reflection. The public surface below is what other apps (and later Blossom primitives) are supposed to use.

## Window

Desktop apps derive from `DesktopApplication` (namespace `Blossom.Desktop`), which adds the window policy to core `Application`. Set it before `Shell.Initialize`:

```csharp
public abstract class DesktopApplication : Application
{
    public WindowOptions Window { get; } = new();
    public bool EnableStatsOverlay { get; set; } // default false
    public string Title { get; set; } = "";     // empty = active view name
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
Shell.SetCursor(UiCursor.Hand);
```

## Embed (`UiSession`)

Desktop apps still call `Shell.Initialize`. Games and tests construct a `UiSession` with an `IUiHost` and pump `Tick` / `Paint` themselves (CI uses `SoftwareUiHost`; no GLFW window).

```csharp
var host = new SoftwareUiHost();
var app = new MyApplication();
using var session = new UiSession(host, app, new UiSessionOptions
{
    PaintMode = PaintMode.FullFrame,
    ClearColor = SKColors.Transparent,
    ClearCanvas = true
});
session.SetSize(logicalW, logicalH, pixelW, pixelH);
session.Init();

session.Tick(dt);
session.Input.PointerMove(x, y); // see Input
if (session.NeedsPaint)
{
    session.Paint(new RenderFrame(
        canvas, session.LogicalSize, session.FramebufferSize,
        gpuContext: null, surface: surface, clearCanvas: true));
}
```

`Tick` is CPU-only (dispatcher, timers, SKSL clocks). `Paint` is the only GPU scope — do not call `Gpu.*` from Tick or a posted callback. `UiHost.Install` is process-lifetime; Tick/Paint cannot nest and do not uninstall the host. Pointer/key/text enter through `session.Input` ([Input](input.md)).

Size is per session (`SetSize`). Layout (`View.Width` / `LayoutWidth`) reads `Application.Session.LogicalSize` at any time, including after `Init` without a paint pass.
