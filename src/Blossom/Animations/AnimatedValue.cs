using System;
using Blossom.Core.Visual;

namespace Blossom.Animations;

/// <summary>
/// A number that eases toward whatever <see cref="Target"/> is set to, like a CSS transition. Setting a
/// new target mid-way continues from the current value. Use it for state-driven motion: hover and
/// selection highlights, a bar filling to a new level, a score rolling up. Read <see cref="Value"/>
/// where you draw, or handle <see cref="Changed"/> to restyle.
/// </summary>
/// <example>
/// <code>
/// _highlight = new AnimatedValue(this, duration: 0.12f);
/// _highlight.Changed += v => Style.BackColor = Anim.Lerp(Idle, Accent, v);
/// Events.OnMouseEnter += _ => _highlight.Target = 1;
/// Events.OnMouseLeave += _ => _highlight.Target = 0;
/// </code>
/// </example>
public sealed class AnimatedValue
{
    private readonly VisualElement _owner;
    private Playback? _playback;
    private float _value;
    private float _from;
    private float _target;

    /// <param name="owner">The element whose paint passes advance this value.</param>
    public AnimatedValue(VisualElement owner, float value = 0f, float duration = 0.15f, Func<float, float>? ease = null)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _value = _from = _target = value;
        Duration = duration;
        Ease = ease ?? Animations.Ease.OutCubic;
    }

    public float Value => _value;

    /// <summary>Seconds to reach a new target. 0 snaps.</summary>
    public float Duration { get; set; }

    public Func<float, float> Ease { get; set; }

    /// <summary>Raised with the new value whenever it changes.</summary>
    public event Action<float>? Changed;

    public bool IsAnimating => _playback is { IsPlaying: true };

    /// <summary>The value to ease toward. Setting it starts a transition from the current value.</summary>
    public float Target
    {
        get => _target;
        set
        {
            if (value == _target)
                return;
            _target = value;
            _playback?.Stop();
            _playback = null;

            if (Duration <= 0f || _owner.IsDisposed)
            {
                Set(value);
                return;
            }

            _from = _value;
            _playback = _owner.Play(Anim.Tween(Duration, p => Set(Anim.Lerp(_from, _target, p)), Ease));
        }
    }

    /// <summary>Jumps to <paramref name="value"/> without animating.</summary>
    public void Snap(float value)
    {
        _playback?.Stop();
        _playback = null;
        _target = _from = value;
        Set(value);
    }

    private void Set(float value)
    {
        if (value == _value)
            return;
        _value = value;
        Changed?.Invoke(value);
    }
}
