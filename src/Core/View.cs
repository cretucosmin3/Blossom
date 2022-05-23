using System;
using Rux.Core.Visual;
using Rux.Core.Input;
using Rux.Core.Delegates.Common;

namespace Rux.Core
{
    public abstract class View : IDisposable
    {
        public EventMap Events = new();
        public ElementsMap Elements = new();
        public event ForVoid Loop;
        private int DefaultFont;
        private VisualElement hoveredElement;

        private string _title = "";
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                //! #render title only
            }
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                //! #render title only
            }
        }

        public Application Application { get; internal set; }
        public VisualElement FocusedElement { get; set; }

        public abstract void Main();

        internal View(string name)
        {
            Name = name;

            Browser.OnLoaded += () =>
            {
                Main();
            };

            Events.OnMouseDown += (btn, pos) =>
            {
                var element = Elements.FirstFromPoint(new System.Drawing.PointF(pos.X, pos.Y));
                element?.Events.HandleMouseDown(btn, pos);
            };

            Events.OnMouseUp += (btn, pos) =>
            {
                var element = Elements.FirstFromPoint(new System.Drawing.PointF(pos.X, pos.Y));
                element?.Events.HandleMouseUp(btn, pos);
            };

            Events.OnMouseMove += (pos) =>
            {
                var element = Elements.FirstFromPoint(new System.Drawing.PointF(pos.X, pos.Y));
                element?.Events.HandleMouseMove(pos);

                if (hoveredElement != element)
                {
                    hoveredElement?.Events.HandleMouseLeave(hoveredElement);
                    element?.Events.HandleMouseEnter(element);
                }
                else if (element == hoveredElement)
                {
                    element?.Events.HandleMouseHover(element, pos);
                }

                hoveredElement = element;
            };
        }

        internal void TriggerLoop() => Loop?.Invoke();

        public void AddElement(VisualElement element)
        {
            Elements.AddElement(ref element, this);
        }

        public void RemoveElement(VisualElement element)
        {
            Elements.RemoveElement(element);
        }

        internal void Render()
        {
            foreach (var element in Elements.Items)
            {
                element.Render();
            }
        }

        public void Dispose()
        {

        }
    }
}