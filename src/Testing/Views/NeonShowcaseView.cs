using System;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;
using Blossom.Testing.Components;

namespace Blossom.Testing.Views
{
    public class NeonShowcaseView : View
    {
        public Action? OnSwitchView;
        public Action? OnSwitchToPaint;
        public Action? OnSwitchToKanban;
        public Action? OnSwitchToGlass;

        private VisualElement _showcaseBg = null!;
        private VisualElement _glassCard = null!;
        private VisualElement _blurText = null!;
        
        private float _baseX;
        private bool _autoAnimate = true;

        private float _transitionTarget = 1.0f;
        private bool _transitionLooping = false;
        private float _transitionSpeed = 1.0f;

        public NeonShowcaseView() : base("Neon Showcase")
        {
            // Dark Cyberpunk Background
            BackColor = SKColors.Black;
        }

        public override void Init()
        {
            // Title Header
            var title = new VisualElement
            {
                Name = "NeonTitle",
                Text = "NEON & SHADER SHOWCASE",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(255, 0, 128),
                        Size = 36,
                        Weight = 800,
                        Alignment = TextAlign.Center,
                        Shadow = new ShadowStyle
                        {
                            Color = new SKColor(255, 0, 128, 180),
                            OffsetX = 0,
                            OffsetY = 0,
                            SpreadX = 15,
                            SpreadY = 15
                        }
                    }
                },
                Transform = new Transform(0, 30, Width, 60)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            AddElement(title);

            // Subtitle Description
            var subtitle = new VisualElement
            {
                Name = "NeonSubtitle",
                Text = "Direct GPU backdrop blurs, dynamic GLSL background shaders, and animated vector border effects.",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(0, 240, 255, 180),
                        Size = 15,
                        Weight = 400,
                        Alignment = TextAlign.Center,
                        Shadow = new ShadowStyle
                        {
                            Color = new SKColor(0, 240, 255, 100),
                            OffsetX = 0,
                            OffsetY = 0,
                            SpreadX = 8,
                            SpreadY = 8
                        }
                    }
                },
                Transform = new Transform(0, 90, Width, 30)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            AddElement(subtitle);

            // 1. Showcase Background Container (centered horizontally)
            float containerW = 800f;
            float containerH = 320f;
            float containerX = (Width / 2f) - (containerW / 2f);
            float containerY = 135f;

            _showcaseBg = new VisualElement
            {
                Name = "ShowcaseBg",
                Style = new ElementStyle
                {
                    BackgroundShader = BackgroundShaderType.LiquidPlasma,
                    BackgroundShaderColor = new SKColor(0, 240, 255),
                    Border = new BorderStyle
                    {
                        Color = new SKColor(0, 240, 255, 120),
                        Width = 2,
                        Roundness = 16
                    }
                },
                Transform = new Transform(containerX, containerY, containerW, containerH)
                {
                    Anchor = Anchor.Top
                }
            };
            AddElement(_showcaseBg);

            // 2. Glassmorphic Card floating on top of the shader background
            float cardW = 280f;
            float cardH = 180f;
            float cardX = (containerW / 2f) - (cardW / 2f);
            float cardY = (containerH / 2f) - (cardH / 2f);

            _glassCard = new VisualElement
            {
                Name = "GlassCard",
                Style = new ElementStyle
                {
                    BackdropBlur = 18f,
                    BackColor = new SKColor(255, 255, 255, 25), // Semi-transparent overlay
                    BackgroundShader = BackgroundShaderType.GlassRefraction,
                    BackgroundShaderColor = new SKColor(255, 255, 255, 30),
                    Border = new BorderStyle
                    {
                        Color = new SKColor(255, 255, 255, 180),
                        Width = 1.5f,
                        Roundness = 12
                    },
                    BorderEffect = BorderEffectType.GlassReflection,
                    BorderEffectSpeed = 1f,
                    TransitionType = TransitionEffectType.HalftoneDots,
                    TransitionProgress = 1.0f,
                    Shadow = new ShadowStyle
                    {
                        Color = new SKColor(0, 0, 0, 80),
                        OffsetX = 0,
                        OffsetY = 10,
                        SpreadX = 15,
                        SpreadY = 15
                    }
                },
                Transform = new Transform(cardX, cardY, cardW, cardH)
            };
            _showcaseBg.AddChild(_glassCard);

            // Text inside Glassmorphic Card
            var cardTitle = new VisualElement
            {
                Name = "CardTitle",
                Text = "GLASSMORPHIC CARD",
                IsClickthrough = true,
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(255, 255, 255, 240),
                        Size = 18,
                        Weight = 700,
                        Alignment = TextAlign.Center
                    }
                },
                Transform = new Transform(0, 30, cardW, 30)
            };
            _glassCard.AddChild(cardTitle);

            var dragText = new VisualElement
            {
                Name = "DragText",
                Text = "Drag me around!",
                IsClickthrough = true,
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(0, 240, 255, 200),
                        Size = 13,
                        Weight = 500,
                        Alignment = TextAlign.Center
                    }
                },
                Transform = new Transform(0, 70, cardW, 20)
            };
            _glassCard.AddChild(dragText);

            _blurText = new VisualElement
            {
                Name = "BlurText",
                Text = "Backdrop Blur: 18px",
                IsClickthrough = true,
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(240, 240, 240, 200),
                        Size = 14,
                        Weight = 600,
                        Alignment = TextAlign.Center
                    }
                },
                Transform = new Transform(0, 100, cardW, 20)
            };
            _glassCard.AddChild(_blurText);

            var doubleClickText = new VisualElement
            {
                Name = "DoubleClickText",
                Text = "Double-click to auto-slide",
                IsClickthrough = true,
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(150, 150, 160, 180),
                        Size = 11,
                        Weight = 400,
                        Alignment = TextAlign.Center
                    }
                },
                Transform = new Transform(0, 135, cardW, 20)
            };
            _glassCard.AddChild(doubleClickText);

            // Drag-to-move implementation
            bool isDragging = false;
            float dragStartX = 0;
            float dragStartY = 0;
            float elementStartX = 0;
            float elementStartY = 0;

            _baseX = cardX;

            _glassCard.Events.OnMouseDown += (s, e) =>
            {
                isDragging = true;
                dragStartX = e.Global.X;
                dragStartY = e.Global.Y;
                elementStartX = _glassCard.Transform.X;
                elementStartY = _glassCard.Transform.Y;
                _autoAnimate = false;
            };

            this.Events.OnMouseMove += (s, e) =>
            {
                if (isDragging)
                {
                    float dx = e.Global.X - dragStartX;
                    float dy = e.Global.Y - dragStartY;
                    
                    // Constrain drag within the parent background container bounds
                    float newX = elementStartX + dx;
                    float newY = elementStartY + dy;
                    
                    newX = Math.Clamp(newX, 0f, containerW - cardW);
                    newY = Math.Clamp(newY, 0f, containerH - cardH);

                    _glassCard.Transform.X = newX;
                    _glassCard.Transform.Y = newY;
                }
            };

            this.Events.OnMouseUp += (s, e) =>
            {
                isDragging = false;
            };

            _glassCard.Events.OnMouseDoubleClick += (s, e) =>
            {
                _baseX = _glassCard.Transform.X;
                _autoAnimate = true;
            };

            // 3. Interactive controls rows
            float startY = 475f;
            float ctrlBtnW = 180f;
            float ctrlBtnH = 45f;
            float btnGap = 20f;
            float totalCtrlW = (4f * ctrlBtnW) + (3f * btnGap);
            float startCtrlX = (Width / 2f) - (totalCtrlW / 2f);

            // Row 1: Shader Selection Buttons
            var btnPlasma = new NeonButton("PLASMA SHADER", new SKColor(0, 240, 255), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX, Y = startY, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _showcaseBg.Style.BackgroundShader = BackgroundShaderType.LiquidPlasma;
                    _showcaseBg.Style.BackgroundShaderColor = new SKColor(0, 240, 255);
                    _showcaseBg.Style.Border.Color = new SKColor(0, 240, 255, 120);
                }
            };
            AddElement(btnPlasma);

            var btnGrid = new NeonButton("GRID SHADER", new SKColor(255, 0, 128), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX + ctrlBtnW + btnGap, Y = startY, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _showcaseBg.Style.BackgroundShader = BackgroundShaderType.SynthwaveGrid;
                    _showcaseBg.Style.BackgroundShaderColor = new SKColor(255, 0, 128);
                    _showcaseBg.Style.Border.Color = new SKColor(255, 0, 128, 120);
                }
            };
            AddElement(btnGrid);

            var btnCrt = new NeonButton("CRT SCANLINES", new SKColor(57, 255, 20), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX + (ctrlBtnW + btnGap) * 2f, Y = startY, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _showcaseBg.Style.BackgroundShader = BackgroundShaderType.Scanlines;
                    _showcaseBg.Style.BackgroundShaderColor = new SKColor(15, 15, 20); // dark background for scanlines
                    _showcaseBg.Style.Border.Color = new SKColor(57, 255, 20, 120);
                }
            };
            AddElement(btnCrt);

            var btnNoShader = new NeonButton("SOLID BACKGROUND", new SKColor(120, 120, 120), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX + (ctrlBtnW + btnGap) * 3f, Y = startY, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _showcaseBg.Style.BackgroundShader = BackgroundShaderType.None;
                    _showcaseBg.Style.BackColor = new SKColor(25, 25, 35);
                    _showcaseBg.Style.Border.Color = new SKColor(120, 120, 120, 120);
                }
            };
            AddElement(btnNoShader);

            // Row 2: Border Effects
            float row2Y = startY + ctrlBtnH + 15f;

            var btnMarching = new NeonButton("BORDER: DYNAMIC", new SKColor(255, 170, 0), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX, Y = row2Y, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _glassCard.Style.BorderEffect = BorderEffectType.MarchingAnts;
                    _glassCard.Style.BorderEffectSpeed = 1f;
                }
            };
            AddElement(btnMarching);

            var btnJitter = new NeonButton("BORDER: GLITCH", new SKColor(255, 0, 110), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX + ctrlBtnW + btnGap, Y = row2Y, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _glassCard.Style.BorderEffect = BorderEffectType.Jitter;
                    _glassCard.Style.BorderEffectSpeed = 1f;
                    _glassCard.Style.BorderEffectAmount = 4f;
                }
            };
            AddElement(btnJitter);

            var btnGlassBorder = new NeonButton("BORDER: GLASS", new SKColor(230, 240, 255), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX + (ctrlBtnW + btnGap) * 2f, Y = row2Y, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _glassCard.Style.BorderEffect = BorderEffectType.GlassReflection;
                    _glassCard.Style.Border.Color = new SKColor(255, 255, 255, 200);
                }
            };
            AddElement(btnGlassBorder);

            var btnNoBorderEffect = new NeonButton("BORDER: SOLID", new SKColor(120, 120, 120), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX + (ctrlBtnW + btnGap) * 3f, Y = row2Y, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _glassCard.Style.BorderEffect = BorderEffectType.None;
                    _glassCard.Style.Border.Color = new SKColor(255, 0, 110, 250);
                }
            };
            AddElement(btnNoBorderEffect);

            // Row 3: Glass Background & Blur Controls
            float row3Y = row2Y + ctrlBtnH + 15f;

            var btnGlassBg = new NeonButton("GLASS BACKGROUND", new SKColor(186, 230, 253), ctrlBtnW * 1.5f, ctrlBtnH)
            {
                Transform = { X = (Width / 2f) - (ctrlBtnW * 1.5f + btnGap + ctrlBtnW) / 2f, Y = row3Y, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    if (_glassCard.Style.BackgroundShader == BackgroundShaderType.GlassRefraction)
                    {
                        _glassCard.Style.BackgroundShader = BackgroundShaderType.None;
                        _glassCard.Style.BackColor = new SKColor(15, 15, 25, 90);
                    }
                    else
                    {
                        _glassCard.Style.BackgroundShader = BackgroundShaderType.GlassRefraction;
                        _glassCard.Style.BackgroundShaderColor = new SKColor(255, 255, 255, 30);
                    }
                }
            };
            AddElement(btnGlassBg);

            var btnAdjustBlur = new NeonButton("CYCLE BLUR", new SKColor(139, 92, 246), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = (Width / 2f) - (ctrlBtnW * 1.5f + btnGap + ctrlBtnW) / 2f + ctrlBtnW * 1.5f + btnGap, Y = row3Y, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    float currentBlur = _glassCard.Style.BackdropBlur;
                    float nextBlur = currentBlur switch
                    {
                        0f => 8f,
                        8f => 18f,
                        18f => 30f,
                        _ => 0f
                    };
                    _glassCard.Style.BackdropBlur = nextBlur;
                    _blurText.Text = $"Backdrop Blur: {nextBlur}px";
                }
            };
            AddElement(btnAdjustBlur);

            // Row 4: Halftone Shader Transition Controls
            float row4Y = row3Y + ctrlBtnH + 15f;

            var btnTransIn = new NeonButton("HALFTONE REVEAL", new SKColor(57, 255, 20), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX, Y = row4Y, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _transitionLooping = false;
                    _transitionTarget = 1.0f;
                }
            };
            AddElement(btnTransIn);

            var btnTransOut = new NeonButton("HALFTONE HIDE", new SKColor(255, 0, 110), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX + ctrlBtnW + btnGap, Y = row4Y, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _transitionLooping = false;
                    _transitionTarget = 0.0f;
                }
            };
            AddElement(btnTransOut);

            var btnTransLoop = new NeonButton("LOOP TRANSITION", new SKColor(255, 170, 0), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX + (ctrlBtnW + btnGap) * 2f, Y = row4Y, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _transitionLooping = !_transitionLooping;
                    if (_transitionLooping)
                    {
                        _transitionTarget = _glassCard.Style.TransitionProgress > 0.5f ? 0.0f : 1.0f;
                    }
                }
            };
            AddElement(btnTransLoop);

            NeonButton btnTransType = null!;
            btnTransType = new NeonButton("DISABLE DOTS", new SKColor(120, 120, 120), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX + (ctrlBtnW + btnGap) * 3f, Y = row4Y, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _transitionLooping = false;
                    if (_glassCard.Style.TransitionType == TransitionEffectType.HalftoneDots)
                    {
                        _glassCard.Style.TransitionType = TransitionEffectType.None;
                        btnTransType.Text = "ENABLE DOTS";
                    }
                    else
                    {
                        _glassCard.Style.TransitionType = TransitionEffectType.HalftoneDots;
                        btnTransType.Text = "DISABLE DOTS";
                    }
                }
            };
            AddElement(btnTransType);

            // Bottom Navigation Links
            float backBtnWidth = 220f; // Slightly narrower to fit 4 buttons easily
            float backBtnHeight = 50f;
            float centerGap = 15f;
            float totalNavWidth = (4f * backBtnWidth) + (3f * centerGap);
            float startNavX = (Width / 2f) - (totalNavWidth / 2f);
            float navY = row4Y + ctrlBtnH + 30f;

            var backBtn = new NeonButton("➜ DASHBOARD", new SKColor(139, 92, 246), backBtnWidth, backBtnHeight)
            {
                Transform = { X = startNavX, Y = navY, Anchor = Anchor.Top },
                OnClick = () => OnSwitchView?.Invoke()
            };
            AddElement(backBtn);

            var paintBtn = new NeonButton("➜ NEON PAINT", new SKColor(255, 0, 110), backBtnWidth, backBtnHeight)
            {
                Transform = { X = startNavX + backBtnWidth + centerGap, Y = navY, Anchor = Anchor.Top },
                OnClick = () => OnSwitchToPaint?.Invoke()
            };
            AddElement(paintBtn);

            var kanbanBtn = new NeonButton("➜ TASK BOARD", new SKColor(0, 240, 255), backBtnWidth, backBtnHeight)
            {
                Transform = { X = startNavX + (backBtnWidth + centerGap) * 2f, Y = navY, Anchor = Anchor.Top },
                OnClick = () => OnSwitchToKanban?.Invoke()
            };
            AddElement(kanbanBtn);

            var glassBtn = new NeonButton("➜ GLASS SHOWCASE", new SKColor(56, 189, 248), backBtnWidth, backBtnHeight)
            {
                Transform = { X = startNavX + (backBtnWidth + centerGap) * 3f, Y = navY, Anchor = Anchor.Top },
                OnClick = () => OnSwitchToGlass?.Invoke()
            };
            AddElement(glassBtn);

            // Register sliding animation to View loop
            Loop += UpdateAnimation;
        }

        private void UpdateAnimation()
        {
            if (_autoAnimate && _glassCard != null)
            {
                float containerW = 800f;
                float cardW = 280f;
                float centerLimitX = (containerW / 2f) - (cardW / 2f);
                float offset = (float)Math.Sin(SKSLShaderTimeTracker.ElapsedSeconds * 1.2f) * 230f;
                
                _glassCard.Transform.X = centerLimitX + offset;
            }

            // Animate transition progress towards target
            if (_glassCard != null)
            {
                float current = _glassCard.Style.TransitionProgress;
                if (_transitionLooping)
                {
                    if (current >= 1.0f) _transitionTarget = 0.0f;
                    else if (current <= 0.0f) _transitionTarget = 1.0f;
                }

                if (Math.Abs(current - _transitionTarget) > 0.001f)
                {
                    float step = SKSLShaderTimeTracker.DeltaTime * _transitionSpeed;
                    if (current < _transitionTarget)
                    {
                        _glassCard.Style.TransitionProgress = Math.Min(_transitionTarget, current + step);
                    }
                    else
                    {
                        _glassCard.Style.TransitionProgress = Math.Max(_transitionTarget, current - step);
                    }
                }
            }
        }
    }
}
