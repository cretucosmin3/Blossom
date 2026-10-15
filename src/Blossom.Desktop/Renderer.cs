using System;
using System.Diagnostics;
using Blossom.Core;
using Blossom.Hosting;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace Blossom.Desktop;

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
    private static uint _offscreenStencilRbo;
    private static GRBackendRenderTarget _offscreenRenderTarget;
    private static bool _stencilOnly = true;

    // Skia's default budget is 256 MB of cached GPU textures, which a simple app fills with scratch
    // textures it never needs again. Frames that need more still render; Skia just keeps less.
    private const long GpuCacheLimitBytes = 96L * 1024 * 1024;
    private static readonly TimeSpan GpuTrimAfter = TimeSpan.FromSeconds(5);
    private static IDisposable? s_gpuTrim;
    private static long s_lastFrameTicks;

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
            if (_offscreenStencilRbo != 0)
            {
                _gl.DeleteRenderbuffer(_offscreenStencilRbo);
                _offscreenStencilRbo = 0;
            }
        }
    }

    private static void RenewCanvas(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        FramebufferWidth = width;
        FramebufferHeight = height;

        OffscreenSurface?.Dispose();
        OffscreenSurface = null!;
        _offscreenRenderTarget?.Dispose();
        _offscreenRenderTarget = null!;

        if (_gl == null) return;

        // 1. Create or resize Offscreen OpenGL Framebuffer with Color Texture + Depth/Stencil attachments
        unsafe
        {
            if (_offscreenFbo == 0)
            {
                _offscreenFbo = _gl.GenFramebuffer();
                _offscreenColorTex = _gl.GenTexture();
                _offscreenStencilRbo = _gl.GenRenderbuffer();
            }

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _offscreenFbo);

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

            // Skia never uses depth, but it does need stencil (complex paths and clips), and it cannot
            // attach one itself to an FBO it did not create. Stencil-only where the driver accepts it,
            // packed depth+stencil otherwise. The choice sticks, so resizes reuse a known-good format.
            GLEnum status = GLEnum.FramebufferUndefined;
            if (_stencilOnly)
            {
                AttachStencil(InternalFormat.StencilIndex8, FramebufferAttachment.StencilAttachment, width, height);
                status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                if (status != GLEnum.FramebufferComplete)
                {
                    _stencilOnly = false;
                    _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment,
                        RenderbufferTarget.Renderbuffer, 0);
                }
            }
            if (!_stencilOnly)
            {
                AttachStencil(InternalFormat.Depth24Stencil8, FramebufferAttachment.DepthStencilAttachment, width, height);
                status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            }
            if (status != GLEnum.FramebufferComplete)
            {
                Log.Error($"Offscreen FBO incomplete: {status}");
            }

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        // 2. Wrap the hardware offscreen FBO in a Skia RenderTarget
        var fbInfo = new GRGlFramebufferInfo(_offscreenFbo, 0x8058); // GL_RGBA8
        _offscreenRenderTarget = new GRBackendRenderTarget(width, height, 0, 8, fbInfo);
        // Pixel geometry must be known or Skia silently downgrades SubpixelAntialias text to grayscale.
        // macOS has no LCD text since Mojave, so keep it Unknown there.
        var pixelGeometry = OperatingSystem.IsMacOS() ? SKPixelGeometry.Unknown : SKPixelGeometry.RgbHorizontal;
        using var surfaceProps = new SKSurfaceProperties(pixelGeometry);
        OffscreenSurface = SKSurface.Create(grContext, _offscreenRenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888, surfaceProps);

        // 3. Set the default canvas to the Offscreen one
        _Canvas = OffscreenSurface.Canvas;

        // Initialize with white background to avoid black screen on startup
        _Canvas.Clear(SKColors.White);
    }

    private static void AttachStencil(InternalFormat format, FramebufferAttachment attachment, int width, int height)
    {
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _offscreenStencilRbo);
        _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, format, (uint)width, (uint)height);
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachment, RenderbufferTarget.Renderbuffer, _offscreenStencilRbo);
    }

    /// <summary>
    /// Arms a trim after a frame. Skia keeps every scratch texture it allocates until its budget is
    /// full, so textures from a page the user left would sit in VRAM for good. This frees what has
    /// gone unused for <see cref="GpuTrimAfter"/>: periodically while frames keep coming, and once
    /// after the app goes idle.
    /// </summary>
    private static void ScheduleGpuTrim()
    {
        s_lastFrameTicks = Stopwatch.GetTimestamp();
        s_gpuTrim ??= UiDispatcher.PostDelayed(GpuTrimAfter, TrimGpuCache);
    }

    private static void TrimGpuCache()
    {
        s_gpuTrim = null;
        grContext?.PurgeUnusedResources((long)GpuTrimAfter.TotalMilliseconds);

        // Not idle long enough for the last frame's textures to count as unused: check again then.
        var idle = Stopwatch.GetElapsedTime(s_lastFrameTicks);
        if (idle < GpuTrimAfter)
            s_gpuTrim = UiDispatcher.PostDelayed(GpuTrimAfter - idle + TimeSpan.FromMilliseconds(100), TrimGpuCache);
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

        ScheduleGpuTrim();
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
        grContext.SetResourceCacheLimit(GpuCacheLimitBytes);

        RenewCanvas(window.FramebufferSize.X, window.FramebufferSize.Y);
        Shell.HandleClientSize(window.Size.X, window.Size.Y);

        void OnWindowResized()
        {
            int fbW = Math.Max(1, window.FramebufferSize.X);
            int fbH = Math.Max(1, window.FramebufferSize.Y);
            int winW = Math.Max(1, window.Size.X);
            int winH = Math.Max(1, window.Size.Y);

            bool fbChanged = fbW != FramebufferWidth || fbH != FramebufferHeight;
            float logicalW = winW / DisplayScale.Factor;
            float logicalH = winH / DisplayScale.Factor;
            bool winChanged = Math.Abs(Shell.RenderRect.Width - logicalW) > 0.01f ||
                              Math.Abs(Shell.RenderRect.Height - logicalH) > 0.01f;

            if (!fbChanged && !winChanged)
                return;

            // Rebuild GPU surfaces for new size (clears offscreen buffer)
            if (fbChanged)
            {
                RenewCanvas(fbW, fbH);
            }

            Shell.HandleClientSize(winW, winH);

            if (Shell.ShellApp?.ActiveView != null)
            {
                Shell.ShellApp.ActiveView.FullRenderRequired = true;
                Shell.ShellApp.ActiveView.RenderRequired = true;
            }

            // Event-driven loop may be waiting; wake it so the frame paints immediately
            UiHost.Current.Wake();

            // On Windows, the Win32 modal sizing loop (WM_ENTERSIZEMOVE / WM_SIZING) blocks the
            // main thread inside DefWindowProc until mouse release. Render synchronously so the
            // window contents reflow in real time while dragging the border.
            if (OperatingSystem.IsWindows() && Shell.IsLoaded && Shell.ShellApp?.ActiveView?.IsLoaded == true)
            {
                try
                {
                    window.DoRender();
                }
                catch { }
            }
        }

        window.FramebufferResize += _ => OnWindowResized();
        window.Resize += _ => OnWindowResized();
    }

    public static void ResetContext()
    {
        grContext.ResetContext();
    }
}