using System;
using SkiaSharp;

namespace Blossom.Core;

/// <summary>
/// Shared Skia GPU context owned by the window.
/// Apps create offscreen surfaces here (including F16/F32 ping-pong), then blit an
/// <see cref="SKImage"/> into the element tree with <see cref="DrawSkImageCommand"/>.
/// All members must be used on the UI/render thread; marshal with <see cref="Browser.Post"/>.
/// </summary>
public static class Gpu
{
    public static bool IsReady => Renderer.grContext != null;

    /// <summary>The window's Skia GPU context, or null before the window has loaded.</summary>
    public static GRContext? Context => Renderer.grContext;

    public static void ResetContext()
    {
        Renderer.grContext?.ResetContext();
    }

    public static void Flush()
    {
        Renderer.grContext?.Flush();
    }

    /// <summary>
    /// GPU-backed surface. Color type may be <see cref="SKColorType.Rgba8888"/>,
    /// <see cref="SKColorType.RgbaF16"/>, or <see cref="SKColorType.RgbaF32"/> (support depends on the driver).
    /// Returns null if the GPU is not ready or Skia cannot allocate the surface.
    /// Caller disposes the surface.
    /// </summary>
    public static SKSurface? CreateSurface(
        int width,
        int height,
        SKColorType colorType = SKColorType.Rgba8888,
        SKAlphaType alphaType = SKAlphaType.Premul)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        return CreateSurface(new SKImageInfo(width, height, colorType, alphaType));
    }

    public static SKSurface? CreateSurface(SKImageInfo info)
    {
        if (Renderer.grContext == null)
            return null;
        if (info.Width < 1 || info.Height < 1)
            return null;

        return SKSurface.Create(Renderer.grContext, false, info);
    }

    public static SKImage? Snapshot(SKSurface surface)
    {
        if (surface == null)
            return null;
        surface.Canvas.Flush();
        Flush();
        return surface.Snapshot();
    }

    public static bool ReadPixels(SKSurface surface, SKImageInfo dstInfo, IntPtr dstPixels, int dstRowBytes, int srcX = 0, int srcY = 0)
    {
        if (surface == null || dstPixels == IntPtr.Zero)
            return false;
        surface.Canvas.Flush();
        Flush();
        return surface.ReadPixels(dstInfo, dstPixels, dstRowBytes, srcX, srcY);
    }
}
