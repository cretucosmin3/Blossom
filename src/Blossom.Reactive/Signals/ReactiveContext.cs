using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Blossom.Reactive;

/// <summary>
/// Manages thread-local execution context for reactive dependency tracking and batching.
/// </summary>
public static class ReactiveContext
{
    // The effect or memo currently tracking reads (null when untracked).
    [ThreadStatic]
    private static IObserver? _currentObserver;

    // The effect that owns effects created right now (null inside EffectScope.Record or a memo).
    [ThreadStatic]
    private static Effect? _currentOwner;

    [ThreadStatic]
    private static int _batchDepth;

    [ThreadStatic]
    private static int _notifyDepth;

    [ThreadStatic]
    private static int _executionDepth;

    [ThreadStatic]
    private static bool _isFlushing;

    [ThreadStatic]
    private static List<Effect>? _pendingQueue;

    [ThreadStatic]
    private static int _flushGeneration;

    private const int MaxRunsPerEffect = 10000;

    /// <summary>
    /// Gets the effect currently executing and tracking dependencies on this thread
    /// (null while untracked or while a memo computes).
    /// </summary>
    public static Effect? CurrentEffect => _currentObserver as Effect;

    /// <summary>
    /// Indicates whether state mutations are currently grouped in a batch.
    /// </summary>
    public static bool IsBatching => _batchDepth > 0;

    internal static bool IsFlushing => _isFlushing;

    internal static void Track(ISource source)
    {
        var observer = _currentObserver;
        if (observer != null && !ReferenceEquals(observer, source))
            observer.Track(source);
    }

    internal static IObserver? SwapObserver(IObserver? observer)
    {
        var prev = _currentObserver;
        _currentObserver = observer;
        return prev;
    }

    internal static Effect? SwapOwner(Effect? owner)
    {
        var prev = _currentOwner;
        _currentOwner = owner;
        return prev;
    }

    /// <summary>
    /// Attaches a new effect to the running effect, or else to the ambient <see cref="EffectScope"/>.
    /// </summary>
    internal static void RegisterWithOwner(IDisposable disposable)
    {
        if (_currentOwner != null)
            _currentOwner.Own(disposable);
        else
            EffectScope.Current?.Register(disposable);
    }

    /// <summary>
    /// Hands a flow controller (For / Show) to the running effect, so a re-run of that effect removes the
    /// controller's elements instead of leaving them in the tree with only their inner effect disposed.
    /// Outside an effect the parent element's lifecycle alone owns it.
    /// </summary>
    internal static void OwnByCurrentEffect(IDisposable disposable) => _currentOwner?.Own(disposable);

    /// <summary>
    /// Marks every observer of <paramref name="from"/> stale. Marking never runs user code, so the
    /// set is iterated in place; queued effects run once the outermost notify/batch/execution ends.
    /// </summary>
    internal static void MarkObservers(HashSet<IObserver> observers, NodeState state, ISource from)
    {
        if (observers.Count == 0) return;

        BeginNotify();
        try
        {
            foreach (var observer in observers)
                observer.MarkStale(state, from);
        }
        finally
        {
            EndNotify();
        }
    }

    internal static void EnterBatch()
    {
        _batchDepth++;
        EnsureQueues();
    }

    internal static void ExitBatch()
    {
        if (_batchDepth <= 0) return;
        _batchDepth--;

        if (_batchDepth == 0 && _notifyDepth == 0 && _executionDepth == 0)
        {
            Flush();
        }
    }

    internal static void BeginNotify()
    {
        _notifyDepth++;
        EnsureQueues();
    }

    internal static void EndNotify()
    {
        if (_notifyDepth <= 0) return;
        _notifyDepth--;

        if (_notifyDepth == 0 && _batchDepth == 0 && _executionDepth == 0)
        {
            Flush();
        }
    }

    internal static void EnterExecution()
    {
        _executionDepth++;
    }

    internal static void ExitExecution()
    {
        if (_executionDepth <= 0) return;
        _executionDepth--;

        if (_executionDepth == 0 && _batchDepth == 0 && _notifyDepth == 0)
        {
            Flush();
        }
    }

    /// <summary>
    /// Queues an effect that just left the clean state; its node state keeps it from being queued twice.
    /// </summary>
    internal static void EnqueuePendingEffect(Effect effect)
    {
        EnsureQueues();
        _pendingQueue!.Add(effect);
    }

    internal static void Flush()
    {
        if (_isFlushing || _batchDepth > 0 || _notifyDepth > 0) return;
        EnsureQueues();
        if (_pendingQueue!.Count == 0) return;

        _isFlushing = true;
        int generation = ++_flushGeneration;
        List<Exception>? errors = null;
        try
        {
            int index = 0;
            while (index < _pendingQueue!.Count)
            {
                var effect = _pendingQueue[index++];

                // Only a single effect re-running without settling is a cycle; many distinct
                // effects (e.g. every binding on a theme signal) are one legitimate flush.
                if (effect.FlushGeneration != generation)
                {
                    effect.FlushGeneration = generation;
                    effect.FlushRuns = 0;
                }
                if (++effect.FlushRuns > MaxRunsPerEffect)
                {
                    // Stop re-running the cyclic effect, but leave it able to react to later writes.
                    effect.AbandonPending();
                    if (effect.FlushRuns == MaxRunsPerEffect + 1)
                    {
                        (errors ??= new List<Exception>()).Add(new InvalidOperationException(
                            "Reactive update cycle exceeded the maximum iteration count. Check for cyclic effect writes."));
                    }
                    continue;
                }

                // One failing effect must not cancel the rest of the queue; rethrow after it drains.
                try
                {
                    effect.RunIfStale();
                }
                catch (Exception ex)
                {
                    (errors ??= new List<Exception>()).Add(ex);
                }
            }
        }
        finally
        {
            _pendingQueue!.Clear();
            _isFlushing = false;
        }

        if (errors == null) return;
        if (errors.Count == 1) ExceptionDispatchInfo.Capture(errors[0]).Throw();
        throw new AggregateException("One or more reactive effects threw during a flush.", errors);
    }

    /// <summary>
    /// Executes the specified action without tracking any signal dependencies read within it.
    /// </summary>
    public static T Untrack<T>(Func<T> action)
    {
        var prev = SwapObserver(null);
        try
        {
            return action();
        }
        finally
        {
            _currentObserver = prev;
        }
    }

    /// <summary>
    /// Executes the specified action without tracking any signal dependencies read within it.
    /// </summary>
    public static void Untrack(Action action)
    {
        var prev = SwapObserver(null);
        try
        {
            action();
        }
        finally
        {
            _currentObserver = prev;
        }
    }

    private static void EnsureQueues()
    {
        _pendingQueue ??= new List<Effect>();
    }
}
