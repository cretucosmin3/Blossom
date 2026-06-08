using System;
using System.Numerics;
using System.Collections.Generic;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class BuilderCanvas : VisualElement
{
    private float _zoom = 1.0f;
    private Vector2 _panOffset = new Vector2(100f, 100f); // Default center offset
    private bool _isPanning;
    private Vector2 _lastMousePos;

    [BuilderProperty("Zoom Factor", "Canvas", min: 0.15f, max: 4.0f, step: 0.05f)]
    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Clamp(value, 0.15f, 4.0f);
            Transform.ScaleX = _zoom;
            Transform.ScaleY = _zoom;
            ScheduleRender();
        }
    }

    [BuilderProperty("Pan X", "Canvas", min: -2000f, max: 2000f, step: 1f)]
    public float PanX
    {
        get => _panOffset.X;
        set
        {
            _panOffset.X = value;
            Transform.X = _panOffset.X;
            ScheduleRender();
        }
    }

    [BuilderProperty("Pan Y", "Canvas", min: -2000f, max: 2000f, step: 1f)]
    public float PanY
    {
        get => _panOffset.Y;
        set
        {
            _panOffset.Y = value;
            Transform.Y = _panOffset.Y;
            ScheduleRender();
        }
    }

    public Vector2 PanOffset
    {
        get => _panOffset;
        set
        {
            _panOffset = value;
            Transform.X = _panOffset.X;
            Transform.Y = _panOffset.Y;
            ScheduleRender();
        }
    }

    public BuilderCanvas()
    {
        Name = "BuilderCanvas";
        
        Style = new ElementStyle
        {
            BackColor = new SKColor(11, 14, 22), // Midnight slate
            Border = new BorderStyle { Width = 0, Color = SKColors.Transparent }
        };

        Transform = new Transform(PanX, PanY, 3000, 3000)
        {
            FixedWidth = true,
            FixedHeight = true
        };

        Zoom = 1.0f;

        // Mouse Events inside Viewport
        Events.OnMouseScroll += (s, scroll) =>
        {
            // Zoom In/Out
            float zoomFactor = scroll.Y > 0 ? 1.08f : 0.92f;
            float oldZoom = Zoom;
            float newZoom = oldZoom * zoomFactor;

            // Zoom target at mouse position
            Vector2 mousePos = _lastMousePos;
            Vector2 localMouse = (mousePos - _panOffset) / oldZoom;

            Zoom = newZoom;
            _panOffset = mousePos - localMouse * Zoom;
            Transform.X = _panOffset.X;
            Transform.Y = _panOffset.Y;
        };

        Events.OnMouseDown += (s, e) =>
        {
            // Middle drag is button 2, or Space + Left button (space is key code handled at view level if we want, but middle drag is reliable)
            // Left drag handles movement if standard component selection doesn't grab it
            if (e.Button == 2 || e.Button == 1) // Middle or Right click to drag
            {
                _isPanning = true;
                _lastMousePos = e.Global;
            }
        };

        Events.OnMouseMove += (s, e) =>
        {
            Vector2 mouseDelta = e.Global - _lastMousePos;
            _lastMousePos = e.Global;

            if (_isPanning)
            {
                _panOffset += mouseDelta;
                Transform.X = _panOffset.X;
                Transform.Y = _panOffset.Y;
            }
        };

        Events.OnMouseUp += (s, e) =>
        {
            _isPanning = false;
        };
    }

    public override void RecordDrawCommands(CommandLedger ledger)
    {
        var cmds = new List<DrawCommand>();
        var rect = new SKRect(0, 0, Transform.Computed.Width, Transform.Computed.Height);

        // 1. Draw solid background
        var bgPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = Style.BackColor,
            IsAntialias = true
        };
        cmds.Add(new DrawRoundRectCommand(rect, 0, 0, 0, 0, bgPaint));

        // 2. Draw dynamic grid dots/lines
        cmds.Add(new DrawBuilderGridCommand(new SKColor(38, 48, 68), 50f));

        ledger.Record(Name, cmds);
    }
}

/// <summary>
/// Custom draw command to paint the editor grid lines on the builder canvas.
/// </summary>
public class DrawBuilderGridCommand : DrawCommand
{
    private readonly SKColor _gridColor;
    private readonly float _gridSize;
    private readonly SKPaint _paint;

    public DrawBuilderGridCommand(SKColor color, float size = 50f)
    {
        _gridColor = color;
        _gridSize = size;
        _paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            Color = _gridColor,
            IsAntialias = false
        };
    }

    public override void Execute(SKCanvas canvas)
    {
        // Get local visible bounds
        SKRect clip = canvas.LocalClipBounds;

        float startX = (float)Math.Floor(clip.Left / _gridSize) * _gridSize;
        float endX = (float)Math.Ceiling(clip.Right / _gridSize) * _gridSize;
        float startY = (float)Math.Floor(clip.Top / _gridSize) * _gridSize;
        float endY = (float)Math.Ceiling(clip.Bottom / _gridSize) * _gridSize;

        // Draw vertical lines
        for (float x = startX; x <= endX; x += _gridSize)
        {
            canvas.DrawLine(x, clip.Top, x, clip.Bottom, _paint);
        }

        // Draw horizontal lines
        for (float y = startY; y <= endY; y += _gridSize)
        {
            canvas.DrawLine(clip.Left, y, clip.Right, y, _paint);
        }
    }

    public override void Dispose()
    {
        _paint.Dispose();
    }
}
