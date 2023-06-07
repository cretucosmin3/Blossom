using System.Drawing;
using System.Threading;
using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Testing.CustomElements;
using SkiaSharp;

namespace Blossom.Testing;

public class RenderCacheView : View
{
    public RenderCacheView() : base("Render Cache View") { }

    public override void Main()
    {
        var GroupParent1 = new VisualElement()
        {
            Name = "Group parent 1",
            Transform = new(700, 100, 300, 300),
            Style = new()
            {
                BackColor = SKColors.LightGray,
                Border = new()
                {
                    Roundness = 3,
                },
                Shadow = new()
                {
                    Color = new(0, 0, 0, 75),
                    SpreadX = 4,
                    SpreadY = 4,
                    OffsetY = 4,
                    OffsetX = 0
                },
            },
        };

        var GroupParent2 = new VisualElement()
        {
            Name = "Group parent 2",
            Transform = new(15, 60, 650, 600),
            Style = new()
            {
                BackColor = SKColors.LightGray,
                Border = new()
                {
                    Roundness = 3,
                },
                Shadow = new()
                {
                    Color = new(0, 0, 0, 75),
                    SpreadX = 4,
                    SpreadY = 4,
                    OffsetY = 4,
                    OffsetX = 0
                },
            },
        };

        var button1Group1 = new Button()
        {
            Name = "Button 1 Group 1",
            Transform = new(15, 15, 100, 60),
        };

        var button2Group1 = new Button()
        {
            Name = "Button 2 Group 1",
            Transform = new(165, 45, 100, 120),
        };

        var button3Group1 = new Button()
        {
            Name = "Button 3 Group 1",
            Transform = new(30, 210, 145, 70),
        };

        AddElement(GroupParent1);
        AddElement(GroupParent2);

        GroupParent1.AddChild(button1Group1);
        GroupParent1.AddChild(button2Group1);
        GroupParent1.AddChild(button3Group1);

        // Add group 2 grid of elements to click

        // Fixed number of buttons in the grid.
        int rows = 26;
        int cols = 26;

        // Spacing between buttons.
        int spacing = 5;

        // Calculate the width and height for each button.
        int buttonWidth = (int)((GroupParent2.Transform.Computed.Width - (cols - 1) * spacing) / cols);
        int buttonHeight = (int)((GroupParent2.Transform.Computed.Height - (rows - 1) * spacing) / rows);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                // Calculate the position for each button, including spacing.
                float posX = (spacing / 2f) + (j * (buttonWidth + spacing));
                float posY = (spacing / 2f) + (i * (buttonHeight + spacing));

                // Create a new button and set its position and size.
                var button = new Button()
                {
                    Name = $"Button {i}_{j}",
                    Transform = new(posX, posY, buttonWidth, buttonHeight),
                };

                // Add the button to the parent.
                GroupParent2.AddChild(button);
            }
        }
    }
}