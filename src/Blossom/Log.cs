using System;
using System.IO;
using System.Text;

namespace Blossom
{
    public static class Log
    {
        public enum Severity
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Fatal = 4
        }

        private static readonly object _lock = new();
        private static string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blossom.log");
        private static bool _initialized = false;

        public static string LogFilePath
        {
            get => _logFilePath;
            set
            {
                lock (_lock)
                {
                    _logFilePath = value;
                }
            }
        }

        public static void Initialize(string? customPath = null)
        {
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(customPath))
                {
                    _logFilePath = customPath;
                }

                if (_initialized) return;
                _initialized = true;

                try
                {
                    var dir = Path.GetDirectoryName(_logFilePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    using var writer = new StreamWriter(_logFilePath, append: true, Encoding.UTF8);
                    writer.WriteLine();
                    writer.WriteLine($"==================== Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ====================");
                    writer.Flush();
                }
                catch
                {
                    // Silently continue if disk write fails
                }

                try
                {
                    Console.SetOut(new LogTextWriter(Severity.Info));
                    Console.SetError(new LogTextWriter(Severity.Error));
                }
                catch
                {
                    // Silently continue if Console redirection is unavailable
                }
            }
        }

        private static void WriteLog(string message, Severity severity)
        {
            if (!_initialized)
            {
                Initialize();
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] [{severity.ToString().ToUpperInvariant()}] {message}";

            lock (_lock)
            {
                try
                {
                    using var writer = new StreamWriter(_logFilePath, append: true, Encoding.UTF8);
                    writer.WriteLine(formattedMessage);
                    writer.Flush();
                }
                catch
                {
                    // Fail silently to avoid application crashes on logging errors
                }
            }
        }

        public static void Debug(string LogMessage) => WriteLog(LogMessage, Severity.Debug);
        public static void Info(string LogMessage) => WriteLog(LogMessage, Severity.Info);
        public static void Warning(string LogMessage) => WriteLog(LogMessage, Severity.Warning);
        public static void Error(string LogMessage) => WriteLog(LogMessage, Severity.Error);
        public static void Fatal(string LogMessage) => WriteLog(LogMessage, Severity.Fatal);

        public static void Error(Exception ex, string? message = null)
        {
            string msg = string.IsNullOrEmpty(message) ? ex.ToString() : $"{message}\n{ex}";
            WriteLog(msg, Severity.Error);
        }

        public static void Fatal(Exception ex, string? message = null)
        {
            string msg = string.IsNullOrEmpty(message) ? ex.ToString() : $"{message}\n{ex}";
            WriteLog(msg, Severity.Fatal);
        }

        private class LogTextWriter : TextWriter
        {
            private readonly Severity _severity;
            private readonly StringBuilder _sb = new();
            private readonly object _writeLock = new();

            public override Encoding Encoding => Encoding.UTF8;

            public LogTextWriter(Severity severity)
            {
                _severity = severity;
            }

            public override void Write(char value)
            {
                lock (_writeLock)
                {
                    if (value == '\n')
                    {
                        var line = _sb.ToString().TrimEnd('\r');
                        _sb.Clear();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            WriteLog(line, _severity);
                        }
                    }
                    else
                    {
                        _sb.Append(value);
                    }
                }
            }

            public override void Write(string? value)
            {
                if (value == null) return;
                lock (_writeLock)
                {
                    var lines = value.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (i == lines.Length - 1)
                        {
                            _sb.Append(lines[i]);
                        }
                        else
                        {
                            _sb.Append(lines[i]);
                            var line = _sb.ToString().TrimEnd('\r');
                            _sb.Clear();
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                WriteLog(line, _severity);
                            }
                        }
                    }
                }
            }

            public override void WriteLine(string? value)
            {
                lock (_writeLock)
                {
                    if (_sb.Length > 0)
                    {
                        _sb.Append(value);
                        var line = _sb.ToString().TrimEnd('\r');
                        _sb.Clear();
                        WriteLog(line, _severity);
                    }
                    else if (value != null)
                    {
                        WriteLog(value, _severity);
                    }
                }
            }

            public override void Flush()
            {
                lock (_writeLock)
                {
                    if (_sb.Length > 0)
                    {
                        var line = _sb.ToString().TrimEnd('\r');
                        _sb.Clear();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            WriteLog(line, _severity);
                        }
                    }
                }
            }
        }
    }
}