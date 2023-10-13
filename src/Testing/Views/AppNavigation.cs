using System.Threading;
using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Testing.CustomElements;
using SkiaSharp;
using Blossom.Core.Input;
using System.Text;

namespace Blossom.Testing;

public class AppNavigation : View
{
    private VisualElement TopPanel;
    private VisualElement SearchBox;
    private VisualElement SearchLabel;
    private VisualElement SearchButton;
    private VisualElement OptionsButton;
    private StringBuilder searchText = new StringBuilder("");

    public AppNavigation() : base("Search") { }

    public override void Init()
    {
        BackColor = new(100, 120, 165, 255);
        TopPanel = new VisualElement()
        {
            Name = "Top Panel",
            IsClipping = true,
            Transform = new(0, 0, Width, 10)
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
            Text = "Name or site",
            Focusable = true,
            Transform = new((Width / 2f) - 175f, 150f, 350, 35)
            {
                Anchor = Anchor.Top,
                FixedHeight = true,
                FixedWidth = true,
            },
            Style = new()
            {
                BackColor = SKColors.White,
                Border = new()
                {
                    Roundness = 15,
                    Color = SKColors.Black,
                    Width = 0
                },
                Text = new()
                {
                    Color = SKColors.Black,
                    Size = 20f,
                    Alignment = TextAlign.Left,
                    Padding = 25,
                },
                Shadow = new()
                {
                    OffsetX = 0,
                    OffsetY = 2,
                    SpreadX = 6,
                    SpreadY = 4,
                    Color = new(0, 0, 50, 100),
                },
            }
        };

        SearchLabel = new VisualElement()
        {
            Name = "Search label",
            Text = "Search an application or site",
            Transform = new((Width / 2f) - 175f, 100f, 350, 35)
            {
                Anchor = Anchor.Top,
                FixedHeight = true,
                FixedWidth = true,
            },
            Style = new()
            {
                Text = new()
                {
                    Color = SKColors.White,
                    Size = 25f,
                    Alignment = TextAlign.Center,
                    Weight = 600,
                    Shadow = new()
                    {
                        OffsetX = 0,
                        OffsetY = 2,
                        SpreadX = 6,
                        SpreadY = 4,
                        Color = new(0, 0, 0, 80)
                    }
                }
            }
        };

        SearchBox.Events.OnKeyType += (char ch) =>
        {
            if (!SearchBox.HasFocus) return;

            searchText.Append(ch);
            SearchBox.Text = searchText.ToString();
        };

        SearchBox.Events.OnKeyDown += (int key) =>
        {
            if (!SearchBox.HasFocus) return;

            // Backspace
            if (key == 14 && searchText.Length > 0)
            {
                searchText.Remove(searchText.Length - 1, 1);
                SearchBox.Text = searchText.ToString();
            }
        };

        SearchBox.OnFocused += (_) => SearchBox.Style.Border.Width = 3f;
        SearchBox.OnFocusLost += (_) => SearchBox.Style.Border.Width = 0f;

        AddElement(TopPanel);
        AddElement(SearchBox);
        AddElement(SearchLabel);
    }
}