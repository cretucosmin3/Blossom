namespace Blossom.Desktop;

/// <summary>
/// Initial OS-window policy for a <see cref="DesktopApplication"/>. Applied when <see cref="Shell"/> creates the window.
/// </summary>
public sealed class WindowOptions
{
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 800;
    public int MinWidth { get; set; }
    public int MinHeight { get; set; }
    public bool CenterOnLoad { get; set; } = true;
    public bool Resizable { get; set; } = true;

    /// <summary>
    /// Reopen maximized when the window was maximized at its last close. Default true. The flag is kept
    /// per <see cref="StateId"/> in the user's local app data folder.
    /// </summary>
    public bool RememberMaximized { get; set; } = true;

    /// <summary>Names the remembered window state. Null uses the entry assembly name.</summary>
    public string? StateId { get; set; }
}
