using System;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.Views
{
    public class BenchmarkStaticView : View
    {
        public BenchmarkStaticView() : base("Benchmark - Static Grid")
        {
            BackColor = new SKColor(10, 15, 25); // Midnight blue
        }

        public override void Init()
        {
            // Title element
            AddElement(new VisualElement
            {
                Name = "BenchStatic_Title",
                Text = "BLOSSOM STATIC BENCHMARK (1250 Elements)",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = SKColors.White, Size = 20, Weight = 800, Alignment = TextAlign.Center }
                },
                Transform = new Transform(0, 15, Width, 30) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Grid configurations
            int cols = 25;
            int rows = 10;
            float cardWidth = 36f;
            float cardHeight = 50f;
            float gapX = 6f;
            float gapY = 6f;
            float startX = 30f;
            float startY = 60f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float x = startX + c * (cardWidth + gapX);
                    float y = startY + r * (cardHeight + gapY);

                    // 1. Container Card
                    var card = new VisualElement
                    {
                        Name = $"Card_{r}_{c}",
                        Style = new ElementStyle
                        {
                            BackColor = new SKColor(30, 41, 59, 150),
                            Border = new BorderStyle { Roundness = 4, Width = 1, Color = new SKColor(255, 255, 255, 20) }
                        },
                        Transform = new Transform(x, y, cardWidth, cardHeight)
                    };
                    AddElement(card);

                    // 2. Card Label (Text)
                    card.AddChild(new VisualElement
                    {
                        Name = $"Label_{r}_{c}",
                        Text = $"{r}:{c}",
                        Style = new ElementStyle
                        {
                            Text = new TextStyle { Color = SKColors.LightGray, Size = 8, Weight = 400, Alignment = TextAlign.Top, Padding = 2 }
                        },
                        Transform = new Transform(0, 2, cardWidth, 12)
                    });

                    // 3. Status Dot
                    card.AddChild(new VisualElement
                    {
                        Name = $"Dot_{r}_{c}",
                        Style = new ElementStyle
                        {
                            BackColor = ((r + c) % 3 == 0) ? SKColors.Green : (((r + c) % 3 == 1) ? SKColors.Yellow : SKColors.Red),
                            Border = new BorderStyle { Roundness = 3 }
                        },
                        Transform = new Transform(cardWidth / 2f - 3, 16, 6, 6)
                    });

                    // 4. Progress Bar Track
                    card.AddChild(new VisualElement
                    {
                        Name = $"Track_{r}_{c}",
                        Style = new ElementStyle
                        {
                            BackColor = SKColors.Black.WithAlpha(80),
                            Border = new BorderStyle { Roundness = 2 }
                        },
                        Transform = new Transform(4, 30, cardWidth - 8, 4)
                    });

                    // 5. Progress Bar Fill
                    float percentage = ((r * cols + c) % 10) / 10f;
                    card.AddChild(new VisualElement
                    {
                        Name = $"Fill_{r}_{c}",
                        Style = new ElementStyle
                        {
                            BackColor = new SKColor(56, 189, 248),
                            Border = new BorderStyle { Roundness = 2 }
                        },
                        Transform = new Transform(4, 30, (cardWidth - 8) * percentage, 4)
                    });
                }
            }
        }
    }
}
