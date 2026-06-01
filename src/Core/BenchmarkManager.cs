using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Blossom.Core
{
    public static class BenchmarkManager
    {
        public static bool IsBenchmarkMode { get; set; } = false;
        public static readonly int FramesPerView = 60; // Total frames to benchmark per view

        private static readonly List<double> CurrentViewFrameTimes = new();
        private static readonly Dictionary<string, ViewStats> ViewResults = new();
        
        private static readonly Stopwatch FrameStopwatch = new();
        private static int FrameCount = 0;
        private static string CurrentViewName = "";

        public struct ViewStats
        {
            public string Name;
            public int TotalFrames;
            public double TotalTimeMs;
            public double AvgMs;
            public double MinMs;
            public double MaxMs;
            public double Fps;
        }

        public static void CheckArgs(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.Equals("--benchmark", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-b", StringComparison.OrdinalIgnoreCase))
                {
                    IsBenchmarkMode = true;
                }
            }
        }

        public static void StartFrame(string viewName)
        {
            CurrentViewName = viewName;
            FrameStopwatch.Restart();
        }

        public static void EndFrame()
        {
            FrameStopwatch.Stop();
            double elapsed = FrameStopwatch.Elapsed.TotalMilliseconds;
            
            CurrentViewFrameTimes.Add(elapsed);
            FrameCount++;

            // Switch view or exit when frame budget is met
            if (FrameCount >= FramesPerView)
            {
                RecordResults();
                TransitionToNext();
            }
            else
            {
                // Force continuous rendering
                if (Browser.BrowserApp.ActiveView != null)
                {
                    Browser.BrowserApp.ActiveView.RenderRequired = true;
                }
            }
        }

        private static void RecordResults()
        {
            if (CurrentViewFrameTimes.Count == 0) return;

            double totalTime = CurrentViewFrameTimes.Sum();
            double avg = totalTime / CurrentViewFrameTimes.Count;
            double min = CurrentViewFrameTimes.Min();
            double max = CurrentViewFrameTimes.Max();
            double fps = (CurrentViewFrameTimes.Count / totalTime) * 1000.0;

            ViewResults[CurrentViewName] = new ViewStats
            {
                Name = CurrentViewName,
                TotalFrames = CurrentViewFrameTimes.Count,
                TotalTimeMs = totalTime,
                AvgMs = avg,
                MinMs = min,
                MaxMs = max,
                Fps = fps
            };
        }

        private static void TransitionToNext()
        {
            FrameCount = 0;
            CurrentViewFrameTimes.Clear();

            var app = Browser.BrowserApp;
            if (app == null) return;

            // Simple state machine: if we were on BenchmarkStatic, switch to BenchmarkDynamic
            if (app.ActiveView.Name == "Benchmark - Static Grid")
            {
                Console.WriteLine("\n[BENCHMARK] Switching to Dynamic Mutation View...");
                
                // Find dynamic view in app Views (we will set it up in TestingApp.cs)
                var nextView = app.ActiveView.Application.ActiveView; 
                // We'll let TestingApp handle switching by exposing an event or checking active view
                // Let's implement active view switching in TestingApp
                TestingAppSwitchBenchmarkView();
            }
            else
            {
                // We completed both views! Print final summary table and exit.
                PrintSummaryTable();
                Environment.Exit(0);
            }
        }

        public static Action? OnRequestNextBenchmarkView;

        private static void TestingAppSwitchBenchmarkView()
        {
            OnRequestNextBenchmarkView?.Invoke();
        }

        private static void PrintSummaryTable()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n==========================================================================");
            Console.WriteLine("                      BLOSSOM OS RENDERING BENCHMARK                      ");
            Console.WriteLine("==========================================================================");
            Console.ResetColor();

            Console.WriteLine(string.Format("{0,-30} | {1,8} | {2,10} | {3,10} | {4,10} | {5,8}", 
                "View Name", "Frames", "Avg (ms)", "Min (ms)", "Max (ms)", "Avg FPS"));
            Console.WriteLine(new string('-', 85));

            foreach (var result in ViewResults.Values)
            {
                if (result.Fps > 60) Console.ForegroundColor = ConsoleColor.Green;
                else if (result.Fps > 30) Console.ForegroundColor = ConsoleColor.Yellow;
                else Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine(string.Format("{0,-30} | {1,8} | {2,10:F2} | {3,10:F2} | {4,10:F2} | {5,8:F1}",
                    result.Name, result.TotalFrames, result.AvgMs, result.MinMs, result.MaxMs, result.Fps));
            }
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==========================================================================\n");
            Console.ResetColor();
        }
    }
}
