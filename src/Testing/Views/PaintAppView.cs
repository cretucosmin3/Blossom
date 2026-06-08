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
        public Action? OnSwitchToGlass;

        private DrawingCanvas? _drawingCanvas;
        
        // Classic Palette Colors (Sleek solid colors - Primaries + Secondary + White/Black)
        private readonly SKColor[] _paletteColors = new[]
        {
            new SKColor(225, 29, 72),   // Ruby Red (Primary)
            new SKColor(37, 99, 235),   // Royal Blue (Primary)
            new SKColor(250, 204, 21),  // Chrome Yellow (Primary)
            new SKColor(5, 150, 105),   // Forest Green (Secondary)
            new SKColor(249, 115, 22),  // Amber Orange (Secondary)
            new SKColor(124, 58, 237),  // Violet Purple (Secondary)
            new SKColor(255, 255, 255), // Pure White
            new SKColor(9, 9, 11)       // Midnight Black
        };
        
        private readonly string[] _paletteNames = new[]
        {
            "Ruby Red", "Royal Blue", "Chrome Yellow", "Forest Green", "Amber Orange", "Violet Purple", "Pure White", "Midnight Black"
        };
        
        private readonly VisualElement[] _colorIndicators = new VisualElement[8];
        private VisualElement? _selectedColorText;
        private Checkbox? _eraserCheckbox;
        private VisualElement? _mixHeader;

        // Color mixing controls
        private readonly VisualElement[] _mixIndicators = new VisualElement[4];
        private float _currentMixRate = 0.25f; // SLOW mixing by default

        // Brush Type controls (Marker vs Brush vs Mixer)
        private readonly VisualElement[] _brushTypeIndicators = new VisualElement[3];
        private readonly VisualElement[] _brushTypeIcons = new VisualElement[3];
        private bool _isBrushMode = false;
        private bool _isMixerMode = false;

        public PaintAppView() : base("Paint Canvas")
        {
            BackColor = new SKColor(248, 250, 252); // Light background
        }

        public override void Init()
        {
            float sidebarWidth = 260f;

            // --- 1. SIDEBAR (Sleek, Flat White UI) ---
            var sidebar = new VisualElement
            {
                Name = "PaintSidebar",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(255, 255, 255), // Flat white
                    Border = new BorderStyle { Width = 1, Color = new SKColor(226, 232, 240), Roundness = 0 }
                },
                Transform = new Transform(0, 0, sidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left,
                    FixedWidth = true,
                    FixedHeight = false
                }
            };
            AddElement(sidebar);

            // Brand Label (Flat, clean styling)
            var brand = new VisualElement
            {
                Name = "PaintSidebar_Brand",
                Text = "⚡ PAINT CANVAS",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(15, 23, 42), Size = 18, Weight = 800, Alignment = TextAlign.Center, Padding = 15 }
                },
                Transform = new Transform(0, 40, sidebarWidth, 50) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            sidebar.AddChild(brand);

            // Palette Header
            var paletteHeader = new VisualElement
            {
                Name = "PaletteHeader",
                Text = "COLOR PALETTE",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(100, 116, 139), Size = 12, Weight = 700, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform(0, 90, sidebarWidth, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            sidebar.AddChild(paletteHeader);

            // Selected Color Indicator Label
            _selectedColorText = new VisualElement
            {
                Name = "SelectedColorText",
                Text = "Active: Ruby Red",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = _paletteColors[0], Size = 12, Weight = 600, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform(0, 115, sidebarWidth, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            sidebar.AddChild(_selectedColorText);

            // Color Palette Grid
            float startX = 20f;
            float startY = 140f;
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
                            Color = i == 0 ? new SKColor(15, 23, 42) : new SKColor(226, 232, 240) 
                        }
                    },
                    Transform = new Transform(x, y, buttonSize, buttonSize)
                    {
                        Anchor = Anchor.Top | Anchor.Left,
                        FixedWidth = true,
                        FixedHeight = true
                    }
                };

                int index = i;
                colorBtn.Events.OnMouseDown += (s, e) =>
                {
                    SelectColor(index);
                };
                colorBtn.Events.OnMouseEnter += (s) =>
                {
                    colorBtn.Transform.ScaleX = 1.1f;
                    colorBtn.Transform.ScaleY = 1.1f;
                    colorBtn.ScheduleRender();
                };
                colorBtn.Events.OnMouseLeave += (s) =>
                {
                    colorBtn.Transform.ScaleX = 1.0f;
                    colorBtn.Transform.ScaleY = 1.0f;
                    colorBtn.ScheduleRender();
                };

                sidebar.AddChild(colorBtn);
                _colorIndicators[i] = colorBtn;
            }

            // Brush Selection Header
            float brushHeaderY = 250f;
            sidebar.AddChild(new VisualElement
            {
                Name = "BrushHeader",
                Text = "BRUSH SIZE",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(100, 116, 139), Size = 12, Weight = 700, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform(0, brushHeaderY, sidebarWidth, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Brush size slider
            var sizeSlider = new Slider(1f, 300f, 20f)
            {
                Transform = new Transform(20, brushHeaderY + 25f, sidebarWidth - 40f, 24) { Anchor = Anchor.Top | Anchor.Left }
            };
            sizeSlider.OnValueChanged += (val) =>
            {
                if (_drawingCanvas != null)
                {
                    _drawingCanvas.BrushSize = val;
                }
            };
            sidebar.AddChild(sizeSlider);

            // Brush options section
            float optionsHeaderY = 320f;
            var optionsHeader = new VisualElement
            {
                Name = "OptionsHeader",
                Text = "BRUSH OPTIONS",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(100, 116, 139), Size = 12, Weight = 700, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform(0, optionsHeaderY, sidebarWidth, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            sidebar.AddChild(optionsHeader);

            var chkVelocity = new Checkbox("Velocity Dynamics", true)
            {
                TextColor = new SKColor(51, 65, 85),
                Transform = new Transform(20, optionsHeaderY + 25f, sidebarWidth - 40f, 24)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            chkVelocity.OnCheckedChanged += (isChecked) =>
            {
                if (_drawingCanvas != null)
                {
                    _drawingCanvas.UseVelocityDynamics = isChecked;
                }
            };
            sidebar.AddChild(chkVelocity);

            var chkOpacity = new Checkbox("Semi-Transparent", false)
            {
                TextColor = new SKColor(51, 65, 85),
                Transform = new Transform(20, optionsHeaderY + 51f, sidebarWidth - 40f, 24)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            chkOpacity.OnCheckedChanged += (isChecked) =>
            {
                if (_drawingCanvas != null)
                {
                    _drawingCanvas.PaintOpacity = isChecked ? 0.35f : 1.0f;
                }
            };
            sidebar.AddChild(chkOpacity);

            _eraserCheckbox = new Checkbox("Eraser Mode", false)
            {
                TextColor = new SKColor(51, 65, 85),
                Transform = new Transform(20, optionsHeaderY + 77f, sidebarWidth - 40f, 24)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            _eraserCheckbox.OnCheckedChanged += (isChecked) =>
            {
                if (_drawingCanvas != null)
                {
                    _drawingCanvas.IsEraserMode = isChecked;
                }
            };
            sidebar.AddChild(_eraserCheckbox);

            // Color mixing selection
            float mixHeaderY = 430f;
            _mixHeader = new VisualElement
            {
                Name = "MixHeader",
                Text = "COLOR MIXING",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(100, 116, 139), Size = 12, Weight = 700, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform(0, mixHeaderY, sidebarWidth, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            sidebar.AddChild(_mixHeader);

            // Mixing speed selector buttons (Flat buttons in a 2x2 grid, styled for white theme)
            float mixBtnW = 104f;
            float mixBtnH = 34f;
            float mixBtnStartX = 20f;
            float mixBtnStartY = mixHeaderY + 25f;
            float mixGapX = 12f;
            float mixGapY = 12f;

            string[] mixLabels = { "NONE", "SLOW", "MEDIUM", "FAST" };
            float[] mixRates = { 0.0f, 0.25f, 0.55f, 0.85f };

            for (int i = 0; i < 4; i++)
            {
                int row = i / 2;
                int col = i % 2;
                float x = mixBtnStartX + col * (mixBtnW + mixGapX);
                float y = mixBtnStartY + row * (mixBtnH + mixGapY);

                var btn = new VisualElement
                {
                    Name = $"MixBtn_{i}",
                    Text = mixLabels[i],
                    Style = new ElementStyle
                    {
                        BackColor = i == 1 ? new SKColor(9, 9, 11) : new SKColor(255, 255, 255),
                        Border = new BorderStyle { Roundness = 8, Width = i == 1 ? 0 : 1, Color = i == 1 ? SKColors.Transparent : new SKColor(226, 232, 240) },
                        Text = new TextStyle { Color = i == 1 ? SKColors.White : new SKColor(71, 85, 105), Size = 11, Weight = 700, Alignment = TextAlign.Center }
                    },
                    Transform = new Transform(x, y, mixBtnW, mixBtnH) { Anchor = Anchor.Top | Anchor.Left }
                };

                float rate = mixRates[i];
                int index = i;
                btn.Events.OnMouseDown += (s, e) =>
                {
                    SelectMixRate(index, rate);
                };
                btn.Events.OnMouseEnter += (s) =>
                {
                    if (_currentMixRate != mixRates[index])
                    {
                        btn.Style.BackColor = new SKColor(248, 250, 252);
                        btn.Style.Border.Color = new SKColor(203, 213, 225);
                    }
                    else
                    {
                        btn.Style.BackColor = new SKColor(30, 41, 59); // slate-800
                    }
                    btn.ScheduleRender();
                };
                btn.Events.OnMouseLeave += (s) =>
                {
                    if (_currentMixRate != mixRates[index])
                    {
                        btn.Style.BackColor = new SKColor(255, 255, 255);
                        btn.Style.Border.Color = new SKColor(226, 232, 240);
                    }
                    else
                    {
                        btn.Style.BackColor = new SKColor(9, 9, 11);
                    }
                    btn.ScheduleRender();
                };

                sidebar.AddChild(btn);
                _mixIndicators[i] = btn;
            }

            // Brush Type Selection (MARKER / BRUSH / MIXER)
            float brushModeHeaderY = 550f;
            var brushModeHeader = new VisualElement
            {
                Name = "BrushModeHeader",
                Text = "BRUSH TYPE",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(100, 116, 139), Size = 12, Weight = 700, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform(0, brushModeHeaderY, sidebarWidth, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            sidebar.AddChild(brushModeHeader);

            float brushModeBtnW = 104f;
            float brushModeBtnH = 34f;
            float brushModeBtnStartX = 20f;
            float brushModeBtnY = brushModeHeaderY + 25f;
            float brushModeGapX = 12f;

            string[] brushModeLabels = { "MARKER", "BRUSH", "MIXER" };

            for (int i = 0; i < 3; i++)
            {
                float btnX = (i < 2) ? (brushModeBtnStartX + i * (brushModeBtnW + brushModeGapX)) : brushModeBtnStartX;
                float btnY = (i < 2) ? brushModeBtnY : (brushModeBtnY + brushModeBtnH + 10f);
                float btnW = (i < 2) ? brushModeBtnW : (sidebarWidth - 40f);

                var btn = new VisualElement
                {
                    Name = $"BrushModeBtn_{i}",
                    Text = brushModeLabels[i],
                    Style = new ElementStyle
                    {
                        BackColor = i == 0 ? new SKColor(9, 9, 11) : new SKColor(255, 255, 255),
                        Border = new BorderStyle { Roundness = 8, Width = i == 0 ? 0 : 1, Color = i == 0 ? SKColors.Transparent : new SKColor(226, 232, 240) },
                        Text = new TextStyle { Color = i == 0 ? SKColors.White : new SKColor(71, 85, 105), Size = 11, Weight = 700, Alignment = TextAlign.Left, Padding = 36 }
                    },
                    Transform = new Transform(btnX, btnY, btnW, brushModeBtnH) { Anchor = Anchor.Top | Anchor.Left }
                };

                var icon = new VisualElement
                {
                    Name = $"BrushModeBtn_Icon_{i}",
                    Style = new ElementStyle
                    {
                        BackColor = SKColors.Transparent,
                        Border = new BorderStyle { Width = 0, Color = SKColors.Transparent }
                    },
                    Transform = new Transform(12, 9, 16, 16) { FixedWidth = true, FixedHeight = true },
                    BackgroundImageTintColor = i == 0 ? SKColors.White : new SKColor(71, 85, 105),
                    BackgroundImageTintBlendMode = SKBlendMode.SrcIn
                };
                if (i == 0) icon.LoadSvgFromFile("assets/marker.svg");
                else if (i == 1) icon.LoadSvgFromFile("assets/brush.svg");
                else icon.LoadSvgFromFile("assets/mixer.svg");
                btn.AddChild(icon);
                _brushTypeIcons[i] = icon;

                int index = i;
                btn.Events.OnMouseDown += (s, e) =>
                {
                    SelectBrushType(index);
                };
                btn.Events.OnMouseEnter += (s) =>
                {
                    bool isActive = (index == 0 && !_isBrushMode && !_isMixerMode) ||
                                    (index == 1 && _isBrushMode) ||
                                    (index == 2 && _isMixerMode);
                    if (!isActive)
                    {
                        btn.Style.BackColor = new SKColor(248, 250, 252);
                        btn.Style.Border.Color = new SKColor(203, 213, 225);
                    }
                    else
                    {
                        btn.Style.BackColor = new SKColor(30, 41, 59); // slate-800
                    }
                    btn.ScheduleRender();
                };
                btn.Events.OnMouseLeave += (s) =>
                {
                    bool isActive = (index == 0 && !_isBrushMode && !_isMixerMode) ||
                                    (index == 1 && _isBrushMode) ||
                                    (index == 2 && _isMixerMode);
                    if (!isActive)
                    {
                        btn.Style.BackColor = new SKColor(255, 255, 255);
                        btn.Style.Border.Color = new SKColor(226, 232, 240);
                    }
                    else
                    {
                        btn.Style.BackColor = new SKColor(9, 9, 11);
                    }
                    btn.ScheduleRender();
                };

                sidebar.AddChild(btn);
                _brushTypeIndicators[i] = btn;
            }

            // Reset Canvas Button next to the back button, above it
            var clearBtn = new VisualElement
            {
                Name = "Btn_ClearCanvas",
                Text = "RESET CANVAS",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(255, 255, 255),
                    Border = new BorderStyle { Roundness = 8, Width = 1, Color = new SKColor(252, 165, 165) },
                    Text = new TextStyle { Color = new SKColor(220, 38, 38), Size = 12, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(20, Height - 110f, sidebarWidth - 40f, 38)
                {
                    Anchor = Anchor.Bottom | Anchor.Left | Anchor.Right
                }
            };
            clearBtn.Events.OnMouseEnter += (s) =>
            {
                clearBtn.Style.BackColor = new SKColor(254, 242, 242);
                clearBtn.Style.Border.Color = new SKColor(239, 68, 68);
                clearBtn.ScheduleRender();
            };
            clearBtn.Events.OnMouseLeave += (s) =>
            {
                clearBtn.Style.BackColor = new SKColor(255, 255, 255);
                clearBtn.Style.Border.Color = new SKColor(252, 165, 165);
                clearBtn.ScheduleRender();
            };
            sidebar.AddChild(clearBtn);

            // Simple Back Button at the bottom of the sidebar navigation
            var backBtn = new VisualElement
            {
                Name = "Btn_BackToDashboard",
                Text = "←  BACK TO DASHBOARD",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(255, 255, 255),
                    Border = new BorderStyle { Roundness = 8, Width = 1, Color = new SKColor(203, 213, 225) },
                    Text = new TextStyle { Color = new SKColor(71, 85, 105), Size = 11, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(20, Height - 60f, sidebarWidth - 40f, 38)
                {
                    Anchor = Anchor.Bottom | Anchor.Left | Anchor.Right
                }
            };
            backBtn.Events.OnMouseEnter += (s) =>
            {
                backBtn.Style.BackColor = new SKColor(248, 250, 252);
                backBtn.Style.Border.Color = new SKColor(100, 116, 139);
                backBtn.Style.Text.Color = new SKColor(15, 23, 42);
                backBtn.ScheduleRender();
            };
            backBtn.Events.OnMouseLeave += (s) =>
            {
                backBtn.Style.BackColor = new SKColor(255, 255, 255);
                backBtn.Style.Border.Color = new SKColor(203, 213, 225);
                backBtn.Style.Text.Color = new SKColor(71, 85, 105);
                backBtn.ScheduleRender();
            };
            backBtn.Events.OnMouseUp += (s, e) =>
            {
                OnSwitchToDashboard?.Invoke();
            };
            sidebar.AddChild(backBtn);

            // --- 2. MAIN CONTENT AREA (Flat, sleek dark canvas card) ---
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

            // Clean Frame for Canvas (no blurs, simple borders, white theme)
            float frameW = Width - sidebarWidth - 60f;
            float frameH = Height - 20f; // Expand container to use top space

            var canvasFrame = new VisualElement
            {
                Name = "CanvasFrame",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(255, 255, 255), // Flat white card background
                    Border = new BorderStyle { Roundness = 12, Width = 1, Color = new SKColor(226, 232, 240) }
                },
                Transform = new Transform(30, 10, frameW, frameH) // Starts at Y = 10
                {
                    Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
                }
            };
            mainContent.AddChild(canvasFrame);

            // Compute centered aspect ratio bounds (4:3 aspect ratio)
            float canvasMaxW = frameW - 40f;
            float canvasMaxH = frameH - 40f;
            float aspectRatio = 4f / 3f;

            float targetW = canvasMaxW;
            float targetH = canvasMaxW / aspectRatio;

            if (targetH > canvasMaxH)
            {
                targetH = canvasMaxH;
                targetW = canvasMaxH * aspectRatio;
            }

            float canvasX = (frameW - targetW) / 2f;
            float canvasY = (frameH - targetH) / 2f;

            // Responsive Aspect-Ratio Drawing Canvas with Background Paint Shader
            _drawingCanvas = new DrawingCanvas(targetW, targetH)
            {
                Name = "DrawingCanvas_Main",
                DrawColor = _paletteColors[0],
                BrushSize = 20f,
                ShaderMixingRate = _currentMixRate,
                IsBrushMode = _isBrushMode,
                Style = new ElementStyle
                {
                    BackgroundShader = BackgroundShaderType.LiquidPaint,
                    BackgroundShaderColor = SKColors.Transparent,
                    ShaderRenderMode = EffectRenderMode.Continuous,
                    Border = new BorderStyle { Roundness = 8, Width = 1, Color = new SKColor(226, 232, 240) }
                },
                Transform = new Transform(canvasX, canvasY, targetW, targetH)
                {
                    Anchor = Anchor.Left | Anchor.Top // Position explicitly relative to Top-Left of parent frame
                }
            };
            _drawingCanvas.Clear(SKColors.Transparent); // Clear to transparent
            canvasFrame.AddChild(_drawingCanvas);

            // Wire reset canvas button to also reset layouts and backing bitmap in case window size changed
            clearBtn.Events.OnMouseUp += (s, e) =>
            {
                if (_drawingCanvas != null)
                {
                    ForceLayoutEvaluation();
                    UpdateCanvasLayout(canvasFrame.Transform.Computed.Width, canvasFrame.Transform.Computed.Height);
                    _drawingCanvas.RecreateBackingBitmap(_drawingCanvas.Transform.Width, _drawingCanvas.Transform.Height);
                }
            };

            float lastFrameW = 0;
            float lastFrameH = 0;
            this.Loop += () =>
            {
                if (canvasFrame.Transform.Computed.Width != lastFrameW || canvasFrame.Transform.Computed.Height != lastFrameH)
                {
                    lastFrameW = canvasFrame.Transform.Computed.Width;
                    lastFrameH = canvasFrame.Transform.Computed.Height;
                    UpdateCanvasLayout(lastFrameW, lastFrameH);
                }
            };

            SelectBrushType(0);
        }

        private void UpdateCanvasLayout(float frameW, float frameH)
        {
            if (_drawingCanvas == null) return;

            // Compute centered aspect ratio bounds (4:3 aspect ratio)
            float canvasMaxW = frameW - 40f;
            float canvasMaxH = frameH - 40f;
            float aspectRatio = 4f / 3f;

            float targetW = canvasMaxW;
            float targetH = canvasMaxW / aspectRatio;

            if (targetH > canvasMaxH)
            {
                targetH = canvasMaxH;
                targetW = canvasMaxH * aspectRatio;
            }

            float canvasX = (frameW - targetW) / 2f;
            float canvasY = (frameH - targetH) / 2f;

            // Update drawing canvas transform to the new centered, aspect-ratio-locked dimensions.
            // Note: X and Y setters expect global coordinates, so we add the parent's global computed position.
            var parent = _drawingCanvas.Transform.Parent;
            float parentX = parent != null ? parent.Computed.X : 0;
            float parentY = parent != null ? parent.Computed.Y : 0;

            _drawingCanvas.Transform.X = parentX + canvasX;
            _drawingCanvas.Transform.Y = parentY + canvasY;
            _drawingCanvas.Transform.Width = targetW;
            _drawingCanvas.Transform.Height = targetH;
        }

        private void SelectColor(int index)
        {
            if (index < 0 || index >= _paletteColors.Length || _drawingCanvas == null) return;

            var color = _paletteColors[index];
            _drawingCanvas.DrawColor = color;

            if (_eraserCheckbox != null && _eraserCheckbox.IsChecked)
            {
                _eraserCheckbox.IsChecked = false;
            }

            if (_selectedColorText != null)
            {
                _selectedColorText.Text = $"Active: {_paletteNames[index]}";
                _selectedColorText.Style.Text.Color = (color == new SKColor(255, 255, 255)) ? new SKColor(71, 85, 105) : color;
            }

            for (int i = 0; i < _colorIndicators.Length; i++)
            {
                var ind = _colorIndicators[i];
                if (ind == null) continue;

                if (i == index)
                {
                    ind.Style.Border.Width = 3;
                    ind.Style.Border.Color = new SKColor(15, 23, 42);
                }
                else
                {
                    ind.Style.Border.Width = 1;
                    ind.Style.Border.Color = new SKColor(226, 232, 240);
                }
                ind.ScheduleRender();
            }
        }

        private void SelectMixRate(int btnIndex, float rate)
        {
            _currentMixRate = rate;
            if (_drawingCanvas != null)
            {
                _drawingCanvas.ShaderMixingRate = rate;
            }

            for (int i = 0; i < 4; i++)
            {
                var ind = _mixIndicators[i];
                if (ind == null) continue;

                if (i == btnIndex)
                {
                    ind.Style.BackColor = new SKColor(9, 9, 11); // Midnight Black
                    ind.Style.Border.Color = SKColors.Transparent;
                    ind.Style.Border.Width = 0;
                    ind.Style.Text.Color = SKColors.White;
                }
                else
                {
                    ind.Style.BackColor = new SKColor(255, 255, 255);
                    ind.Style.Border.Color = new SKColor(226, 232, 240);
                    ind.Style.Border.Width = 1;
                    ind.Style.Text.Color = new SKColor(71, 85, 105);
                }
                ind.ScheduleRender();
            }
        }

        private void SelectBrushType(int btnIndex)
        {
            _isBrushMode = (btnIndex == 1);
            _isMixerMode = (btnIndex == 2);
            if (_drawingCanvas != null)
            {
                _drawingCanvas.IsBrushMode = _isBrushMode;
                _drawingCanvas.IsMixerMode = _isMixerMode;
            }

            for (int i = 0; i < 3; i++)
            {
                var ind = _brushTypeIndicators[i];
                if (ind == null) continue;

                var icon = _brushTypeIcons[i];

                if (i == btnIndex)
                {
                    ind.Style.BackColor = new SKColor(9, 9, 11); // Midnight Black
                    ind.Style.Border.Color = SKColors.Transparent;
                    ind.Style.Border.Width = 0;
                    ind.Style.Text.Color = SKColors.White;
                    if (icon != null)
                    {
                        icon.BackgroundImageTintColor = SKColors.White;
                        icon.ScheduleRender();
                    }
                }
                else
                {
                    ind.Style.BackColor = new SKColor(255, 255, 255);
                    ind.Style.Border.Color = new SKColor(226, 232, 240);
                    ind.Style.Border.Width = 1;
                    ind.Style.Text.Color = new SKColor(71, 85, 105);
                    if (icon != null)
                    {
                        icon.BackgroundImageTintColor = new SKColor(71, 85, 105);
                        icon.ScheduleRender();
                    }
                }
                ind.ScheduleRender();
            }

            // Adjust Mix / Strength panel based on mode
            float mixBtnW = 104f;
            float mixBtnH = 34f;
            float mixBtnStartX = 20f;
            float mixHeaderY = 430f;
            float mixBtnStartY = mixHeaderY + 25f;
            float mixGapX = 12f;
            float mixGapY = 12f;

            if (_isMixerMode)
            {
                if (_mixHeader != null) _mixHeader.Text = "MIXER STRENGTH";
                
                // Hide button 0 ("NONE")
                if (_mixIndicators[0] != null) _mixIndicators[0].Visible = false;

                // Adjust positions and text of buttons 1, 2, 3
                string[] mixerLabels = { "", "LOW", "MEDIUM", "HIGH" };
                float mixerBtnW = 65f;
                float mixerGap = 12.5f;
                float startX = 20f;
                
                for (int i = 1; i < 4; i++)
                {
                    var btn = _mixIndicators[i];
                    if (btn != null)
                    {
                        btn.Visible = true;
                        btn.Text = mixerLabels[i];
                        btn.Transform.X = startX + (i - 1) * (mixerBtnW + mixerGap);
                        btn.Transform.Y = mixBtnStartY;
                        btn.Transform.Width = mixerBtnW;
                        btn.Transform.Height = mixBtnH;
                        btn.Style.Text.Padding = 0;
                    }
                }
                
                // If mix rate was 0, select Medium (index 2)
                if (_currentMixRate == 0f)
                {
                    SelectMixRate(2, 0.55f);
                }
                else
                {
                    // Update selection style
                    int activeIdx = 2; // Default to medium
                    if (_currentMixRate == 0.25f) activeIdx = 1;
                    else if (_currentMixRate == 0.85f) activeIdx = 3;
                    SelectMixRate(activeIdx, _currentMixRate);
                }
            }
            else
            {
                if (_mixHeader != null) _mixHeader.Text = "COLOR MIXING";
                
                // Show button 0 ("NONE")
                if (_mixIndicators[0] != null)
                {
                    _mixIndicators[0].Visible = true;
                    _mixIndicators[0].Text = "NONE";
                    _mixIndicators[0].Transform.X = mixBtnStartX;
                    _mixIndicators[0].Transform.Y = mixBtnStartY;
                    _mixIndicators[0].Transform.Width = mixBtnW;
                    _mixIndicators[0].Transform.Height = mixBtnH;
                }

                string[] mixLabels = { "NONE", "SLOW", "MEDIUM", "FAST" };
                for (int i = 1; i < 4; i++)
                {
                    var btn = _mixIndicators[i];
                    if (btn != null)
                    {
                        btn.Visible = true;
                        btn.Text = mixLabels[i];
                        int row = i / 2;
                        int col = i % 2;
                        btn.Transform.X = mixBtnStartX + col * (mixBtnW + mixGapX);
                        btn.Transform.Y = mixBtnStartY + row * (mixBtnH + mixGapY);
                        btn.Transform.Width = mixBtnW;
                        btn.Transform.Height = mixBtnH;
                        btn.Style.Text.Padding = 0;
                    }
                }

                // Update selection style
                int activeIdx = 1; // Default to SLOW
                if (_currentMixRate == 0f) activeIdx = 0;
                else if (_currentMixRate == 0.55f) activeIdx = 2;
                else if (_currentMixRate == 0.85f) activeIdx = 3;
                SelectMixRate(activeIdx, _currentMixRate);
            }
        }
    }
}
