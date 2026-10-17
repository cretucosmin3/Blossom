using System;
using System.Collections.Generic;

namespace Blossom.Reactive;

/// <summary>
/// A reactive state cell containing a value that notifies tracking effects when modified.
/// </summary>
/// <typeparam name="T">The type of value held by the signal.</typeparam>
public class Signal<T> : ISource
{
    private T _value;
    private readonly HashSet<IObserver> _observers = new();
    private readonly IEqualityComparer<T> _comparer;

    public Signal(T initialValue, IEqualityComparer<T>? comparer = null)
    {
        _value = initialValue;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>
    /// Gets or sets the value of the signal. Reading registers the active effect as a subscriber.
    /// Writing notifies all subscriber effects if the value has changed.
    /// </summary>
    public T Value
    {
        get
        {
            ReactiveContext.Track(this);
            return _value;
        }
        set
        {
            if (!_comparer.Equals(_value, value))
            {
                _value = value;
                ReactiveContext.MarkObservers(_observers, NodeState.Dirty, this);
            }
        }
    }

    /// <summary>
    /// Reads the signal value and registers the dependency with the active effect.
    /// </summary>
    public T Get() => Value;

    /// <summary>
    /// Updates the signal value, notifying subscribers if changed.
    /// </summary>
    public void Set(T value) => Value = value;

    /// <summary>
    /// Updates the signal value by applying a transformation function to its current value.
    /// </summary>
    public void Update(Func<T, T> updateFunc)
    {
        if (updateFunc == null) throw new ArgumentNullException(nameof(updateFunc));
        Value = updateFunc(_value);
    }

    /// <summary>
    /// Reads the value without registering a dependency with the ambient effect.
    /// </summary>
    public T Peek() => _value;

    void ISource.AddObserver(IObserver observer) => _observers.Add(observer);

    void ISource.RemoveObserver(IObserver observer) => _observers.Remove(observer);

    void ISource.UpdateIfNecessary() { }

    public static implicit operator T(Signal<T> signal) => signal != null ? signal.Value : default!;

    public override string? ToString() => _value?.ToString();
}
