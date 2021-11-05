using System;
using System.IO;

public static class Log
{
	public enum Severity
	{
		Debug,
		Info,
		Warning,
		Error,
		Fatal
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
		ConsoleColor SeverityColor = severity switch
		{
			Severity.Debug => ConsoleColor.DarkGray,
			Severity.Info => ConsoleColor.White,
			Severity.Warning => ConsoleColor.Yellow,
			Severity.Error => ConsoleColor.Red,
			Severity.Fatal => ConsoleColor.DarkMagenta,
		};

		WritePrefix($"[{severity.ToString().ToUpper()}] ", SeverityColor);
		Console.WriteLine(LogMessage);
		LogToFile(LogMessage, severity);
	}

	public static void Debug(string LogMessage)
	{
		Write(LogMessage, Severity.Debug);
	}

	public static void Info(string LogMessage)
	{
		Write(LogMessage, Severity.Info);
	}

	public static void Warning(string LogMessage)
	{
		Write(LogMessage, Severity.Warning);
	}

	public static void Error(string LogMessage)
	{
		Write(LogMessage, Severity.Error);
	}

	public static void Fatal(string LogMessage)
	{
		Write(LogMessage, Severity.Fatal);
	}
}