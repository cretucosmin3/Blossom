using System;
using Blossom.Core.Visual;
using SkiaSharp;
using System.Linq;

namespace Blossom.Testing.Components
{
    public class SimpleBarChart : VisualElement
    {
        public SimpleBarChart(string title, float[] data, SKColor barColor)
        {
            Name = $"Chart_{Guid.NewGuid().ToString().Substring(0, 5)}";
            Style = new ElementStyle
            {
                BackColor = new SKColor(0, 0, 0, 40),
                Border = new BorderStyle { Roundness = 16, Width = 1, Color = new SKColor(255, 255, 255, 20) }
            };

            // Title
            AddChild(new VisualElement
            {
                Name = $"{Name}_Title",
                Text = title,
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = SKColors.White, Size = 18, Weight = 600, Padding = 20 }
                },
                Transform = new Transform { Anchor = Anchor.Top | Anchor.Left, Height = 50 }
            });

            // Bars
            float maxVal = data.Max();
            float barWidth = 30; // Consider making this dynamic if width reduces
            float spacing = 20;
            float startX = 20;
            // CORRECTED: Ensure bars fit within the default 300px height
            float bottomY = 280; 
            float maxHeight = 200; 

            for (int i = 0; i < data.Length; i++)
            {
                float h = (data[i] / maxVal) * maxHeight;
                
                var bar = new VisualElement
                {
                    Name = $"{Name}_Bar_{i}",
                    Style = new ElementStyle
                    {
                        BackColor = barColor,
                        Border = new BorderStyle { RoundnessTopLeft = 6, RoundnessTopRight = 6 }
                    },
                    Transform = new Transform(startX + (i * (barWidth + spacing)), bottomY - h, barWidth, h)
                    {
                        Anchor = Anchor.Bottom | Anchor.Left
                    }
                };

                // Hover
                bar.Events.OnMouseEnter += (s) => bar.Style.BackColor = SKColors.White;
                bar.Events.OnMouseLeave += (s) => bar.Style.BackColor = barColor;

                AddChild(bar);
            }
        }
    }
}
