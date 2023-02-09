using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.CustomElements;

public class AreaMarker : VisualElement
{
    public AreaMarker()
    {
        Style = new()
        {
            Border = new()
            {
                Roundness = 5,
                Width = 1f,
                Color = new(0, 0, 0, 100),
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