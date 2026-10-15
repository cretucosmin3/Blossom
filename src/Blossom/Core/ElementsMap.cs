using Blossom.Core.Visual;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SkiaSharp;

namespace Blossom.Core;

public class ElementTree : IDisposable
{
    private readonly Dictionary<Guid, VisualElement> _byId = new();
    private readonly Dictionary<string, VisualElement> _byName = new();

    public VisualElement[] Items { get => _byId.Values.ToArray(); }
    public int Count => _byId.Count;

    internal ElementTree() { }

    public VisualElement? FindById(Guid id)
    {
        return _byId.TryGetValue(id, out var element) ? element : null;
    }

    public VisualElement? FindByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (_byName.TryGetValue(name, out var elem)) return elem;
        return _byId.Values.FirstOrDefault(x => x.Name == name);
    }

    public List<VisualElement> ComponentsFromPoint(PointF point)
    {
        return ElementsIntersecting(new RectangleF(point.X - 1, point.Y - 1, 2, 2));
    }

    public List<VisualElement> CollidedComponents(VisualElement element)
    {
        var Result = ElementsIntersecting(element.Transform.Computed.RectF);
        Result.Remove(element);
        return Result;
    }

    public VisualElement FirstFromPoint(PointF point) =>
        FirstFromPoint(point.X, point.Y);

    /// <summary>
    /// Appends <paramref name="root"/> and its visible subtree in paint order
    /// (parent first, children by ascending ZIndex, then tree order).
    /// </summary>
    internal static void CollectPaintOrder(VisualElement root, List<VisualElement> list)
    {
        if (!root.Visible) return;
        list.Add(root);
        var sortedChildren = root.GetVisualChildren().Where(c => c != null && c.Visible).OrderBy(c => c.ZIndex).ToList();
        foreach (var child in sortedChildren)
        {
            CollectPaintOrder(child, list);
        }
    }

    /// <summary>
    /// Front-to-back hit test over every root in paint order. <see cref="View.HitTest"/> is the
    /// cheaper, overlay-aware path the view uses for input.
    /// </summary>
    public VisualElement FirstFromPoint(float x, float y)
    {
        var elements = new List<VisualElement>();
        foreach (var root in _byId.Values.Where(e => !e.IsDisposed && e.Parent == null).OrderBy(e => e.ZIndex))
        {
            CollectPaintOrder(root, elements);
        }

        for (int i = elements.Count - 1; i >= 0; i--)
        {
            var elementFromPoint = elements[i];
            if (elementFromPoint.IsDisposed)
                continue;
            if (!elementFromPoint.EffectiveVisible || elementFromPoint.ComputedVisibility == Visibility.Hidden)
                continue;

            if (!Hits(elementFromPoint, x, y))
                continue;

            var resolved = ResolveClickthrough(elementFromPoint);
            if (resolved != null)
                return resolved;
        }

        return default!;
    }

    /// <summary>
    /// <see cref="VisualElement.IsClickthrough"/> is pointer-events:none: keep walking to the
    /// nearest ancestor that can actually receive hover/click (e.g. a button's icon/label).
    /// </summary>
    internal static VisualElement? ResolveClickthrough(VisualElement el)
    {
        while (el != null && el.IsClickthrough)
            el = el.Parent!;
        if (el == null || el.IsDisposed)
            return null;
        if (!el.EffectiveVisible || !el.EffectiveInteractive)
            return null;
        if (el.ComputedVisibility == Visibility.Hidden)
            return null;
        return el;
    }

    internal static bool Hits(VisualElement elementFromPoint, float x, float y)
    {
        if (elementFromPoint == null || elementFromPoint.IsDisposed)
            return false;

        var bounds = elementFromPoint.RenderBounds;
        if (x < bounds.Left || x > bounds.Right || y < bounds.Top || y > bounds.Bottom)
            return false;

        SKMatrix44 globalMatrix = elementFromPoint.Transform.GetGlobalM44();

        if (!Transform.TryUnproject(globalMatrix, x, y, out float localX, out float localY))
            return false;

        if (!elementFromPoint.HitTestLocal(localX, localY))
            return false;

        if (elementFromPoint.ComputedVisibility == Visibility.Clipped || elementFromPoint.HasClippingAncestors)
        {
            var ancestor = elementFromPoint.Parent;
            while (ancestor != null)
            {
                if (ancestor.IsDisposed)
                    return false;
                if (ancestor.IsClipping)
                {
                    var ancestorGlobal = ancestor.Transform.GetGlobalM44();
                    if (!Transform.TryUnproject(ancestorGlobal, x, y, out float ancestorLocalX, out float ancestorLocalY) ||
                        !ancestor.HitTestLocal(ancestorLocalX, ancestorLocalY))
                        return false;
                }
                ancestor = ancestor.Parent;
            }
        }

        return true;
    }

    public VisualElement? FirstFromQuad(RectangleF quad)
    {
        VisualElement? result = null;
        foreach (var element in _byId.Values)
        {
            if (!element.Transform.Computed.RectF.IntersectsWith(quad)) continue;
            if (result == null || element.Layer > result.Layer)
                result = element;
        }

        return result;
    }

    public VisualElement[] ElementsFromRect(RectangleF rect)
    {
        return ElementsIntersecting(rect).ToArray();
    }

    public bool ComponentsIntersect(VisualElement elm1, VisualElement elm2)
    {
        return elm1.Transform.Computed.RectF.IntersectsWith(elm2.Transform.Computed.RectF);
    }

    private List<VisualElement> ElementsIntersecting(RectangleF rect)
    {
        var result = new List<VisualElement>();
        foreach (var element in _byId.Values)
        {
            if (element.Transform.Computed.RectF.IntersectsWith(rect))
                result.Add(element);
        }

        return result;
    }

    public void AddElement(ref VisualElement element)
    {
        if (_byId.ContainsKey(element.Id))
        {
            return;
        }

        _byId.Add(element.Id, element);

        if (!string.IsNullOrEmpty(element.Name))
        {
            _byName[element.Name] = element;
        }

        element.OnDisposing += Element_OnDispose;
    }

    public void AddElement(VisualElement element)
    {
        AddElement(ref element);
    }

    public void RemoveElement(VisualElement element)
    {
        if (element == null || !_byId.Remove(element.Id)) return;

        if (!string.IsNullOrEmpty(element.Name) && _byName.TryGetValue(element.Name, out var named) && named == element)
        {
            _byName.Remove(element.Name);
        }

        element.OnDisposing -= Element_OnDispose;
    }

    private void Element_OnDispose(VisualElement e)
    {
        RemoveElement(e);
    }

    public void Dispose()
    {
        _byId.Clear();
        _byName.Clear();
    }
}
