using System;
using SkiaSharp;

namespace Blossom.Hosting;

/// <summary>Clip policy only. Clear is <see cref="RenderFrame.ClearCanvas"/>.</summary>
public enum PaintMode
{
    /// <summary>Scissor to dirty union (desktop).</summary>
    DirtyRect,
    /// <summary>No scissor; paint the full logical viewport (HUD).</summary>
    FullFrame
}

/// <summary>Per-paint binding. Construct with the required canvas. Valid only during a GPU pass.</summary>
public readonly struct RenderFrame
{
    public RenderFrame(
        SKCanvas canvas,
        UiSize logicalSize,
        FramebufferSize framebufferSize,
        GRContext? gpuContext = null,
        SKSurface? surface = null,
        bool clearCanvas = true,
        SKColor? clearColor = null)
    {
        Canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        LogicalSize = logicalSize;
        FramebufferSize = framebufferSize;
        GpuContext = gpuContext;
        Surface = surface;
        ClearCanvas = clearCanvas;
        ClearColor = clearColor ?? SKColors.Transparent;
    }

    public SKCanvas Canvas { get; }
    public SKSurface? Surface { get; }
    public GRContext? GpuContext { get; }
    public UiSize LogicalSize { get; }
    public FramebufferSize FramebufferSize { get; }
    public bool ClearCanvas { get; }
    public SKColor ClearColor { get; }
}
