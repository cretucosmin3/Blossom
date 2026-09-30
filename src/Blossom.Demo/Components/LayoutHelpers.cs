using Blossom.Core.Visual;
using Blossom.Primitives;
using SkiaSharp;

namespace Blossom.Testing.Components;

internal static class DemoLayout
{
    public static VisualElement GrowSpacer(string name = "Spacer")
    {
        var spacer = new VisualElement
        {
            Name = name,
            IsClickthrough = true
        };
        spacer.Style.BackColor = SKColors.Transparent;
        spacer.Style.Border.Width = 0;
        spacer.Style.Shadow = null!;
        Stack.SetGrow(spacer, 1f);
        return spacer;
    }

    public static void Panel(VisualElement el, SKColor back, float roundness, SKColor? border = null)
    {
        el.Style.BackColor = back;
        el.Style.Border = new BorderStyle
        {
            Width = 1,
            Color = border ?? new SKColor(58, 58, 58),
            Roundness = roundness
        };
    }
}
