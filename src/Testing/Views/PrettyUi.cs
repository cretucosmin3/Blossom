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
                Transform = new(550 - 100, 300, 200, 40)
                {
                    Anchor = Anchor.Top,
                    FixedWidth = true,
                    FixedHeight = true,
                    ValidateOnAnchor = false,
                },
                Style = new()
                {
                    BackColor = SKColors.DimGray,
                    Border = new()
                    {
                        Roundness = 5f,
                        Width = 1f,
                        Color = SKColors.White,
                    },
                    Text = new()
                    {
                        Size = 22f,
                        Alignment = TextAlign.Center,
                        Color = SKColors.White,
                    }
                },
            };

            Button.Events.OnMouseDown += (btn, pos) =>
            {
                Button.Style.BackColor = SKColors.DarkGray;
            };

            Button.Events.OnMouseUp += (btn, pos) =>
            {
                Button.Style.BackColor = SKColors.DimGray;
            };

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