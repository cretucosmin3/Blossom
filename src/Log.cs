using System;
using System.IO;
using static System.Console;

namespace Blossom
{
    public static class Log
    {
        private enum Severity
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Fatal = 4
        }

        private static void LogToFile(string message, Severity severity)
        {
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");

            string filePath = $"{Directory.GetCurrentDirectory()}/logs/{DateTime.Now:yyyy-MM-dd}.log";

            using StreamWriter writer = new StreamWriter(filePath, true);
            writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{severity}] {message}");
        }

        private static void WritePrefix(string text, ConsoleColor color)
        {
            ForegroundColor = color;
            Write(text);
            ResetColor();
        }

        private static void CWrite(string LogMessage, Severity severity)
        {
            ConsoleColor SeverityColor = ((int)severity) switch
            {
                0 => ConsoleColor.DarkGray,
                1 => ConsoleColor.White,
                2 => ConsoleColor.Yellow,
                3 => ConsoleColor.Red,
                4 => ConsoleColor.DarkMagenta,
                _ => default,
            };

            WritePrefix($"[{severity.ToString().ToUpper()}] ", SeverityColor);
            WriteLine(LogMessage);
        }

        public static void Debug(string LogMessage) => CWrite(LogMessage, Severity.Debug);
        public static void Info(string LogMessage) => CWrite(LogMessage, Severity.Info);
        public static void Warning(string LogMessage) => CWrite(LogMessage, Severity.Warning);
        public static void Error(string LogMessage) => CWrite(LogMessage, Severity.Error);
        public static void Fatal(string LogMessage) => CWrite(LogMessage, Severity.Fatal);
    }
}