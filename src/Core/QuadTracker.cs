using QuadTrees.QTreeRectF;
using System.Drawing;
using Kara.Core.Visual;

namespace Kara.Core
{
	internal class ComponentTracker : IRectFQuadStorable
	{
		public RectangleF Rect;
		public VisualElement Component;
		RectangleF IRectFQuadStorable.Rect => Component.Transform;

		public ComponentTracker(ref VisualElement com)
		{
			Rect = com.Transform;
			Component = com;
		}
	}
}
