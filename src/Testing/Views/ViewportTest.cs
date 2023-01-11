using System;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;
using System.Collections.Generic;
using Blossom.Testing.CustomElements;

namespace Blossom.Testing;

public class ViewportTest : View
{
    private AreaMarker BoundingArea;

    public ViewportTest() : base("Viewport Test") { }

    public override void Main()
    {
        for (int i = 0; i < 15; i++)
        {
            float x = Random.Shared.Next(150, 750);
            float y = Random.Shared.Next(100, 550);

            var newEl = new Draggable()
            {
                Name = $"e{i}",
                Transform = new(x, y, Random.Shared.Next(120, 160), 65)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                },
                Text = $"{i}"
            };

            newEl.OnDragged += ElementDragged;
            newEl.OnDropped += ElementDropped;

            AddElement(newEl);
        }

        BoundingArea = new()
        {
            Name = "Bounding Area",
            Transform = new(150, 150, 100, 100)
            {
                Anchor = Anchor.Left | Anchor.Top
            }
        };

        AddElement(BoundingArea);
    }

    public void ElementDropped(VisualElement element)
    {
        var max = Elements.BoundAxis.SortIndexes.Count;
        foreach (var (el, indx) in Elements.BoundAxis.SortIndexes)
        {
            el.Text = $"{indx.Right}";

            if (indx.Right == 0)
            {
                el.Style.Border.Color = SKColors.Red;
                el.Style.Border.Width = 3;
            }
            else if (indx.Right == max - 1)
            {
                el.Style.Border.Color = SKColors.Green;
                el.Style.Border.Width = 3;
            }
            else
            {
                el.Style.Border.Width = 1f;
                el.Style.Border.Color = new(0, 0, 0, 255);
            }
        }
    }

    public void ElementDragged(VisualElement element)
    {
        var boundingRect = Elements.BoundAxis.GetBoundingRect();

        foreach (var (el, indx) in Elements.BoundAxis.SortIndexes)
        {
            el.Text = $"{indx.Right}";
        }

        var neighbours = Elements.BoundAxis.GetNeighbours(element);

        foreach (var item in Elements.Items)
        {
            if (item.Name != element.Name)
            {
                item.Style.Border.Color = new(0, 0, 0, 255);
                item.Style.Border.Width = 1;
            }
        }

        foreach (var neighbour in neighbours)
        {
            neighbour.Style.Border.Color = SKColors.Purple;
            neighbour.Style.Border.Width = 3;
        }

        BoundingArea.Transform.X = boundingRect.X - 10;
        BoundingArea.Transform.Y = boundingRect.Y - 10;
        BoundingArea.Transform.Width = boundingRect.Width + 20;
        BoundingArea.Transform.Height = boundingRect.Height + 20;
    }
}