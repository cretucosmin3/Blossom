using System;
using System.Collections.Generic;
using Blossom.Core.Visual;

namespace Blossom.Primitives;

/// <summary>
/// Base for hosts that assign child frames in <see cref="VisualElement.LayoutChildren"/>.
/// </summary>
public abstract class LayoutElement : VisualElement
{
    protected IEnumerable<VisualElement> ParticipatingChildren()
    {
        var children = Children;
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child == null || !child.Visible) continue;
            if (Layout.IsManual(child)) continue;
            yield return child;
        }
    }

    protected static void Place(VisualElement child, float absX, float absY, float width, float height)
    {
        child.Transform.SetAbsoluteFrame(absX, absY, Math.Max(0f, width), Math.Max(0f, height));
    }

    protected static float PreferredWidth(VisualElement child, float maxWidth, float maxHeight)
    {
        var size = child.GetPreferredSize(maxWidth, maxHeight);
        if (size.Width > 0) return size.Width;
        return child.Transform.Width;
    }

    protected static float PreferredHeight(VisualElement child, float maxWidth, float maxHeight)
    {
        var size = child.GetPreferredSize(maxWidth, maxHeight);
        if (size.Height > 0) return size.Height;
        return child.Transform.Height;
    }

    protected static void AlignInSlot(VisualElement child, float slotX, float slotY, float slotW, float slotH,
        float prefW, float prefH, LayoutAlign alignX, LayoutAlign alignY)
    {
        float w = alignX == LayoutAlign.Stretch ? slotW : Math.Min(prefW, slotW);
        float h = alignY == LayoutAlign.Stretch ? slotH : Math.Min(prefH, slotH);
        float x = slotX;
        float y = slotY;
        if (alignX == LayoutAlign.Center) x = slotX + (slotW - w) * 0.5f;
        else if (alignX == LayoutAlign.End) x = slotX + slotW - w;
        if (alignY == LayoutAlign.Center) y = slotY + (slotH - h) * 0.5f;
        else if (alignY == LayoutAlign.End) y = slotY + slotH - h;
        Place(child, x, y, w, h);
    }
}
