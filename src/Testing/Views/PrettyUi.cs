using System.Text;
using System;
using Blossom.Core;
using Blossom.Core.Visual;

namespace Blossom.Testing
{
    public class PrettyUi : View
    {
        VisualElement SearchBar;

        VisualElement TL;
        VisualElement TR;
        VisualElement BL;
        VisualElement BR;

        VisualElement LC;
        VisualElement TC;
        VisualElement RC;
        VisualElement BC;


        StringBuilder SearchText = new StringBuilder("");
        private int clickedTimes = 0;

        public PrettyUi() : base("PrettyUi View")
        {
            this.Events.OnKeyType += (ch) =>
            {
                SearchText.Append(ch);
                SearchBar.Text = SearchText.ToString();
                SearchBar.Style.ScheduleRender();
            };

            this.Events.OnKeyDown += (key) =>
            {
                if (SearchText.Length > 0)
                {
                    if (key == 14) SearchText.Remove(SearchText.Length - 1, 1);

                    SearchBar.Text = SearchText.ToString();
                }
            };
        }

        public override void Init()
        {
            var HalfWidth = Width / 2;
            var winW = Browser.window.Size.X;
            var winH = Browser.window.Size.Y;

            ElementStyle StyleAnchors = new()
            {
                BackColor = new(35, 50, 200, 190),
                Border = new()
                {
                    Roundness = 5,
                    Width = 2f,
                    Color = new(255, 255, 255, 255)
                },
                Shadow = new()
                {
                    Color = new(0, 0, 0, 160),
                    SpreadX = 5,
                    SpreadY = 5,
                }
            };

            ElementStyle RedStyleAnchors = new()
            {
                BackColor = new(200, 50, 35, 120),
                Border = new()
                {
                    Roundness = 5,
                    Width = 2f,
                    Color = new(255, 255, 255, 255)
                },
                Shadow = new()
                {
                    Color = new(0, 0, 0, 160),
                    SpreadX = 5,
                    SpreadY = 5,
                }
            };

            ElementStyle SharedElementStyle = new()
            {
                BackColor = new(255, 255, 255, 255),
                Border = new()
                {
                    Roundness = 5,
                    Width = 0.5f,
                    Color = new(0, 0, 0, 25)
                },
                Text = new()
                {
                    Size = 22,
                    Spacing = 20,
                    Padding = 20,
                    Weight = 450,
                    Alignment = TextAlign.Left,
                    Color = new(200, 50, 50, 220),
                },
                Shadow = new()
                {
                    Color = new(0, 0, 0, 35),
                    SpreadX = 4,
                    SpreadY = 4,
                    OffsetY = 3
                }
            };

            SearchBar = new VisualElement()
            {
                Name = "ClickMe",
                Transform = new(HalfWidth - 200, 120, 400, 38)
                {
                    Anchor = Anchor.Top,
                    FixedWidth = true,
                    FixedHeight = true,
                    ValidateOnAnchor = false,
                },
                Style = SharedElementStyle,
                Text = "Search ..."
            };

            TL = new()
            {
                Name = "Top Left",
                Transform = new(10, 10, 30, 30)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    FixedWidth = false,
                    FixedHeight = true,
                    ValidateOnAnchor = false,
                },
                Style = StyleAnchors,
            };

            TR = new()
            {
                Name = "Top Right",
                Transform = new(winW - 40, 10, 30, 30)
                {
                    Anchor = Anchor.Top | Anchor.Right,
                    FixedWidth = false,
                    FixedHeight = true,
                    ValidateOnAnchor = false,
                },
                Style = StyleAnchors,
            };

            BL = new()
            {
                Name = "Bottom Left",
                Transform = new(10, winH - 40, 30, 30)
                {
                    Anchor = Anchor.Bottom | Anchor.Left,
                    FixedWidth = false,
                    FixedHeight = true,
                    ValidateOnAnchor = false,
                },
                Style = StyleAnchors,
            };

            BR = new()
            {
                Name = "Bottom Right",
                Transform = new(winW - 40, winH - 40, 30, 30)
                {
                    Anchor = Anchor.Bottom | Anchor.Right,
                    FixedWidth = false,
                    FixedHeight = true,
                    ValidateOnAnchor = false,
                },
                Style = StyleAnchors,
            };

            TC = new()
            {
                Name = "Top Center",
                Transform = new(60, 10, winW - 120, 30)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right,
                    FixedWidth = false,
                    FixedHeight = true,
                    ValidateOnAnchor = false,
                },
                Style = RedStyleAnchors,
            };

            Action Hovered = () =>
            {
                SearchBar.Style.Border.Width = 1f;
                SearchBar.Style.Border.Color = new(0, 0, 0, 225);
                SearchBar.Style.Text.Color = new(0, 0, 0, 255);
            };

            Action ToNormal = () =>
            {
                SearchBar.Style.Border.Width = 0.5f;
                SearchBar.Style.Border.Color = new(0, 0, 0, 25);
                SearchBar.Style.Text.Color = new(200, 50, 50, 220);
            };

            SearchBar.Events.OnMouseLeave += (VisualElement e) => ToNormal();
            SearchBar.Events.OnMouseEnter += (VisualElement e) => Hovered();
            SearchBar.Events.OnMouseUp += (obj, args) =>
            {
                Log.Debug($"{SearchBar.Transform.X} :: {SearchBar.Transform.Computed.X}");

                SearchBar.Transform.X += 10;
                SearchBar.Transform.Width -= 20;
                Hovered();
            };
            SearchBar.Events.OnMouseDown += (obj, args) =>
            {
                SearchBar.Style.Border.Width = 2.5f;
                SearchBar.Style.Border.Color = new(200, 50, 50, 220);
                SearchBar.Style.Text.Color = new(200, 50, 50, 220);

                SearchBar.Transform.X -= 10;
                SearchBar.Transform.Width += 20;
            };


            AddElement(SearchBar);
            AddElement(TL);
            AddElement(TR);
            AddElement(BL);
            AddElement(BR);
            AddElement(TC);
        }
    }
}