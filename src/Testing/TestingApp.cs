using System.Collections.Generic;
using Blossom.Core;

namespace Blossom.Testing
{
    public class TestingApplication : Application
    {
        // private readonly PrettyUi PrettyUi = new();
        // private readonly DrawingView DrawingView = new();
        // private readonly AnchorsView AnchorsView = new();
        // private readonly ChildrenAxis ChildrenAxis = new();
        // private readonly GridTest GridTest = new();
        // private readonly NeonView NeonView = new();
        // private readonly RenderCacheView RenderCacheView = new();
        private readonly AppNavigation AppNav = new();

        private readonly Dictionary<int, View> ViewSelectors;

        public TestingApplication()
        {
            AddView(AppNav);
            // AddView(PrettyUi);
            // AddView(AnchorsView);
            // AddView(DrawingView);
            // AddView(ChildrenAxis);
            // AddView(GridTest);
            // AddView(NeonView);
            // AddView(RenderCacheView);

            SetActiveView(AppNav);

            // ViewSelectors = new Dictionary<int, View>(){
            //     {59, PrettyUi},
            //     {60, AnchorsView},
            //     {61, DrawingView},
            //     {62, ChildrenAxis},
            //     {63, GridTest},
            //     {64, NeonView},
            // };

            // Events.OnKeyUp += (int keyPressed) =>
            // {
            //     if (!ViewSelectors.ContainsKey(keyPressed)) return;
            //     SetActiveView(ViewSelectors[keyPressed]);
            // };
        }
    }
}