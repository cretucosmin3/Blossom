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
                        
                        if (hasFlag)
                            TestElement.Transform.Anchor &= ~Anchor.Left;
                        else
                            TestElement.Transform.Anchor |= Anchor.Left;
                            
                        break;
                    case 'd':
                        hasFlag = TestElement.Transform.Anchor.HasFlag(Anchor.Right);

                        if (hasFlag)
                            TestElement.Transform.Anchor &= ~Anchor.Right;
                        else
                            TestElement.Transform.Anchor |= Anchor.Right;

                        break;
                    case 'w':
                        hasFlag = TestElement.Transform.Anchor.HasFlag(Anchor.Top);

                        if (hasFlag)
                            TestElement.Transform.Anchor &= ~Anchor.Top;
                        else
                            TestElement.Transform.Anchor |= Anchor.Top;

                        break;
                    case 's':
                        hasFlag = TestElement.Transform.Anchor.HasFlag(Anchor.Bottom);

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
                // 333 -> right
                // 331 -> left
                // 326 -> down
                // 328 -> up

                Console.WriteLine(key);

                if(key == 333){
                    Parent.Transform.Width += 20;
                }

                if(key == 331){
                    Parent.Transform.Width -= 20;
                }

                if(key == 336){
                    Parent.Transform.Height += 20;
                }

                if(key == 328){
                    Parent.Transform.Height -= 20;
                }
            };

            Parent = new VisualElement(){
                Name = "Parent",
                Transform = new(50, 50, 400, 400),
                Style = new()
                {
                    BorderColor = SKColors.Black,
                    BackColor = SKColors.AliceBlue,
                    BorderWidth = 2,
                    Roundness = 5
                }
            };

            TestElement = new VisualElement()
            {
                Name = "TestElement",
                Text = "Testing",
                Transform = new(20, 20, 360, 360),
                Style = new()
                {
                    BorderWidth = 2f,
                    Roundness = 6f,
                    BorderColor = SKColors.Black,
                    BackColor = SKColors.DarkGray,
                    Text = new()
                    {
                        Color = SKColors.Black,
                        Size = 26,
                        Alignment = TextAlign.Center
                    }
                },
            };
            Parent.AddChild(TestElement);

            Elements.AddElement(ref TestElement, this);
            Elements.AddElement(ref Parent, this);

            stopwatch.Start();
        }

        private float progress = 0;
        private bool increase = true;
        private float duration = 1000;
        private Stopwatch stopwatch = new Stopwatch();
        private void Update()
        {
            if (increase)
            {
                progress = stopwatch.ElapsedMilliseconds / duration;
                if (stopwatch.ElapsedMilliseconds >= duration)
                {
                    progress = 1;
                    increase = false;
                    stopwatch.Restart();
                }
            }
            else
            {
                progress = 1 - (stopwatch.ElapsedMilliseconds / duration);
                if (stopwatch.ElapsedMilliseconds >= duration)
                {
                    progress = 0;
                    increase = true;
                    stopwatch.Restart();
                }
            }

            Parent.Transform.Width = smoothLerp(200, 450, progress);
            Parent.Transform.Height = smoothLerp(150, 350, progress);
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