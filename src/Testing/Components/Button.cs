using System;
using Blossom.Core.Visual;
using Blossom.Core.Input; // Needed for MouseEventArgs if I were explicit, but lambda inference works
using SkiaSharp;

namespace Blossom.Testing.Components
{
    public class Button : VisualElement
    {
        private readonly SKColor _normalColor;
        private readonly SKColor _hoverColor;
        private readonly SKColor _pressColor;
        public Action? OnClick; // Nullable to fix CS8618

        public Button(string text, SKColor color)
        {
            Name = $"Button_{text}";
            Text = text;
            _normalColor = color;
            
            // Calculate hover/press colors (lighter/darker)
            _hoverColor = new SKColor(
                (byte)Math.Min(255, color.Red + 20),
                (byte)Math.Min(255, color.Green + 20),
                (byte)Math.Min(255, color.Blue + 20),
                color.Alpha);
                
            _pressColor = new SKColor(
                (byte)Math.Max(0, color.Red - 20),
                (byte)Math.Max(0, color.Green - 20),
                (byte)Math.Max(0, color.Blue - 20),
                color.Alpha);

            Style = new ElementStyle
            {
                BackColor = _normalColor,
                Border = new BorderStyle 
                { 
                    Width = 0, 
                    Color = SKColors.Transparent,
                    Roundness = 8 
                },
                Shadow = new ShadowStyle 
                { 
                    Color = SKColors.Black.WithAlpha(50), 
                    SpreadX = 0, 
                    SpreadY = 2, 
                    OffsetX = 0, 
                    OffsetY = 2 
                },
                Text = new TextStyle
                {
                    Color = SKColors.White,
                    Size = 14,
                    Weight = 600,
                    Alignment = TextAlign.Center,
                    Padding = 0
                }
            };

            // Events
            // OnMouseEnter is Action<VisualElement> -> 1 arg
            Events.OnMouseEnter += (s) => 
            {
                Style.BackColor = _hoverColor;
                Transform.ScaleX = 1.05f;
                Transform.ScaleY = 1.05f;
            };

            // OnMouseLeave is Action<VisualElement> -> 1 arg
            Events.OnMouseLeave += (s) => 
            {
                Style.BackColor = _normalColor;
                Transform.ScaleX = 1.0f;
                Transform.ScaleY = 1.0f;
            };
            
            // OnMouseDown is Action<object, MouseEventArgs> -> 2 args
            Events.OnMouseDown += (s, e) =>
            {
                Style.BackColor = _pressColor;
                Transform.ScaleX = 0.98f;
                Transform.ScaleY = 0.98f;
            };
            
            // OnMouseUp is Action<object, MouseEventArgs> -> 2 args
            Events.OnMouseUp += (s, e) =>
            {
                Style.BackColor = _hoverColor;
                Transform.ScaleX = 1.05f;
                Transform.ScaleY = 1.05f;
                OnClick?.Invoke();
            };
        }
    }
}
