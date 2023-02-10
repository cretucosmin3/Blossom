using System.Threading;
using System.Text;
using System.Numerics;
using System;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing
{
    public class DatePicker : View
    {
        private bool MouseLeft = true;

        public DatePicker() : base("DatePicker View")
        {

        }

        public override void Main()
        {
            var neonButton = new VisualElement()
            {
                Name = "Button",
                IsClipping = false,
                Text = "NEON",
                Transform = new(250, 250, 220, 55)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                },
                Style = new()
                {
                    BackColor = new SKColor(0, 0, 0, 255),
                    Text = new()
                    {
                        Color = new(255, 0, 65),
                        Size = 30,
                        Weight = 700,
                        Shadow = new()
                        {
                            Color = new(255, 0, 65, 255),
                            OffsetX = 0,
                            OffsetY = 2,
                            SpreadX = 15,
                            SpreadY = 15
                        }
                    },
                    Border = new()
                    {
                        Color = new(255, 0, 65, 150),
                        Width = 4,
                        Roundness = 5,
                    },
                    Shadow = new()
                    {
                        Color = new(255, 0, 65, 255),
                        OffsetX = 0,
                        OffsetY = 0,
                        SpreadX = 25f,
                        SpreadY = 25f,
                    }
                }
            };

            AddElement(neonButton);

            neonButton.Events.OnMouseEnter += (_) =>
            {
                MouseLeft = false;
                new Thread(() =>
                {
                    neonButton.Style.Border.Color = new(255, 0, 65, 120);

                    Action flashBorder = () =>
                    {
                        var randomStart = Random.Shared.NextSingle();
                        var randomEnd = Random.Shared.NextSingle();
                        neonButton.Style.Border.PathEffect = SKPathEffect.CreateTrim(randomStart, randomEnd);
                        neonButton.Style.Shadow.Color = new(255, 0, 65, (byte)(255 * randomStart));
                        neonButton.Style.Text.Color = new(255, 0, 65, (byte)(255 * randomEnd));

                        if (randomStart < 0.1f) randomStart = 0.1f;
                        if (randomEnd < 0.1f) randomEnd = 0.1f;

                        neonButton.Style.Shadow.SpreadX = 50 * randomStart;
                        neonButton.Style.Shadow.SpreadY = 50 * randomEnd;

                        neonButton.Style.Text.Shadow.SpreadX = 35 * randomStart;
                        neonButton.Style.Text.Shadow.SpreadY = 35 * randomEnd;
                    };

                    for (float i = 0f; i <= 1.05f; i += 0.05f)
                    {
                        if (MouseLeft) return;

                        neonButton.Style.Text.PathEffect?.Dispose();

                        neonButton.Style.Text.PathEffect = SKPathEffect.CreateTrim(0f, i);

                        byte alpha = (byte)(255 * i < 180 ? 180 : 255 * i);
                        neonButton.Style.Text.Color = new(255, 0, 65, alpha);

                        if (Random.Shared.NextSingle() > 0.6f)
                            flashBorder();

                        Thread.Sleep(2);
                    }

                    neonButton.Style.Border.Color = new(255, 0, 65, 255);
                    neonButton.Style.Shadow.Color = new(255, 0, 65, 255);
                    neonButton.Style.Text.Color = new(255, 0, 65, 255);

                    neonButton.Style.Shadow.SpreadX = 22f;
                    neonButton.Style.Shadow.SpreadY = 22f;

                    neonButton.Style.Text.Shadow.SpreadX = 15;
                    neonButton.Style.Text.Shadow.SpreadY = 15;

                    neonButton.Style.Border.PathEffect.Dispose();
                    neonButton.Style.Border.PathEffect = null;
                }).Start();
            };

            neonButton.Events.OnMouseLeave += (_) =>
            {
                MouseLeft = true;
                new Thread(() =>
                {
                    neonButton.Style.Border.Color = new(255, 0, 65, 80);
                    neonButton.Style.Shadow.Color = new(255, 0, 65, 100);

                    for (float i = 1f; i >= -0.05f; i -= 0.05f)
                    {
                        if (!MouseLeft) return;

                        neonButton.Style.Text.PathEffect?.Dispose();
                        neonButton.Style.BackgroundPathEffect?.Dispose();

                        neonButton.Style.Text.PathEffect = SKPathEffect.CreateTrim(0f, i);

                        byte alpha = (byte)(255 * i < 180 ? 180 : 255 * i);

                        Thread.Sleep(2);
                    }

                    RenderChanges(() =>
                    {
                        neonButton.Style.Text.PathEffect?.Dispose();

                        neonButton.Style.Text.PathEffect = null;
                        neonButton.Style.Text.Color = new(255, 0, 65, 35);
                    });

                }).Start();
            };
        }
    }
}