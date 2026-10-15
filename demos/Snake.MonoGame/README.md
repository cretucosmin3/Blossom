# Snake.MonoGame

A tiny Snake game whose UI is built entirely with Blossom, embedded headlessly in a MonoGame DesktopGL game loop. Blossom paints into a GPU `SkiaRenderTarget2D` from the [SkiaGameRendering](https://www.nuget.org/packages/SkiaGameRendering) NuGet package.

---

## Architecture

- **No Silk.NET / GLFW**: Project-references only the Blossom core trio (`Blossom`, `Blossom.Reactive`, `Blossom.Primitives`) from this repo. Does **not** reference `Blossom.Desktop`. SkiaGameRendering and MonoGame come from NuGet.
- **Session** (`Game1.cs`, `MonoGameUiHost.cs`): one `UiSession` built straight from `HudView` (no `Application` subclass). The host turns Blossom cursor requests into MonoGame cursors, so buttons show a hand.
- **Game world** (`SnakeGame.cs`, `Game1.cs`): engine-agnostic Snake rules on a 28×20 grid; MonoGame draws the board, snake, and food with `SpriteBatch`.
- **Shared state** (`GameState.cs`): `Signal<T>`s for phase, score, best, length, level, and difficulty. The game writes them; the UI binds to them, so every change schedules exactly the repaint it needs.
- **UI** (`HudView.cs`, `HudWidgets.cs`, `Look.cs`): a hard-edged terminal look (monospace, square corners, flat fills, hard shadows).
  - Top stat bar laid out by a `Stack`; readouts roll toward their value and punch when it grows, a phase tag follows the phase signal, and pips track progress to the next level.
  - Menu / pause / game-over dialogs mounted by `Show.When` off the phase signal, centered by a `Grid` backdrop, and switched on like a CRT. Rows are `ButtonBase` shells; hover and Up/Down drive the same selection. The blinking title cursor runs on a host timer, so an open dialog costs two repaints a second.
  - In-game effects: "+points" popups spawned at the food's screen position, a level-up banner, a counting-up final score, and a "NEW BEST" stamp.
  - Every animation uses `Blossom.Animations`: one-shot effects are `element.Animate()` timelines that end in `.Call(Dispose)`, and selection highlights and the rolling numbers are `AnimatedValue`s. They run on Blossom's UI time, so they play at the same speed with VSync on or off, and the UI goes idle (no repaints) once nothing is moving.
- **Perf overlay** (`PerfStats.cs`, `PerfOverlay.cs`): bottom-right panel with FPS, frame time (average and worst), CPU time for Update, Draw, and the Blossom UI repaint (which Draw includes), UI repaints per second, GC heap, allocation rate, gen-0 collections, and a frame-time graph against the 60 FPS budget. It publishes 4x a second, so it costs about 4 UI repaints per second itself. Times are CPU-side; GPU time isn't measured.
- **GPU composite**: Blossom renders into an offscreen `SkiaRenderTarget2D` via `session.Paint(renderFrame)` whenever `session.NeedsPaint` is true (throttled at `MaxFps = 60`); otherwise the cached texture is composited over the board.

---

## Running

From the repo root:

```bash
dotnet run --project demos/Snake.MonoGame/Snake.MonoGame.csproj
```

It is also in `Blossom.sln` under `demos`. `--smoke-test` renders a few frames, starts a game, and exits 0.

### Controls

- **Arrows / WASD**: Steer.
- **Up/Down + Enter**, or the mouse: Pick a row in a dialog.
- **1 / 2 / 3** or **Left/Right** on the menu: Chill, Normal, or Frantic.
- **Esc / P**: Pause and resume. **R** restarts from the pause menu. **Esc** on the game-over screen returns to the menu.
- **F3**: Show or hide the perf overlay. **F4**: Toggle VSync (off also uncaps MonoGame's fixed 60 Hz timestep).

Every five foods the level goes up: the snake speeds up and each food is worth more.
