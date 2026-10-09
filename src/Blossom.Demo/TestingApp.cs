using System;
using Blossom.Core;
using Blossom.Testing.Frame;
using Blossom.Testing.Views;

namespace Blossom.Testing
{
    public class TestingApplication : Application
    {
        public TestingApplication()
        {
            Title = "Blossom Gallery";
            EnableStatsOverlay = true;

            if (BenchmarkManager.IsBenchmarkMode)
            {
                Log.Info("Booting up in Benchmark mode...");

                var benchStaticView = new BenchmarkStaticView();
                var benchDynamicView = new BenchmarkDynamicView();

                AddView(benchStaticView);
                AddView(benchDynamicView);

                BenchmarkManager.OnRequestNextBenchmarkView += () =>
                {
                    SetActiveView(benchDynamicView);
                };

                SetActiveView(benchStaticView);
                return;
            }

            var gallery = new GalleryView();
            AddView(gallery);
            SetActiveView(gallery);
        }
    }
}
