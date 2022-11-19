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
        VisualElement Button;
        private int clickedTimes = 0;
        public PrettyUi() : base("PrettyUi View") { }

        public override void Main()
        {
            Console.WriteLine("Pretty UI Main Happens");
            Button = new VisualElement()
            {
                Name = "ClickMe",
                Transform = new(100, 100, 220, 60)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    FixedWidth = true,
                    FixedHeight = false,
                    ValidateOnAnchor = false,
                },
                Style = new()
                {
                    BackColor = new SKColor(230, 230, 230, 255),
                    Border = new BorderStyle()
                    {
                        Roundness = 10,
                        Width = 3,
                        Color = new SKColor(200, 200, 200, 255)
                    },
                    Text = new TextStyle()
                    {
                        Size = 20,
                        Spacing = 15,
                        Color = new SKColor(40, 40, 40, 255),
                        Weight = 900
                    }
                },
                Text = "Create new"
            };

            Button.Events.OnMouseEnter += (VisualElement e) =>
            {
                Button.Style.Border.Color = new SKColor(150, 150, 150, 255);
                Button.Style.Border.Width = 4;
            };

            Button.Events.OnMouseLeave += (VisualElement e) =>
            {
                Button.Style.Border.Color = new SKColor(200, 200, 200, 255);
                Button.Style.Border.Width = 3;
            };

            Button.Events.OnMouseDown += (int btn, Vector2 pos) =>
            {
                clickedTimes++;
                Button.Style.Text.Size = 19;
                Button.Style.Border.Color = new SKColor(100, 100, 100, 255);
                Button.Style.Border.Width = 4;
            };

            Button.Events.OnMouseUp += (int btn, Vector2 pos) =>
            {
                Button.Style.Text.Size = 20;
                Button.Style.Border.Color = new SKColor(150, 150, 150, 255);
                Button.Style.Border.Width = 4;

                Button.Text = $"clicked {clickedTimes}" + ((clickedTimes == 0) ? " time" : " times");
            };

            AddElement(Button);
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