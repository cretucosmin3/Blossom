using System.Threading;
using Kara.Core;
using Kara.Core.Visual;
using System.Drawing;

namespace Kara.Testing
{
    public class Parenting : View
    {
        float speed = 3f;
        float from = 10;
        float to = 100;
        bool oscilateDirectiton = true;
        int elements = 500;

        VisualElement p = new VisualElement()
        {
            Name = "p",
            Text = "",
            X = 10,
            Y = 10,
            Width = 200f,
            Height = 700f,
            FontSize = 20,
            BorderWidth = 1f,
            BorderColor = Color.DarkGray,
            TextAlignment = TextAlign.Bottom,
            TextPadding = 10,
            Anchor = Anchor.Top | Anchor.Left,
        };

        public Parenting() : base("Parenting View")
        {
            // Browser.ShowFps();
            float elHeight = p.Height / (float)elements;

            Elements.AddElement(ref p, this);

            float y = 0;
            for (int i = 0; i < elements; i++)
            {
                float opacity = (float)i / (float)elements;
                VisualElement newElement = new VisualElement()
                {
                    Name = "c" + i,
                    Text = "",
                    X = 0,
                    Y = y,
                    Width = p.Width,
                    Height = elHeight,
                    BackColor = Color.FromArgb((int)(255f * opacity), Color.Black),
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right,
                };

                y += elHeight;

                Elements.AddElement(ref newElement, this);
                p.AddChild(newElement);
            }
        }

        public override void Main()
        {
            Loop += Update;
            // Browser.ShowFps();
        }

        private void Update()
        {
            if (oscilateDirectiton)
            {
                if (p.X < to)
                {
                    p.X += speed;
                    p.Width += speed;
                }
                else
                    oscilateDirectiton = false;
            }
            else
            {
                if (p.X > from)
                {
                    p.X -= speed;
                    p.Width -= speed;
                }
                else
                    oscilateDirectiton = true;
            }
        }
    }
}