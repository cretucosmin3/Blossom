using System;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Components
{
    public class GlassButton : VisualElement
    {
        private readonly SKColor _baseColor;
        private readonly SKColor _hoverColor;
        private readonly SKColor _pressColor;
        private readonly SKColor _borderColor;
        private readonly SKColor _borderHoverColor;

        public Action? OnClick;

        public GlassButton(string text, SKColor tintColor, float width = 200, float height = 50)
        {
            Name = $"GlassButton_{text}_{Guid.NewGuid().ToString().Substring(0, 4)}";
            Text = text;
            IsClipping = false;

            _baseColor = new SKColor(tintColor.Red, tintColor.Green, tintColor.Blue, 30); // 12% opacity tint
            _hoverColor = new SKColor(tintColor.Red, tintColor.Green, tintColor.Blue, 65); // 25% opacity tint
            _pressColor = new SKColor(tintColor.Red, tintColor.Green, tintColor.Blue, 100); // 40% opacity tint

            _borderColor = new SKColor(255, 255, 255, 100);
            _borderHoverColor = new SKColor(255, 255, 255, 200);

            // Glassmorphic Style
            Style = new ElementStyle
            {
                BackdropBlur = 15f,
                BackColor = _baseColor,
                BackgroundShader = BackgroundShaderType.GlassRefraction,
                BackgroundShaderColor = new SKColor(255, 255, 255, 15),
                Border = new BorderStyle
                {
                    Color = _borderColor,
                    Width = 1.5f,
                    Roundness = 10
                },
                BorderEffect = BorderEffectType.GlassReflection,
                BorderEffectSpeed = 1.2f,
                Text = new TextStyle
                {
                    Color = SKColors.White,
                    Size = 15,
                    Weight = 600,
                    Alignment = TextAlign.Center
                },
                Shadow = new ShadowStyle
                {
                    Color = new SKColor(0, 0, 0, 80),
                    OffsetX = 0,
                    OffsetY = 4,
                    SpreadX = 8,
                    SpreadY = 8
                }
            };

            Transform = new Transform(0, 0, width, height);

            // Hook Hover & Press Events
            Events.OnMouseEnter += (s) =>
            {
                Style.BackColor = _hoverColor;
                Style.Border.Color = _borderHoverColor;
                Style.Border.Width = 2.0f;
                Transform.ScaleX = 1.04f;
                Transform.ScaleY = 1.04f;
            };

            Events.OnMouseLeave += (s) =>
            {
                Style.BackColor = _baseColor;
                Style.Border.Color = _borderColor;
                Style.Border.Width = 1.5f;
                Transform.ScaleX = 1.0f;
                Transform.ScaleY = 1.0f;
            };

            Events.OnMouseDown += (s, e) =>
            {
                Style.BackColor = _pressColor;
                Transform.ScaleX = 0.97f;
                Transform.ScaleY = 0.97f;
            };

            Events.OnMouseUp += (s, e) =>
            {
                Style.BackColor = _hoverColor;
                Transform.ScaleX = 1.04f;
                Transform.ScaleY = 1.04f;
                OnClick?.Invoke();
            };
        }
    }
}
