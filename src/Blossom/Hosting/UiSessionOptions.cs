using SkiaSharp;

namespace Blossom.Hosting;

public sealed class UiSessionOptions
{
    public PaintMode PaintMode { get; set; } = PaintMode.DirtyRect;
    public SKColor ClearColor { get; set; } = SKColors.White;
    public bool ClearCanvas { get; set; } = true;
    public bool InstallSynchronizationContext { get; set; } = true;
    /// <summary>Optional maximum UI frames per second for game hosting. Null or &lt;= 0 for uncapped.</summary>
    public int? MaxFps { get; set; }
}
