using System;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using System.Drawing;

namespace Blossom.Testing
{
    public class TestingApplication : Application
    {
        // private PrettyUi BrowserView;
        // private LoadView LoadView;
        private AnchorsView AnchorsView;

        public TestingApplication()
        {
            this.Events.Access = EventAccess.Keyboard;

            // BrowserView = new PrettyUi();
            // LoadView = new LoadView();
            AnchorsView = new AnchorsView();

            // AddView(BrowserView);
            AddView(AnchorsView);
            // AddView(LoadView);

            SetActiveView(AnchorsView);
        }
    }
}