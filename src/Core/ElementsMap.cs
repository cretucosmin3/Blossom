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
    private readonly Dictionary<string, (VisualElement, ElementTracker)> Map = new();
    internal readonly SortedAxis BoundAxis = new();

    private readonly QuadTreeRectF<ElementTracker> QuadTree = new(
        float.MinValue / 2f, float.MinValue / 2f,
        float.MaxValue, float.MaxValue
    );

    public VisualElement[] Items { get => Map.Values.Select(x => x.Item1).ToArray(); }

    internal ElementTree() { }

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
        var sortedChildren = root.Children.Where(c => c != null).OrderByDescending(c => c.ZIndex).ToList();
        foreach (var child in sortedChildren)
        {
            CollectElementsForHitTest(child, list);
        }
        list.Add(root);
    }

    public VisualElement FirstFromPoint(float x, float y)
    {
        var rootElements = Map.Values.Select(x => x.Item1)
            .Where(e => e.Parent == null)
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
            if (elementFromPoint.ComputedVisibility == Visibility.Hidden)
                continue;

            SKMatrix44 globalMatrix = elementFromPoint.Transform.GetGlobalM44();

            // Solve:
            // x = (m00 * Xl + m01 * Yl + m03) / (m30 * Xl + m31 * Yl + m33)
            // y = (m10 * Xl + m11 * Yl + m13) / (m30 * Xl + m31 * Yl + m33)
            //
            // Rearranged as linear system:
            // (x * m30 - m00) * Xl + (x * m31 - m01) * Yl = m03 - x * m33
            // (y * m30 - m10) * Xl + (y * m31 - m11) * Yl = m13 - y * m33

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
                continue;

            float localX = (C1 * B2 - B1 * C2) / D;
            float localY = (A1 * C2 - C1 * A2) / D;

            // Ensure the clicked point is in front of the camera (w > 0)
            float w = m30 * localX + m31 * localY + m33;
            if (w <= 1e-6f)
                continue;

            if (!elementFromPoint.IsPointInside(localX, localY))
            {
                continue;
            }

            if (elementFromPoint.ComputedVisibility == Visibility.Clipped || elementFromPoint.HasClippingAncestors)
            {
                bool insideClipping = true;
                var ancestor = elementFromPoint.Parent;
                while (ancestor != null)
                {
                    if (ancestor.IsClipping)
                    {
                        var globalPt3D = MapPoint3D(globalMatrix, localX, localY, 0f);
                        var ancestorGlobal = ancestor.Transform.GetGlobalM44();
                        var invAncestorGlobal = new SKMatrix44();
                        if (ancestorGlobal.Invert(invAncestorGlobal))
                        {
                            var ancestorLocalPt = MapPoint3D(invAncestorGlobal, globalPt3D.X, globalPt3D.Y, globalPt3D.Z);
                            if (!ancestor.IsPointInside(ancestorLocalPt.X, ancestorLocalPt.Y))
                            {
                                insideClipping = false;
                                break;
                            }
                        }
                    }
                    ancestor = ancestor.Parent;
                }
                if (!insideClipping) continue;
            }

            if (!elementFromPoint.IsClickthrough)
                return elementFromPoint;
        }

        return default!;
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

    private void RemoveTracker(VisualElement Element)
    {
        if (Map.TryGetValue(Element.Name, out var entry))
        {
            QuadTree.Remove(entry.Item2);
        }
    }

    public void AddElement(ref VisualElement element)
    {
        if (Map.ContainsKey(element.Name))
        {
            Log.Error($"A component with name {element.Name} already exists.");
            return;
        }

        var tracker = AddTracker(ref element);

        // Add element and tracker to the map
        Map.Add(element.Name, (element, tracker));

        if (element.Name != "Bounding Area")
            BoundAxis.AddElement(element);

        element.OnDisposing += Element_OnDispose;
    }

    public void RemoveElement(VisualElement element)
    {
        if (element == null || !Map.ContainsKey(element.Name)) return;
        RemoveTracker(element);
        Map.Remove(element.Name);
        BoundAxis.RemoveElement(element);
    }

    private void Element_OnDispose(VisualElement e)
    {
        RemoveElement(e);
    }

    public void Dispose()
    {
        foreach (var (element, _) in Map.Values)
        {
            element.Dispose();
        }

        QuadTree.Clear();
        Map.Clear();
    }
}