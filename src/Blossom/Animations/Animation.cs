using System;
using System.Collections.Generic;

namespace Blossom.Animations;

/// <summary>
/// A piece of motion over time. Every animation is seekable: it applies the state for a local time
/// rather than stepping by frames, so composites can place, loop, mirror, and re-time their children,
/// and a playback lands in the same place however large or small the frame steps are.
/// <para>
/// Build animations with <see cref="Anim"/> or an element's <see cref="Timeline"/>
/// (<c>element.Animate()</c>), and play them with <c>VisualElement.Play</c>. Subclass this for
/// anything the built-ins don't cover: implement <see cref="Duration"/> and <see cref="Seek"/>.
/// </para>
/// An instance holds the state of the play-through it is in (captured start values, fired callbacks),
/// so play one instance in one place at a time.
/// </summary>
public abstract class Animation
{
    /// <summary>Length in seconds. <see cref="float.PositiveInfinity"/> for an animation that never ends.</summary>
    public abstract float Duration { get; }

    /// <summary>
    /// Applies the state at <paramref name="time"/>, between 0 and <see cref="Duration"/>. Time moves
    /// forward from one call to the next, except inside a yoyo, where it runs back.
    /// </summary>
    protected internal abstract void Seek(float time);

    /// <summary>
    /// A new play-through is starting. Forget everything the last one captured, such as the start value
    /// of a "to" tween. The default calls <see cref="Rewind"/>.
    /// </summary>
    protected internal virtual void Begin() => Rewind();

    /// <summary>
    /// The next loop of a repeat is starting. Re-arm per-loop state such as callbacks, but keep captured
    /// start values, so every loop plays the same motion.
    /// </summary>
    protected internal virtual void Rewind() { }

    /// <summary>Plays this <paramref name="count"/> times (-1 forever); <paramref name="yoyo"/> runs every other loop backwards.</summary>
    public Animation Repeat(int count = -1, bool yoyo = false) => new RepeatAnimation(this, count, yoyo);

    /// <summary>Waits <paramref name="seconds"/> before this starts.</summary>
    public Animation Delay(float seconds) => Anim.Sequence(Anim.Wait(seconds), this);

    /// <summary>Re-times the whole animation through <paramref name="ease"/> (on top of any eases inside it).</summary>
    public Animation Eased(Func<float, float> ease) => new EasedAnimation(this, ease);

    /// <summary>This, then <paramref name="next"/> in order.</summary>
    public Animation Then(params Animation[] next)
    {
        var steps = new Animation[next.Length + 1];
        steps[0] = this;
        next.CopyTo(steps, 1);
        return Anim.Sequence(steps);
    }

    /// <summary>This and <paramref name="others"/> at the same time.</summary>
    public Animation With(params Animation[] others)
    {
        var tracks = new Animation[others.Length + 1];
        tracks[0] = this;
        others.CopyTo(tracks, 1);
        return Anim.Parallel(tracks);
    }
}

/// <summary>Drives a value over a fixed time through an ease. The base of every property tween.</summary>
internal sealed class TweenAnimation : Animation
{
    private readonly float _duration;
    private readonly Func<float, float> _ease;
    private readonly Action<float> _apply;
    private readonly Action? _capture;
    private bool _captured;

    /// <param name="apply">Receives eased progress (0..1, beyond it for overshooting eases).</param>
    /// <param name="capture">Runs once per play-through, on the first seek: records "from" values.</param>
    public TweenAnimation(float duration, Func<float, float>? ease, Action<float> apply, Action? capture = null)
    {
        if (float.IsNaN(duration) || duration < 0 || float.IsInfinity(duration))
            throw new ArgumentOutOfRangeException(nameof(duration), "A tween needs a finite, non-negative duration.");
        _duration = duration;
        _ease = ease ?? Blossom.Animations.Ease.OutCubic;
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        _capture = capture;
    }

    public override float Duration => _duration;

    protected internal override void Seek(float time)
    {
        if (!_captured)
        {
            _captured = true;
            _capture?.Invoke();
        }

        float p = _duration <= 0f ? 1f : Math.Clamp(time / _duration, 0f, 1f);
        _apply(_ease(p));
    }

    protected internal override void Begin() => _captured = false;
}

internal sealed class WaitAnimation : Animation
{
    private readonly float _duration;

    public WaitAnimation(float duration)
    {
        if (float.IsNaN(duration) || duration < 0)
            throw new ArgumentOutOfRangeException(nameof(duration));
        _duration = duration;
    }

    public override float Duration => _duration;
    protected internal override void Seek(float time) { }
}

/// <summary>Runs an action once when reached. Not run again when a yoyo passes back over it.</summary>
internal sealed class CallAnimation : Animation
{
    private readonly Action _action;
    private bool _fired;

    public CallAnimation(Action action) => _action = action ?? throw new ArgumentNullException(nameof(action));

    public override float Duration => 0f;

    protected internal override void Seek(float time)
    {
        if (_fired)
            return;
        _fired = true;
        _action();
    }

    protected internal override void Rewind() => _fired = false;
}

/// <summary>Never ends; hands the elapsed time to a callback every frame (blinks, pulses, idle bobbing).</summary>
internal sealed class ForeverAnimation : Animation
{
    private readonly Action<float> _update;

    public ForeverAnimation(Action<float> update) => _update = update ?? throw new ArgumentNullException(nameof(update));

    public override float Duration => float.PositiveInfinity;
    protected internal override void Seek(float time) => _update(time);
}

/// <summary>
/// Children placed at start times, possibly overlapping. A child is first applied when the time
/// reaches its start, and then only while its local time changes, so a finished child stops writing
/// and a later child animating the same property takes over.
/// </summary>
internal sealed class TrackAnimation : Animation
{
    private sealed class Entry
    {
        public required float Start;
        public required Animation Animation;
        public float Last = float.NaN; // Local time last applied; NaN until the child is reached.
    }

    private readonly List<Entry> _entries = new();
    private float _lastTime = float.NaN;

    public void Add(float start, Animation animation)
    {
        ArgumentNullException.ThrowIfNull(animation);
        if (float.IsNaN(start) || start < 0)
            throw new ArgumentOutOfRangeException(nameof(start));
        _entries.Add(new Entry { Start = start, Animation = animation });
    }

    public override float Duration
    {
        get
        {
            float end = 0f;
            foreach (var e in _entries)
                end = Math.Max(end, e.Start + e.Animation.Duration);
            return end;
        }
    }

    protected internal override void Seek(float time)
    {
        bool backward = time < _lastTime;
        _lastTime = time;

        // Later entries win when they animate the same property, so they apply last. Running
        // backwards (a yoyo), the earlier entry is the one in charge, so the order flips.
        if (!backward)
        {
            for (int i = 0; i < _entries.Count; i++)
                Apply(_entries[i], time);
        }
        else
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
                Apply(_entries[i], time);
        }
    }

    private static void Apply(Entry e, float time)
    {
        if (float.IsNaN(e.Last) && time < e.Start)
            return; // Not reached yet: don't capture start values early.

        float local = Math.Clamp(time - e.Start, 0f, e.Animation.Duration);
        if (local == e.Last)
            return;
        e.Last = local;
        e.Animation.Seek(local);
    }

    protected internal override void Begin()
    {
        _lastTime = float.NaN;
        foreach (var e in _entries)
        {
            e.Last = float.NaN;
            e.Animation.Begin();
        }
    }

    protected internal override void Rewind()
    {
        _lastTime = float.NaN;
        foreach (var e in _entries)
        {
            e.Last = float.NaN;
            e.Animation.Rewind();
        }
    }
}

internal sealed class RepeatAnimation : Animation
{
    private readonly Animation _child;
    private readonly int _count;
    private readonly bool _yoyo;
    private int _loop = -1;

    public RepeatAnimation(Animation child, int count, bool yoyo)
    {
        _child = child ?? throw new ArgumentNullException(nameof(child));
        if (count == 0 || count < -1)
            throw new ArgumentOutOfRangeException(nameof(count), "Use a positive count, or -1 to repeat forever.");
        _count = count;
        _yoyo = yoyo;
    }

    public override float Duration
    {
        get
        {
            float d = _child.Duration;
            if (_count < 0)
                return d > 0 ? float.PositiveInfinity : 0f;
            return d * _count;
        }
    }

    protected internal override void Seek(float time)
    {
        float d = _child.Duration;
        if (float.IsPositiveInfinity(d) || d <= 0f)
        {
            _child.Seek(float.IsPositiveInfinity(d) ? time : 0f); // Nothing to loop.
            return;
        }

        int loop = (int)MathF.Floor(time / d);
        if (_count > 0)
            loop = Math.Min(loop, _count - 1); // The very end belongs to the last loop.
        float local = Math.Clamp(time - loop * d, 0f, d);

        if (loop != _loop)
        {
            if (_loop >= 0)
                _child.Seek(IsMirrored(_loop) ? 0f : d); // Finish the loop that was playing.

            // A yoyo's backward half replays the forward one in reverse, so only forward loops re-arm.
            if (_loop >= 0 && !IsMirrored(loop))
                _child.Rewind();
            _loop = loop;
        }

        _child.Seek(IsMirrored(loop) ? d - local : local);
    }

    private bool IsMirrored(int loop) => _yoyo && (loop & 1) == 1;

    protected internal override void Begin()
    {
        _loop = -1;
        _child.Begin();
    }

    protected internal override void Rewind()
    {
        _loop = -1;
        _child.Rewind();
    }
}

internal sealed class EasedAnimation : Animation
{
    private readonly Animation _child;
    private readonly Func<float, float> _ease;

    public EasedAnimation(Animation child, Func<float, float> ease)
    {
        _child = child ?? throw new ArgumentNullException(nameof(child));
        _ease = ease ?? throw new ArgumentNullException(nameof(ease));
    }

    public override float Duration => _child.Duration;

    protected internal override void Seek(float time)
    {
        float d = _child.Duration;
        if (float.IsPositiveInfinity(d) || d <= 0f)
        {
            _child.Seek(time);
            return;
        }
        _child.Seek(Math.Clamp(d * _ease(time / d), 0f, d));
    }

    protected internal override void Begin() => _child.Begin();
    protected internal override void Rewind() => _child.Rewind();
}
