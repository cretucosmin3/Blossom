using System;
using System.Collections.Generic;
using Blossom.Core;
using Blossom.Testing.Views;
using Silk.NET.Input;

namespace Blossom.Testing
{
    public class TestingApplication : Application
    {
        private readonly DashboardView _dashboardView;
        private readonly NeonShowcaseView _neonShowcaseView;
        private readonly PaintAppView _paintAppView;
        private readonly KanbanBoardView _kanbanBoardView;
        private readonly Transform3DView _transform3DView;
        private readonly GlassmorphismShowcaseView _glassShowcaseView;

        private readonly BenchmarkStaticView? _benchStaticView;
        private readonly BenchmarkDynamicView? _benchDynamicView;

        public TestingApplication()
        {
            if (BenchmarkManager.IsBenchmarkMode)
            {
                Console.WriteLine("[BENCHMARK] Booting up in Benchmark mode...");

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

            // Normal Mode
            _dashboardView = new DashboardView();
            _neonShowcaseView = new NeonShowcaseView();
            _paintAppView = new PaintAppView();
            _kanbanBoardView = new KanbanBoardView();
            _transform3DView = new Transform3DView();
            _glassShowcaseView = new GlassmorphismShowcaseView();

            AddView(_dashboardView);
            AddView(_neonShowcaseView);
            AddView(_paintAppView);
            AddView(_kanbanBoardView);
            AddView(_transform3DView);
            AddView(_glassShowcaseView);

            // Hook up view transition callback events
            _dashboardView.OnSwitchView += () =>
            {
                SetActiveView(_neonShowcaseView);
            };
            _dashboardView.OnSwitchToNeon += () =>
            {
                SetActiveView(_neonShowcaseView);
            };
            _dashboardView.OnSwitchToPaint += () =>
            {
                SetActiveView(_paintAppView);
            };
            _dashboardView.OnSwitchToKanban += () =>
            {
                SetActiveView(_kanbanBoardView);
            };
            _dashboardView.OnSwitchTo3D += () =>
            {
                SetActiveView(_transform3DView);
            };
            _dashboardView.OnSwitchToGlass += () =>
            {
                SetActiveView(_glassShowcaseView);
            };

            _neonShowcaseView.OnSwitchView += () =>
            {
                SetActiveView(_dashboardView);
            };
            _neonShowcaseView.OnSwitchToPaint += () =>
            {
                SetActiveView(_paintAppView);
            };
            _neonShowcaseView.OnSwitchToKanban += () =>
            {
                SetActiveView(_kanbanBoardView);
            };
            _neonShowcaseView.OnSwitchToGlass += () =>
            {
                SetActiveView(_glassShowcaseView);
            };

            _paintAppView.OnSwitchToDashboard += () =>
            {
                SetActiveView(_dashboardView);
            };
            _paintAppView.OnSwitchToNeonShowcase += () =>
            {
                SetActiveView(_neonShowcaseView);
            };
            _paintAppView.OnSwitchToKanban += () =>
            {
                SetActiveView(_kanbanBoardView);
            };
            _paintAppView.OnSwitchTo3D += () =>
            {
                SetActiveView(_transform3DView);
            };
            _paintAppView.OnSwitchToGlass += () =>
            {
                SetActiveView(_glassShowcaseView);
            };

            _kanbanBoardView.OnSwitchToDashboard += () =>
            {
                SetActiveView(_dashboardView);
            };
            _kanbanBoardView.OnSwitchToNeonShowcase += () =>
            {
                SetActiveView(_neonShowcaseView);
            };
            _kanbanBoardView.OnSwitchToPaint += () =>
            {
                SetActiveView(_paintAppView);
            };
            _kanbanBoardView.OnSwitchTo3D += () =>
            {
                SetActiveView(_transform3DView);
            };
            _kanbanBoardView.OnSwitchToGlass += () =>
            {
                SetActiveView(_glassShowcaseView);
            };

            _transform3DView.OnSwitchToDashboard += () =>
            {
                SetActiveView(_dashboardView);
            };

            _glassShowcaseView.OnSwitchToDashboard += () =>
            {
                SetActiveView(_dashboardView);
            };
            _glassShowcaseView.OnSwitchToNeon += () =>
            {
                SetActiveView(_neonShowcaseView);
            };
            _glassShowcaseView.OnSwitchToPaint += () =>
            {
                SetActiveView(_paintAppView);
            };
            _glassShowcaseView.OnSwitchToKanban += () =>
            {
                SetActiveView(_kanbanBoardView);
            };
            _glassShowcaseView.OnSwitchTo3D += () =>
            {
                SetActiveView(_transform3DView);
            };

            // Set the dashboard as the starting view
            SetActiveView(_dashboardView);

            // Handle Hotkeys to switch views
            // Silk.NET Keys: 
            // Key.Number1 = 49, Key.D = 68 -> Dashboard
            // Key.Number2 = 50, Key.N = 78 -> Neon
            // Key.Number3 = 51, Key.P = 80 -> Paint Canvas
            // Key.Number4 = 52, Key.K = 75 -> Kanban Task Board
            // Key.Number5 = 53, Key.T = 84 -> 3D Showcase
            // Key.Number6 = 54, Key.G = 71 -> Glass Showcase
            Events.OnKeyUp += (int keyPressed) =>
            {
                if (keyPressed == 49 || keyPressed == 68) // '1' or 'D'
                {
                    Console.WriteLine("Hotkey: Switching to Dashboard View");
                    SetActiveView(_dashboardView);
                }
                else if (keyPressed == 50 || keyPressed == 78) // '2' or 'N'
                {
                    Console.WriteLine("Hotkey: Switching to Neon Showcase View");
                    SetActiveView(_neonShowcaseView);
                }
                else if (keyPressed == 51 || keyPressed == 80) // '3' or 'P'
                {
                    Console.WriteLine("Hotkey: Switching to Paint Canvas View");
                    SetActiveView(_paintAppView);
                }
                else if (keyPressed == 52 || keyPressed == 75) // '4' or 'K'
                {
                    Console.WriteLine("Hotkey: Switching to Kanban View");
                    SetActiveView(_kanbanBoardView);
                }
                else if (keyPressed == 53 || keyPressed == 84) // '5' or 'T'
                {
                    Console.WriteLine("Hotkey: Switching to 3D Showcase View");
                    SetActiveView(_transform3DView);
                }
                else if (keyPressed == 54 || keyPressed == 71) // '6' or 'G'
                {
                    Console.WriteLine("Hotkey: Switching to Glass Showcase View");
                    SetActiveView(_glassShowcaseView);
                }
            };
        }
    }
}