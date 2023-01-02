using System.Globalization;
using System.Security.Cryptography;
using System.Numerics;
using System.Net.Mime;
using System;
using System.Diagnostics;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using System.Drawing;
using System.Threading;
using SkiaSharp;
using System.Linq;
using System.Collections.Generic;
using Blossom.Testing.CustomElements;

namespace Blossom.Testing;

public class ViewportTest : View
{
    private VisualElement LeftMax;
    private VisualElement LastLeft;

    private VisualElement RightMax;
    private VisualElement RightLast;

    private VisualElement TopMax;
    private VisualElement TopLast;

    private VisualElement BottomMax;
    private VisualElement BottomLast;

    private AreaMarker BoxMarker;

    public ViewportTest() : base("Viewport Test") { }

    public override void Main()
    {
        for (int i = 0; i < 5; i++)
        {
            float x = Random.Shared.Next(100, 600);
            float y = Random.Shared.Next(100, 600);

            var newEl = new Draggable()
            {
                Name = $"Draggable {i}",
                Transform = new(x, y, 120, 120)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                }
            };

            newEl.OnDropped += ElementDragged;

            AddElement(newEl);
        }

        BoxMarker = new()
        {
            Name = $"Box Marker",
            Transform = new(10, 10, 400, 400)
            {
                Anchor = Anchor.Top | Anchor.Left,
            }
        };

        float maxLeft = float.MaxValue,
            maxRight = float.MinValue,
            maxTop = float.MaxValue,
            maxBottom = float.MinValue;

        foreach (var item in this.Elements.Items)
        {
            if (item.Transform.X < maxLeft) maxLeft = item.Transform.X;
            if (item.Transform.X + item.Transform.Width > maxRight) maxRight = item.Transform.X + item.Transform.Width;
            if (item.Transform.Y < maxTop) maxTop = item.Transform.Y;
            if (item.Transform.Y + item.Transform.Height > maxBottom) maxBottom = item.Transform.Y + item.Transform.Height;
        }

        BoxMarker.Transform.X = maxLeft;
        BoxMarker.Transform.Y = maxTop;
        BoxMarker.Transform.Width = maxRight - maxLeft;
        BoxMarker.Transform.Height = maxBottom - maxTop;

        AddElement(BoxMarker);
    }

    public void ElementDragged(VisualElement element)
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var item in this.Elements.Items)
        {
            if (item == BoxMarker) continue;
            if (item.Transform.X < minX) minX = item.Transform.X;
            if (item.Transform.X + item.Transform.Width > maxX) maxX = item.Transform.X + item.Transform.Width;
            if (item.Transform.Y < minY) minY = item.Transform.Y;
            if (item.Transform.Y + item.Transform.Height > maxY) maxY = item.Transform.Y + item.Transform.Height;
        }

        BoxMarker.Transform.X = minX;
        BoxMarker.Transform.Y = minY;
        BoxMarker.Transform.Width = maxX - minX;
        BoxMarker.Transform.Height = maxY - minY;

        BoxMarker.Text = $"{(int)minX} {(int)maxX} {(int)minY} {(int)maxY}";
    }
}