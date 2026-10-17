using System;
using System.Collections.Generic;
using System.Linq;
using Blossom.Reactive;
using static Blossom.Reactive.ReactiveEngine;

namespace ReactiveLab.State;

public enum Stage
{
    Todo,
    Doing,
    Review,
    Done
}

public enum Priority
{
    Low,
    Medium,
    High,
    Urgent
}

/// <summary>
/// One card. Every field is its own signal, so editing a card re-runs only that card's bindings, plus
/// whichever memos read that field right now (sorting by points reads every Points, an empty search
/// reads no Title at all).
/// </summary>
public sealed class Item
{
    public Item(int id, string title, int points, Priority priority, Stage stage)
    {
        Id = id;
        Title = CreateSignal(title);
        Points = CreateSignal(points);
        Priority = CreateSignal(priority);
        Stage = CreateSignal(stage);
    }

    public int Id { get; }
    public Signal<string> Title { get; }
    public Signal<int> Points { get; }
    public Signal<Priority> Priority { get; }
    public Signal<Stage> Stage { get; }
    public Signal<bool> Starred { get; } = CreateSignal(false);

    /// <summary>Per-item selection flag: changing the selection touches two cards, not every card.</summary>
    public Signal<bool> Selected { get; } = CreateSignal(false);
}

/// <summary>Lists are equal when they hold the same items in the same order, so an unchanged column does not reconcile.</summary>
public sealed class SameItems : IEqualityComparer<IReadOnlyList<Item>>
{
    public static readonly SameItems Instance = new();

    public bool Equals(IReadOnlyList<Item>? x, IReadOnlyList<Item>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null || x.Count != y.Count) return false;
        for (int i = 0; i < x.Count; i++)
        {
            if (!ReferenceEquals(x[i], y[i])) return false;
        }
        return true;
    }

    public int GetHashCode(IReadOnlyList<Item> obj) => obj.Count;
}

/// <summary>
/// Plain counters (not signals) bumped inside memos and bindings. <see cref="Ops"/> reports how much
/// each operation moved them, which shows what batching, memo cut-off and dynamic dependencies save.
/// </summary>
public static class Diag
{
    /// <summary>Re-runs of existing cards' bindings (building a card does not count).</summary>
    public static long CardUpdates;
    public static long VisibleRuns;
    public static long ColumnRuns;
    public static long StatsRuns;
}

public static class Board
{
    public static readonly string[] SortNames = { "List order", "Points", "Title", "Priority" };
    public static readonly string[] PriorityFilterNames = { "Any", "Med+", "High+", "Urgent" };

    private static readonly string[] Verbs =
    {
        "Refactor", "Profile", "Fix", "Document", "Ship", "Review", "Polish", "Test", "Measure", "Port", "Cache", "Split"
    };

    private static readonly string[] Nouns =
    {
        "layout pass", "hit testing", "text shaping", "scroll physics", "theme tokens", "keyed lists", "memo graph",
        "dirty rects", "font cache", "input routing", "overlay stack", "shader pipeline", "pointer capture", "animations"
    };

    private static readonly Random Rng = new(1234);
    private static int _nextId;

    public static readonly Signal<IReadOnlyList<Item>> Items = CreateSignal<IReadOnlyList<Item>>(Array.Empty<Item>());

    public static readonly Signal<string> Query = CreateSignal("");
    public static readonly Signal<int> Sort = CreateSignal(0);
    public static readonly Signal<bool> Descending = CreateSignal(false);
    public static readonly Signal<bool> StarredOnly = CreateSignal(false);
    public static readonly Signal<int> MinPriority = CreateSignal(0);
    public static readonly Signal<Item?> Selected = CreateSignal<Item?>(null);

    /// <summary>
    /// Filtered and sorted items. Dependencies are dynamic: titles are read only while a search is
    /// typed, stars only with "Starred only", points only when sorting by points.
    /// </summary>
    public static readonly Memo<IReadOnlyList<Item>> Visible = CreateMemo(() =>
    {
        Diag.VisibleRuns++;
        string q = Query.Value.Trim();
        bool starredOnly = StarredOnly.Value;
        int minPriority = MinPriority.Value;

        IEnumerable<Item> items = Items.Value;
        if (q.Length > 0)
            items = items.Where(i => i.Title.Value.Contains(q, StringComparison.OrdinalIgnoreCase));
        if (starredOnly)
            items = items.Where(i => i.Starred.Value);
        if (minPriority > 0)
            items = items.Where(i => (int)i.Priority.Value >= minPriority);

        bool desc = Descending.Value;
        items = Sort.Value switch
        {
            1 => desc ? items.OrderByDescending(i => i.Points.Value).ThenBy(i => i.Id) : items.OrderBy(i => i.Points.Value).ThenBy(i => i.Id),
            2 => desc ? items.OrderByDescending(i => i.Title.Value, StringComparer.OrdinalIgnoreCase) : items.OrderBy(i => i.Title.Value, StringComparer.OrdinalIgnoreCase),
            3 => desc ? items.OrderBy(i => i.Priority.Value).ThenBy(i => i.Id) : items.OrderByDescending(i => i.Priority.Value).ThenBy(i => i.Id),
            _ => desc ? items.Reverse() : items,
        };
        return items.ToList();
    }, SameItems.Instance);

    private static readonly Memo<IReadOnlyList<Item>>[] Columns = Enum.GetValues<Stage>()
        .Select(stage => CreateMemo(() =>
        {
            Diag.ColumnRuns++;
            return (IReadOnlyList<Item>)Visible.Value.Where(i => i.Stage.Value == stage).ToList();
        }, SameItems.Instance))
        .ToArray();

    public static Memo<IReadOnlyList<Item>> Column(Stage stage) => Columns[(int)stage];

    /// <summary>Points per column, derived from the column memo (memo of memo of memo).</summary>
    public static Memo<int> ColumnPoints(Stage stage) => ColumnPointMemos[(int)stage];

    private static readonly Memo<int>[] ColumnPointMemos = Enum.GetValues<Stage>()
        .Select(stage => CreateMemo(() => Columns[(int)stage].Value.Sum(i => i.Points.Value)))
        .ToArray();

    // Diamond: both read Items and every Points; Progress reads both and must never see them half-updated.
    public static readonly Memo<int> TotalPoints = CreateMemo(() =>
    {
        Diag.StatsRuns++;
        return Items.Value.Sum(i => i.Points.Value);
    });

    public static readonly Memo<int> DonePoints = CreateMemo(() =>
    {
        Diag.StatsRuns++;
        return Items.Value.Where(i => i.Stage.Value == Stage.Done).Sum(i => i.Points.Value);
    });

    public static readonly Memo<float> Progress = CreateMemo(() =>
    {
        Diag.StatsRuns++;
        int total = TotalPoints.Value;
        return total == 0 ? 0f : DonePoints.Value / (float)total;
    });

    public static readonly Memo<int> StarredCount = CreateMemo(() => Items.Value.Count(i => i.Starred.Value));

    public static readonly Memo<int> UrgentOpen = CreateMemo(() =>
        Items.Value.Count(i => i.Priority.Value == Priority.Urgent && i.Stage.Value != Stage.Done));

    // ── Mutations ──────────────────────────────────────────────────────────────────────────────

    public static Item NewItem()
    {
        int id = ++_nextId;
        string title = $"{Verbs[Rng.Next(Verbs.Length)]} {Nouns[Rng.Next(Nouns.Length)]}";
        var stage = (Stage)Rng.Next(4);
        var item = new Item(id, title, Fib[Rng.Next(Fib.Length)], (Priority)Rng.Next(4), stage);
        item.Starred.Value = Rng.Next(6) == 0;
        return item;
    }

    private static readonly int[] Fib = { 1, 2, 3, 5, 8, 13 };

    public static void Add(int count)
    {
        var added = new List<Item>(count);
        for (int i = 0; i < count; i++) added.Add(NewItem());
        Items.Value = Items.Value.Concat(added).ToList();
    }

    public static void Prepend(int count)
    {
        var added = new List<Item>(count);
        for (int i = 0; i < count; i++) added.Add(NewItem());
        Items.Value = added.Concat(Items.Value).ToList();
    }

    public static void Shuffle()
    {
        var list = Items.Value.ToArray();
        Rng.Shuffle(list);
        Items.Value = list;
    }

    public static void Reverse() => Items.Value = Items.Value.Reverse().ToList();

    public static void RemoveRandomHalf()
    {
        var keep = Items.Value.Where(_ => Rng.Next(2) == 0).ToList();
        DropSelectionIfMissing(keep);
        Items.Value = keep;
    }

    public static void RemoveDone()
    {
        var keep = Items.Value.Where(i => i.Stage.Peek() != Stage.Done).ToList();
        DropSelectionIfMissing(keep);
        Items.Value = keep;
    }

    public static void Clear()
    {
        Select(null);
        Items.Value = Array.Empty<Item>();
    }

    public static void Remove(Item item)
    {
        if (Selected.Peek() == item) Select(null);
        Items.Value = Items.Value.Where(i => i != item).ToList();
    }

    public static void Move(Item item, int delta)
    {
        int next = Math.Clamp((int)item.Stage.Peek() + delta, 0, 3);
        item.Stage.Value = (Stage)next;
    }

    public static void AddPoints(Item item, int delta) => item.Points.Value = Math.Clamp(item.Points.Peek() + delta, 0, 99);

    /// <summary>Random field edits: points, title, priority, star. Each is one signal write.</summary>
    public static void EditRandom(int count)
    {
        var items = Items.Peek();
        if (items.Count == 0) return;
        for (int n = 0; n < count; n++)
        {
            var item = items[Rng.Next(items.Count)];
            switch (Rng.Next(4))
            {
                case 0: AddPoints(item, Rng.Next(2) == 0 ? -1 : 1); break;
                case 1: item.Title.Value = $"{Verbs[Rng.Next(Verbs.Length)]} {Nouns[Rng.Next(Nouns.Length)]}"; break;
                case 2: item.Priority.Value = (Priority)Rng.Next(4); break;
                default: item.Starred.Value = !item.Starred.Peek(); break;
            }
        }
    }

    public static void MoveRandom(int count)
    {
        var items = Items.Peek();
        if (items.Count == 0) return;
        for (int n = 0; n < count; n++)
        {
            var item = items[Rng.Next(items.Count)];
            item.Stage.Value = (Stage)Rng.Next(4);
        }
    }

    public static void Select(Item? item)
    {
        var previous = Selected.Peek();
        if (previous == item) return;
        Batch(() =>
        {
            if (previous != null) previous.Selected.Value = false;
            if (item != null) item.Selected.Value = true;
            Selected.Value = item;
        });
    }

    private static void DropSelectionIfMissing(List<Item> keep)
    {
        var selected = Selected.Peek();
        if (selected != null && !keep.Contains(selected)) Select(null);
    }
}
