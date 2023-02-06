using System.Drawing;
using System.Threading;
using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Testing.CustomElements;
using SkiaSharp;

namespace Blossom.Testing;

public class ChildrenAxis : View
{
    private AreaMarker BoundingArea;
    private Draggable DraggableParent;

    public ChildrenAxis() : base("Children axis Test") { }

    public override void Main()
    {
        DraggableParent = new Draggable()
        {
            Name = "Draggable parent",
            IsClipping = true,
            DownColor = new(240, 240, 240),
            UpColor = new(220, 220, 220),
            Transform = new(100, 100, 650, 550)
            {
                Anchor = Anchor.Top | Anchor.Left,
            },
            Style = new()
            {
                BackColor = new(220, 220, 220),
                Border = new()
                {
                    Color = SKColors.DarkGray
                }
            }
        };

        AddElement(DraggableParent);

        for (int i = 0; i < 4; i++)
        {
            float x = Random.Shared.Next(-50, 600);
            float y = Random.Shared.Next(-50, 500);

            var newEl = new Draggable()
            {
                IsClipping = false,
                Name = $"e{i}",
                Text = "Hello",
                Transform = new(x, y, Random.Shared.Next(120, 160), 65)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                },
            };

            newEl.OnDragged += ElementDragged;
            newEl.OnDropped += ElementDropped;
            newEl.TransformChanged += OnTransformChanged;

            var newChildOfChild = new Draggable()
            {
                Name = $"Child of child {i}",
                DownColor = new(240, 240, 240),
                UpColor = new(220, 220, 220),
                IsClipping = false,
                Text = "Hello",
                Transform = new(45, 45, 135, 60)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                },
                Style = new()
                {
                    BackColor = new(255, 55, 92),
                    Text = new()
                    {
                        Color = SKColors.White,
                        Size = 26,
                        Weight = 600
                    },
                    Border = new()
                    {
                        Roundness = 10
                    },
                    Shadow = new()
                    {
                        Color = new(255, 55, 92, 90),
                        OffsetY = 4,
                        SpreadX = 5,
                        SpreadY = 5
                    }
                }
            };

            DraggableParent.AddChild(newEl);
            newEl.AddChild(newChildOfChild);
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