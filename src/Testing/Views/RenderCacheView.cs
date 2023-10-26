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

    public override void Init()
    {
        var GroupParent1 = new Button()
        {
            Name = "Group parent 1",
            Transform = new(750, 100, 200, 200),
            Style = new()
            {
                BackColor = SKColors.AntiqueWhite,
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
            Transform = new(50, 50, 650, 600),
            Style = new()
            {
                BackColor = SKColors.LightGray,
                Border = new()
                {
                    Roundness = 3,
                },
            },
        };

        AddElement(GroupParent1);

        // Add group 2 grid of elements to click
        AddElement(GroupParent2);

        var topLeft = new Button()
        {
            Name = $"topLeft",
            Transform = new(0, 0, 100, 100)
            {
                ValidateOnAnchor = false,
            }
        };

        var bottomRight = new Button()
        {
            Name = $"bottomRight",
            Transform = new(GroupParent2.Transform.Width - 100, GroupParent2.Transform.Height - 100, 100, 100)
            {
                ValidateOnAnchor = false,
            }
        };

        // GroupParent2.AddChild(topLeft);
        // GroupParent2.AddChild(bottomRight);

        // Fixed number of buttons in the grid.
        float rows = 5;
        float cols = 5;

        // Spacing between buttons and also from parent's edges.
        float spacing = 15f;

        // Adjusted width and height calculation considering the spacing.
        float buttonWidth = (650f - spacing * (cols + 1)) / cols; // Subtracting total spacing and dividing by number of columns
        float buttonHeight = (600f - spacing * (rows + 1)) / rows; // Subtracting total spacing and dividing by number of rows

        for (float i = 0; i < rows; i++)
        {
            for (float j = 0; j < cols; j++)
            {
                // Calculate the position for each button, including spacing.
                // Here, we're considering spacing between elements as well as from the parent's left/top edge.
                float posX = j * (buttonWidth + spacing) + spacing; // Adding an extra 'spacing' for the space from the parent's left edge.
                float posY = i * (buttonHeight + spacing) + spacing; // Adding an extra 'spacing' for the space from the parent's top edge.

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