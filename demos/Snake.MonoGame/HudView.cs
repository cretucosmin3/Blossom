using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Primitives;
using Blossom.Reactive;
using SkiaSharp;

namespace SnakeDemo;

/// <summary>
/// Every piece of Snake's UI, built with Blossom: a stat bar along the top, in-game effects (score
/// popups, level-up banner), and the menu, pause, and game-over dialogs. The dialogs are mounted
/// and unmounted by <see cref="Show.When"/> off the <see cref="GameState.Phase"/> signal, so the game
/// only ever flips the phase and the UI follows. MonoGame draws the board underneath.
/// </summary>
public sealed class HudView : View
{
    public const float TopBarHeight = 56;

    private readonly GameState _state;
    private VisualElement _fx = null!;
    private MenuList? _menu;

    public event Action? PlayRequested;
    public event Action? ResumeRequested;
    public event Action? RestartRequested;
    public event Action? MenuRequested;
    public event Action? QuitRequested;

    public HudView(GameState state) : base("HUD")
    {
        _state = state;
    }

    public override void Init()
    {
        var root = new VisualElement { Name = "HudRoot" };
        root.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
        root.AddChild(CreateTopBar());

        // Transient effects live in their own full-screen layer, under the dialogs.
        _fx = new VisualElement { Name = "Effects" };
        _fx.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
        root.AddChild(_fx);
        // Right|Bottom anchors keep the distance to the edges as they are when laid out, so place it
        // against the real viewport size, not a design size.
        root.AddChild(new PerfOverlay(_state, Width - 12 - PerfOverlay.Width, Height - 12 - PerfOverlay.Height));
        AddElement(root);

        // One dialog per phase. Show.When disposes a dialog (and its bindings) when the phase leaves it.
        Show.When(root, () => _state.Phase.Value == GamePhase.Menu, () => Backdrop(CreateMenuCard()));
        Show.When(root, () => _state.Phase.Value == GamePhase.Paused, () => Backdrop(CreatePauseCard()));
        Show.When(root, () => _state.Phase.Value == GamePhase.GameOver, () => Backdrop(CreateGameOverCard()));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // API for the game
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Moves the highlighted row of the open dialog.</summary>
    public void MoveSelection(int delta) => _menu?.Move(delta);

    /// <summary>Activates the highlighted row of the open dialog.</summary>
    public void ActivateSelection() => _menu?.Activate();

    /// <summary>Floats a "+points" label up from a screen position.</summary>
    public void ShowPopup(float x, float y, string text) => _fx.AddChild(new Popup(text, x, y, Look.Gold));

    /// <summary>Slams a level-up strip across the screen, centered on <paramref name="centerY"/>.</summary>
    public void ShowLevelUp(int level, float centerY)
    {
        var banner = new LevelBanner(level);
        banner.Transform.LocalY = centerY - banner.Transform.Height / 2;
        _fx.AddChild(banner);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Top bar
    // ══════════════════════════════════════════════════════════════════════════

    private VisualElement CreateTopBar()
    {
        var bar = new VisualElement { Name = "TopBar" };
        bar.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top;
        bar.Transform.Width = 800;
        bar.Transform.Height = TopBarHeight;
        bar.Style.BackColor = Look.Panel.WithAlpha(245);
        bar.Style.Border = new BorderStyle { Width = 0, Roundness = 0 };

        var rule = Look.Box(0, TopBarHeight - 2, 800, 2, Look.Accent);
        rule.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top;
        bar.AddChild(rule);

        // A Stack lays the bar out: the title soaks up spare width and the readouts stay right-aligned
        // as the window resizes. Insets go in Padding, because a stretched L|R child reseeds its width
        // to "parent width minus LocalX" and would lose a right-hand margin set any other way.
        var row = new Stack { Name = "TopBarRow", Orientation = Orientation.Horizontal, Gap = 18 };
        row.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
        row.Padding = new Thickness(16, 7, 16, 9);

        var title = new VisualElement { Name = "TitleBlock" };
        title.AddChild(Look.Label("SNAKE", 0, 0, 80, 22, 17f, Look.Accent, TextAlign.Left, 900));
        title.AddChild(Look.Label("blossom ui // monogame world", 0, 23, 260, 14, 9.5f, Look.Dim));
        title.AddChild(CreatePhaseTag(78, 3));
        row.AddChild(title);
        Stack.SetGrow(title, 1f);

        row.AddChild(Divider());
        row.AddChild(new StatReadout("SCORE", () => _state.Score.Value, Look.Gold, 70));
        row.AddChild(new StatReadout("BEST", () => _state.Best.Value, Look.Text, 60));
        row.AddChild(new StatReadout("LENGTH", () => _state.Length.Value, Look.Accent, 54));
        row.AddChild(Divider());
        row.AddChild(CreateLevelReadout());

        bar.AddChild(row);
        return bar;
    }

    private static VisualElement Divider()
    {
        var d = new VisualElement { Name = "Divider" };
        d.Transform.Width = 1;
        d.Style.BackColor = Look.Line;
        return d;
    }

    /// <summary>PLAYING / PAUSED / ... tag whose text and colors follow the phase signal.</summary>
    private VisualElement CreatePhaseTag(float x, float y)
    {
        var tag = Look.Label("", x, y, 84, 16, 9f, Look.Ink, TextAlign.Center, 800);
        tag.Style.Border = new BorderStyle { Width = 0, Roundness = 0 };
        tag.Bind(t =>
        {
            var phase = _state.Phase.Value;
            t.Text = phase switch
            {
                GamePhase.Playing => "PLAYING",
                GamePhase.Paused => "PAUSED",
                GamePhase.GameOver => "GAME OVER",
                _ => "MENU",
            };
            t.Style.BackColor = phase switch
            {
                GamePhase.Playing => Look.Accent,
                GamePhase.Paused => Look.Gold,
                GamePhase.GameOver => Look.Danger,
                _ => Look.Line,
            };
            t.Style.Text.Color = phase == GamePhase.Menu ? Look.Dim : Look.Ink;
        });
        return tag;
    }

    /// <summary>Level number plus pips that fill toward the next level.</summary>
    private VisualElement CreateLevelReadout()
    {
        var level = new VisualElement { Name = "LevelReadout" };
        level.Transform.Width = 80;
        level.AddChild(Look.Place(new StatReadout("LEVEL", () => _state.Level.Value, Look.Accent, 28), 0, 0, 28, 40));

        for (int i = 0; i < Game1.FoodsPerLevel; i++)
        {
            int pip = i;
            var box = Look.Box(30 + i * 10, 22, 7, 12, Look.Line);
            box.BindBackColor(() =>
                (_state.Length.Value - SnakeGame.StartLength) % Game1.FoodsPerLevel > pip ? Look.Accent : Look.Line);
            level.AddChild(box);
        }

        return level;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Dialogs
    // ══════════════════════════════════════════════════════════════════════════

    private static VisualElement Backdrop(VisualElement card)
    {
        var backdrop = new FadeBackdrop
        {
            Name = "Backdrop",
            Columns = $"*, {card.Transform.Width}, *",
            Rows = $"*, {card.Transform.Height}, *",
        };
        backdrop.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
        backdrop.Style.BackColor = new SKColor(0, 0, 0, 170);
        backdrop.AddChild(card);
        Grid.SetCell(card, column: 1, row: 1);
        return backdrop;
    }

    /// <summary>A square dialog with a solid title strip, like a terminal window.</summary>
    private static VisualElement Card(string name, string title, string tag, float w, float h, SKColor accent)
    {
        var card = new CrtCard { Name = name };
        card.Transform.Width = w;
        card.Transform.Height = h;
        card.Style.BackColor = Look.Panel;
        card.Style.Border = new BorderStyle { Width = 1, Color = accent, Roundness = 0 };
        card.Style.Shadow = Look.HardShadow(8);

        card.AddChild(Look.Box(0, 0, w, 22, accent));
        card.AddChild(Look.Label(title, 10, 2, w - 100, 18, 10.5f, Look.Ink, TextAlign.Left, 900));
        card.AddChild(Look.Label(tag, w - 110, 2, 100, 18, 9.5f, Look.Ink.WithAlpha(170), TextAlign.Right, 700));
        return card;
    }

    /// <summary>A big title followed by a blinking block cursor.</summary>
    private void AddHeadline(VisualElement card, string text, float y, float size, SKColor color)
    {
        var label = Look.Label(text, 20, y, 300, size + 10, size, color, TextAlign.Left, 900);
        card.AddChild(label);
        float width = label.Style.Text.SkFont.MeasureText(text);
        card.AddChild(Look.Place(new Blinker(Application.Session!.Host, color), 20 + width + 6, y + size * 0.2f, size * 0.45f, size * 0.85f));
    }

    private MenuList NewMenu() => _menu = new MenuList();

    private VisualElement CreateMenuCard()
    {
        const float w = 360, inner = w - 40;
        var card = Card("MenuCard", "SNAKE.EXE", "v1.0", w, 334, Look.Accent);

        AddHeadline(card, "SNAKE", 36, 40f, Look.Accent);
        card.AddChild(Look.Label("eat. grow. don't bite yourself.", 20, 88, inner, 16, 11f, Look.Dim));

        card.AddChild(Look.Label("SPEED", 20, 120, 100, 14, 9f, Look.Dim, TextAlign.Left, 700));
        card.AddChild(Look.Label("1-3  /  LEFT-RIGHT", w - 220, 120, 200, 14, 9f, Look.Dim, TextAlign.Right));
        var segments = Look.Place(new Stack { Name = "SpeedPicker", Orientation = Orientation.Horizontal, Gap = 6 },
            20, 136, inner, 30);
        foreach (var d in Enum.GetValues<Difficulty>())
        {
            var seg = new SegmentButton($"{(int)d + 1} {d.ToString().ToUpperInvariant()}");
            seg.Clicked += () => _state.Difficulty.Value = d;
            seg.Bind(s => s.Selected = _state.Difficulty.Value == d);
            segments.AddChild(seg);
            Stack.SetGrow(seg, 1f);
        }
        card.AddChild(segments);

        var menu = NewMenu();
        card.AddChild(Look.Place(menu.Add(new MenuItem("PLAY", "ENTER", inner, 36, Look.Accent), () => PlayRequested?.Invoke()), 20, 182, inner, 36));
        card.AddChild(Look.Place(menu.Add(new MenuItem("QUIT", "", inner, 36, Look.Danger), () => QuitRequested?.Invoke()), 20, 222, inner, 36));

        card.AddChild(Look.Box(20, 274, inner, 1, Look.Line));
        card.AddChild(Look.Label("UP/DN select    ENTER confirm", 20, 284, inner, 14, 9.5f, Look.Dim));
        card.AddChild(Look.Label("WASD/ARROWS steer    ESC pause", 20, 302, inner, 14, 9.5f, Look.Dim));
        return card;
    }

    private VisualElement CreatePauseCard()
    {
        const float w = 320, inner = w - 40;
        var card = Card("PauseCard", "PAUSED", "ESC", w, 284, Look.Gold);

        AddHeadline(card, "PAUSED", 36, 30f, Look.Gold);
        var summary = Look.Label("", 20, 78, inner, 16, 11f, Look.Dim);
        summary.BindText(() => $"score {_state.Score.Value} // len {_state.Length.Value} // lv {_state.Level.Value}");
        card.AddChild(summary);

        var menu = NewMenu();
        float y = 108;
        foreach (var (label, key, accent, action) in new (string, string, SKColor, Action)[]
        {
            ("RESUME", "ESC", Look.Gold, () => ResumeRequested?.Invoke()),
            ("RESTART", "R", Look.Gold, () => RestartRequested?.Invoke()),
            ("MAIN MENU", "", Look.Gold, () => MenuRequested?.Invoke()),
            ("QUIT", "", Look.Danger, () => QuitRequested?.Invoke()),
        })
        {
            card.AddChild(Look.Place(menu.Add(new MenuItem(label, key, inner, 36, accent), action), 20, y, inner, 36));
            y += 40;
        }
        return card;
    }

    private VisualElement CreateGameOverCard()
    {
        const float w = 360, inner = w - 40;
        var card = Card("GameOverCard", "GAME OVER", "R.I.P.", w, 266, Look.Danger);

        string cause = _state.Death.Peek() switch
        {
            DeathCause.Wall => "> you hit the wall",
            DeathCause.Self => "> you bit yourself",
            DeathCause.BoardFull => "> board cleared. respect.",
            _ => "> ...",
        };
        card.AddChild(Look.Label(cause, 20, 34, inner, 18, 12f, Look.Danger, TextAlign.Left, 700));

        card.AddChild(Look.Label("FINAL SCORE", 20, 64, 140, 14, 9f, Look.Dim, TextAlign.Left, 700));
        var score = Look.Place(new CountUp(_state.Score.Peek()), 20, 76, 200, 54);
        score.Style.Text.Font = Look.Mono;
        score.Style.Text.Size = 46f;
        score.Style.Text.Weight = 900;
        score.Style.Text.Color = Look.Gold;
        score.Style.Text.Alignment = TextAlign.Left;
        card.AddChild(score);

        var stamp = Look.Place(new Stamp("NEW BEST", Look.Gold, delay: 0.8f), w - 140, 88, 112, 28);
        stamp.Visible = _state.NewBest.Peek();
        card.AddChild(stamp);

        var stats = Look.Label("", 20, 134, inner, 16, 11f, Look.Dim);
        stats.BindText(() =>
            $"len {_state.Length.Value} // lv {_state.Level.Value} // {_state.Difficulty.Value.ToString().ToLowerInvariant()}");
        card.AddChild(stats);

        var menu = NewMenu();
        card.AddChild(Look.Place(menu.Add(new MenuItem("PLAY AGAIN", "ENTER", inner, 36, Look.Accent), () => PlayRequested?.Invoke()), 20, 170, inner, 36));
        card.AddChild(Look.Place(menu.Add(new MenuItem("MAIN MENU", "ESC", inner, 36, Look.Danger), () => MenuRequested?.Invoke()), 20, 210, inner, 36));
        return card;
    }
}
