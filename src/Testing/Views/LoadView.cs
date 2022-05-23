using System.Security.Cryptography;
using System.Numerics;
using System.Net.Mime;
using System;
using System.Diagnostics;
using Rux.Core;
using Rux.Core.Input;
using Rux.Core.Visual;
using System.Drawing;
using System.Threading;
using SkiaSharp;
using System.Linq;
using System.Collections.Generic;

namespace Rux.Testing
{
    public class LoadView : View
    {

        List<VisualElement> LoadElements = new List<VisualElement>();

        public LoadView() : base("LoadView View") { }

        private int counter = 0;
        public override void Main()
        {
            Browser.ShowFps();

            int increase = 10;
            int xy = increase;
            for (int i = 0; i < 50; i++)
            {
                LoadElements.Add(new VisualElement()
                {
                    Name = "load" + i,
                    Text = "Welcome back!",
                    Transform = new(i == 0 ? 300 : increase / 2, i == 0 ? 80 : increase / 2, 500 - xy, 500 - xy)
                    {
                        // Anchor = Anchor.Top,
                        FixedWidth = false,
                        FixedHeight = false,
                        ValidateOnAnchor = false,
                    },
                    Style = new()
                    {
                        Border = new()
                        {
                            Color = new SKColor(255, 255, 255, 15),
                            Width = 1f,
                        },
                    },
                });

                // LoadElements[i].Events.OnMouseEnter += (VisualElement el) =>
                // {
                //     el.Style.Border.Color = SKColors.Red;
                // };

                // LoadElements[i].Events.OnMouseLeave += (VisualElement el) =>
                // {
                //     el.Style.Border.Color = new SKColor(255, 255, 255, 5);
                // };

                xy += increase;
                if(i == 0) continue;

                LoadElements[i - 1].AddChild(LoadElements[i]);

            }

            foreach (var element in LoadElements)
            {
                AddElement(element);
            }

            Loop += Update;
        }

        private float lastSin = 0;

        private void Update()
        {
            // Console.WriteLine(MathF.Sin(lastSin += 0.001f));

            // Button.Style.Border.Width = 3 + (2 * MathF.Sin(lastSin += 0.01f));

            // if (lastSin > 3.14 * 2)
            // {
            //     lastSin = 0;
            // }
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