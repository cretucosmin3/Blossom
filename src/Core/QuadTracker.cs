using QuadTrees.QTreeRectF;
using System.Drawing;
using Kara.Core.Visual;

namespace Kara.Core
{
	internal class ElementTracker : IRectFQuadStorable
	{
		public RectangleF Rect;
		public VisualElement Element;
		RectangleF IRectFQuadStorable.Rect => Element.Transform.Computed.RectF;

		public ElementTracker(ref VisualElement com)
		{
			Rect = com.Transform.Computed.RectF;
			Element = com;
		}
	}
}
