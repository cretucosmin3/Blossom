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
        // Every element this list mounted, to tell its children apart from the parent's other children.
        private readonly HashSet<VisualElement> _owned = new();
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

            List<(VisualElement Element, EffectScope Scope)>? removed = null;
            foreach (var key in _nodeCache.Keys.Where(k => !itemsByKey.ContainsKey(k)).ToList())
            {
                (removed ??= new List<(VisualElement, EffectScope)>()).Add(_nodeCache[key]);
                _nodeCache.Remove(key);
            }

            // The item elements in list order. An element someone else took out of the parent is left out,
            // as before: the list does not pull it back.
            var block = new List<VisualElement>(newKeys.Count);
            bool added = false;
            for (int i = 0; i < newKeys.Count; i++)
            {
                var key = newKeys[i];
                if (!_nodeCache.TryGetValue(key, out var cached))
                {
                    var item = itemsByKey[key];
                    // Untracked like a component body: signals the template reads directly must not
                    // subscribe this reconcile effect (bindings inside it track themselves).
                    var (scope, element) = ReactiveContext.Untrack(() => EffectScope.Record(() => _template(item)));
                    cached = (element, scope);
                    _nodeCache[key] = cached;
                    _owned.Add(element);
                    added = true;
                }
                else if (cached.Element.Parent != _parent)
                {
                    continue;
                }
                block.Add(cached.Element);
            }

            // Unchanged list (the common case): every item already sits in its slot.
            var children = _parent.Children;
            int start = Math.Min(_startIndex, children.Count - block.Count);
            if (removed == null && !added && start >= 0 && InPlace(children, block, start))
                return;

            // Splice the items in at the start index among the parent's other children (static chrome,
            // other lists) and apply the whole order at once: per-item moves cost O(n) each.
            var others = new List<VisualElement>(children.Count);
            foreach (var child in children)
            {
                if (!_owned.Contains(child))
                    others.Add(child);
            }
            start = Math.Min(_startIndex, others.Count);
            var order = new List<VisualElement>(others.Count + block.Count);
            for (int i = 0; i < start; i++) order.Add(others[i]);
            order.AddRange(block);
            for (int i = start; i < others.Count; i++) order.Add(others[i]);
            _parent.ReplaceChildren(order);

            if (removed != null)
            {
                foreach (var (element, scope) in removed)
                {
                    _owned.Remove(element);
                    scope.Dispose();
                    element.Dispose();
                }
            }
        }

        private static bool InPlace(IReadOnlyList<VisualElement> children, List<VisualElement> block, int start)
        {
            for (int i = 0; i < block.Count; i++)
            {
                if (children[start + i] != block[i]) return false;
            }
            return true;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _effect?.Dispose();
            _effect = null;

            // Detach every item in one pass; skipped when the parent is going away with them.
            if (!_parent.IsDisposed && _owned.Count > 0)
                _parent.ReplaceChildren(_parent.Children.Where(c => !_owned.Contains(c)).ToList());

            foreach (var (_, cached) in _nodeCache)
            {
                cached.Scope.Dispose();
                cached.Element.Dispose();
            }
            _nodeCache.Clear();
            _owned.Clear();
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
        ReactiveContext.OwnByCurrentEffect(reconciler);
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
