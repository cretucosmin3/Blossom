using System.Threading;
using System;
using Blossom.Core;
using Blossom.Core.Visual;
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
            float x = Random.Shared.Next(240, 950);
            float y = Random.Shared.Next(170, 550);

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

        foreach (var n in Elements.Items)
        {
            if (n.Name != "Bounding Area")
                SpinElementAnimation(n, 0.0005f, Random.Shared.Next(50, 150));
        }
    }

    public void SpinElementAnimation(VisualElement eA, float Speed, float R)
    {
        new Thread(() =>
        {
            var x_0 = eA.Transform.X;
            var y_0 = eA.Transform.Y;

            Speed *= Random.Shared.NextSingle() > 0.5f ? 1 : -1;

            Thread.Sleep(Random.Shared.Next(100, 1500));

            while (true)
            {
                for (double t = 0; t < 2 * Math.PI; t += Speed)
                {
                    var x = (R * Math.Cos(t)) + x_0;
                    var y = (R * Math.Sin(t)) + y_0;

                    this.RenderChanges(() =>
                    {
                        eA.Transform.X = (float)x;
                        eA.Transform.Y = (float)y;
                    });
                }

                Thread.Sleep(1);
            }
        }).Start();
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