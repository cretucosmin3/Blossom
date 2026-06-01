using System;
using System.Collections.Generic;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Views
{
    public class BenchmarkDynamicView : View
    {
        private readonly List<VisualElement> _animatingElements = new();
        private readonly List<float> _baseX = new();
        private readonly List<float> _baseY = new();
        private int _tick = 0;

        public BenchmarkDynamicView() : base("Benchmark - Dynamic Mutation")
        {
            BackColor = new SKColor(5, 5, 10); // Very dark gray-blue
        }

        public override void Init()
        {
            // Title
            AddElement(new VisualElement
            {
                Name = "BenchDynamic_Title",
                Text = "BLOSSOM DYNAMIC BENCHMARK (300 Mutating Elements)",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = SKColors.White, Size = 20, Weight = 800, Alignment = TextAlign.Center }
                },
                Transform = new Transform(0, 15, Width, 30) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            int elementCount = 300;
            Random rand = new Random(42);

            for (int i = 0; i < elementCount; i++)
            {
                float bx = rand.Next(50, Width - 100);
                float by = rand.Next(80, Height - 100);
                float size = rand.Next(15, 30);

                var el = new VisualElement
                {
                    Name = $"DynamicEl_{i}",
                    Style = new ElementStyle
                    {
                        BackColor = new SKColor((byte)rand.Next(100, 255), (byte)rand.Next(100, 255), (byte)rand.Next(100, 255)),
                        Border = new BorderStyle { Roundness = size / 2f, Width = 1, Color = SKColors.White.WithAlpha(50) },
                        Shadow = new ShadowStyle
                        {
                            Color = new SKColor((byte)rand.Next(100, 255), (byte)rand.Next(100, 255), (byte)rand.Next(100, 255), 180),
                            OffsetX = 0,
                            OffsetY = 0,
                            SpreadX = 5,
                            SpreadY = 5
                        }
                    },
                    Transform = new Transform(bx, by, size, size)
                };

                AddElement(el);
                _animatingElements.Add(el);
                _baseX.Add(bx);
                _baseY.Add(by);
            }

            // Register frame animation update loop
            Loop += AnimateElements;
        }

        private void AnimateElements()
        {
            _tick++;
            float time = _tick * 0.03f;

            for (int i = 0; i < _animatingElements.Count; i++)
            {
                var el = _animatingElements[i];

                // Mutate positions using sine/cosine waves relative to their base coordinates
                float newX = _baseX[i] + (float)Math.Sin(time + i * 0.2f) * 40f;
                float newY = _baseY[i] + (float)Math.Cos(time + i * 0.15f) * 40f;

                el.Transform.X = newX;
                el.Transform.Y = newY;

                // Mutate background color smoothly
                byte r = (byte)(128 + 127 * Math.Sin(time + i));
                byte g = (byte)(128 + 127 * Math.Cos(time + i * 1.5f));
                byte b = (byte)(128 + 127 * Math.Sin(time + i * 2.0f));
                el.Style.BackColor = new SKColor(r, g, b);

                // Mutate glowing shadow spreads and opacity
                float shadowSpread = 4f + (float)Math.Abs(Math.Sin(time + i)) * 12f;
                if (el.Style.Shadow != null)
                {
                    el.Style.Shadow.SpreadX = shadowSpread;
                    el.Style.Shadow.SpreadY = shadowSpread;
                    el.Style.Shadow.Color = new SKColor(r, g, b, (byte)(100 + 155 * Math.Abs(Math.Cos(time + i))));
                }
            }
        }
    }
}
