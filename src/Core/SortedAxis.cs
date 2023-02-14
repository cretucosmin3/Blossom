using System.Net.Security;
using System.Reflection.Metadata.Ecma335;
using System.Net;
using System.Collections;
using System;
using System.Collections.Generic;
using Blossom.Core.Visual;
using System.Linq;
using System.Diagnostics;

namespace Blossom.Core;

public class SortedAxis
{
    public class SortedPositions
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    private readonly object lockObj = new();

    public readonly Dictionary<VisualElement, SortedPositions> SortIndexes = new();

    public List<VisualElement> Lefts = new();
    public List<VisualElement> Rights = new();
    public List<VisualElement> Tops = new();
    public List<VisualElement> Bottoms = new();

    public void AddElement(VisualElement element)
    {
        SortIndexes.Add(element, new()
        {
            Left = Lefts.Count,
            Right = Rights.Count,
            Top = Tops.Count,
            Bottom = Bottoms.Count,
        });

        Lefts.Insert(Lefts.Count, element);
        Rights.Insert(Rights.Count, element);
        Tops.Insert(Tops.Count, element);
        Bottoms.Insert(Bottoms.Count, element);

        Reposition(element);

        element.TransformChanged += ElementMoved;
    }

    public void RemoveElement(VisualElement element)
    {
        if (!SortIndexes.ContainsKey(element)) return;

        var Indexes = SortIndexes[element];

        Lefts.RemoveAt(Indexes.Left);
        Rights.RemoveAt(Indexes.Right);
        Tops.RemoveAt(Indexes.Top);
        Bottoms.RemoveAt(Indexes.Bottom);

        SortIndexes.Remove(element);

        element.TransformChanged -= ElementMoved;
    }

    private void ElementMoved(VisualElement element, Transform transform)
    {
        lock (lockObj)
        {
            Reposition(element);
        }
    }

    private void Reposition(VisualElement element)
    {
        if (!SortIndexes.ContainsKey(element))
            return;

        var Indexes = SortIndexes[element];

        // Left border moved left
        bool movedLeft = HasIndex(Lefts, Indexes.Left - 1) &&
            element.Transform.Left < Lefts[Indexes.Left - 1].Transform.Left;

        // Left border moved right
        bool movedLeftReversed = HasIndex(Lefts, Indexes.Left + 1) &&
            element.Transform.Left > Lefts[Indexes.Left + 1].Transform.Left;

        // Right border moved right
        bool movedRight = HasIndex(Rights, Indexes.Right + 1) &&
            element.Transform.Right > Rights[Indexes.Right + 1].Transform.Right;

        // Right border moved left
        bool movedRightReversed = HasIndex(Rights, Indexes.Right - 1) &&
            element.Transform.Right < Rights[Indexes.Right - 1].Transform.Right;

        // Top border moved up
        bool movedUp = HasIndex(Tops, Indexes.Top - 1) &&
            element.Transform.Top < Tops[Indexes.Top - 1].Transform.Top;

        // Top border moved down
        bool movedUpReversed = !movedUp && HasIndex(Tops, Indexes.Top + 1) &&
            element.Transform.Top > Tops[Indexes.Top + 1].Transform.Top;

        // Bottom border moved down
        bool movedDown = HasIndex(Bottoms, Indexes.Bottom + 1) &&
            element.Transform.Bottom > Bottoms[Indexes.Bottom + 1].Transform.Bottom;

        // Bottom border moved up
        bool movedDownReversed = !movedDown && HasIndex(Bottoms, Indexes.Bottom - 1) &&
            element.Transform.Bottom < Bottoms[Indexes.Bottom - 1].Transform.Bottom;

        if (movedLeft || movedLeftReversed)
        {
            Indexes.Left = FindAndShiftPositions(
                element, Lefts,
                e => e.Transform.Left,
                (e, val) => e.Left += val,
                Indexes.Left, !movedLeft && movedLeftReversed);
        }

        if (movedRight || movedRightReversed)
        {
            Indexes.Right = FindAndShiftPositions(
                element, Rights,
                e => e.Transform.Right,
                (e, val) => e.Right += val,
                Indexes.Right,
                movedRight || !movedRightReversed);
        }

        if (movedUp || movedUpReversed)
        {
            Indexes.Top = FindAndShiftPositions(
                element, Tops,
                e => e.Transform.Top,
                (e, val) => e.Top += val,
                Indexes.Top, !movedUp && movedUpReversed);
        }

        if (movedDown || movedDownReversed)
        {
            Indexes.Bottom = FindAndShiftPositions(
                element, Bottoms,
                e => e.Transform.Bottom,
                (e, val) => e.Bottom += val,
                Indexes.Bottom, movedDown && !movedDownReversed);
        }
    }

    private int FindAndShiftPositions(
        VisualElement e,
        List<VisualElement> list,
        Func<VisualElement, float> propertySelector,
        Action<SortedPositions, int> positionAttributor,
        int currentIndex, bool linear = false)
    {
        int newIndex = currentIndex + (linear ? 1 : -1);

        Func<bool> whileCheck = linear ?
        () => newIndex < list.Count && propertySelector(e) > propertySelector(list[newIndex]) :
        () => newIndex >= 0 && propertySelector(e) < propertySelector(list[newIndex]);

        while (whileCheck())
        {
            positionAttributor(SortIndexes[list[newIndex]], linear ? -1 : 1);
            newIndex += linear ? 1 : -1;
        }

        newIndex += linear ? -1 : 1;
        list.RemoveAt(currentIndex);
        list.Insert(newIndex, e);
        return newIndex;
    }

    private static bool HasIndex(List<VisualElement> list, int index)
    {
        return index >= 0 && index < list.Count;
    }

    public Rect GetBoundingRect()
    {
        lock (lockObj)
        {
            return new Rect(
                Lefts[0].Transform.X,
                Tops[0].Transform.Y,
                Rights.Last().Transform.Right - Lefts[0].Transform.X,
                Bottoms.Last().Transform.Bottom - Tops[0].Transform.Y
            );
        }
    }

    public VisualElement[] GetNeighbours(VisualElement element)
    {
        if (element == null)
            return Array.Empty<VisualElement>();

        HashSet<VisualElement> Result = new();

        var Indexes = SortIndexes[element];

        if (HasIndex(Lefts, Indexes.Left - 1) && !Result.Contains(Lefts[Indexes.Left - 1]))
            Result.Add(Lefts[Indexes.Left - 1]);

        if (HasIndex(Lefts, Indexes.Left + 1) && !Result.Contains(Lefts[Indexes.Left + 1]))
            Result.Add(Lefts[Indexes.Left + 1]);

        if (HasIndex(Rights, Indexes.Right - 1) && !Result.Contains(Rights[Indexes.Right - 1]))
            Result.Add(Rights[Indexes.Right - 1]);

        if (HasIndex(Rights, Indexes.Right + 1) && !Result.Contains(Rights[Indexes.Right + 1]))
            Result.Add(Rights[Indexes.Right + 1]);

        if (HasIndex(Tops, Indexes.Top - 1) && !Result.Contains(Tops[Indexes.Top - 1]))
            Result.Add(Tops[Indexes.Top - 1]);

        if (HasIndex(Tops, Indexes.Top + 1) && !Result.Contains(Tops[Indexes.Top + 1]))
            Result.Add(Tops[Indexes.Top + 1]);

        if (HasIndex(Bottoms, Indexes.Bottom - 1) && !Result.Contains(Bottoms[Indexes.Bottom - 1]))
            Result.Add(Bottoms[Indexes.Bottom - 1]);

        if (HasIndex(Bottoms, Indexes.Bottom + 1) && !Result.Contains(Bottoms[Indexes.Bottom + 1]))
            Result.Add(Bottoms[Indexes.Bottom + 1]);

        return Result.ToArray();
    }
}