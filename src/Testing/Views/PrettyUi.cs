using System;
using System.Diagnostics;
using Kara.Core;
using Kara.Core.Input;
using Kara.Core.Visual;
using System.Drawing;
using System.Threading;
using SkiaSharp;
using System.Linq;

namespace Kara.Testing
{
    public class PrettyUi : View
    {
        VisualElement TestElement;

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
                        var current = Array.IndexOf(values, TestElement.Style.Text.Alignment);

                        current++;

                        if (current == values.Length)
                            current = 0;

                        TestElement.Style.Text.Alignment = values[current];
                        TestElement.Text = $"{values[current].ToString()}";
                        break;
                    default:
                        break;
                }
            };

            TestElement = new VisualElement()
            {
                Name = "TestElement",
                Text = "Testing",
                Transform = new(50, 50, 650, 400)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    FixedHeight = true,
                },
                Style = new() {
                    BorderWidth = 5f,
                    Roundness = 10f,
                    BorderColor = SKColors.DeepSkyBlue,
                    BackColor = SKColors.DimGray,
                    Text = new()
                    {
                        Color = SKColors.White,
                        Size = 75f,
                        Padding = 25f,
                        Alignment = TextAlign.Center
                    }
                },
            };

            Elements.AddElement(ref TestElement, this);

            watch.Start();
        }

        private Stopwatch watch = new Stopwatch();
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