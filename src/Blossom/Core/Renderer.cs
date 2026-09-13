using System;
using Silk.NET.OpenGL;
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

    public static int FramebufferWidth { get; private set; }
    public static int FramebufferHeight { get; private set; }

    private static void RenewCanvas(int width, int height)
    {
        FramebufferWidth = width;
        FramebufferHeight = height;

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

        // The GL swapchain is cleared every frame. Always copy the full persistent
        // offscreen buffer — a dirty-rect blit leaves the rest of the window blank.
        Surface.Canvas.DrawSurface(OffscreenSurface, 0, 0);
        Surface.Canvas.Flush();
    }

    public static void SetCanvas(IWindow window)
    {
        try
        {
            grGlInterface = GRGlInterface.Create();
            grGlInterface.Validate();
        }
        catch (Exception ex)
        {
            Log.Warning($"Default GRGlInterface.Create() failed ({ex.Message}), trying window GLContext...");
            grGlInterface = GRGlInterface.Create(name =>
            {
                if (window.GLContext != null)
                {
                    try
                    {
                        var ptr = window.GLContext.GetProcAddress(name);
                        if (ptr != IntPtr.Zero) return ptr;
                    }
                    catch { }
                }
                return IntPtr.Zero;
            });
            grGlInterface.Validate();
        }

        grContext = GRContext.CreateGl(grGlInterface);

        RenewCanvas(window.FramebufferSize.X, window.FramebufferSize.Y);
        Browser.RenderRect = new(0, 0, window.Size.X, window.Size.Y);

        void OnWindowResized()
        {
            int fbW = Math.Max(1, window.FramebufferSize.X);
            int fbH = Math.Max(1, window.FramebufferSize.Y);
            int winW = Math.Max(1, window.Size.X);
            int winH = Math.Max(1, window.Size.Y);

            // Rebuild GPU surfaces for new size (clears offscreen buffer)
            if (fbW != FramebufferWidth || fbH != FramebufferHeight)
            {
                RenewCanvas(fbW, fbH);
            }

            Browser.RenderRect = new(0, 0, winW, winH);
            Browser.WasResized = true;

            if (Browser.BrowserApp?.ActiveView != null)
            {
                Browser.BrowserApp.ActiveView.FullRenderRequired = true;
                Browser.BrowserApp.ActiveView.RenderRequired = true;
                Browser.BrowserApp.ActiveView.ForceLayoutEvaluation();
            }

            // Event-driven loop may be waiting; wake it so the frame paints immediately
            try
            {
                Silk.NET.GLFW.GlfwProvider.GLFW.Value.PostEmptyEvent();
            }
            catch { /* GLFW may not be ready during teardown */ }
        }

        window.FramebufferResize += _ => OnWindowResized();
        window.Resize += _ => OnWindowResized();
    }

    public static void ResetContext()
    {
        grContext.ResetContext();
    }
}