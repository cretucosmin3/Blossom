using System;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using Blossom.Core.Delegates.Common;

namespace Blossom.Core
{
    public abstract class View : IDisposable
    {
        public EventMap Events = new();
        public ElementsMap Elements = new();
        public event ForVoid Loop;

        private bool _renderRequired = true;
        public bool renderRequired
        {
            get
            {
                var temp = _renderRequired;
                _renderRequired = false;
                return temp;
            }
            set => _renderRequired = value;
        }

        private int DefaultFont;
        private VisualElement hoveredElement;
        private VisualElement mouseDownElement;

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
                // Console.WriteLine("-- OnLoaded --");
                // Main();
            };

            Events.OnMouseDown += (btn, pos, relative) =>
            {
                var element = Elements.FirstFromPoint(new System.Drawing.PointF(pos.X, pos.Y));
                element?.Events.HandleMouseDown(btn, pos, element);
                mouseDownElement = element;
            };

            Events.OnMouseUp += (btn, pos, relative) =>
            {
                if (mouseDownElement != null)
                {
                    mouseDownElement.Events.HandleMouseUp(btn, pos, mouseDownElement);
                    mouseDownElement = null;
                    return;
                }

                var element = Elements.FirstFromPoint(new System.Drawing.PointF(pos.X, pos.Y));
                element?.Events.HandleMouseUp(btn, pos, element);
            };

            Events.OnMouseMove += (pos, relative) =>
            {
                var element = Elements.FirstFromPoint(new System.Drawing.PointF(pos.X, pos.Y));
                element?.Events.HandleMouseMove(pos, element);

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
            Browser.BrowserApp.ActiveView.renderRequired = true;
        }

        public void RemoveElement(VisualElement element)
        {
            Elements.RemoveElement(element);
            Browser.BrowserApp.ActiveView.renderRequired = true;
        }

        internal void Render()
        {
            foreach (var element in Elements.Items)
            {
                lock (element)
                {
                    element.Render();
                }
            }
        }

        internal void RenderChanges(Action doChanges)
        {
            // clear state
            doChanges();
            // pull changes
            // update state
            // render from previous state or new
            renderRequired = true;
        }

        public void Dispose()
        {

        }
    }
}