using Blossom.Core.Visual;
using QuadTrees;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SkiaSharp;

namespace Blossom.Core;

public class ElementTree : IDisposable
{
    private readonly Dictionary<Guid, (VisualElement, ElementTracker)> _byId = new();
    private readonly Dictionary<string, VisualElement> _byName = new();
    internal readonly SortedAxis BoundAxis = new();

    private readonly QuadTreeRectF<ElementTracker> QuadTree = new(
        float.MinValue / 2f, float.MinValue / 2f,
        float.MaxValue, float.MaxValue
    );

    public VisualElement[] Items { get => _byId.Values.Select(x => x.Item1).ToArray(); }
    public int Count => _byId.Count;

    internal ElementTree() { }

    public VisualElement? FindById(Guid id)
    {
        return _byId.TryGetValue(id, out var entry) ? entry.Item1 : null;
    }

    public VisualElement? FindByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (_byName.TryGetValue(name, out var elem)) return elem;
        return _byId.Values.FirstOrDefault(x => x.Item1.Name == name).Item1;
    }

    public List<VisualElement> ComponentsFromPoint(PointF point)
    {
        return QuadTree.GetObjects(new RectangleF(point.X - 1, point.Y - 1, 2, 2)).ToArray().Select(x => x.Element).ToList();
    }

    public List<VisualElement> CollidedComponents(VisualElement element)
    {
        var Collided = QuadTree.GetObjects(element.Transform.Computed.RectF);
        List<VisualElement> Result = new();
        foreach (var Tracker in Collided)
        {
            if (Tracker.Element == element) continue;
            Result.Add(Tracker.Element);
        }

        return Result;
    }

    public VisualElement FirstFromPoint(PointF point) =>
        FirstFromPoint(point.X, point.Y);

    private void CollectElementsForHitTest(VisualElement root, List<VisualElement> list)
    {
        if (root == null || root.IsDisposed || !root.Visible) return;

        var sortedChildren = root.GetVisualChildren()
            .Where(c => c != null && !c.IsDisposed && c.Visible)
            .OrderByDescending(c => c.ZIndex)
            .ToList();
        foreach (var child in sortedChildren)
        {
            CollectElementsForHitTest(child, list);
        }
        list.Add(root);
    }

    public VisualElement FirstFromPoint(float x, float y)
    {
        var nearby = QuadTree.GetObjects(new RectangleF(x - 2f, y - 2f, 4f, 4f));
        if (nearby != null && nearby.Count > 0)
        {
            nearby.Sort((a, b) =>
            {
                if (a.Element == null || a.Element.IsDisposed) return 1;
                if (b.Element == null || b.Element.IsDisposed) return -1;
                int ld = b.Element.Layer.CompareTo(a.Element.Layer);
                if (ld != 0) return ld;
                int zd = b.Element.ZIndex.CompareTo(a.Element.ZIndex);
                if (zd != 0) return zd;
                // Same stacking: smaller control wins so a button beats a full-width bar.
                float aa = Math.Max(a.Element.Transform.Computed.Width * a.Element.Transform.Computed.Height, a.Element.Transform.Width * a.Element.Transform.Height);
                float ba = Math.Max(b.Element.Transform.Computed.Width * b.Element.Transform.Computed.Height, b.Element.Transform.Width * b.Element.Transform.Height);
                return aa.CompareTo(ba);
            });
            foreach (var tracker in nearby)
            {
                var el = tracker.Element;
                if (el == null || el.IsDisposed) continue;
                if (!el.EffectiveVisible || el.ComputedVisibility == Visibility.Hidden)
                    continue;
                if (!Hits(el, x, y))
                    continue;
                var resolved = ResolveClickthrough(el);
                if (resolved != null)
                    return resolved;
            }
        }

        var rootElements = _byId.Values.Select(x => x.Item1)
            .Where(e => e != null && !e.IsDisposed && e.Parent == null)
            .Reverse()
            .OrderByDescending(e => e.ZIndex)
            .ToList();

        var elements = new List<VisualElement>();
        foreach (var root in rootElements)
        {
            CollectElementsForHitTest(root, elements);
        }

        foreach (var elementFromPoint in elements)
        {
            if (elementFromPoint == null || elementFromPoint.IsDisposed)
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
        var components = QuadTree.GetObjects(quad);

        if (!components.Any()) return null;

        int maxLayer = components.Max(t => t.Element.Layer);

        return components.Find(t => t.Element.Layer == maxLayer).Element;
    }

    public VisualElement[] ElementsFromRect(RectangleF rect)
    {
        var components = QuadTree.GetObjects(rect);

        if (components?.Count > 0)
            return components.Select(e => e.Element).ToArray();

        return Array.Empty<VisualElement>();
    }

    public bool ComponentsIntersect(VisualElement elm1, VisualElement elm2)
    {
        var Intersected = QuadTree.GetObjects(elm1.Transform.Computed.RectF);

        for (int i = 0; i < Intersected?.Count; i++)
            if (Intersected[i].Element == elm2) return true;

        return false;
    }

    private ElementTracker AddTracker(ref VisualElement element)
    {
        var NewTracker = new ElementTracker(ref element);

        QuadTree.Add(NewTracker);

        return NewTracker;
    }

    private void RemoveTracker(VisualElement element)
    {
        if (_byId.TryGetValue(element.Id, out var entry))
        {
            QuadTree.Remove(entry.Item2);
        }
    }

    public void AddElement(ref VisualElement element)
    {
        if (_byId.ContainsKey(element.Id))
        {
            return;
        }

        var tracker = AddTracker(ref element);

        // Add element and tracker to the map
        _byId.Add(element.Id, (element, tracker));

        if (!string.IsNullOrEmpty(element.Name))
        {
            _byName[element.Name] = element;
        }

        if (element.Name != "Bounding Area")
            BoundAxis.AddElement(element);

        element.OnDisposing += Element_OnDispose;
    }

    public void AddElement(VisualElement element)
    {
        AddElement(ref element);
    }

    public void RemoveElement(VisualElement element)
    {
        if (element == null || !_byId.TryGetValue(element.Id, out var entry)) return;
        QuadTree.Remove(entry.Item2);
        _byId.Remove(element.Id);

        if (!string.IsNullOrEmpty(element.Name) && _byName.TryGetValue(element.Name, out var named) && named == element)
        {
            _byName.Remove(element.Name);
        }

        BoundAxis.RemoveElement(element);
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
        QuadTree.Clear();
    }
}