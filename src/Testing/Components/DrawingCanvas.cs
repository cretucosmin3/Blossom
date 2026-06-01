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
        private readonly int _cols;
        private readonly int _rows;
        private readonly SKColor[,] _gridColors;
        private SKBitmap? _cachedGridBitmap;
        private SKCanvas? _cachedBitmapCanvas;
        
        public SKColor DrawColor { get; set; } = SKColors.Black;

        public DrawingCanvas(int cols, int rows, float width, float height)
        {
            Name = $"DrawingCanvas_{Guid.NewGuid().ToString().Substring(0, 4)}";
            _cols = cols;
            _rows = rows;
            _gridColors = new SKColor[cols, rows];

            Transform = new Transform(0, 0, width, height);

            // Initialize grid to white
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    _gridColors[x, y] = SKColors.White;

            RebuildBitmap();

            // Bind click/drag inputs locally within the element bounds
            Events.OnMouseDown += HandleDrawInput;
            Events.OnMouseMove += HandleDrawInput;

            OnDisposing += (el) =>
            {
                _cachedGridBitmap?.Dispose();
                _cachedBitmapCanvas?.Dispose();
            };
        }

        private void RebuildBitmap()
        {
            _cachedGridBitmap?.Dispose();
            _cachedBitmapCanvas?.Dispose();

            // Create a hardware-friendly offscreen texture
            _cachedGridBitmap = new SKBitmap(_cols, _rows);
            _cachedBitmapCanvas = new SKCanvas(_cachedGridBitmap);
            
            // Draw grid colors onto texture
            for (int x = 0; x < _cols; x++)
            {
                for (int y = 0; y < _rows; y++)
                {
                    _cachedGridBitmap.SetPixel(x, y, _gridColors[x, y]);
                }
            }
        }

        private void HandleDrawInput(object sender, MouseEventArgs e)
        {
            if (ParentView == null) return;
            if (ParentView.Events.IsMouseDown(0) == false) return; // Only draw on Left Click Click/Drag

            // Convert canvas coordinate to grid coordinate
            float cellWidth = Transform.Computed.Width / _cols;
            float cellHeight = Transform.Computed.Height / _rows;

            int col = (int)(e.Relative.X / cellWidth);
            int row = (int)(e.Relative.Y / cellHeight);

            if (col >= 0 && col < _cols && row >= 0 && row < _rows)
            {
                if (_gridColors[col, row] != DrawColor)
                {
                    _gridColors[col, row] = DrawColor;
                    
                    // Update texture directly
                    _cachedGridBitmap?.SetPixel(col, row, DrawColor);
                    
                    // Invalidate ONLY the single cell bounding box region!
                    var dirtyCell = new SKRect(
                        Transform.Computed.X + col * cellWidth,
                        Transform.Computed.Y + row * cellHeight,
                        Transform.Computed.X + (col + 1) * cellWidth,
                        Transform.Computed.Y + (row + 1) * cellHeight
                    );
                    
                    ParentView.AddDirtyRect(dirtyCell);
                    ParentView.RenderRequired = true;
                }
            }
        }

        // Custom recording of draw commands bypasses element children entirely
        public override void RecordDrawCommands(CommandLedger ledger)
        {
            var cmds = new List<DrawCommand>();
            
            if (_cachedGridBitmap != null)
            {
                // Replay bitmap draw command directly
                var destRect = new SKRect(0, 0, Transform.Computed.Width, Transform.Computed.Height);
                
                // Draw grid texture scaled to element size
                cmds.Add(new DrawBitmapCommand(_cachedGridBitmap, destRect));
            }
            
            ledger.Record(Name, cmds);
        }

        public void Clear(SKColor clearColor)
        {
            for (int x = 0; x < _cols; x++)
            {
                for (int y = 0; y < _rows; y++)
                {
                    _gridColors[x, y] = clearColor;
                }
            }
            RebuildBitmap();
            ScheduleRender();
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
