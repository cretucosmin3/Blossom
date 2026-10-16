using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Blossom.Reactive;

/// <summary>
/// A memoized, derived computation that only re-evaluates when its upstream dependencies change,
/// and only notifies downstream subscribers when its computed output changes.
/// </summary>
/// <remarks>
/// Upstream writes only mark the memo stale. It recomputes when read (by an effect, another memo, or
/// directly), after bringing its own sources up to date, so it never sees a half-updated graph and
/// runs at most once per change however many paths lead to it.
/// <para>
/// Disposing a memo (directly or through its <see cref="EffectScope"/>) releases it rather than
/// killing it: it unsubscribes from its inputs once nothing reads it, and a later read recomputes and
/// subscribes again. A static memo that happened to be created while some component was being built
/// therefore keeps working after that component's scope is disposed.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the computed value.</typeparam>
public sealed class Memo<T> : ISource, IObserver, IDisposable
{
    private readonly Func<T> _compute;
    private readonly IEqualityComparer<T> _comparer;
    private readonly HashSet<IObserver> _observers = new();
    private readonly SourceList _sources = new();
    private T _value = default!;
    private ExceptionDispatchInfo? _error;
    private NodeState _state = NodeState.Dirty;
    private bool _hasValue;
    private bool _isReleased;
    private bool _isComputing;

    public Memo(Func<T> compute, IEqualityComparer<T>? comparer = null)
    {
        _compute = compute ?? throw new ArgumentNullException(nameof(compute));
        _comparer = comparer ?? EqualityComparer<T>.Default;

        EffectScope.Current?.Register(this);
        UpdateIfNecessary();
        _error?.Throw();
    }

    /// <summary>
    /// Gets the memoized computed value, registering the active effect as a subscriber.
    /// </summary>
    public T Value
    {
        get
        {
            // Reading itself while computing is a cycle: return the previous value untracked.
            if (_isComputing) return _value;

            UpdateIfNecessary();
            ReactiveContext.Track(this);
            _error?.Throw();
            return _value;
        }
    }

    public T Get() => Value;

    /// <summary>
    /// Reads the up-to-date value without registering a dependency with the ambient effect.
    /// </summary>
    public T Peek()
    {
        if (_isComputing) return _value;

        UpdateIfNecessary();
        _error?.Throw();
        return _value;
    }

    void ISource.UpdateIfNecessary() => UpdateIfNecessary();

    private void UpdateIfNecessary()
    {
        if (_isComputing) return;

        if (_state == NodeState.Check)
        {
            // Sources in read order: once one changes, later ones may not be read at all this time.
            for (int i = 0; i < _sources.Count; i++)
            {
                _sources[i].UpdateIfNecessary();
                if (_state == NodeState.Dirty) break;
            }
        }

        if (_state == NodeState.Dirty)
            Recompute();
        else
            _state = NodeState.Clean;
    }

    private void Recompute()
    {
        _isComputing = true;
        _state = NodeState.Clean;

        var prevObserver = ReactiveContext.SwapObserver(this);
        var prevOwner = ReactiveContext.SwapOwner(null);
        T newValue = default!;
        ExceptionDispatchInfo? error = null;
        try
        {
            newValue = _compute();
        }
        catch (Exception ex)
        {
            error = ExceptionDispatchInfo.Capture(ex);
        }
        finally
        {
            ReactiveContext.SwapObserver(prevObserver);
            ReactiveContext.SwapOwner(prevOwner);
            _sources.Commit(this);
            _isComputing = false;
        }

        bool hadValue = _hasValue || _error != null;
        bool changed;
        if (error != null)
        {
            // Keep the last good value; readers get the error until an upstream change recomputes.
            changed = true;
            _error = error;
        }
        else
        {
            changed = _error != null || !_hasValue || !_comparer.Equals(_value, newValue);
            _error = null;
            _value = newValue;
            _hasValue = true;
        }

        if (changed && hadValue)
            ReactiveContext.MarkObservers(_observers, NodeState.Dirty, this);
    }

    void IObserver.Track(ISource source) => _sources.Track(source, this);

    void IObserver.MarkStale(NodeState state, ISource from)
    {
        // While computing, a source not read yet will be read fresh; only a re-read one is stale.
        if (_isComputing && !_sources.IsTrackedThisRun(from)) return;
        if (_state >= state) return;

        bool wasClean = _state == NodeState.Clean;
        _state = state;
        if (wasClean)
        {
            foreach (var observer in _observers)
                observer.MarkStale(NodeState.Check, this);
        }
    }

    void ISource.AddObserver(IObserver observer) => _observers.Add(observer);

    void ISource.RemoveObserver(IObserver observer)
    {
        if (_observers.Remove(observer) && _isReleased && _observers.Count == 0)
            Detach();
    }

    public static implicit operator T(Memo<T> memo) => memo != null ? memo.Value : default!;

    /// <summary>
    /// Releases the memo: it stops tracking its inputs as soon as nothing reads it (right away if
    /// nothing does now). Reading it again afterwards recomputes it.
    /// </summary>
    public void Dispose()
    {
        _isReleased = true;
        if (_observers.Count == 0)
            Detach();
    }

    // Unsubscribed and stale: the next read recomputes and subscribes again.
    private void Detach()
    {
        if (_isComputing) return;
        _sources.Clear(this);
        _state = NodeState.Dirty;
    }

    public override string? ToString() => _value?.ToString();
}
