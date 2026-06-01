using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Blossom.Core;

/// <summary>
/// Represents a single recorded draw operation on a Skia canvas.
/// </summary>
public abstract class DrawCommand : IDisposable
{
    public abstract void Execute(SKCanvas canvas);
    public virtual void Dispose() {}
}

/// <summary>
/// Command to draw a rounded rectangle (used for element backgrounds, borders, and shadows).
/// </summary>
public class DrawRoundRectCommand : DrawCommand
{
    public SKRect Rect { get; }
    public float RoundnessTopLeft { get; }
    public float RoundnessTopRight { get; }
    public float RoundnessBottomRight { get; }
    public float RoundnessBottomLeft { get; }
    public SKPaint Paint { get; }
    public SKRoundRect RoundRect { get; }

    public DrawRoundRectCommand(
        SKRect rect,
        float rTopLeft,
        float rTopRight,
        float rBottomRight,
        float rBottomLeft,
        SKPaint paint)
    {
        Rect = rect;
        RoundnessTopLeft = rTopLeft;
        RoundnessTopRight = rTopRight;
        RoundnessBottomRight = rBottomRight;
        RoundnessBottomLeft = rBottomLeft;
        Paint = paint.Clone(); // Clone to prevent changes if the original is mutated or disposed

        RoundRect = new SKRoundRect(Rect);
        RoundRect.SetRectRadii(Rect, new SKPoint[] {
            new(RoundnessTopLeft, RoundnessTopLeft),
            new(RoundnessTopRight, RoundnessTopRight),
            new(RoundnessBottomRight, RoundnessBottomRight),
            new(RoundnessBottomLeft, RoundnessBottomLeft),
        });
    }

    public override void Execute(SKCanvas canvas)
    {
        canvas.DrawRoundRect(RoundRect, Paint);
    }

    public override void Dispose()
    {
        RoundRect.Dispose();
        Paint.Dispose();
    }
}

/// <summary>
/// Command to draw text.
/// </summary>
public class DrawTextCommand : DrawCommand
{
    public string Text { get; }
    public SKPoint Position { get; }
    public SKPaint Paint { get; }

    public DrawTextCommand(string text, SKPoint position, SKPaint paint)
    {
        Text = text;
        Position = position;
        Paint = paint.Clone();
    }

    public override void Execute(SKCanvas canvas)
    {
        canvas.DrawText(Text, Position, Paint);
    }

    public override void Dispose()
    {
        Paint.Dispose();
    }
}

/// <summary>
/// A transaction recorded in the ledger, representing a state transition for undo/redo (analogous to a git commit/revert).
/// </summary>
public class LedgerTransaction
{
    public string ElementId { get; }
    public List<DrawCommand> Before { get; }
    public List<DrawCommand> After { get; }

    public LedgerTransaction(string elementId, List<DrawCommand> before, List<DrawCommand> after)
    {
        ElementId = elementId;
        Before = before;
        After = after;
    }
}

/// <summary>
/// The central drawing ledger that manages element command logs, supports undo/redo transaction state,
/// and handles high-performance frame playback with cumulative clipping and transforms.
/// </summary>
public class CommandLedger
{
    private readonly Dictionary<string, List<DrawCommand>> _commands = new();
    private readonly Stack<LedgerTransaction> _undoStack = new();
    private readonly Stack<LedgerTransaction> _redoStack = new();

    public void Record(string elementId, List<DrawCommand> elementCommands)
    {
        if (_commands.TryGetValue(elementId, out var existing))
        {
            for (int i = 0; i < existing.Count; i++)
            {
                existing[i].Dispose();
            }
        }

        _commands[elementId] = elementCommands;
    }

    /// <summary>
    /// Reverts the last drawing change (analogous to git revert).
    /// </summary>
    public bool Undo()
    {
        if (_undoStack.Count == 0) return false;

        var tx = _undoStack.Pop();
        _commands[tx.ElementId] = tx.Before;
        _redoStack.Push(tx);
        return true;
    }

    /// <summary>
    /// Re-applies a reverted drawing change.
    /// </summary>
    public bool Redo()
    {
        if (_redoStack.Count == 0) return false;

        var tx = _redoStack.Pop();
        _commands[tx.ElementId] = tx.After;
        _undoStack.Push(tx);
        return true;
    }

    /// <summary>
    /// Clears the entire ledger.
    /// </summary>
    public void Clear()
    {
        foreach (var list in _commands.Values)
        {
            foreach (var cmd in list)
            {
                cmd.Dispose();
            }
        }
        _commands.Clear();
        
        // Also clean up stacks (they hold copies or transactions)
        while (_undoStack.Count > 0)
        {
            var tx = _undoStack.Pop();
            foreach (var cmd in tx.Before) cmd.Dispose();
            foreach (var cmd in tx.After) cmd.Dispose();
        }
        while (_redoStack.Count > 0)
        {
            var tx = _redoStack.Pop();
            foreach (var cmd in tx.Before) cmd.Dispose();
            foreach (var cmd in tx.After) cmd.Dispose();
        }

        _undoStack.Clear();
        _redoStack.Clear();
    }

    /// <summary>
    /// Gets the list of recorded draw commands for a given element.
    /// </summary>
    public List<DrawCommand> GetCommands(string elementId)
    {
        return _commands.TryGetValue(elementId, out var list) ? list : null;
    }
}
