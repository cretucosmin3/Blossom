using System;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using System.Drawing;

namespace Blossom.Testing
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