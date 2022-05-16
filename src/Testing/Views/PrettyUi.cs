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
        VisualElement Parent;
        VisualElement LeftIndicator;
        VisualElement RightIndicator;
        VisualElement TopIndicator;
        VisualElement BottomIndicator;

        VisualElement TestElement;

        public PrettyUi() : base("PrettyUi View") { }

        public override void Main()
        {
            Browser.ShowFps();

            this.Loop += Update;

            this.Events.OnKeyType += (char c) =>
            {
                bool hasFlag = false;
                switch (c)
                {
                    case '1':
                        var values = Enum.GetValues(typeof(TextAlign)).Cast<TextAlign>().ToArray();
                        var current = Array.IndexOf(values, TestElement.Style.Text.Alignment);

                        current++;

                        if (current == values.Length)
                            current = 0;

                        TestElement.Style.Text.Alignment = values[current];
                        TestElement.Text = values[current].ToString();
                        break;
                    case 'a':
                        hasFlag = TestElement.Transform.Anchor.HasFlag(Anchor.Left);
                        LeftIndicator.Style.BackColor = !hasFlag ? SKColors.Black : SKColors.LightGray;

                        if (hasFlag)
                            TestElement.Transform.Anchor &= ~Anchor.Left;
                        else
                            TestElement.Transform.Anchor |= Anchor.Left;

                        break;
                    case 'd':
                        hasFlag = TestElement.Transform.Anchor.HasFlag(Anchor.Right);
                        RightIndicator.Style.BackColor = !hasFlag ? SKColors.Black : SKColors.LightGray;

                        if (hasFlag)
                            TestElement.Transform.Anchor &= ~Anchor.Right;
                        else
                            TestElement.Transform.Anchor |= Anchor.Right;

                        break;
                    case 'w':
                        hasFlag = TestElement.Transform.Anchor.HasFlag(Anchor.Top);
                        TopIndicator.Style.BackColor = !hasFlag ? SKColors.Black : SKColors.LightGray;

                        if (hasFlag)
                            TestElement.Transform.Anchor &= ~Anchor.Top;
                        else
                            TestElement.Transform.Anchor |= Anchor.Top;

                        break;
                    case 's':
                        hasFlag = TestElement.Transform.Anchor.HasFlag(Anchor.Bottom);
                        BottomIndicator.Style.BackColor = !hasFlag ? SKColors.Black : SKColors.LightGray;

                        if (hasFlag)
                            TestElement.Transform.Anchor &= ~Anchor.Bottom;
                        else
                            TestElement.Transform.Anchor |= Anchor.Bottom;

                        break;
                    default:
                        break;
                }
            };

            this.Events.OnKeyUp += key =>
            {
                Console.WriteLine(key);

                if (key == 116)
                {
                    Parent.Transform.Height += 20;
                }

                if (key == 111)
                {
                    Parent.Transform.Height -= 20;
                }

                if (key == 113)
                {
                    Parent.Transform.Width -= 20;
                }

                if (key == 114)
                {
                    Parent.Transform.Width += 20;
                }
            };

            int iThinckness = 5;

            Parent = new VisualElement()
            {
                Name = "Parent",
                Text = "Testing",
                Transform = new(150, 150, 400, 400),
                Style = new()
                {
                    BackColor = SKColors.AliceBlue,
                }
            };

            LeftIndicator = new VisualElement()
            {
                Name = "LeftIndicator",
                Transform = new(0, 0, iThinckness, Parent.Transform.Height)
                {
                    Anchor = Anchor.Left | Anchor.Top | Anchor.Bottom
                },
                Style = new()
                {
                    BackColor = SKColors.Black,
                }
            };

            RightIndicator = new VisualElement()
            {
                Name = "RightIndicator",
                Transform = new(Parent.Transform.Width - iThinckness, 0, iThinckness, 400)
                {
                    Anchor = Anchor.Right | Anchor.Top | Anchor.Bottom
                },
                Style = new()
                {
                    BackColor = SKColors.LightGray,
                }
            };

            TopIndicator = new VisualElement()
            {
                Name = "TopIndicator",
                Transform = new(0, 0, Parent.Transform.Width, iThinckness)
                {
                    Anchor = Anchor.Left | Anchor.Right | Anchor.Top
                },
                Style = new()
                {
                    BackColor = SKColors.Black,
                }
            };

            BottomIndicator = new VisualElement()
            {
                Name = "BottomIndicator",
                Transform = new(0, Parent.Transform.Height - iThinckness, Parent.Transform.Width, iThinckness)
                {
                    Anchor = Anchor.Left | Anchor.Right | Anchor.Bottom
                },
                Style = new()
                {
                    BackColor = SKColors.LightGray,
                }
            };

            TestElement = new VisualElement()
            {
                Name = "TestElement",
                Text = "Testing",
                Transform = new(Parent.Transform.Width / 2f - 120 / 2f, Parent.Transform.Height / 2f - 80 / 2f, 120, 80)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    FixedWidth = true,
                    FixedHeight = true
                },
                Style = new()
                {
                    BackColor = SKColors.DarkSlateGray,
                    Border = new()
                    {
                        Width = 2f,
                        Color = SKColors.Black,
                    },
                    Text = new()
                    {
                        Padding = 0,
                        Color = SKColors.White,
                        Size = 28,
                        Alignment = TextAlign.Center
                    }
                },
            };

            Parent.AddChild(TestElement);
            Parent.AddChild(LeftIndicator);
            Parent.AddChild(RightIndicator);
            Parent.AddChild(TopIndicator);
            Parent.AddChild(BottomIndicator);

            Elements.AddElement(ref TestElement, this);
            Elements.AddElement(ref Parent, this);
            Elements.AddElement(ref LeftIndicator, this);
            Elements.AddElement(ref RightIndicator, this);
            Elements.AddElement(ref TopIndicator, this);
            Elements.AddElement(ref BottomIndicator, this);

            stopwatch.Start();
        }

        private float progress = 0;
        private bool increase = true;
        private float duration = 2500;
        private Stopwatch stopwatch = new Stopwatch();
        private void Update()
        {
            // if (increase)
            // {
            //     progress = stopwatch.ElapsedMilliseconds / duration;
            //     if (stopwatch.ElapsedMilliseconds >= duration)
            //     {
            //         progress = 1;
            //         increase = false;
            //         stopwatch.Restart();
            //     }
            // }
            // else
            // {
            //     progress = 1 - (stopwatch.ElapsedMilliseconds / duration);
            //     if (stopwatch.ElapsedMilliseconds >= duration)
            //     {
            //         progress = 0;
            //         increase = true;
            //         stopwatch.Restart();
            //     }
            // }

            // Parent.Transform.Width = smoothLerp(250, 450, progress);
            // Parent.Transform.Height = smoothLerp(320, 365, progress);
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