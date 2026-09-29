using System;
using System.Collections.Generic;

namespace Blossom.Reactive;

/// <summary>
/// A memoized, derived computation that only re-evaluates when its upstream dependencies change,
/// and only notifies downstream subscribers when its computed output changes.
/// </summary>
/// <typeparam name="T">The type of the computed value.</typeparam>
public sealed class Memo<T> : ISignal, IDisposable
{
    private readonly Func<T> _compute;
    private readonly IEqualityComparer<T> _comparer;
    private readonly HashSet<Effect> _subscribers = new();
    private Effect? _tracker;
    private T _value = default!;
    private bool _isDirty = true;
    private bool _isDisposed;
    private bool _isComputing;

    public Memo(Func<T> compute, IEqualityComparer<T>? comparer = null)
    {
        _compute = compute ?? throw new ArgumentNullException(nameof(compute));
        _comparer = comparer ?? EqualityComparer<T>.Default;

        // Tracker is subscribed to upstream signals. It must not use Effect.Execute:
        // Execute would drop dependencies before the stale handler runs.
        _tracker = new Effect(static () => { }, autoExecute: false);
        _tracker.SetStaleHandler(OnUpstreamStale);
        Recompute();
    }

    /// <summary>
    /// Gets the memoized computed value, registering the active effect as a subscriber.
    /// </summary>
    public T Value
    {
        get
        {
            ReactiveContext.CurrentEffect?.AddDependency(this);
            if (_isDirty)
            {
                Recompute();
            }
            return _value;
        }
    }

    public T Get() => Value;

    private void OnUpstreamStale()
    {
        if (_isDisposed || _isComputing) return;

        var prevValue = _value;
        Recompute();
        if (!_comparer.Equals(prevValue, _value))
        {
            NotifySubscribers();
        }
    }

    private void Recompute()
    {
        if (_isDisposed || _tracker == null || _isComputing) return;

        _isComputing = true;
        _tracker.CleanupDependencies();

        var prev = ReactiveContext.CurrentEffect;
        ReactiveContext.CurrentEffect = _tracker;
        try
        {
            _value = _compute();
            _isDirty = false;
        }
        finally
        {
            ReactiveContext.CurrentEffect = prev;
            _isComputing = false;
        }
    }

    private void NotifySubscribers()
    {
        if (_subscribers.Count == 0) return;

        var subs = new List<Effect>(_subscribers);
        ReactiveContext.BeginNotify();
        try
        {
            foreach (var sub in subs)
            {
                sub.Notify();
            }
        }
        finally
        {
            ReactiveContext.EndNotify();
        }
    }

    void ISignal.Subscribe(Effect effect) => _subscribers.Add(effect);

    void ISignal.Unsubscribe(Effect effect) => _subscribers.Remove(effect);

    public static implicit operator T(Memo<T> memo) => memo != null ? memo.Value : default!;

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _tracker?.Dispose();
        _tracker = null;
        _subscribers.Clear();
    }

    public override string? ToString() => _value?.ToString();
}
