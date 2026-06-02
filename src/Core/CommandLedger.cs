using System;
using System.Collections.Generic;
using SkiaSharp;
using Blossom.Core.Visual;

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
/// Command to render backdrop blur for glassmorphism.
/// </summary>
public class DrawBackdropBlurCommand : DrawCommand
{
    private readonly float _blurSigma;
    private readonly float _rTopLeft;
    private readonly float _rTopRight;
    private readonly float _rBottomRight;
    private readonly float _rBottomLeft;
    private readonly VisualElement _element;

    public DrawBackdropBlurCommand(
        float blurSigma,
        float rTopLeft,
        float rTopRight,
        float rBottomRight,
        float rBottomLeft,
        VisualElement element)
    {
        _blurSigma = blurSigma;
        _rTopLeft = rTopLeft;
        _rTopRight = rTopRight;
        _rBottomRight = rBottomRight;
        _rBottomLeft = rBottomLeft;
        _element = element;
    }

    public override void Execute(SKCanvas canvas)
    {
        if (_blurSigma <= 0) return;

        // 1. Take a snapshot of the current offscreen surface pixels
        using var snapshot = Renderer.OffscreenSurface.Snapshot();
        if (snapshot == null) return;

        // 2. Create the local rounded rectangle path
        float w = _element.Transform.Computed.Width;
        float h = _element.Transform.Computed.Height;
        var rect = new SKRect(0, 0, w, h);
        using var localRoundRect = new SKRoundRect(rect);
        localRoundRect.SetRectRadii(rect, new SKPoint[] {
            new(_rTopLeft, _rTopLeft),
            new(_rTopRight, _rTopRight),
            new(_rBottomRight, _rBottomRight),
            new(_rBottomLeft, _rBottomLeft),
        });

        using var path = new SKPath();
        path.AddRoundRect(localRoundRect);

        // Transform the path to global screen space using the element's global matrix
        var globalMatrix = _element.Transform.GetGlobalM44().Matrix;
        path.Transform(globalMatrix);

        // 3. Draw the blurred backdrop image clipped to the transformed path
        using (new SKAutoCanvasRestore(canvas))
        {
            // Reset transform to draw in screen-space
            canvas.SetMatrix(SKMatrix.Identity);

            // Clip drawing to the element's actual shape
            canvas.ClipPath(path, SKClipOperation.Intersect, true);

            // Blur paint
            using var paint = new SKPaint
            {
                IsAntialias = true,
                ImageFilter = SKImageFilter.CreateBlur(_blurSigma, _blurSigma)
            };

            var globalBounds = path.Bounds;
            
            // Draw the snapshot portion onto the canvas
            canvas.DrawImage(snapshot, globalBounds, globalBounds, paint);
        }
    }
}

/// <summary>
/// Command to draw dynamic GPU fragment shader background (SKSL).
/// </summary>
public class DrawShaderBackgroundCommand : DrawCommand
{
    private readonly Blossom.Core.Visual.BackgroundShaderType _type;
    private readonly SKColor _baseColor;
    private readonly float _rTopLeft;
    private readonly float _rTopRight;
    private readonly float _rBottomRight;
    private readonly float _rBottomLeft;
    private readonly VisualElement _element;
    private readonly SKRoundRect _roundRect;

    public DrawShaderBackgroundCommand(
        Blossom.Core.Visual.BackgroundShaderType type,
        SKColor baseColor,
        float rTopLeft,
        float rTopRight,
        float rBottomRight,
        float rBottomLeft,
        VisualElement element)
    {
        _type = type;
        _baseColor = baseColor;
        _rTopLeft = rTopLeft;
        _rTopRight = rTopRight;
        _rBottomRight = rBottomRight;
        _rBottomLeft = rBottomLeft;
        _element = element;

        var rect = new SKRect(0, 0, _element.Transform.Computed.Width, _element.Transform.Computed.Height);
        _roundRect = new SKRoundRect(rect);
        _roundRect.SetRectRadii(rect, new SKPoint[] {
            new(_rTopLeft, _rTopLeft),
            new(_rTopRight, _rTopRight),
            new(_rBottomRight, _rBottomRight),
            new(_rBottomLeft, _rBottomLeft),
        });
    }

    public override void Execute(SKCanvas canvas)
    {
        float time = Blossom.Core.Visual.SKSLShaderTimeTracker.ElapsedSeconds;
        float hoverProgress = _element.HoverProgress;
        float w = _element.Transform.Computed.Width;
        float h = _element.Transform.Computed.Height;

        using var shader = Blossom.Core.Visual.SKSLShaderManager.CreateShader(_type, time, w, h, _baseColor, hoverProgress);
        if (shader == null) return;

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Shader = shader,
            PathEffect = _element.Style?.BackgroundPathEffect
        };

        canvas.DrawRoundRect(_roundRect, paint);
    }

    public override void Dispose()
    {
        _roundRect.Dispose();
    }
}

/// <summary>
/// Command to draw dynamic borders with vector animation effects (jitter, marching ants).
/// </summary>
public class DrawBorderCommand : DrawCommand
{
    private readonly Blossom.Core.Visual.BorderEffectType _effectType;
    private readonly float _width;
    private readonly float _speed;
    private readonly float _amount;
    private readonly SKColor _color;
    private readonly float _rTopLeft;
    private readonly float _rTopRight;
    private readonly float _rBottomRight;
    private readonly float _rBottomLeft;
    private readonly VisualElement _element;
    private readonly SKRoundRect _roundRect;

    public DrawBorderCommand(
        Blossom.Core.Visual.BorderEffectType effectType,
        float width,
        float speed,
        float amount,
        SKColor color,
        float rTopLeft,
        float rTopRight,
        float rBottomRight,
        float rBottomLeft,
        VisualElement element)
    {
        _effectType = effectType;
        _width = width;
        _speed = speed;
        _amount = amount;
        _color = color;
        _rTopLeft = rTopLeft;
        _rTopRight = rTopRight;
        _rBottomRight = rBottomRight;
        _rBottomLeft = rBottomLeft;
        _element = element;

        var rect = new SKRect(0, 0, _element.Transform.Computed.Width, _element.Transform.Computed.Height);
        rect.Inflate(_width / 2f, _width / 2f);

        _roundRect = new SKRoundRect(rect);
        _roundRect.SetRectRadii(rect, new SKPoint[] {
            new(_rTopLeft, _rTopLeft),
            new(_rTopRight, _rTopRight),
            new(_rBottomRight, _rBottomRight),
            new(_rBottomLeft, _rBottomLeft),
        });
    }

    public override void Execute(SKCanvas canvas)
    {
        float time = Blossom.Core.Visual.SKSLShaderTimeTracker.ElapsedSeconds;

        SKPathEffect? effect = null;
        if (_effectType == Blossom.Core.Visual.BorderEffectType.Jitter)
        {
            float seedVariation = (float)(Math.Sin(time * _speed * 10f) + 1.0) / 2.0f;
            float jitterAmount = _amount * (0.5f + 0.5f * seedVariation);
            effect = SKPathEffect.CreateDiscrete(8f, jitterAmount);
        }
        else if (_effectType == Blossom.Core.Visual.BorderEffectType.MarchingAnts)
        {
            float phase = -time * _speed * 50f;
            effect = SKPathEffect.CreateDash(new float[] { 15f, 10f }, phase);
        }

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = _width,
            Color = _color,
            PathEffect = effect
        };

        canvas.DrawRoundRect(_roundRect, paint);
        effect?.Dispose();
    }

    public override void Dispose()
    {
        _roundRect.Dispose();
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
