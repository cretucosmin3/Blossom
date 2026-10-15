using System;
using Blossom.Hosting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SkiaGameRendering;
using SkiaSharp;

namespace SnakeDemo;

/// <summary>
/// Snake: MonoGame owns the window, input, and the board (drawn with SpriteBatch); Blossom owns
/// every piece of UI (stat bar, menu, pause, game over) and renders it headlessly into a
/// <see cref="SkiaRenderTarget2D"/> that is composited over the board.
/// </summary>
public sealed class Game1 : Game
{
    public const int FoodsPerLevel = 5;
    private const float BoardMargin = 12;

    private readonly GraphicsDeviceManager _graphics;
    private readonly bool _smokeTest;
    private int _frameCount;

    private SpriteBatch _spriteBatch = null!;
    private Texture2D _whitePixel = null!;
    private SkiaRenderTarget2D _uiTarget = null!;
    private UiSession _session = null!;
    private HudView _hudView = null!;

    private readonly GameState _state = new();
    private readonly SnakeGame _snake = new();
    private float _stepTimer;
    private float _time;
    private readonly PerfStats _perf = new();

    private MouseState _prevMouse;
    private KeyboardState _prevKeyboard;
    private bool _pointerInside;

    public int ExitCode { get; private set; }

    public Game1(bool smokeTest = false)
    {
        _smokeTest = smokeTest;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 800,
            PreferredBackBufferHeight = 600
        };
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "Snake · Blossom + SkiaGameRendering";
    }

    protected override void Initialize()
    {
        base.Initialize();
        SkiaRenderer.Initialize(GraphicsDevice);

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        int w = GraphicsDevice.Viewport.Width;
        int h = GraphicsDevice.Viewport.Height;
        _uiTarget = new SkiaRenderTarget2D(GraphicsDevice, w, h);

        _hudView = new HudView(_state);
        _hudView.PlayRequested += StartGame;
        _hudView.ResumeRequested += TogglePause;
        _hudView.RestartRequested += Restart;
        _hudView.MenuRequested += () => _state.Phase.Value = GamePhase.Menu;
        _hudView.QuitRequested += Exit;

        var options = new UiSessionOptions
        {
            PaintMode = PaintMode.FullFrame,
            ClearColor = SKColors.Transparent,
            ClearCanvas = true,
            MaxFps = 60
        };

        // One view, so no Application subclass: the session wraps the view itself.
        _session = new UiSession(new MonoGameUiHost(), _hudView, options);
        _session.SetSize(w, h, w, h);
        _session.Init();

        Window.ClientSizeChanged += OnClientSizeChanged;

        _snake.Ate += OnSnakeAte;
        _snake.Reset(); // Something to look at behind the menu.
        _state.Length.Value = _snake.Length;
    }

    private void OnClientSizeChanged(object? sender, EventArgs e)
    {
        int w = Math.Max(1, GraphicsDevice.Viewport.Width);
        int h = Math.Max(1, GraphicsDevice.Viewport.Height);
        if (w == _uiTarget.Texture.Width && h == _uiTarget.Texture.Height)
            return; // A same-size SetSize is a no-op in Blossom, so a fresh target would stay blank.

        _uiTarget.Dispose();
        _uiTarget = new SkiaRenderTarget2D(GraphicsDevice, w, h);
        _session.SetSize(w, h, w, h); // A real size change forces a full repaint into the new target.
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Game flow
    // ══════════════════════════════════════════════════════════════════════════

    // The HUD buttons and the hotkeys both call these, so each is a no-op outside its phase.
    private void StartGame()
    {
        if (_state.Phase.Value is GamePhase.Menu or GamePhase.GameOver)
            BeginRound();
    }

    // Straight from Paused to Playing: going through Menu would mount the menu dialog for one frame.
    private void Restart()
    {
        if (_state.Phase.Value == GamePhase.Paused)
            BeginRound();
    }

    private void BeginRound()
    {
        _snake.Reset();
        _stepTimer = 0;
        _state.Score.Value = 0;
        _state.Level.Value = 1;
        _state.Length.Value = _snake.Length;
        _state.NewBest.Value = false;
        _state.Death.Value = DeathCause.None;
        _state.Phase.Value = GamePhase.Playing;
    }

    private void TogglePause()
    {
        _state.Phase.Value = _state.Phase.Value switch
        {
            GamePhase.Playing => GamePhase.Paused,
            GamePhase.Paused => GamePhase.Playing,
            var other => other,
        };
    }

    private void OnSnakeAte(int length)
    {
        int points = GameState.PointsPerFood(_state.Difficulty.Value) * _state.Level.Value;
        _state.Length.Value = length;
        _state.Score.Value += points;

        // World position -> screen position: the UI effect spawns right on top of the eaten food.
        var board = Board;
        var head = _snake.Head;
        _hudView.ShowPopup(board.X + (head.X + 0.5f) * board.Cell, board.Y + head.Y * board.Cell, $"+{points}");

        int level = 1 + (length - SnakeGame.StartLength) / FoodsPerLevel;
        if (level > _state.Level.Value)
            _hudView.ShowLevelUp(level, board.Y + board.Height / 2f);
        _state.Level.Value = level;
    }

    private void EndGame()
    {
        if (_state.Score.Value > _state.Best.Value)
        {
            _state.Best.Value = _state.Score.Value;
            _state.NewBest.Value = true;
        }

        _state.Death.Value = _snake.Death;
        _state.Phase.Value = GamePhase.GameOver;
    }

    private float StepSeconds =>
        GameState.BaseStepSeconds(_state.Difficulty.Value) * MathF.Pow(0.92f, _state.Level.Value - 1);

    // ══════════════════════════════════════════════════════════════════════════
    // Update
    // ══════════════════════════════════════════════════════════════════════════

    protected override void Update(GameTime gameTime)
    {
        long updateStart = PerfStats.Now;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _time += dt;

        // Advance Blossom's timers and reactive dispatchers.
        _session.Tick(dt);

        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        ForwardInputToBlossom(mouse, keyboard);

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (!_prevKeyboard.IsKeyDown(key))
                OnGameKey(key);
        }

        if (_state.Phase.Value == GamePhase.Playing)
        {
            _stepTimer += dt;
            float step = StepSeconds;
            while (_stepTimer >= step && _state.Phase.Value == GamePhase.Playing)
            {
                _stepTimer -= step;
                _snake.Step();
                if (_snake.IsDead)
                    EndGame();
            }
        }

        _prevMouse = mouse;
        _prevKeyboard = keyboard;

        base.Update(gameTime);

        _perf.AddUpdate(PerfStats.MsSince(updateStart));
        if (_perf.TryPublish(_graphics.SynchronizeWithVerticalRetrace, out var snapshot))
            _state.Perf.Value = snapshot;
    }

    /// <summary>
    /// VSync off also turns off MonoGame's fixed 60 Hz timestep, so FPS shows what the game can
    /// actually reach. Gameplay is unaffected: the snake steps on accumulated time, not frames.
    /// </summary>
    private void ToggleVSync()
    {
        bool on = !_graphics.SynchronizeWithVerticalRetrace;
        _graphics.SynchronizeWithVerticalRetrace = on;
        IsFixedTimeStep = on;

        // ApplyChanges also re-applies the preferred back buffer size, which is still the startup
        // 800x600. Without this, toggling VSync in a resized window snaps it back to that size.
        var client = Window.ClientBounds;
        _graphics.PreferredBackBufferWidth = Math.Max(1, client.Width);
        _graphics.PreferredBackBufferHeight = Math.Max(1, client.Height);
        _graphics.ApplyChanges();
    }

    private void OnGameKey(Keys key)
    {
        if (key == Keys.F3)
        {
            _state.ShowPerf.Value = !_state.ShowPerf.Value;
            return;
        }
        if (key == Keys.F4)
        {
            ToggleVSync();
            return;
        }

        switch (_state.Phase.Value)
        {
            case GamePhase.Playing:
                switch (key)
                {
                    case Keys.Up or Keys.W: _snake.Turn(Direction.Up); break;
                    case Keys.Down or Keys.S: _snake.Turn(Direction.Down); break;
                    case Keys.Left or Keys.A: _snake.Turn(Direction.Left); break;
                    case Keys.Right or Keys.D: _snake.Turn(Direction.Right); break;
                    case Keys.Escape or Keys.P: TogglePause(); break;
                }
                break;

            case GamePhase.Paused:
                if (key is Keys.Escape or Keys.P)
                    TogglePause();
                else if (key == Keys.R)
                    Restart();
                else
                    NavigateDialog(key);
                break;

            case GamePhase.Menu:
                if (key is Keys.D1 or Keys.D2 or Keys.D3)
                    _state.Difficulty.Value = (Difficulty)(key - Keys.D1);
                else if (key is Keys.Left or Keys.A or Keys.Right or Keys.D)
                {
                    int delta = key is Keys.Left or Keys.A ? -1 : 1;
                    _state.Difficulty.Value = (Difficulty)Math.Clamp((int)_state.Difficulty.Value + delta, 0, 2);
                }
                else
                    NavigateDialog(key);
                break;

            case GamePhase.GameOver:
                if (key == Keys.Escape)
                    _state.Phase.Value = GamePhase.Menu;
                else
                    NavigateDialog(key);
                break;
        }
    }

    /// <summary>Keyboard drives the open Blossom dialog's selection, same as hovering with the mouse.</summary>
    private void NavigateDialog(Keys key)
    {
        if (key is Keys.Up or Keys.W)
            _hudView.MoveSelection(-1);
        else if (key is Keys.Down or Keys.S)
            _hudView.MoveSelection(1);
        else if (key is Keys.Enter or Keys.Space)
            _hudView.ActivateSelection();
    }

    private void ForwardInputToBlossom(MouseState mouse, KeyboardState keyboard)
    {
        // MonoGame keeps reporting the last position once the pointer leaves the window, so tell
        // Blossom explicitly; otherwise the row under the exit point stays hovered.
        // A drag that leaves the window keeps sending moves (pointer capture wants them); the leave
        // waits until the button is released.
        var client = Window.ClientBounds;
        bool inside = mouse.X >= 0 && mouse.Y >= 0 && mouse.X < client.Width && mouse.Y < client.Height;
        bool dragging = mouse.LeftButton == ButtonState.Pressed;
        if (inside)
            _pointerInside = true;
        else if (_pointerInside && !dragging)
        {
            _session.Input.PointerLeave();
            _pointerInside = false;
        }

        // Only on actual movement, like a desktop host's move events. Each PointerMove runs Blossom's
        // full hover routing and hit test (~13 KB of garbage), so sending one every frame is wasted work.
        if ((inside || dragging) && (mouse.X != _prevMouse.X || mouse.Y != _prevMouse.Y))
            _session.Input.PointerMove(mouse.X, mouse.Y);

        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            _session.Input.PointerDown(mouse.X, mouse.Y, 0); // 0 = Left
        else if (mouse.LeftButton == ButtonState.Released && _prevMouse.LeftButton == ButtonState.Pressed)
            _session.Input.PointerUp(mouse.X, mouse.Y, 0);

        int scrollDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
        if (scrollDelta != 0)
            _session.Input.Scroll(0, scrollDelta / 120f);

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (!_prevKeyboard.IsKeyDown(key))
                _session.Input.KeyDown(HudKeyMap.FromXna(key));
        }

        foreach (var key in _prevKeyboard.GetPressedKeys())
        {
            if (!keyboard.IsKeyDown(key))
                _session.Input.KeyUp(HudKeyMap.FromXna(key));
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Draw
    // ══════════════════════════════════════════════════════════════════════════

    protected override void Draw(GameTime gameTime)
    {
        _perf.BeginFrame();
        long drawStart = PerfStats.Now;

        GraphicsDevice.Clear(new Color(5, 6, 5));

        // 1. The board, drawn by MonoGame.
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        DrawBoard();
        _spriteBatch.End();

        // 2. Blossom UI on top: repaint into the Skia target only when something changed,
        //    otherwise composite the cached texture.
        if (_session.NeedsPaint)
        {
            long uiStart = PerfStats.Now;
            _uiTarget.Begin();
            var frame = new RenderFrame(
                _uiTarget.Canvas,
                _session.LogicalSize,
                _session.FramebufferSize,
                gpuContext: null,
                surface: _uiTarget.Canvas.Surface,
                clearCanvas: true,
                clearColor: SKColors.Transparent);

            _session.Paint(frame);
            _uiTarget.End(); // Composites the fresh UI onto the backbuffer.
            _perf.AddUiPaint(PerfStats.MsSince(uiStart));
        }
        else
        {
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _spriteBatch.Draw(_uiTarget.Texture, Vector2.Zero, Color.White);
            _spriteBatch.End();
        }

        if (_smokeTest)
        {
            _frameCount++;
            if (_frameCount == 2)
                StartGame(); // Exercise the Menu -> Playing transition and its unmount.
            if (_frameCount == 3)
                OnSnakeAte(SnakeGame.StartLength + FoodsPerLevel); // And the popup + level-up effects.
            if (_frameCount >= 5)
            {
                ExitCode = 0;
                Console.WriteLine("Snake.MonoGame smoke test PASSED");
                Exit();
            }
        }

        _perf.AddDraw(PerfStats.MsSince(drawStart));
        base.Draw(gameTime);
    }

    private readonly record struct BoardLayout(int X, int Y, int Cell, int Width, int Height);

    /// <summary>Largest whole-pixel cell size that fits the grid under the top bar, centered.</summary>
    private BoardLayout Board
    {
        get
        {
            var vp = GraphicsDevice.Viewport;
            float top = HudView.TopBarHeight + BoardMargin;
            int cell = (int)MathF.Max(4, MathF.Floor(MathF.Min(
                (vp.Width - BoardMargin * 2) / SnakeGame.Columns,
                (vp.Height - top - BoardMargin) / SnakeGame.Rows)));
            int w = cell * SnakeGame.Columns;
            int h = cell * SnakeGame.Rows;
            return new BoardLayout(
                (vp.Width - w) / 2,
                (int)top + ((int)(vp.Height - top - BoardMargin) - h) / 2,
                cell, w, h);
        }
    }

    private void DrawBoard()
    {
        var (ox, oy, cell, boardW, boardH) = Board;

        // Frame and a faint grid.
        Fill(new Rectangle(ox - 1, oy - 1, boardW + 2, boardH + 2), new Color(39, 39, 42));
        Fill(new Rectangle(ox, oy, boardW, boardH), new Color(9, 11, 9));
        for (int x = 1; x < SnakeGame.Columns; x++)
            Fill(new Rectangle(ox + x * cell, oy, 1, boardH), new Color(20, 24, 20));
        for (int y = 1; y < SnakeGame.Rows; y++)
            Fill(new Rectangle(ox, oy + y * cell, boardW, 1), new Color(20, 24, 20));

        Rectangle CellRect(Cell c, int inset) =>
            new(ox + c.X * cell + inset, oy + c.Y * cell + inset, cell - inset * 2, cell - inset * 2);

        // Food blinks between two sizes, like an arcade pickup.
        bool big = _time % 0.5f < 0.25f;
        Fill(CellRect(_snake.Food, big ? 3 : 5), new Color(252, 211, 77));

        // Snake: bright head fading toward the tail; red when dead.
        bool dead = _snake.IsDead && _state.Phase.Value == GamePhase.GameOver;
        var head = dead ? new Color(244, 63, 94) : new Color(190, 242, 100);
        var tail = dead ? new Color(76, 5, 25) : new Color(54, 83, 20);
        int i = 0, count = _snake.Length;
        foreach (var segment in _snake.Body)
        {
            float t = count <= 1 ? 0 : i / (float)(count - 1);
            Fill(CellRect(segment, 1), Color.Lerp(head, tail, t));
            i++;
        }
    }

    private void Fill(Rectangle rect, Color color) => _spriteBatch.Draw(_whitePixel, rect, color);

    protected override void UnloadContent()
    {
        _session?.Dispose();
        _uiTarget?.Dispose();
        _whitePixel?.Dispose();
        _spriteBatch?.Dispose();
        SkiaRenderer.Dispose();
        base.UnloadContent();
    }
}
