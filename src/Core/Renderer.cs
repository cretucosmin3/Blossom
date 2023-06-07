using System;
using Silk.NET.Windowing;
using SkiaSharp;

namespace Blossom.Core;

internal static class Renderer
{
    // private static Nvg _renderPipeline;
    // private static int DefaultFont;

    // Renderring
    private static readonly object _lock = new();
    public static SKSurface Surface;
    public static GRBackendRenderTarget RenderTarget;
    public static GRGlInterface grGlInterface;
    public static GRContext grContext;
    private static SKCanvas _Canvas;

    internal static SKCanvas Canvas
    {
        get
        {
            lock (_lock)
            {
                return _Canvas;
            }
        }
    }

    private static void RenewCanvas(int width, int height)
    {
        RenderTarget?.Dispose();
        _Canvas?.Dispose();
        Surface?.Dispose();

        RenderTarget = new GRBackendRenderTarget(width, height, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
        Surface = SKSurface.Create(grContext, RenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);

        _Canvas = Surface.Canvas;
    }

    public static void SetCanvas(IWindow window)
    {
        grGlInterface = GRGlInterface.Create();
        grGlInterface.Validate();

        grContext = GRContext.CreateGl(grGlInterface);

        RenewCanvas(window.Size.X, window.Size.Y);

        window.FramebufferResize += newSize =>
        {
            RenewCanvas(newSize.X, newSize.Y);
            Browser.RenderRect = new(0, 0, newSize.X, newSize.Y);
            Browser.WasResized = true;

            window.DoRender();
        };
    }

    public static void ResetContext()
    {
        grContext.ResetContext();
    }
}