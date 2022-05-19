using System.Numerics;
using System.Net.Mime;
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

            // this.Events.OnMouseClick += (int button, Vector2 pos) =>
            // {
            //     if (button > 0) return;
            //     Parent.Transform.X = pos.X;
            //     Parent.Transform.Y = pos.Y;
            // };

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
                        LeftIndicator.Style.BackColor = !hasFlag ? SKColors.IndianRed : SKColors.LightGray;

                        if (hasFlag)
                            TestElement.Transform.Anchor &= ~Anchor.Left;
                        else
                            TestElement.Transform.Anchor |= Anchor.Left;

                        break;
                    case 'd':
                        hasFlag = TestElement.Transform.Anchor.HasFlag(Anchor.Right);
                        RightIndicator.Style.BackColor = !hasFlag ? SKColors.IndianRed : SKColors.LightGray;

                        if (hasFlag)
                            TestElement.Transform.Anchor &= ~Anchor.Right;
                        else
                            TestElement.Transform.Anchor |= Anchor.Right;

                        break;
                    case 'w':
                        hasFlag = TestElement.Transform.Anchor.HasFlag(Anchor.Top);
                        TopIndicator.Style.BackColor = !hasFlag ? SKColors.IndianRed : SKColors.LightGray;

                        if (hasFlag)
                            TestElement.Transform.Anchor &= ~Anchor.Top;
                        else
                            TestElement.Transform.Anchor |= Anchor.Top;

                        break;
                    case 's':
                        hasFlag = TestElement.Transform.Anchor.HasFlag(Anchor.Bottom);
                        BottomIndicator.Style.BackColor = !hasFlag ? SKColors.IndianRed : SKColors.LightGray;

                        if (hasFlag)
                            TestElement.Transform.Anchor &= ~Anchor.Bottom;
                        else
                            TestElement.Transform.Anchor |= Anchor.Bottom;

                        break;
                    default:
                        break;
                }
            };

            // this.Events.OnMouseClick += (int button, Vector2 position) =>
            // {
            //     Console.WriteLine($"{button} {position.ToString("0,0")}");
            // };

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

            int iThinckness = 8;

            Parent = new VisualElement()
            {
                Name = "Parent",
                Text = "Testing",
                Transform = new(55, 55, 150, 150)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right | Anchor.Bottom
                },
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
                    Anchor = Anchor.Left | Anchor.Top | Anchor.Bottom,
                },
                Style = new()
                {
                    BackColor = SKColors.IndianRed,
                }
            };

            RightIndicator = new VisualElement()
            {
                Name = "RightIndicator",
                Transform = new(Parent.Transform.Width - iThinckness, 0, iThinckness, Parent.Transform.Height)
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
                    BackColor = SKColors.IndianRed,
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
                Text = "Hello",
                Transform = new(Parent.Transform.Width / 2f - 80 / 2f, Parent.Transform.Height / 2f - 80 / 2f, 80, 80)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    FixedWidth = true,
                    FixedHeight = true,
                    ValidateOnAnchor = true,
                },
                Style = new()
                {
                    BackColor = SKColors.IndianRed,
                    Border = new()
                    {
                        Width = 2f,
                        Color = SKColors.White,
                    },
                    Text = new()
                    {
                        Alignment = TextAlign.Center,
                        Color = SKColors.White,
                    }
                },
            };

            Parent.AddChild(TestElement);
            Parent.AddChild(LeftIndicator);
            Parent.AddChild(RightIndicator);
            Parent.AddChild(TopIndicator);
            Parent.AddChild(BottomIndicator);

            AddElement(TestElement);
            AddElement(Parent);
            AddElement(LeftIndicator);
            AddElement(RightIndicator);
            AddElement(TopIndicator);
            AddElement(BottomIndicator);

            stopwatch.Start();
        }

        private float progress = 0;
        private bool increase = true;
        private float duration = 200;
        private Stopwatch stopwatch = new Stopwatch();
        private int testStage = 0;

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

            //         testStage++;
            //         ApplyTest(testStage);
            //         stopwatch.Restart();
            //     }
            // }

            // Parent.Transform.Width = smoothLerp(150, 450, progress);
            // Parent.Transform.Height = smoothLerp(150, 450, progress);
        }

        private void ApplyTest(int stage)
        {
            switch (stage)
            {
                case 1:
                    TestElement.Transform.Anchor = Anchor.Right | Anchor.Bottom;
                    break;
                case 2:
                    TestElement.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top;
                    break;
                case 3:
                    TestElement.Transform.Anchor = Anchor.Left | Anchor.Right;
                    break;
                case 4:
                    TestElement.Transform.Anchor = Anchor.Top | Anchor.Bottom;
                    break;
                case 5:
                    TestElement.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
                    break;
                case 6:
                    TestElement.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top;
                    break;
                case 7:
                    TestElement.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Bottom;
                    break;
                case 8:
                    TestElement.Transform.Anchor = Anchor.Left | Anchor.Bottom;
                    break;
                case 9:
                    TestElement.Transform.Anchor = Anchor.Top | Anchor.Right;
                    break;
                case 10:
                    TestElement.Transform.Anchor = Anchor.Left | Anchor.Top;
                    testStage = 0;
                    break;
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