using System;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Animations;

/// <summary>
/// A fluent, element-bound sequence of steps. Each step starts when the previous one ends;
/// <see cref="With"/> starts the next step together with the previous one instead, and
/// <see cref="At"/> places it at an exact time. Property steps tween from wherever the property is
/// when the step starts, and their ease defaults to <see cref="Ease.OutCubic"/>.
/// <para>
/// <c>element.Animate()</c> creates one that is already playing on the element; add every step in
/// the same call chain. A timeline is also an <see cref="Animation"/>, so one built with
/// <c>new Timeline(element)</c> can be nested in another or played later.
/// </para>
/// </summary>
/// <example>
/// <code>
/// // Slam a banner in, hold it, collapse it, then remove it.
/// banner.Animate()
///     .ScaleY(1, 0.12f, Ease.OutBack)
///     .With().Fade(1, 0.08f)
///     .Wait(1f)
///     .ScaleY(0, 0.18f, Ease.InCubic)
///     .Call(banner.Dispose);
/// </code>
/// </example>
public sealed class Timeline : Animation
{
    private readonly VisualElement? _target;
    private readonly TrackAnimation _track = new();
    private Animation _root;
    private float _end;       // Where the next step starts by default.
    private float _lastStart; // Where the previous step started, for With().
    private float? _nextStart;

    /// <param name="target">The element the property shortcuts animate. Optional when only generic steps are used.</param>
    public Timeline(VisualElement? target = null)
    {
        _target = target;
        _root = _track;
    }

    /// <summary>The playback when created by <c>element.Animate()</c>.</summary>
    public Playback? Playback { get; internal set; }

    public override float Duration => _root.Duration;
    protected internal override void Seek(float time) => _root.Seek(time);
    protected internal override void Begin() => _root.Begin();
    protected internal override void Rewind() => _root.Rewind();

    // ── Placement ──────────────────────────────────────────────────────────

    /// <summary>Appends any animation as the next step.</summary>
    public Timeline Add(Animation step)
    {
        ArgumentNullException.ThrowIfNull(step);
        float start = _nextStart ?? _end;
        _nextStart = null;
        _track.Add(start, step);
        _lastStart = start;
        _end = Math.Max(_end, start + step.Duration);
        return this;
    }

    /// <summary>The next step starts at the same time as the previous one.</summary>
    public Timeline With()
    {
        _nextStart = _lastStart;
        return this;
    }

    /// <summary>The next step starts <paramref name="seconds"/> into the timeline.</summary>
    public Timeline At(float seconds)
    {
        if (float.IsNaN(seconds) || seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        _nextStart = seconds;
        return this;
    }

    /// <summary>Plays the whole timeline <paramref name="count"/> times (-1 forever); <paramref name="yoyo"/> runs every other loop backwards.</summary>
    public new Timeline Repeat(int count = -1, bool yoyo = false)
    {
        _root = new RepeatAnimation(_track, count, yoyo);
        return this;
    }

    // ── Generic steps ──────────────────────────────────────────────────────

    public Timeline Wait(float seconds) => Add(Anim.Wait(seconds));

    /// <summary>Runs <paramref name="action"/> when reached, e.g. <c>.Call(element.Dispose)</c> at the end.</summary>
    public Timeline Call(Action action) => Add(Anim.Call(action));

    /// <inheritdoc cref="Anim.Tween(float, Action{float}, Func{float, float}?)"/>
    public Timeline Tween(float duration, Action<float> apply, Func<float, float>? ease = null) =>
        Add(Anim.Tween(duration, apply, ease));

    /// <inheritdoc cref="Anim.Tween(float, float, float, Action{float}, Func{float, float}?)"/>
    public Timeline Tween(float from, float to, float duration, Action<float> set, Func<float, float>? ease = null) =>
        Add(Anim.Tween(from, to, duration, set, ease));

    // ── Element properties ─────────────────────────────────────────────────

    public Timeline Fade(float to, float duration, Func<float, float>? ease = null)
    {
        var e = Target;
        return Add(Anim.To(() => e.Opacity, v => e.Opacity = v, to, duration, ease));
    }

    /// <summary>Scales both axes to <paramref name="to"/>.</summary>
    public Timeline Scale(float to, float duration, Func<float, float>? ease = null) =>
        ScaleX(to, duration, ease).With().ScaleY(to, duration, ease);

    public Timeline ScaleX(float to, float duration, Func<float, float>? ease = null)
    {
        var t = Target.Transform;
        return Add(Anim.To(() => t.ScaleX, v => t.ScaleX = v, to, duration, ease));
    }

    public Timeline ScaleY(float to, float duration, Func<float, float>? ease = null)
    {
        var t = Target.Transform;
        return Add(Anim.To(() => t.ScaleY, v => t.ScaleY = v, to, duration, ease));
    }

    /// <summary>Moves to <c>Transform.LocalX/LocalY</c> = (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public Timeline MoveTo(float x, float y, float duration, Func<float, float>? ease = null)
    {
        var t = Target.Transform;
        return Add(Anim.To(() => t.LocalX, v => t.LocalX = v, x, duration, ease))
            .With().Add(Anim.To(() => t.LocalY, v => t.LocalY = v, y, duration, ease));
    }

    /// <summary>Moves by an offset from wherever the element is when the step starts.</summary>
    public Timeline MoveBy(float dx, float dy, float duration, Func<float, float>? ease = null)
    {
        var t = Target.Transform;
        float x0 = 0, y0 = 0;
        return Add(new TweenAnimation(duration, ease,
            p => { t.LocalX = x0 + dx * p; t.LocalY = y0 + dy * p; },
            () => { x0 = t.LocalX; y0 = t.LocalY; }));
    }

    /// <summary>Rotates to <paramref name="degrees"/> around Z (<c>Transform.RotationZ</c>).</summary>
    public Timeline Rotate(float degrees, float duration, Func<float, float>? ease = null)
    {
        var t = Target.Transform;
        return Add(Anim.To(() => t.RotationZ, v => t.RotationZ = v, degrees, duration, ease));
    }

    public Timeline BackColor(SKColor to, float duration, Func<float, float>? ease = null)
    {
        var e = Target;
        return Add(Anim.To(() => e.Style.BackColor, v => e.Style.BackColor = v, to, duration, ease));
    }

    public Timeline TextColor(SKColor to, float duration, Func<float, float>? ease = null)
    {
        var e = Target;
        return Add(Anim.To(() => e.Style.Text.Color, v => e.Style.Text.Color = v, to, duration, ease));
    }

    // ── Playback ───────────────────────────────────────────────────────────

    /// <summary>Runs <paramref name="action"/> when the timeline ends (not when stopped). Needs <c>element.Animate()</c>.</summary>
    public Timeline OnComplete(Action action)
    {
        if (Playback == null)
            throw new InvalidOperationException("OnComplete needs a playing timeline. Use element.Animate(), or .Call(action) as the last step.");
        Playback.OnComplete(action);
        return this;
    }

    /// <summary>Stops the playback started by <c>element.Animate()</c>, leaving properties where they are.</summary>
    public void Stop() => Playback?.Stop();

    private VisualElement Target =>
        _target ?? throw new InvalidOperationException("This step animates an element property. Create the timeline with element.Animate() or new Timeline(element).");
}
