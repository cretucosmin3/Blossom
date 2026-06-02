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
        Paint = paint; // Take ownership to avoid double allocation and prevent leaks

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
/// Defines how a background image scales to fit its target element.
/// </summary>
public enum ImageScaleMode
{
    Stretch,
    Contain,
    Cover
}

/// <summary>
/// Command to draw a background image with scaling and optional rounded corner clipping.
/// </summary>
public class DrawImageCommand : DrawCommand
{
    private readonly SKBitmap _bitmap;
    private readonly SKRect _drawRect;
    private readonly SKRoundRect? _clipRoundRect;
    private readonly SKPaint _paint;

    public DrawImageCommand(
        SKBitmap bitmap,
        SKRect dest,
        ImageScaleMode scaleMode,
        float rTopLeft,
        float rTopRight,
        float rBottomRight,
        float rBottomLeft,
        float blurSigma,
        float grayscaleAmount,
        SKColor tintColor,
        SKBlendMode tintBlendMode)
    {
        _bitmap = bitmap;

        float bw = bitmap.Width;
        float bh = bitmap.Height;
        if (scaleMode == ImageScaleMode.Contain && bw > 0 && bh > 0)
        {
            float scale = Math.Min(dest.Width / bw, dest.Height / bh);
            float drawW = bw * scale;
            float drawH = bh * scale;
            float drawX = dest.Left + (dest.Width - drawW) / 2f;
            float drawY = dest.Top + (dest.Height - drawH) / 2f;
            _drawRect = new SKRect(drawX, drawY, drawX + drawW, drawY + drawH);
        }
        else if (scaleMode == ImageScaleMode.Cover && bw > 0 && bh > 0)
        {
            float scale = Math.Max(dest.Width / bw, dest.Height / bh);
            float drawW = bw * scale;
            float drawH = bh * scale;
            float drawX = dest.Left + (dest.Width - drawW) / 2f;
            float drawY = dest.Top + (dest.Height - drawH) / 2f;
            _drawRect = new SKRect(drawX, drawY, drawX + drawW, drawY + drawH);
        }
        else
        {
            _drawRect = dest;
        }

        if (rTopLeft > 0 || rTopRight > 0 || rBottomRight > 0 || rBottomLeft > 0)
        {
            _clipRoundRect = new SKRoundRect(dest);
            _clipRoundRect.SetRectRadii(dest, new SKPoint[] {
                new(rTopLeft, rTopLeft),
                new(rTopRight, rTopRight),
                new(rBottomRight, rBottomRight),
                new(rBottomLeft, rBottomLeft),
            });
        }

        _paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium };

        if (blurSigma > 0)
        {
            _paint.ImageFilter = SKImageFilter.CreateBlur(blurSigma, blurSigma);
        }

        if (grayscaleAmount > 0 || tintColor.Alpha > 0)
        {
            SKColorFilter? filter = null;
            if (grayscaleAmount > 0)
            {
                float g = Math.Clamp(grayscaleAmount, 0f, 1f);
                float invG = 1f - g;
                float[] colorMatrix = new float[] {
                    invG + g * 0.2126f, g * 0.7152f,       g * 0.0722f,       0, 0,
                    g * 0.2126f,       invG + g * 0.7152f, g * 0.0722f,       0, 0,
                    g * 0.2126f,       g * 0.7152f,       invG + g * 0.0722f, 0, 0,
                    0,                 0,                 0,                 1, 0
                };
                filter = SKColorFilter.CreateColorMatrix(colorMatrix);
            }

            if (tintColor.Alpha > 0)
            {
                var tintFilter = SKColorFilter.CreateBlendMode(tintColor, tintBlendMode);
                if (filter != null)
                {
                    var chained = SKColorFilter.CreateCompose(tintFilter, filter);
                    filter.Dispose();
                    tintFilter.Dispose();
                    filter = chained;
                }
                else
                {
                    filter = tintFilter;
                }
            }

            _paint.ColorFilter = filter;
        }
    }

    public override void Execute(SKCanvas canvas)
    {
        if (_bitmap == null || _bitmap.Width <= 0 || _bitmap.Height <= 0) return;

        using (new SKAutoCanvasRestore(canvas))
        {
            if (_clipRoundRect != null)
            {
                canvas.ClipRoundRect(_clipRoundRect, SKClipOperation.Intersect, true);
            }

            canvas.DrawBitmap(_bitmap, _drawRect, _paint);
        }
    }

    public override void Dispose()
    {
        _clipRoundRect?.Dispose();
        _paint.ImageFilter?.Dispose();
        _paint.ColorFilter?.Dispose();
        _paint.Dispose();
    }
}

/// <summary>
/// Command to draw a background SVG picture with scaling, optional clipping, and filters.
/// </summary>
public class DrawSvgCommand : DrawCommand
{
    private readonly SkiaSharp.Extended.Svg.SKSvg _svg;
    private readonly SKRoundRect? _clipRoundRect;
    private readonly SKPaint _paint;
    private readonly SKMatrix _transformMatrix;

    public DrawSvgCommand(
        SkiaSharp.Extended.Svg.SKSvg svg,
        SKRect dest,
        ImageScaleMode scaleMode,
        float rTopLeft,
        float rTopRight,
        float rBottomRight,
        float rBottomLeft,
        float blurSigma,
        float grayscaleAmount,
        SKColor tintColor,
        SKBlendMode tintBlendMode)
    {
        _svg = svg;

        float sw = svg.CanvasSize.Width;
        float sh = svg.CanvasSize.Height;
        SKRect drawRect;

        if (scaleMode == ImageScaleMode.Contain && sw > 0 && sh > 0)
        {
            float scale = Math.Min(dest.Width / sw, dest.Height / sh);
            float drawW = sw * scale;
            float drawH = sh * scale;
            float drawX = dest.Left + (dest.Width - drawW) / 2f;
            float drawY = dest.Top + (dest.Height - drawH) / 2f;
            drawRect = new SKRect(drawX, drawY, drawX + drawW, drawY + drawH);
        }
        else if (scaleMode == ImageScaleMode.Cover && sw > 0 && sh > 0)
        {
            float scale = Math.Max(dest.Width / sw, dest.Height / sh);
            float drawW = sw * scale;
            float drawH = sh * scale;
            float drawX = dest.Left + (dest.Width - drawW) / 2f;
            float drawY = dest.Top + (dest.Height - drawH) / 2f;
            drawRect = new SKRect(drawX, drawY, drawX + drawW, drawY + drawH);
        }
        else
        {
            drawRect = dest;
        }

        _transformMatrix = SKMatrix.CreateTranslation(drawRect.Left, drawRect.Top);
        if (sw > 0 && sh > 0)
        {
            SKMatrix.PreConcat(ref _transformMatrix, SKMatrix.CreateScale(drawRect.Width / sw, drawRect.Height / sh));
        }

        if (rTopLeft > 0 || rTopRight > 0 || rBottomRight > 0 || rBottomLeft > 0)
        {
            _clipRoundRect = new SKRoundRect(dest);
            _clipRoundRect.SetRectRadii(dest, new SKPoint[] {
                new(rTopLeft, rTopLeft),
                new(rTopRight, rTopRight),
                new(rBottomRight, rBottomRight),
                new(rBottomLeft, rBottomLeft),
            });
        }

        _paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium };

        if (blurSigma > 0)
        {
            _paint.ImageFilter = SKImageFilter.CreateBlur(blurSigma, blurSigma);
        }

        if (grayscaleAmount > 0 || tintColor.Alpha > 0)
        {
            SKColorFilter? filter = null;
            if (grayscaleAmount > 0)
            {
                float g = Math.Clamp(grayscaleAmount, 0f, 1f);
                float invG = 1f - g;
                float[] colorMatrix = new float[] {
                    invG + g * 0.2126f, g * 0.7152f,       g * 0.0722f,       0, 0,
                    g * 0.2126f,       invG + g * 0.7152f, g * 0.0722f,       0, 0,
                    g * 0.2126f,       g * 0.7152f,       invG + g * 0.0722f, 0, 0,
                    0,                 0,                 0,                 1, 0
                };
                filter = SKColorFilter.CreateColorMatrix(colorMatrix);
            }

            if (tintColor.Alpha > 0)
            {
                var tintFilter = SKColorFilter.CreateBlendMode(tintColor, tintBlendMode);
                if (filter != null)
                {
                    var chained = SKColorFilter.CreateCompose(tintFilter, filter);
                    filter.Dispose();
                    tintFilter.Dispose();
                    filter = chained;
                }
                else
                {
                    filter = tintFilter;
                }
            }

            _paint.ColorFilter = filter;
        }
    }

    public override void Execute(SKCanvas canvas)
    {
        if (_svg == null || _svg.Picture == null || _svg.CanvasSize.Width <= 0 || _svg.CanvasSize.Height <= 0) return;

        using (new SKAutoCanvasRestore(canvas))
        {
            if (_clipRoundRect != null)
            {
                canvas.ClipRoundRect(_clipRoundRect, SKClipOperation.Intersect, true);
            }

            canvas.SaveLayer(_paint);

            var mat = _transformMatrix;
            canvas.Concat(ref mat);

            canvas.DrawPicture(_svg.Picture);

            canvas.Restore();
        }
    }

    public override void Dispose()
    {
        _clipRoundRect?.Dispose();
        _paint.ImageFilter?.Dispose();
        _paint.ColorFilter?.Dispose();
        _paint.Dispose();
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
