using System.Numerics;
namespace Blossom.Core.Visual;

using System;
using Blossom.Core.Input;

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