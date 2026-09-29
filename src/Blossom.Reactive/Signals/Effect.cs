using System;
using System.Collections.Generic;

namespace Blossom.Reactive;

/// <summary>
/// A reactive computation that re-executes whenever any of its read signal dependencies change.
/// </summary>
public sealed class Effect : IDisposable
{
    private readonly Action _action;
    private readonly HashSet<ISignal> _dependencies = new();
    private Action? _staleHandler;
    private bool _isDisposed;
    private bool _isRunning;

    public Effect(Action action, bool autoExecute = true)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        EffectScope.Current?.Register(this);

        if (autoExecute)
        {
            Execute();
        }
    }

    /// <summary>
    /// When set, the effect is a memo tracker: upstream changes mark the memo dirty
    /// instead of re-running a user action. Dependencies are refreshed only on recompute.
    /// </summary>
    internal void SetStaleHandler(Action handler) => _staleHandler = handler;

    /// <summary>
    /// Called by signals (and memos) when an upstream value changes.
    /// </summary>
    internal void Notify()
    {
        if (_isDisposed) return;

        if (_staleHandler != null)
        {
            _staleHandler();
            return;
        }

        ReactiveContext.EnqueuePendingEffect(this);
    }

    /// <summary>
    /// Executes the effect, re-tracking all dependencies.
    /// </summary>
    public void Execute()
    {
        if (_isDisposed) return;

        if (_isRunning)
        {
            ReactiveContext.EnqueuePendingEffect(this);
            return;
        }

        _isRunning = true;
        ReactiveContext.EnterExecution();
        try
        {
            CleanupDependencies();

            var prev = ReactiveContext.CurrentEffect;
            ReactiveContext.CurrentEffect = this;
            try
            {
                _action();
            }
            finally
            {
                ReactiveContext.CurrentEffect = prev;
            }
        }
        finally
        {
            _isRunning = false;
            ReactiveContext.ExitExecution();
        }
    }

    internal void AddDependency(ISignal signal)
    {
        if (_dependencies.Add(signal))
        {
            signal.Subscribe(this);
        }
    }

    internal void CleanupDependencies()
    {
        foreach (var dep in _dependencies)
        {
            dep.Unsubscribe(this);
        }
        _dependencies.Clear();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        CleanupDependencies();
    }
}
