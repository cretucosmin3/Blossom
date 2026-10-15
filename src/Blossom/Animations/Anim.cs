using System;
using SkiaSharp;

namespace Blossom.Animations;

/// <summary>
/// Builds animations from plain values and callbacks, for anything an element's
/// <see cref="Timeline"/> has no shortcut for. Every ease defaults to <see cref="Ease.OutCubic"/>.
/// </summary>
/// <example>
/// <code>
/// // Count a label from 0 to the score over 0.8 s.
/// label.Play(Anim.Tween(0, score, 0.8f, v => label.Text = ((int)v).ToString()));
///
/// // Compose: shake, then pulse forever.
/// card.Play(Anim.Sequence(
///     Anim.Tween(0.3f, p => card.Transform.RotationZ = MathF.Sin(p * 30) * 6 * (1 - p), Ease.Linear),
///     Anim.Forever(t => card.Opacity = 0.75f + 0.25f * MathF.Sin(t * 4))));
/// </code>
/// </example>
public static class Anim
{
    /// <summary>Calls <paramref name="apply"/> with eased progress, 0 to 1, over <paramref name="duration"/> seconds.</summary>
    public static Animation Tween(float duration, Action<float> apply, Func<float, float>? ease = null) =>
        new TweenAnimation(duration, ease, apply);

    /// <summary>Moves a value from <paramref name="from"/> to <paramref name="to"/>, handing each step to <paramref name="set"/>.</summary>
    public static Animation Tween(float from, float to, float duration, Action<float> set, Func<float, float>? ease = null)
    {
        ArgumentNullException.ThrowIfNull(set);
        return new TweenAnimation(duration, ease, p => set(Lerp(from, to, p)));
    }

    /// <summary>
    /// Moves a value to <paramref name="to"/> from wherever it is when this tween starts, so chained
    /// "to" tweens of the same property follow on from each other.
    /// </summary>
    public static Animation To(Func<float> get, Action<float> set, float to, float duration, Func<float, float>? ease = null)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        float from = 0f;
        return new TweenAnimation(duration, ease, p => set(Lerp(from, to, p)), () => from = get());
    }

    /// <summary>Fades a color to <paramref name="to"/> from whatever it is when this tween starts.</summary>
    public static Animation To(Func<SKColor> get, Action<SKColor> set, SKColor to, float duration, Func<float, float>? ease = null)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        SKColor from = default;
        return new TweenAnimation(duration, ease, p => set(Lerp(from, to, p)), () => from = get());
    }

    /// <summary>Does nothing for <paramref name="seconds"/>.</summary>
    public static Animation Wait(float seconds) => new WaitAnimation(seconds);

    /// <summary>Runs <paramref name="action"/> once when the animation reaches this point.</summary>
    public static Animation Call(Action action) => new CallAnimation(action);

    /// <summary>Never ends: calls <paramref name="update"/> with the seconds since it started, every frame.</summary>
    public static Animation Forever(Action<float> update) => new ForeverAnimation(update);

    /// <summary>Plays <paramref name="steps"/> one after another.</summary>
    public static Animation Sequence(params Animation[] steps)
    {
        var track = new TrackAnimation();
        float at = 0f;
        foreach (var step in steps)
        {
            track.Add(at, step);
            at += step.Duration;
        }
        return track;
    }

    /// <summary>Plays <paramref name="tracks"/> together; it ends when the longest one does.</summary>
    public static Animation Parallel(params Animation[] tracks)
    {
        var track = new TrackAnimation();
        foreach (var t in tracks)
            track.Add(0f, t);
        return track;
    }

    /// <summary>Plays each of <paramref name="items"/> <paramref name="stagger"/> seconds after the previous one starts.</summary>
    public static Animation Stagger(float stagger, params Animation[] items)
    {
        var track = new TrackAnimation();
        for (int i = 0; i < items.Length; i++)
            track.Add(i * stagger, items[i]);
        return track;
    }

    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>Blends two colors per channel. <paramref name="t"/> is clamped, so overshooting eases stay valid colors.</summary>
    public static SKColor Lerp(SKColor a, SKColor b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new SKColor(
            (byte)MathF.Round(Lerp((float)a.Red, b.Red, t)),
            (byte)MathF.Round(Lerp((float)a.Green, b.Green, t)),
            (byte)MathF.Round(Lerp((float)a.Blue, b.Blue, t)),
            (byte)MathF.Round(Lerp((float)a.Alpha, b.Alpha, t)));
    }
}
