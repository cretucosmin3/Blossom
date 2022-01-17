using System;
using Kara.Core;
using Kara.Core.Input;
using Kara.Core.Visual;
using System.Drawing;

namespace Kara.Testing
{
    public class TestingApplication : Application
    {
        private Parenting ParentingView;
        private PrettyUi PrettyView;

        public TestingApplication()
        {
            Console.WriteLine("TestingApplication()");
            this.Events.Access = EventAccess.Keyboard;

            ParentingView = new Parenting();
            PrettyView = new PrettyUi();

            AddView(ParentingView);
            AddView(PrettyView);

            SetActiveView(PrettyView);
        }
    }
}