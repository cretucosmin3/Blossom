using System;
using System.Collections.Generic;

namespace Blossom.Reactive;

/// <summary>
/// A hierarchical scope that captures and disposes reactive effects and bindings.
/// </summary>
public sealed class EffectScope : IDisposable
{
    [ThreadStatic]
    private static EffectScope? _current;

    private readonly List<IDisposable> _disposables = new();
    private bool _isDisposed;

    /// <summary>
    /// Gets the ambient active effect scope on the current thread.
    /// </summary>
    public static EffectScope? Current => _current;

    /// <summary>
    /// Executes the given function inside a new EffectScope and returns both the scope and the result.
    /// </summary>
    public static (EffectScope Scope, T Result) Record<T>(Func<T> factory)
    {
        var scope = new EffectScope();
        var prev = _current;
        _current = scope;
        try
        {
            var result = factory();
            return (scope, result);
        }
        finally
        {
            _current = prev;
        }
    }

    /// <summary>
    /// Executes the given action inside a new EffectScope and returns the scope.
    /// </summary>
    public static EffectScope Record(Action action)
    {
        var scope = new EffectScope();
        var prev = _current;
        _current = scope;
        try
        {
            action();
            return scope;
        }
        finally
        {
            _current = prev;
        }
    }

    /// <summary>
    /// Registers an IDisposable item with this scope.
    /// </summary>
    public void Register(IDisposable disposable)
    {
        if (_isDisposed)
        {
            disposable.Dispose();
            return;
        }
        _disposables.Add(disposable);
    }

    /// <summary>
    /// Disposes all registered effects and subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        var items = _disposables.ToArray();
        _disposables.Clear();
        foreach (var item in items)
        {
            item.Dispose();
        }
    }
}
