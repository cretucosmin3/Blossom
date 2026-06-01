using System;
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
        private VisualElement? _complexCard;

        private int _clickCount = 0;
        private float _time = 0f;
        private DateTime _lastTime = DateTime.Now;

        private VisualElement? _draggedCard;
        private float _dragOffsetX;
        private float _dragOffsetY;
        
        private VisualElement? _nestedChildY;
        private VisualElement? _nestedChildX;
        private VisualElement? _nestedChildZ;
        private VisualElement? _nestedChildXYZ;

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
                        Size = 30,
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
                Transform = new Transform(0, 25, Width, 45)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            AddElement(title);

            // Subtitle Description
            var subtitle = new VisualElement
            {
                Name = "3DSubtitle",
                Text = "All cards animate smoothly. Click cards to drag and rearrange. Click the center card to count hits.",
                Style = new ElementStyle
                {
                    Text = new TextStyle
                    {
                        Color = new SKColor(148, 163, 184),
                        Size = 14,
                        Weight = 400,
                        Alignment = TextAlign.Center
                    }
                },
                Transform = new Transform(0, 75, Width, 25)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right
                }
            };
            AddElement(subtitle);

            float cardW = 230f;
            float cardH = 150f;
            
            float col1X = (Width / 2f) - 510f;
            float col2X = (Width / 2f) - 130f;
            float col3X = (Width / 2f) + 280f;

            // --- Row 1 (Y = 130f) ---

            // Column 1: Rotation Y (Clipping Enabled)
            _cardRotY = new VisualElement
            {
                Name = "CardRotY",
                Text = "3D ROTATE Y\nSpinning...",
                IsClipping = true,
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59, 255),
                    Border = new BorderStyle { Color = new SKColor(56, 189, 248), Width = 3, Roundness = 12 },
                    Text = new TextStyle { Color = SKColors.White, Size = 15, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(col1X, 130f, cardW, cardH)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    RotationY = 0f,
                    Perspective = 500f,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            AddElement(_cardRotY);

            // Clipped Nested Child
            _nestedChildY = new VisualElement
            {
                Name = "NestedChildY",
                Text = "NESTED\nCLIP",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(244, 63, 94, 200), // Semitransparent rose
                    Border = new BorderStyle { Color = new SKColor(255, 255, 255), Width = 2, Roundness = 8 },
                    Text = new TextStyle { Color = SKColors.White, Size = 12, Weight = 800, Alignment = TextAlign.Center }
                },
                Transform = new Transform(50f, 20f, 130, 110)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            _cardRotY.AddChild(_nestedChildY);


            // Column 3: Rotation X (No Clipping)
            _cardRotX = new VisualElement
            {
                Name = "CardRotX",
                Text = "3D ROTATE X\nSpinning...",
                IsClipping = false,
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59, 255),
                    Border = new BorderStyle { Color = new SKColor(244, 63, 94), Width = 3, Roundness = 12 },
                    Text = new TextStyle { Color = SKColors.White, Size = 15, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(col3X, 130f, cardW, cardH)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    RotationX = 0f,
                    Perspective = 500f,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            AddElement(_cardRotX);

            // Unclipped Nested Child X (slithers outward)
            _nestedChildX = new VisualElement
            {
                Name = "NestedChildX",
                Text = "NESTED\nNO CLIP",
                IsClipping = false,
                Style = new ElementStyle
                {
                    BackColor = new SKColor(56, 189, 248, 180), // Semi-transparent cyan
                    Border = new BorderStyle { Color = new SKColor(56, 189, 248), Width = 2, Roundness = 10 },
                    Text = new TextStyle { Color = SKColors.White, Size = 11, Weight = 800, Alignment = TextAlign.Center }
                },
                Transform = new Transform(60f, 30f, 110, 90)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            _cardRotX.AddChild(_nestedChildX);


            // --- Row 1.5 (Y = 200f) ---

            // Column 2: Clickable rotating card in the center
            _rotatingCard = new VisualElement
            {
                Name = "RotatingCard3D",
                Text = "3D SPIN Y\nHits: 0\n(Click Me)",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(23, 23, 37, 255),
                    Border = new BorderStyle { Color = new SKColor(0, 255, 128), Width = 4, Roundness = 16 },
                    Text = new TextStyle { Color = SKColors.White, Size = 18, Weight = 800, Alignment = TextAlign.Center },
                    Shadow = new ShadowStyle { Color = new SKColor(0, 255, 128, 120), SpreadX = 15, SpreadY = 15 }
                },
                Transform = new Transform(col2X, 200f, 260f, 180f)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f,
                    Perspective = 800f
                }
            };
            _rotatingCard.Events.OnMouseDown += (s, e) =>
            {
                if (e.Button == 0)
                {
                    _clickCount++;
                    _rotatingCard.Text = $"3D SPIN Y\nHits: {_clickCount}\n(Click Me)";
                    var rng = new Random();
                    _rotatingCard.Style.Border.Color = SKColor.FromHsv(rng.Next(360), 85, 100);
                    _rotatingCard.Style.Shadow.Color = _rotatingCard.Style.Border.Color.WithAlpha(120);
                    _rotatingCard.ScheduleRender();
                }
            };
            AddElement(_rotatingCard);


            // --- Row 2 (Y = 300f) ---

            // Column 1: Rotation Z (2D Flat Spin)
            _cardRotZ = new VisualElement
            {
                Name = "CardRotZ",
                Text = "2D ROTATE Z\nSpinning...",
                IsClipping = false,
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59, 255),
                    Border = new BorderStyle { Color = new SKColor(168, 85, 247), Width = 3, Roundness = 12 },
                    Text = new TextStyle { Color = SKColors.White, Size = 15, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(col1X, 300f, cardW, cardH)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    RotationZ = 0f,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            AddElement(_cardRotZ);

            // Nested Child Z spinning on Y-axis
            _nestedChildZ = new VisualElement
            {
                Name = "NestedChildZ",
                Text = "3D IN 2D",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(168, 85, 247, 180), // Semi-transparent purple
                    Border = new BorderStyle { Color = SKColors.White, Width = 1.5f, Roundness = 8 },
                    Text = new TextStyle { Color = SKColors.White, Size = 11, Weight = 800, Alignment = TextAlign.Center }
                },
                Transform = new Transform(65f, 30f, 100, 90)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f,
                    Perspective = 300f
                }
            };
            _cardRotZ.AddChild(_nestedChildZ);


            // Column 3: Scale Card (Scale Pulsing)
            _cardScale = new VisualElement
            {
                Name = "CardScale",
                Text = "SCALE PULSE\nPulsing...",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59, 255),
                    Border = new BorderStyle { Color = new SKColor(34, 197, 94), Width = 3, Roundness = 12 },
                    Text = new TextStyle { Color = SKColors.White, Size = 15, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(col3X, 300f, cardW, cardH)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    ScaleX = 1.0f,
                    ScaleY = 1.0f,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            AddElement(_cardScale);


            // --- Row 3 (Y = 470f) ---

            // Column 2: Complex multi-axis spinning card with orbiting child
            _complexCard = new VisualElement
            {
                Name = "ComplexCard",
                Text = "MULTI AXIS\nSpinning...",
                IsClipping = false,
                Style = new ElementStyle
                {
                    BackColor = new SKColor(23, 23, 37, 255),
                    Border = new BorderStyle { Color = new SKColor(236, 72, 153), Width = 3, Roundness = 30 }, // Large roundness
                    Text = new TextStyle { Color = SKColors.White, Size = 15, Weight = 800, Alignment = TextAlign.Center },
                    Shadow = new ShadowStyle { Color = new SKColor(236, 72, 153, 90), SpreadX = 20, SpreadY = 20 } // Large shadow glow
                },
                Transform = new Transform(col2X, 470f, 260f, 150f)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    Perspective = 600f,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            AddElement(_complexCard);

            // Orbiting child
            _nestedChildXYZ = new VisualElement
            {
                Name = "NestedChildXYZ",
                Text = "ORBIT",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(236, 72, 153, 200),
                    Border = new BorderStyle { Color = SKColors.White, Width = 2, Roundness = 12 },
                    Text = new TextStyle { Color = SKColors.White, Size = 11, Weight = 800, Alignment = TextAlign.Center }
                },
                Transform = new Transform(85f, 30f, 90, 90)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    TransformOriginX = 0.5f,
                    TransformOriginY = 0.5f
                }
            };
            _complexCard.AddChild(_nestedChildXYZ);


            // Global mouse drag logic
            Events.OnMouseMove += (s, e) =>
            {
                if (_draggedCard != null)
                {
                    _draggedCard.Transform.X = e.Global.X - _dragOffsetX;
                    _draggedCard.Transform.Y = e.Global.Y - _dragOffsetY;
                }
            };

            Events.OnMouseUp += (s, e) =>
            {
                if (e.Button == 0 && _draggedCard != null)
                {
                    _draggedCard.ZIndex = 0;
                    _draggedCard.ScheduleRender();
                    _draggedCard = null;
                }
            };

            // Bind dragging
            MakeDraggable(_cardRotY);
            MakeDraggable(_cardRotX);
            MakeDraggable(_cardRotZ);
            MakeDraggable(_cardScale);
            MakeDraggable(_rotatingCard);
            MakeDraggable(_complexCard);

            // Navigation back button
            var backBtn = new NeonButton("➜ BACK TO DASHBOARD", new SKColor(99, 102, 241), 320, 42)
            {
                Transform = { X = (Width / 2f) - 160f, Y = 640f, Anchor = Anchor.Top | Anchor.Left },
                OnClick = () => OnSwitchToDashboard?.Invoke()
            };
            AddElement(backBtn);
        }

        public override void OnActivated()
        {
            _lastTime = DateTime.Now;
            Loop += Animate;
        }

        public override void OnDeactivated()
        {
            Loop -= Animate;
        }

        private void Animate()
        {
            var now = DateTime.Now;
            float dt = (float)(now - _lastTime).TotalSeconds;
            _lastTime = now;

            if (dt > 0.1f) dt = 0.1f;

            // Controlled time scale for slower, readable animations
            _time += dt * 0.7f;

            // 1. Row 1 Column 1: Rotate Y (Clipped Child)
            if (_cardRotY != null)
            {
                float angleY = (_time * 25f) % 360f;
                _cardRotY.Transform.RotationY = angleY;
                _cardRotY.Text = $"3D SPIN Y\nAngle: {angleY:F0}°";

                if (_nestedChildY != null)
                {
                    float childAngleZ = (_time * 50f) % 360f;
                    _nestedChildY.Transform.RotationZ = childAngleZ;

                    float childX = 50f + 65f * (float)Math.Sin(_time * 1.5f);
                    _nestedChildY.Transform.X = _cardRotY.Transform.X + childX;
                }
            }

            // 2. Row 1 Column 3: Rotate X (Unclipped child slithering)
            if (_cardRotX != null)
            {
                float angleX = (_time * 25f) % 360f;
                _cardRotX.Transform.RotationX = angleX;
                _cardRotX.Text = $"3D SPIN X\nAngle: {angleX:F0}°";

                if (_nestedChildX != null)
                {
                    float childAngleX_Y = (_time * -40f) % 360f;
                    _nestedChildX.Transform.RotationY = childAngleX_Y;

                    float childX_X = 60f + 95f * (float)Math.Sin(_time * 1.2f);
                    float childY_X = 30f + 45f * (float)Math.Cos(_time * 1.2f);
                    _nestedChildX.Transform.X = _cardRotX.Transform.X + childX_X;
                    _nestedChildX.Transform.Y = _cardRotX.Transform.Y + childY_X;
                }
            }

            // 3. Row 1.5 Column 2: Center Interactive Y-Spin
            if (_rotatingCard != null)
            {
                float spinY = (_time * 30f) % 360f;
                _rotatingCard.Transform.RotationY = spinY;
                _rotatingCard.Transform.RotationX = 8f * (float)Math.Sin(_time * 0.7f);
            }

            // 4. Row 2 Column 1: Rotate Z (Unclipped child 3D-in-2D)
            if (_cardRotZ != null)
            {
                float angleZ = (_time * 20f) % 360f;
                _cardRotZ.Transform.RotationZ = angleZ;
                _cardRotZ.Text = $"2D SPIN Z\nAngle: {angleZ:F0}°";

                if (_nestedChildZ != null)
                {
                    float childAngleZ_Y = (_time * 60f) % 360f;
                    _nestedChildZ.Transform.RotationY = childAngleZ_Y;
                }
            }

            // 5. Row 2 Column 3: Scale Pulse
            if (_cardScale != null)
            {
                float scale = 1.0f + 0.25f * (float)Math.Sin(_time * 1.2f);
                _cardScale.Transform.ScaleX = scale;
                _cardScale.Transform.ScaleY = scale;
                _cardScale.Text = $"SCALE PULSE\nScale: {scale:F2}x";
            }

            // 6. Row 3 Column 2: Complex Card & Orbiting Child
            if (_complexCard != null)
            {
                float rX = (_time * 12f) % 360f;
                float rY = (_time * 24f) % 360f;
                float rZ = (_time * 8f) % 360f;
                _complexCard.Transform.RotationX = rX;
                _complexCard.Transform.RotationY = rY;
                _complexCard.Transform.RotationZ = rZ;
                _complexCard.Text = $"MULTI-AXIS\nX:{rX:F0}° Y:{rY:F0}°";

                if (_nestedChildXYZ != null)
                {
                    float orbitAngle = _time * 1.8f;
                    float radius = 120f;
                    _nestedChildXYZ.Transform.X = _complexCard.Transform.X + 85f + radius * (float)Math.Cos(orbitAngle);
                    _nestedChildXYZ.Transform.Y = _complexCard.Transform.Y + 30f + radius * (float)Math.Sin(orbitAngle);
                    _nestedChildXYZ.Transform.RotationZ = (_time * -50f) % 360f;
                }
            }
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
                    card.ZIndex = 10;
                    card.ScheduleRender();
                }
            };

            card.Events.OnMouseUp += (sender, args) =>
            {
                if (args.Button == 0 && _draggedCard == card)
                {
                    _draggedCard = null;
                    card.ZIndex = 0;
                    card.ScheduleRender();
                }
            };
        }
    }
}
