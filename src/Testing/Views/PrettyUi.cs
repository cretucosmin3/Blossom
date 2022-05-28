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
        VisualElement Ground;

        public PrettyUi() : base("PrettyUi View") { }

        private int counter = 0;
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

            Ground = new VisualElement()
            {
                Name = "Ground",
                Transform = new(400, 462f, 300, 60)
                {
                    Anchor = Anchor.Bottom,
                    FixedWidth = true,
                    FixedHeight = true,
                    ValidateOnAnchor = false,
                },
                Style = new()
                {
                    BackColor = new SKColor(30, 30, 40),
                    Text = new()
                    {
                        Size = 28f,
                        Alignment = TextAlign.Center,
                        Color = SKColors.White,
                    },
                    Border = new()
                    {
                        Roundness = 2f,
                        Width = 0.5f,
                        Color = SKColors.Black,
                    }
                },
            };

            Button.Events.OnMouseClick += (btn, pos) =>
            {
                time = 700f; // ms
                jumpDistance += 150f;
                sw.Restart();
            };

            Button.Events.OnMouseDown += (btn, pos) =>
            {
                Button.Style.Border.Width = 1f;
                Button.Style.BackColor = new SKColor(96 + 10, 16 + 10, 16 + 10);
            };

            Button.Events.OnMouseUp += (btn, pos) =>
            {
                Button.Style.Border.Width = 0.6f;
                Button.Style.BackColor = new SKColor(96, 16, 16);
            };

            AddElement(Text);
            AddElement(Button);
            AddElement(Ground);

            Loop += Update;
        }

        float time = 700f; // ms
        float jumpDistance = 150f;
        Stopwatch sw = Stopwatch.StartNew();
        private void Update()
        {
            var p = MathF.PI * (sw.ElapsedMilliseconds / time);
            var v = MathF.Sin(p);
            Button.Transform.Y = 420f - jumpDistance * v;

            if (sw.ElapsedMilliseconds > time && time > 60)
            {
                sw.Restart();
                time /= 1.6f;
                jumpDistance /= 2f;
                Ground.Text = $"{counter += 1}";
                return;
            }
            if(time < 60)
            {
                time = 700f;
                jumpDistance = 0;
                sw.Stop();
            }
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