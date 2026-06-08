using System;
using System.Collections.Generic;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class ComponentPaletteModal : VisualElement
{
    public Action<string>? OnComponentSelected;
    public Action? OnCloseRequested;

    private readonly string[] _componentTypes = new[]
    {
        "Container Box",
        "Label Text",
        "Classic Button",
        "Neon Matrix Button",
        "Progress Bar",
        "Slider Range",
        "Checkbox State",
        "Switch Toggle"
    };

    private readonly string[] _componentDescriptions = new[]
    {
        "Basic visual card/box",
        "Static text label element",
        "Interactive hover/press button",
        "Neon glowing button with animations",
        "Percentage based loading bar",
        "Interactive slider selector",
        "Toggle check box selector",
        "Sliding switch selector"
    };

    public ComponentPaletteModal()
    {
        Name = "ComponentPaletteModal";

        Style = new ElementStyle
        {
            BackColor = new SKColor(16, 20, 30, 245), // Semi-transparent overlay
            Border = new BorderStyle { Width = 1.5f, Color = new SKColor(56, 189, 248), Roundness = 12f }, // cyan glow border
            Shadow = new ShadowStyle { Color = new SKColor(56, 189, 248, 80), SpreadX = 12, SpreadY = 12 }
        };

        Transform = new Transform(0, 0, 520, 320)
        {
            FixedWidth = true,
            FixedHeight = true
        };

        // Title
        var title = new VisualElement
        {
            Name = "PaletteTitle",
            Text = "👾 CHOOSE COMPONENT TO SPAWN",
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 15, Weight = 800, Alignment = TextAlign.Center }
            },
            Transform = new Transform(0, 15, 520, 25)
        };
        AddChild(title);

        // Subtitle instructions
        var subtitle = new VisualElement
        {
            Name = "PaletteSubtitle",
            Text = "Select an item to place it in the active workspace view. Press ESC or Tab to close.",
            Style = new ElementStyle
            {
                Text = new TextStyle { Color = new SKColor(100, 116, 139), Size = 10, Weight = 500, Alignment = TextAlign.Center }
            },
            Transform = new Transform(0, 40, 520, 20)
        };
        AddChild(subtitle);

        // Dynamic buttons grid
        float startX = 25f;
        float startY = 75f;
        float cardW = 225f;
        float cardH = 45f;
        float gapX = 20f;
        float gapY = 12f;

        for (int i = 0; i < _componentTypes.Length; i++)
        {
            int row = i / 2;
            int col = i % 2;

            float x = startX + col * (cardW + gapX);
            float y = startY + row * (cardH + gapY);

            string typeName = _componentTypes[i];
            string typeDesc = _componentDescriptions[i];

            var card = new VisualElement
            {
                Name = $"PaletteCard_{i}",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59, 150),
                    Border = new BorderStyle { Width = 1, Color = new SKColor(71, 85, 105), Roundness = 8f }
                },
                Transform = new Transform(x, y, cardW, cardH)
                {
                    FixedWidth = true,
                    FixedHeight = true
                }
            };

            var nameEl = new VisualElement
            {
                Name = $"PaletteCardName_{i}",
                Text = typeName,
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = SKColors.White, Size = 12, Weight = 700, Alignment = TextAlign.Left }
                },
                Transform = new Transform(12, 6, cardW - 24, 18)
            };

            var descEl = new VisualElement
            {
                Name = $"PaletteCardDesc_{i}",
                Text = typeDesc,
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(148, 163, 184), Size = 9, Weight = 500, Alignment = TextAlign.Left }
                },
                Transform = new Transform(12, 24, cardW - 24, 16)
            };

            card.AddChild(nameEl);
            card.AddChild(descEl);

            card.Events.OnMouseEnter += (s) =>
            {
                card.Style.BackColor = new SKColor(56, 189, 248, 40);
                card.Style.Border.Color = new SKColor(56, 189, 248);
                nameEl.Style.Text.Color = new SKColor(56, 189, 248);
                card.Transform.ScaleX = 1.02f;
                card.Transform.ScaleY = 1.02f;
            };

            card.Events.OnMouseLeave += (s) =>
            {
                card.Style.BackColor = new SKColor(30, 41, 59, 150);
                card.Style.Border.Color = new SKColor(71, 85, 105);
                nameEl.Style.Text.Color = SKColors.White;
                card.Transform.ScaleX = 1.0f;
                card.Transform.ScaleY = 1.0f;
            };

            card.Events.OnMouseUp += (s, e) =>
            {
                OnComponentSelected?.Invoke(typeName);
            };

            AddChild(card);
        }
    }
}
