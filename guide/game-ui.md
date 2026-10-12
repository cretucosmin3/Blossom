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
        └── If session.NeedsPaint:
              ├── canvas.Begin()
              ├── session.Paint(renderFrame)
              └── canvas.End() -> Composite UI texture over game world
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

Call `session.Tick(deltaTime)` and feed input every game tick:

```csharp
public void Update(float deltaTime)
{
    // 1. Advance UI timers, reactive queues, and animations
    _session.Tick(deltaTime);

    // 2. Feed mouse / pointer input
    var mouse = Mouse.GetState();
    _session.Input.PointerMove(mouse.X, mouse.Y);

    if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        _session.Input.PointerDown(mouse.X, mouse.Y, UiMouseButton.Left);
    else if (mouse.LeftButton == ButtonState.Released && _prevMouse.LeftButton == ButtonState.Pressed)
        _session.Input.PointerUp(mouse.X, mouse.Y, UiMouseButton.Left);

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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SkiaGameRendering;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SkiaRenderTarget2D _uiTarget;
    private UiSession _session;

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
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _session.Tick(dt);

        var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
        _session.Input.PointerMove(mouse.X, mouse.Y);
        // ... map mouse and keyboard buttons ...

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // 1. Draw your game world
        DrawWorld();

        // 2. Draw Blossom UI if dirty
        if (_session.NeedsPaint)
        {
            _uiTarget.Begin();
            var frame = new RenderFrame(
                _uiTarget.Canvas,
                _session.LogicalSize,
                _session.FramebufferSize,
                gpuContext: null,
                surface: _uiTarget.Canvas.Surface,
                clearCanvas: true,
                clearColor: SkiaSharp.SKColors.Transparent);

            _session.Paint(frame);
            _uiTarget.End();
        }

        // 3. Composite UI target with premultiplied alpha blend
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        _spriteBatch.Draw(_uiTarget, Vector2.Zero, Color.White);
        _spriteBatch.End();

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
        statsPanel.Transform.X = 20;
        statsPanel.Transform.Y = 20;
        statsPanel.Transform.Width = 200;
        statsPanel.Transform.Height = 24;
        statsPanel.Style.BackColor = new SKColor(0, 0, 0, 160); // Semitransparent dark backing
        statsPanel.Style.Border = new BorderStyle { Width = 1, Color = new SKColor(255, 255, 255, 60), Radius = 4 };

        // Health fill
        HealthBar = new VisualElement { Name = "HealthFill" };
        HealthBar.Transform.Anchor = Anchor.Left | Anchor.Top | Anchor.Bottom;
        HealthBar.Transform.Width = 200;
        HealthBar.Style.BackColor = new SKColor(239, 68, 68, 255); // Red

        statsPanel.AddChild(HealthBar);
        root.AddChild(statsPanel);

        // 3. Minimap frame anchored to Top-Right
        var minimapFrame = new VisualElement { Name = "MinimapFrame" };
        minimapFrame.Transform.Anchor = Anchor.Right | Anchor.Top;
        minimapFrame.Transform.X = 20; // 20px from right edge
        minimapFrame.Transform.Y = 20;
        minimapFrame.Transform.Width = 150;
        minimapFrame.Transform.Height = 150;
        minimapFrame.Style.Border = new BorderStyle { Width = 2, Color = SKColors.Gold, Radius = 8 };

        root.AddChild(minimapFrame);

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
Bind.Width(healthBar, () => (PlayerState.Health.Value / 100f) * 200f);
Bind.Text(ammoLabel, () => $"AMMO: {PlayerState.Ammo.Value}");

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

When the player presses <kbd>Esc</kbd>, display a pause menu that blocks all clicks from reaching the game world underneath:

```csharp
public void OpenPauseMenu()
{
    var pauseOverlay = new VisualElement { Name = "PauseOverlay" };
    pauseOverlay.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
    pauseOverlay.Style.BackColor = new SKColor(0, 0, 0, 180); // Dark backdrop

    var resumeButton = new Button
    {
        Text = "Resume",
        Width = 160,
        Height = 40
    };
    resumeButton.OnClick += (e) => ActiveView.PopOverlay();
    pauseOverlay.AddChild(resumeButton);

    // BlockHitsUnderneath = true ensures mouse clicks do not pass through to the game world or HUD
    ActiveView.PushOverlay(pauseOverlay, blockHitsUnderneath: true);
}
```

---

## 7. Performance Best Practices for Game Developers

| Practice | Why it matters |
|---|---|
| **Set `MaxFps = 30` or `60`** | Prevents UI redraws at 144–240 FPS while the 3D engine runs at high refresh rates. |
| **Check `session.NeedsPaint`** | Skip the entire Skia `Begin()`, `Paint()`, and `End()` pipeline when the UI has not changed. |
| **Use `PaintMode.FullFrame`** | Best suited for rendering to game render targets/textures. |
| **Set `Visible = false`** | Avoids redundant layout and hit-testing overhead for offscreen or hidden panels. |
| **Avoid per-frame string allocations** | Use signals and formatted labels only when values actually change. |
| **Pre-register Fonts** | Call `FontManager.RegisterFont(...)` once during game asset loading. |
