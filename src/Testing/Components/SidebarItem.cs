using System;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Components
{
    public class SidebarItem : VisualElement
    {
        private readonly SKColor StandardColor = SKColors.Transparent;
        private readonly SKColor HoverColor = new SKColor(255, 255, 255, 20);

        public SidebarItem(string title)
        {
            Name = title;
            Text = title;
            ZIndex = 1;

            Style = new ElementStyle
            {
                BackColor = StandardColor,
                Text = new TextStyle 
                { 
                    Color = SKColors.White, 
                    Size = 16, 
                    Padding = 20, 
                    Alignment = TextAlign.Left 
                }
            };
            
            Transform = new Transform(0, 0, 250, 40)
            {
                Anchor = Anchor.Top | Anchor.Left,
                FixedHeight = true,
                FixedWidth = true
            };

            Events.OnMouseEnter += (s) => Style.BackColor = HoverColor;
            Events.OnMouseLeave += (s) => Style.BackColor = StandardColor;
        }
    }
}
