using System;
using Blossom.Core;
using Blossom.Testing.Views;

namespace Blossom.Testing
{
    public class TestingApplication : Application
    {
        private readonly StudioView? _studioView;
        private readonly BenchmarkStaticView? _benchStaticView;
        private readonly BenchmarkDynamicView? _benchDynamicView;

        public TestingApplication()
        {
            Title = "Blossom Studio";
            EnableStatsOverlay = true;

            if (BenchmarkManager.IsBenchmarkMode)
            {
                Log.Info("Booting up in Benchmark mode...");

                _benchStaticView = new BenchmarkStaticView();
                _benchDynamicView = new BenchmarkDynamicView();

                AddView(_benchStaticView);
                AddView(_benchDynamicView);

                BenchmarkManager.OnRequestNextBenchmarkView += () =>
                {
                    SetActiveView(_benchDynamicView);
                };

                SetActiveView(_benchStaticView);
                return;
            }

            DemoThemes.Ensure();
            _studioView = new StudioView();
            AddView(_studioView);
            SetActiveView(_studioView);
        }
    }
}