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
    public static SKSurface OffscreenSurface;
    public static GRGlInterface grGlInterface;
    public static GRContext grContext;
    private static SKCanvas _Canvas;

    private static GL _gl;
    private static uint _offscreenFbo;
    private static uint _offscreenColorTex;
    private static uint _offscreenDepthStencilRbo;
    private static GRBackendRenderTarget _offscreenRenderTarget;

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

    private static void CleanupOffscreen()
    {
        _Canvas?.Dispose();
        _Canvas = null!;

        OffscreenSurface?.Dispose();
        OffscreenSurface = null!;

        _offscreenRenderTarget?.Dispose();
        _offscreenRenderTarget = null!;

        if (_gl != null)
        {
            if (_offscreenFbo != 0)
            {
                _gl.DeleteFramebuffer(_offscreenFbo);
                _offscreenFbo = 0;
            }
            if (_offscreenColorTex != 0)
            {
                _gl.DeleteTexture(_offscreenColorTex);
                _offscreenColorTex = 0;
            }
            if (_offscreenDepthStencilRbo != 0)
            {
                _gl.DeleteRenderbuffer(_offscreenDepthStencilRbo);
                _offscreenDepthStencilRbo = 0;
            }
        }
    }

    private static void RenewCanvas(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        FramebufferWidth = width;
        FramebufferHeight = height;

        CleanupOffscreen();

        if (_gl == null) return;

        // 1. Create Offscreen OpenGL Framebuffer with Color Texture + Depth/Stencil attachments
        unsafe
        {
            _offscreenFbo = _gl.GenFramebuffer();
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _offscreenFbo);

            _offscreenColorTex = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, _offscreenColorTex);
            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba8,
                (uint)width,
                (uint)height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                (void*)null);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            _gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                _offscreenColorTex,
                0);

            _offscreenDepthStencilRbo = _gl.GenRenderbuffer();
            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _offscreenDepthStencilRbo);
            _gl.RenderbufferStorage(
                RenderbufferTarget.Renderbuffer,
                InternalFormat.Depth24Stencil8,
                (uint)width,
                (uint)height);
            _gl.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthStencilAttachment,
                RenderbufferTarget.Renderbuffer,
                _offscreenDepthStencilRbo);

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
            {
                Log.Error($"Offscreen FBO incomplete: {status}");
            }

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        // 2. Wrap the hardware offscreen FBO in a Skia RenderTarget
        var fbInfo = new GRGlFramebufferInfo(_offscreenFbo, 0x8058); // GL_RGBA8
        _offscreenRenderTarget = new GRBackendRenderTarget(width, height, 0, 8, fbInfo);
        OffscreenSurface = SKSurface.Create(grContext, _offscreenRenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);

        // 3. Set the default canvas to the Offscreen one
        _Canvas = OffscreenSurface.Canvas;

        // Initialize with white background to avoid black screen on startup
        _Canvas.Clear(SKColors.White);
    }

    public static void FlushToScreen()
    {
        if (OffscreenSurface == null || _gl == null || _offscreenFbo == 0) return;

        // Ensure Skia flushes all pending draw operations to the offscreen FBO
        OffscreenSurface.Canvas.Flush();
        grContext?.Flush();

        // Bind source (offscreen) and destination (window framebuffer 0)
        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _offscreenFbo);
        _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

        // Scissor test must be disabled during full-screen presentation blit,
        // otherwise any dirty-rect scissor from Skia's previous draw pass would clip the blit
        _gl.Disable(EnableCap.ScissorTest);

        // Fast hardware ROP/DMA blit directly on the GPU
        _gl.BlitFramebuffer(
            0, 0, FramebufferWidth, FramebufferHeight,
            0, 0, FramebufferWidth, FramebufferHeight,
            ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Nearest);

        // Restore default FBO binding and sync Skia's cached GL state
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        grContext?.ResetContext();
    }

    public static void SetCanvas(IWindow window)
    {
        try
        {
            _gl = GL.GetApi(window.GLContext);
        }
        catch (Exception ex)
        {
            Log.Warning($"GL.GetApi(window.GLContext) failed ({ex.Message}), trying window...");
            _gl = GL.GetApi(window);
        }

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