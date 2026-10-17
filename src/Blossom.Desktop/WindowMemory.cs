using System;
using System.IO;
using System.Reflection;

namespace Blossom.Desktop;

/// <summary>
/// Remembers per app whether the window was maximized when it last closed, in
/// <c>{LocalApplicationData}/{StateId}/window-state</c> (<c>~/.local/share</c> on Linux,
/// <c>%LOCALAPPDATA%</c> on Windows, <c>~/Library/Application Support</c> on macOS).
/// Failures are logged and ignored: a missing or unwritable file only means a default start.
/// </summary>
internal static class WindowMemory
{
    private const string MaximizedKey = "maximized=";

    internal static bool LoadMaximized(WindowOptions options)
    {
        var path = GetPath(options);
        if (path == null || !File.Exists(path))
            return false;

        try
        {
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.StartsWith(MaximizedKey, StringComparison.Ordinal))
                    return bool.TryParse(line.AsSpan(MaximizedKey.Length).Trim(), out bool maximized) && maximized;
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not read window state '{path}': {ex.Message}");
        }
        return false;
    }

    internal static void SaveMaximized(WindowOptions options, bool maximized)
    {
        var path = GetPath(options);
        if (path == null)
            return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, MaximizedKey + (maximized ? "true" : "false") + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not save window state '{path}': {ex.Message}");
        }
    }

    private static string? GetPath(WindowOptions options)
    {
        if (!options.RememberMaximized)
            return null;

        string? id = string.IsNullOrWhiteSpace(options.StateId)
            ? Assembly.GetEntryAssembly()?.GetName().Name
            : options.StateId;
        if (string.IsNullOrWhiteSpace(id))
            return null;

        // The id becomes a folder name.
        foreach (char c in Path.GetInvalidFileNameChars())
            id = id.Replace(c, '_');

        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrEmpty(root) ? null : Path.Combine(root, id, "window-state");
    }
}
