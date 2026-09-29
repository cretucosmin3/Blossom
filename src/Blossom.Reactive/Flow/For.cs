using System;
using System.Collections.Generic;
using System.Linq;
using Blossom.Core.Visual;

namespace Blossom.Reactive;

/// <summary>
/// Provides keyed list reconciliation for dynamic collections of VisualElements.
/// </summary>
public static class For
{
    private sealed class Reconciler<T, TKey> : IDisposable where TKey : notnull
    {
        private readonly VisualElement _parent;
        private readonly Func<IEnumerable<T>> _itemsAccessor;
        private readonly Func<T, TKey> _keySelector;
        private readonly Func<T, VisualElement> _template;
        private readonly int _startIndex;
        private readonly Dictionary<TKey, (VisualElement Element, EffectScope Scope)> _nodeCache = new();
        private Effect? _effect;
        private bool _isDisposed;

        public Reconciler(
            VisualElement parent,
            Func<IEnumerable<T>> itemsAccessor,
            Func<T, TKey> keySelector,
            Func<T, VisualElement> template,
            int startIndex = 0)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _itemsAccessor = itemsAccessor ?? throw new ArgumentNullException(nameof(itemsAccessor));
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _template = template ?? throw new ArgumentNullException(nameof(template));
            _startIndex = Math.Max(0, startIndex);
        }

        public void Start()
        {
            _effect = new Effect(Reconcile);
        }

        private void Reconcile()
        {
            if (_isDisposed) return;

            var items = _itemsAccessor()?.ToList() ?? new List<T>();
            var newKeys = new List<TKey>(items.Count);
            var itemsByKey = new Dictionary<TKey, T>();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var key = _keySelector(item);
                if (!itemsByKey.TryAdd(key, item))
                {
                    continue;
                }
                newKeys.Add(key);
            }

            var keysToRemove = _nodeCache.Keys.Where(k => !itemsByKey.ContainsKey(k)).ToList();
            foreach (var key in keysToRemove)
            {
                if (_nodeCache.TryGetValue(key, out var cached))
                {
                    _parent.RemoveChild(cached.Element);
                    cached.Scope.Dispose();
                    cached.Element.Dispose();
                    _nodeCache.Remove(key);
                }
            }

            for (int i = 0; i < newKeys.Count; i++)
            {
                var key = newKeys[i];
                var item = itemsByKey[key];
                int targetIndex = _startIndex + i;

                if (!_nodeCache.TryGetValue(key, out var cached))
                {
                    var (scope, element) = EffectScope.Record(() => _template(item));
                    _nodeCache[key] = (element, scope);
                    _parent.InsertChild(targetIndex, element);
                }
                else
                {
                    int currentIndex = _parent.IndexOfChild(cached.Element);
                    if (currentIndex != targetIndex && currentIndex >= 0)
                    {
                        _parent.SetChildIndex(cached.Element, targetIndex);
                    }
                }
            }

            _parent.InvalidateLayout();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _effect?.Dispose();
            _effect = null;

            foreach (var (_, cached) in _nodeCache)
            {
                _parent.RemoveChild(cached.Element);
                cached.Scope.Dispose();
                cached.Element.Dispose();
            }
            _nodeCache.Clear();
        }
    }

    /// <summary>
    /// Reconciles children of a VisualElement with a reactive collection using key-based diffing.
    /// Preserves existing elements, reorders minimally, and only mounts/unmounts elements that changed.
    /// </summary>
    public static IDisposable Each<T, TKey>(
        VisualElement parent,
        Func<IEnumerable<T>> items,
        Func<T, TKey> keySelector,
        Func<T, VisualElement> template,
        int startIndex = 0) where TKey : notnull
    {
        var reconciler = new Reconciler<T, TKey>(parent, items, keySelector, template, startIndex);
        reconciler.Start();
        parent.BindLifecycle(reconciler);
        return reconciler;
    }

    /// <summary>
    /// Reconciles children of a VisualElement with a reactive List using key-based diffing.
    /// </summary>
    public static IDisposable Each<T, TKey>(
        VisualElement parent,
        Func<List<T>> items,
        Func<T, TKey> keySelector,
        Func<T, VisualElement> template,
        int startIndex = 0) where TKey : notnull
    {
        return Each(parent, () => (IEnumerable<T>)items(), keySelector, template, startIndex);
    }

    /// <summary>
    /// Reconciles children of a VisualElement with a reactive IReadOnlyList using key-based diffing.
    /// </summary>
    public static IDisposable Each<T, TKey>(
        VisualElement parent,
        Func<IReadOnlyList<T>> items,
        Func<T, TKey> keySelector,
        Func<T, VisualElement> template,
        int startIndex = 0) where TKey : notnull
    {
        return Each(parent, () => (IEnumerable<T>)items(), keySelector, template, startIndex);
    }
}
