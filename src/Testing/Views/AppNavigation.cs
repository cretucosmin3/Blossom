using System.Threading;
using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Testing.CustomElements;
using SkiaSharp;

namespace Blossom.Testing;

public class AppNavigation : View
{
    private VisualElement TopPanel;
    private VisualElement SearchBox;
    private VisualElement SearchButton;
    private VisualElement OptionsButton;

    public AppNavigation() : base("Search") { }

    public override void Main()
    {
        BackColor = new(100, 100, 100, 255);
        TopPanel = new VisualElement()
        {
            Name = "Top Panel",
            IsClipping = true,
            Transform = new(0, 0, Width, 45)
            {
                Anchor = Anchor.Top | Anchor.Left | Anchor.Right,
                FixedHeight = true,
            },
            Style = new()
            {
                BackColor = new SKColor(65, 65, 65, 255),
            }
        };

        SearchBox = new VisualElement()
        {
            Name = "Search box",
            Text = "www:...",
            Transform = new(25, 5, Width * 0.5f, 35)
            {
                Anchor = Anchor.Top | Anchor.Left | Anchor.Right,
                FixedHeight = true,
                FixedWidth = false,
            },
            Style = new()
            {
                BackColor = SKColors.White,
                Border = new()
                {
                    Roundness = 15
                },
                Text = new()
                {
                    Color = SKColors.Black,
                    Size = 20f,
                    Alignment = TextAlign.Left,
                    Padding = 25
                }
            }
        };

        SearchButton = new VisualElement()
        {
            Name = "Search button",
            Text = ">",
            Transform = new(((Width * 0.5f) + 25) - 25, 5, 40, 35)
            {
                Anchor = Anchor.Top | Anchor.Right,
                FixedHeight = true,
                FixedWidth = true,
            },
            Style = new()
            {
                BackColor = new(100, 170, 100, 255),
                Border = new()
                {
                    Roundness = 8,
                },
                Text = new()
                {
                    Color = SKColors.White,
                    Size = 35,
                    Weight = 500,
                    Alignment = TextAlign.Center,
                    PathEffect = SKPathEffect.CreateDiscrete(5, 1.5f)
                }
            }
        };

        OptionsButton = new VisualElement()
        {
            Name = "Options button",
            Text = "...",
            Transform = new(Width - 65, 5, 45, 35)
            {
                Anchor = Anchor.Top | Anchor.Right,
                FixedHeight = true,
                FixedWidth = true,
            },
            Style = new()
            {
                BackColor = new(100, 100, 100, 255),
                Border = new()
                {
                    Roundness = 5,
                },
                Text = new()
                {
                    Color = SKColors.White,
                    Size = 20f,
                    Weight = 500,
                    Alignment = TextAlign.Center,
                    PathEffect = SKPathEffect.CreateDiscrete(5, 1.5f)
                }
            }
        };

        AddElement(TopPanel);

        TopPanel.AddChild(SearchBox);
        TopPanel.AddChild(SearchButton);
        TopPanel.AddChild(OptionsButton);
    }
}