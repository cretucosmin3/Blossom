using System;
using Kara.Core;
using Kara.Core.Input;
using Kara.Core.Visual;
using System.Drawing;

namespace Kara.Testing
{
    public class TestingApplication : Application
    {
        private PrettyUi PrettyView;

        public TestingApplication()
        {
            this.Events.Access = EventAccess.Keyboard;

            PrettyView = new PrettyUi();

            AddView(PrettyView);

            SetActiveView(PrettyView);
        }
    }
}