using System.Runtime.CompilerServices;
using Blossom.Core.Visual;

namespace Blossom.Primitives;

/// <summary>
/// Attached layout flags shared by <see cref="Stack"/>, <see cref="Grid"/>, and <see cref="Split"/>.
/// </summary>
public static class Layout
{
    private static readonly ConditionalWeakTable<VisualElement, Marker> Manual = new();

    /// <summary>
    /// When true, layout hosts skip this child (you keep placing it with <c>SetAbsoluteFrame</c>).
    /// </summary>
    public static void SetManual(VisualElement element, bool manual)
    {
        if (element == null) return;
        if (manual)
            Manual.GetOrCreateValue(element);
        else
            Manual.Remove(element);

        if (element.Parent is VisualElement parent)
            parent.InvalidateLayout();
    }

    public static bool IsManual(VisualElement? element)
        => element != null && Manual.TryGetValue(element, out _);

    private sealed class Marker { }
}
