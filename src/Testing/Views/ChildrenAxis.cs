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
            DownColor = new(245, 245, 245),
            UpColor = new(235, 235, 235),
            Transform = new(100, 100, 750, 550)
            {
                Anchor = Anchor.Top | Anchor.Left,
            },
            Style = new()
            {
                BackColor = new(235, 235, 235),
                Border = new()
                {
                    Color = SKColors.Black,
                    Width = 1,
                    Roundness = 5
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
                IsClipping = Random.Shared.Next(100) > 50,
                // IsClipping = false,
                Name = $"e{i}",
                Text = "Child",
                Transform = new(x, y, Random.Shared.Next(120, 160), 45)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                },
            };

            newEl.Style.Border = new()
            {
                Color = SKColors.White,
                Width = 1,
                Roundness = 10
            };

            newEl.Style.Shadow = new()
            {
                Color = new(0, 0, 0, 200),
                OffsetY = 8,
                OffsetX = 6
            };

            newEl.OnDragged += ElementDragged;
            newEl.OnDropped += ElementDropped;
            newEl.TransformChanged += OnTransformChanged;

            var newChildOfChild = new Draggable()
            {
                Name = $"Child of child {i}",
                DownColor = new(255, 80, 130),
                UpColor = new(255, 55, 92),
                IsClipping = false,
                Text = "Grand Child",
                Transform = new(45, 25, 180, 55)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                },
                Style = new()
                {
                    BackColor = new(255, 55, 92),
                    Text = new()
                    {
                        Color = SKColors.White,
                        Size = 22,
                        Weight = 400
                    },
                    Border = new()
                    {
                        Color = SKColors.White,
                        Width = 1,
                        Roundness = 10
                    },
                    Shadow = new()
                    {
                        Color = new(0, 0, 0, 200),
                        OffsetY = 8,
                        OffsetX = 6
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
        var boundingRect = DraggableParent.BoundingRect;

        BoundingArea.Transform.X = boundingRect.X - 10;
        BoundingArea.Transform.Y = boundingRect.Y - 10;
        BoundingArea.Transform.Width = boundingRect.Width + 20;
        BoundingArea.Transform.Height = boundingRect.Height + 20;
    }
}