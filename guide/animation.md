# Animation

`Blossom.Animations` moves element properties over time. It runs on Blossom's UI clock, so an animation lasts the same time at 30, 60, or 7000 FPS. It keeps frames coming only while something moves, and lets the UI go idle afterwards.

```csharp
using Blossom.Animations;
```

There are three levels. Pick the simplest one that fits:

| You want | Use |
|---|---|
| A one-shot effect: pop in, slide, hold, fade, remove | `element.Animate()` (a `Timeline`) |
| Motion that follows state: hover, selection, a bar filling, a score rolling | `AnimatedValue` |
| Any value, any composition, or motion the built-ins don't cover | `Anim.*` building blocks, `element.Play(...)`, or a subclass of `Animation` |

## Timelines: `element.Animate()`

`Animate()` starts a timeline on the element, playing from the next frame. Chain every step in the same statement. Each step starts when the previous one ends.

```csharp
// "+20" pops in, rises, fades out over the second half, and removes itself.
popup.Transform.ScaleX = popup.Transform.ScaleY = 1.5f;
popup.Animate()
    .MoveBy(0, -40, 0.9f)
    .With().Scale(1f, 0.12f, Ease.Linear)  // starts together with MoveBy
    .At(0.45f).Fade(0f, 0.45f)             // starts 0.45 s into the timeline
    .Call(popup.Dispose);                  // runs after everything above ends
```

| Step | Does |
|---|---|
| `Fade(to, s)` | `Opacity` |
| `Scale(to, s)`, `ScaleX`, `ScaleY` | `Transform.ScaleX/ScaleY` |
| `MoveTo(x, y, s)`, `MoveBy(dx, dy, s)` | `Transform.LocalX/LocalY` |
| `Rotate(degrees, s)` | `Transform.RotationZ` |
| `BackColor(color, s)`, `TextColor(color, s)` | `Style.BackColor`, `Style.Text.Color` |
| `Tween(s, p => ...)`, `Tween(from, to, s, v => ...)` | Anything you can set from a number |
| `Wait(s)` | A pause |
| `Call(action)` | Runs once when reached, for example `Dispose` or a sound effect |
| `Add(animation)` | Any `Animation`, including another `Timeline` |

Placement is controlled by `With()` (the next step starts with the previous one) and `At(seconds)` (the next step starts at an exact time). `Repeat(count, yoyo)` loops the whole timeline. `count` is `-1` for forever, and `yoyo` plays every other loop backwards.

Property steps tween **from wherever the property is when the step starts**, so chained steps follow on from each other. Set a start value before `Animate()` when it should differ from the current one, like the `1.5f` scale above.

Every `ease` parameter is optional and defaults to `Ease.OutCubic`.

```csharp
// A terminal cursor that blinks forever.
cursor.Animate()
    .Call(() => cursor.Opacity = 1).Wait(0.5f)
    .Call(() => cursor.Opacity = 0).Wait(0.5f)
    .Repeat();

// Slam a banner in, hold it, collapse it, remove it.
banner.Transform.ScaleY = 0;
banner.Animate()
    .ScaleY(1, 0.12f, Ease.OutBack)
    .Wait(1f)
    .ScaleY(0, 0.18f, Ease.InCubic)
    .Call(banner.Dispose);
```

## State-driven motion: `AnimatedValue`

An `AnimatedValue` eases toward whatever `Target` is set to, like a CSS transition. Setting a new target mid-way continues from the current value, with no jump. Read `Value` where you draw, or handle `Changed`.

```csharp
sealed class MenuRow : ButtonBase
{
    private readonly AnimatedValue _highlight;

    public MenuRow()
    {
        _highlight = new AnimatedValue(this, duration: 0.09f);
        _highlight.Changed += v => Style.BackColor = Anim.Lerp(Idle, Accent, v);
        Events.OnMouseEnter += _ => _highlight.Target = 1;
        Events.OnMouseLeave += _ => _highlight.Target = 0;
    }
}

// A score that rolls up instead of jumping.
_shown = new AnimatedValue(this, duration: 0.3f);
_shown.Changed += v => label.Text = ((int)v).ToString();
this.Bind(_ => _shown.Target = state.Score.Value);
```

`Snap(value)` jumps without animating, for example on a reset. With `Duration = 0`, every new target snaps.

## Building blocks: `Anim` and `Play`

`element.Play(animation)` plays any `Animation` on an element. The `Anim` class builds and composes them:

| Builder | Makes |
|---|---|
| `Anim.Tween(s, p => ...)` | Eased progress 0 to 1 |
| `Anim.Tween(from, to, s, v => ...)` | A value from `from` to `to` |
| `Anim.To(get, set, to, s)` | A float or `SKColor`, from its value when the tween starts |
| `Anim.Wait(s)`, `Anim.Call(action)` | A pause, a one-off action |
| `Anim.Forever(t => ...)` | Never ends; gets seconds since start every frame (pulses, bobbing) |
| `Anim.Sequence(...)`, `Anim.Parallel(...)`, `Anim.Stagger(gap, ...)` | Compositions |
| `a.Then(b)`, `a.With(b)`, `a.Delay(s)`, `a.Repeat(n, yoyo)`, `a.Eased(ease)` | The same, fluently |

```csharp
// Deal five cards in, 60 ms apart, then pulse the deck forever.
deck.Play(Anim.Sequence(
    Anim.Stagger(0.06f, cards.Select(c => Anim.To(() => c.Transform.LocalY, v => c.Transform.LocalY = v, 0, 0.3f, Ease.OutBack)).ToArray()),
    Anim.Forever(t => deck.Opacity = 0.8f + 0.2f * MathF.Sin(t * 4))));

// Shake: a damped sine over 0.4 s.
el.Play(Anim.Tween(0.4f, p => el.Transform.LocalX = x0 + MathF.Sin(p * 40) * 8 * (1 - p), Ease.Linear));
```

The animation can change anything: an element in another part of the tree, a field your `Paint` override reads, or a game value. The element you call `Play` on only owns the playback. The animation pauses while that element is hidden, and stops when it is disposed.

### Custom animations

Every animation is **seekable**: given a local time, it applies the state for that time. Composites only rearrange time, so anything you write nests, loops, yoyos, and re-eases like the built-ins. Implement `Duration` (`float.PositiveInfinity` for open-ended) and `Seek`:

```csharp
sealed class Typewriter : Animation
{
    private readonly VisualElement _label;
    private readonly string _text;
    public Typewriter(VisualElement label, string text) { _label = label; _text = text; }

    public override float Duration => _text.Length * 0.03f;
    protected override void Seek(float time) =>
        _label.Text = _text[..Math.Min(_text.Length, (int)(time / 0.03f))];
}

title.Play(new Typewriter(title, "GAME OVER").Then(Anim.Call(ShowButtons)));
```

Override `Begin()` to reset per-play state, such as values captured on the first `Seek`. Override `Rewind()` to re-arm per-loop state, such as one-shot callbacks.

## Controlling a playback

`Play` returns a `Playback`, and a `Timeline` exposes it as `.Playback`.

| Member | Does |
|---|---|
| `Stop()` | Stops where it is; `OnComplete` doesn't run |
| `Finish()` | Jumps to the end, applies the final state, and runs `OnComplete` |
| `Pause()` / `Resume()` | A paused playback requests no frames |
| `Speed` | 2 is twice as fast, 0.5 is half speed |
| `OnComplete(action)` | Runs at the end. Safe to `Dispose()` the element there |
| `IsPlaying`, `IsComplete`, `Time` | State |

`element.StopAnimations()` stops everything on an element. Several playbacks can run on one element. When two animate the same property, the one started last wins.

## Time

Animations and the `dt` passed to `VisualElement.OnUpdate` both use `Application.Time`. Only `UiSession.Tick(dt)` advances that clock, so a game that pauses or slows its time pauses or slows its UI too.

Each paint pass advances by the UI time since the previous pass, however many ticks happened in between. A game with VSync off can tick thousands of times per paint, and its animations still run at the right speed.

After the UI was idle, the first step is capped at one 60 Hz frame. Mid-animation, a long hitch is capped at 0.25 s.

The first frame of a new playback shows its start state, and time before that frame isn't played. So an effect spawned during a hitch still starts from its first frame.
