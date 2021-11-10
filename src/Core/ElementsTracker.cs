using Kara.Core;
using Kara.Core.Visual;
using QuadTrees;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kara.src.Core
{
    public class ElementsMap
    {
        private readonly Dictionary<string, VisualElement> Elements = new Dictionary<string, VisualElement>();
        private readonly Dictionary<VisualElement, ComponentTracker> Trackers = new Dictionary<VisualElement, ComponentTracker>();
        private readonly QuadTreeRectF<ComponentTracker> InteractionMap;
        public List<VisualElement> UiComponents = new List<VisualElement>();

        internal Application AppRef;

        internal ElementsMap(ref Application appref)
        {
            AppRef = appref;
        }

        /// <summary>
        /// Get components from a given point
        /// </summary>
        internal List<ComponentTracker> ComponentsFromPoint(PointF point)
        {
            return InteractionMap.GetObjects(new RectangleF(point.X, point.Y, 3, 3));
        }

        /// <summary>
        /// Finds components that collide with a components's rect
        /// </summary>
        /// <param name="Com">Component used to search collided components by</param>
        /// <returns></returns>
        internal List<VisualElement> CollidedComponents(VisualElement Com)
        {
            var Collided = InteractionMap.GetObjects(Com.Transform);
            List<VisualElement> Result = new();
            foreach (var Tracker in Collided)
            {
                if (Tracker.Component == Com) continue;
                Result.Add(Tracker.Component);
            }

            return Result;
        }

        /// <summary>
        /// Get first element from a specific point
        /// </summary>
        internal VisualElement FirstFromPoint(System.Numerics.Vector2 point)
        {
            var components = InteractionMap.GetObjects(new RectangleF(point.X, point.Y, 1, 1));
            if (!components.Any()) return null;


            var z = InteractionMap.ToList()[0].Component;

            int maxLayer = components.Max(t => t.Component.Layer);
            return components.Find(t => t.Component.Layer == maxLayer).Component;
        }

        /// <summary>
        /// Check collision of 2 elements
        /// </summary>
        /// <returns>true if com1 and com2 intersect</returns>
        internal bool ComponentsIntersect(VisualElement com1, VisualElement com2)
        {
            var Intersected = InteractionMap.GetObjects(com1.Transform);

            foreach (var Tracker in Intersected)
                if (Tracker.Component == com2) return true;

            return false;
        }

        private void RemoveTracker(VisualElement Com)
        {
            InteractionMap.Remove(Trackers[Com]);
            Trackers.Remove(Com);
        }

        private void AddTracker(ref VisualElement Com)
        {
            var NewTracker = new ComponentTracker(ref Com);
            InteractionMap.Add(NewTracker);
            Trackers.Add(Com, NewTracker);
        }

        /// <summary>
        /// Registers a <see langword="VisualElement"/>.
        /// </summary>
        public void Add(ref VisualElement e)
        {
            if (Elements.ContainsKey(e.Name))
            {
                Log.Error($"A component with name {e.Name} already exists.");
                return;
            }

            e.ApplicationParent = AppRef;
            Elements.Add(e.Name, e);
            AddTracker(ref e);

            e.OnDisposing += Element_OnDispose;
        }

        /// <summary>
        /// Removes a given <see langword="VisualElement"/> and it's children.
        /// </summary>
        /// <param name="e"></param>
        public void RemoveComponent(VisualElement e)
        {
            Elements.Remove(e.Name);
            RemoveTracker(e);

            // Remove children if any
            e.Children.ForEach(child => child.Dispose());
        }

        private void Element_OnDispose(VisualElement e)
        {
            RemoveComponent(e);
        }
    }
}
