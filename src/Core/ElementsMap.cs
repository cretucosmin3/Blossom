﻿using Kara.Core;
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
	public class ElementsMap : IDisposable
	{
		private readonly Dictionary<string, (VisualElement, ElementTracker)> Map = new();
		private readonly QuadTreeRectF<ElementTracker> QuadTree = new(
			float.MinValue / 2f, float.MinValue / 2f,
			float.MaxValue, float.MaxValue
		);

		internal ElementsMap() { }

		public List<VisualElement> ComponentsFromPoint(PointF point)
		{
			return QuadTree.GetObjects(new RectangleF(point.X - 1, point.Y - 1, 2, 2)).ToArray().Select(x => x.Element).ToList();
		}

		public List<VisualElement> CollidedComponents(VisualElement element)
		{
			var Collided = QuadTree.GetObjects(element.Transform);
			List<VisualElement> Result = new();
			foreach (var Tracker in Collided)
			{
				if (Tracker.Element == element) continue;
				Result.Add(Tracker.Element);
			}

			return Result;
		}

		public VisualElement FirstFromPoint(System.Numerics.Vector2 point)
		{
			var components = QuadTree.GetObjects(new RectangleF(point.X, point.Y, 1, 1));
			if (!components.Any()) return null;

			int maxLayer = components.Max(t => t.Element.Layer);
			return components.Find(t => t.Element.Layer == maxLayer).Element;
		}

		public bool ComponentsIntersect(VisualElement elm1, VisualElement elm2)
		{
			var Intersected = QuadTree.GetObjects(elm1.Transform);

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
