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
                            e.Text = $"{values[current].ToString()}";
                        }
                        break;
                    default:
                        break;
                }
            };

            for (int i = 0; i < 5; i++)
            {
                for (int x = 0; x < 5; x++)
                {
                    var newE = new VisualElement()
                    {
                        Name = "TestElement" + i + x,
                        Text = ">",
                        Transform = new(165 * i, 125 * x, 150, 100)
                        {
                            Anchor = Anchor.Top | Anchor.Left,
                            FixedHeight = true,
                        },
                        Style = new()
                        {
                            // BorderWidth = 1f,
                            // BorderColor = SKColors.DeepSkyBlue,
                            // BackColor = SKColors.DimGray,
                            Text = new()
                            {
                                Color = SKColors.Black,
                                Size = 22,
                                Padding = 3f,
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