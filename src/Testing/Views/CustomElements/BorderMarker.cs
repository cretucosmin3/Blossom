using System.Diagnostics;
using System;
using System.Numerics;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.CustomElements;

public class AreaMarker : VisualElement
{
    public AreaMarker()
    {
        Style = new()
        {
            IsClipping = false,
            Border = new()
            {
                Roundness = 5,
                Width = 2f,
                Color = new(0, 0, 0, 255)
            },
            Text = new()
            {
                Size = 16,
                Color = SKColors.Black,
                Alignment = TextAlign.Center
            }
        };
    }

    public override void AddedToView() { }
}