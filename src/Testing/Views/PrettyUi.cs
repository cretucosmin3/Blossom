using System;
using System.Diagnostics;
using Kara.Core;
using Kara.Core.Input;
using Kara.Core.Visual;
using System.Drawing;
using System.Threading;
using SkiaSharp;
using System.Linq;
using System.Collections.Generic;

namespace Kara.Testing
{
    public class PrettyUi : View
    {
        List<VisualElement> TestElements = new List<VisualElement>();

        public PrettyUi() : base("PrettyUi View") { }

        public override void Main()
        {
            Browser.ShowFps();

            this.Events.OnKeyType += (char c) =>
            {
                switch (c)
                {
                    case '1':
                        var values = Enum.GetValues(typeof(TextAlign)).Cast<TextAlign>().ToArray();
                        var current = Array.IndexOf(values, TestElements[0].Style.Text.Alignment);

                        current++;

                        if (current == values.Length)
                            current = 0;

                        foreach (var e in TestElements)
                        {
                            e.Style.Text.Alignment = values[current];
                        }
                        break;
                    default:
                        break;
                }
            };

            var count = 12;
            for (int i = 1; i <= count * 2; i++)
            {
                for (int x = 1; x <= count; x++)
                {
                    var z = (byte)(255 - (255 / count) * (x - 1));
                    SKColor clr = new SKColor(z, z, z);

                    var y = (byte)(255 - z);
                    SKColor textClr = new SKColor(y, y, y);

                    var newE = new VisualElement()
                    {
                        Name = i + "TestElement" + x,
                        Text = "●",
                        Transform = new(60 * i, 60 * x, 55, 55)
                        {
                            Anchor = Anchor.Top | Anchor.Left,
                            FixedHeight = true,
                        },
                        Style = new()
                        {
                            BorderWidth = 2f,
                            Roundness = 2f,
                            BorderColor = SKColors.Black,
                            BackColor = clr,
                            Text = new()
                            {
                                Color = textClr,
                                Size = 15,
                                Padding = 5f,
                                Alignment = TextAlign.Center
                            }
                        },
                    };

                    TestElements.Add(newE);
                    Elements.AddElement(ref newE, this);
                }
            }
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