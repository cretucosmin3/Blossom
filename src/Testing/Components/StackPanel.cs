using System;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Components;

public enum Orientation
{
    Vertical,
    Horizontal
}

/// <summary>
/// Sample custom layout element demonstrating LayoutChildren, Padding, and GetPreferredSize.
/// Stacks children vertically or horizontally with configurable Gap and Padding.
/// </summary>
public class StackPanel : VisualElement
{
    private Orientation _orientation = Orientation.Vertical;
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation != value)
            {
                _orientation = value;
                InvalidateLayout();
            }
        }
    }

    private float _gap = 0f;
    public float Gap
    {
        get => _gap;
        set
        {
            if (_gap != value)
            {
                _gap = value;
                InvalidateLayout();
            }
        }
    }

    public StackPanel()
    {
    }

    protected override void LayoutChildren()
    {
        var children = Children;
        if (children.Count == 0) return;

        // Transform.X/Y are absolute (computed) coordinates, not parent-local.
        float originX = Transform.Computed.X;
        float originY = Transform.Computed.Y;

        if (Orientation == Orientation.Vertical)
        {
            float y = Padding.Top;
            float availableWidth = Math.Max(0, Transform.Width - Padding.Horizontal);

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null || !child.Visible) continue;

                var preferred = child.GetPreferredSize(availableWidth, 0);
                float childWidth = (child.Transform.Anchor.HasFlag(Anchor.Left) && child.Transform.Anchor.HasFlag(Anchor.Right))
                    ? availableWidth
                    : preferred.Width;

                float childHeight = preferred.Height > 0 ? preferred.Height : child.Transform.Height;

                if (childWidth > 0)
                    child.Transform.Width = childWidth;
                if (childHeight > 0)
                    child.Transform.Height = childHeight;
                child.Transform.X = originX + Padding.Left;
                child.Transform.Y = originY + y;

                y += childHeight + Gap;
            }
        }
        else
        {
            float x = Padding.Left;
            float availableHeight = Math.Max(0, Transform.Height - Padding.Vertical);

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null || !child.Visible) continue;

                var preferred = child.GetPreferredSize(0, availableHeight);
                float childWidth = preferred.Width > 0 ? preferred.Width : child.Transform.Width;
                float childHeight = (child.Transform.Anchor.HasFlag(Anchor.Top) && child.Transform.Anchor.HasFlag(Anchor.Bottom))
                    ? availableHeight
                    : preferred.Height;

                if (childWidth > 0)
                    child.Transform.Width = childWidth;
                if (childHeight > 0)
                    child.Transform.Height = childHeight;
                child.Transform.X = originX + x;
                child.Transform.Y = originY + Padding.Top;

                x += childWidth + Gap;
            }
        }
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        var children = Children;
        float totalW = 0;
        float totalH = 0;

        if (Orientation == Orientation.Vertical)
        {
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null || !child.Visible) continue;
                var size = child.GetPreferredSize(maxWidth, 0);
                totalW = Math.Max(totalW, size.Width);
                totalH += size.Height + (i > 0 ? Gap : 0);
            }
            totalW += Padding.Horizontal;
            totalH += Padding.Vertical;
        }
        else
        {
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null || !child.Visible) continue;
                var size = child.GetPreferredSize(0, maxHeight);
                totalW += size.Width + (i > 0 ? Gap : 0);
                totalH = Math.Max(totalH, size.Height);
            }
            totalW += Padding.Horizontal;
            totalH += Padding.Vertical;
        }

        if (MinWidth.HasValue) totalW = Math.Max(totalW, MinWidth.Value);
        if (MaxWidth.HasValue) totalW = Math.Min(totalW, MaxWidth.Value);
        if (MinHeight.HasValue) totalH = Math.Max(totalH, MinHeight.Value);
        if (MaxHeight.HasValue) totalH = Math.Min(totalH, MaxHeight.Value);

        if (maxWidth > 0) totalW = Math.Min(totalW, maxWidth);
        if (maxHeight > 0) totalH = Math.Min(totalH, maxHeight);

        return new SKSize(totalW, totalH);
    }
}
