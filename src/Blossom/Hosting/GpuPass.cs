using System;

namespace Blossom.Hosting;

/// <summary>Set only inside Shell.Render (and later UiSession.Paint). Not a layout-size source.</summary>
internal static class GpuPass
{
    [ThreadStatic] internal static bool InGpuPass;
    [ThreadStatic] internal static RenderFrame Frame;
    [ThreadStatic] internal static PaintMode PaintMode;
}
