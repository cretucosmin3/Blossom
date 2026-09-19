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

    private static SKPoint3 MapPoint3D(SKMatrix44 matrix, float x, float y, float z)
    {
        float[] result = matrix.MapScalars(x, y, z, 1f);
        float w = result[3];
        if (Math.Abs(w) > 1e-6f)
        {
            return new SKPoint3(result[0] / w, result[1] / w, result[2] / w);
        }
        return new SKPoint3(result[0], result[1], result[2]);
    }

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

        float m00 = globalMatrix[0, 0];
        float m01 = globalMatrix[0, 1];
        float m03 = globalMatrix[0, 3];

        float m10 = globalMatrix[1, 0];
        float m11 = globalMatrix[1, 1];
        float m13 = globalMatrix[1, 3];

        float m30 = globalMatrix[3, 0];
        float m31 = globalMatrix[3, 1];
        float m33 = globalMatrix[3, 3];

        float A1 = x * m30 - m00;
        float B1 = x * m31 - m01;
        float C1 = m03 - x * m33;

        float A2 = y * m30 - m10;
        float B2 = y * m31 - m11;
        float C2 = m13 - y * m33;

        float D = A1 * B2 - B1 * A2;
        if (Math.Abs(D) < 1e-6f)
            return false;

        float localX = (C1 * B2 - B1 * C2) / D;
        float localY = (A1 * C2 - C1 * A2) / D;

        float w = m30 * localX + m31 * localY + m33;
        if (w <= 1e-6f)
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
                    var globalPt3D = MapPoint3D(globalMatrix, localX, localY, 0f);
                    var ancestorGlobal = ancestor.Transform.GetGlobalM44();
                    using var invAncestorGlobal = new SKMatrix44();
                    if (ancestorGlobal.Invert(invAncestorGlobal))
                    {
                        var ancestorLocalPt = MapPoint3D(invAncestorGlobal, globalPt3D.X, globalPt3D.Y, globalPt3D.Z);
                        if (!ancestor.HitTestLocal(ancestorLocalPt.X, ancestorLocalPt.Y))
                            return false;
                    }
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