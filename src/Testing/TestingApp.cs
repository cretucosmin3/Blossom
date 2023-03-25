using System.Collections.Generic;
using System;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using System.Drawing;

namespace Blossom.Testing
{
    public class TestingApplication : Application
    {
        private readonly PrettyUi PrettyUi = new();
        private readonly LoadView LoadView = new();
        private readonly AnchorsView AnchorsView = new();
        private readonly ViewportTest ViewportTest = new();
        private readonly ChildrenAxis ChildrenAxis = new();
        private readonly GridTest GridTest = new();
        private readonly DatePicker DatePickerView = new();

        private readonly Dictionary<int, View> ViewSelectors;

        public TestingApplication()
        {
            AddView(PrettyUi);
            AddView(AnchorsView);
            AddView(LoadView);
            AddView(ViewportTest);
            AddView(ChildrenAxis);
            AddView(GridTest);
            AddView(DatePickerView);

            SetActiveView(LoadView);

            ViewSelectors = new Dictionary<int, View>(){
                {59, PrettyUi},
                {60, AnchorsView},
                {61, LoadView},
                {62, ViewportTest},
                {63, ChildrenAxis},
                {64, GridTest},
                {65, DatePickerView},
            };

            Events.OnKeyUp += (int keyPressed) =>
            {
                if (!ViewSelectors.ContainsKey(keyPressed)) return;
                SetActiveView(ViewSelectors[keyPressed]);
            };
        }
    }
}