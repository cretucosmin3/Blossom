using System;
using SkiaSharp;

namespace Blossom.Hosting;

/// <summary>Set only inside Shell.Render (and later UiSession.Paint). Not a layout-size source.</summary>
internal static class GpuPass
{
    [ThreadStatic] internal static bool InGpuPass;
    [ThreadStatic] internal static RenderFrame Frame;
    [ThreadStatic] internal static PaintMode PaintMode;

    static bool s_loggedMissingSnapshotSurface;

    /// <summary>
    /// Surface to snapshot for backdrop/glass. Host-supplied <see cref="RenderFrame.Surface"/>,
    /// else <see cref="SKCanvas.Surface"/>. Null means skip the filter (logged once).
    /// </summary>
    internal static SKSurface? ResolveSnapshotSurface()
    {
        if (!InGpuPass)
            return null;

        var frame = Frame;
        var surface = frame.Surface ?? frame.Canvas?.Surface;
        if (surface == null && !s_loggedMissingSnapshotSurface)
        {
            s_loggedMissingSnapshotSurface = true;
            Blossom.Log.Warning("Backdrop snapshot skipped: GpuPass.Frame has no SKSurface (Canvas.Surface was also null).");
        }

        return surface;
    }
}
