using System;
using Blossom.Hosting;
using SkiaSharp;

namespace Blossom.Core;

/// <summary>
/// Shared Skia GPU (or CPU) surfaces for the current paint pass.
/// Apps create offscreen surfaces here (including F16/F32 ping-pong), then blit an
/// <see cref="SKImage"/> into the element tree with <see cref="DrawSkImageCommand"/>.
/// Legal only while <c>GpuPass.InGpuPass</c> is true (<c>Shell.Render</c> / later <c>UiSession.Paint</c>).
/// </summary>
public static class Gpu
{
    const string GpuPassRequired = "Gpu is only legal during a GPU pass (Shell.Render / UiSession.Paint).";

    public static bool IsReady => GpuPass.InGpuPass && GpuPass.Frame.GpuContext != null;

    /// <summary>The paint-pass Skia GPU context, or null outside a pass / on a software host.</summary>
    public static GRContext? Context => GpuPass.InGpuPass ? GpuPass.Frame.GpuContext : null;

    public static void ResetContext()
    {
        EnsureGpuPass();
        GpuPass.Frame.GpuContext?.ResetContext();
    }

    public static void Flush()
    {
        EnsureGpuPass();
        GpuPass.Frame.GpuContext?.Flush();
    }

    /// <summary>
    /// GPU-backed surface when the pass has a <see cref="GRContext"/>; otherwise a CPU surface.
    /// Color type may be <see cref="SKColorType.Rgba8888"/>,
    /// <see cref="SKColorType.RgbaF16"/>, or <see cref="SKColorType.RgbaF32"/> (support depends on the driver).
    /// Throws if called outside a GPU pass. Caller disposes the surface.
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
        EnsureGpuPass();
        if (info.Width < 1 || info.Height < 1)
            return null;

        var ctx = GpuPass.Frame.GpuContext;
        if (ctx != null)
            return SKSurface.Create(ctx, false, info);

        return SKSurface.Create(info);
    }

    public static SKImage? Snapshot(SKSurface surface)
    {
        EnsureGpuPass();
        if (surface == null)
            return null;
        surface.Canvas.Flush();
        Flush();
        return surface.Snapshot();
    }

    public static bool ReadPixels(SKSurface surface, SKImageInfo dstInfo, IntPtr dstPixels, int dstRowBytes, int srcX = 0, int srcY = 0)
    {
        EnsureGpuPass();
        if (surface == null || dstPixels == IntPtr.Zero)
            return false;
        surface.Canvas.Flush();
        Flush();
        return surface.ReadPixels(dstInfo, dstPixels, dstRowBytes, srcX, srcY);
    }

    static void EnsureGpuPass()
    {
        if (!GpuPass.InGpuPass)
            throw new InvalidOperationException(GpuPassRequired);
    }
}
