using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class Container : VisualElement
{
    public Container()
    {
        Style = new ElementStyle
        {
            BackColor = new SKColor(38, 38, 38), // Gray 800
            Border = new BorderStyle
            {
                Width = 1,
                Color = new SKColor(58, 58, 58), // Gray 700
                Roundness = 8
            },
            Shadow = new ShadowStyle
            {
                Color = SKColors.Black.WithAlpha(40),
                SpreadX = 0,
                SpreadY = 2,
                OffsetX = 0,
                OffsetY = 2
            }
        };
    }

    public Container(SKColor backColor, float roundness = 8f) : this()
    {
        Style.BackColor = backColor;
        Style.Border.Roundness = roundness;
    }

    /// <summary>
    /// Position and size a child relative to this container (absolute frame helper).
    /// </summary>
    public void PlaceChild(VisualElement child, float localX, float localY, float width, float height)
    {
        child.Transform.SetAbsoluteFrame(
            Transform.Computed.X + Padding.Left + localX,
            Transform.Computed.Y + Padding.Top + localY,
            width,
            height);
    }
}
