using System;
using Rux.Core;
using Rux.Core.Input;
using Rux.Core.Visual;
using System.Drawing;

namespace Rux.Testing
{
    public class TestingApplication : Application
    {
        private PrettyUi PrettyView;
        private LoadView LoadView;

        public TestingApplication()
        {
            this.Events.Access = EventAccess.Keyboard;

            PrettyView = new PrettyUi();
            LoadView = new LoadView();

            AddView(PrettyView);
            AddView(LoadView);

            SetActiveView(PrettyView);
        }
    }
}