using System;
using System.Collections.Generic;

namespace Blossom.Reactive;

/// <summary>
/// Manages thread-local execution context for reactive dependency tracking and batching.
/// </summary>
public static class ReactiveContext
{
    [ThreadStatic]
    private static Effect? _currentEffect;

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
    private static HashSet<Effect>? _pendingSet;

    private const int MaxFlushIterations = 10000;

    /// <summary>
    /// Gets or sets the effect currently executing and tracking dependencies on this thread.
    /// </summary>
    public static Effect? CurrentEffect
    {
        get => _currentEffect;
        internal set => _currentEffect = value;
    }

    /// <summary>
    /// Indicates whether state mutations are currently grouped in a batch.
    /// </summary>
    public static bool IsBatching => _batchDepth > 0;

    internal static bool IsFlushing => _isFlushing;

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

    internal static void EnqueuePendingEffect(Effect effect)
    {
        if (effect == null) return;
        EnsureQueues();
        if (_pendingSet!.Add(effect))
        {
            _pendingQueue!.Add(effect);
        }
    }

    internal static void Flush()
    {
        if (_isFlushing || _batchDepth > 0 || _notifyDepth > 0) return;
        EnsureQueues();

        _isFlushing = true;
        try
        {
            int safety = 0;
            int index = 0;
            while (index < _pendingQueue!.Count)
            {
                if (++safety > MaxFlushIterations)
                {
                    _pendingQueue.Clear();
                    _pendingSet!.Clear();
                    throw new InvalidOperationException(
                        "Reactive update cycle exceeded the maximum iteration count. Check for cyclic effect writes.");
                }

                var effect = _pendingQueue[index++];
                _pendingSet!.Remove(effect);
                effect.Execute();
            }
        }
        finally
        {
            _pendingQueue!.Clear();
            _pendingSet!.Clear();
            _isFlushing = false;
        }
    }

    /// <summary>
    /// Executes the specified action without tracking any signal dependencies read within it.
    /// </summary>
    public static T Untrack<T>(Func<T> action)
    {
        var prev = _currentEffect;
        _currentEffect = null;
        try
        {
            return action();
        }
        finally
        {
            _currentEffect = prev;
        }
    }

    /// <summary>
    /// Executes the specified action without tracking any signal dependencies read within it.
    /// </summary>
    public static void Untrack(Action action)
    {
        var prev = _currentEffect;
        _currentEffect = null;
        try
        {
            action();
        }
        finally
        {
            _currentEffect = prev;
        }
    }

    private static void EnsureQueues()
    {
        _pendingQueue ??= new List<Effect>();
        _pendingSet ??= new HashSet<Effect>();
    }
}
