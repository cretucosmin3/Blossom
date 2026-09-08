using System;
using System.IO;
using System.Threading.Tasks;
using Blossom.Testing;

namespace Blossom
{
    internal static class BlossomEntry
    {
        private static readonly string[] _args = Environment.GetCommandLineArgs();
        private static readonly string _appName = Path.GetFileNameWithoutExtension(_args.Length > 0 ? _args[0] : "Blossom");
        private static readonly string _appPath = AppDomain.CurrentDomain.BaseDirectory;

        static void Main()
        {   
            Log.Initialize();
            Log.Info($"Starting {_appName}");
            Log.Info($"Application directory: {_appPath}");
            Log.Info($"Log file: {Log.LogFilePath}");

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Fatal($"Unhandled domain exception: {e.ExceptionObject}");
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Log.Error($"Unobserved task exception: {e.Exception}");
                e.SetObserved();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Log.Info("Application exiting");

            foreach (var arg in _args)
            {
                if (arg.Equals("--show-fps", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--fps", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--debug-overlay", StringComparison.OrdinalIgnoreCase))
                {
                    Browser.ShowDebugOverlay = true;
                }
            }

            Blossom.Core.BenchmarkManager.CheckArgs(Environment.GetCommandLineArgs());

            // Run SKSL Shader verification tests
            Blossom.Core.Visual.SKSLShaderManager.TestCompilation();

            Browser.Initialize(new TestingApplication());
            Environment.Exit(0);
        }
    }
}