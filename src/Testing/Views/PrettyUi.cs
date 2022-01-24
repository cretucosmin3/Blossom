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
        float time = 2000f; // seconds
        float from = 150;
        float to = 400;
        bool moveTo = true;

        VisualElement parent;
        VisualElement CloseButton;
        VisualElement SearchBox;
        VisualElement SearchButton;
        VisualElement AnimatedParent;
        VisualElement CenterElement;
        VisualElement InfoLabel;

        public PrettyUi() : base("PrettyUi View") { }

        public override void Main()
        {
            Browser.ShowFps();

            this.Events.OnKeyDown += (int x) =>
            {
                Console.WriteLine($"KeyDown: {x}");
                if (x == 30)
                {
                    AnimatedParent.Width -= 50;
                }
                else if (x == 32)
                {
                    AnimatedParent.Width += 50;
                }
                else if (x == 17)
                {
                    AnimatedParent.Height -= 50;
                }
                else if (x == 31)
                {
                    AnimatedParent.Height += 50;
                }
                else if (x == 328)
                {
                    CenterElement.FixedHeight = !CenterElement.FixedHeight;
                }
                else if (x == 336)
                {
                    CenterElement.FixedWidth = !CenterElement.FixedWidth;
                }
            };

            parent = new VisualElement()
            {
                Name = "parent",
                Text = "",
                X = 0,
                Y = 0,
                Width = Browser.RenderRect.Width,
                Height = 40f,
                FontSize = 20,
                BackColor = SKColors.Black,
                Anchor = Anchor.Top | Anchor.Left,
            };

            CloseButton = new VisualElement()
            {
                Name = "close button",
                Text = "X",
                X = 5,
                Y = 4,
                Width = 30f,
                Height = 32f,
                FontSize = 18,
                Roundness = 3f,
                BackColor = SKColors.DimGray,
                FontColor = SKColors.White,
                Anchor = Anchor.Top | Anchor.Left,
            };

            SearchBox = new VisualElement()
            {
                Name = "search",
                Text = "Search...",
                X = CloseButton.X + CloseButton.Width + 5,
                Y = 4,
                Width = 350f,
                Height = 32f,
                FontSize = 18,
                BorderWidth = 0.5f,
                Roundness = 2f,
                BorderColor = SKColors.Black,
                BackColor = SKColors.White,
                FontColor = SKColors.White,
                TextAlignment = TextAlign.Left,
                TextPadding = 10,
                Anchor = Anchor.Top | Anchor.Left,
            };

            SearchButton = new VisualElement()
            {
                Name = "button",
                Text = "Go",
                X = SearchBox.X + SearchBox.Width + 5,
                Y = 4,
                Width = 42f,
                Height = 32f,
                FontSize = 18,
                Roundness = 2f,
                BackColor = SKColors.Aquamarine,
                FontColor = SKColors.Black,
                Anchor = Anchor.Top | Anchor.Left,
            };

            InfoLabel = new VisualElement()
            {
                Name = "InfoLabel",
                X = 20,
                Y = 60,
                Width = 300,
                Height = 50,
                FontSize = 18,
                BorderWidth = 1f,
                BorderColor = SKColors.Black,
                Anchor = Anchor.Top | Anchor.Left,
            };

            AnimatedParent = new VisualElement()
            {
                Name = "AnimatedParent",
                X = 20,
                Y = 120,
                Width = 200,
                Height = 200,
                BorderWidth = 1f,
                Roundness = 10f,
                BorderColor = SKColors.Black,
                BackColor = SKColors.AliceBlue,
                Anchor = Anchor.Top | Anchor.Left,
            };

            CenterElement = new VisualElement()
            {
                Name = "CenterElement",
                X = 75,
                Y = 75,
                Width = 50,
                Height = 50,
                BorderWidth = 1f,
                BorderColor = SKColors.Black,
                BackColor = SKColors.AliceBlue,
                FixedWidth = true,
                FixedHeight = true,
            };

            Elements.AddElement(ref parent, this);
            Elements.AddElement(ref CloseButton, this);
            Elements.AddElement(ref SearchBox, this);
            Elements.AddElement(ref SearchButton, this);

            Elements.AddElement(ref AnimatedParent, this);
            Elements.AddElement(ref CenterElement, this);

            Elements.AddElement(ref InfoLabel, this);

            AnimatedParent.AddChild(CenterElement);

            parent.AddChild(CloseButton);
            parent.AddChild(SearchBox);
            parent.AddChild(SearchButton);

            Loop += Update;
            Events.OnMouseMove += OnMouseMove;
            watch.Start();
        }

        private VisualElement previousHovered;
        public void OnMouseMove(float x, float y)
        {
            VisualElement hovered = Elements.FirstFromPoint(x, y);
            InfoLabel.Text = hovered?.Name ?? "";
        }

        private Stopwatch watch = new Stopwatch();
        private void Update()
        {
            return;
            float progress = (float)watch.ElapsedMilliseconds / (time / 2f);
            if (progress > 1f) progress = 1f;

            float newVal = moveTo ? smoothLerp(from, to, progress) : smoothLerp(to, from, progress);

            int alpha = (int)(moveTo ? smoothLerp(0, 255, progress) : smoothLerp(255, 0, progress));

            AnimatedParent.Width = newVal;
            AnimatedParent.Height = newVal;

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