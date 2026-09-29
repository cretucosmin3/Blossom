using System;
using Blossom.Core.Visual;

namespace Blossom.Reactive;

/// <summary>
/// Provides conditional rendering flow controls for VisualElement trees.
/// </summary>
public static class Show
{
    private sealed class Controller : IDisposable
    {
        private readonly VisualElement _parent;
        private readonly Func<bool> _condition;
        private readonly Func<VisualElement> _contentFactory;
        private readonly Func<VisualElement?>? _fallbackFactory;
        private VisualElement? _currentMounted;
        private EffectScope? _currentScope;
        private bool? _lastCondition;
        private Effect? _effect;
        private bool _isDisposed;

        public Controller(
            VisualElement parent,
            Func<bool> condition,
            Func<VisualElement> contentFactory,
            Func<VisualElement?>? fallbackFactory)
        {
            _parent = parent;
            _condition = condition;
            _contentFactory = contentFactory;
            _fallbackFactory = fallbackFactory;
        }

        public void Start()
        {
            _effect = new Effect(Apply);
        }

        private void Apply()
        {
            if (_isDisposed) return;

            bool isTrue = _condition();
            if (_lastCondition.HasValue && _lastCondition.Value == isTrue) return;
            _lastCondition = isTrue;

            UnmountCurrent();

            if (isTrue)
            {
                Mount(_contentFactory);
            }
            else if (_fallbackFactory != null)
            {
                Mount(_fallbackFactory);
            }
        }

        private void Mount(Func<VisualElement?> factory)
        {
            ( _currentScope, _currentMounted ) = EffectScope.Record(factory);
            if (_currentMounted != null)
            {
                _parent.AddChild(_currentMounted);
            }
        }

        private void UnmountCurrent()
        {
            if (_currentMounted != null)
            {
                _parent.RemoveChild(_currentMounted);
                _currentScope?.Dispose();
                _currentMounted.Dispose();
                _currentMounted = null;
                _currentScope = null;
            }
            else
            {
                _currentScope?.Dispose();
                _currentScope = null;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _effect?.Dispose();
            _effect = null;
            UnmountCurrent();
        }
    }

    /// <summary>
    /// Conditionally mounts content into a parent container when the condition signal evaluates to true,
    /// or mounts the optional fallback when false.
    /// </summary>
    public static IDisposable When(
        VisualElement parent,
        Func<bool> condition,
        Func<VisualElement> contentFactory,
        Func<VisualElement?>? fallbackFactory = null)
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        if (contentFactory == null) throw new ArgumentNullException(nameof(contentFactory));

        var controller = new Controller(parent, condition, contentFactory, fallbackFactory);
        controller.Start();
        parent.BindLifecycle(controller);
        return controller;
    }
}
