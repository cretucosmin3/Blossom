# Using Blossom for Game UI

Blossom is a retained-mode C# UI framework that can run entirely detached from desktop windowing systems. You can embed Blossom directly into any game engine or custom render loop (MonoGame, FNA, Raylib, Stride, Godot, or custom OpenGL/Vulkan/DirectX/Metal engines).

In games, Blossom runs **headlessly**: the game engine owns the window, input polling, and graphics context, while Blossom maintains the retained UI element tree, runs animations/reactive bindings, routes input, and renders into a caller-provided Skia `SKCanvas` (typically backed by a GPU `RenderTarget2D` or texture).

---

## 1. Project References (Silk-Free Core)

Blossom's core libraries are 100% independent of Silk.NET and GLFW. When integrating into a game, reference **only the core trio**:

```xml
<ItemGroup>
  <!-- Core Blossom retained element tree & hosting -->
  <ProjectReference Include="path/to/Blossom/src/Blossom/Blossom.csproj" />
  <!-- Reactive signals and data-binding (optional but recommended) -->
  <ProjectReference Include="path/to/Blossom/src/Blossom.Reactive/Blossom.Reactive.csproj" />
  <!-- Layout primitives, colors, and themes -->
  <ProjectReference Include="path/to/Blossom/src/Blossom.Primitives/Blossom.Primitives.csproj" />
</ItemGroup>
```

> [!IMPORTANT]
> **Do NOT reference `Blossom.Desktop`** in game projects. `Blossom.Desktop` contains the desktop `Shell` and GLFW/Silk.NET window host. By omitting it, your game runtime remains 100% free of GLFW/Silk dependencies.

---

## 2. Architecture & Mental Model

```text
Game Engine (MonoGame, Raylib, Stride, Custom)
  │
  ├── Update Loop (every tick, e.g. 60–240 FPS)
  │     ├── session.Tick(dt)
  │     └── session.Input.PointerMove / PointerDown / KeyDown (...)
  │
  └── Render Loop
        ├── Render 3D World / Game Sprites
        ├── If session.NeedsPaint:
        │     ├── canvas.Begin()
        │     ├── session.Paint(renderFrame)
        │     └── canvas.End() -> Composite the fresh UI texture over the game world
        └── Else: composite the cached UI texture from the last paint
```

1. **`Application`**: Owns your game's `View`s (e.g. HUD view, Pause menu view, Inventory view).
2. **`UiSession`**: Headless session manager. Connects your `Application` with an `IUiHost`, tracks logical layout size, throttles frames via `MaxFps`, and exposes `session.Input`.
3. **`session.Input`**: Headless input injector. You translate game engine input events (mouse, keyboard, touch) into Blossom's input model.
4. **`session.Paint`**: Draws dirty UI elements onto your game's `SKCanvas`.

---

## 3. Setting Up `UiSession`

Use `SoftwareUiHost` (built into Blossom) or a custom `IUiHost`:

```csharp
using Blossom.Core;
using Blossom.Hosting;
using SkiaSharp;

public class GameUi
{
    private readonly UiSession _session;
    private readonly Application _app;

    public GameUi(int viewportWidth, int viewportHeight)
    {
        var host = new SoftwareUiHost();
        _app = new MyGameUiApp();

        var options = new UiSessionOptions
        {
            // FullFrame redraws the target texture when dirty (recommended for render targets)
            PaintMode = PaintMode.FullFrame,
            
            // Transparent clear color lets the game world show through behind HUD elements
            ClearColor = SKColors.Transparent,
            ClearCanvas = true,
            
            // Limit UI renders to save CPU/GPU cycles (e.g. 30 or 60 FPS)
            MaxFps = 60
        };

        _session = new UiSession(host, _app, options);
        _session.SetSize(viewportWidth, viewportHeight, viewportWidth, viewportHeight);
        _session.Init();
    }

    public UiSession Session => _session;
}
```

### Single-View Sessions (No `Application` Subclass)

A panel with one screen, such as a nameplate or a world-space display, can skip the `Application` subclass. Pass the view directly; the session wraps it in a private one-view application:

```csharp
var session = new UiSession(new SoftwareUiHost(), new NameplateView(), options);
```

Each session keeps its own UI clock (`session.Application.Time`), so animations and continuous shaders run at the right speed however many sessions you tick per frame. Use an `Application` subclass when the panel switches between several views.

### Handling Window Resize

When your game window or resolution changes:

```csharp
// logicalWidth, logicalHeight: UI units (taking DPI/UI scale into account)
// pixelWidth, pixelHeight: actual framebuffer pixel dimensions
_session.SetSize(logicalWidth, logicalHeight, pixelWidth, pixelHeight);
```

Blossom automatically triggers anchor recalculations and layout reflows without requiring an immediate paint pass.

---

## 4. Game Loop Integration

### A. The `Update` Step (Input & Timers)

Call `session.Tick(deltaTime)` every game tick, and feed input when it changes. Send `PointerMove` only when the pointer actually moved: each call runs the hit test and hover pass, and a desktop host doesn't send moves for a still mouse either.

```csharp
public void Update(float deltaTime)
{
    // 1. Advance UI timers, reactive queues, and animations
    _session.Tick(deltaTime);

    // 2. Feed mouse / pointer input
    var mouse = Mouse.GetState();
    if (mouse.X != _prevMouse.X || mouse.Y != _prevMouse.Y)
        _session.Input.PointerMove(mouse.X, mouse.Y);

    // Buttons are ints: 0 = left, 1 = right, 2 = middle.
    if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        _session.Input.PointerDown(mouse.X, mouse.Y, 0);
    else if (mouse.LeftButton == ButtonState.Released && _prevMouse.LeftButton == ButtonState.Pressed)
        _session.Input.PointerUp(mouse.X, mouse.Y, 0);

    // 3. Feed mouse wheel
    int wheelDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
    if (wheelDelta != 0)
        _session.Input.Scroll(0, wheelDelta / 120f);

    _prevMouse = mouse;
}
```

### B. Keyboard Input

Map your engine keys to Blossom's `UiKey`:

```csharp
public void OnKeyDown(Keys key)
{
    UiKey uiKey = key switch
    {
        Keys.Escape => UiKey.Escape,
        Keys.Enter => UiKey.Enter,
        Keys.Space => UiKey.Space,
        Keys.Tab => UiKey.Tab,
        Keys.W => UiKey.W,
        Keys.A => UiKey.A,
        Keys.S => UiKey.S,
        Keys.D => UiKey.D,
        _ => UiKey.Unknown
    };

    if (uiKey != UiKey.Unknown)
        _session.Input.KeyDown(uiKey);
}
```

### C. The `Draw` Step (`NeedsPaint` & `MaxFps`)

Most games run at 144, 240, or unlocked FPS, but UI rarely needs to render faster than 30 or 60 FPS. 

`UiSession.NeedsPaint` automatically:
1. Returns `false` if nothing in the UI tree is dirty (no animations, no hover changes, no signal updates).
2. Throttles to your configured `MaxFps` so changes accumulate smoothly without burning GPU bandwidth.

```csharp
public void Draw(SKSurface uiSurface)
{
    // Render your game world (3D terrain, sprites, meshes) first
    RenderGameWorld();

    // Only render Blossom UI when changes are ready
    if (_session.NeedsPaint)
    {
        var frame = new RenderFrame(
            canvas: uiSurface.Canvas,
            logicalSize: _session.LogicalSize,
            framebufferSize: _session.FramebufferSize,
            gpuContext: null, // or your GRContext if hardware-accelerated Skia
            surface: uiSurface,
            clearCanvas: true,
            clearColor: SKColors.Transparent);

        _session.Paint(frame);
    }

    // Composite the UI surface/texture on top of your game view
    DrawUiOverlayTexture(uiSurface);
}
```

---

## 5. MonoGame / FNA Integration with `SkiaGameRendering`

If using [SkiaGameRendering](https://github.com/vchelaru/SkiaGameRendering) in MonoGame or FNA:

```csharp
using Blossom.Core;
using Blossom.Hosting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SkiaGameRendering;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SkiaRenderTarget2D _uiTarget;
    private UiSession _session;
    private MouseState _prevMouse;

    protected override void Initialize()
    {
        base.Initialize();
        SkiaRenderer.Initialize(GraphicsDevice);
        
        _uiTarget = new SkiaRenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        
        var app = new MyGameHudApp();
        _session = new UiSession(new SoftwareUiHost(), app, new UiSessionOptions
        {
            PaintMode = PaintMode.FullFrame,
            ClearColor = SkiaSharp.SKColors.Transparent,
            ClearCanvas = true,
            MaxFps = 60
        });
        _session.SetSize(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height,
                         GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        _session.Init();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _session.Tick(dt);

        var mouse = Mouse.GetState();
        if (mouse.X != _prevMouse.X || mouse.Y != _prevMouse.Y)
            _session.Input.PointerMove(mouse.X, mouse.Y);
        // ... map mouse and keyboard buttons (see section A) ...
        _prevMouse = mouse;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // 1. Draw your game world
        DrawWorld();

        // 2. Draw Blossom UI: repaint it when dirty, otherwise reuse the last frame
        if (_session.NeedsPaint)
        {
            _uiTarget.Begin(); // Clears the target; FullFrame Paint then redraws the whole UI
            var frame = new RenderFrame(
                _uiTarget.Canvas,
                _session.LogicalSize,
                _session.FramebufferSize,
                gpuContext: null,
                surface: _uiTarget.Canvas.Surface,
                clearCanvas: true,
                clearColor: SkiaSharp.SKColors.Transparent);

            _session.Paint(frame);
            _uiTarget.End(); // Also composites the fresh UI over the game world
        }
        else
        {
            // Nothing changed: composite the texture from the last paint (premultiplied alpha)
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _spriteBatch.Draw(_uiTarget.Texture, Vector2.Zero, Color.White);
            _spriteBatch.End();
        }

        base.Draw(gameTime);
    }
}
```

---

## 6. Authoring Game UI (HUDs, Overlays & Menus)

### A. HUD Layouts (Transparent Screen-Edge Anchoring)

In games, the root view should fill the viewport, while individual HUD widgets anchor to edges. Empty regions between widgets should allow clicks through to the game world.

```csharp
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using SkiaSharp;

public class HudView : View
{
    public VisualElement HealthBar { get; private set; }

    public HudView() : base("HUD") { }

    public override void Init()
    {
        // 1. Root container fills the screen
        var root = new VisualElement { Name = "HudRoot" };
        root.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;

        // 2. Player Status Bar anchored to Top-Left
        var statsPanel = new VisualElement { Name = "StatsPanel" };
        statsPanel.Transform.Anchor = Anchor.Left | Anchor.Top;
        statsPanel.Transform.LocalX = 20;
        statsPanel.Transform.LocalY = 20;
        statsPanel.Transform.Width = 200;
        statsPanel.Transform.Height = 24;
        statsPanel.Style.BackColor = new SKColor(0, 0, 0, 160); // Semitransparent dark backing
        statsPanel.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(255, 255, 255, 60), Roundness = 4 };

        // Health fill
        HealthBar = new VisualElement { Name = "HealthFill" };
        HealthBar.Transform.Anchor = Anchor.Left | Anchor.Top | Anchor.Bottom;
        HealthBar.Transform.Width = 200;
        HealthBar.Style.BackColor = new SKColor(239, 68, 68, 255); // Red

        statsPanel.AddChild(HealthBar);
        root.AddChild(statsPanel);

        // 3. Bottom Dock anchored flush to Bottom (in e.g. 800x600 viewport)
        var bottomDock = new VisualElement { Name = "BottomDock" };
        bottomDock.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Bottom;
        bottomDock.Transform.LocalX = 0;
        bottomDock.Transform.LocalY = 600 - 72; // Height 72 -> 0px distance from bottom
        bottomDock.Transform.Height = 72;
        bottomDock.Style.BackColor = new SKColor(10, 14, 23, 230);
        bottomDock.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(56, 189, 248, 80), Roundness = 0 };
        root.AddChild(bottomDock);

        AddElement(root);
    }
}
```

### B. Reactive Game State (Health, Ammo, Scores)

Using `Blossom.Reactive`, you can bind game variables to the UI using signals. Updating a signal automatically marks the corresponding element dirty for the next frame without manually invalidating the view:

```csharp
using Blossom.Reactive;

public class PlayerState
{
    // Reactive signals
    public static readonly Signal<float> Health = new(100f);
    public static readonly Signal<int> Ammo = new(30);
    public static readonly Signal<int> Gold = new(0);
}

// In your HUD element initialization:
healthBar.Bind(el => el.Transform.Width = (PlayerState.Health.Value / 100f) * 200f);
ammoLabel.BindText(() => $"AMMO: {PlayerState.Ammo.Value}");

// In your game gameplay code:
PlayerState.Health.Value -= damageTaken; // Automatically schedules paint!
```

### C. Hiding Elements Efficiently

To hide UI elements (e.g. hiding the minimap or quest tracker in cutscenes), set `Visible = false`:

```csharp
minimapFrame.Visible = false;
```

> [!TIP]
> Setting `Visible = false` completely bypasses paint command recording, drops all continuous shader keep-alives, disables hit-testing in `ElementsMap`, and reflows layout containers. Never move elements to negative coordinates to hide them.

### D. Modal Pause Menus & Hit Blocking (`PushOverlay`)

When the player presses <kbd>Esc</kbd>, display a pause menu that blocks all clicks from reaching the game world underneath. `PushOverlay` is a `View` method, and it returns a lease: dispose the lease to close the overlay.

```csharp
using Blossom.Primitives; // ButtonBase: a headless button shell you give a look

// Members to add to the HudView from section A.
private IDisposable? _pauseMenu;

public void TogglePauseMenu()
{
    if (_pauseMenu != null)
    {
        _pauseMenu.Dispose(); // Pops the overlay
        _pauseMenu = null;
        return;
    }

    var pauseOverlay = new VisualElement { Name = "PauseOverlay" };
    pauseOverlay.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
    pauseOverlay.Style.BackColor = new SKColor(0, 0, 0, 180); // Dark backdrop

    var resumeButton = new ButtonBase { Text = "Resume" };
    resumeButton.Transform.Anchor = Anchor.Left | Anchor.Top;
    resumeButton.Transform.LocalX = 320;
    resumeButton.Transform.LocalY = 280;
    resumeButton.Transform.Width = 160;
    resumeButton.Transform.Height = 40;
    resumeButton.Style.BackColor = new SKColor(37, 99, 235);
    resumeButton.Style.Text.Color = SKColors.White;
    resumeButton.Clicked += TogglePauseMenu;
    pauseOverlay.AddChild(resumeButton);

    // BlockHitsUnderneath (the default) keeps clicks from reaching the HUD under the overlay
    _pauseMenu = PushOverlay(pauseOverlay, new OverlayOptions { BlockHitsUnderneath = true });
}
```

> [!TIP]
> If the menu should follow game state instead, drive it from a signal with `Show.When(root, () => phase.Value == Phase.Paused, () => CreatePauseMenu())`. Show.When mounts the menu when the condition turns true and disposes it, bindings included, when it turns false.

### E. One-Shot Effects (Score Popups, Banners)

Use `element.Animate()` from [Animation](animation.md). Chain the steps and end with `.Call(Dispose)`. The effect plays on UI time, so it lasts the same at any frame rate, and the UI goes idle once it is gone. Disposing from an animation callback (or from `OnUpdate`) is safe: the element leaves the tree, and the next frame repaints without it.

```csharp
sealed class ScorePopup : VisualElement
{
    public ScorePopup(string text)
    {
        Text = text;
        Animate()
            .MoveBy(0, -40, 0.8f)
            .With().Fade(0f, 0.8f, Ease.InQuad)
            .Call(Dispose);
    }
}

// Gameplay code: spawn it at the food's screen position.
hudRoot.AddChild(new ScorePopup("+20"));
```

State-driven motion, such as a highlighted menu row or a score that rolls up, fits `AnimatedValue` better. For per-frame logic of your own, override `OnUpdate(dt)`: `dt` is UI time since the previous paint pass, not since the last `Tick`.

---

## 7. Performance Best Practices for Game Developers

| Practice | Why it matters |
|---|---|
| **Set `MaxFps = 30` or `60`** | Prevents UI redraws at 144–240 FPS while the 3D engine runs at high refresh rates. |
| **Check `session.NeedsPaint`** | Skip the entire Skia `Begin()`, `Paint()`, and `End()` pipeline when the UI has not changed. |
| **Use `PaintMode.FullFrame`** | Best suited for rendering to game render targets/textures. |
| **Set `Visible = false`** | Avoids redundant layout and hit-testing overhead for offscreen or hidden panels. |
| **Avoid per-frame string allocations** | Use signals and formatted labels only when values actually change. |
| **Pre-register Fonts** | Call `Blossom.Utils.Fonts.RegisterFromFile(path)` (or `RegisterFromStream`) once during game asset loading. |
