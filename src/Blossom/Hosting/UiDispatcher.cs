using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Blossom.Hosting;

/// <summary>
/// Process-wide post queue and timer heap. Worker Post always enqueues here
/// then calls <see cref="UiHost.Current"/>.Wake().
/// </summary>
internal static class UiDispatcher
{
    static readonly ConcurrentQueue<Action> s_postQueue = new();
    static int s_uiThreadId;
    static readonly object s_timerLock = new();
    static readonly List<PostedTimer> s_timerHeap = new();

    internal static bool HasUiThread => s_uiThreadId != 0;

    internal static void BindUiThread() =>
        s_uiThreadId = Environment.CurrentManagedThreadId;

    internal static void ClearUiThread() => s_uiThreadId = 0;

    internal static bool CheckAccess() =>
        s_uiThreadId != 0 && Environment.CurrentManagedThreadId == s_uiThreadId;

    internal static void Post(Action action)
    {
        if (action == null) return;
        s_postQueue.Enqueue(action);
        UiHost.Current.Wake();
    }

    internal static Task PostAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try
            {
                action();
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

    internal static IDisposable PostDelayed(TimeSpan delay, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay));
        return ScheduleTimer(delay, TimeSpan.Zero, periodic: false, action);
    }

    internal static IDisposable PostPeriodic(TimeSpan period, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (period <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(period));
        return ScheduleTimer(period, period, periodic: true, action);
    }

    internal static void DrainPostQueue()
    {
        while (s_postQueue.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.Error($"Exception executing posted action:\n{ex}");
            }
        }
    }

    static IDisposable ScheduleTimer(TimeSpan delay, TimeSpan period, bool periodic, Action action)
    {
        var timer = new PostedTimer
        {
            DueTicks = Stopwatch.GetTimestamp() + ToStopwatchTicks(delay),
            Period = period,
            Action = action,
            IsPeriodic = periodic
        };
        lock (s_timerLock)
            TimerPush(timer);
        UiHost.Current.Wake();
        return timer;
    }

    static long ToStopwatchTicks(TimeSpan span) =>
        (long)(span.TotalSeconds * Stopwatch.Frequency);

    internal static void DrainTimers()
    {
        while (true)
        {
            PostedTimer? due;
            long now = Stopwatch.GetTimestamp();
            lock (s_timerLock)
            {
                CompactCancelledTimers();
                if (s_timerHeap.Count == 0 || s_timerHeap[0].DueTicks > now)
                    return;
                due = TimerPop();
            }

            if (due.Cancelled)
                continue;

            try
            {
                due.Action();
            }
            catch (Exception ex)
            {
                Log.Error($"Exception executing timed action:\n{ex}");
            }

            if (!due.IsPeriodic || due.Cancelled)
                continue;

            due.DueTicks = Stopwatch.GetTimestamp() + ToStopwatchTicks(due.Period);
            lock (s_timerLock)
            {
                if (!due.Cancelled)
                    TimerPush(due);
            }
        }
    }

    /// <summary>
    /// Seconds until the next scheduled timer, 0 if one is already due, or null to wait indefinitely.
    /// </summary>
    internal static double? SecondsUntilNextTimer()
    {
        long due;
        lock (s_timerLock)
        {
            CompactCancelledTimers();
            if (s_timerHeap.Count == 0)
                return null;
            due = s_timerHeap[0].DueTicks;
        }

        long now = Stopwatch.GetTimestamp();
        if (due <= now)
            return 0;
        return (due - now) / (double)Stopwatch.Frequency;
    }

    internal static long? NextTimerDueTicks()
    {
        lock (s_timerLock)
        {
            CompactCancelledTimers();
            if (s_timerHeap.Count == 0)
                return null;
            return s_timerHeap[0].DueTicks;
        }
    }

    static void CompactCancelledTimers()
    {
        while (s_timerHeap.Count > 0 && s_timerHeap[0].Cancelled)
            TimerPop();
    }

    static void TimerPush(PostedTimer timer)
    {
        s_timerHeap.Add(timer);
        int i = s_timerHeap.Count - 1;
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (s_timerHeap[parent].DueTicks <= timer.DueTicks)
                break;
            s_timerHeap[i] = s_timerHeap[parent];
            i = parent;
        }
        s_timerHeap[i] = timer;
    }

    static PostedTimer TimerPop()
    {
        int last = s_timerHeap.Count - 1;
        var min = s_timerHeap[0];
        var x = s_timerHeap[last];
        s_timerHeap.RemoveAt(last);
        if (last == 0)
            return min;

        int i = 0;
        int n = s_timerHeap.Count;
        while (true)
        {
            int left = i * 2 + 1;
            if (left >= n)
                break;
            int right = left + 1;
            int smallest = (right < n && s_timerHeap[right].DueTicks < s_timerHeap[left].DueTicks) ? right : left;
            if (x.DueTicks <= s_timerHeap[smallest].DueTicks)
                break;
            s_timerHeap[i] = s_timerHeap[smallest];
            i = smallest;
        }
        s_timerHeap[i] = x;
        return min;
    }

    sealed class PostedTimer : IDisposable
    {
        public long DueTicks;
        public TimeSpan Period;
        public Action Action = null!;
        public bool IsPeriodic;
        public bool Cancelled;

        public void Dispose()
        {
            lock (s_timerLock)
            {
                if (Cancelled)
                    return;
                Cancelled = true;
            }
            UiHost.Current.Wake();
        }
    }
}
