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

        VisualElement LoginText;
        VisualElement InputText;
        VisualElement Button;

        public PrettyUi() : base("PrettyUi View") { }

        public override void Main()
        {
            Browser.ShowFps();

            this.Events.OnMouseClick += (btn, pos) =>
            {
                Console.WriteLine($"Clicked {btn} at {pos}");
            };

            this.Events.OnMouseDoubleClick += (btn, pos) =>
            {
                Console.WriteLine($"Double Clicked {btn} at {pos}");
            };

        
            LoginText = new VisualElement()
            {
                Name = "LoginText",
                Text = "Welcome back!",
                Transform = new(550 - 200, 130, 400, 80)
                {
                    Anchor = Anchor.Top,
                    FixedWidth = false,
                    FixedHeight = true,
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

            InputText = new VisualElement()
            {
                Name = "InputText",
                Text = "Type your full name...",
                Transform = new(550 - 250, 230, 500, 40)
                {
                    Anchor = Anchor.Top,
                    FixedWidth = true,
                    FixedHeight = true,
                    ValidateOnAnchor = false,
                },
                Style = new()
                {
                    Border = new()
                    {
                        Width = 1f,
                        Color = SKColors.White,
                        Roundness = 5f,
                    },
                    Text = new()
                    {
                        Size = 16f,
                        Alignment = TextAlign.Left,
                        Padding = 12f,
                        Color = SKColors.White,
                    }
                },
            };

            Button = new VisualElement()
            {
                Name = "Button",
                Text = "Submit",
                Transform = new(550 - 100, 300, 200, 40)
                {
                    Anchor = Anchor.Top,
                    FixedWidth = true,
                    FixedHeight = true,
                    ValidateOnAnchor = false,
                },
                Style = new()
                {
                    BackColor = new SKColor(96, 16, 16),
                    Border = new()
                    {
                        Roundness = 6f,
                        Width = 0.6f,
                        Color = SKColors.Red,
                    },
                    Text = new()
                    {
                        Size = 20f,
                        Alignment = TextAlign.Center,
                        Color = SKColors.White,
                    }
                },
            };

            AddElement(LoginText);
            AddElement(InputText);
            AddElement(Button);
        }

        private void Update()
        {

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