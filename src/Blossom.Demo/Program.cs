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

            bool enableStatsOverlay = true;
            foreach (var arg in _args)
            {
                if (arg.Equals("--no-fps", StringComparison.OrdinalIgnoreCase))
                {
                    enableStatsOverlay = false;
                }
            }

            Blossom.Core.BenchmarkManager.CheckArgs(Environment.GetCommandLineArgs());

            // Run SKSL Shader verification tests
            Blossom.Core.Visual.SKSLShaderManager.TestCompilation();

            Shell.MaxFps = 10;
            for (int i = 0; i < _args.Length; i++)
            {
                if (_args[i].Equals("--max-fps", StringComparison.OrdinalIgnoreCase) && i + 1 < _args.Length && int.TryParse(_args[i + 1], out int customFps))
                {
                    Shell.MaxFps = customFps;
                }
            }
            Log.Info($"Configured Shell.MaxFps = {Shell.MaxFps}");

            var app = new TestingApplication();
            app.EnableStatsOverlay = true;
            Shell.ShowDebugOverlay = enableStatsOverlay;

            Shell.Initialize(app);
            Environment.Exit(0);
        }
    }
}