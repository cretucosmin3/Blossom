using System;
using System.Runtime.CompilerServices;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Reactive;

/// <summary>
/// Provides fluent binding extensions for connecting VisualElement properties directly to reactive signals and effects.
/// </summary>
public static class VisualElementBindingExtensions
{
    private static readonly ConditionalWeakTable<VisualElement, EffectScope> _elementScopes = new();

    /// <summary>
    /// Gets or creates the EffectScope associated with the specified VisualElement.
    /// The scope is automatically disposed when the element is disposed.
    /// </summary>
    public static EffectScope GetReactiveScope(this VisualElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));

        if (!_elementScopes.TryGetValue(element, out var scope))
        {
            scope = new EffectScope();
            _elementScopes.Add(element, scope);

            element.Disposed += _ =>
            {
                scope.Dispose();
                _elementScopes.Remove(element);
            };
        }

        return scope;
    }

    /// <summary>
    /// Registers a disposable resource (such as an effect subscription) to be cleaned up when this element is disposed.
    /// </summary>
    public static T BindLifecycle<T>(this T element, IDisposable disposable) where T : VisualElement
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (disposable == null) throw new ArgumentNullException(nameof(disposable));

        element.GetReactiveScope().Register(disposable);
        return element;
    }

    /// <summary>
    /// Creates a reactive effect scoped to this element that re-runs whenever any signals read within it change.
    /// </summary>
    public static T Bind<T>(this T element, Action<T> bindingAction) where T : VisualElement
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (bindingAction == null) throw new ArgumentNullException(nameof(bindingAction));

        var effect = new Effect(() => bindingAction(element));
        element.BindLifecycle(effect);
        return element;
    }

    /// <summary>
    /// Reactively binds the Text property of this VisualElement to a signal or computed expression.
    /// </summary>
    public static T BindText<T>(this T element, Func<string> textAccessor) where T : VisualElement
    {
        if (textAccessor == null) throw new ArgumentNullException(nameof(textAccessor));
        return element.Bind(el => el.Text = textAccessor());
    }

    /// <summary>
    /// Reactively binds the Visible property of this VisualElement to a boolean signal or computed expression.
    /// </summary>
    public static T BindVisible<T>(this T element, Func<bool> visibleAccessor) where T : VisualElement
    {
        if (visibleAccessor == null) throw new ArgumentNullException(nameof(visibleAccessor));
        return element.Bind(el => el.Visible = visibleAccessor());
    }

    /// <summary>
    /// Reactively binds the element's background color to a color accessor function.
    /// </summary>
    public static T BindBackColor<T>(this T element, Func<SKColor> colorAccessor) where T : VisualElement
    {
        if (colorAccessor == null) throw new ArgumentNullException(nameof(colorAccessor));
        return element.Bind(el => el.Style.BackColor = colorAccessor());
    }

    /// <summary>
    /// Reactively binds the element's opacity to a float accessor function.
    /// </summary>
    public static T BindOpacity<T>(this T element, Func<float> opacityAccessor) where T : VisualElement
    {
        if (opacityAccessor == null) throw new ArgumentNullException(nameof(opacityAccessor));
        return element.Bind(el => el.Opacity = opacityAccessor());
    }

    /// <summary>
    /// Reactively binds the element's border roundness to a float accessor function.
    /// </summary>
    public static T BindBorderRoundness<T>(this T element, Func<float> roundnessAccessor) where T : VisualElement
    {
        if (roundnessAccessor == null) throw new ArgumentNullException(nameof(roundnessAccessor));
        return element.Bind(el =>
        {
            if (el.Style.Border == null)
            {
                el.Style.Border = new BorderStyle();
            }
            el.Style.Border.Roundness = roundnessAccessor();
            el.InvalidatePaint();
        });
    }
}
