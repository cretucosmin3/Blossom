using System.Collections.Generic;

namespace Blossom.Reactive;

/// <summary>
/// How far a derived node is from its sources. A write marks direct observers <see cref="Dirty"/> and
/// everything further downstream <see cref="Check"/>; a node in <see cref="Check"/> brings its sources up
/// to date (in read order) and only re-runs if one of them actually changed.
/// </summary>
internal enum NodeState : byte
{
    Clean,
    Check,
    Dirty
}

/// <summary>
/// Something an observer can read and subscribe to: a signal or a memo.
/// </summary>
internal interface ISource
{
    void AddObserver(IObserver observer);
    void RemoveObserver(IObserver observer);

    /// <summary>
    /// Brings a derived source up to date before its value is compared or read. No-op for plain signals.
    /// Never throws: a memo whose computation failed keeps the error and rethrows it on read.
    /// </summary>
    void UpdateIfNecessary();
}

/// <summary>
/// Something that tracks sources while it runs: an effect or a memo.
/// </summary>
internal interface IObserver
{
    void Track(ISource source);

    /// <summary>
    /// Raises this observer to at least <paramref name="state"/> because <paramref name="from"/> changed.
    /// Must not run user code: sources iterate their observers in place while marking.
    /// </summary>
    void MarkStale(NodeState state, ISource from);
}

/// <summary>
/// The sources an observer read, in read order. Each run is diffed against the previous one so
/// unchanged dependencies keep their subscription, and the buffers are reused so a steady-state
/// re-run allocates nothing.
/// </summary>
internal sealed class SourceList
{
    // Small dependency lists are scanned linearly; larger ones get a set for lookups.
    private const int HashThreshold = 16;

    private List<ISource> _current = new();
    private List<ISource> _next = new();
    private HashSet<ISource>? _currentSet;
    private HashSet<ISource>? _nextSet;

    public int Count => _current.Count;

    public ISource this[int index] => _current[index];

    /// <summary>Whether <paramref name="source"/> was already read during the run in progress.</summary>
    public bool IsTrackedThisRun(ISource source) => Contains(_next, _nextSet, source);

    public void Track(ISource source, IObserver observer)
    {
        if (Contains(_next, _nextSet, source)) return;

        _next.Add(source);
        if (_nextSet != null)
            _nextSet.Add(source);
        else if (_next.Count > HashThreshold)
            _nextSet = new HashSet<ISource>(_next);

        if (!Contains(_current, _currentSet, source))
            source.AddObserver(observer);
    }

    /// <summary>Ends a run: drops subscriptions that were not read again and keeps the new set.</summary>
    public void Commit(IObserver observer)
    {
        foreach (var source in _current)
        {
            if (!Contains(_next, _nextSet, source))
                source.RemoveObserver(observer);
        }

        (_current, _next) = (_next, _current);
        (_currentSet, _nextSet) = (_nextSet, _currentSet);
        _next.Clear();
        _nextSet?.Clear();
    }

    /// <summary>Unsubscribes from everything, including sources read by a run still in progress.</summary>
    public void Clear(IObserver observer)
    {
        foreach (var source in _current)
            source.RemoveObserver(observer);
        foreach (var source in _next)
            source.RemoveObserver(observer);

        _current.Clear();
        _next.Clear();
        _currentSet?.Clear();
        _nextSet?.Clear();
    }

    private static bool Contains(List<ISource> list, HashSet<ISource>? set, ISource source)
    {
        if (set != null) return set.Contains(source);
        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], source)) return true;
        }
        return false;
    }
}
