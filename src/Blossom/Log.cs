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
        private static TextWriter _stdio = Console.Out;
        private static TextWriter _stderr = Console.Error;
        private static FileStream? _stream;
        private static StreamWriter? _writer;

        /// <summary>Last breadcrumb. Native crashes never throw; this is what was in flight.</summary>
        public static string LastMark { get; private set; } = "(none)";

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

                    _stream = new FileStream(
                        _logFilePath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.ReadWrite,
                        bufferSize: 4096,
                        FileOptions.WriteThrough);
                    _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                    _writer.WriteLine();
                    _writer.WriteLine($"==================== Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ====================");
                    _writer.Flush();
                    _stream.Flush(flushToDisk: true);
                }
                catch
                {
                    // Silently continue if disk write fails
                }

                try
                {
                    _stdio = Console.Out;
                    _stderr = Console.Error;
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
                LastMark = message ?? "";
                try
                {
                    if (_writer == null)
                    {
                        using var writer = new StreamWriter(_logFilePath, append: true, Encoding.UTF8);
                        writer.WriteLine(formattedMessage);
                        writer.Flush();
                    }
                    else
                    {
                        _writer.WriteLine(formattedMessage);
                        _writer.Flush();
                        _stream?.Flush(flushToDisk: true);
                    }
                }
                catch
                {
                    // Fail silently to avoid application crashes on logging errors
                }

                // Console.SetError is redirected into this logger, so write to the
                // original stderr or "Could not open" never appears in the terminal.
                if (severity >= Severity.Warning)
                {
                    try
                    {
                        _stderr.WriteLine(formattedMessage);
                        _stderr.Flush();
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static void Debug(string LogMessage) => WriteLog(LogMessage, Severity.Debug);
        public static void Info(string LogMessage) => WriteLog(LogMessage, Severity.Info);
        public static void Warning(string LogMessage) => WriteLog(LogMessage, Severity.Warning);
        public static void Error(string LogMessage) => WriteLog(LogMessage, Severity.Error);
        public static void Fatal(string LogMessage) => WriteLog(LogMessage, Severity.Fatal);

        /// <summary>Signal-handler path: best-effort disk write of the last breadcrumb.</summary>
        public static void WriteNativeCrash(string signal)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [FATAL] Native {signal}. Last: {LastMark}";
            try
            {
                lock (_lock)
                {
                    if (_writer != null)
                    {
                        _writer.WriteLine(line);
                        _writer.Flush();
                        _stream?.Flush(flushToDisk: true);
                    }
                    else
                    {
                        File.AppendAllText(_logFilePath, line + Environment.NewLine);
                    }
                }
            }
            catch { }

            try
            {
                _stderr.WriteLine(line);
                _stderr.Flush();
            }
            catch { }
        }

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