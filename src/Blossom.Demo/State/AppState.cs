using System;
using System.Collections.Generic;
using System.Linq;
using Blossom.Reactive;
using Blossom.Testing.Components;
using static Blossom.Reactive.ReactiveEngine;

namespace Blossom.Testing.State;

public enum Route
{
    Dashboard,
    Components,
    Tasks,
    Playground
}

public enum TaskStatus
{
    Backlog,
    Active,
    Done
}

public sealed record TaskItem(Guid Id, string Title, string Category, TaskStatus Status)
{
    public static TaskItem New(string title, string category, TaskStatus status = TaskStatus.Backlog)
        => new(Guid.NewGuid(), title, category, status);
}

public sealed record ActivityItem(Guid Id, string Who, string What, DateTime At);

public sealed record ToastItem(Guid Id, Tone Tone, string Title, string Message);

/// <summary>
/// App-wide store. Signals hold state; memos derive it. Pages read these inside
/// <c>Bind</c>/<c>For.Each</c>/<c>Show.When</c>, so only the nodes that depend on a value update.
/// Lists are immutable snapshots: writes assign a new list so subscribers are notified.
/// </summary>
public static class AppState
{
    public static readonly string[] Categories = { "Core", "Reactive", "UI", "Engine" };

    public static readonly Signal<Route> Route = CreateSignal(State.Route.Dashboard);
    public static readonly Signal<string> Search = CreateSignal("");
    public static readonly Signal<string> UserName = CreateSignal("Ada Lovelace");

    public static readonly Signal<IReadOnlyList<TaskItem>> Tasks = CreateSignal<IReadOnlyList<TaskItem>>(new[]
    {
        TaskItem.New("Pointer capture for drag handles", "Core", TaskStatus.Done),
        TaskItem.New("Fine-grained signals and memos", "Reactive", TaskStatus.Done),
        TaskItem.New("Keyed list reconciler", "Reactive", TaskStatus.Done),
        TaskItem.New("Subpixel LCD text", "Engine", TaskStatus.Done),
        TaskItem.New("Gallery shell and navigation", "UI", TaskStatus.Active),
        TaskItem.New("Dirty-rect scissor pipeline", "Engine", TaskStatus.Active),
        TaskItem.New("Theme recipes for every control", "UI", TaskStatus.Active),
        TaskItem.New("Virtualised list benchmark", "Core", TaskStatus.Backlog),
        TaskItem.New("Inverse hit-test for 3D layers", "Engine", TaskStatus.Backlog),
        TaskItem.New("Accessible colour tokens", "UI", TaskStatus.Backlog),
    });

    public static readonly Signal<IReadOnlyList<ActivityItem>> Activity = CreateSignal<IReadOnlyList<ActivityItem>>(new[]
    {
        new ActivityItem(Guid.NewGuid(), "Grace Hopper", "completed “Subpixel LCD text”", DateTime.Now.AddMinutes(-12)),
        new ActivityItem(Guid.NewGuid(), "Alan Turing", "moved “Dirty-rect scissor pipeline” to Active", DateTime.Now.AddMinutes(-47)),
        new ActivityItem(Guid.NewGuid(), "Katherine Johnson", "added “Accessible colour tokens”", DateTime.Now.AddHours(-3)),
        new ActivityItem(Guid.NewGuid(), "Linus Torvalds", "completed “Keyed list reconciler”", DateTime.Now.AddHours(-6)),
    });

    public static readonly Signal<IReadOnlyList<ToastItem>> Toasts = CreateSignal<IReadOnlyList<ToastItem>>(Array.Empty<ToastItem>());

    /// <summary>0 = week, 1 = month, 2 = year.</summary>
    public static readonly Signal<int> ChartRange = CreateSignal(0);

    public static readonly Memo<int> OpenCount = CreateMemo(() => Tasks.Value.Count(t => t.Status != TaskStatus.Done));
    public static readonly Memo<int> DoneCount = CreateMemo(() => Tasks.Value.Count(t => t.Status == TaskStatus.Done));
    public static readonly Memo<float> Completion = CreateMemo(() =>
        Tasks.Value.Count == 0 ? 0f : DoneCount.Value / (float)Tasks.Value.Count);

    public static readonly Memo<(float[] Values, string[] Labels)> Throughput = CreateMemo(() => ChartRange.Value switch
    {
        1 => (new float[] { 38, 52, 47, 61 }, new[] { "W1", "W2", "W3", "W4" }),
        2 => (new float[] { 120, 135, 128, 160, 172, 158, 190, 210, 198, 224, 240, 236 },
              new[] { "J", "F", "M", "A", "M", "J", "J", "A", "S", "O", "N", "D" }),
        _ => (new float[] { 8, 12, 9, 14, 11, 6, 4 }, new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }),
    });

    public static void Navigate(Route route) => Route.Value = route;

    public static void AddTask(string title, string category)
    {
        title = title.Trim();
        if (title.Length == 0) return;
        Batch(() =>
        {
            Tasks.Value = Tasks.Value.Prepend(TaskItem.New(title, category)).ToList();
            Log($"added “{title}”");
        });
    }

    public static void Move(TaskItem item, TaskStatus status)
    {
        Batch(() =>
        {
            Tasks.Value = Tasks.Value.Select(t => t.Id == item.Id ? t with { Status = status } : t).ToList();
            Log(status == TaskStatus.Done ? $"completed “{item.Title}”" : $"moved “{item.Title}” to {status}");
        });
    }

    public static void Remove(TaskItem item)
    {
        Batch(() =>
        {
            Tasks.Value = Tasks.Value.Where(t => t.Id != item.Id).ToList();
            Log($"deleted “{item.Title}”");
        });
    }

    private static void Log(string what)
    {
        var entry = new ActivityItem(Guid.NewGuid(), UserName.Peek(), what, DateTime.Now);
        Activity.Value = Activity.Value.Prepend(entry).Take(8).ToList();
    }

    /// <summary>Shows a toast and dismisses it after a few seconds.</summary>
    public static void Notify(string title, string message, Tone tone = Tone.Info)
    {
        var toast = new ToastItem(Guid.NewGuid(), tone, title, message);
        Toasts.Value = Toasts.Value.Append(toast).TakeLast(4).ToList();
        Blossom.Shell.PostDelayed(TimeSpan.FromSeconds(4), () => Dismiss(toast.Id));
    }

    public static void Dismiss(Guid id)
    {
        if (Toasts.Peek().Any(t => t.Id == id))
            Toasts.Value = Toasts.Value.Where(t => t.Id != id).ToList();
    }

    public static string Ago(DateTime at)
    {
        var d = DateTime.Now - at;
        if (d.TotalMinutes < 1) return "just now";
        if (d.TotalMinutes < 60) return $"{(int)d.TotalMinutes}m ago";
        if (d.TotalHours < 24) return $"{(int)d.TotalHours}h ago";
        return $"{(int)d.TotalDays}d ago";
    }
}
