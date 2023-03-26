using System.Drawing;
using System;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using Blossom.Core.Delegates.Common;
using SkiaSharp;
using Blossom.Core.Render;

namespace Blossom.Core
{
    public abstract class View : IDisposable
    {
        public EventMap Events = new();
        public ElementTree Elements = new();
<<<<<<< Updated upstream
        internal RenderCycle RenderFlow = new();
=======
        internal RenderCycle RenderCycle = new();
>>>>>>> Stashed changes

        public event ForVoid Loop;

        internal bool IsLoaded { get; set; } = false;

        private bool _renderRequired = true;
        public bool RenderRequired
        {
            get
            {
                var temp = _renderRequired;
                _renderRequired = false;
                return temp;
            }
            set => _renderRequired = value;
        }

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
            }
        }

        public Application Application { get; internal set; }
        public VisualElement FocusedElement { get; set; }

        public abstract void Main();

        internal View(string name)
        {
            Name = name;

            Events.OnMouseDown += (_, args) =>
            {
                var element = Elements.FirstFromPoint(
                    new(args.Global.X, args.Global.Y));

                element?.Events.HandleMouseDown(args.Button, args.Global, element);
                mouseDownElement = element;
            };

            Events.OnMouseUp += (_, args) =>
            {
                if (mouseDownElement != null)
                {
                    mouseDownElement.Events.HandleMouseUp(args.Button, args.Global, mouseDownElement);
                    mouseDownElement = null;
                    return;
                }

                var element = Elements.FirstFromPoint(new(args.Global.X, args.Global.Y));
                element?.Events.HandleMouseUp(args.Button, args.Global, element);
            };

            Events.OnMouseMove += (_, args) =>
            {
                var element = Elements.FirstFromPoint(new(args.Global.X, args.Global.Y));
                element?.Events.HandleMouseMove(args.Global, element);

                if (hoveredElement != element)
                {
                    hoveredElement?.Events.HandleMouseLeave(hoveredElement);
                    element?.Events.HandleMouseEnter(element);
                }
                else if (element == hoveredElement)
                {
                    element?.Events.HandleMouseHover(element, args.Global);
                }

                hoveredElement = element;
            };
        }

        internal void TriggerLoop() => Loop?.Invoke();

        public void AddElement(VisualElement element)
        {
            element.ParentView = this;
            Elements.AddElement(ref element, this);

            element.AddedToView();
            Browser.BrowserApp.ActiveView.RenderRequired = true;
        }

        public void RemoveElement(VisualElement element)
        {
            Elements.RemoveElement(element);
            Browser.BrowserApp.ActiveView.RenderRequired = true;
        }

        public void TrackElement(ref VisualElement element)
        {
            Elements.AddElement(ref element, this);
            element.AddedToView();
            Browser.BrowserApp.ActiveView.RenderRequired = true;
        }

        public void UntrackElement(ref VisualElement element)
        {

            Elements.RemoveElement(element);
            Browser.BrowserApp.ActiveView.RenderRequired = true;
        }

        internal void Render()
        {
            foreach (var element in Elements.Items)
            {
                lock (element)
                {
                    using (new SKAutoCanvasRestore(Renderer.Canvas))
                    {
                        element.Render();
                    }
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
            RenderRequired = true;
        }

        public void Dispose()
        {

        }
    }
}