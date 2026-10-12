using System;
using System.Threading;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Hosting;
using SkiaSharp;
using Xunit;

namespace Blossom.Hosting.Tests;

public class UiSessionTests
{
    [Fact]
    public void Paint_WritesPixelsOnSoftwareSurface()
    {
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var host = new SoftwareUiHost();
        var app = new HostingApp();
        using var session = CreateSession(host, app, 80, 40);

        session.Paint(Frame(surface, session), force: true);

        using var pixmap = surface.PeekPixels();
        Assert.False(pixmap.GetPixelColor(10, 10).Alpha == 0);
        Assert.Equal(255, pixmap.GetPixelColor(10, 10).Red);
        Assert.Equal(0, pixmap.GetPixelColor(50, 30).Alpha);
    }

    [Fact]
    public void SetSize_UpdatesLayoutWithoutPaint_AndStretchAfterPaint()
    {
        using var surface = SKSurface.Create(new SKImageInfo(161, 41, SKColorType.Rgba8888, SKAlphaType.Premul));
        var host = new SoftwareUiHost();
        var app = new HostingApp();
        using var session = new UiSession(host, app, HudOptions());
        session.SetSize(80.5f, 40.25f, 81, 40);
        session.Init();

        Assert.Equal(80.5f, app.Main.LayoutWidth.Value, 2);
        Assert.Equal(40.25f, app.Main.LayoutHeight.Value, 2);

        session.Paint(Frame(surface, session), force: true);
        Assert.InRange(app.Main.Stretch!.Transform.Width, 80f, 81.5f);

        session.SetSize(161f, 40.25f, 161, 40);
        Assert.Equal(161f, app.Main.LayoutWidth.Value, 2);

        session.Paint(Frame(surface, session), force: true);
        Assert.InRange(app.Main.Stretch.Transform.Width, 160f, 162f);
    }

    [Fact]
    public void Tick_RunsPostFromWorker()
    {
        var host = new SoftwareUiHost();
        var app = new HostingApp();
        using var session = CreateSession(host, app, 80, 40);

        int ran = 0;
        using var posted = new ManualResetEventSlim(false);
        var worker = new Thread(() =>
        {
            host.Post(() => ran = 1);
            posted.Set();
        });
        worker.IsBackground = true;
        worker.Start();
        Assert.True(posted.Wait(TimeSpan.FromSeconds(5)));
        worker.Join(TimeSpan.FromSeconds(5));
        Assert.Equal(0, ran);

        session.Tick(0.016f);
        Assert.Equal(1, ran);
    }

    [Fact]
    public void NeedsPaint_RespectsMaxFps_ThrottlesUntilFramePeriodElapses()
    {
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var host = new SoftwareUiHost();
        var app = new HostingApp();
        var options = HudOptions();
        options.MaxFps = 30;
        using var session = new UiSession(host, app, options);
        session.SetSize(80, 40, 80, 40);
        session.Init();

        Assert.True(session.NeedsPaint);
        session.Paint(Frame(surface, session));
        Assert.False(session.NeedsPaint);

        // Mark an element dirty
        app.Main.Stretch!.Style.BackColor = SKColors.Green;
        Assert.False(session.NeedsPaint);

        // Removing MaxFps unthrottles immediately
        session.MaxFps = null;
        Assert.True(session.NeedsPaint);

        // Re-enabling MaxFps throttles again after paint
        session.MaxFps = 20;
        session.Paint(Frame(surface, session));
        Assert.False(session.NeedsPaint);

        app.Main.Stretch.Style.BackColor = SKColors.Blue;
        Assert.False(session.NeedsPaint);

        // Wait past 50ms (for 20fps)
        Thread.Sleep(60);
        Assert.True(session.NeedsPaint);
    }

    static UiSession CreateSession(IUiHost host, Application app, float logicalW, float logicalH)
    {
        var session = new UiSession(host, app, HudOptions());
        session.SetSize(logicalW, logicalH, (int)logicalW, (int)logicalH);
        session.Init();
        return session;
    }

    static UiSessionOptions HudOptions() => new()
    {
        PaintMode = PaintMode.FullFrame,
        ClearColor = SKColors.Transparent,
        ClearCanvas = true,
        InstallSynchronizationContext = true
    };

    static RenderFrame Frame(SKSurface surface, UiSession session) =>
        new(
            canvas: surface.Canvas,
            logicalSize: session.LogicalSize,
            framebufferSize: session.FramebufferSize,
            gpuContext: null,
            surface: surface,
            clearCanvas: true,
            clearColor: SKColors.Transparent);

    sealed class HostingApp : Application
    {
        public HostingView Main { get; }

        public HostingApp()
        {
            Main = new HostingView();
            AddView(Main);
            SetActiveView(Main);
        }
    }

    sealed class HostingView : View
    {
        public VisualElement? Stretch;

        public HostingView() : base("main") { }

        public override void Init()
        {
            var root = new VisualElement { Name = "root" };
            root.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;

            var box = new VisualElement { Name = "box" };
            box.Style.BackColor = new SKColor(255, 0, 0, 255);
            box.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top;
            box.Transform.Height = 20;

            root.AddChild(box);
            AddElement(root);
            Stretch = box;
        }
    }
}
