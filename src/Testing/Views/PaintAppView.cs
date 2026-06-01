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
        public Action? OnSwitchTo3D;

        private DrawingCanvas? _drawingCanvas;
        
        // Classic High-Fidelity Palette Colors (Non-neon, solid colors)
        private readonly SKColor[] _paletteColors = new[]
        {
            new SKColor(239, 68, 68),   // Ruby Red
            new SKColor(59, 130, 246),  // Royal Blue
            new SKColor(34, 197, 94),   // Forest Green
            new SKColor(249, 115, 22),  // Amber Orange
            new SKColor(139, 92, 246),  // Violet Purple
            new SKColor(234, 179, 8),   // Chrome Yellow
            new SKColor(255, 255, 255), // Pure White
            new SKColor(30, 41, 59)     // Slate 800 (Eraser)
        };
        
        private readonly string[] _paletteNames = new[]
        {
            "Ruby Red", "Royal Blue", "Forest Green", "Amber Orange", "Violet Purple", "Chrome Yellow", "Pure White", "Eraser"
        };
        
        private readonly VisualElement[] _colorIndicators = new VisualElement[8];
        private VisualElement? _selectedColorText;

        public PaintAppView() : base("Neon Paint")
        {
            // Consistent slate background color
            BackColor = new SKColor(11, 14, 22);
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
                    BackColor = new SKColor(16, 20, 30, 240), // Same slate sidebar color
                    Border = new BorderStyle { Width = 0, Color = SKColors.Transparent },
                    Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(100), SpreadX = 8, SpreadY = 0, OffsetX = 2 }
                },
                Transform = new Transform(0, 0, sidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left,
                    FixedWidth = true,
                    FixedHeight = false
                }
            };
            AddElement(sidebar);

            // Brand Label (shifted down to prevent stats box overlap)
            var brand = new VisualElement
            {
                Name = "PaintSidebar_Brand",
                Text = "⚡ PAINT CANVAS",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 22, Weight = 800, Alignment = TextAlign.Center, Padding = 15 }
                },
                Transform = new Transform(0, 50, sidebarWidth, 60) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            sidebar.AddChild(brand);

            // Palette Header
            var paletteHeader = new VisualElement
            {
                Name = "PaletteHeader",
                Text = "SELECT PALETTE",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(100, 116, 139), Size = 10, Weight = 700, Alignment = TextAlign.Left, Padding = 25 }
                },
                Transform = new Transform(0, 130, sidebarWidth, 25)
            };
            sidebar.AddChild(paletteHeader);

            // Selected Color Indicator Label
            _selectedColorText = new VisualElement
            {
                Name = "SelectedColorText",
                Text = "Color: Ruby Red",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = _paletteColors[0], Size = 12, Weight = 600, Alignment = TextAlign.Left, Padding = 25 }
                },
                Transform = new Transform(0, 155, sidebarWidth, 25)
            };
            sidebar.AddChild(_selectedColorText);

            // Color Palette Selector Grid (starts at startY = 185f)
            float startX = 25f;
            float startY = 185f;
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
                            Color = SKColors.Black.WithAlpha(40),
                            SpreadX = 0,
                            SpreadY = i == 0 ? 3 : 0,
                            OffsetY = i == 0 ? 2 : 0
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

            // Tools & Action buttons
            float navY = startY + (buttonSize + gapY) * 2 + 25f;
            
            var clearBtn = new Button("CLEAR CANVAS", new SKColor(239, 68, 68))
            {
                Name = "Btn_ClearCanvas",
                Transform = new Transform(20, navY, sidebarWidth - 40f, 42) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            clearBtn.Style.Text.Color = SKColors.White;
            clearBtn.Style.Border.Roundness = 8;
            clearBtn.OnClick = () =>
            {
                _drawingCanvas?.Clear(new SKColor(30, 41, 59));
            };
            sidebar.AddChild(clearBtn);

            // Unified Sidebar Navigation Items
            string[] menuItems = { "Overview", "Neon Showcase", "Neon Paint", "Task Board", "3D Showcase" };
            float sidebarMenuY = navY + 60f;
            for (int i = 0; i < menuItems.Length; i++)
            {
                var item = menuItems[i];
                var btn = new SidebarButton(item, i == 2) // Paint is active (i == 2)
                {
                    Transform = { X = 20, Y = sidebarMenuY, Width = sidebarWidth - 40 }
                };

                int idx = i;
                btn.OnClick = () =>
                {
                    if (idx == 0) OnSwitchToDashboard?.Invoke();
                    else if (idx == 1) OnSwitchToNeonShowcase?.Invoke();
                    else if (idx == 3) OnSwitchToKanban?.Invoke();
                    else if (idx == 4) OnSwitchTo3D?.Invoke();
                };

                sidebar.AddChild(btn);
                sidebarMenuY += 55f;
            }


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

            // Unified Header breadcrumb title block (shifted down to match Dashboard)
            var titleBlock = new VisualElement
            {
                Name = "PaintTitle",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(30, 18, Width - sidebarWidth - 60f, 44)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right,
                    FixedHeight = true
                }
            };
            mainContent.AddChild(titleBlock);

            titleBlock.AddChild(new VisualElement
            {
                Name = "Paint_HeaderTitle",
                Text = "PAINT CANVAS",
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.White, Size = 20, Weight = 800, Alignment = TextAlign.Left } },
                Transform = new Transform(0, 0, 400, 24) { Anchor = Anchor.Top | Anchor.Left }
            });

            titleBlock.AddChild(new VisualElement
            {
                Name = "Paint_HeaderSub",
                Text = "FREEHAND DRAWING & DESIGN HUB",
                Style = new ElementStyle { Text = new TextStyle { Color = new SKColor(100, 116, 139), Size = 9, Weight = 600, Alignment = TextAlign.Left } },
                Transform = new Transform(0, 24, 400, 20) { Anchor = Anchor.Top | Anchor.Left }
            });

            // Grid Canvas Container (Glassmorphic frame)
            var canvasFrame = new VisualElement
            {
                Name = "CanvasFrame",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(22, 28, 41, 180), // Sleek glass card background
                    Border = new BorderStyle { Roundness = 16, Width = 1, Color = new SKColor(255, 255, 255, 12) },
                    Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(50), SpreadY = 5, OffsetY = 5 }
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
                    ind.Style.Shadow.SpreadY = 3;
                    ind.Style.Shadow.OffsetY = 2;
                }
                else
                {
                    ind.Style.Border.Width = 1;
                    ind.Style.Border.Color = new SKColor(255, 255, 255, 60);
                    ind.Style.Shadow.SpreadY = 0;
                    ind.Style.Shadow.OffsetY = 0;
                }
                ind.ScheduleRender();
            }
        }
    }
}
