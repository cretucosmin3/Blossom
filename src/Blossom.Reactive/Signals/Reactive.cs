using System;
using System.Collections.Generic;
namespace Blossom.Reactive;

/// <summary>
/// Central static engine for creating and operating with reactive signals, effects, and batches.
/// </summary>
public static class ReactiveEngine
{
    /// <summary>
    /// Creates a new reactive signal initialized with the specified value.
    /// </summary>
    public static Signal<T> CreateSignal<T>(T initialValue, IEqualityComparer<T>? comparer = null)
    {
        return new Signal<T>(initialValue, comparer);
    }

    /// <summary>
    /// Creates a getter/setter tuple pair for a reactive signal, suitable for tuple deconstruction.
    /// </summary>
    public static (Func<T> Read, Action<T> Write) CreateSignalPair<T>(T initialValue, IEqualityComparer<T>? comparer = null)
    {
        var signal = new Signal<T>(initialValue, comparer);
        return (signal.Get, signal.Set);
    }

    /// <summary>
    /// Creates and immediately runs a reactive effect that automatically re-executes whenever its signal dependencies change.
    /// </summary>
    public static Effect CreateEffect(Action action)
    {
        return new Effect(action);
    }

    /// <summary>
    /// Creates a memoized derived computation that caches its result and only recomputes when dependencies change.
    /// </summary>
    public static Memo<T> CreateMemo<T>(Func<T> compute, IEqualityComparer<T>? comparer = null)
    {
        return new Memo<T>(compute, comparer);
    }

    /// <summary>
    /// Groups multiple signal writes into a single transaction so dependent effects run only once after completion.
    /// </summary>
    public static void Batch(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        ReactiveContext.EnterBatch();
        try
        {
            action();
        }
        finally
        {
            ReactiveContext.ExitBatch();
        }
    }

    /// <summary>
    /// Groups multiple signal writes into a single transaction and returns the result of the action.
    /// </summary>
    public static T Batch<T>(Func<T> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        ReactiveContext.EnterBatch();
        try
        {
            return action();
        }
        finally
        {
            ReactiveContext.ExitBatch();
        }
    }

    /// <summary>
    /// Executes an action without tracking any signal dependencies read within it.
    /// </summary>
    public static T Untrack<T>(Func<T> action) => ReactiveContext.Untrack(action);

    /// <summary>
    /// Executes an action without tracking any signal dependencies read within it.
    /// </summary>
    public static void Untrack(Action action) => ReactiveContext.Untrack(action);
}

/// <summary>
/// Convenient shorthand for reactive primitives.
/// </summary>
public static class Signals
{
    public static Signal<T> Cell<T>(T initialValue) => ReactiveEngine.CreateSignal(initialValue);
    public static Effect Effect(Action action) => ReactiveEngine.CreateEffect(action);
    public static Memo<T> Memo<T>(Func<T> compute) => ReactiveEngine.CreateMemo(compute);
    public static void Batch(Action action) => ReactiveEngine.Batch(action);
    public static T Batch<T>(Func<T> action) => ReactiveEngine.Batch(action);
}
