using System.Diagnostics;
using System.Text;
using System.Numerics;
using System;
using Blossom.Core;
using Blossom.Core.Visual;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Blossom.Testing
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
                Transform = new(HalfWidth - 200, 30, 400, 38)
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
                SearchBar.Style.Text.Color = new(200, 50, 50, 220);
            };

            SearchBar.Events.OnMouseLeave += (VisualElement e) => ToNormal();
            SearchBar.Events.OnMouseEnter += (VisualElement e) => Hovered();
            SearchBar.Events.OnMouseUp += (int btn, Vector2 pos) => Hovered();
            SearchBar.Events.OnMouseDown += (int btn, Vector2 pos) =>
            {
                SearchBar.Style.Border.Width = 2.5f;
                SearchBar.Style.Border.Color = new(200, 50, 50, 220);
                SearchBar.Style.Text.Color = new(200, 50, 50, 220);
            };


            AddElement(SearchBar);
        }
    }
}