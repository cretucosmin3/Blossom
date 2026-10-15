using System;
using System.Collections.Generic;
using Blossom.Animations;
using Blossom.Core.Visual;
using Blossom.Hosting;
using Blossom.Core.Visual.Enums;
using Blossom.Primitives;
using Blossom.Reactive;
using SkiaSharp;

namespace SnakeDemo;

// Small animated building blocks for HudView, all built on Blossom.Animations: one-shot effects
// chain steps with element.Animate(), and state-driven motion (selection, rolling numbers) eases an
// AnimatedValue toward its target. Both run on Blossom's UI time, so they play at the same speed at
// any frame rate, and the UI goes idle (no repaints) once nothing is moving.

/// <summary>
/// A block that blinks forever: the terminal cursor. A host timer flips it, so the UI repaints twice
/// per period. A looping animation would also work, but an animation that is waiting still repaints
/// every frame.
/// </summary>
internal sealed class Blinker : VisualElement
{
    public Blinker(IUiHost host, SKColor color, float period = 1f)
    {
        Style.BackColor = color;
        Style.Border = new BorderStyle { Width = 0, Roundness = 0 };
        var timer = host.PostPeriodic(TimeSpan.FromSeconds(period / 2), () => Opacity = Opacity > 0 ? 0f : 1f);
        Disposed += _ => timer.Dispose();
    }
}

/// <summary>Full-screen dim layer that fades in. A <see cref="Grid"/>, so it also centers the card.</summary>
internal sealed class FadeBackdrop : Grid
{
    public FadeBackdrop()
    {
        Opacity = 0;
        Animate().Fade(1f, 0.12f, Ease.Linear);
    }
}

/// <summary>A dialog that switches on like a CRT: a bright line that snaps open vertically.</summary>
internal sealed class CrtCard : VisualElement
{
    public CrtCard()
    {
        Transform.ScaleY = 0.02f;
        Transform.ScaleX = 1.06f;
        Animate().ScaleY(1f, 0.18f).With().ScaleX(1f, 0.18f);
    }
}

/// <summary>Floating "+20" that rises off the food, fades, and disposes itself.</summary>
internal sealed class Popup : VisualElement
{
    public Popup(string text, float x, float y, SKColor color)
    {
        Look.Place(this, x - 50, y - 12, 100, 24);
        Text = text;
        Style.Text.Font = Look.Mono;
        Style.Text.Size = 15f;
        Style.Text.Weight = 800;
        Style.Text.Color = color;
        Style.Text.Alignment = TextAlign.Center;
        Style.Text.Shadow = new ShadowStyle(2, 2, 0, 0, new SKColor(0, 0, 0, 255));
        Transform.ScaleX = Transform.ScaleY = 1.5f;

        Animate()
            .MoveBy(0, -40, 0.9f)
            .With().Scale(1f, 0.12f, Ease.Linear)        // Punch in...
            .At(0.45f).Fade(0f, 0.45f, Ease.Linear)      // ...and fade out over the second half.
            .Call(Dispose);
    }
}

/// <summary>Full-width strip that slams in on level up, holds, collapses, and disposes itself.</summary>
internal sealed class LevelBanner : VisualElement
{
    public LevelBanner(int level)
    {
        Name = "LevelBanner";
        Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top;
        Transform.Height = 48;
        Style.BackColor = Look.Accent;
        Style.Border = new BorderStyle { Width = 0, Roundness = 0 };
        Style.Shadow = Look.HardShadow(0);
        Transform.ScaleY = 0;

        AddChild(Look.Label($">> LEVEL {level}", 24, 6, 400, 22, 18f, Look.Ink, TextAlign.Left, 900));
        AddChild(Look.Label("speed up  //  points up", 24, 27, 400, 16, 10f, Look.Ink.WithAlpha(170)));

        Animate()
            .ScaleY(1f, 0.12f)
            .Wait(1f)
            .ScaleY(0f, 0.18f, Ease.Linear)
            .Call(Dispose);
    }
}

/// <summary>
/// A number in the top bar. It rolls up toward its value instead of jumping, and punches (scales up
/// and flashes white) whenever the value grows.
/// </summary>
internal sealed class StatReadout : VisualElement
{
    private readonly VisualElement _value;
    private readonly SKColor _accent;
    private readonly AnimatedValue _shown;
    private Timeline? _punch;
    private int _target;

    public StatReadout(string caption, Func<int> value, SKColor accent, float width = 76)
    {
        Name = caption + "Readout";
        _accent = accent;
        Transform.Width = width;

        AddChild(Look.Label(caption, 0, 3, width, 12, 8.5f, Look.Dim, TextAlign.Left, 700));
        _value = Look.Label("0", 0, 16, width, 22, 18f, accent, TextAlign.Left, 800);
        _value.Transform.TransformOriginX = 0f;
        AddChild(_value);

        _shown = new AnimatedValue(this, duration: 0.3f);
        _shown.Changed += v => _value.Text = ((int)MathF.Round(v)).ToString();

        this.Bind(_ =>
        {
            int next = value();
            if (next > _target)
            {
                _shown.Target = next;
                Punch();
            }
            else if (next < _target)
            {
                _shown.Snap(next); // Resets snap instead of rolling down.
            }
            _target = next;
        });
    }

    private void Punch()
    {
        _punch?.Stop();
        _value.Transform.ScaleX = _value.Transform.ScaleY = 1.35f;
        _value.Style.Text.Color = SKColors.White;
        _punch = _value.Animate().Scale(1f, 0.25f).With().TextColor(_accent, 0.25f);
    }
}

/// <summary>A number that counts up from zero when it appears (the game-over score).</summary>
internal sealed class CountUp : VisualElement
{
    public CountUp(int target)
    {
        Text = "0";
        Animate().Tween(0, target, 0.8f, v => Text = ((int)MathF.Round(v)).ToString());
    }
}

/// <summary>A tilted rubber stamp that slams down after <c>delay</c> seconds.</summary>
internal sealed class Stamp : VisualElement
{
    public Stamp(string text, SKColor color, float delay)
    {
        Text = text;
        Style.Text.Font = Look.Mono;
        Style.Text.Size = 12f;
        Style.Text.Weight = 900;
        Style.Text.Color = Look.Ink;
        Style.Text.Alignment = TextAlign.Center;
        Style.BackColor = color;
        Style.Border = new BorderStyle { Width = 0, Roundness = 0 };
        Style.Shadow = Look.HardShadow(4);
        Transform.RotationZ = -6f;
        Transform.ScaleX = Transform.ScaleY = 2.2f;
        Opacity = 0;

        Animate().Wait(delay).Fade(1f, 0f).Scale(1f, 0.14f);
    }
}

/// <summary>
/// Headless <see cref="ButtonBase"/> with an animated "selected" state. Keyboard focus is off:
/// <see cref="MenuList"/> owns keyboard selection, so Enter never fires a button twice.
/// </summary>
internal abstract class Selectable : ButtonBase
{
    private readonly AnimatedValue _progress;

    protected Selectable()
    {
        ReceivesKeyboard = false;
        Style.Border = new BorderStyle { Width = 0, Roundness = 0 };
        _progress = new AnimatedValue(this, duration: 0.09f);
        _progress.Changed += _ => Restyle();
    }

    /// <summary>0 idle to 1 selected, already eased. ButtonBase can restyle before the constructor sets it up.</summary>
    protected float Progress => _progress?.Value ?? 0f;

    public bool Selected
    {
        get => _progress.Target > 0.5f;
        set => _progress.Target = value ? 1f : 0f;
    }

    protected override void OnStateChanged()
    {
        base.OnStateChanged();
        Restyle();
    }

    protected abstract void Restyle();
}

/// <summary>A full-width menu row: "> LABEL ....... KEY". Selecting it inverts it and slides the label.</summary>
internal sealed class MenuItem : Selectable
{
    private readonly SKColor _accent;
    private readonly VisualElement _marker;
    private readonly VisualElement _label;
    private readonly VisualElement _key;

    public MenuItem(string label, string key, float width, float height, SKColor accent)
    {
        _accent = accent;
        Transform.Width = width;
        Transform.Height = height;

        float textY = (height - 20) / 2;
        _marker = Look.Label(">", 10, textY, 16, 20, 13f, Look.Ink, TextAlign.Left, 900);
        _label = Look.Label(label, 12, textY, width - 120, 20, 13f, Look.Text, TextAlign.Left, 700);
        _key = Look.Label(key, width - 100, textY, 88, 20, 10f, Look.Dim, TextAlign.Right, 700);
        AddChild(_marker);
        AddChild(_label);
        AddChild(_key);
        Restyle();
    }

    protected override void Restyle()
    {
        if (_marker == null)
            return; // ButtonBase can raise state changes before this constructor has run.

        float p = Progress;
        var fill = IsPressed ? Anim.Lerp(_accent, Look.Ink, 0.3f) : _accent;
        Style.BackColor = fill.WithAlpha((byte)(255 * p));
        _marker.Opacity = p;
        _label.Transform.LocalX = 12 + 16 * p;
        _label.Style.Text.Color = Anim.Lerp(Look.Text, Look.Ink, p);
        _key.Style.Text.Color = Anim.Lerp(Look.Dim, Look.Ink.WithAlpha(180), p);
    }
}

/// <summary>One option of the difficulty picker: a bracketed cell that fills in when chosen.</summary>
internal sealed class SegmentButton : Selectable
{
    public SegmentButton(string label)
    {
        Text = label;
        Style.Text.Font = Look.Mono;
        Style.Text.Size = 11f;
        Style.Text.Weight = 700;
        Style.Text.Alignment = TextAlign.Center;
        Restyle();
        Events.OnMouseEnter += _ => Restyle();
        Events.OnMouseLeave += _ => Restyle();
    }

    protected override void Restyle()
    {
        float p = Progress;
        var idle = IsHovered ? Look.Line : Look.Panel;
        Style.BackColor = Anim.Lerp(idle, Look.Accent, p);
        Style.Border = new BorderStyle { Width = 1, Color = Anim.Lerp(Look.Line, Look.Accent, p), Roundness = 0 };
        Style.Text.Color = Anim.Lerp(Look.Dim, Look.Ink, p);
    }
}

/// <summary>
/// The rows of one dialog. Hovering a row selects it, Up/Down moves the selection, and Enter
/// activates it, so mouse and keyboard drive the same state.
/// </summary>
internal sealed class MenuList
{
    private readonly List<(MenuItem Item, Action Action)> _items = new();
    private int _index;

    public MenuItem Add(MenuItem item, Action action)
    {
        int i = _items.Count;
        _items.Add((item, action));
        item.Clicked += action;
        item.Events.OnMouseEnter += _ => Select(i);
        item.Selected = i == 0;
        return item;
    }

    public void Move(int delta)
    {
        if (_items.Count > 0)
            Select((_index + delta + _items.Count) % _items.Count);
    }

    public void Activate()
    {
        if (_items.Count > 0)
            _items[_index].Action();
    }

    private void Select(int index)
    {
        _index = index;
        for (int i = 0; i < _items.Count; i++)
            _items[i].Item.Selected = i == index;
    }
}
