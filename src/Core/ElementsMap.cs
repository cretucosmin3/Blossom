using Kara.Core;
using Kara.Core.Visual;
using QuadTrees;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kara.Core
{
	public class ElementsMap
	{
		private readonly Dictionary<string, (VisualElement, ComponentTracker)> Map = new();
		private readonly QuadTreeRectF<ComponentTracker> QuadTree = new(
			float.MinValue / 2f, float.MinValue / 2f,
			float.MaxValue, float.MaxValue
		);

		public List<VisualElement> UiComponents = new List<VisualElement>();

		internal ElementsMap() { }

		/// <summary>
		/// Get components from a given point
		/// </summary>
		public List<VisualElement> ComponentsFromPoint(PointF point)
		{
			return QuadTree.GetObjects(new RectangleF(point.X, point.Y, 3, 3)).ToArray().Select(x => x.Component).ToList();
		}

		/// <summary>
		/// Finds components that collide with a components's rect
		/// </summary>
		/// <param name="Com">Component used to search collided components by</param>
		/// <returns></returns>
		public List<VisualElement> CollidedComponents(VisualElement Com)
		{
			var Collided = QuadTree.GetObjects(Com.Transform);
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
		public VisualElement FirstFromPoint(System.Numerics.Vector2 point)
		{
			var components = QuadTree.GetObjects(new RectangleF(point.X, point.Y, 1, 1));
			if (!components.Any()) return null;

			int maxLayer = components.Max(t => t.Component.Layer);
			return components.Find(t => t.Component.Layer == maxLayer).Component;
		}

		/// <summary>
		/// Check collision of 2 elements
		/// </summary>
		/// <returns>true if com1 and com2 intersect</returns>
		public bool ComponentsIntersect(VisualElement com1, VisualElement com2)
		{
			var Intersected = QuadTree.GetObjects(com1.Transform);

			foreach (var Tracker in Intersected)
				if (Tracker.Component == com2) return true;

			return false;
		}

		private ComponentTracker AddTracker(ref VisualElement Com)
		{
			var NewTracker = new ComponentTracker(ref Com);
			QuadTree.Add(NewTracker);

			return NewTracker;
		}

		private void RemoveTracker(VisualElement Element)
		{
			var (_, tracker) = Map[Element.Name];
			QuadTree.Remove(tracker);
		}

		/// <summary>
		/// Registers a <see langword="VisualElement"/>.
		/// </summary>
		public void AddElement(ref VisualElement e, Application app)
		{
			if (Map.ContainsKey(e.Name))
			{
				Log.Error($"A component with name {e.Name} already exists.");
				return;
			}

			e.ApplicationParent = app;
			var tracker = AddTracker(ref e);

			// Add element and tracker to the map
			Map.Add(e.Name, (e, tracker));

			e.OnDisposing += Element_OnDispose;
		}

		/// <summary>
		/// Removes a given <see langword="VisualElement"/> and it's children.
		/// </summary>
		/// <param name="e"></param>
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
	}
}
