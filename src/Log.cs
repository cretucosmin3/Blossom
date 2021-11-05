using System;
using System.IO;

namespace Kara.Utils
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
			if (Directory.Exists("logs") == false)
				Directory.CreateDirectory("logs");

			string filePath = $"{Directory.GetCurrentDirectory()}/logs/{DateTime.Now.ToString("yyyy-MM-dd")}.log";

			using (StreamWriter writer = new StreamWriter(filePath, true))
			{
				writer.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] [{severity.ToString()}] {message}");
				writer.Close();
			}
		}

		private static void WritePrefix(string text, ConsoleColor color)
		{
			Console.ForegroundColor = color;
			Console.Write(text);
			Console.ResetColor();
		}

		private static void Write(string LogMessage, Severity severity)
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
			Console.WriteLine(LogMessage);
			LogToFile(LogMessage, severity);
		}

		public static void Debug(string LogMessage)		=> Write(LogMessage, Severity.Debug);
		public static void Info(string LogMessage)		=> Write(LogMessage, Severity.Info);
		public static void Warning(string LogMessage)	=> Write(LogMessage, Severity.Warning);
		public static void Error(string LogMessage)		=> Write(LogMessage, Severity.Error);
		public static void Fatal(string LogMessage)		=> Write(LogMessage, Severity.Fatal);
	}
}