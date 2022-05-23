using System.Numerics;
namespace Rux.Core.Visual;

using System;
using Rux.Core.Input;

public class ElementEvents : EventMap
{
    public event Action<VisualElement> OnMouseEnter;
    public event Action<VisualElement, Vector2> OnMouseHover;
    public event Action<VisualElement> OnMouseLeave;

    public void HandleMouseEnter(VisualElement el) =>
        OnMouseEnter?.Invoke(el);

    public void HandleMouseHover(VisualElement el, Vector2 pos) =>
        OnMouseHover?.Invoke(el, pos);

    public void HandleMouseLeave(VisualElement el) =>
        OnMouseLeave?.Invoke(el);
}