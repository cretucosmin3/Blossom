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

    [Fact]
    public void ReplaceChildren_DetachesAttachesAndReorders_AndNewChildFollowsParentResize()
    {
        using var surface = SKSurface.Create(new SKImageInfo(160, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var removed = new VisualElement { Name = "removed" };
        var app = new HostingApp(root => root.AddChild(removed));
        using var session = CreateSession(new SoftwareUiHost(), app, 80, 40);
        var root = app.Main.Stretch!.Parent!;
        var box = app.Main.Stretch;
        var added = new VisualElement { Name = "added" };
        added.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top;
        added.Transform.Height = 10;
        var formerParent = new VisualElement();
        formerParent.AddChild(added);

        root.ReplaceChildren(new[] { added, box });

        Assert.Equal(new[] { added, box }, root.Children);
        Assert.Null(removed.Parent);
        Assert.Null(removed.ParentView);
        Assert.Same(root, added.Parent);
        Assert.Same(app.Main, added.ParentView);
        Assert.Empty(formerParent.Children);

        session.Paint(Frame(surface, session), force: true);
        Assert.InRange(added.Transform.Width, 79f, 81f);

        session.SetSize(160, 40, 160, 40);
        session.Paint(Frame(surface, session), force: true);
        Assert.InRange(added.Transform.Width, 159f, 161f);
    }

    [Fact]
    public void ScrolledOutRow_SkipsItsSubtree_AndPlacesShowsAndHitsItWhenScrolledBack()
    {
        // A row scrolled out of the clip skips layout and evaluation for everything under it. Scrolling it
        // back must place, draw and hit-test its children as if nothing had been skipped.
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var scroller = new ScrollContainer { OverflowY = OverflowMode.Scroll, SmoothScroll = false };
        scroller.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
        var rows = new DotRow[6];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = new DotRow(i * 20);
            scroller.AddChild(rows[i]);
        }
        var app = new HostingApp(root => root.AddChild(scroller));
        using var session = CreateSession(new SoftwareUiHost(), app, 80, 40);
        session.Paint(Frame(surface, session), force: true);

        Assert.NotEqual(Visibility.Hidden, rows[0].Dot.ComputedVisibility);
        Assert.Equal(Visibility.Hidden, rows[4].Dot.ComputedVisibility);

        scroller.ScrollY = 80;
        session.Paint(Frame(surface, session), force: true);
        session.Paint(Frame(surface, session), force: true);

        var dot = rows[4].Dot;
        Assert.Equal(Visibility.Hidden, rows[0].Dot.ComputedVisibility);
        Assert.NotEqual(Visibility.Hidden, dot.ComputedVisibility);
        Assert.InRange(dot.Transform.Computed.Y, 4.9f, 5.1f);
        using (var pixmap = surface.PeekPixels())
            Assert.Equal(255, pixmap.GetPixelColor(35, 10).Green);

        int clicks = 0;
        dot.Events.OnClick += (_, _) => clicks++;
        session.Input.PointerDown(35, 10, 0);
        session.Input.PointerUp(35, 10, 0);
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void DisposedElement_IsCollected_AndFreesItsOwnStyleButNotASharedOne()
    {
        // The view's draw-command ledger and a shared style's element list both used to keep every
        // removed element alive, and an element's own default style waited for the finalizer.
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var shared = new ElementStyle { BackColor = new SKColor(255, 0, 0, 255) };
        VisualElement root = null!;
        var app = new HostingApp(r => root = r);
        using var session = CreateSession(new SoftwareUiHost(), app, 80, 40);

        var (removed, ownPaint, key) = PaintThenDispose(root, shared, session, surface);
        Assert.Null(app.Main.Ledger.GetCommands(key));
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(removed.IsAlive);
        Assert.Equal(IntPtr.Zero, ownPaint.Handle);
        Assert.NotEqual(IntPtr.Zero, shared.Text.Paint.Handle);
    }

    [Fact]
    public void AnimationThatDisposesItsElement_MidFrame_DoesNotDrawTheDisposedSubtree()
    {
        // A banner that disposes itself when its animation ends: the frame that runs the callback must
        // not go on to draw it or its children with their freed paints and fonts.
        using var surface = SKSurface.Create(new SKImageInfo(80, 40, SKColorType.Rgba8888, SKAlphaType.Premul));
        var banner = new VisualElement { Transform = new Transform(0, 0, 80, 40) };
        banner.Style.BackColor = new SKColor(0, 255, 0, 255);
        banner.AddChild(new VisualElement { Text = "LEVEL 2", Transform = new Transform(0, 0, 80, 20) });
        banner.Animate().ScaleY(0.5f, 0.05f).Call(banner.Dispose);
        var app = new HostingApp(root => root.AddChild(banner));
        using var session = CreateSession(new SoftwareUiHost(), app, 80, 40);

        for (int i = 0; i < 10 && !banner.IsDisposed; i++)
        {
            session.Tick(0.016f);
            session.Paint(Frame(surface, session));
        }

        Assert.True(banner.IsDisposed);
        session.Paint(Frame(surface, session), force: true);
        using var pixmap = surface.PeekPixels();
        Assert.NotEqual(255, pixmap.GetPixelColor(40, 30).Green);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static (WeakReference, SKPaint, string) PaintThenDispose(VisualElement root, ElementStyle shared, UiSession session, SKSurface surface)
    {
        var withShared = new VisualElement { Style = shared, Transform = new Transform(0, 0, 40, 40) };
        var withOwn = new VisualElement { Text = "hi", Transform = new Transform(40, 0, 40, 40) };
        root.AddChild(withShared);
        root.AddChild(withOwn);
        session.Paint(Frame(surface, session), force: true);
        string key = withShared.Id.ToString("N");
        Assert.NotNull(withShared.ParentView.Ledger.GetCommands(key));

        withShared.Dispose();
        withOwn.Dispose();
        session.Paint(Frame(surface, session), force: true);
        return (new WeakReference(withShared), withOwn.Style.Text.Paint, key);
    }

    /// <summary>A 20px row that places its green dot in <see cref="LayoutChildren"/>.</summary>
    sealed class DotRow : VisualElement
    {
        public readonly VisualElement Dot = new() { Name = "dot" };

        public DotRow(float y)
        {
            Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top;
            Transform.LocalY = y;
            Transform.Height = 20;
            Dot.Style.BackColor = new SKColor(0, 255, 0, 255);
            AddChild(Dot);
        }

        protected override void LayoutChildren() =>
            Dot.Transform.SetAbsoluteFrame(Transform.AbsoluteX + 30, Transform.AbsoluteY + 5, 10, 10);
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
