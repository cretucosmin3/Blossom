using System;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;
using Blossom.Core.Input;
using Blossom.Testing.Components;
using System.Collections.Generic;

namespace Blossom.Testing.Views
{
    public class DashboardView : View
    {
        public Action? OnSwitchView;
        public Action? OnSwitchToNeon;
        public Action? OnSwitchToPaint;
        public Action? OnSwitchToKanban;
        public Action? OnSwitchTo3D;
        public Action? OnSwitchToGlass;

        // Interactive Fields
        private VisualElement? _mainContent;
        private VisualElement? _clockContainer;
        private VisualElement? _clockText;
        private VisualElement? _heroCard;
        private DynamicWaveformChart? _waveformChart;
        private VisualElement? _goalsPanel;
        
        private LiveTelemetryCard? _card1;
        private LiveTelemetryCard? _card2;
        private LiveTelemetryCard? _card3;

        private VisualElement? _imageCard1;
        private VisualElement? _imageCard2;
        private VisualElement? _imageCard3;

        private float _animTime = 0f;
        private float _lastWidth = 0f;
        private float _lastHeight = 0f;
        private DateTime _lastTime = DateTime.Now;

        public DashboardView() : base("Web Dashboard")
        {
            // Sleek midnight background color
            BackColor = new SKColor(11, 14, 22);
        }

        public override void Init()
        {
            float sidebarWidth = 260f;

            // --- 1. SIDEBAR ---
            var sidebar = new VisualElement
            {
                Name = "Sidebar",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(16, 20, 30, 240), // Darker slate sidebar
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

            // Brand Label with subtle neon accent (shifted down to prevent overlap)
            var brand = new VisualElement
            {
                Name = "Sidebar_Brand",
                Text = "⚡ BLOSSOM OS",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 22, Weight = 800, Alignment = TextAlign.Center, Padding = 15 }
                },
                Transform = new Transform(0, 50, sidebarWidth, 60) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            sidebar.AddChild(brand);

            // Navigation Menu Items (shifted down to start at Y = 130f)
            string[] menuItems = { "Overview", "Neon Showcase", "Neon Paint", "Task Board", "3D Showcase", "Glass Showcase" };
            float menuY = 130f;
            for (int i = 0; i < menuItems.Length; i++)
            {
                var item = menuItems[i];
                var btn = new SidebarButton(item, i == 0) // Overview is active (i == 0)
                {
                    Transform = { X = 20, Y = menuY, Width = sidebarWidth - 40 }
                };

                int idx = i;
                btn.OnClick = () =>
                {
                    if (idx == 1) OnSwitchToNeon?.Invoke();
                    else if (idx == 2) OnSwitchToPaint?.Invoke();
                    else if (idx == 3) OnSwitchToKanban?.Invoke();
                    else if (idx == 4) OnSwitchTo3D?.Invoke();
                    else if (idx == 5) OnSwitchToGlass?.Invoke();
                };

                sidebar.AddChild(btn);
                menuY += 55f;
            }

            // Quick Switch Button
            var switchBtn = new Button("Switch View ➜", new SKColor(79, 70, 229))
            {
                Name = "Sidebar_SwitchBtn",
                Transform = new Transform(20, menuY + 15, sidebarWidth - 40, 42) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            switchBtn.Style.Text.Color = SKColors.White;
            switchBtn.Style.Text.Size = 13;
            switchBtn.Style.Border.Roundness = 8;
            switchBtn.OnClick = () => OnSwitchToNeon?.Invoke();
            sidebar.AddChild(switchBtn);

            // Sidebar Footer Profile
            var footer = new VisualElement
            {
                Name = "Sidebar_Footer",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(22, 28, 41, 150),
                    Border = new BorderStyle { Roundness = 8, Width = 1, Color = new SKColor(255, 255, 255, 8) }
                },
                Transform = new Transform(20, Height - 85, sidebarWidth - 40, 65)
                {
                    Anchor = Anchor.Bottom | Anchor.Left | Anchor.Right,
                    FixedHeight = true
                }
            };
            sidebar.AddChild(footer);

            footer.AddChild(new VisualElement
            {
                Name = "Footer_Avatar",
                Style = new ElementStyle { BackColor = new SKColor(56, 189, 248), Border = new BorderStyle { Roundness = 16 } },
                Transform = new Transform(12, 16, 32, 32) { FixedSize = true, Anchor = Anchor.Left }
            });

            footer.AddChild(new VisualElement
            {
                Name = "Footer_Username",
                Text = "Kozmo Admin",
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.White, Size = 12, Weight = 700, Padding = 15, Alignment = TextAlign.Left } },
                Transform = new Transform(44, 10, sidebarWidth - 100, 25) { Anchor = Anchor.Left | Anchor.Right }
            });

            var statusDot = new VisualElement
            {
                Name = "Footer_StatusDot",
                Style = new ElementStyle { BackColor = new SKColor(16, 185, 129), Border = new BorderStyle { Roundness = 3.5f } },
                Transform = new Transform(48, 38, 7, 7) { FixedSize = true, Anchor = Anchor.Left }
            };
            footer.AddChild(statusDot);

            footer.AddChild(new VisualElement
            {
                Name = "Footer_StatusText",
                Text = "SYSTEM ONLINE",
                Style = new ElementStyle { Text = new TextStyle { Color = new SKColor(16, 185, 129), Size = 9, Weight = 600, Padding = 12, Alignment = TextAlign.Left } },
                Transform = new Transform(58, 32, 100, 20) { Anchor = Anchor.Left }
            });


            // --- 2. MAIN CONTENT AREA ---
            _mainContent = new VisualElement
            {
                Name = "MainContent",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(sidebarWidth, 0, Width - sidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right | Anchor.Bottom
                }
            };
            AddElement(_mainContent);

            // Header Area
            var header = new VisualElement
            {
                Name = "Header",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(0, 0, Width - sidebarWidth, 80)
                {
                    FixedHeight = true,
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            _mainContent.AddChild(header);

            // Breadcrumb Header Title (aligned Left)
            var titleContainer = new VisualElement
            {
                Name = "Header_TitleContainer",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(30, 18, 300, 44) { Anchor = Anchor.Top | Anchor.Left }
            };
            header.AddChild(titleContainer);

            titleContainer.AddChild(new VisualElement
            {
                Name = "Header_Title",
                Text = "DIAGNOSTICS HUB",
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.White, Size = 20, Weight = 800, Alignment = TextAlign.Left } },
                Transform = new Transform(0, 0, 300, 24) { Anchor = Anchor.Top | Anchor.Left }
            });

            titleContainer.AddChild(new VisualElement
            {
                Name = "Header_Sub",
                Text = "TELEMETRY & SIMULATION PANEL",
                Style = new ElementStyle { Text = new TextStyle { Color = new SKColor(100, 116, 139), Size = 9, Weight = 600, Alignment = TextAlign.Left } },
                Transform = new Transform(0, 24, 300, 20) { Anchor = Anchor.Top | Anchor.Left }
            });

            // Live Clock Widget (aligned Right)
            _clockContainer = new VisualElement
            {
                Name = "Header_ClockContainer",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59, 100),
                    Border = new BorderStyle { Roundness = 8, Width = 1, Color = new SKColor(255, 255, 255, 10) }
                },
                Transform = new Transform(Width - sidebarWidth - 190f, 18, 160, 44)
                {
                    Anchor = Anchor.Top | Anchor.Right,
                    FixedWidth = true,
                    FixedHeight = true
                }
            };
            header.AddChild(_clockContainer);

            _clockText = new VisualElement
            {
                Name = "Header_ClockText",
                Text = "00:00:00",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 16, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(0, 0, 160, 44) { Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right }
            };
            _clockContainer.AddChild(_clockText);


            // --- 2.5 SCROLLABLE CONTAINER ---
            var scrollArea = new ScrollContainer
            {
                Name = "DashboardScrollArea",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(0, 80, Width - sidebarWidth, Height - 80)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
                }
            };
            _mainContent.AddChild(scrollArea);


            // --- 3. HERO BANNER CARD ---
            _heroCard = new VisualElement
            {
                Name = "Hero",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(79, 70, 229, 220), // Glowing Indigo
                    Border = new BorderStyle { Roundness = 16 },
                    Shadow = new ShadowStyle { Color = new SKColor(79, 70, 229).WithAlpha(100), SpreadY = 12, OffsetY = 6 }
                },
                Transform = new Transform(30, 15, Width - sidebarWidth - 60f, 150)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right,
                    FixedHeight = true
                }
            };
            scrollArea.AddChild(_heroCard);

            _heroCard.AddChild(new VisualElement
            {
                Name = "Hero_Title",
                Text = "Engine Telemetry Active",
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.White, Size = 24, Weight = 800, Alignment = TextAlign.Left, Padding = 25 } },
                Transform = new Transform(0, 15, 600, 40)
            });

            _heroCard.AddChild(new VisualElement
            {
                Name = "Hero_Subtitle",
                Text = "Experience multi-threaded SkiaSharp rendering running at high framework FPS.",
                Style = new ElementStyle { Text = new TextStyle { Color = new SKColor(224, 231, 255), Size = 13, Alignment = TextAlign.Left, Padding = 25 } },
                Transform = new Transform(0, 50, 700, 30)
            });

            var ctaBtn = new Button("TELEMETRY CANVAS", SKColors.White)
            {
                Name = "Hero_Btn",
                Transform = { X = 25, Y = 95, Width = 180, Height = 36 },
                OnClick = () => OnSwitchToNeon?.Invoke()
            };
            ctaBtn.Style.Text.Color = new SKColor(79, 70, 229);
            ctaBtn.Style.Border.Roundness = 8;
            _heroCard.AddChild(ctaBtn);


            // --- 4. METRICS / TELEMETRY GRID ---
            _card1 = new LiveTelemetryCard("Render Efficiency", "98.5%", "ONLINE", new SKColor(16, 185, 129))
            {
                Transform = { Y = 195f }
            };
            scrollArea.AddChild(_card1);

            _card2 = new LiveTelemetryCard("Heap Memory", "4.20 GB", "8.00 GB MAX", new SKColor(56, 189, 248))
            {
                Transform = { Y = 195f }
            };
            scrollArea.AddChild(_card2);

            _card3 = new LiveTelemetryCard("Frame Budget", "85%", "STABLE", new SKColor(244, 63, 94))
            {
                Transform = { Y = 195f }
            };
            scrollArea.AddChild(_card3);


            // --- 5. ANALYTICS: OSCILLOSCOPE CHART & DIAGNOSTIC PANEL ---
            _waveformChart = new DynamicWaveformChart("System Oscilloscope Waveform", new SKColor(56, 189, 248))
            {
                Transform = new Transform(30, 350f, 400f, 320f) { Anchor = Anchor.Top | Anchor.Left }
            };
            scrollArea.AddChild(_waveformChart);

            _goalsPanel = new VisualElement
            {
                Name = "GoalsPanel",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(22, 28, 41, 200),
                    Border = new BorderStyle { Roundness = 16, Width = 1, Color = new SKColor(255, 255, 255, 12) },
                    Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(50), SpreadY = 5, OffsetY = 5 }
                },
                Transform = new Transform(0, 350f, 300f, 320f) { Anchor = Anchor.Top | Anchor.Left }
            };
            scrollArea.AddChild(_goalsPanel);

            _goalsPanel.AddChild(new VisualElement
            {
                Name = "Goals_Title",
                Text = "Diagnostics Overview",
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.White, Size = 15, Weight = 700, Alignment = TextAlign.Left, Padding = 20 } },
                Transform = new Transform(0, 0, 300, 40) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Sales Goal Progress
            _goalsPanel.AddChild(new VisualElement
            {
                Name = "G1_Label",
                Text = "Render Thread Budget (85%)",
                Style = new ElementStyle { Text = new TextStyle { Color = new SKColor(148, 163, 184), Size = 12, Alignment = TextAlign.Left, Padding = 20 } },
                Transform = new Transform(0, 40, 300, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });
            _goalsPanel.AddChild(new ProgressBar(0.85f, new SKColor(16, 185, 129))
            {
                Name = "Goal1_Bar",
                Transform = new Transform(20, 65, 260, 8) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Signup Goal Progress
            _goalsPanel.AddChild(new VisualElement
            {
                Name = "G2_Label",
                Text = "QuadTree Occupancy (58%)",
                Style = new ElementStyle { Text = new TextStyle { Color = new SKColor(148, 163, 184), Size = 12, Alignment = TextAlign.Left, Padding = 20 } },
                Transform = new Transform(0, 95, 300, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });
            _goalsPanel.AddChild(new ProgressBar(0.58f, new SKColor(56, 189, 248))
            {
                Name = "Goal2_Bar",
                Transform = new Transform(20, 120, 260, 8) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Server Goal Progress
            _goalsPanel.AddChild(new VisualElement
            {
                Name = "G3_Label",
                Text = "Asset Cache Overhead (92%)",
                Style = new ElementStyle { Text = new TextStyle { Color = new SKColor(148, 163, 184), Size = 12, Alignment = TextAlign.Left, Padding = 20 } },
                Transform = new Transform(0, 150, 300, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });
            _goalsPanel.AddChild(new ProgressBar(0.92f, new SKColor(244, 63, 94))
            {
                Name = "Goal3_Bar",
                Transform = new Transform(20, 175, 260, 8) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Trigger Optimize Button
            var optimizeBtn = new Button("TRIGGER OPTIMIZATION", new SKColor(30, 41, 59))
            {
                Name = "Goal_OptimizeBtn",
                Transform = new Transform(20, 230, 260, 42) { Anchor = Anchor.Bottom | Anchor.Left | Anchor.Right }
            };
            optimizeBtn.Style.Text.Color = new SKColor(56, 189, 248);
            optimizeBtn.Style.Border.Color = new SKColor(56, 189, 248, 100);
            optimizeBtn.Style.Border.Width = 1;
            optimizeBtn.OnClick = () => Console.WriteLine("Triggering UI optimization...");
            _goalsPanel.AddChild(optimizeBtn);

            // --- 6. IMAGE SHOWCASE GRID ---
            var imgStyle1 = new ElementStyle
            {
                BackColor = new SKColor(30, 27, 75), // Dark indigo background for empty zones
                Border = new BorderStyle { Roundness = 16, Width = 1, Color = new SKColor(255, 255, 255, 20) },
                Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(60), SpreadY = 8, OffsetY = 4 }
            };

            var imgStyle2 = new ElementStyle
            {
                BackColor = new SKColor(30, 27, 75),
                Border = new BorderStyle { Roundness = 16, Width = 1, Color = new SKColor(255, 255, 255, 20) },
                Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(60), SpreadY = 8, OffsetY = 4 }
            };

            var imgStyle3 = new ElementStyle
            {
                BackColor = new SKColor(30, 27, 75),
                Border = new BorderStyle { Roundness = 16, Width = 1, Color = new SKColor(255, 255, 255, 20) },
                Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(60), SpreadY = 8, OffsetY = 4 }
            };

            _imageCard1 = new VisualElement
            {
                Name = "ImageCard1",
                Style = imgStyle1,
                Transform = new Transform(0, 710f, 200, 240f) { Anchor = Anchor.Top | Anchor.Left },
                BackgroundImageScale = ImageScaleMode.Contain,
                BackgroundImageTintColor = new SKColor(56, 189, 248, 255),
                BackgroundImageTintBlendMode = SKBlendMode.SrcIn
            };

            _imageCard1.LoadSvgFromUrl("https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/check.svg");
            scrollArea.AddChild(_imageCard1);

            _imageCard1.AddChild(new VisualElement
            {
                Name = "Image1_Badge",
                Text = "SVG + SOLID BLUE TINT",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(15, 23, 42, 180), // Glassmorphic slate
                    Border = new BorderStyle { Roundness = 6 },
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 10, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(12, 12, 170, 24) { Anchor = Anchor.Top | Anchor.Left }
            });

            _imageCard2 = new VisualElement
            {
                Name = "ImageCard2",
                Style = imgStyle2,
                Transform = new Transform(0, 710f, 200, 240f) { Anchor = Anchor.Top | Anchor.Left }
            };
            _imageCard2.BackgroundImageScale = ImageScaleMode.Cover;
            _imageCard2.BackgroundImageBlur = 8f; // Enable Blur effect
            _imageCard2.LoadImageFromUrl("https://picsum.photos/id/48/600/400");
            scrollArea.AddChild(_imageCard2);

            _imageCard2.AddChild(new VisualElement
            {
                Name = "Image2_Badge",
                Text = "COVER + BLUR (8px)",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(15, 23, 42, 180),
                    Border = new BorderStyle { Roundness = 6 },
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 10, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(12, 12, 170, 24) { Anchor = Anchor.Top | Anchor.Left }
            });

            _imageCard3 = new VisualElement
            {
                Name = "ImageCard3",
                Style = imgStyle3,
                Transform = new Transform(0, 710f, 200, 240f) { Anchor = Anchor.Top | Anchor.Left }
            };
            _imageCard3.BackgroundImageScale = ImageScaleMode.Stretch;
            _imageCard3.BackgroundImageTintColor = new SKColor(56, 189, 248, 80); // Enable Sky-blue Tint overlay
            _imageCard3.LoadImageFromUrl("https://picsum.photos/id/60/600/400");
            scrollArea.AddChild(_imageCard3);

            _imageCard3.AddChild(new VisualElement
            {
                Name = "Image3_Badge",
                Text = "STRETCH + BLUE TINT",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(15, 23, 42, 180),
                    Border = new BorderStyle { Roundness = 6 },
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 10, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(12, 12, 170, 24) { Anchor = Anchor.Top | Anchor.Left }
            });

            // Execute initial layout solving
            LayoutGrid();
        }

        public override void OnActivated()
        {
            _lastTime = DateTime.Now;
            Loop += UpdateDashboard;
        }

        public override void OnDeactivated()
        {
            Loop -= UpdateDashboard;
        }

        private void UpdateDashboard()
        {
            var now = DateTime.Now;
            float dt = (float)(now - _lastTime).TotalSeconds;
            _lastTime = now;

            // Clamp delta time to avoid large jumps during loading or lag spikes
            if (dt > 0.1f) dt = 0.1f;

            // Update anim time relative to actual elapsed time (e.g. 1.0 units per second)
            _animTime += dt * 1.0f;

            // 1. Live digital clock
            if (_clockText != null)
            {
                _clockText.Text = DateTime.Now.ToString("HH:mm:ss");
            }

            // 2. Oscilloscope wave update (rate-limited based on delta time)
            if (_waveformChart != null)
            {
                _waveformChart.UpdateWave(dt * 2.0f);
            }

            // 3. Fluctuating telemetry card stats (smooth oscillations)
            float effVal = 0.97f + 0.025f * (float)Math.Sin(_animTime * 0.6f);
            if (_card1 != null)
            {
                _card1.SetProgress(effVal, $"{effVal * 100f:F1}%");
            }

            float memLoad = 4.10f + 0.35f * (float)Math.Sin(_animTime * 0.3f);
            if (_card2 != null)
            {
                _card2.SetProgress(memLoad / 8.0f, $"{memLoad:F2} GB");
            }

            float frameVal = 70f + 25f * (float)Math.Sin(_animTime * 0.8f + Math.Cos(_animTime * 0.4f));
            if (_card3 != null)
            {
                _card3.SetProgress(frameVal / 100f, $"{frameVal:F0}%");
            }

            // 4. Responsive Grid layout solver
            if (Width != _lastWidth || Height != _lastHeight)
            {
                _lastWidth = Width;
                _lastHeight = Height;
                LayoutGrid();
            }

            // Mark view dirty to trigger redraw on main event loop
            RenderRequired = true;
            Silk.NET.GLFW.GlfwProvider.GLFW.Value.PostEmptyEvent();
        }

        private void LayoutGrid()
        {
            if (_mainContent == null) return;

            float sidebarWidth = 260f;
            float totalWidth = Width - sidebarWidth;

            // Clock element placement (locked to the top-right corner)
            if (_clockContainer != null)
            {
                _clockContainer.Transform.X = sidebarWidth + totalWidth - 180f;
            }

            // Hero element scaling
            float padding = 30f;
            if (_heroCard != null)
            {
                _heroCard.Transform.X = sidebarWidth + padding;
                _heroCard.Transform.Width = totalWidth - (padding * 2f);
            }

            // Responsive 3-column calculation
            float gap = 20f;
            float availableWidth = totalWidth - (padding * 2f) - (gap * 2f);
            float cardWidth = availableWidth / 3f;

            if (_card1 != null)
            {
                _card1.Transform.X = sidebarWidth + padding;
                _card1.Transform.Width = cardWidth;
            }
            if (_card2 != null)
            {
                _card2.Transform.X = sidebarWidth + padding + cardWidth + gap;
                _card2.Transform.Width = cardWidth;
            }
            if (_card3 != null)
            {
                _card3.Transform.X = sidebarWidth + padding + (cardWidth + gap) * 2f;
                _card3.Transform.Width = cardWidth;
            }

            // Responsive image cards positioning
            if (_imageCard1 != null)
            {
                _imageCard1.Transform.X = sidebarWidth + padding;
                _imageCard1.Transform.Width = cardWidth;
            }
            if (_imageCard2 != null)
            {
                _imageCard2.Transform.X = sidebarWidth + padding + cardWidth + gap;
                _imageCard2.Transform.Width = cardWidth;
            }
            if (_imageCard3 != null)
            {
                _imageCard3.Transform.X = sidebarWidth + padding + (cardWidth + gap) * 2f;
                _imageCard3.Transform.Width = cardWidth;
            }

            // Waveform chart and Goals panel alignment
            float goalsWidth = 300f;
            float chartWidth = totalWidth - (padding * 2f) - gap - goalsWidth;

            if (_waveformChart != null)
            {
                _waveformChart.Transform.X = sidebarWidth + padding;
                _waveformChart.Transform.Width = chartWidth;
            }
            if (_goalsPanel != null)
            {
                _goalsPanel.Transform.X = sidebarWidth + padding + chartWidth + gap;
                _goalsPanel.Transform.Width = goalsWidth;
            }
        }
    }

    // --- SUPPORTING UI COMPONENTS ---

    /// <summary>
    /// Sleek navigation menu button for the glassmorphic sidebar.
    /// </summary>
    public class SidebarButton : VisualElement
    {
        public Action? OnClick;
        private readonly SKColor _hoverColor = new SKColor(255, 255, 255, 12);
        private readonly SKColor _activeColor = new SKColor(255, 255, 255, 20);
        private readonly bool _isActive;

        public SidebarButton(string text, bool isActive)
        {
            Name = $"SidebarBtn_{text}";
            Text = text;
            _isActive = isActive;

            Style = new ElementStyle
            {
                BackColor = _isActive ? _activeColor : SKColors.Transparent,
                Border = new BorderStyle { Roundness = 8 },
                Text = new TextStyle
                {
                    Color = _isActive ? new SKColor(56, 189, 248) : new SKColor(148, 163, 184), // Sky 400 or Slate 400
                    Size = 14,
                    Weight = _isActive ? 700 : 500,
                    Alignment = TextAlign.Left,
                    Padding = 16
                }
            };

            Transform = new Transform(0, 0, 220, 45)
            {
                Anchor = Anchor.Top | Anchor.Left | Anchor.Right,
                FixedHeight = true
            };

            // Subtle neon left bar indicator for the active item
            if (_isActive)
            {
                var bar = new VisualElement
                {
                    Name = $"{Name}_Bar",
                    Style = new ElementStyle { BackColor = new SKColor(56, 189, 248) },
                    Transform = new Transform(0, 8, 3, 29) { Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left, FixedWidth = true }
                };
                AddChild(bar);
            }

            Events.OnMouseEnter += (s) =>
            {
                if (!_isActive)
                {
                    Style.BackColor = _hoverColor;
                    Style.Text.Color = SKColors.White;
                }
                Transform.ScaleX = 1.05f;
                Transform.ScaleY = 1.05f;
            };

            Events.OnMouseLeave += (s) =>
            {
                if (!_isActive)
                {
                    Style.BackColor = SKColors.Transparent;
                    Style.Text.Color = new SKColor(148, 163, 184);
                }
                Transform.ScaleX = 1.0f;
                Transform.ScaleY = 1.0f;
            };

            Events.OnMouseDown += (s, e) =>
            {
                Transform.ScaleX = 0.98f;
                Transform.ScaleY = 0.98f;
            };

            Events.OnMouseUp += (s, e) =>
            {
                Transform.ScaleX = 1.05f;
                Transform.ScaleY = 1.05f;
                OnClick?.Invoke();
            };
        }
    }

    /// <summary>
    /// Live Telemetry Card component that supports dynamic visual value updates.
    /// </summary>
    public class LiveTelemetryCard : VisualElement
    {
        private readonly SKColor _accentColor;
        private readonly VisualElement _progressBar;
        private readonly VisualElement _valEl;
        private readonly VisualElement _fillEl;
        private float _valPercent = 0.5f;

        public LiveTelemetryCard(string title, string initialValue, string unit, SKColor accentColor)
        {
            _accentColor = accentColor;
            Name = $"TelemetryCard_{title.Replace(" ", "_")}";

            Style = new ElementStyle
            {
                BackColor = new SKColor(22, 28, 41, 180), // Glassmorphic card
                Border = new BorderStyle
                {
                    Roundness = 12,
                    Width = 1,
                    Color = new SKColor(255, 255, 255, 12)
                },
                Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(50), SpreadY = 4, OffsetY = 4 }
            };

            Transform = new Transform(0, 0, 280, 130)
            {
                FixedHeight = true,
                Anchor = Anchor.Top | Anchor.Left
            };

            // Title - aligned Left so it doesn't move with resize
            AddChild(new VisualElement
            {
                Name = $"{Name}_Title",
                Text = title.ToUpper(),
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(148, 163, 184), Size = 10, Weight = 700, Alignment = TextAlign.Left, Padding = 18 }
                },
                Transform = new Transform(0, 0, 280, 30) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Value - aligned Left
            _valEl = new VisualElement
            {
                Name = $"{Name}_Value",
                Text = initialValue,
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = SKColors.White, Size = 28, Weight = 800, Alignment = TextAlign.Left, Padding = 18 }
                },
                Transform = new Transform(0, 25, 280, 45) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            };
            AddChild(_valEl);

            // Unit/Trend Badge (locked to Right)
            AddChild(new VisualElement
            {
                Name = $"{Name}_Unit",
                Text = unit,
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = accentColor, Size = 11, Weight = 700, Alignment = TextAlign.Right, Padding = 18 }
                },
                Transform = new Transform(0, 34, 280, 30) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Bottom Progress Bar
            _progressBar = new VisualElement
            {
                Name = $"{Name}_Progress",
                Style = new ElementStyle { BackColor = accentColor.WithAlpha(40), Border = new BorderStyle { Roundness = 2 } },
                Transform = new Transform(18, 95, 244, 4) { Anchor = Anchor.Bottom | Anchor.Left | Anchor.Right }
            };
            AddChild(_progressBar);

            _fillEl = new VisualElement
            {
                Name = $"{Name}_ProgressFill",
                Style = new ElementStyle { BackColor = accentColor, Border = new BorderStyle { Roundness = 2 } },
                Transform = new Transform(0, 0, 122, 4) { Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left }
            };
            _progressBar.AddChild(_fillEl);

            // Hover state styling
            Events.OnMouseEnter += (s) =>
            {
                Style.Border.Color = accentColor.WithAlpha(150);
                Style.Border.Width = 1.5f;
            };

            Events.OnMouseLeave += (s) =>
            {
                Style.Border.Color = new SKColor(255, 255, 255, 12);
                Style.Border.Width = 1f;
            };
        }

        public void SetProgress(float pct, string textVal)
        {
            _valPercent = Math.Clamp(pct, 0f, 1f);
            _valEl.Text = textVal;
            _fillEl.Transform.Width = _progressBar.Transform.Computed.Width * _valPercent;
        }
    }

    /// <summary>
    /// Custom dynamic waveform line chart simulating an oscilloscope.
    /// </summary>
    public class DynamicWaveformChart : VisualElement
    {
        private readonly float[] _points = new float[35];
        private float _phase = 0f;
        private readonly SKColor _chartColor;

        public DynamicWaveformChart(string title, SKColor chartColor)
        {
            Name = $"WaveformChart_{Guid.NewGuid().ToString().Substring(0, 5)}";
            _chartColor = chartColor;

            Style = new ElementStyle
            {
                BackColor = new SKColor(22, 28, 41, 200),
                Border = new BorderStyle { Roundness = 16, Width = 1, Color = new SKColor(255, 255, 255, 12) },
                Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(50), SpreadY = 5, OffsetY = 5 }
            };

            Transform = new Transform(0, 0, 400, 320)
            {
                Anchor = Anchor.Top | Anchor.Left
            };

            // Title - aligned Left
            AddChild(new VisualElement
            {
                Name = $"{Name}_Title",
                Text = title,
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = SKColors.White, Size = 15, Weight = 700, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform { Anchor = Anchor.Top | Anchor.Left, Height = 40 }
            });
        }

        public void UpdateWave(float phaseDelta)
        {
            _phase += phaseDelta;
            for (int i = 0; i < _points.Length; i++)
            {
                float t = (float)i / _points.Length;
                // Complex wave synthesis for realistic look
                _points[i] = 0.5f + 0.32f * (float)Math.Sin(t * 8f + _phase) + 0.08f * (float)Math.Sin(t * 22f - _phase * 1.4f);
            }
        }

        public override void RecordDrawCommands(CommandLedger ledger)
        {
            var cmds = new List<DrawCommand>();

            var rect = new SKRect(0, 0, Transform.Computed.Width, Transform.Computed.Height);

            // Draw card background
            var fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                Color = Style.BackColor
            };
            cmds.Add(new DrawRoundRectCommand(rect, Style.Border.Roundness, Style.Border.Roundness, Style.Border.Roundness, Style.Border.Roundness, fillPaint));

            // Draw card border
            var borderPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeWidth = Style.Border.Width,
                Color = Style.Border.Color
            };
            var borderRect = rect;
            borderRect.Inflate(Style.Border.Width / 2f, Style.Border.Width / 2f);
            cmds.Add(new DrawRoundRectCommand(borderRect, Style.Border.Roundness, Style.Border.Roundness, Style.Border.Roundness, Style.Border.Roundness, borderPaint));

            // Draw wave telemetry line
            if (Transform.Computed.Width > 0 && Transform.Computed.Height > 0)
            {
                cmds.Add(new DrawWaveCommand(_points, rect, _chartColor));
            }

            ledger.Record(Name, cmds);
        }
    }

    /// <summary>
    /// Custom Skia draw command to draw a neon oscilloscope waveform.
    /// </summary>
    public class DrawWaveCommand : DrawCommand
    {
        private readonly float[] _points;
        private readonly SKRect _bounds;
        private readonly SKColor _color;

        public DrawWaveCommand(float[] points, SKRect bounds, SKColor color)
        {
            _points = (float[])points.Clone();
            _bounds = bounds;
            _color = color;
        }

        public override void Execute(SKCanvas canvas)
        {
            if (_points == null || _points.Length < 2) return;

            using var path = new SKPath();
            using var fillPath = new SKPath();

            float chartY = _bounds.Top + 60f;
            float chartHeight = _bounds.Height - 90f;
            float chartWidth = _bounds.Width - 40f;
            float startX = _bounds.Left + 20f;

            float stepX = chartWidth / (_points.Length - 1);

            path.MoveTo(startX, chartY + (1f - _points[0]) * chartHeight);
            fillPath.MoveTo(startX, chartY + chartHeight);
            fillPath.LineTo(startX, chartY + (1f - _points[0]) * chartHeight);

            for (int i = 1; i < _points.Length; i++)
            {
                float x = startX + i * stepX;
                float y = chartY + (1f - _points[i]) * chartHeight;
                path.LineTo(x, y);
                fillPath.LineTo(x, y);
            }

            fillPath.LineTo(startX + chartWidth, chartY + chartHeight);
            fillPath.Close();

            // 1. Glowing linear gradient under the wave
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(_bounds.Left, chartY),
                new SKPoint(_bounds.Left, chartY + chartHeight),
                new SKColor[] { _color.WithAlpha(40), _color.WithAlpha(0) },
                null,
                SKShaderTileMode.Clamp);

            using var fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                Shader = shader
            };
            canvas.DrawPath(fillPath, fillPaint);

            // 2. Wide glowing backdrop shadow (simulating neon diffusion)
            using var glowFilter = SKImageFilter.CreateBlur(3.5f, 3.5f);
            using var glowPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 4.5f,
                IsAntialias = true,
                Color = _color.WithAlpha(90),
                ImageFilter = glowFilter
            };
            canvas.DrawPath(path, glowPaint);

            // 3. Crisp neon center line
            using var linePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2.5f,
                IsAntialias = true,
                Color = _color
            };
            canvas.DrawPath(path, linePaint);
        }
    }
}
