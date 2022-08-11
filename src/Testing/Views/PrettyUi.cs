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

        VisualElement Text;
        VisualElement Button;

        public PrettyUi() : base("PrettyUi View") { }

        public override void Main()
        {
            Browser.ShowFps();

            Text = new VisualElement()
            {
                Name = "LoginText",
                Text = "Hello world!",
                Transform = new(550 - 200, 180, 400, 80)
                {
                    Anchor = Anchor.Top,
                    FixedWidth = false,
                    FixedHeight = false,
                    ValidateOnAnchor = false,
                },
                Style = new()
                {
                    Text = new()
                    {
                        Size = 35f,
                        Alignment = TextAlign.Center,
                        Color = SKColors.White,
                    }
                },
            };

            Button = new VisualElement()
            {
                Name = "Button",
                Text = "Click me",
                Transform = new(550 - 120, 300, 240, 45)
                {
                    Anchor = Anchor.Top,
                    FixedWidth = true,
                    FixedHeight = true,
                    ValidateOnAnchor = false,
                },
                Style = new()
                {
                    BackColor = SKColors.WhiteSmoke,
                    Border = new()
                    {
                        Roundness = 10,
                        Width = 1,
                        Color = SKColors.Black,
                    },
                    Text = new()
                    {
                        Size = 25f,
                        Weight = 400,
                        Alignment = TextAlign.Center,
                        Color = SKColors.Black,
                    }
                },
            };

            Button.Events.OnMouseDown += (btn, pos) =>
            {
                Button.Style.BackColor = new SKColor(50, 50, 50, 255);
                Button.Style.Text.Color = SKColors.White;
                Button.Style.Border.Color = SKColors.White;
                Button.Style.Border.Width = 2;
            };

            Button.Events.OnMouseUp += (btn, pos) =>
            {
                Button.Style.BackColor = SKColors.WhiteSmoke;
                Button.Style.Text.Color = SKColors.Black;
                Button.Style.Border.Color = SKColors.Black;
                Button.Style.Border.Width = 1;
            };

            List<VisualStyle> styles = new(){
                VisualStyle.OnEvent("LoginButton", VisualEvents.MouseHover, (VisualElement elm) => {
                    elm.Style.BackColor = new SKColor(50, 50, 50, 255);
                    elm.Style.Text.Color = SKColors.White;
                    elm.Style.Border.Color = SKColors.White;
                }),
                VisualStyle.Base("LoginButton", (VisualElement elm) => {
                    elm.Style.BackColor = SKColors.WhiteSmoke;
                    elm.Style.Text.Color = SKColors.Black;
                    elm.Style.Border.Color = SKColors.Black;
                }),
                VisualStyle.Base("LoginForm", new ElementStyle()
                {
                    BackColor = SKColors.WhiteSmoke,
                    Border = new()
                    {
                        Roundness = 10,
                        Width = 1,
                        Color = SKColors.Black,
                    },
                    Text = new()
                    {
                        Size = 25f,
                        Weight = 400,
                        Alignment = TextAlign.Center,
                        Color = SKColors.Black,
                    }
                }),
            };

            RenderChanges(() =>
            {
                Text.Text = "Hello world!";
                Button.Text = "Click me";
            });

            AddElement(Text);
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
        return new VisualStyle(className) {
            _Style = style,
        };
    }

    public static VisualStyle Base(string className, Action<VisualElement> styleEvent)
    {
        return new VisualStyle(className) {
            _StyleEvent = styleEvent
        };
    }

    public static VisualStyle OnEvent(string className, VisualEvents eventName, Action<VisualElement> styleEvent)
    {
        return new VisualStyle(className) {
            _Event = eventName,
            _StyleEvent = styleEvent
        };
    }
}