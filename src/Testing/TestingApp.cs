using System.Collections.Generic;
using Blossom.Core;

namespace Blossom.Testing
{
    public class TestingApplication : Application
    {
        private readonly PrettyUi PrettyUi = new();
        private readonly LoadView LoadView = new();
        private readonly AnchorsView AnchorsView = new();
        private readonly ChildrenAxis ChildrenAxis = new();
        private readonly GridTest GridTest = new();
        private readonly NeonView DatePickerView = new();

        private readonly Dictionary<int, View> ViewSelectors;

        public TestingApplication()
        {
            AddView(PrettyUi);
            AddView(AnchorsView);
            AddView(LoadView);
            AddView(ChildrenAxis);
            AddView(GridTest);
            AddView(DatePickerView);

            SetActiveView(AnchorsView);

            ViewSelectors = new Dictionary<int, View>(){
                {59, PrettyUi},
                {60, AnchorsView},
                {61, LoadView},
                {62, ChildrenAxis},
                {63, GridTest},
                {64, DatePickerView},
            };

            Events.OnKeyUp += (int keyPressed) =>
            {
                if (!ViewSelectors.ContainsKey(keyPressed)) return;
                SetActiveView(ViewSelectors[keyPressed]);
            };
        }
    }
}