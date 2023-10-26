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

        private static void PrintTimeWithMarta()
        {
            DateTime startTime = new DateTime(2019, 7, 13);
            DateTime endTime = DateTime.Now;

            TimeSpan difference = endTime - startTime;
            var years = difference.Days / 365d;
            var restYears = years - Math.Truncate(years);
            var months = restYears * 12;
            var restMonths = months - Math.Truncate(months);
            var days = restMonths * 30;
            var restDays = days - Math.Truncate(days);
            var hours = restDays * 24;
            var restHours = hours - Math.Truncate(hours);
            var minutes = restHours * 60;

            Console.WriteLine($"{(int)years} years, {(int)months} months, {(int)days} days, {(int)hours} hours, {(int)minutes} minutes");
        }
    }
}