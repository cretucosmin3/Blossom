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

        private VisualElement _showcaseBg = null!;
        private VisualElement _glassCard = null!;
        private VisualElement _blurText = null!;
        
        private float _baseX;
        private bool _autoAnimate = true;

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
                    BackColor = new SKColor(15, 15, 25, 90), // Semi-transparent dark overlay
                    Border = new BorderStyle
                    {
                        Color = new SKColor(255, 0, 110, 250),
                        Width = 3,
                        Roundness = 12
                    },
                    BorderEffect = BorderEffectType.MarchingAnts,
                    BorderEffectSpeed = 1f,
                    Shadow = new ShadowStyle
                    {
                        Color = new SKColor(255, 0, 110, 100),
                        OffsetX = 0,
                        OffsetY = 0,
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

            // Row 2: Border Effects & Blur Selection Buttons
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

            var btnNoBorderEffect = new NeonButton("BORDER: SOLID", new SKColor(120, 120, 120), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX + (ctrlBtnW + btnGap) * 2f, Y = row2Y, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    _glassCard.Style.BorderEffect = BorderEffectType.None;
                }
            };
            AddElement(btnNoBorderEffect);

            var btnAdjustBlur = new NeonButton("CYCLE BLUR", new SKColor(139, 92, 246), ctrlBtnW, ctrlBtnH)
            {
                Transform = { X = startCtrlX + (ctrlBtnW + btnGap) * 3f, Y = row2Y, Anchor = Anchor.Top },
                OnClick = () =>
                {
                    // Cycle blur level between 0, 8, 18, 30 px
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

            // Bottom Navigation Links
            float backBtnWidth = 240f;
            float backBtnHeight = 50f;
            float centerGap = 20f;
            float totalNavWidth = (3f * backBtnWidth) + (2f * centerGap);
            float startNavX = (Width / 2f) - (totalNavWidth / 2f);
            float navY = row2Y + ctrlBtnH + 30f;

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
        }
    }
}
