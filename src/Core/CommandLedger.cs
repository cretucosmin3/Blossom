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
    public SKBitmap Bitmap { get; }
    public SKRect Dest { get; }
    public ImageScaleMode ScaleMode { get; }
    public float RoundnessTopLeft { get; }
    public float RoundnessTopRight { get; }
    public float RoundnessBottomRight { get; }
    public float RoundnessBottomLeft { get; }
    public float BlurSigma { get; }
    public float GrayscaleAmount { get; }
    public SKColor TintColor { get; }
    public SKBlendMode TintBlendMode { get; }

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
        Bitmap = bitmap;
        Dest = dest;
        ScaleMode = scaleMode;
        RoundnessTopLeft = rTopLeft;
        RoundnessTopRight = rTopRight;
        RoundnessBottomRight = rBottomRight;
        RoundnessBottomLeft = rBottomLeft;
        BlurSigma = blurSigma;
        GrayscaleAmount = grayscaleAmount;
        TintColor = tintColor;
        TintBlendMode = tintBlendMode;
    }

    public override void Execute(SKCanvas canvas)
    {
        if (Bitmap == null || Bitmap.Width <= 0 || Bitmap.Height <= 0) return;

        float bw = Bitmap.Width;
        float bh = Bitmap.Height;
        SKRect drawRect;

        if (ScaleMode == ImageScaleMode.Contain)
        {
            float scale = Math.Min(Dest.Width / bw, Dest.Height / bh);
            float drawW = bw * scale;
            float drawH = bh * scale;
            float drawX = Dest.Left + (Dest.Width - drawW) / 2f;
            float drawY = Dest.Top + (Dest.Height - drawH) / 2f;
            drawRect = new SKRect(drawX, drawY, drawX + drawW, drawY + drawH);
        }
        else if (ScaleMode == ImageScaleMode.Cover)
        {
            float scale = Math.Max(Dest.Width / bw, Dest.Height / bh);
            float drawW = bw * scale;
            float drawH = bh * scale;
            float drawX = Dest.Left + (Dest.Width - drawW) / 2f;
            float drawY = Dest.Top + (Dest.Height - drawH) / 2f;
            drawRect = new SKRect(drawX, drawY, drawX + drawW, drawY + drawH);
        }
        else
        {
            drawRect = Dest;
        }

        using (new SKAutoCanvasRestore(canvas))
        {
            if (RoundnessTopLeft > 0 || RoundnessTopRight > 0 || RoundnessBottomRight > 0 || RoundnessBottomLeft > 0)
            {
                using var roundRect = new SKRoundRect(Dest);
                roundRect.SetRectRadii(Dest, new SKPoint[] {
                    new(RoundnessTopLeft, RoundnessTopLeft),
                    new(RoundnessTopRight, RoundnessTopRight),
                    new(RoundnessBottomRight, RoundnessBottomRight),
                    new(RoundnessBottomLeft, RoundnessBottomLeft),
                });
                canvas.ClipRoundRect(roundRect, SKClipOperation.Intersect, true);
            }

            using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium };

            if (BlurSigma > 0)
            {
                paint.ImageFilter = SKImageFilter.CreateBlur(BlurSigma, BlurSigma);
            }

            if (GrayscaleAmount > 0 || TintColor.Alpha > 0)
            {
                SKColorFilter? filter = null;
                if (GrayscaleAmount > 0)
                {
                    float g = Math.Clamp(GrayscaleAmount, 0f, 1f);
                    float invG = 1f - g;
                    float[] matrix = new float[] {
                        invG + g * 0.2126f, g * 0.7152f,       g * 0.0722f,       0, 0,
                        g * 0.2126f,       invG + g * 0.7152f, g * 0.0722f,       0, 0,
                        g * 0.2126f,       g * 0.7152f,       invG + g * 0.0722f, 0, 0,
                        0,                 0,                 0,                 1, 0
                    };
                    filter = SKColorFilter.CreateColorMatrix(matrix);
                }

                if (TintColor.Alpha > 0)
                {
                    var tintFilter = SKColorFilter.CreateBlendMode(TintColor, TintBlendMode);
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

                paint.ColorFilter = filter;
            }

            canvas.DrawBitmap(Bitmap, drawRect, paint);

            paint.ImageFilter?.Dispose();
            paint.ColorFilter?.Dispose();
        }
    }
}

/// <summary>
/// Command to draw a background SVG picture with scaling, optional clipping, and filters.
/// </summary>
public class DrawSvgCommand : DrawCommand
{
    public SkiaSharp.Extended.Svg.SKSvg Svg { get; }
    public SKRect Dest { get; }
    public ImageScaleMode ScaleMode { get; }
    public float RoundnessTopLeft { get; }
    public float RoundnessTopRight { get; }
    public float RoundnessBottomRight { get; }
    public float RoundnessBottomLeft { get; }
    public float BlurSigma { get; }
    public float GrayscaleAmount { get; }
    public SKColor TintColor { get; }
    public SKBlendMode TintBlendMode { get; }

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
        Svg = svg;
        Dest = dest;
        ScaleMode = scaleMode;
        RoundnessTopLeft = rTopLeft;
        RoundnessTopRight = rTopRight;
        RoundnessBottomRight = rBottomRight;
        RoundnessBottomLeft = rBottomLeft;
        BlurSigma = blurSigma;
        GrayscaleAmount = grayscaleAmount;
        TintColor = tintColor;
        TintBlendMode = tintBlendMode;
    }

    public override void Execute(SKCanvas canvas)
    {
        if (Svg == null || Svg.Picture == null || Svg.CanvasSize.Width <= 0 || Svg.CanvasSize.Height <= 0) return;

        float sw = Svg.CanvasSize.Width;
        float sh = Svg.CanvasSize.Height;
        SKRect drawRect;

        if (ScaleMode == ImageScaleMode.Contain)
        {
            float scale = Math.Min(Dest.Width / sw, Dest.Height / sh);
            float drawW = sw * scale;
            float drawH = sh * scale;
            float drawX = Dest.Left + (Dest.Width - drawW) / 2f;
            float drawY = Dest.Top + (Dest.Height - drawH) / 2f;
            drawRect = new SKRect(drawX, drawY, drawX + drawW, drawY + drawH);
        }
        else if (ScaleMode == ImageScaleMode.Cover)
        {
            float scale = Math.Max(Dest.Width / sw, Dest.Height / sh);
            float drawW = sw * scale;
            float drawH = sh * scale;
            float drawX = Dest.Left + (Dest.Width - drawW) / 2f;
            float drawY = Dest.Top + (Dest.Height - drawH) / 2f;
            drawRect = new SKRect(drawX, drawY, drawX + drawW, drawY + drawH);
        }
        else
        {
            drawRect = Dest;
        }

        using (new SKAutoCanvasRestore(canvas))
        {
            if (RoundnessTopLeft > 0 || RoundnessTopRight > 0 || RoundnessBottomRight > 0 || RoundnessBottomLeft > 0)
            {
                using var roundRect = new SKRoundRect(Dest);
                roundRect.SetRectRadii(Dest, new SKPoint[] {
                    new(RoundnessTopLeft, RoundnessTopLeft),
                    new(RoundnessTopRight, RoundnessTopRight),
                    new(RoundnessBottomRight, RoundnessBottomRight),
                    new(RoundnessBottomLeft, RoundnessBottomLeft),
                });
                canvas.ClipRoundRect(roundRect, SKClipOperation.Intersect, true);
            }

            using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.Medium };

            if (BlurSigma > 0)
            {
                paint.ImageFilter = SKImageFilter.CreateBlur(BlurSigma, BlurSigma);
            }

            if (GrayscaleAmount > 0 || TintColor.Alpha > 0)
            {
                SKColorFilter? filter = null;
                if (GrayscaleAmount > 0)
                {
                    float g = Math.Clamp(GrayscaleAmount, 0f, 1f);
                    float invG = 1f - g;
                    float[] colorMatrix = new float[] {
                        invG + g * 0.2126f, g * 0.7152f,       g * 0.0722f,       0, 0,
                        g * 0.2126f,       invG + g * 0.7152f, g * 0.0722f,       0, 0,
                        g * 0.2126f,       g * 0.7152f,       invG + g * 0.0722f, 0, 0,
                        0,                 0,                 0,                 1, 0
                    };
                    filter = SKColorFilter.CreateColorMatrix(colorMatrix);
                }

                if (TintColor.Alpha > 0)
                {
                    var tintFilter = SKColorFilter.CreateBlendMode(TintColor, TintBlendMode);
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

                paint.ColorFilter = filter;
            }

            // Save layer to apply paint filters (blur, grayscale, tint) correctly to vector commands
            canvas.SaveLayer(paint);

            // Translate and scale SVG drawing matrix to target drawRect boundaries
            var matrix = SKMatrix.CreateTranslation(drawRect.Left, drawRect.Top);
            SKMatrix.PreConcat(ref matrix, SKMatrix.CreateScale(drawRect.Width / sw, drawRect.Height / sh));
            canvas.Concat(ref matrix);

            canvas.DrawPicture(Svg.Picture);

            canvas.Restore();

            paint.ImageFilter?.Dispose();
            paint.ColorFilter?.Dispose();
        }
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
