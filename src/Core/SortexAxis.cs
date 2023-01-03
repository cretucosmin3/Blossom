using System.Reflection.Metadata.Ecma335;
using System.Net;
using System.Collections;
using System;
using System.Collections.Generic;
using Blossom.Core.Visual;
using System.Linq;

namespace Blossom.Core;

public class SortedAxis
{
    private struct SortedPositions
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    private enum Direction
    {
        Left,
        Right,
        Top,
        Bottom
    }

    private readonly Dictionary<VisualElement, SortedPositions> SortIndexex = new();

    private readonly List<VisualElement> Lefts = new();
    private readonly List<VisualElement> Rights = new();
    private readonly List<VisualElement> Tops = new();
    private readonly List<VisualElement> Bottoms = new();

    public void AddElement(VisualElement element)
    {
        SortIndexex.Add(element, new());

        Lefts.Add(element);
        Rights.Add(element);
        Tops.Add(element);
        Bottoms.Add(element);
    }

    public void RemoveElement(VisualElement element)
    {
        if (!SortIndexex.ContainsKey(element)) return;

        var Indexes = SortIndexex[element];

        Lefts.RemoveAt(Indexes.Left);
        Rights.RemoveAt(Indexes.Right);
        Tops.RemoveAt(Indexes.Top);
        Bottoms.RemoveAt(Indexes.Bottom);

        SortIndexex.Remove(element);
    }

    public void ApplySort()
    {
        if (SortIndexex.Count > 0) return;

        Lefts.Sort((p, q) => p.Transform.X.CompareTo(q.Transform.X));
        Rights.Sort((p, q) => p.Transform.Right.CompareTo(q.Transform.Right));
        Tops.Sort((p, q) => p.Transform.Top.CompareTo(q.Transform.Top));
        Bottoms.Sort((p, q) => p.Transform.Bottom.CompareTo(q.Transform.Bottom));
    }

    public void Reposition(VisualElement element)
    {
        var Indexes = SortIndexex[element];
    }

    private Direction FindDirections(VisualElement element)
    {
        Direction directions = default;

        var Indexes = SortIndexex[element];

        bool movedLeft = HasIndex(Lefts, Indexes.Left - 1) &&
            element.Transform.X < Lefts[Indexes.Left - 1].Transform.X;

        bool movedRight = HasIndex(Rights, Indexes.Right + 1) &&
            element.Transform.Right > Rights[Indexes.Right + 1].Transform.Right;

        bool movedUp = HasIndex(Rights, Indexes.Top - 1) &&
            element.Transform.Top < Rights[Indexes.Top - 1].Transform.Top;

        bool movedDown = HasIndex(Rights, Indexes.Bottom + 1) &&
            element.Transform.Bottom > Rights[Indexes.Bottom - 1].Transform.Bottom;

        return directions;
    }

    private bool HasIndex(List<VisualElement> list, int index)
    {
        return index >= 0 && index < list.Count;
    }

    // public VisualElement[] GetNeighbours(VisualElement element)
    // {
    //     throw NotImplementedException;
    // }
}