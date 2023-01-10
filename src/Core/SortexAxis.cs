using System.Net.Security;
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
    private class SortedPositions
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

    private readonly Dictionary<VisualElement, SortedPositions> SortIndexes = new();

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
        Reposition(element);
    }

    private void Reposition(VisualElement element)
    {
        var Indexes = SortIndexes[element];

        // Left border moved left
        bool movedLeft = HasIndex(Lefts, Indexes.Left - 1) &&
            element.Transform.Left < Lefts[Indexes.Left - 1].Transform.Left;

        // Left border moved right
        bool movedLeftReversed = HasIndex(Lefts, Indexes.Left + 1) &&
            element.Transform.Left > Lefts[Indexes.Left + 1].Transform.Left;

        // Right border moved right
        // bool movedRight = HasIndex(Rights, Indexes.Right + 1) &&
        //     element.Transform.Right > Rights[Indexes.Right + 1].Transform.Right;

        // // Right border moved left
        // bool movedRightReversed = HasIndex(Rights, Indexes.Right - 1) &&
        //     element.Transform.Right < Rights[Indexes.Right - 1].Transform.Right;

        // Top border moved up
        // bool movedUp = firstTime || (HasIndex(Tops, Indexes.Top - 1) &&
        //     element.Transform.Top < Tops[Indexes.Top - 1].Transform.Top);

        // Top border moved down
        // bool movedUpR = firstTime || (!movedUp && HasIndex(Tops, Indexes.Top + 1) &&
        //     element.Transform.Top > Tops[Indexes.Top + 1].Transform.Top);

        // Bottom border moved down
        // bool movedDown = firstTime || (HasIndex(Bottoms, Indexes.Bottom + 1) &&
        //     element.Transform.Bottom > Bottoms[Indexes.Bottom + 1].Transform.Bottom);

        // Bottom border moved up
        // bool movedDownR = firstTime || (!movedDown && HasIndex(Bottoms, Indexes.Bottom - 1) &&
        //     element.Transform.Bottom < Bottoms[Indexes.Bottom - 1].Transform.Bottom);

        if (movedLeft)
        {
            int newIndex = Indexes.Left - 1;
            while (newIndex >= 0 && element.Transform.Left < Lefts[newIndex].Transform.Left)
            {
                newIndex--;
            }

            newIndex++;
            Lefts.RemoveAt(Indexes.Left);
            Lefts.Insert(newIndex, element);
            Indexes.Left = newIndex;
        }
        else if (movedLeftReversed)
        {
            int newIndex = Indexes.Left + 1;
            while (newIndex < Lefts.Count && element.Transform.Left > Lefts[newIndex].Transform.Left)
            {
                newIndex++;
            }

            newIndex--;

            Lefts.RemoveAt(Indexes.Left);
            Lefts.Insert(newIndex, element);
            Indexes.Left = newIndex;
        }
        // else if (movedRight)
        // {
        //     int newIndex = Indexes.Right + 1;
        //     while (newIndex < Rights.Count && element.Transform.Right > Rights[newIndex].Transform.Right)
        //     {
        //         newIndex++;
        //     }
        //     newIndex--; // move back one position because we went too far
        //     Rights.RemoveAt(Indexes.Right);
        //     Rights.Insert(newIndex, element);
        //     Indexes.Right = newIndex;
        // }
        // else if (movedRightReversed)
        // {
        //     int newIndex = Indexes.Right - 1;
        //     while (newIndex >= 0 && element.Transform.Right < Rights[newIndex].Transform.Right)
        //     {
        //         newIndex--;
        //     }
        //     newIndex++; // move back one position because we went too far
        //     Rights.RemoveAt(Indexes.Right);
        //     Rights.Insert(newIndex, element);
        //     Indexes.Right = newIndex;
        // }
    }

    private static bool HasIndex(List<VisualElement> list, int index)
    {
        return index >= 0 && index < list.Count;
    }

    public Rect GetBoundingRect()
    {
        return new Rect(
            Lefts[0].Transform.X,
            Tops[0].Transform.Y,
            Rights.Last().Transform.Right,
            Bottoms.Last().Transform.Bottom
        );
    }

    // public VisualElement[] GetNeighbours(VisualElement element)
    // {

    // }
}