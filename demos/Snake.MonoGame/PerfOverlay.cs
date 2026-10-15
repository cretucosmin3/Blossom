using System;
using Blossom.Core.Visual;
using Blossom.Core.Visual.Enums;
using Blossom.Primitives;
using Blossom.Reactive;
using SkiaSharp;

namespace SnakeDemo;

/// <summary>
/// Corner panel showing <see cref="GameState.Perf"/>: FPS, frame time, CPU time per phase, Blossom's own
/// repaint cost, GC pressure, and a frame-time graph. Every row is a binding on the one snapshot signal.
/// </summary>
internal sealed class PerfOverlay : VisualElement
{
    public const float Width = 236, Height = 162;
    private const float BudgetMs = 1000f / 60f;

    public PerfOverlay(GameState state, float x, float y)
    {
        Name = "PerfOverlay";
        Look.Place(this, x, y, Width, Height);
        Transform.Anchor = Anchor.Right | Anchor.Bottom;
        ZIndex = 100; // Above the dialogs, so menus and their animations can be measured too.
        IsClickthrough = true;
        Style.BackColor = Look.Panel.WithAlpha(235);
        Style.Border = new BorderStyle { Width = 1, Color = Look.Line, Roundness = 0 };
        Style.Shadow = Look.HardShadow(4);
        this.BindVisible(() => state.ShowPerf.Value);

        AddChild(Look.Box(0, 0, Width, 18, Look.Line));
        AddChild(Look.Label("PERF", 8, 1, 60, 16, 9.5f, Look.Text, TextAlign.Left, 900));
        AddChild(Look.Label("F3 hide  F4 vsync", Width - 158, 1, 150, 16, 8.5f, Look.Dim, TextAlign.Right, 700));

        float y0 = 24, row = 15;
        var fps = Row(y0, 13f, 900);
        fps.Bind(el =>
        {
            var p = state.Perf.Value;
            el.Text = $"{p.Fps,4:0} FPS";
            el.Style.Text.Color = p.Fps >= 58 ? Look.Accent : p.Fps >= 30 ? Look.Gold : Look.Danger;
        });
        var vsync = Look.Label("", Width - 108, y0 + 2, 100, 14, 9f, Look.Dim, TextAlign.Right, 700);
        vsync.BindText(() => state.Perf.Value.VSync ? "VSYNC ON" : "VSYNC OFF");
        AddChild(vsync);

        Row(y0 + row * 1.3f).BindText(() =>
            $"frame  {state.Perf.Value.FrameMs,6:0.00} ms  max {state.Perf.Value.FrameMaxMs,5:0.0}");
        Row(y0 + row * 2.3f).BindText(() =>
            $"update {state.Perf.Value.UpdateMs,6:0.00} ms  draw {state.Perf.Value.DrawMs,5:0.00}");
        Row(y0 + row * 3.3f).BindText(() =>
            $"ui     {state.Perf.Value.UiPaintMs,6:0.00} ms  {state.Perf.Value.UiPaintsPerSec,3}/s paints");
        Row(y0 + row * 4.3f).BindText(() =>
            $"gc {state.Perf.Value.HeapMb,5:0.0} MB  +{state.Perf.Value.AllocKbPerSec,5:0} KB/s  g0 {state.Perf.Value.Gen0Collections}");

        var graph = Look.Place(new FrameGraph(BudgetMs), 8, y0 + row * 5.5f, Width - 16, Height - (y0 + row * 5.5f) - 8);
        graph.Bind(g => g.History = state.Perf.Value.FrameHistory);
        AddChild(graph);
    }

    private VisualElement Row(float y, float size = 9.5f, int weight = 400)
    {
        var label = Look.Label("", 8, y, Width - 16, size + 5, size, Look.Text, TextAlign.Left, weight);
        AddChild(label);
        return label;
    }
}

/// <summary>
/// One bar per frame (oldest left), height = frame time, colored against the 60 FPS budget. The scale
/// fits the slowest frame shown (labelled top-left), and the budget is drawn as a line when in range.
/// Drawn with Skia directly through <see cref="Control.Paint"/>.
/// </summary>
internal sealed class FrameGraph : Control
{
    private readonly float _budgetMs;
    private readonly SKFont _font = Blossom.Utils.Fonts.CreateFont(Look.Mono, 8f);
    private float[] _history = Array.Empty<float>();

    public FrameGraph(float budgetMs)
    {
        _budgetMs = budgetMs;
        IsClickthrough = true;
        Style.BackColor = Look.Ink;
        Style.Border = new BorderStyle { Width = 0, Roundness = 0 };
    }

    public float[] History
    {
        get => _history;
        set => SetAndPaint(ref _history, value ?? Array.Empty<float>());
    }

    protected override void Paint(SKCanvas c)
    {
        if (_history.Length == 0)
            return;

        float scaleMs = 0.5f;
        foreach (var ms in _history)
            scaleMs = Math.Max(scaleMs, ms * 1.15f);

        using var paint = new SKPaint { IsAntialias = false };
        float barW = W / _history.Length;
        for (int i = 0; i < _history.Length; i++)
        {
            float ms = _history[i];
            if (ms <= 0)
                continue;
            float h = Math.Max(1f, ms / scaleMs * H);
            paint.Color = ms <= _budgetMs * 1.1f ? Look.Accent.WithAlpha(200)
                : ms <= _budgetMs * 2.1f ? Look.Gold
                : Look.Danger;
            c.DrawRect(i * barW, H - h, Math.Max(1f, barW - 0.5f), h, paint);
        }

        paint.Color = Look.Text.WithAlpha(110);
        if (_budgetMs <= scaleMs)
            c.DrawRect(0, H - _budgetMs / scaleMs * H, W, 1, paint);

        paint.IsAntialias = true;
        paint.Color = Look.Dim;
        c.DrawText($"{scaleMs:0.0} ms", 2, 9, SKTextAlign.Left, _font, paint);
    }
}
