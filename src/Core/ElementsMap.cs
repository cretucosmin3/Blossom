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
        private readonly QuadTreeRectF<ElementTracker> QuadTree = new(
            float.MinValue / 2f, float.MinValue / 2f,
            float.MaxValue, float.MaxValue
        );

        public VisualElement[] Items { get => Map.Values.Select(x => x.Item1).ToArray(); }

        internal ElementTree() { }

        public List<VisualElement> ComponentsFromPoint(PointF point)
        {
            QuadTree.Max(e => e.Rect.X);
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
            var components = QuadTree.GetObjects(new RectangleF(x, y, 1, 1));
            if (!components.Any()) return null;

            for (int i = components.Count - 1; i >= 0; i--)
            {
                // if (!components[i].Element.IsClickthrough || components[i].Element.Style?.BackColor.Alpha > 0)
                if (components[i].Element.Style?.BackColor.Alpha > 0 && !components[i].Element.IsClickthrough)
                    return components[i].Element;
            }

            return null;
            // return components.Last().Element;
            // int maxLayer = components.Max(t => t.Element.Layer);
            // return components.Find(t => t.Element.Layer == maxLayer).Element;
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

        public void AddElement(ref VisualElement e, View view)
        {
            if (Map.ContainsKey(e.Name))
            {
                Log.Error($"A component with name {e.Name} already exists.");
                return;
            }

            e.ParentApplication = view.Application;
            e.ParentView = view;
            var tracker = AddTracker(ref e);

            // Add element and tracker to the map
            Map.Add(e.Name, (e, tracker));

            e.OnDisposing += Element_OnDispose;
        }

        public void RemoveElement(VisualElement e)
        {
            Map.Remove(e.Name);

            // Remove children if any
            e.Children.ForEach(child => child.Dispose());
        }

        private void Element_OnDispose(VisualElement e)
        {
            RemoveElement(e);
        }

        public void Dispose()
        {
            foreach (var (element, tracker) in Map.Values)
            {
                element.Dispose();
            }

            QuadTree.Clear();
            Map.Clear();
        }
    }
}
