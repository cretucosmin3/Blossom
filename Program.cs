using System.Reflection;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Blossom
{
    internal static class BlossomEntry
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        private static readonly string[] _args = Environment.GetCommandLineArgs();
        private static readonly string _appName = Path.GetFileNameWithoutExtension(_args[0]);
        private static readonly string _appPath = Path.GetDirectoryName(_args[0]);
        private static readonly string _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _appName);
        private static readonly string _appDataPathConfig = Path.Combine(_appDataPath, "config.json");
        private static readonly string _appDataPathLog = Path.Combine(_appDataPath, "log.txt");
        private static readonly string _appDataPathScreenshot = Path.Combine(_appDataPath, "screenshot.png");

        static void Main()
        {
            Log.Info(_appName);
            Log.Info(_appPath);
            Log.Info(_appDataPath);
            Log.Info(_appDataPathConfig);
            Log.Info(_appDataPathLog);
            Log.Info(_appDataPathScreenshot);

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Log.Debug("Closing");

            Browser.Initialize();
            Environment.Exit(0);
        }
    }
}