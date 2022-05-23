using System.Numerics;
namespace Rux.Core.Visual;

using System;
using Rux.Core.Input;

public class ElementEvents : EventMap
{
    public event Action OnMouseEnter;
    public event Action<Vector2> OnMouseHover;
    public event Action OnMouseLeave;

    public void HandleMouseEnter() =>
        OnMouseEnter?.Invoke();

    public void HandleMouseHover(Vector2 pos) =>
        OnMouseHover?.Invoke(pos);

    public void HandleMouseLeave() =>
        OnMouseLeave?.Invoke();
}