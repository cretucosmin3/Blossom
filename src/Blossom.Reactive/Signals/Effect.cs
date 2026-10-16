using System;
using System.Collections.Generic;

namespace Blossom.Reactive;

/// <summary>
/// A reactive computation that re-executes whenever any of its read signal dependencies change.
/// </summary>
/// <remarks>
/// Effects created while this effect runs belong to it: they are disposed before each re-run and when
/// this effect is disposed. An <see cref="EffectScope.Record(Action)"/> inside the run takes them instead.
/// </remarks>
public sealed class Effect : IDisposable, IObserver
{
    private readonly Action _action;
    private readonly SourceList _sources = new();
    private List<IDisposable>? _owned;
    private NodeState _state;
    private bool _isDisposed;
    private bool _isRunning;

    // Per-flush run counter for cycle detection, owned by ReactiveContext.Flush.
    internal int FlushGeneration;
    internal int FlushRuns;

    public Effect(Action action, bool autoExecute = true)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        ReactiveContext.RegisterWithOwner(this);

        if (autoExecute)
        {
            Execute();
        }
    }

    /// <summary>
    /// Called by the flush for a queued effect: re-runs only if a source actually changed.
    /// </summary>
    internal void RunIfStale()
    {
        if (_isDisposed) return;

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
            Execute();
        else
            _state = NodeState.Clean;
    }

    /// <summary>
    /// Executes the effect, re-tracking all dependencies.
    /// </summary>
    public void Execute()
    {
        if (_isDisposed) return;

        if (_isRunning)
        {
            Invalidate(NodeState.Dirty);
            return;
        }

        _isRunning = true;
        _state = NodeState.Clean;
        ReactiveContext.EnterExecution();
        var prevObserver = ReactiveContext.SwapObserver(null);
        var prevOwner = ReactiveContext.SwapOwner(null);
        try
        {
            DisposeOwned();

            ReactiveContext.SwapObserver(this);
            ReactiveContext.SwapOwner(this);
            _action();
        }
        finally
        {
            ReactiveContext.SwapObserver(prevObserver);
            ReactiveContext.SwapOwner(prevOwner);
            if (_isDisposed)
                _sources.Clear(this);
            else
                _sources.Commit(this);
            _isRunning = false;
            ReactiveContext.ExitExecution();
        }
    }

    void IObserver.Track(ISource source) => _sources.Track(source, this);

    void IObserver.MarkStale(NodeState state, ISource from)
    {
        if (_isDisposed) return;
        // While running, a source not read yet will be read fresh; only a re-read one is stale
        // (e.g. the effect wrote a signal it had already read, so it must run again).
        if (_isRunning && !_sources.IsTrackedThisRun(from)) return;
        Invalidate(state);
    }

    private void Invalidate(NodeState state)
    {
        if (_state >= state) return;

        bool wasClean = _state == NodeState.Clean;
        _state = state;
        if (wasClean)
            ReactiveContext.EnqueuePendingEffect(this);
    }

    /// <summary>Drops a queued run (cycle guard) without disposing the effect.</summary>
    internal void AbandonPending() => _state = NodeState.Clean;

    internal void Own(IDisposable disposable)
    {
        if (_isDisposed)
        {
            disposable.Dispose();
            return;
        }
        (_owned ??= new List<IDisposable>()).Add(disposable);
    }

    private void DisposeOwned()
    {
        var owned = _owned;
        if (owned == null || owned.Count == 0) return;

        _owned = null;
        for (int i = owned.Count - 1; i >= 0; i--)
        {
            owned[i].Dispose();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        DisposeOwned();
        _sources.Clear(this);
    }
}
