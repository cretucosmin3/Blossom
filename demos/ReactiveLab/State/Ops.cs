using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Blossom.Desktop;
using Blossom.Reactive;
using static Blossom.Reactive.ReactiveEngine;

namespace ReactiveLab.State;

public sealed record OpReport(int Seq, string Name, double Ms, long Cards, long Visible, long Columns, long Stats, bool Batched);

/// <summary>
/// Runs board operations with timing. The time covers the whole reactive update (writes, memo
/// recomputes, every effect and reconcile), because effects flush synchronously before the write
/// (or the batch) returns. Layout and paint happen later, on the next frame.
/// </summary>
public static class Ops
{
    private static int _seq;

    /// <summary>When off, each signal write flushes on its own (watch the memo counters climb).</summary>
    public static readonly Signal<bool> Batched = CreateSignal(true);

    public static readonly Signal<IReadOnlyList<OpReport>> Log = CreateSignal<IReadOnlyList<OpReport>>(Array.Empty<OpReport>());

    public static void Run(string name, Action action)
    {
        long cards = Diag.CardUpdates, visible = Diag.VisibleRuns, columns = Diag.ColumnRuns, stats = Diag.StatsRuns;
        bool batched = Batched.Peek();
        var sw = Stopwatch.StartNew();
        if (batched)
            Batch(action);
        else
            action();
        sw.Stop();

        var report = new OpReport(++_seq, name, sw.Elapsed.TotalMilliseconds,
            Diag.CardUpdates - cards, Diag.VisibleRuns - visible, Diag.ColumnRuns - columns, Diag.StatsRuns - stats, batched);
        Log.Value = Log.Peek().Prepend(report).Take(14).ToList();
    }

    // ── Live mode: a timer that keeps editing the board ─────────────────────────────────────

    public static readonly int[] Rates = { 0, 10, 30, 60 };
    public static readonly int[] EditsPerTick = { 1, 10, 100, 500 };

    public static readonly Signal<int> LiveRate = CreateSignal(0);
    public static readonly Signal<int> LiveEdits = CreateSignal(1);
    public static readonly Signal<bool> LiveMoves = CreateSignal(true);
    public static readonly Signal<long> LiveTicks = CreateSignal(0L);
    public static readonly Signal<double> LiveTickMs = CreateSignal(0.0);

    private static IDisposable? _timer;
    private static Effect? _liveEffect;

    /// <summary>Starts or stops the timer whenever the rate changes. Called once by the view.</summary>
    public static void StartLiveDriver()
    {
        _liveEffect ??= CreateEffect(() =>
        {
            int rate = Rates[LiveRate.Value];
            _timer?.Dispose();
            _timer = rate == 0 ? null : Shell.PostPeriodic(TimeSpan.FromMilliseconds(1000.0 / rate), Tick);
        });
    }

    private static void Tick()
    {
        var sw = Stopwatch.StartNew();
        int edits = EditsPerTick[LiveEdits.Peek()];
        bool moves = LiveMoves.Peek();
        Batch(() =>
        {
            Board.EditRandom(edits);
            if (moves) Board.MoveRandom(Math.Max(1, edits / 4));
        });
        sw.Stop();
        Batch(() =>
        {
            LiveTickMs.Value = sw.Elapsed.TotalMilliseconds;
            LiveTicks.Value = LiveTicks.Peek() + 1;
        });
    }
}
