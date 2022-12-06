using System.Text;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Numerics;
using System.Net.Mime;
using System;
using System.Diagnostics;
using Rux.Core;
using Rux.Core.Input;
using Rux.Core.Visual;
using System.Drawing;
using System.Threading;
using SkiaSharp;
using System.Linq;
using System.Collections.Generic;

namespace Rux.Testing
{
    public class PrettyUi : View
    {
        VisualElement SearchBar;
        StringBuilder SearchText = new StringBuilder("");
        private int clickedTimes = 0;
        public PrettyUi() : base("PrettyUi View") { }

        public override void Main()
        {
            this.Events.OnKeyType += (ch) =>
            {
                Console.WriteLine($"Key down {ch}");
                SearchText.Append(ch);
                SearchBar.Text = SearchText.ToString();
                SearchBar.Style.ScheduleRender();
            };

            this.Events.OnKeyDown += (key) =>
            {
                if (SearchText.Length > 0)
                {
                    if (key == 14) SearchText.Remove(SearchText.Length - 1, 1);
                    Console.WriteLine($"Key pressed {key}");

                    SearchBar.Text = SearchText.ToString();
                }
            };

            var HalfWidth = 1100 / 2;

            Console.WriteLine("Pretty UI Main Happens");
            SearchBar = new VisualElement()
            {
                Name = "ClickMe",
                Transform = new(HalfWidth - 225, 10, 450, 40)
                {
                    Anchor = Anchor.Top,
                    FixedWidth = true,
                    FixedHeight = true,
                    ValidateOnAnchor = true,
                },
                Style = new()
                {
                    BackColor = SKColors.White,
                    Border = new BorderStyle()
                    {
                        Roundness = 5,
                        Width = 1,
                        Color = SKColors.DimGray
                    },
                    Text = new TextStyle()
                    {
                        Size = 18,
                        Spacing = 20,
                        Padding = 20,
                        Color = new SKColor(80, 80, 80, 255),
                        Weight = 500,
                        Alignment = TextAlign.Left
                    },
                    Shadow = new()
                    {
                        Color = new SKColor(220, 220, 220),
                        OffsetY = 8,
                        SpreadX = 16,
                        SpreadY = 8
                    }
                },
                Text = "Search ..."
            };

            SearchBar.Events.OnMouseDown += (int btn, Vector2 pos) =>
            {
                clickedTimes++;
                SearchBar.Style.Text.Size = 19f;

                SearchBar.Style.Shadow.OffsetX = 0;
                SearchBar.Style.Shadow.OffsetY = 3;

                SearchBar.Transform.Width -= 4;
                SearchBar.Transform.X += 2;

                SearchBar.Transform.Height -= 4;
                SearchBar.Transform.Y += 2;

                SearchBar.Text = "Mouse Down";
            };

            SearchBar.Events.OnMouseUp += (int btn, Vector2 pos) =>
            {
                SearchBar.Style.Text.Size = 20;
                SearchBar.Style.BackColor = SKColors.White;

                SearchBar.Text = $"clicked {clickedTimes}" + ((clickedTimes == 0) ? " time" : " times");
                SearchBar.Style.Shadow.OffsetX = 0;
                SearchBar.Style.Shadow.OffsetY = 5;

                SearchBar.Transform.Width += 4;
                SearchBar.Transform.X -= 2;

                SearchBar.Transform.Height += 4;
                SearchBar.Transform.Y -= 2;
            };

            AddElement(SearchBar);
        }

        float smoothLerp(float from, float to, float progress)
        {
            return from + (to - from) * (progress * progress * (3 - 2 * progress));
        }

        float lerp(float a, float b, float f)
        {
            return a + f * (b - a);
        }
    }
}


public enum VisualEvents
{
    MouseDown,
    MouseUp,
    MouseMove,
    MouseEnter,
    MouseLeave,
    MouseHover,
}

public class VisualStyle
{
    private string _Class;
    private VisualEvents _Event;
    private Action<VisualElement> _StyleEvent;
    private ElementStyle _Style;

    public VisualStyle(string className)
    {
        _Class = className;
    }

    public VisualStyle(string className, VisualEvents eventName)
    {
        _Class = className;
        _Event = eventName;
    }

    public static VisualStyle Base(string className, ElementStyle style)
    {
        return new VisualStyle(className)
        {
            _Style = style,
        };
    }

    public static VisualStyle Base(string className, Action<VisualElement> styleEvent)
    {
        return new VisualStyle(className)
        {
            _StyleEvent = styleEvent
        };
    }

    public static VisualStyle OnEvent(string className, VisualEvents eventName, Action<VisualElement> styleEvent)
    {
        return new VisualStyle(className)
        {
            _Event = eventName,
            _StyleEvent = styleEvent
        };
    }
}