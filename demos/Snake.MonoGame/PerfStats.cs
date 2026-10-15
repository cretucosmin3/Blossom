using System;
using System.Diagnostics;

namespace SnakeDemo;

/// <summary>One published window of measurements, for the perf overlay.</summary>
public readonly record struct PerfSnapshot(
    float Fps,
    float FrameMs,
    float FrameMaxMs,
    float UpdateMs,
    float DrawMs,
    float UiPaintMs,
    int UiPaintsPerSec,
    float HeapMb,
    float AllocKbPerSec,
    int Gen0Collections,
    bool VSync,
    float[] FrameHistory);

/// <summary>
/// Collects per-frame CPU timings and publishes an averaged <see cref="PerfSnapshot"/> a few times a
/// second. Publishing slowly is deliberate: the overlay's text then changes 4x a second instead of every
/// frame, so the overlay itself costs Blossom only a few repaints per second.
/// </summary>
public sealed class PerfStats
{
    public const int HistoryLength = 120;
    private const double PublishSeconds = 0.25;

    private readonly float[] _history = new float[HistoryLength];
    private int _historyHead;

    private long _lastFrame;
    private long _windowStart;
    private int _frames, _updates, _uiPaints;
    private double _frameSum, _frameMax, _updateSum, _drawSum, _uiSum;
    private long _allocAtWindowStart;
    private int _gen0AtWindowStart;

    public static long Now => Stopwatch.GetTimestamp();
    public static double MsSince(long start) => Stopwatch.GetElapsedTime(start).TotalMilliseconds;

    /// <summary>Call once at the start of every Draw. Frame time is the interval between these calls.</summary>
    public void BeginFrame()
    {
        long now = Now;
        if (_lastFrame != 0)
        {
            double ms = Stopwatch.GetElapsedTime(_lastFrame, now).TotalMilliseconds;
            _frames++;
            _frameSum += ms;
            _frameMax = Math.Max(_frameMax, ms);
            _history[_historyHead] = (float)ms;
            _historyHead = (_historyHead + 1) % HistoryLength;
        }
        else
        {
            StartWindow(now);
        }

        _lastFrame = now;
    }

    public void AddUpdate(double ms)
    {
        _updates++;
        _updateSum += ms;
    }

    public void AddDraw(double ms) => _drawSum += ms;

    public void AddUiPaint(double ms)
    {
        _uiPaints++;
        _uiSum += ms;
    }

    /// <summary>Returns a snapshot and starts a new window once enough time has passed.</summary>
    public bool TryPublish(bool vsync, out PerfSnapshot snapshot)
    {
        long now = Now;
        double seconds = Stopwatch.GetElapsedTime(_windowStart, now).TotalSeconds;
        if (_frames == 0 || seconds < PublishSeconds)
        {
            snapshot = default;
            return false;
        }

        var history = new float[HistoryLength];
        for (int i = 0; i < HistoryLength; i++)
            history[i] = _history[(_historyHead + i) % HistoryLength]; // Oldest first.

        long allocated = GC.GetTotalAllocatedBytes(false);
        snapshot = new PerfSnapshot(
            Fps: (float)(_frames / seconds),
            FrameMs: (float)(_frameSum / _frames),
            FrameMaxMs: (float)_frameMax,
            UpdateMs: _updates == 0 ? 0 : (float)(_updateSum / _updates),
            DrawMs: (float)(_drawSum / _frames),
            UiPaintMs: _uiPaints == 0 ? 0 : (float)(_uiSum / _uiPaints),
            UiPaintsPerSec: (int)Math.Round(_uiPaints / seconds),
            HeapMb: GC.GetTotalMemory(false) / (1024f * 1024f),
            AllocKbPerSec: (float)((allocated - _allocAtWindowStart) / 1024.0 / seconds),
            Gen0Collections: GC.CollectionCount(0) - _gen0AtWindowStart,
            VSync: vsync,
            FrameHistory: history);

        StartWindow(now);
        return true;
    }

    private void StartWindow(long now)
    {
        _windowStart = now;
        _frames = _updates = _uiPaints = 0;
        _frameSum = _frameMax = _updateSum = _drawSum = _uiSum = 0;
        _allocAtWindowStart = GC.GetTotalAllocatedBytes(false);
        _gen0AtWindowStart = GC.CollectionCount(0);
    }
}
