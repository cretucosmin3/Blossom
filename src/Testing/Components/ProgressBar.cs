using System;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Components
{
    public class ProgressBar : VisualElement
    {
        private float _percentage; // 0 to 1
        private readonly VisualElement _fill;

        public ProgressBar(float percentage, SKColor color)
        {
            Name = $"ProgressBar_{Guid.NewGuid().ToString().Substring(0, 5)}";
            _percentage = Math.Clamp(percentage, 0, 1);

            Style = new ElementStyle
            {
                BackColor = SKColors.Black.WithAlpha(50), // Track color
                Border = new BorderStyle { Roundness = 4 }
            };

            _fill = new VisualElement
            {
                Name = $"{Name}_Fill",
                Style = new ElementStyle
                {
                    BackColor = color,
                    Border = new BorderStyle { Roundness = 4 }
                },
                Transform = new Transform
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Bottom,
                    X = 0, Y = 0,
                    Height = 0, // Fill parent height
                    Width = 0 // Will set in UpdateLayout
                }
            };
            AddChild(_fill);

            Transform.OnChanged += (s) => UpdateFill();
        }

        private void UpdateFill()
        {
            if (Transform.Computed.Width > 0)
            {
                _fill.Transform.Width = Transform.Computed.Width * _percentage;
                _fill.Transform.FixedWidth = true; 
            }
        }
        
        public float Percentage
        {
            get => _percentage;
            set
            {
                _percentage = Math.Clamp(value, 0, 1);
                UpdateFill();
            }
        }
    }
}
