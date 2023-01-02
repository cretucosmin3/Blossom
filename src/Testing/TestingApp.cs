using System;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using System.Drawing;

namespace Blossom.Testing
{
    public class TestingApplication : Application
    {
        // private readonly PrettyUi BrowserView;
        // private readonly LoadView LoadView;
        // private readonly AnchorsView AnchorsView;
        private readonly ViewportTest ViewportTest;

        public TestingApplication()
        {
            this.Events.Access = EventAccess.Keyboard;

            // BrowserView = new PrettyUi();
            // LoadView = new LoadView();
            // AnchorsView = new AnchorsView();
            ViewportTest = new ViewportTest();

            // AddView(BrowserView);
            // AddView(AnchorsView);
            // AddView(LoadView);
            AddView(ViewportTest);

            SetActiveView(ViewportTest);
        }
    }
}