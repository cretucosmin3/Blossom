using System;
using System.Collections.Generic;

namespace SnakeDemo;

public enum Direction { Up, Down, Left, Right }

public enum DeathCause { None, Wall, Self, BoardFull }

public readonly record struct Cell(int X, int Y)
{
    public Cell Step(Direction dir) => dir switch
    {
        Direction.Up => new Cell(X, Y - 1),
        Direction.Down => new Cell(X, Y + 1),
        Direction.Left => new Cell(X - 1, Y),
        _ => new Cell(X + 1, Y),
    };
}

/// <summary>
/// Engine-agnostic Snake rules on a fixed grid: movement, buffered turns, growth, and collisions.
/// Knows nothing about MonoGame or Blossom; <see cref="Game1"/> drives it and mirrors its state into
/// <see cref="GameState"/> signals for the UI.
/// </summary>
public sealed class SnakeGame
{
    public const int Columns = 28;
    public const int Rows = 20;
    public const int StartLength = 4;
    private const int MaxBufferedTurns = 2;

    private readonly LinkedList<Cell> _body = new();
    private readonly HashSet<Cell> _occupied = new();
    private readonly Queue<Direction> _turns = new();
    private readonly Random _rand = new();
    private Direction _heading;

    public IEnumerable<Cell> Body => _body;
    public int Length => _body.Count;
    public Cell Head => _body.First!.Value;
    public Cell Food { get; private set; }
    public DeathCause Death { get; private set; }
    public bool IsDead => Death != DeathCause.None;

    /// <summary>Raised when the snake eats; the argument is the new length.</summary>
    public event Action<int>? Ate;

    public void Reset()
    {
        _body.Clear();
        _occupied.Clear();
        _turns.Clear();
        _heading = Direction.Right;
        Death = DeathCause.None;

        var start = new Cell(Columns / 4, Rows / 2);
        for (int i = 0; i < StartLength; i++)
        {
            var c = new Cell(start.X - i, start.Y);
            _body.AddLast(c);
            _occupied.Add(c);
        }

        SpawnFood();
    }

    /// <summary>Queues a turn. Reversals and repeats of the last queued heading are ignored.</summary>
    public void Turn(Direction dir)
    {
        if (_turns.Count >= MaxBufferedTurns)
            return;

        Direction last = _turns.Count > 0 ? LastQueued() : _heading;
        if (dir == last || IsOpposite(dir, last))
            return;

        _turns.Enqueue(dir);
    }

    /// <summary>Advances the snake one cell.</summary>
    public void Step()
    {
        if (IsDead)
            return;

        if (_turns.Count > 0)
            _heading = _turns.Dequeue();

        var next = Head.Step(_heading);
        bool eating = next == Food;

        // The tail cell moves out of the way this step unless the snake is growing.
        var tail = _body.Last!.Value;
        bool hitsWall = next.X < 0 || next.Y < 0 || next.X >= Columns || next.Y >= Rows;
        bool hitsSelf = _occupied.Contains(next) && (eating || next != tail);
        if (hitsWall || hitsSelf)
        {
            Death = hitsWall ? DeathCause.Wall : DeathCause.Self;
            return;
        }

        if (!eating)
        {
            _body.RemoveLast();
            _occupied.Remove(tail);
        }

        _body.AddFirst(next);
        _occupied.Add(next);

        if (eating)
        {
            Ate?.Invoke(_body.Count);
            if (!SpawnFood())
                Death = DeathCause.BoardFull; // Nowhere left to go.
        }
    }

    private bool SpawnFood()
    {
        int free = Columns * Rows - _occupied.Count;
        if (free <= 0)
            return false;

        int pick = _rand.Next(free);
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                var c = new Cell(x, y);
                if (_occupied.Contains(c))
                    continue;
                if (pick-- == 0)
                {
                    Food = c;
                    return true;
                }
            }
        }

        return false;
    }

    private Direction LastQueued()
    {
        Direction last = _heading;
        foreach (var d in _turns)
            last = d;
        return last;
    }

    private static bool IsOpposite(Direction a, Direction b) =>
        (a, b) is (Direction.Up, Direction.Down) or (Direction.Down, Direction.Up)
            or (Direction.Left, Direction.Right) or (Direction.Right, Direction.Left);
}
