using System.Text;
using System.Numerics;
using System;
using Rux.Core;
using Rux.Core.Visual;

namespace Rux.Testing
{
    public class PrettyUi : View
    {
        VisualElement SearchBar;
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

        public override void Main()
        {
            var HalfWidth = 1100 / 2;

            SearchBar = new VisualElement()
            {
                Name = "ClickMe",
                Transform = new(HalfWidth - 225, 35, 450, 35)
                {
                    Anchor = Anchor.Top,
                    FixedWidth = true,
                    FixedHeight = true,
                    ValidateOnAnchor = true,
                },
                Style = new()
                {
                    BackColor = new(255, 255, 255, 255),
                    Border = new()
                    {
                        Roundness = 4,
                        Width = 0.5f,
                        Color = new(0, 0, 0, 25)
                    },
                    Text = new()
                    {
                        Size = 18,
                        Spacing = 20,
                        Padding = 20,
                        Weight = 450,
                        Alignment = TextAlign.Left,
                        Color = new(0, 0, 0, 180),
                    },
                    Shadow = new()
                    {
                        Color = new(0, 0, 0, 35),
                        SpreadX = 4,
                        SpreadY = 4,
                        OffsetY = 3
                    }
                },
                Text = "Search ..."
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
                SearchBar.Style.Text.Color = new(0, 0, 0, 180);
            };

            SearchBar.Events.OnMouseLeave += (VisualElement e) => ToNormal();
            SearchBar.Events.OnMouseEnter += (VisualElement e) => Hovered();
            SearchBar.Events.OnMouseUp += (int btn, Vector2 pos) => Hovered();
            SearchBar.Events.OnMouseDown += (int btn, Vector2 pos) =>
            {
                SearchBar.Style.Border.Width = 2f;
                SearchBar.Style.Border.Color = new(0, 0, 0, 255);
                SearchBar.Style.Text.Color = new(0, 0, 0, 255);
            };


            AddElement(SearchBar);
        }
    }
}