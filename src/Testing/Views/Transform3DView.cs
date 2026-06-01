using System;
using System.Threading;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;
using Blossom.Testing.Components;

namespace Blossom.Testing.Views
{
    public class Transform3DView : View
    {
        public Action? OnSwitchToDashboard;
        
        private VisualElement? _cardRotY;
        private VisualElement? _cardRotX;
        private VisualElement? _cardRotZ;
        private VisualElement? _cardScale;
        private VisualElement? _rotatingCard;

        private int _clickCount = 0;
        private Thread? _animationThread;
        private bool _running = false;
        private float _time = 0f;

        private VisualElement? _draggedCard;
        private float _dragOffsetX;
        private float _dragOffsetY;
        
        private VisualElement? _nestedChildY;

        public Transform3DView() : base("3D Transforms Showcase")
        {
            BackColor = new SKColor(11, 15, 26, 255); // Premium dark slate background
            UseReferenceResolution = false;
            ReferenceWidth = 1280;
            ReferenceHeight = 800;
        }

        public override void Init()
        {
            // Title Header
            var title = new VisualElement
            {
                Name = "3DTitle",
                Text = "2D & 3D MATRIX TRANSFORMS",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(0, 240, 255),
                        Size = 34,
                        Weight = 900,
                        Alignment = TextAlign.Center,
                        Shadow = new ShadowStyle
                        {
                            Color = new SKColor(0, 240, 255, 100),
                            OffsetX = 0,
                            OffsetY = 0,
                            SpreadX = 12,
                            SpreadY = 12
                        }
                    }
                },
                Transform = new Transform(0, 30, Width, 50)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            AddElement(title);

            // Subtitle Description
            var subtitle = new VisualElement
            {
                Name = "3DSubtitle",
                Text = "All cards animate continuously. The center card is interactive—click to increment hits.",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(148, 163, 184),
                        Size = 15,
                        Weight = 400,
                        Alignment = TextAlign.Center
                    }
                },
                Transform = new Transform(0, 85, Width, 30)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            AddElement(subtitle);

            float cardW = 230f;
            float cardH = 200f;
            float centerYOffset = 150f;

            // Spacing calculations for 1280 width:
            // Width is 1280.
            // Left column X: Width / 2 - 510 = 130
            // Right column X: Width / 2 + 280 = 920
            // Center column X: Width / 2 - 130 = 510
            // Gaps are exactly 150px.

            _cardRotY = new VisualElement
            {
                Name = "CardRotY",
                Text = "3D ROTATE Y\nSpinning...",
                IsClipping = true,
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59, 255),
                    Border = new BorderStyle { Color = new SKColor(56, 189, 248), Width = 3, Roundness = 12 },
                    Text = new TextStyle { Color = SKColors.White, Size = 16, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(0, 0, cardW, cardH)
                {
                    X = (Width / 2f) - 510f,
                    Y = centerYOffset + 20f,
                    Anchor = Anchor.Top,
                    RotationY = 0f,
                    Perspective = 500f,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            AddElement(_cardRotY);

            // Add a nested child component to demonstrate nested 3D transforms & parent clipping
            _nestedChildY = new VisualElement
            {
                Name = "NestedChildY",
                Text = "NESTED\nCLIP",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(244, 63, 94, 200), // Semitransparent rose background
                    Border = new BorderStyle { Color = new SKColor(255, 255, 255), Width = 2, Roundness = 8 },
                    Text = new TextStyle { Color = SKColors.White, Size = 13, Weight = 800, Alignment = TextAlign.Center }
                },
                Transform = new Transform(0, 0, 130, 130)
                {
                    X = 50f,
                    Y = 35f,
                    Anchor = Anchor.Top | Anchor.Left,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            _cardRotY.AddChild(_nestedChildY);

            // 2. TOP-RIGHT: Rotation X (3D Tilt Spin)
            _cardRotX = new VisualElement
            {
                Name = "CardRotX",
                Text = "3D ROTATE X\nSpinning...",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59, 255),
                    Border = new BorderStyle { Color = new SKColor(244, 63, 94), Width = 3, Roundness = 12 },
                    Text = new TextStyle { Color = SKColors.White, Size = 16, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(0, 0, cardW, cardH)
                {
                    X = (Width / 2f) + 280f,
                    Y = centerYOffset + 20f,
                    Anchor = Anchor.Top,
                    RotationX = 0f,
                    Perspective = 500f,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            AddElement(_cardRotX);

            // 3. BOTTOM-LEFT: Rotation Z (2D Spin)
            _cardRotZ = new VisualElement
            {
                Name = "CardRotZ",
                Text = "2D ROTATE Z\nSpinning...",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59, 255),
                    Border = new BorderStyle { Color = new SKColor(168, 85, 247), Width = 3, Roundness = 12 },
                    Text = new TextStyle { Color = SKColors.White, Size = 16, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(0, 0, cardW, cardH)
                {
                    X = (Width / 2f) - 510f,
                    Y = centerYOffset + 260f,
                    Anchor = Anchor.Top,
                    RotationZ = 0f,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            AddElement(_cardRotZ);

            // 4. BOTTOM-RIGHT: Scale (Uniform Scale Pulsing)
            _cardScale = new VisualElement
            {
                Name = "CardScale",
                Text = "SCALE PULSE\nPulsing...",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59, 255),
                    Border = new BorderStyle { Color = new SKColor(34, 197, 94), Width = 3, Roundness = 12 },
                    Text = new TextStyle { Color = SKColors.White, Size = 16, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(0, 0, cardW, cardH)
                {
                    X = (Width / 2f) + 280f,
                    Y = centerYOffset + 260f,
                    Anchor = Anchor.Top,
                    ScaleX = 1.0f,
                    ScaleY = 1.0f,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            AddElement(_cardScale);

            // 5. CENTER: Constantly Rotating Card (Y Axis Spin, Clickable)
            _rotatingCard = new VisualElement
            {
                Name = "RotatingCard3D",
                Text = "3D SPIN Y\nHits: 0\n(Click Me)",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(23, 23, 37, 255),
                    Border = new BorderStyle
                    {
                        Color = new SKColor(0, 255, 128),
                        Width = 4,
                        Roundness = 16
                    },
                    Text = new TextStyle
                    {
                        Color = SKColors.White,
                        Size = 20,
                        Weight = 800,
                        Alignment = TextAlign.Center
                    },
                    Shadow = new ShadowStyle
                    {
                        Color = new SKColor(0, 255, 128, 120),
                        OffsetX = 0,
                        OffsetY = 0,
                        SpreadX = 15,
                        SpreadY = 15
                    }
                },
                Transform = new Transform(0, 0, 260, 260)
                {
                    X = (Width / 2f) - 130f,
                    Y = centerYOffset + 110f,
                    Anchor = Anchor.Top,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f,
                    Perspective = 800f
                }
            };
            
            _rotatingCard.Events.OnMouseDown += (s, e) =>
            {
                _clickCount++;
                _rotatingCard.Text = $"3D SPIN Y\nHits: {_clickCount}\n(Click Me)";
                
                var rng = new Random();
                _rotatingCard.Style.Border.Color = SKColor.FromHsv(rng.Next(360), 85, 100);
                _rotatingCard.Style.Shadow.Color = _rotatingCard.Style.Border.Color.WithAlpha(120);
                _rotatingCard.ScheduleRender();
            };
            AddElement(_rotatingCard);

            // Global mouse move handler for drag updates
            Events.OnMouseMove += (s, e) =>
            {
                if (_draggedCard != null)
                {
                    _draggedCard.Transform.X = e.Global.X - _dragOffsetX;
                    _draggedCard.Transform.Y = e.Global.Y - _dragOffsetY;
                }
            };

            // Global mouse up handler to release drag target
            Events.OnMouseUp += (s, e) =>
            {
                if (e.Button == 0 && _draggedCard != null)
                {
                    _draggedCard.ZIndex = 0;
                    _draggedCard.ScheduleRender();
                    _draggedCard = null;
                }
            };

            // Make each of the cards draggable (fixed size, absolute positioning)
            MakeDraggable(_cardRotY);
            MakeDraggable(_cardRotX);
            MakeDraggable(_cardRotZ);
            MakeDraggable(_cardScale);
            MakeDraggable(_rotatingCard);

            // Navigation back button
            var backBtn = new NeonButton("➜ BACK TO DASHBOARD", new SKColor(99, 102, 241), 320, 50)
            {
                Transform = { X = (Width / 2f) - 160f, Y = centerYOffset + 490f, Anchor = Anchor.Top },
                OnClick = () => 
                {
                    OnSwitchToDashboard?.Invoke();
                }
            };
            AddElement(backBtn);
        }

        public override void OnActivated()
        {
            if (!_running)
            {
                _running = true;
                _animationThread = new Thread(AnimateLoop) { IsBackground = true };
                _animationThread.Start();
            }
        }

        public override void OnDeactivated()
        {
            StopAnimation();
        }

        private void AnimateLoop()
        {
            while (_running)
            {
                _time += 0.03f;

                // 1. Continuous 3D spin Y and Nested Child animation
                if (_cardRotY != null)
                {
                    float angleY = (_time * 50f) % 360f;
                    _cardRotY.Transform.RotationY = angleY;
                    _cardRotY.Text = $"3D SPIN Y\nAngle: {angleY:F0}°";

                    if (_nestedChildY != null)
                    {
                        // Spin Z on child continuously (faster for visual contrast)
                        float childAngleZ = (_time * 120f) % 360f;
                        _nestedChildY.Transform.RotationZ = childAngleZ;

                        // Slide side-to-side to trigger parent clipping bounds
                        float childX = 50f + 65f * (float)Math.Sin(_time * 2.5f);
                        _nestedChildY.Transform.X = _cardRotY.Transform.X + childX;
                    }
                }

                // 2. Continuous 3D spin X
                if (_cardRotX != null)
                {
                    float angleX = (_time * 50f) % 360f;
                    _cardRotX.Transform.RotationX = angleX;
                    _cardRotX.Text = $"3D SPIN X\nAngle: {angleX:F0}°";
                }

                // 3. Continuous 2D flat spin Z
                if (_cardRotZ != null)
                {
                    float angleZ = (_time * 50f) % 360f;
                    _cardRotZ.Transform.RotationZ = angleZ;
                    _cardRotZ.Text = $"2D SPIN Z\nAngle: {angleZ:F0}°";
                }

                // 4. Uniform Scale Pulsing between 0.6x and 1.4x
                if (_cardScale != null)
                {
                    float scale = 1.0f + 0.4f * (float)Math.Sin(_time * 1.5f);
                    _cardScale.Transform.ScaleX = scale;
                    _cardScale.Transform.ScaleY = scale;
                    _cardScale.Text = $"SCALE PULSE\nScale: {scale:F2}x";
                }

                // 5. Continuous 3D Y-Axis spin with a gentle X wobble for premium depth
                if (_rotatingCard != null)
                {
                    float spinY = (_time * 70f) % 360f;
                    _rotatingCard.Transform.RotationY = spinY;
                    _rotatingCard.Transform.RotationX = 15f * (float)Math.Sin(_time * 1.0f);
                }

                Thread.Sleep(16); // ~60 FPS
            }
        }

        public void StopAnimation()
        {
            _running = false;
        }

        private void MakeDraggable(VisualElement card)
        {
            if (card == null) return;

            card.Transform.FixedWidth = true;
            card.Transform.FixedHeight = true;
            card.Transform.Anchor = Anchor.Top | Anchor.Left;

            card.Events.OnMouseDown += (sender, args) =>
            {
                if (args.Button == 0) // Left click
                {
                    _draggedCard = card;
                    _dragOffsetX = args.Global.X - card.Transform.X;
                    _dragOffsetY = args.Global.Y - card.Transform.Y;
                    card.ZIndex = 10; // Bring to front during drag
                    card.ScheduleRender();
                }
            };

            card.Events.OnMouseUp += (sender, args) =>
            {
                if (args.Button == 0 && _draggedCard == card)
                {
                    _draggedCard = null;
                    card.ZIndex = 0; // Restore default ZIndex
                    card.ScheduleRender();
                }
            };
        }
    }
}
