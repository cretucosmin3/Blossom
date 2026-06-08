using System;
using System.Collections.Generic;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using SkiaSharp;

namespace Blossom.Testing.Components
{
    public class DrawingCanvas : VisualElement
    {
        private SKBitmap? _cachedGridBitmap;
        private SKCanvas? _cachedBitmapCanvas;
        private SKBitmap? _strokeStartBitmap;
        
        private bool _isDrawing = false;
        private bool _hasDrawn = false;
        private SKPoint _lastMousePos;
        private DateTime _lastDrawTime;
        private float _lastBrushWidth;
        private SKColor _activeStrokeColor;
        private bool _isMouseHovering = false;
        private SKPoint _hoverMousePos;

        public SKColor DrawColor { get; set; } = SKColors.Black;
        public int BrushRadius { get; set; } = 0; // Deprecated
        public float BrushSize { get; set; } = 12f;
        public bool UseVelocityDynamics { get; set; } = true;
        public float PaintOpacity { get; set; } = 1.0f;
        public bool IsEraserMode { get; set; } = false;
        public bool IsBrushMode { get; set; } = false;
        private float _paintRemaining = 1.0f;

        public DrawingCanvas(float width, float height)
        {
            Name = $"DrawingCanvas_{Guid.NewGuid().ToString().Substring(0, 4)}";
            Transform = new Transform(0, 0, width, height);

            // Create a high-res backing bitmap (target width of 2048 for high-fidelity crisp lines)
            float resolutionScale = 2048f / Math.Max(1f, width);
            int w = (int)Math.Max(1, width * resolutionScale);
            int h = (int)Math.Max(1, height * resolutionScale);
            _cachedGridBitmap = new SKBitmap(w, h);
            _cachedBitmapCanvas = new SKCanvas(_cachedGridBitmap);

            // Fill with default canvas background (transparent for shader compatibility)
            _cachedBitmapCanvas.Clear(SKColors.Transparent);

            // Bind the bitmap resource to be used by background shaders
            GetShaderBitmapResource = () => _cachedGridBitmap;

            // Bind input events
            Events.OnMouseDown += (s, e) =>
            {
                if (ParentView == null) return;
                if (!ParentView.Events.IsMouseDown(0)) return; // Only trigger drawing on left click

                _isDrawing = true;
                _hasDrawn = false;
                _paintRemaining = 1.0f;

                // Scale input coordinate relative to element computed scale to keep alignment when window is resized
                float scaleX = (float)_cachedGridBitmap.Width / Math.Max(1f, Transform.Computed.Width);
                float scaleY = (float)_cachedGridBitmap.Height / Math.Max(1f, Transform.Computed.Height);
                _lastMousePos = new SKPoint(e.Relative.X * scaleX, e.Relative.Y * scaleY);
                _lastDrawTime = DateTime.Now;
                _activeStrokeColor = DrawColor;

                // Take a snapshot of the current canvas bitmap to sample from during this stroke
                // to avoid feedback loops where the brush mixes with its own newly painted color.
                if (_cachedGridBitmap != null)
                {
                    _strokeStartBitmap?.Dispose();
                    _strokeStartBitmap = new SKBitmap(_cachedGridBitmap.Width, _cachedGridBitmap.Height);
                    using var canvas = new SKCanvas(_strokeStartBitmap);
                    canvas.DrawBitmap(_cachedGridBitmap, 0, 0);
                }
            };

            Events.OnMouseUp += (s, e) =>
            {
                if (_isDrawing)
                {
                    if (!_hasDrawn)
                    {
                        // Draw a single starting dot at the click position since no movement occurred
                        float baseWidth = BrushSize;
                        var finalColor = IsEraserMode ? SKColors.Transparent : DrawColor.WithAlpha((byte)(DrawColor.Alpha * PaintOpacity));
                        DrawPaintDot(_lastMousePos, baseWidth, finalColor);
                    }
                }
                _isDrawing = false;
                _strokeStartBitmap?.Dispose();
                _strokeStartBitmap = null;
            };

            Events.OnMouseMove += (s, e) =>
            {
                _hoverMousePos = new SKPoint(e.Relative.X, e.Relative.Y);

                if (!_isDrawing)
                {
                    InvalidateCanvas();
                    return;
                }
                if (ParentView == null || !ParentView.Events.IsMouseDown(0))
                {
                    if (!_hasDrawn)
                    {
                        float baseWidth = BrushSize;
                        var finalColor = IsEraserMode ? SKColors.Transparent : DrawColor.WithAlpha((byte)(DrawColor.Alpha * PaintOpacity));
                        DrawPaintDot(_lastMousePos, baseWidth, finalColor);
                    }
                    _isDrawing = false;
                    _strokeStartBitmap?.Dispose();
                    _strokeStartBitmap = null;
                    InvalidateCanvas();
                    return;
                }

                // Scale input coordinate relative to element computed scale to keep alignment when window is resized
                float scaleX = (float)_cachedGridBitmap.Width / Math.Max(1f, Transform.Computed.Width);
                float scaleY = (float)_cachedGridBitmap.Height / Math.Max(1f, Transform.Computed.Height);
                var currentPos = new SKPoint(e.Relative.X * scaleX, e.Relative.Y * scaleY);
                
                // Only update _lastMousePos if we actually drew a stroke segment (exceeded jitter threshold)
                if (DrawPaintStroke(_lastMousePos, currentPos))
                {
                    _lastMousePos = currentPos;
                }
            };

            Events.OnMouseEnter += (s) =>
            {
                _isMouseHovering = true;
                try { Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Crosshair); } catch {}
                InvalidateCanvas();
            };

            Events.OnMouseLeave += (s) =>
            {
                _isMouseHovering = false;
                try { Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Arrow); } catch {}
                InvalidateCanvas();
            };

            OnDisposing += (el) =>
            {
                if (_isMouseHovering)
                {
                    try { Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Arrow); } catch {}
                }
                _cachedBitmapCanvas?.Dispose();
                _cachedGridBitmap?.Dispose();
                _strokeStartBitmap?.Dispose();
            };
        }

        public void RecreateBackingBitmap(float width, float height)
        {
            _cachedBitmapCanvas?.Dispose();
            _cachedGridBitmap?.Dispose();

            float resolutionScale = 2048f / Math.Max(1f, width);
            int w = (int)Math.Max(1, width * resolutionScale);
            int h = (int)Math.Max(1, height * resolutionScale);
            _cachedGridBitmap = new SKBitmap(w, h);
            _cachedBitmapCanvas = new SKCanvas(_cachedGridBitmap);
            _cachedBitmapCanvas.Clear(SKColors.Transparent);

            GetShaderBitmapResource = () => _cachedGridBitmap;
            InvalidateCanvas();
        }

        private SKColor BlendColors(SKColor brushColor, SKColor canvasColor, float canvasWeight)
        {
            if (canvasColor.Alpha == 0) return brushColor;
            
            // Subtractive CMY mixing is much more realistic than simple RGB lerp
            float c1 = 1f - (brushColor.Red / 255f);
            float m1 = 1f - (brushColor.Green / 255f);
            float y1 = 1f - (brushColor.Blue / 255f);

            float c2 = 1f - (canvasColor.Red / 255f);
            float m2 = 1f - (canvasColor.Green / 255f);
            float y2 = 1f - (canvasColor.Blue / 255f);

            float c_mix = c1 + (c2 - c1) * canvasWeight;
            float m_mix = m1 + (m2 - m1) * canvasWeight;
            float y_mix = y1 + (y2 - y1) * canvasWeight;

            byte r = (byte)Math.Clamp((1f - c_mix) * 255f, 0, 255);
            byte g = (byte)Math.Clamp((1f - m_mix) * 255f, 0, 255);
            byte b = (byte)Math.Clamp((1f - y_mix) * 255f, 0, 255);
            
            byte a = (byte)Math.Clamp(brushColor.Alpha + (canvasColor.Alpha - brushColor.Alpha) * canvasWeight, 0, 255);

            return new SKColor(r, g, b, a);
        }

        private void DrawPaintDot(SKPoint pos, float width, SKColor color)
        {
            if (_cachedBitmapCanvas == null) return;

            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                BlendMode = IsEraserMode ? SKBlendMode.Clear : SKBlendMode.SrcOver
            };
            _cachedBitmapCanvas.DrawCircle(pos.X, pos.Y, width / 2f, paint);
            InvalidateCanvas();
        }

        private bool DrawPaintStroke(SKPoint from, SKPoint to)
        {
            if (_cachedBitmapCanvas == null) return false;

            float distance = SKPoint.Distance(from, to);
            
            // Ignore tiny initial movements to prevent mouse jitter from starting the stroke as a slow blob
            if (!_hasDrawn && distance < 1.5f)
            {
                return false;
            }

            if (IsBrushMode)
            {
                _paintRemaining = Math.Max(0f, _paintRemaining - (distance / 1200f));
            }
            else
            {
                _activeStrokeColor = DrawColor;
            }

            var now = DateTime.Now;
            double elapsedSeconds = (now - _lastDrawTime).TotalSeconds;
            _lastDrawTime = now;

            // Limit elapsed seconds to a maximum of 16ms (60Hz) to represent standard frame-rate polling
            // and prevent startup pauses or lag spikes from inflating the time delta, which causes slow velocity calculations.
            double effectiveElapsed = Math.Min(elapsedSeconds, 0.016);
            
            // Default velocity to 0 if no movement or time is too small
            float velocity = 0f;
            if (distance > 0 && effectiveElapsed > 0.0001)
            {
                velocity = distance / (float)effectiveElapsed;
            }

            float baseWidth = BrushSize;
            
            // Dynamic brush width based on mouse velocity:
            // Fast movement -> scale down to 40% of base width
            // Slow movement -> scale up to 115% of base width
            float targetWidth = UseVelocityDynamics
                ? baseWidth * Math.Clamp(1.5f - (velocity * 0.0007f), 0.4f, 1.15f)
                : baseWidth;
            
            // Smooth transition between brush widths to prevent sudden jumps/blobs.
            float strokeWidth;
            if (!_hasDrawn)
            {
                strokeWidth = targetWidth;
                _hasDrawn = true;
            }
            else
            {
                strokeWidth = UseVelocityDynamics
                    ? _lastBrushWidth + (targetWidth - _lastBrushWidth) * 0.2f
                    : targetWidth;
            }
            _lastBrushWidth = strokeWidth;

            SKColor startColor = _activeStrokeColor;
            SKColor colorAtFrom = SKColors.Transparent;
            SKColor colorAtTo = SKColors.Transparent;

            // Sample from the snapshot of the canvas taken at the start of the stroke
            // to avoid feedback loops where the brush mixes with its own newly painted color.
            var sampleSource = _strokeStartBitmap ?? _cachedGridBitmap;
            if (sampleSource != null)
            {
                int fx = (int)Math.Clamp(from.X, 0, sampleSource.Width - 1);
                int fy = (int)Math.Clamp(from.Y, 0, sampleSource.Height - 1);
                colorAtFrom = sampleSource.GetPixel(fx, fy);

                int tx = (int)Math.Clamp(to.X, 0, sampleSource.Width - 1);
                int ty = (int)Math.Clamp(to.Y, 0, sampleSource.Height - 1);
                colorAtTo = sampleSource.GetPixel(tx, ty);
            }

            // Smear/Oil mixing simulation: Brush absorbs paint colors already on the canvas along the path
            if (!IsEraserMode && colorAtTo.Alpha > 10)
            {
                float mixingRate = ShaderMixingRate;
                if (mixingRate > 0.01f)
                {
                    // Subtractive mixing in CMY space
                    float c1 = 1f - (_activeStrokeColor.Red / 255f);
                    float m1 = 1f - (_activeStrokeColor.Green / 255f);
                    float y1 = 1f - (_activeStrokeColor.Blue / 255f);

                    float c2 = 1f - (colorAtTo.Red / 255f);
                    float m2 = 1f - (colorAtTo.Green / 255f);
                    float y2 = 1f - (colorAtTo.Blue / 255f);

                    // Absorb existing color slowly per stroke segment, scaled by the canvas paint's alpha
                    float blendFactor = mixingRate * 0.15f * (colorAtTo.Alpha / 255f); 
                    float c_mix = c1 + (c2 - c1) * blendFactor;
                    float m_mix = m1 + (m2 - m1) * blendFactor;
                    float y_mix = y1 + (y2 - y1) * blendFactor;

                    byte r = (byte)Math.Clamp((1f - c_mix) * 255f, 0, 255);
                    byte g = (byte)Math.Clamp((1f - m_mix) * 255f, 0, 255);
                    byte b = (byte)Math.Clamp((1f - y_mix) * 255f, 0, 255);

                    _activeStrokeColor = new SKColor(r, g, b, _activeStrokeColor.Alpha);
                }
            }

            float opacityAtFrom = PaintOpacity;
            float opacityAtTo = PaintOpacity;
            if (IsBrushMode)
            {
                // Dry brush: contribution of fresh paint decays, but smearing opacity remains active if over existing paint
                float smearFactor = 0.1f + ShaderMixingRate * 0.4f;
                
                float smearOpacityFrom = PaintOpacity * smearFactor * (colorAtFrom.Alpha / 255f);
                opacityAtFrom = PaintOpacity * _paintRemaining;
                opacityAtFrom = Math.Max(opacityAtFrom, smearOpacityFrom);

                float smearOpacityTo = PaintOpacity * smearFactor * (colorAtTo.Alpha / 255f);
                opacityAtTo = PaintOpacity * _paintRemaining;
                opacityAtTo = Math.Max(opacityAtTo, smearOpacityTo);
            }

            // Blend colors at start and end based on how dry the brush is (_paintRemaining)
            SKColor blendedStartColor = startColor;
            SKColor blendedEndColor = _activeStrokeColor;

            if (IsBrushMode && !IsEraserMode)
            {
                blendedStartColor = BlendColors(startColor, colorAtFrom, 1f - _paintRemaining);
                blendedEndColor = BlendColors(_activeStrokeColor, colorAtTo, 1f - _paintRemaining);
            }

            var finalStartColor = IsEraserMode ? SKColors.Transparent : blendedStartColor.WithAlpha((byte)(blendedStartColor.Alpha * opacityAtFrom));
            var finalEndColor = IsEraserMode ? SKColors.Transparent : blendedEndColor.WithAlpha((byte)(blendedEndColor.Alpha * opacityAtTo));

            // Draw line on the offscreen canvas with round caps and joins (linear gradient to prevent color segment blobs)
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeWidth = strokeWidth,
                IsAntialias = true,
                BlendMode = IsEraserMode ? SKBlendMode.Clear : SKBlendMode.SrcOver
            };

            if (!IsEraserMode)
            {
                paint.Shader = SKShader.CreateLinearGradient(
                    from,
                    to,
                    new[] { finalStartColor, finalEndColor },
                    null,
                    SKShaderTileMode.Clamp);
            }
            else
            {
                paint.Color = SKColors.Transparent;
            }

            _cachedBitmapCanvas.DrawLine(from, to, paint);
            InvalidateCanvas();
            return true;
        }

        private void InvalidateCanvas()
        {
            if (ParentView == null) return;
            var dirtyRect = new SKRect(
                Transform.Computed.X,
                Transform.Computed.Y,
                Transform.Computed.X + Transform.Computed.Width,
                Transform.Computed.Y + Transform.Computed.Height
            );
            ParentView.AddDirtyRect(dirtyRect);
            ParentView.RenderRequired = true;
        }

        // Custom recording of draw commands bypasses element children entirely
        public override void RecordDrawCommands(CommandLedger ledger)
        {
            if (Style != null && Style.BackgroundShader != BackgroundShaderType.None)
            {
                base.RecordDrawCommands(ledger);
                
                if (_isMouseHovering)
                {
                    var cmdsList = ledger.GetCommands(Name);
                    if (cmdsList != null)
                    {
                        float baseWidth = BrushSize;
                        float resolutionScale = Transform.Computed.Width / 2048f;
                        float drawRadius = (baseWidth * resolutionScale) / 2f;
                        
                        cmdsList.Add(new DrawBrushCursorCommand(_hoverMousePos, drawRadius));
                    }
                }
                return;
            }

            var cmds = new List<DrawCommand>();
            
            if (_cachedGridBitmap != null)
            {
                // Replay bitmap draw command directly
                var destRect = new SKRect(0, 0, Transform.Computed.Width, Transform.Computed.Height);
                
                // Draw grid texture scaled to element size
                cmds.Add(new DrawBitmapCommand(_cachedGridBitmap, destRect));
            }

            if (_isMouseHovering)
            {
                float baseWidth = BrushSize;
                float resolutionScale = Transform.Computed.Width / 2048f;
                float drawRadius = (baseWidth * resolutionScale) / 2f;
                
                cmds.Add(new DrawBrushCursorCommand(_hoverMousePos, drawRadius));
            }
            
            ledger.Record(Name, cmds);
        }

        public void Clear(SKColor clearColor)
        {
            if (_cachedBitmapCanvas != null)
            {
                _cachedBitmapCanvas.Clear(clearColor);
            }
            InvalidateCanvas();
        }
    }

    // A custom DrawCommand for drawing a circular brush cursor target on hover
    public class DrawBrushCursorCommand : DrawCommand
    {
        private readonly SKPoint _center;
        private readonly float _radius;
        private readonly SKPaint _paint;

        public DrawBrushCursorCommand(SKPoint center, float radius)
        {
            _center = center;
            _radius = radius;
            _paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(9, 9, 11, 150), // Semi-transparent black
                StrokeWidth = 1f,
                IsAntialias = true
            };
        }

        public override void Execute(SKCanvas canvas)
        {
            canvas.DrawCircle(_center.X, _center.Y, _radius, _paint);
        }

        public override void Dispose()
        {
            _paint.Dispose();
        }
    }

    // A fast custom DrawCommand for replaying bitmap handles in the CommandLedger
    public class DrawBitmapCommand : DrawCommand
    {
        private readonly SKBitmap _bitmap;
        private readonly SKRect _dest;
        private readonly SKPaint _paint;

        public DrawBitmapCommand(SKBitmap bitmap, SKRect dest)
        {
            _bitmap = bitmap;
            _dest = dest;
            _paint = new SKPaint { FilterQuality = SKFilterQuality.None }; // None = Pixel Art scaling
        }

        public override void Execute(SKCanvas canvas)
        {
            canvas.DrawBitmap(_bitmap, _dest, _paint);
        }

        public override void Dispose()
        {
            _paint.Dispose();
        }
    }
}
