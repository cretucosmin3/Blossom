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