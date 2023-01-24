using System.Threading;
using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Testing.CustomElements;

namespace Blossom.Testing;

public class GridTest : View
{
    private AreaMarker BoundingArea;

    public GridTest() : base("Viewport Test") { }

    public override void Main()
    {
        for (int i = 0; i < 4; i++)
        {
            float x = Random.Shared.Next(240, 950);
            float y = Random.Shared.Next(170, 550);

            var newEl = new Draggable()
            {
                Name = $"e{i}",
                Transform = new(x, y, Random.Shared.Next(120, 160), 65)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                },
                Text = "Hello"
            };

            newEl.OnDragged += ElementDragged;
            newEl.OnDropped += ElementDropped;
            newEl.TransformChanged += OnTransformChanged;

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

    public void OnTransformChanged(VisualElement el, Transform tr)
    {
        ElementDragged(el);
    }

    public void ElementDropped(VisualElement element)
    {
        // var max = Elements.BoundAxis.SortIndexes.Count;
    }

    public void ElementDragged(VisualElement element)
    {
        var boundingRect = Elements.BoundAxis.GetBoundingRect();

        BoundingArea.Transform.X = boundingRect.X - 10;
        BoundingArea.Transform.Y = boundingRect.Y - 10;
        BoundingArea.Transform.Width = boundingRect.Width + 20;
        BoundingArea.Transform.Height = boundingRect.Height + 20;
    }
}