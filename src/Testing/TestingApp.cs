using System;
using Rux.Core;
using Rux.Core.Input;
using Rux.Core.Visual;
using System.Drawing;

namespace Rux.Testing
{
    public class TestingApplication : Application
    {
        private PrettyUi BrowserView;
        private LoadView LoadView;

        public TestingApplication()
        {
            this.Events.Access = EventAccess.Keyboard;

            BrowserView = new PrettyUi();
            LoadView = new LoadView();

            AddView(BrowserView);
            // AddView(LoadView);

            SetActiveView(BrowserView);
        }
    }
}