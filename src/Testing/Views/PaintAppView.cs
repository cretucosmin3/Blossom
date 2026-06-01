using System;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;
using Blossom.Testing.Components;

namespace Blossom.Testing.Views
{
    public class PaintAppView : View
    {
        public Action? OnSwitchToDashboard;
        public Action? OnSwitchToNeonShowcase;
        public Action? OnSwitchToKanban;

        private DrawingCanvas? _drawingCanvas;
        private readonly SKColor[] _paletteColors = new[]
        {
            new SKColor(255, 0, 110),   // Neon Pink
            new SKColor(0, 240, 255),   // Cyber Blue
            new SKColor(57, 255, 20),   // Lime Glow
            new SKColor(255, 170, 0),   // Amber Orange
            new SKColor(255, 0, 255),   // Purple Neon
            new SKColor(255, 255, 0),   // Bright Yellow
            new SKColor(255, 255, 255), // Pure White
            new SKColor(30, 41, 59)     // Slate 800 (Eraser)
        };
        private readonly string[] _paletteNames = new[]
        {
            "Neon Pink", "Cyber Blue", "Lime Glow", "Amber Orange", "Purple Neon", "Bright Yellow", "Pure White", "Eraser"
        };
        private readonly VisualElement[] _colorIndicators = new VisualElement[8];
        private VisualElement? _selectedColorText;

        public PaintAppView() : base("Neon Paint")
        {
            BackColor = new SKColor(9, 13, 22); // Deep space background
        }

        public override void Init()
        {
            float sidebarWidth = 260f;

            // --- 1. SIDEBAR ---
            var sidebar = new VisualElement
            {
                Name = "PaintSidebar",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(17, 24, 39), // Slate 900
                    Border = new BorderStyle { Width = 0, Color = SKColors.Transparent },
                    Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(120), SpreadX = 8, SpreadY = 0 }
                },
                Transform = new Transform(0, 0, sidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left,
                    FixedWidth = true,
                    FixedHeight = false
                }
            };
            AddElement(sidebar);

            // Glowing Sidebar Brand
            var brand = new VisualElement
            {
                Name = "PaintSidebar_Brand",
                Text = "⚡ NEON ART",
                Style = new ElementStyle
                {
                    Text = new TextStyle 
                    { 
                        Color = new SKColor(255, 0, 110), 
                        Size = 26, 
                        Weight = 800, 
                        Alignment = TextAlign.Center, 
                        Padding = 25,
                        Shadow = new ShadowStyle
                        {
                            Color = new SKColor(255, 0, 110, 150),
                            SpreadX = 8,
                            SpreadY = 8
                        }
                    }
                },
                Transform = new Transform(0, 0, sidebarWidth, 80)
            };
            sidebar.AddChild(brand);

            // Palette Header
            var paletteHeader = new VisualElement
            {
                Name = "PaletteHeader",
                Text = "SELECT PALETTE",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = SKColors.Gray, Size = 11, Weight = 700, Alignment = TextAlign.Left, Padding = 25 }
                },
                Transform = new Transform(0, 80, sidebarWidth, 25)
            };
            sidebar.AddChild(paletteHeader);

            // Add Selected Color Indicator Label
            _selectedColorText = new VisualElement
            {
                Name = "SelectedColorText",
                Text = "Color: Neon Pink",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(255, 0, 110), Size = 12, Weight = 600, Alignment = TextAlign.Left, Padding = 25 }
                },
                Transform = new Transform(0, 105, sidebarWidth, 25)
            };
            sidebar.AddChild(_selectedColorText);

            // Color Palette Selector Grid
            float startX = 25f;
            float startY = 135f;
            float buttonSize = 40f;
            float gapX = 12f;
            float gapY = 12f;

            for (int i = 0; i < _paletteColors.Length; i++)
            {
                int row = i / 4;
                int col = i % 4;
                float x = startX + col * (buttonSize + gapX);
                float y = startY + row * (buttonSize + gapY);

                var color = _paletteColors[i];
                var name = _paletteNames[i];

                var colorBtn = new VisualElement
                {
                    Name = $"ColorIndicator_{i}",
                    Style = new ElementStyle
                    {
                        BackColor = color,
                        Border = new BorderStyle 
                        { 
                            Roundness = buttonSize / 2f, 
                            Width = i == 0 ? 3 : 1, 
                            Color = i == 0 ? SKColors.White : new SKColor(255, 255, 255, 60) 
                        },
                        Shadow = new ShadowStyle
                        {
                            Color = color.WithAlpha(100),
                            SpreadX = i == 0 ? 5 : 0,
                            SpreadY = i == 0 ? 5 : 0
                        }
                    },
                    Transform = new Transform(x, y, buttonSize, buttonSize)
                    {
                        FixedWidth = true,
                        FixedHeight = true
                    }
                };

                int index = i;
                colorBtn.Events.OnMouseDown += (s, e) =>
                {
                    SelectColor(index);
                };

                sidebar.AddChild(colorBtn);
                _colorIndicators[i] = colorBtn;
            }

            // Tools Navigation Section
            float navY = startY + (buttonSize + gapY) * 2 + 30f;
            
            var clearBtn = new NeonButton("CLEAR CANVAS", new SKColor(244, 63, 94), sidebarWidth - 40f, 45f)
            {
                Name = "Btn_ClearCanvas",
                Transform = { X = 20, Y = navY, Anchor = Anchor.Top }
            };
            clearBtn.OnClick = () =>
            {
                _drawingCanvas?.Clear(new SKColor(30, 41, 59));
            };
            sidebar.AddChild(clearBtn);

            var dashboardBtn = new NeonButton("DASHBOARD ➜", new SKColor(56, 189, 248), sidebarWidth - 40f, 45f)
            {
                Name = "Btn_GoToDashboard",
                Transform = { X = 20, Y = navY + 55f, Anchor = Anchor.Top }
            };
            dashboardBtn.OnClick = () => OnSwitchToDashboard?.Invoke();
            sidebar.AddChild(dashboardBtn);

            var showcaseBtn = new NeonButton("NEON SHOWCASE ➜", new SKColor(139, 92, 246), sidebarWidth - 40f, 45f)
            {
                Name = "Btn_GoToShowcase",
                Transform = { X = 20, Y = navY + 110f, Anchor = Anchor.Top }
            };
            showcaseBtn.OnClick = () => OnSwitchToNeonShowcase?.Invoke();
            sidebar.AddChild(showcaseBtn);

            var kanbanBtn = new NeonButton("TASK BOARD ➜", new SKColor(16, 185, 129), sidebarWidth - 40f, 45f)
            {
                Name = "Btn_GoToKanban",
                Transform = { X = 20, Y = navY + 165f, Anchor = Anchor.Top }
            };
            kanbanBtn.OnClick = () => OnSwitchToKanban?.Invoke();
            sidebar.AddChild(kanbanBtn);


            // --- 2. MAIN CONTENT AREA ---
            var mainContent = new VisualElement
            {
                Name = "PaintMainContent",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(sidebarWidth, 0, Width - sidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right | Anchor.Bottom
                }
            };
            AddElement(mainContent);

            // Title block
            var titleBlock = new VisualElement
            {
                Name = "PaintTitle",
                Text = "⚡ CYBERPUNK PAINT GRID",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(0, 240, 255),
                        Size = 24,
                        Weight = 800,
                        Padding = 30,
                        Shadow = new ShadowStyle { Color = new SKColor(0, 240, 255, 100), SpreadX = 5, SpreadY = 5 }
                    }
                },
                Transform = new Transform(0, 0, Width - sidebarWidth, 75)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            mainContent.AddChild(titleBlock);

            // Grid Canvas Container (Glassmorphic frame)
            var canvasFrame = new VisualElement
            {
                Name = "CanvasFrame",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59, 80), // Transparent frame
                    Border = new BorderStyle { Roundness = 16, Width = 2, Color = new SKColor(255, 255, 255, 20) },
                    Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(80), SpreadX = 0, SpreadY = 10, OffsetY = 8 }
                },
                Transform = new Transform(30, 80, Width - sidebarWidth - 60, Height - 110)
                {
                    Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
                }
            };
            mainContent.AddChild(canvasFrame);

            // Responsive Drawing Grid Canvas itself
            _drawingCanvas = new DrawingCanvas(48, 36, Width - sidebarWidth - 100, Height - 150)
            {
                Name = "DrawingCanvas_Main",
                DrawColor = _paletteColors[0],
                Transform = new Transform(20, 20, Width - sidebarWidth - 100, Height - 150)
                {
                    Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
                }
            };
            _drawingCanvas.Clear(new SKColor(30, 41, 59)); // Initialize grid to deep slate
            canvasFrame.AddChild(_drawingCanvas);
        }

        private void SelectColor(int index)
        {
            if (index < 0 || index >= _paletteColors.Length || _drawingCanvas == null) return;

            var color = _paletteColors[index];
            _drawingCanvas.DrawColor = color;

            if (_selectedColorText != null)
            {
                _selectedColorText.Text = $"Color: {_paletteNames[index]}";
                _selectedColorText.Style.Text.Color = color;
            }

            for (int i = 0; i < _colorIndicators.Length; i++)
            {
                var ind = _colorIndicators[i];
                if (ind == null) continue;

                if (i == index)
                {
                    ind.Style.Border.Width = 3;
                    ind.Style.Border.Color = SKColors.White;
                    ind.Style.Shadow.SpreadX = 5;
                    ind.Style.Shadow.SpreadY = 5;
                }
                else
                {
                    ind.Style.Border.Width = 1;
                    ind.Style.Border.Color = new SKColor(255, 255, 255, 60);
                    ind.Style.Shadow.SpreadX = 0;
                    ind.Style.Shadow.SpreadY = 0;
                }
                ind.ScheduleRender();
            }
        }
    }
}
