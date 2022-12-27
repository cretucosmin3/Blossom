
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;
using System.Collections.Generic;

namespace Blossom.Testing
{
    public class LoadView : View
    {
        public LoadView() : base("LoadView View") { }

        public override void Main()
        {
            List<VisualElement> LoadElements = new List<VisualElement>();

            float totalWidth = Browser.window.Size.X;
            float totalHeight = Browser.window.Size.Y;
            float columns = 140;
            float rows = 140;
            float margin = 0;

            totalWidth -= margin * 2f;
            totalHeight -= margin * 2f;

            float rectWidth = (totalWidth - (columns - 1) * margin) / columns;
            float rectHeight = (totalHeight - (rows - 1) * margin) / rows;

            for (float row = 0f; row < rows; row++)
            {
                for (float col = 0f; col < columns; col++)
                {
                    float x = col * (rectWidth + margin) + margin;
                    float y = row * (rectHeight + margin) + margin;

                    var NewElement = new VisualElement()
                    {
                        Name = $"element {row}:{col}",
                        Transform = new(x, y, rectWidth, rectHeight),
                        Style = new()
                        {
                            IsClipping = true,
                            BackColor = new(22, 22, 22, 45),
                            Text = new()
                            {
                                Size = 14,
                                Color = SKColors.WhiteSmoke,
                                Alignment = TextAlign.Center
                            },
                            Border = new()
                            {
                                Roundness = 0,
                                Width = 1f,
                                Color = new(0, 0, 0, 120)
                            },
                        },
                        // Text = "X"
                    };

                    NewElement.Events.OnMouseEnter += OnMouseEnter;
                    NewElement.Events.OnMouseLeave += OnMouseLeave;

                    LoadElements.Add(NewElement);
                }
            }

            foreach (var element in LoadElements)
            {
                AddElement(element);
            }
        }

        public void OnMouseEnter(VisualElement el)
        {
            el.Style.BackColor = new(200, 50, 35, 255);
        }

        public void OnMouseLeave(VisualElement el)
        {
            el.Style.BackColor = new(22, 22, 22, 120);
        }
    }
}