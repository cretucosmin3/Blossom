using System;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;
using Blossom.Testing.Components;
using System.Collections.Generic;

namespace Blossom.Testing.Views
{
    public class GlassmorphismShowcaseView : View
    {
        public Action? OnSwitchToDashboard;
        public Action? OnSwitchToNeon;
        public Action? OnSwitchToPaint;
        public Action? OnSwitchToKanban;
        public Action? OnSwitchTo3D;

        private VisualElement _bgContainer = null!;
        private VisualElement _mainContent = null!;
        private VisualElement _previewCard = null!;
        private VisualElement _statusLogText = null!;
        private VisualElement _blurValueText = null!;
        private VisualElement _speedValueText = null!;
        private ProgressBar _previewProgressBar = null!;
        private VisualElement _sidebar = null!;
        private GlassButton _shadingModeBtn = null!;
        private bool _isContinuous = true;

        private float _currentBlur = 20f;
        private float _currentSpeed = 1.2f;
        private SKColor _currentTintColor = new SKColor(0, 240, 255); // Default Cyan

        private List<string> _logEntries = new()
        {
            "[SYSTEM] Booting Glassmorphism Showcase...",
            "[SYSTEM] Initialized backdrop blur pipeline.",
            "[SYSTEM] Ready for user interaction."
        };

        public GlassmorphismShowcaseView() : base("Glassmorphism Showcase")
        {
            BackColor = SKColors.Black;
        }

        public override void Init()
        {
            // 1. Full-screen background shader container (Quantum/Holographic)
            _bgContainer = new VisualElement
            {
                Name = "GlassShowcaseBg",
                Transform = new Transform(0, 0, Width, Height)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
                },
                Style = new ElementStyle
                {
                    BackgroundShader = BackgroundShaderType.QuantumDots,
                    BackgroundShaderColor = new SKColor(0, 36, 40, 255), // Deep dark cyan base
                    ShaderRenderMode = EffectRenderMode.Continuous
                }
            };
            AddElement(_bgContainer);

            // 2. Glassmorphic Navigation Sidebar
            float sidebarWidth = 260f;
            _sidebar = new VisualElement
            {
                Name = "GlassSidebar",
                Style = new ElementStyle
                {
                    BackdropBlur = 22f,
                    BackColor = new SKColor(10, 12, 18, 90), // Extremely translucent dark blue
                    ShaderRenderMode = EffectRenderMode.Continuous,
                    Border = new BorderStyle
                    {
                        Width = 1,
                        Color = new SKColor(255, 255, 255, 35),
                        Roundness = 0
                    },
                    Shadow = new ShadowStyle
                    {
                        Color = SKColors.Black.WithAlpha(120),
                        SpreadX = 8,
                        SpreadY = 0,
                        OffsetX = 3
                    }
                },
                Transform = new Transform(0, 0, sidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left,
                    FixedWidth = true,
                    FixedHeight = false
                }
            };
            AddElement(_sidebar);

            // Brand Header inside Sidebar
            var brand = new VisualElement
            {
                Name = "SidebarBrand",
                Text = "⚡ GLASS OS",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(56, 189, 248),
                        Size = 22,
                        Weight = 800,
                        Alignment = TextAlign.Center,
                        Padding = 15
                    }
                },
                Transform = new Transform(0, 40, sidebarWidth, 50)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            _sidebar.AddChild(brand);

            // Sidebar Menu Items
            string[] menuItems = { "Overview", "Neon Showcase", "Neon Paint", "Task Board", "3D Showcase", "Glass Showcase" };
            float menuY = 120f;
            for (int i = 0; i < menuItems.Length; i++)
            {
                string itemText = menuItems[i];
                bool isActive = (i == 5); // Glass Showcase is active

                var btn = new VisualElement
                {
                    Name = $"SidebarItem_{i}",
                    Text = isActive ? $"✦  {itemText.ToUpper()}" : $"   {itemText}",
                    Style = new ElementStyle
                    {
                        BackColor = isActive ? new SKColor(255, 255, 255, 30) : SKColors.Transparent,
                        Border = new BorderStyle
                        {
                            Width = isActive ? 1.5f : 0,
                            Color = isActive ? new SKColor(56, 189, 248, 180) : SKColors.Transparent,
                            Roundness = 8
                        },
                        Text = new TextStyle
                        {
                            Color = isActive ? new SKColor(56, 189, 248) : new SKColor(200, 200, 200, 180),
                            Size = 13,
                            Weight = isActive ? 800 : 500,
                            Alignment = TextAlign.Left,
                            Padding = 15
                        }
                    },
                    Transform = new Transform(15, menuY, sidebarWidth - 30, 42)
                    {
                        Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                    }
                };

                // Hover Effects
                if (!isActive)
                {
                    btn.Events.OnMouseEnter += (s) =>
                    {
                        btn.Style.BackColor = new SKColor(255, 255, 255, 15);
                        btn.Style.Text.Color = SKColors.White;
                    };
                    btn.Events.OnMouseLeave += (s) =>
                    {
                        btn.Style.BackColor = SKColors.Transparent;
                        btn.Style.Text.Color = new SKColor(200, 200, 200, 180);
                    };
                }

                // Click transitions
                int idx = i;
                btn.Events.OnMouseUp += (s, e) =>
                {
                    if (idx == 0) OnSwitchToDashboard?.Invoke();
                    else if (idx == 1) OnSwitchToNeon?.Invoke();
                    else if (idx == 2) OnSwitchToPaint?.Invoke();
                    else if (idx == 3) OnSwitchToKanban?.Invoke();
                    else if (idx == 4) OnSwitchTo3D?.Invoke();
                };

                _sidebar.AddChild(btn);
                menuY += 52f;
            }

            // 3. Main Content Container
            _mainContent = new VisualElement
            {
                Name = "MainContent",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(sidebarWidth, 0, Width - sidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
                }
            };
            AddElement(_mainContent);

            // Header Area
            var header = new VisualElement
            {
                Name = "Header",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(30, 30, Width - sidebarWidth - 60, 80)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            _mainContent.AddChild(header);

            // Title
            header.AddChild(new VisualElement
            {
                Name = "HeaderTitle",
                Text = "GLASSMORPHIC INTERFACE",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = SKColors.White,
                        Size = 28,
                        Weight = 800,
                        Alignment = TextAlign.Left
                    }
                },
                Transform = new Transform(0, 0, 500, 36)
            });

            // Subtitle
            header.AddChild(new VisualElement
            {
                Name = "HeaderSubtitle",
                Text = "GPU blurs, refraction shaders, and reflection borders overlaying dynamic backgrounds.",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(200, 200, 200, 150),
                        Size = 14,
                        Weight = 400,
                        Alignment = TextAlign.Left
                    }
                },
                Transform = new Transform(0, 42, 700, 25)
            });

            // Columns layout for Control Panel (Left) and Preview Card (Right)
            float padding = 30f;
            float leftPanelW = 380f;
            float rightPanelW = Width - sidebarWidth - leftPanelW - (padding * 3f);
            float panelsY = 130f;
            float panelsH = Height - panelsY - padding;

            // 4. CONTROL PANEL CARD (Left)
            var controlPanel = new VisualElement
            {
                Name = "ControlPanel",
                Style = new ElementStyle
                {
                    BackdropBlur = 12f,
                    BackColor = new SKColor(15, 20, 35, 100),
                    Border = new BorderStyle
                    {
                        Color = new SKColor(255, 255, 255, 45),
                        Width = 1f,
                        Roundness = 16
                    },
                    Shadow = new ShadowStyle
                    {
                        Color = SKColors.Black.WithAlpha(100),
                        SpreadX = 10,
                        SpreadY = 10,
                        OffsetX = 0,
                        OffsetY = 8
                    }
                },
                Transform = new Transform(padding, panelsY, leftPanelW, panelsH)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left,
                    FixedWidth = true
                }
            };
            _mainContent.AddChild(controlPanel);

            // Control Panel Title
            var cpTitle = new VisualElement
            {
                Name = "CpTitle",
                Text = "CONTROLLER SYSTEM",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(56, 189, 248),
                        Size = 16,
                        Weight = 800,
                        Alignment = TextAlign.Left,
                        Padding = 20
                    }
                },
                Transform = new Transform(0, 0, leftPanelW, 50)
            };
            controlPanel.AddChild(cpTitle);

            // Subtitle for Theme selectors
            controlPanel.AddChild(new VisualElement
            {
                Name = "ThemeSecTitle",
                Text = "TINT SELECTORS",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(150, 150, 150), Size = 11, Weight = 700, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform(0, 45, leftPanelW, 20)
            });

            // Tint Buttons (Cyan, Magenta, Emerald, Amber)
            float btnW = 160f;
            float btnH = 40f;
            float btnX1 = 20f;
            float btnX2 = leftPanelW - btnW - 20f;
            float btnY = 70f;

            var cyanBtn = new GlassButton("CYAN AURA", new SKColor(0, 240, 255), btnW, btnH)
            {
                Transform = { X = btnX1, Y = btnY }
            };
            cyanBtn.OnClick = () => ApplyTint(new SKColor(0, 240, 255), "CYAN AURA");
            controlPanel.AddChild(cyanBtn);

            var magentaBtn = new GlassButton("MAGENTA SHINE", new SKColor(255, 0, 128), btnW, btnH)
            {
                Transform = { X = btnX2, Y = btnY }
            };
            magentaBtn.OnClick = () => ApplyTint(new SKColor(255, 0, 128), "MAGENTA SHINE");
            controlPanel.AddChild(magentaBtn);

            var emeraldBtn = new GlassButton("EMERALD GLOW", new SKColor(16, 185, 129), btnW, btnH)
            {
                Transform = { X = btnX1, Y = btnY + 50 }
            };
            emeraldBtn.OnClick = () => ApplyTint(new SKColor(16, 185, 129), "EMERALD GLOW");
            controlPanel.AddChild(emeraldBtn);

            var amberBtn = new GlassButton("AMBER FLARE", new SKColor(245, 158, 11), btnW, btnH)
            {
                Transform = { X = btnX2, Y = btnY + 50 }
            };
            amberBtn.OnClick = () => ApplyTint(new SKColor(245, 158, 11), "AMBER FLARE");
            controlPanel.AddChild(amberBtn);

            // Dynamics Adjustment Section
            float dynamicsY = 180f;
            controlPanel.AddChild(new VisualElement
            {
                Name = "DynamicsSecTitle",
                Text = "DYNAMICS ADJUSTER",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(150, 150, 150), Size = 11, Weight = 700, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform(0, dynamicsY, leftPanelW, 20)
            });

            // Blur Adjusters
            float blurCtrY = dynamicsY + 25f;
            controlPanel.AddChild(new VisualElement
            {
                Name = "BlurLabel",
                Text = "BACKDROP BLUR LEVEL",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = SKColors.White, Size = 12, Weight = 600, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform(0, blurCtrY, 200, 20)
            });

            _blurValueText = new VisualElement
            {
                Name = "BlurValText",
                Text = $"{_currentBlur:0} px",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 12, Weight = 700, Alignment = TextAlign.Right, Padding = 20 }
                },
                Transform = new Transform(leftPanelW - 100, blurCtrY, 80, 20)
            };
            controlPanel.AddChild(_blurValueText);

            var decBlurBtn = new GlassButton("DECREASE (-5)", new SKColor(239, 68, 68), 160f, 35f)
            {
                Transform = { X = btnX1, Y = blurCtrY + 25 }
            };
            decBlurBtn.OnClick = () => AdjustBlur(-5f);
            controlPanel.AddChild(decBlurBtn);

            var incBlurBtn = new GlassButton("INCREASE (+5)", new SKColor(34, 197, 94), 160f, 35f)
            {
                Transform = { X = btnX2, Y = blurCtrY + 25 }
            };
            incBlurBtn.OnClick = () => AdjustBlur(5f);
            controlPanel.AddChild(incBlurBtn);

            // Reflection Border Speed Adjusters
            float speedCtrY = blurCtrY + 75f;
            controlPanel.AddChild(new VisualElement
            {
                Name = "SpeedLabel",
                Text = "BORDER REFLECTION SPEED",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = SKColors.White, Size = 12, Weight = 600, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform(0, speedCtrY, 200, 20)
            });

            _speedValueText = new VisualElement
            {
                Name = "SpeedValText",
                Text = $"{_currentSpeed:0.0}x",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 12, Weight = 700, Alignment = TextAlign.Right, Padding = 20 }
                },
                Transform = new Transform(leftPanelW - 100, speedCtrY, 80, 20)
            };
            controlPanel.AddChild(_speedValueText);

            var decSpeedBtn = new GlassButton("SLOWER (-0.2x)", new SKColor(239, 68, 68), 160f, 35f)
            {
                Transform = { X = btnX1, Y = speedCtrY + 25 }
            };
            decSpeedBtn.OnClick = () => AdjustSpeed(-0.2f);
            controlPanel.AddChild(decSpeedBtn);

            var incSpeedBtn = new GlassButton("FASTER (+0.2x)", new SKColor(34, 197, 94), 160f, 35f)
            {
                Transform = { X = btnX2, Y = speedCtrY + 25 }
            };
            incSpeedBtn.OnClick = () => AdjustSpeed(0.2f);
            controlPanel.AddChild(incSpeedBtn);

            // Action System
            float actionSecY = speedCtrY + 75f;
            controlPanel.AddChild(new VisualElement
            {
                Name = "ActionSecTitle",
                Text = "REFLECTION STATE",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(150, 150, 150), Size = 11, Weight = 700, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform(0, actionSecY, leftPanelW, 20)
            });

            var toggleReflectionBtn = new GlassButton("TOGGLE REFLECTION", new SKColor(139, 92, 246), leftPanelW - 40, 38)
            {
                Transform = { X = 20, Y = actionSecY + 25 }
            };
            toggleReflectionBtn.OnClick = ToggleReflection;
            controlPanel.AddChild(toggleReflectionBtn);

            // Shading System
            float shaderSecY = actionSecY + 75f;
            controlPanel.AddChild(new VisualElement
            {
                Name = "ShaderSecTitle",
                Text = "QUANTUM SHADER CONTROL",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(150, 150, 150), Size = 11, Weight = 700, Alignment = TextAlign.Left, Padding = 20 }
                },
                Transform = new Transform(0, shaderSecY, leftPanelW, 20)
            });

            _shadingModeBtn = new GlassButton("MODE: CONTINUOUS ACTIVE", new SKColor(59, 130, 246), leftPanelW - 40, 38)
            {
                Transform = { X = 20, Y = shaderSecY + 25 }
            };
            _shadingModeBtn.OnClick = ToggleShadingMode;
            controlPanel.AddChild(_shadingModeBtn);


            // 5. INTERACTIVE PREVIEW CARD (Right)
            _previewCard = new VisualElement
            {
                Name = "PreviewCard",
                Style = new ElementStyle
                {
                    BackdropBlur = _currentBlur,
                    BackColor = new SKColor(255, 255, 255, 18),
                    BackgroundShader = BackgroundShaderType.GlassRefraction,
                    BackgroundShaderColor = new SKColor(_currentTintColor.Red, _currentTintColor.Green, _currentTintColor.Blue, 30),
                    ShaderRenderMode = EffectRenderMode.Continuous,
                    Border = new BorderStyle
                    {
                        Color = new SKColor(255, 255, 255, 120),
                        Width = 1.5f,
                        Roundness = 16
                    },
                    BorderEffect = BorderEffectType.GlassReflection,
                    BorderEffectSpeed = _currentSpeed,
                    Shadow = new ShadowStyle
                    {
                        Color = SKColors.Black.WithAlpha(120),
                        SpreadX = 15,
                        SpreadY = 15,
                        OffsetX = 0,
                        OffsetY = 12
                    }
                },
                Transform = new Transform(padding * 2f + leftPanelW, panelsY, rightPanelW, panelsH)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
                }
            };
            _mainContent.AddChild(_previewCard);

            // Preview Title
            var pvTitle = new VisualElement
            {
                Name = "PvTitle",
                Text = "DYNAMICS PREVIEW ENGINE",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = SKColors.White,
                        Size = 16,
                        Weight = 800,
                        Alignment = TextAlign.Left,
                        Padding = 24
                    }
                },
                Transform = new Transform(0, 0, rightPanelW, 50)
            };
            _previewCard.AddChild(pvTitle);

            // Subtitle
            _previewCard.AddChild(new VisualElement
            {
                Name = "PvSubtitle",
                Text = "This panel renders dynamic backend blurs overlaying the wallpaper.",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(200, 200, 200, 160),
                        Size = 12,
                        Weight = 500,
                        Alignment = TextAlign.Left,
                        Padding = 24
                    }
                },
                Transform = new Transform(0, 40, rightPanelW, 20)
            });

            // ProgressBar component to show status
            _previewProgressBar = new ProgressBar(0.65f, _currentTintColor)
            {
                Transform = new Transform(24, 75, rightPanelW - 48, 16)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            _previewCard.AddChild(_previewProgressBar);

            // System Logs title
            _previewCard.AddChild(new VisualElement
            {
                Name = "LogsTitle",
                Text = "SYSTEM EVENT LOG",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(140, 140, 150),
                        Size = 11,
                        Weight = 700,
                        Alignment = TextAlign.Left,
                        Padding = 24
                    }
                },
                Transform = new Transform(0, 110, rightPanelW, 20)
            });

            // Logs text container (simulated terminal output)
            _statusLogText = new VisualElement
            {
                Name = "StatusLogText",
                Text = BuildLogString(),
                Style = new ElementStyle
                {
                    BackColor = new SKColor(0, 0, 0, 70),
                    Border = new BorderStyle
                    {
                        Color = new SKColor(255, 255, 255, 25),
                        Width = 1f,
                        Roundness = 8
                    },
                    Text = new TextStyle
                    {
                        Color = new SKColor(0, 240, 255),
                        Size = 12,
                        Weight = 500,
                        Alignment = TextAlign.Left,
                        Padding = 12
                    }
                },
                Transform = new Transform(24, 135, rightPanelW - 48, 140)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
                }
            };
            _previewCard.AddChild(_statusLogText);

            // Interactive Info Box
            var infoBox = new VisualElement
            {
                Name = "InfoBox",
                Text = "COMPUTED PIPELINE METRICS:\nFPS: 165hz (V-SYNC)\nFRAME TIME: 6.06ms\nBACKDROP SOLVER: SkiaSharp GPU context\nDISPOSAL LIFECYCLE: Safe recursive dispose enabled",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(255, 255, 255, 8),
                    Border = new BorderStyle
                    {
                        Color = new SKColor(255, 255, 255, 15),
                        Width = 1f,
                        Roundness = 10
                    },
                    Text = new TextStyle
                    {
                        Color = new SKColor(180, 180, 190),
                        Size = 11,
                        Weight = 500,
                        Alignment = TextAlign.Left,
                        Padding = 12
                    }
                },
                Transform = new Transform(24, 290, rightPanelW - 48, 85)
                {
                    Anchor = Anchor.Bottom | Anchor.Left | Anchor.Right
                }
            };
            _previewCard.AddChild(infoBox);
        }

        private void ApplyTint(SKColor color, string name)
        {
            _currentTintColor = color;
            _previewCard.Style.BackgroundShaderColor = new SKColor(color.Red, color.Green, color.Blue, 30);
            _previewProgressBar.Percentage = (float)Random.Shared.NextDouble() * 0.4f + 0.5f; // Random fluctuation
            _previewProgressBar.Percentage = Math.Clamp(_previewProgressBar.Percentage, 0.1f, 1.0f);
            
            // Re-fetch the child fill to apply styling change if we want it to match
            if (_previewProgressBar.Children.Length > 0)
            {
                _previewProgressBar.Children[0].Style.BackColor = color;
            }

            // Update background container's shader color too! (with nice 20% intensity dark contrast)
            _bgContainer.Style.BackgroundShaderColor = new SKColor(
                (byte)(color.Red * 0.2f),
                (byte)(color.Green * 0.2f),
                (byte)(color.Blue * 0.2f),
                255
            );
            
            // Clear render caches to redraw everything on-demand with new color
            _bgContainer.ClearRenderCache();
            _bgContainer.ScheduleRender();

            _previewCard.ClearRenderCache();
            _previewCard.ScheduleRender();

            _sidebar.ClearRenderCache();
            _sidebar.ScheduleRender();

            LogEvent($"[USER] SELECTED THEME: {name}");
        }

        private void ToggleShadingMode()
        {
            _isContinuous = !_isContinuous;
            var mode = _isContinuous ? EffectRenderMode.Continuous : EffectRenderMode.OnDemand;

            _bgContainer.Style.ShaderRenderMode = mode;
            _previewCard.Style.ShaderRenderMode = mode;
            _sidebar.Style.ShaderRenderMode = mode;

            _bgContainer.ClearRenderCache();
            _previewCard.ClearRenderCache();
            _sidebar.ClearRenderCache();

            _bgContainer.ScheduleRender();
            _previewCard.ScheduleRender();
            _sidebar.ScheduleRender();

            _shadingModeBtn.Text = _isContinuous ? "MODE: CONTINUOUS ACTIVE" : "MODE: ON-DEMAND (PAUSED)";
            LogEvent($"[USER] SET SHADING MODE: {(_isContinuous ? "CONTINUOUS" : "ON-DEMAND")}");
        }

        private void ReseedShader()
        {
            // Clear all on-demand render caches to capture the new coordinate-warped lattice state
            _bgContainer.ClearRenderCache();
            _bgContainer.ScheduleRender();

            _previewCard.ClearRenderCache();
            _previewCard.ScheduleRender();

            _sidebar.ClearRenderCache();
            _sidebar.ScheduleRender();

            LogEvent("[USER] RE-SEEDED QUANTUM DOTS FIELD");
        }

        private void AdjustBlur(float delta)
        {
            _currentBlur = Math.Clamp(_currentBlur + delta, 0f, 50f);
            _previewCard.Style.BackdropBlur = _currentBlur;
            _blurValueText.Text = $"{_currentBlur:0} px";
            
            LogEvent($"[USER] ADJUSTED BACKDROP BLUR TO {_currentBlur:0}px");
        }

        private void AdjustSpeed(float delta)
        {
            _currentSpeed = Math.Clamp(_currentSpeed + delta, 0f, 5f);
            _previewCard.Style.BorderEffectSpeed = _currentSpeed;
            _speedValueText.Text = $"{_currentSpeed:0.0}x";
            
            LogEvent($"[USER] ADJUSTED BORDER REFLECTION SPEED TO {_currentSpeed:0.0}x");
        }

        private void ToggleReflection()
        {
            if (_previewCard.Style.BorderEffect == BorderEffectType.GlassReflection)
            {
                _previewCard.Style.BorderEffect = BorderEffectType.None;
                LogEvent("[USER] SET BORDER REFLECTION: OFF");
            }
            else
            {
                _previewCard.Style.BorderEffect = BorderEffectType.GlassReflection;
                LogEvent("[USER] SET BORDER REFLECTION: ON (GLASS)");
            }
        }

        private void LogEvent(string msg)
        {
            _logEntries.Add(msg);
            if (_logEntries.Count > 6)
            {
                _logEntries.RemoveAt(0);
            }
            _statusLogText.Text = BuildLogString();
        }

        private string BuildLogString()
        {
            return string.Join("\n", _logEntries);
        }
    }
}
