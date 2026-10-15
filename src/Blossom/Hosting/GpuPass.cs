using System;
using SkiaSharp;

namespace Blossom.Hosting;

/// <summary>Set only inside UiSession.Paint (and desktop Shell.Render). Not a layout-size source.</summary>
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

    /// <summary>
    /// Copies the part of the frame surface under <paramref name="deviceRect"/> (framebuffer pixels),
    /// grown by <paramref name="outset"/> for effects that sample around the element. The frame
    /// surface usually wraps a framebuffer Skia cannot share, so every snapshot is a GPU copy: a
    /// whole-window copy per effect per frame costs a full-window texture each (33 MB at 4K).
    /// <paramref name="origin"/> is where the copy's top-left sits on the surface.
    /// </summary>
    internal static SKImage? SnapshotRegion(SKRect deviceRect, float outset, out SKPointI origin)
    {
        origin = default;
        var surface = ResolveSnapshotSurface();
        if (surface == null)
            return null;

        // Skia clips the request to the surface, so only the top-left needs clamping to know the origin.
        int left = Math.Max(0, (int)MathF.Floor(deviceRect.Left - outset));
        int top = Math.Max(0, (int)MathF.Floor(deviceRect.Top - outset));
        int right = (int)MathF.Ceiling(deviceRect.Right + outset);
        int bottom = (int)MathF.Ceiling(deviceRect.Bottom + outset);
        if (right <= left || bottom <= top)
            return null;

        origin = new SKPointI(left, top);
        return surface.Snapshot(new SKRectI(left, top, right, bottom));
    }

    /// <summary>
    /// <see cref="SnapshotRegion"/> as a shader in surface coordinates, for SKSL effects that sample the
    /// backdrop at screen positions.
    /// </summary>
    internal static SKShader? SnapshotRegionShader(SKRect deviceRect, float outset, out SKImage? image)
    {
        image = SnapshotRegion(deviceRect, outset, out var origin);
        return image?.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, SKMatrix.CreateTranslation(origin.X, origin.Y));
    }
}
