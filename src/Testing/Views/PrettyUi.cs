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
                Text = "Click to jump",
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
                Text = "Hello",
                Transform = new(550 - 100, 300, 200, 40)
                {
                    Anchor = Anchor.Bottom,
                    FixedWidth = true,
                    FixedHeight = true,
                    ValidateOnAnchor = false,
                },
                Style = new()
                {
                    BackColor = SKColors.DimGray,
                    Border = new()
                    {
                        Roundness = 6f,
                        Width = 5f,
                        Color = SKColors.White,
                    },
                    Text = new()
                    {
                        Size = 20f,
                        Alignment = TextAlign.Center,
                        Color = SKColors.White,
                    }
                },
            };

            Button.Events.OnMouseDown += (btn, pos) =>
            {
                Button.Style.Border.Width = 2f;
                Button.Style.Border.Color = SKColors.White;
                Button.Style.BackColor = SKColors.Blue;
            };

            Button.Events.OnMouseUp += (btn, pos) =>
            {
                Button.Style.Border.Width = 5f;
                Button.Style.Border.Color = SKColors.White;
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