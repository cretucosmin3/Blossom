using System;
using Blossom.Core.Visual;

namespace Blossom.Animations;

/// <summary>
/// One animation playing on an element, returned by <see cref="VisualElement.Play"/>. The element
/// advances it on UI time (<see cref="Blossom.Core.Application.Time"/>) during each paint pass, keeps
/// frames coming while it runs, and lets the UI go idle once it ends. Disposing the element stops it.
/// </summary>
public sealed class Playback
{
    private bool _started;
    private bool _stopped;
    private float _speed = 1f;
    private Action? _completed;

    internal Playback(VisualElement owner, Animation animation)
    {
        Owner = owner;
        Animation = animation;
    }

    public VisualElement Owner { get; }
    public Animation Animation { get; }

    /// <summary>Seconds played so far, scaled by <see cref="Speed"/>.</summary>
    public double Time { get; private set; }

    /// <summary>Playback rate: 2 plays twice as fast, 0.5 at half speed. Can't be negative.</summary>
    public float Speed
    {
        get => _speed;
        set => _speed = float.IsNaN(value) ? 1f : Math.Max(0f, value);
    }

    public bool IsPaused { get; private set; }
    public bool IsComplete { get; private set; }
    public bool IsStopped => _stopped;

    /// <summary>Started and neither finished nor stopped. A paused playback is still playing.</summary>
    public bool IsPlaying => !_stopped && !IsComplete;

    /// <summary>
    /// Runs when the animation reaches its end (not when it is stopped). Safe to dispose the element
    /// here. Added after completion, it runs right away.
    /// </summary>
    public Playback OnComplete(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsComplete)
            action();
        else
            _completed += action;
        return this;
    }

    public void Pause() => IsPaused = true;

    public void Resume()
    {
        if (!IsPaused)
            return;
        IsPaused = false;
        Owner.InvalidatePaint();
    }

    /// <summary>Stops where it is. Properties keep their current values and <see cref="OnComplete"/> doesn't run.</summary>
    public void Stop() => _stopped = true;

    /// <summary>
    /// Jumps to the end now, applying the final state and running <see cref="OnComplete"/>.
    /// Does nothing for an animation that never ends.
    /// </summary>
    public void Finish()
    {
        if (!IsPlaying || float.IsPositiveInfinity(Animation.Duration))
            return;
        if (!_started)
        {
            _started = true;
            Animation.Begin();
        }
        Complete();
    }

    /// <summary>Advances by <paramref name="dt"/>; true while it needs another frame.</summary>
    internal bool Advance(float dt)
    {
        if (!IsPlaying || IsPaused)
            return false;

        // The first frame shows the starting state. Its dt is time that passed before this playback
        // was ever drawn, so it isn't played.
        if (!_started)
        {
            _started = true;
            Animation.Begin();
        }
        else
        {
            Time += dt * _speed;
        }

        if (Time >= Animation.Duration)
        {
            Complete();
            return false;
        }

        Animation.Seek((float)Time);
        return IsPlaying;
    }

    private void Complete()
    {
        float end = Animation.Duration;
        Time = end;
        Animation.Seek(end);
        if (_stopped)
            return; // A callback at the very end stopped it.
        IsComplete = true;
        var completed = _completed;
        _completed = null;
        completed?.Invoke();
    }
}
