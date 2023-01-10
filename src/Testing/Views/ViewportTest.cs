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
        for (int i = 0; i < 6; i++)
        {
            float x = Random.Shared.Next(100, 800);
            float y = Random.Shared.Next(100, 600);

            var newEl = new Draggable()
            {
                Name = $"e{i}",
                Transform = new(x, y, Random.Shared.Next(120, 200), 120)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                },
                Text = $"{i}"
            };

            newEl.OnDragged += ElementDragged;

            AddElement(newEl);
        }
    }

    public void ElementDragged(VisualElement element)
    {
        // Rect BoundingRect = this.Elements.BoundAxis.GetBoundingRect();

        for (int i = 0; i < Elements.BoundAxis.Lefts.Count; i++)
        {
            var leftElement = Elements.BoundAxis.Lefts[i];
            leftElement.Text = $"{leftElement.Name} : {i}";

            leftElement.Style.Border.Width = 1f;
            leftElement.Style.Border.Color = new(0, 0, 0, 255);

            if (i == 0)
            {
                leftElement.Style.Border.Color = SKColors.Red;
                leftElement.Style.Border.Width = 2;
            }
            else if (i == Elements.BoundAxis.Lefts.Count - 1)
            {
                leftElement.Style.Border.Color = SKColors.Green;
                leftElement.Style.Border.Width = 2;
            }
        }

        // for (int i = 1; i < this.Elements.BoundAxis.Tops.Count; i++)
        // {
        //     var rightElement = this.Elements.BoundAxis.Tops[i];
        //     rightElement.Text += $" {i}";
        // }
    }
}