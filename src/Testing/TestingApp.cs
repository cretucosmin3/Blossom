using System;
using Blossom.Core;
using Blossom.Testing.Views;

namespace Blossom.Testing
{
    public class TestingApplication : Application
    {
        private readonly KanbanView? _kanbanView;
        private readonly BenchmarkStaticView? _benchStaticView;
        private readonly BenchmarkDynamicView? _benchDynamicView;

        public TestingApplication()
        {
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

            // Normal Mode - Single Kanban View
            _kanbanView = new KanbanView();
            AddView(_kanbanView);
            SetActiveView(_kanbanView);
        }
    }
}