using Blossom.Core;

namespace Blossom.Desktop;

/// <summary>
/// <see cref="Application"/> run by the desktop <see cref="Shell"/>. Adds the OS-window policy that a
/// headless <see cref="Blossom.Hosting.UiSession"/> has no use for.
/// </summary>
public abstract class DesktopApplication : Application
{
    /// <summary>Initial window size, min size, resizable border, and center-on-load. Read by <see cref="Shell"/> at create time.</summary>
    public WindowOptions Window { get; } = new();

    /// <summary>
    /// When true, F12 toggles Blossom’s stats overlay and is consumed by the host
    /// before any element, view, or application key bind. Default false.
    /// </summary>
    public bool EnableStatsOverlay { get; set; }

    /// <summary>Window title. Empty falls back to the active view's name.</summary>
    public string Title { get; set; } = "";
}
