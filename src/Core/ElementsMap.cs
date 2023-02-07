using Blossom.Core.Visual;
using QuadTrees;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Blossom.Core
{
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

        public VisualElement FirstFromPoint(float x, float y)
        {
            var hitPoint = new RectangleF(x, y, 1, 1);
            var components = QuadTree.GetObjects(hitPoint);
            if (!components.Any()) return null;

            for (int i = components.Count - 1; i >= 0; i--)
            {
                var elementFromPoint = components[i].Element;
                var isWithinParent = true;

                if (elementFromPoint.ComputedVisibility == Visibility.Hidden)
                    continue;


                if (elementFromPoint.ComputedVisibility == Visibility.Clipped)
                {
                    isWithinParent = elementFromPoint.Parent.Transform.Computed.RectF.Contains(elementFromPoint.Transform.Computed.RectF);
                }

                if (isWithinParent && !elementFromPoint.IsClickthrough)
                    return elementFromPoint;
            }

            return null;
        }

        public VisualElement FirstFromQuad(RectangleF quad)
        {
            var components = QuadTree.GetObjects(quad);
            if (!components.Any()) return null;

            int maxLayer = components.Max(t => t.Element.Layer);
            return components.Find(t => t.Element.Layer == maxLayer).Element;
        }

        public bool ComponentsIntersect(VisualElement elm1, VisualElement elm2)
        {
            var Intersected = QuadTree.GetObjects(elm1.Transform.Computed.RectF);

            foreach (var Tracker in Intersected)
                if (Tracker.Element == elm2) return true;

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
            var (_, tracker) = Map[Element.Name];
            QuadTree.Remove(tracker);
        }

        public void AddElement(ref VisualElement element, View view)
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
}
