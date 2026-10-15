using System;
using Blossom.Hosting;
using SkiaSharp;

namespace Blossom.Core;

/// <summary>
/// Device-pixel snapping. Layout stays in fractional logical units; paint rounds edges to whole
/// framebuffer pixels so 1px lines and box edges do not straddle two pixel rows at 125% / 150%.
/// Scale is read from <see cref="GpuPass.Frame"/> during paint.
/// </summary>
internal static class PixelGrid
{
    /// <summary>Framebuffer pixels per logical unit (the display scale the view renders with).</summary>
    public static float ScaleX
    {
        get
        {
            var frame = GpuPass.Frame;
            return frame.LogicalSize.Width > 0 && frame.FramebufferSize.Width > 0
                ? frame.FramebufferSize.Width / frame.LogicalSize.Width : 1f;
        }
    }

    public static float ScaleY
    {
        get
        {
            var frame = GpuPass.Frame;
            return frame.LogicalSize.Height > 0 && frame.FramebufferSize.Height > 0
                ? frame.FramebufferSize.Height / frame.LogicalSize.Height : 1f;
        }
    }

    /// <summary>Rounds a logical coordinate to the nearest device pixel boundary.</summary>
    public static float Snap(float value, float scale) => MathF.Floor(value * scale + 0.5f) / scale;

    /// <summary>Rounds a logical length to whole device pixels, keeping a visible length at least one pixel.</summary>
    public static float SnapLength(float value, float scale)
    {
        if (value <= 0f) return value;
        return Math.Max(1f, MathF.Floor(value * scale + 0.5f)) / scale;
    }

    /// <summary>
    /// Stroke width in whole device pixels, rounded down like browsers do (1px at 150% stays one
    /// device pixel, not two), never thinner than one pixel.
    /// </summary>
    public static float SnapStroke(float value, float scale)
    {
        if (value <= 0f) return value;
        return Math.Max(1f, MathF.Floor(value * scale + 0.01f)) / scale;
    }

    /// <summary>
    /// True when <paramref name="m"/> maps (x, y, 0) by translation only. Scaled, rotated or
    /// perspective matrices are left alone: their edges cannot land on the grid anyway.
    /// </summary>
    public static bool IsTranslateOnly(in SKMatrix44 m)
    {
        const float e = 1e-5f;
        return Math.Abs(m[0, 0] - 1f) < e && Math.Abs(m[1, 1] - 1f) < e
            && Math.Abs(m[0, 1]) < e && Math.Abs(m[1, 0]) < e
            && Math.Abs(m[0, 3]) < e && Math.Abs(m[1, 3]) < e
            && Math.Abs(m[3, 3] - 1f) < e;
    }

    /// <summary>Snaps the translation of a translate-only matrix to device pixels; other matrices pass through.</summary>
    public static SKMatrix44 SnapTranslation(SKMatrix44 m)
    {
        if (!IsTranslateOnly(m)) return m;
        m[3, 0] = Snap(m[3, 0], ScaleX);
        m[3, 1] = Snap(m[3, 1], ScaleY);
        return m;
    }

    /// <summary>Expands a logical rect outward so every edge sits on a device pixel boundary.</summary>
    public static SKRect SnapOutward(SKRect r)
    {
        float sx = ScaleX, sy = ScaleY;
        return new SKRect(
            MathF.Floor(r.Left * sx) / sx,
            MathF.Floor(r.Top * sy) / sy,
            MathF.Ceiling(r.Right * sx) / sx,
            MathF.Ceiling(r.Bottom * sy) / sy);
    }
}
