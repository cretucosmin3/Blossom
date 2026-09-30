using System;
using System.Runtime.CompilerServices;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Primitives;

/// <summary>
/// Lays out visible children in a row or column. Writes position and size.
/// Use <see cref="SetGrow"/> so a child takes leftover space on the main axis.
/// </summary>
public class Stack : LayoutElement
{
    private static readonly ConditionalWeakTable<VisualElement, GrowBox> Grow = new();

    private Orientation _orientation = Orientation.Vertical;
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation == value) return;
            _orientation = value;
            InvalidateLayout();
        }
    }

    private float _gap;
    public float Gap
    {
        get => _gap;
        set
        {
            if (_gap == value) return;
            _gap = Math.Max(0f, value);
            InvalidateLayout();
        }
    }

    private LayoutAlign _align = LayoutAlign.Stretch;
    /// <summary>Cross-axis alignment. Stretch (default) sizes children to the stack's inner size.</summary>
    public LayoutAlign Align
    {
        get => _align;
        set
        {
            if (_align == value) return;
            _align = value;
            InvalidateLayout();
        }
    }

    /// <summary>Star weight on the main axis. 0 = preferred size only.</summary>
    public static void SetGrow(VisualElement element, float weight)
    {
        if (element == null) return;
        if (weight <= 0f)
            Grow.Remove(element);
        else
        {
            if (Grow.TryGetValue(element, out var box))
                box.Weight = weight;
            else
                Grow.Add(element, new GrowBox { Weight = weight });
        }

        (element.Parent as VisualElement)?.InvalidateLayout();
    }

    public static float GetGrow(VisualElement? element)
        => element != null && Grow.TryGetValue(element, out var box) ? box.Weight : 0f;

    protected override void LayoutChildren()
    {
        bool vertical = Orientation == Orientation.Vertical;
        float innerW = Math.Max(0f, Transform.Width - Padding.Horizontal);
        float innerH = Math.Max(0f, Transform.Height - Padding.Vertical);
        float originX = Transform.AbsoluteX + Padding.Left;
        float originY = Transform.AbsoluteY + Padding.Top;

        var kids = new System.Collections.Generic.List<VisualElement>();
        foreach (var child in ParticipatingChildren())
            kids.Add(child);
        if (kids.Count == 0) return;

        float totalGap = Gap * Math.Max(0, kids.Count - 1);
        float leftover = vertical ? innerH - totalGap : innerW - totalGap;
        float starSum = 0f;
        var prefMain = new float[kids.Count];

        for (int i = 0; i < kids.Count; i++)
        {
            float grow = GetGrow(kids[i]);
            if (grow > 0f)
            {
                starSum += grow;
                prefMain[i] = 0f;
            }
            else
            {
                prefMain[i] = vertical
                    ? PreferredHeight(kids[i], innerW, 0f)
                    : PreferredWidth(kids[i], 0f, innerH);
                leftover -= prefMain[i];
            }
        }

        leftover = Math.Max(0f, leftover);
        float cursor = 0f;

        for (int i = 0; i < kids.Count; i++)
        {
            var child = kids[i];
            float grow = GetGrow(child);
            float main = grow > 0f && starSum > 0f
                ? leftover * (grow / starSum)
                : prefMain[i];

            if (vertical)
            {
                float prefW = PreferredWidth(child, innerW, main);
                AlignInSlot(child, originX, originY + cursor, innerW, main, prefW, main, Align, LayoutAlign.Stretch);
            }
            else
            {
                float prefH = PreferredHeight(child, main, innerH);
                AlignInSlot(child, originX + cursor, originY, main, innerH, main, prefH, LayoutAlign.Stretch, Align);
            }

            cursor += main + Gap;
        }
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        bool vertical = Orientation == Orientation.Vertical;
        float totalMain = 0f;
        float cross = 0f;
        int n = 0;

        foreach (var child in ParticipatingChildren())
        {
            var size = child.GetPreferredSize(vertical ? maxWidth : 0f, vertical ? 0f : maxHeight);
            float w = size.Width > 0 ? size.Width : child.Transform.Width;
            float h = size.Height > 0 ? size.Height : child.Transform.Height;
            if (vertical)
            {
                totalMain += h;
                cross = Math.Max(cross, w);
            }
            else
            {
                totalMain += w;
                cross = Math.Max(cross, h);
            }
            n++;
        }

        if (n > 1)
            totalMain += Gap * (n - 1);

        float width = (vertical ? cross : totalMain) + Padding.Horizontal;
        float height = (vertical ? totalMain : cross) + Padding.Vertical;

        if (MinWidth.HasValue) width = Math.Max(width, MinWidth.Value);
        if (MaxWidth.HasValue) width = Math.Min(width, MaxWidth.Value);
        if (MinHeight.HasValue) height = Math.Max(height, MinHeight.Value);
        if (MaxHeight.HasValue) height = Math.Min(height, MaxHeight.Value);
        if (maxWidth > 0) width = Math.Min(width, maxWidth);
        if (maxHeight > 0) height = Math.Min(height, maxHeight);

        return new SKSize(width, height);
    }

    private sealed class GrowBox
    {
        public float Weight;
    }
}
