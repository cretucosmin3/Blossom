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

        private int counter = 0;
        public override void Main()
        {
            Browser.ShowFps();

            Text = new VisualElement()
            {
                Name = "LoginText",
                Text = "Welcome back!",
                Transform = new(550 - 200, 240, 400, 80)
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
                Text = "Submit",
                Transform = new(550 - 100, 300, 200, 40)
                {
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

            Button.Events.OnMouseClick += (btn, pos) =>
            {
                Console.WriteLine("Button Clicked");
                Text.Text = $"+{counter += 1}";
            };

            Button.Events.OnMouseDown += (btn, pos) =>
            {
                Button.Style.Border.Width = 1f;
                Button.Style.BackColor = new SKColor(96 + 10, 16 + 10, 16 + 10);
                Text.Style.Text.Size += 6f;
            };

            Button.Events.OnMouseUp += (btn, pos) =>
            {
                Button.Style.Border.Width = 0.6f;
                Button.Style.BackColor = new SKColor(96, 16, 16);
                Text.Style.Text.Size -= 6f;
            };

            AddElement(Text);
            AddElement(Button);

            Loop += Update;
        }

        float time = 1000f; // ms
        float jumpDistance = 100f;
        Stopwatch sw = Stopwatch.StartNew();
        private void Update()
        {
            var p = MathF.PI * (sw.ElapsedMilliseconds / time);
            var v = MathF.Sin(p);
            Button.Transform.Y = 420f - jumpDistance * MathF.Sin(v);

            if(sw.ElapsedMilliseconds > time)
                sw.Restart();
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