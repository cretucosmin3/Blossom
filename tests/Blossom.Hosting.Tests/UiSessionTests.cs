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
    public void ViewSession_PaintsWithoutApplicationSubclass()
    {
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var view = new HostingView();
        using var session = new UiSession(new SoftwareUiHost(), view, HudOptions());
        session.SetSize(80, 40, 80, 40);
        session.Init();

        Assert.Same(view, session.Application.ActiveView);
        Assert.Same(session.Application, view.Application);

        session.Paint(Frame(surface, session), force: true);

        using var pixmap = surface.PeekPixels();
        Assert.Equal(255, pixmap.GetPixelColor(10, 10).Red);
        Assert.Equal(0, pixmap.GetPixelColor(50, 30).Alpha);
    }

    [Fact]
    public void ViewSession_RejectsViewOwnedByAnotherApplication()
    {
        var app = new HostingApp();
        Assert.Throws<InvalidOperationException>(() => new UiSession(new SoftwareUiHost(), app.Main, HudOptions()));
    }

    [Fact]
    public void Tick_AdvancesOnlyItsOwnSessionClock()
    {
        var host = new SoftwareUiHost();
        using var a = CreateSession(host, new HostingApp(), 80, 40);
        using var b = new UiSession(host, new HostingView(), HudOptions());
        b.SetSize(80, 40, 80, 40);
        b.Init();

        a.Tick(0.5f);
        a.Tick(0.5f);
        b.Tick(0.25f);

        Assert.Equal(1.0, a.Application.Time, 6);
        Assert.Equal(0.25, b.Application.Time, 6);
    }

    [Fact]
    public void FullFramePaint_RedrawsEverything_WhenOnlyATimerIsDue()
    {
        // A game host clears its target whenever NeedsPaint is true, then calls Paint. A due timer
        // makes NeedsPaint true with nothing dirty; the paint must still produce the whole UI.
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var host = new SoftwareUiHost();
        using var session = CreateSession(host, new HostingApp(), 80, 40);
        session.Paint(Frame(surface, session), force: true);
        Assert.False(session.NeedsPaint);

        using var timer = host.PostDelayed(TimeSpan.Zero, () => { });
        Assert.True(session.NeedsPaint);

        surface.Canvas.Clear(SKColors.Transparent);
        session.Paint(Frame(surface, session));

        using var pixmap = surface.PeekPixels();
        Assert.Equal(255, pixmap.GetPixelColor(10, 10).Red);
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

    [Fact]
    public void Paint_FullFrame_RedrawsEverything_OnFrameThatFollowsAnUpdateInvalidation()
    {
        // A host that clears its target before each Paint (SkiaRenderTarget2D.Begin() does) must get a
        // whole frame back every time NeedsPaint is true, including the follow-up frame that an
        // invalidation raised during OnUpdate schedules.
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var app = new HostingApp(root => root.AddChild(new InvalidatesOnFirstUpdate()));
        using var session = CreateSession(new SoftwareUiHost(), app, 80, 40);

        session.Paint(Frame(surface, session));
        Assert.True(session.NeedsPaint);

        surface.Canvas.Clear(SKColors.Transparent);
        session.Paint(Frame(surface, session));

        using var pixmap = surface.PeekPixels();
        Assert.Equal(255, pixmap.GetPixelColor(10, 10).Red);
    }

    [Fact]
    public void Paint_ElementDisposingItselfInOnUpdate_IsNotDrawnAndDoesNotThrow()
    {
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var effect = new DisposesOnUpdate();
        var app = new HostingApp(root => root.AddChild(effect));
        using var session = CreateSession(new SoftwareUiHost(), app, 80, 40);

        session.Paint(Frame(surface, session), force: true);

        Assert.True(effect.IsDisposed);
        Assert.Null(effect.Parent);
        using (var pixmap = surface.PeekPixels())
            Assert.Equal(0, pixmap.GetPixelColor(50, 30).Alpha);

        // The tree changed mid-pass, so another frame is due, and it paints without the element.
        Assert.True(session.NeedsPaint);
        session.Paint(Frame(surface, session));
        using (var pixmap = surface.PeekPixels())
        {
            Assert.Equal(0, pixmap.GetPixelColor(50, 30).Alpha);
            Assert.Equal(255, pixmap.GetPixelColor(10, 10).Red);
        }
    }

    [Fact]
    public void Paint_ElementRemovedBySiblingInOnUpdate_IsNotDrawn()
    {
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var victim = Corner();
        var remover = new RemovesOnUpdate(victim);
        var app = new HostingApp(root =>
        {
            root.AddChild(remover);
            root.AddChild(victim); // Updated after the remover, so it is removed before its own turn.
        });
        using var session = CreateSession(new SoftwareUiHost(), app, 80, 40);

        session.Paint(Frame(surface, session), force: true);
        using (var pixmap = surface.PeekPixels())
            Assert.Equal(255, pixmap.GetPixelColor(50, 30).Green);

        remover.Armed = true;
        session.Paint(Frame(surface, session), force: true);

        Assert.Null(victim.Parent);
        Assert.False(victim.IsDisposed);
        using (var pixmap = surface.PeekPixels())
            Assert.Equal(0, pixmap.GetPixelColor(50, 30).Alpha);
    }

    [Fact]
    public void Paint_ElementDisposedAfterItsOwnUpdate_IsNotDrawn()
    {
        // The victim is updated (and judged visible) first; a later sibling then disposes it, so only a
        // liveness check in the draw loop keeps the pass from painting a disposed element.
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var victim = Corner();
        var remover = new RemovesOnUpdate(victim, dispose: true);
        var app = new HostingApp(root =>
        {
            root.AddChild(victim);
            root.AddChild(remover);
        });
        using var session = CreateSession(new SoftwareUiHost(), app, 80, 40);

        session.Paint(Frame(surface, session), force: true);
        remover.Armed = true;
        session.Paint(Frame(surface, session), force: true);

        Assert.True(victim.IsDisposed);
        using var pixmap = surface.PeekPixels();
        Assert.Equal(0, pixmap.GetPixelColor(50, 30).Alpha);
    }

    [Fact]
    public void OnUpdate_ReceivesTimeSinceLastPaint_WhenHostTicksMoreOftenThanItPaints()
    {
        // A game with VSync off ticks thousands of times a second but paints at MaxFps. Animations must
        // advance by the time that passed since the last paint, not by the last tick's delta.
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var recorder = new RecordsDt { KeepAnimating = true };
        var app = new HostingApp(root => root.AddChild(recorder));
        using var session = CreateSession(new SoftwareUiHost(), app, 80, 40);
        session.Paint(Frame(surface, session), force: true);

        for (int i = 0; i < 40; i++) // A 25 FPS frame, ticked at 1000 Hz.
            session.Tick(0.001f);
        session.Paint(Frame(surface, session), force: true);

        Assert.Equal(0.040f, recorder.LastDt, 4);
    }

    [Fact]
    public void OnUpdate_AfterIdleWait_StartsWithOneFrameStep()
    {
        // The first frame after the UI sat idle must not report the whole wait, or animations
        // started by that frame would jump straight to their end.
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var recorder = new RecordsDt();
        var app = new HostingApp(root => root.AddChild(recorder));
        using var session = CreateSession(new SoftwareUiHost(), app, 80, 40);
        session.Paint(Frame(surface, session), force: true);

        session.Tick(5f);
        session.Paint(Frame(surface, session), force: true);

        Assert.InRange(recorder.LastDt, 0.001f, 1f / 30f);
    }

    /// <summary>A green box in the lower-right corner, clear of the red stretch box.</summary>
    static VisualElement Corner()
    {
        var el = new VisualElement { Name = "corner" };
        el.Transform.Anchor = Anchor.Left | Anchor.Top;
        el.Transform.LocalX = 40;
        el.Transform.LocalY = 24;
        el.Transform.Width = 30;
        el.Transform.Height = 14;
        el.Style.BackColor = new SKColor(0, 255, 0, 255);
        return el;
    }

    sealed class RecordsDt : VisualElement
    {
        public float LastDt = -1;
        public bool KeepAnimating;

        protected override void OnUpdate(float dt)
        {
            LastDt = dt;
            if (KeepAnimating)
                InvalidatePaint();
        }
    }

    sealed class InvalidatesOnFirstUpdate : VisualElement
    {
        private bool _done;

        protected override void OnUpdate(float dt)
        {
            if (_done) return;
            _done = true;
            InvalidatePaint();
        }
    }

    sealed class DisposesOnUpdate : VisualElement
    {
        public DisposesOnUpdate()
        {
            Transform.Anchor = Anchor.Left | Anchor.Top;
            Transform.LocalX = 40;
            Transform.LocalY = 24;
            Transform.Width = 30;
            Transform.Height = 14;
            Style.BackColor = new SKColor(0, 255, 0, 255);
        }

        protected override void OnUpdate(float dt) => Dispose();
    }

    sealed class RemovesOnUpdate : VisualElement
    {
        private readonly VisualElement _target;
        private readonly bool _dispose;

        public RemovesOnUpdate(VisualElement target, bool dispose = false)
        {
            _target = target;
            _dispose = dispose;
        }

        public bool Armed { get; set; }

        protected override void OnUpdate(float dt)
        {
            if (!Armed) return;
            if (_dispose)
                _target.Dispose();
            else
                _target.Parent?.RemoveChild(_target);
        }
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

        public HostingApp(Action<VisualElement>? addToRoot = null)
        {
            Main = new HostingView(addToRoot);
            AddView(Main);
            SetActiveView(Main);
        }
    }

    sealed class HostingView : View
    {
        private readonly Action<VisualElement>? _addToRoot;
        public VisualElement? Stretch;

        public HostingView(Action<VisualElement>? addToRoot = null) : base("main") => _addToRoot = addToRoot;

        public override void Init()
        {
            var root = new VisualElement { Name = "root" };
            root.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;

            var box = new VisualElement { Name = "box" };
            box.Style.BackColor = new SKColor(255, 0, 0, 255);
            box.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top;
            box.Transform.Height = 20;

            root.AddChild(box);
            _addToRoot?.Invoke(root);
            AddElement(root);
            Stretch = box;
        }
    }
}
