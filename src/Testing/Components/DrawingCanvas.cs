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
        public bool IsMixerMode { get; set; } = false;
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
                if (IsMixerMode && _cachedGridBitmap != null)
                {
                    _activeStrokeColor = SampleFootprintColor(_cachedGridBitmap, _lastMousePos, BrushSize / 2f);
                }

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

        private SKColor SampleFootprintColor(SKBitmap sampleSource, SKPoint p, float R)
        {
            if (sampleSource == null) return SKColors.Transparent;

            float sampleOffset = Math.Max(1f, R * 0.6f);
            
            SKPoint[] samplePoints = new[]
            {
                p,
                new SKPoint(p.X - sampleOffset, p.Y),
                new SKPoint(p.X + sampleOffset, p.Y),
                new SKPoint(p.X, p.Y - sampleOffset),
                new SKPoint(p.X, p.Y + sampleOffset)
            };

            float sumC = 0f, sumM = 0f, sumY = 0f;
            float sumAlpha = 0f;
            int validSamples = 0;

            for (int s = 0; s < 5; s++)
            {
                SKPoint sp = samplePoints[s];
                int px = (int)Math.Clamp(sp.X, 0, sampleSource.Width - 1);
                int py = (int)Math.Clamp(sp.Y, 0, sampleSource.Height - 1);
                SKColor col = sampleSource.GetPixel(px, py);

                if (col.Alpha > 0)
                {
                    // Convert to CMY
                    float c = 1f - (col.Red / 255f);
                    float m = 1f - (col.Green / 255f);
                    float y = 1f - (col.Blue / 255f);
                    
                    sumC += c * (col.Alpha / 255f);
                    sumM += m * (col.Alpha / 255f);
                    sumY += y * (col.Alpha / 255f);
                    sumAlpha += col.Alpha;
                    validSamples++;
                }
            }

            if (validSamples > 0)
            {
                float avgAlpha = sumAlpha / 5f; // relative to whole footprint
                float w = sumAlpha > 0 ? sumAlpha / 255f : 1f;
                float avgC = sumC / w;
                float avgM = sumM / w;
                float avgY = sumY / w;

                byte r = (byte)Math.Clamp((1f - avgC) * 255f, 0, 255);
                byte g = (byte)Math.Clamp((1f - avgM) * 255f, 0, 255);
                byte b = (byte)Math.Clamp((1f - avgY) * 255f, 0, 255);
                byte a = (byte)Math.Clamp(avgAlpha, 0, 255);

                return new SKColor(r, g, b, a);
            }

            return SKColors.Transparent;
        }

        private SKColor LerpColors(SKColor colorA, SKColor colorB, float t)
        {
            byte r = (byte)Math.Clamp(colorA.Red + (colorB.Red - colorA.Red) * t, 0, 255);
            byte g = (byte)Math.Clamp(colorA.Green + (colorB.Green - colorA.Green) * t, 0, 255);
            byte b = (byte)Math.Clamp(colorA.Blue + (colorB.Blue - colorA.Blue) * t, 0, 255);
            byte a = (byte)Math.Clamp(colorA.Alpha + (colorB.Alpha - colorA.Alpha) * t, 0, 255);
            return new SKColor(r, g, b, a);
        }

        private void DrawPaintDot(SKPoint pos, float width, SKColor color)
        {
            if (_cachedBitmapCanvas == null) return;

            float R = width / 2f;
            float bufferScale = 1.1f;
            if (IsBrushMode) bufferScale = 1.5f;
            else if (IsMixerMode) bufferScale = 1.7f;

            float R_outer = Math.Max(0.5f, R * bufferScale);
            float midPos = 1.0f / bufferScale;

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                BlendMode = IsEraserMode ? SKBlendMode.Clear : SKBlendMode.SrcOver
            };

            var sampleSource = _strokeStartBitmap ?? _cachedGridBitmap;
            SKColor colorAtP = SampleFootprintColor(sampleSource, pos, R);

            float opacity = PaintOpacity;

            if (IsEraserMode)
            {
                SKColor C_center = SKColors.Black.WithAlpha((byte)(255 * opacity));
                SKColor C_mid = SKColors.Black.WithAlpha((byte)(128 * opacity));
                SKColor C_edge = SKColors.Black.WithAlpha(0);

                paint.Shader = SKShader.CreateRadialGradient(
                    pos,
                    R_outer,
                    new[] { C_center, C_mid, C_edge },
                    new[] { 0.0f, midPos, 1.0f },
                    SKShaderTileMode.Clamp);
            }
            else
            {
                SKColor C_center = color.WithAlpha((byte)(color.Alpha * opacity));
                
                float mixFactor = ShaderMixingRate * 0.5f;
                SKColor mixedColor = BlendColors(color, colorAtP, mixFactor);
                SKColor C_mid = mixedColor.WithAlpha((byte)(mixedColor.Alpha * opacity * 0.5f));
                
                SKColor C_edge = colorAtP.WithAlpha(0);

                paint.Shader = SKShader.CreateRadialGradient(
                    pos,
                    R_outer,
                    new[] { C_center, C_mid, C_edge },
                    new[] { 0.0f, midPos, 1.0f },
                    SKShaderTileMode.Clamp);
            }

            _cachedBitmapCanvas.DrawCircle(pos.X, pos.Y, R_outer, paint);
            InvalidateCanvas();
        }

        private bool DrawPaintStroke(SKPoint from, SKPoint to)
        {
            if (_cachedBitmapCanvas == null) return false;

            float distance = SKPoint.Distance(from, to);
            
            // Ignore tiny movements to prevent mouse jitter and degenerate zero-length segments
            if (distance < 1.0f)
            {
                return false;
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

            float bufferScale = 1.1f; // Marker default
            if (IsBrushMode) bufferScale = 1.5f;
            else if (IsMixerMode) bufferScale = 1.7f;

            var sampleSource = _strokeStartBitmap ?? _cachedGridBitmap;
            if (sampleSource == null) return false;

            float stepSize = Math.Max(1.0f, strokeWidth * 0.08f);
            int numSteps = (int)Math.Ceiling(distance / stepSize);

            for (int i = 0; i <= numSteps; i++)
            {
                float t = numSteps == 0 ? 1.0f : (float)i / numSteps;
                SKPoint p = new SKPoint(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);
                
                float R = strokeWidth / 2f;
                // Sample color from footprint snapshot
                SKColor colorAtP = SampleFootprintColor(sampleSource, p, R);

                // Update active brush color and remaining paint based on actual movement per step
                float stepDist = distance / (numSteps == 0 ? 1 : numSteps);
                
                if (IsBrushMode || IsMixerMode)
                {
                    float stepDecay = IsMixerMode ? 500f : 1200f;
                    _paintRemaining = Math.Max(0f, _paintRemaining - (stepDist / stepDecay));
                }
                else
                {
                    float replenishmentRate = Math.Clamp(stepDist / 150f, 0f, 1f);
                    _activeStrokeColor = LerpColors(_activeStrokeColor, DrawColor, replenishmentRate);
                }

                if (!IsEraserMode && colorAtP.Alpha > 10)
                {
                    float mixingRate = ShaderMixingRate;
                    if (mixingRate > 0.01f)
                    {
                        // Scale blend factor by step distance to keep it uniform regardless of mouse speed
                        float blendFactor = mixingRate * 0.15f * (colorAtP.Alpha / 255f) * (stepDist / 15f);
                        blendFactor = Math.Clamp(blendFactor, 0f, 1f);
                        
                        if (IsMixerMode && _activeStrokeColor.Alpha < 30)
                        {
                            blendFactor = 0.8f;
                        }

                        // Subtractive mixing in CMY space
                        float c1 = 1f - (_activeStrokeColor.Red / 255f);
                        float m1 = 1f - (_activeStrokeColor.Green / 255f);
                        float y1 = 1f - (_activeStrokeColor.Blue / 255f);

                        float c2 = 1f - (colorAtP.Red / 255f);
                        float m2 = 1f - (colorAtP.Green / 255f);
                        float y2 = 1f - (colorAtP.Blue / 255f);

                        float c_mix = c1 + (c2 - c1) * blendFactor;
                        float m_mix = m1 + (m2 - m1) * blendFactor;
                        float y_mix = y1 + (y2 - y1) * blendFactor;

                        byte r = (byte)Math.Clamp((1f - c_mix) * 255f, 0, 255);
                        byte g = (byte)Math.Clamp((1f - m_mix) * 255f, 0, 255);
                        byte b = (byte)Math.Clamp((1f - y_mix) * 255f, 0, 255);

                        byte a = _activeStrokeColor.Alpha;
                        if (IsMixerMode)
                        {
                            a = (byte)Math.Clamp(_activeStrokeColor.Alpha + (colorAtP.Alpha - _activeStrokeColor.Alpha) * blendFactor, 0, 255);
                        }

                        _activeStrokeColor = new SKColor(r, g, b, a);
                    }
                }

                float opacity = PaintOpacity;
                if (IsBrushMode || IsMixerMode)
                {
                    float smearFactor = 0.1f + ShaderMixingRate * 0.4f;
                    float smearOpacity = PaintOpacity * smearFactor * (colorAtP.Alpha / 255f);
                    opacity = PaintOpacity * _paintRemaining;
                    opacity = Math.Max(opacity, smearOpacity);
                }

                float R_outer = Math.Max(0.5f, R * bufferScale);
                float midPos = 1.0f / bufferScale;

                using var paint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true,
                    BlendMode = IsEraserMode ? SKBlendMode.Clear : SKBlendMode.SrcOver
                };

                if (IsEraserMode)
                {
                    SKColor C_center = SKColors.Black.WithAlpha((byte)(255 * opacity));
                    SKColor C_mid = SKColors.Black.WithAlpha((byte)(128 * opacity));
                    SKColor C_edge = SKColors.Black.WithAlpha(0);

                    paint.Shader = SKShader.CreateRadialGradient(
                        p,
                        R_outer,
                        new[] { C_center, C_mid, C_edge },
                        new[] { 0.0f, midPos, 1.0f },
                        SKShaderTileMode.Clamp);
                }
                else
                {
                    SKColor C_center = _activeStrokeColor.WithAlpha((byte)(_activeStrokeColor.Alpha * opacity));
                    
                    float mixFactor = ShaderMixingRate * 0.5f;
                    SKColor mixedColor = BlendColors(_activeStrokeColor, colorAtP, mixFactor);
                    SKColor C_mid = mixedColor.WithAlpha((byte)(mixedColor.Alpha * opacity * 0.5f));
                    
                    SKColor C_edge = colorAtP.WithAlpha(0);

                    paint.Shader = SKShader.CreateRadialGradient(
                        p,
                        R_outer,
                        new[] { C_center, C_mid, C_edge },
                        new[] { 0.0f, midPos, 1.0f },
                        SKShaderTileMode.Clamp);
                }

                _cachedBitmapCanvas.DrawCircle(p.X, p.Y, R_outer, paint);
            }

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
