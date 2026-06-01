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
    public static SKSurface OffscreenSurface;
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
        OffscreenSurface?.Dispose();

        // 1. Create the Screen Surface (Wrapper around OpenGL Framebuffer)
        RenderTarget = new GRBackendRenderTarget(width, height, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
        Surface = SKSurface.Create(grContext, RenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);

        // 2. Create the Offscreen Surface (Persistent Back Buffer) - Texture backed
        var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        OffscreenSurface = SKSurface.Create(grContext, false, imageInfo);

        // 3. Set the default canvas to the Offscreen one
        _Canvas = OffscreenSurface.Canvas;
        
        // Initialize with white background to avoid black screen on startup
        _Canvas.Clear(SKColors.White);
    }

    public static void FlushToScreen()
    {
        if (Surface == null || OffscreenSurface == null) return;

        // Blit the Offscreen Buffer to the Screen Buffer
        Surface.Canvas.DrawSurface(OffscreenSurface, 0, 0);
        Surface.Canvas.Flush();
    }

    public static void SetCanvas(IWindow window)
    {
        grGlInterface = GRGlInterface.Create();
        grGlInterface.Validate();

        grContext = GRContext.CreateGl(grGlInterface);

        RenewCanvas(window.Size.X, window.Size.Y);
        // Ensure RenderRect matches actual window size immediately
        Browser.RenderRect = new(0, 0, window.Size.X, window.Size.Y); 

        window.FramebufferResize += newSize =>
        {
            RenewCanvas(newSize.X, newSize.Y);
            Browser.RenderRect = new(0, 0, newSize.X, newSize.Y);
            Browser.WasResized = true;

            if (Browser.BrowserApp?.ActiveView != null)
            {
                Browser.BrowserApp.ActiveView.RenderRequired = true;
            }
        };
    }

    public static void ResetContext()
    {
        grContext.ResetContext();
    }
}