using System;
using System.Diagnostics;
using Kara.Core;
using Kara.Core.Input;
using Kara.Core.Visual;
using System.Drawing;
using System.Threading;
using SkiaSharp;

namespace Kara.Testing
{
    public class PrettyUi : View
    {
        float time = 3000f; // seconds
        float from = 150;
        float to = 400;
        bool moveTo = true;

        VisualElement AnimatedParent;
        VisualElement CenterElement;

        public PrettyUi() : base("PrettyUi View") { }

        public override void Main()
        {
            Browser.ShowFps();

            this.Events.OnKeyDown += (int x) =>
            {
                Console.WriteLine($"KeyDown: {x}");
                if (x == 30)
                {
                    AnimatedParent.Transform.X -= 50;
                }
                else if (x == 32)
                {
                    AnimatedParent.Transform.X += 50;
                }
                else if (x == 17)
                {
                    AnimatedParent.Transform.Y -= 50;
                }
                else if (x == 31)
                {
                    AnimatedParent.Transform.Y += 50;
                }
                else if (x == 328)
                {
                    CenterElement.Transform.FixedHeight = !CenterElement.Transform.FixedHeight;
                }
                else if (x == 336)
                {
                    CenterElement.Transform.FixedWidth = !CenterElement.Transform.FixedWidth;
                }
            };

            AnimatedParent = new VisualElement()
            {
                Name = "AnimatedParent",
                BorderWidth = 1f,
                Roundness = 5f,
                BorderColor = SKColors.Black,
                BackColor = SKColors.Green,
                Transform = new Transform(100, 100, 200, 200) {
                    Anchor = Anchor.Top | Anchor.Left
                }
            };

            CenterElement = new VisualElement()
            {
                Name = "CenterElement",
                BorderWidth = 1f,
                Roundness = 5f,
                BorderColor = SKColors.White,
                BackColor = SKColors.Purple,
            };

            CenterElement.Transform = new Transform(50, 10, 100, 100) {
                Anchor = Anchor.Top | Anchor.Bottom,
                FixedWidth = true,
                FixedHeight = true
            };

            AnimatedParent.AddChild(CenterElement);

            Elements.AddElement(ref AnimatedParent, this);
            Elements.AddElement(ref CenterElement, this);

            Loop += Update;
            watch.Start();
        }

        private Stopwatch watch = new Stopwatch();
        private void Update()
        {
            float progress = (float)watch.ElapsedMilliseconds / (time / 2f);
            if (progress > 1f) progress = 1f;

            float newVal = moveTo ? smoothLerp(from, to, progress) : smoothLerp(to, from, progress);

            int alpha = (int)(moveTo ? smoothLerp(0, 255, progress) : smoothLerp(255, 0, progress));

            AnimatedParent.Transform.Width = newVal;
            AnimatedParent.Transform.Height = newVal;

            if (watch.ElapsedMilliseconds >= (time / 2f))
            {
                moveTo = !moveTo;
                watch.Reset();
                watch.Start();
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