using System;
using System.Collections.Generic;
using Blossom.Animations;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Hosting;
using SkiaSharp;
using Xunit;

namespace Blossom.Hosting.Tests;

public class AnimationTests
{
    [Theory]
    [InlineData(60, 60)]     // VSync: one tick per paint.
    [InlineData(1000, 60)]   // VSync off: many ticks per paint (MaxFps caps the UI).
    [InlineData(1000, 20)]   // A slow UI frame rate.
    public void Tween_ReachesTheSameValue_WhateverTheTickAndPaintRates(int ticksPerSecond, int paintsPerSecond)
    {
        using var ui = new Ui();
        var box = ui.Add(Box());
        ui.Paint();

        box.Play(Anim.Tween(0, 100, 1f, v => box.Transform.LocalX = v, Ease.Linear));
        ui.Run(0.5f, ticksPerSecond, paintsPerSecond);

        // The first paint shows the start state, and the last paint can land up to one interval
        // before the end of the run, so the value trails 50 by at most two paint intervals.
        Assert.InRange(box.Transform.LocalX, 50 - 200f / paintsPerSecond - 0.5f, 50.5f);
    }

    [Fact]
    public void Tween_FirstFrameShowsStart_AndLastFrameLandsExactlyOnEnd_ThenGoesIdle()
    {
        using var ui = new Ui();
        var box = ui.Add(Box());
        ui.Paint();

        float value = -1;
        var playback = box.Play(Anim.Tween(10, 20, 0.2f, v => value = v));
        ui.Tick(0.5f); // Time that passed before the animation was ever drawn isn't played.
        ui.Paint();
        Assert.Equal(10, value);

        ui.Run(0.3f);
        Assert.Equal(20, value);
        Assert.True(playback.IsComplete);

        ui.Paint();
        Assert.False(ui.Session.NeedsPaint);
    }

    [Fact]
    public void Timeline_StepsFollowEachOther_WithStartsTogether_AtPlacesExactly()
    {
        using var ui = new Ui();
        var box = ui.Add(Box());
        ui.Paint();

        var events = new List<(string, double)>();
        box.Animate()
            .Wait(0.2f)
            .Call(() => events.Add(("a", ui.Session.Application.Time)))
            .Wait(0.3f)
            .With().Call(() => events.Add(("with", ui.Session.Application.Time)))
            .Call(() => events.Add(("b", ui.Session.Application.Time)))
            .At(0.1f).Call(() => events.Add(("at", ui.Session.Application.Time)));

        double start = ui.Session.Application.Time;
        ui.Run(1f);

        Assert.Equal(new[] { "at", "a", "with", "b" }, events.ConvertAll(e => e.Item1));
        Assert.Equal(0.1, events[0].Item2 - start, 1);
        Assert.Equal(0.2, events[1].Item2 - start, 1);
        Assert.Equal(0.2, events[2].Item2 - start, 1);
        Assert.Equal(0.5, events[3].Item2 - start, 1);
    }

    [Fact]
    public void Timeline_ChainedToTweens_ContinueFromWhereThePreviousStepLeftOff()
    {
        using var ui = new Ui();
        var box = ui.Add(Box());
        box.Opacity = 0.2f;
        ui.Paint();

        var seen = new List<float>();
        box.Animate()
            .Fade(1f, 0.1f, Ease.Linear)
            .Fade(0.5f, 0.1f, Ease.Linear)
            .Tween(0.0001f, _ => seen.Add(box.Opacity));
        box.Play(Anim.Forever(_ => seen.Add(box.Opacity)));

        ui.Run(0.3f);

        Assert.Equal(0.5f, box.Opacity, 3);
        Assert.Equal(0.2f, seen[0], 3);
        Assert.Contains(seen, v => v > 0.95f); // Reached 1 before heading to 0.5.
        for (int i = 1; i < seen.Count && seen[i] < 0.95f; i++)
            Assert.True(seen[i] >= seen[i - 1]); // No jump back to 0.2 when the second step starts.
    }

    [Fact]
    public void Repeat_Yoyo_PlaysForwardThenBack_AndFiresCallbacksOnForwardLoopsOnly()
    {
        using var ui = new Ui();
        var box = ui.Add(Box());
        ui.Paint();

        int calls = 0;
        bool completed = false;
        var timeline = box.Animate()
            .Call(() => calls++)
            .Tween(0, 100, 0.2f, v => box.Transform.LocalX = v, Ease.Linear)
            .Repeat(4, yoyo: true)
            .OnComplete(() => completed = true);

        ui.Run(0.3f); // Halfway back.
        Assert.InRange(box.Transform.LocalX, 40, 60);

        ui.Run(0.6f);
        Assert.True(completed);
        Assert.Equal(0, box.Transform.LocalX, 3); // Four loops end on a backward one.
        Assert.Equal(2, calls);
        Assert.True(timeline.Playback!.IsComplete);
    }

    [Fact]
    public void Repeat_Forever_KeepsTheUiPainting_UntilStopped()
    {
        using var ui = new Ui();
        var box = ui.Add(Box());
        ui.Paint();

        var playback = box.Play(Anim.Tween(0, 1, 0.1f, v => box.Opacity = v).Repeat());
        ui.Run(1f);
        Assert.True(ui.Session.NeedsPaint);

        playback.Stop();
        ui.Paint();
        ui.Paint();
        Assert.False(ui.Session.NeedsPaint);
    }

    [Fact]
    public void Stop_LeavesValuesWhereTheyAre_AndSkipsOnComplete()
    {
        using var ui = new Ui();
        var box = ui.Add(Box());
        ui.Paint();

        bool completed = false;
        var playback = box.Play(Anim.Tween(0, 100, 1f, v => box.Transform.LocalX = v, Ease.Linear))
            .OnComplete(() => completed = true);
        ui.Run(0.5f);
        float at = box.Transform.LocalX;
        playback.Stop();
        ui.Run(1f);

        Assert.Equal(at, box.Transform.LocalX);
        Assert.False(completed);
    }

    [Fact]
    public void Finish_AppliesTheEndAndRunsOnComplete()
    {
        using var ui = new Ui();
        var box = ui.Add(Box());
        ui.Paint();

        bool completed = false;
        var playback = box.Play(Anim.Tween(0, 100, 5f, v => box.Transform.LocalX = v)).OnComplete(() => completed = true);
        playback.Finish();

        Assert.Equal(100, box.Transform.LocalX);
        Assert.True(completed);
    }

    [Fact]
    public void Pause_StopsRequestingFrames_AndResumeContinuesFromTheSameTime()
    {
        using var ui = new Ui();
        var box = ui.Add(Box());
        ui.Paint();

        var playback = box.Play(Anim.Tween(0, 100, 1f, v => box.Transform.LocalX = v, Ease.Linear));
        ui.Run(0.3f);
        playback.Pause();
        ui.Paint();
        ui.Paint();
        Assert.False(ui.Session.NeedsPaint);

        float at = box.Transform.LocalX;
        ui.Tick(5f); // Paused time passes.
        playback.Resume();
        ui.Run(0.1f);
        Assert.InRange(box.Transform.LocalX, at, at + 12);
    }

    [Fact]
    public void Call_ThatDisposesTheElement_RemovesItWithoutThrowing()
    {
        using var ui = new Ui();
        var box = ui.Add(Box());
        ui.Paint();

        box.Animate().Fade(0, 0.1f).Call(box.Dispose);
        ui.Run(0.3f);

        Assert.True(box.IsDisposed);
        Assert.Equal(0, ui.Pixel(5, 5).Alpha);
        Assert.False(ui.Session.NeedsPaint);
    }

    [Fact]
    public void DisposingTheElement_StopsItsAnimations()
    {
        using var ui = new Ui();
        var box = ui.Add(Box());
        ui.Paint();

        var playback = box.Play(Anim.Forever(_ => { }));
        box.Dispose();

        Assert.True(playback.IsStopped);
        Assert.Throws<ObjectDisposedException>(() => box.Play(Anim.Wait(1)));
    }

    [Fact]
    public void AnimatedValue_Retarget_ContinuesFromTheCurrentValue()
    {
        using var ui = new Ui();
        var box = ui.Add(Box());
        ui.Paint();

        var value = new AnimatedValue(box, 0f, duration: 0.2f, ease: Ease.Linear);
        var changes = new List<float>();
        value.Changed += changes.Add;

        value.Target = 1f;
        ui.Run(0.1f);
        float mid = value.Value;
        Assert.InRange(mid, 0.3f, 0.6f);

        value.Target = 0f;
        ui.Paint();
        Assert.Equal(mid, value.Value, 3); // No jump when the target changes.

        ui.Run(0.3f);
        Assert.Equal(0f, value.Value);
        Assert.False(value.IsAnimating);
        Assert.Equal(0f, changes[^1]);

        value.Snap(0.75f);
        Assert.Equal(0.75f, value.Value);
    }

    [Fact]
    public void Stagger_StartsEachItemAfterThePrevious()
    {
        using var ui = new Ui();
        var a = ui.Add(Box());
        var b = ui.Add(Box());
        ui.Paint();

        a.Play(Anim.Stagger(0.2f,
            Anim.Tween(0, 1, 0.1f, v => a.Opacity = v),
            Anim.Tween(0, 1, 0.1f, v => b.Opacity = v)));
        b.Opacity = 0;
        ui.Run(0.15f);

        Assert.Equal(1f, a.Opacity);
        Assert.Equal(0f, b.Opacity);
    }

    [Fact]
    public void Eases_StartAtZero_AndEndAtOne()
    {
        var eases = new Func<float, float>[]
        {
            Ease.Linear, Ease.InQuad, Ease.OutQuad, Ease.InOutQuad, Ease.InCubic, Ease.OutCubic, Ease.InOutCubic,
            Ease.InQuart, Ease.OutQuart, Ease.InOutQuart, Ease.InExpo, Ease.OutExpo, Ease.InSine, Ease.OutSine,
            Ease.InOutSine, Ease.InBack, Ease.OutBack, Ease.InOutBack, Ease.OutElastic, Ease.OutBounce,
            Ease.InBounce, Ease.Steps(4), Ease.CubicBezier(0.25f, 0.1f, 0.25f, 1f), Ease.Reverse(Ease.OutCubic),
        };
        foreach (var ease in eases)
        {
            Assert.Equal(0f, ease(0f), 3);
            Assert.Equal(1f, ease(1f), 3);
        }

        Assert.Equal(0.5f, Ease.Steps(4)(0.6f));
        Assert.Equal(0.5f, Ease.CubicBezier(0, 0, 1, 1)(0.5f), 3);
    }

    static VisualElement Box()
    {
        var el = new VisualElement();
        el.Transform.Anchor = Anchor.Left | Anchor.Top;
        el.Transform.Width = 20;
        el.Transform.Height = 20;
        el.Style.BackColor = new SKColor(255, 0, 0, 255);
        return el;
    }

    /// <summary>A session driven like a game loop: <see cref="Run"/> ticks and paints at given rates.</summary>
    sealed class Ui : IDisposable
    {
        private readonly SKSurface _surface = SKSurface.Create(new SKImageInfo(100, 100, SKColorType.Rgba8888, SKAlphaType.Premul));
        private readonly TestApp _app = new();

        public Ui()
        {
            Session = new UiSession(new SoftwareUiHost(), _app, new UiSessionOptions
            {
                PaintMode = PaintMode.FullFrame,
                ClearColor = SKColors.Transparent,
                ClearCanvas = true
            });
            Session.SetSize(100, 100, 100, 100);
            Session.Init();
        }

        public UiSession Session { get; }

        public VisualElement Add(VisualElement el)
        {
            _app.Main.Root.AddChild(el);
            return el;
        }

        public void Tick(float dt) => Session.Tick(dt);

        public void Paint() =>
            Session.Paint(new RenderFrame(_surface.Canvas, Session.LogicalSize, Session.FramebufferSize), force: true);

        /// <summary>Plays <paramref name="seconds"/> of game time, painting whenever a paint interval has passed.</summary>
        public void Run(float seconds, int ticksPerSecond = 60, int paintsPerSecond = 60)
        {
            int ticks = (int)MathF.Round(seconds * ticksPerSecond);
            float tick = 1f / ticksPerSecond, paintEvery = 1f / paintsPerSecond, sincePaint = 0;
            for (int i = 0; i < ticks; i++)
            {
                Tick(tick);
                sincePaint += tick;
                if (sincePaint >= paintEvery - 1e-5f)
                {
                    sincePaint = 0;
                    Paint();
                }
            }
        }

        public SKColor Pixel(int x, int y)
        {
            using var pixmap = _surface.PeekPixels();
            return pixmap.GetPixelColor(x, y);
        }

        public void Dispose()
        {
            Session.Dispose();
            _surface.Dispose();
        }
    }

    sealed class TestApp : Application
    {
        public TestView Main { get; } = new();

        public TestApp()
        {
            AddView(Main);
            SetActiveView(Main);
        }
    }

    sealed class TestView : View
    {
        public VisualElement Root { get; } = new() { Name = "root" };

        public TestView() : base("animation-test") { }

        public override void Init()
        {
            Root.Transform.Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom;
            AddElement(Root);
        }
    }
}
