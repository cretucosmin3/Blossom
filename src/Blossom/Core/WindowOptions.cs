namespace Blossom.Core;

/// <summary>
/// Initial OS-window policy for an <see cref="Application"/>. Applied when the host creates the window.
/// </summary>
public sealed class WindowOptions
{
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 800;
    public int MinWidth { get; set; }
    public int MinHeight { get; set; }
    public bool CenterOnLoad { get; set; } = true;
    public bool Resizable { get; set; } = true;
}
