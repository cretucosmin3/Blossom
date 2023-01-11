using System;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;
using System.Collections.Generic;
using Blossom.Testing.CustomElements;

namespace Blossom.Testing;

public class ViewportTest : View
{
    private VisualElement TestingElement;
    private List<VisualElement> ListToTest = new();

    private VisualElement Top;
    private VisualElement Left;
    private VisualElement Right;
    private VisualElement Bottom;

    public ViewportTest() : base("Viewport Test") { }

    public override void Main()
    {
        for (int i = 0; i < 5; i++)
        {
            float x = Random.Shared.Next(100, 500);
            float y = Random.Shared.Next(100, 500);

            var newEl = new Draggable()
            {
                Name = $"e{i}",
                Transform = new(x, y, Random.Shared.Next(120, 160), 80)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                },
                Text = $"{i}"
            };

            newEl.OnDragged += ElementDragged;
            newEl.OnDropped += ElementDropped;

            AddElement(newEl);
        }
    }

    public void ElementDropped(VisualElement element)
    {
        var max = Elements.BoundAxis.SortIndexes.Count;
        foreach (var (el, indx) in Elements.BoundAxis.SortIndexes)
        {
            el.Text = $"{indx.Left}";

            if (indx.Left == 0)
            {
                el.Style.Border.Color = SKColors.Red;
                el.Style.Border.Width = 3;
            }
            else if (indx.Left == max - 1)
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
        foreach (var (el, indx) in Elements.BoundAxis.SortIndexes)
        {
            el.Text = $"{indx.Left}";
        }
    }
}