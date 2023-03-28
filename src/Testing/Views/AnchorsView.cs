using System.Globalization;
using System.Security.Cryptography;
using System.Numerics;
using System.Net.Mime;
using System;
using System.Diagnostics;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using System.Drawing;
using System.Threading;
using SkiaSharp;
using System.Linq;
using System.Collections.Generic;

namespace Blossom.Testing
{
    public class AnchorsView : View
    {
        private bool isDragged = false;
        private Vector2 dragPoint;
        VisualElement Draggable;

        VisualElement AnchorTop;
        VisualElement AnchorBottom;
        VisualElement AnchorLeft;
        VisualElement AnchorRight;

        public AnchorsView() : base("Anchors View")
        {
            Events.OnMouseMove += HandleMove;
            Events.OnKeyDown += (int key) =>
            {
                switch (key)
                {
                    case 328: // UP
                        HandleAnchorClick(AnchorTop, Anchor.Top);
                        break;
                    case 336: // DOWN
                        HandleAnchorClick(AnchorBottom, Anchor.Bottom);
                        break;
                    case 331: // LEFT
                        HandleAnchorClick(AnchorLeft, Anchor.Left);
                        break;
                    case 333: // RIGHT
                        HandleAnchorClick(AnchorRight, Anchor.Right);
                        break;
                }
            };
        }

        public override void Main()
        {
            Draggable = new VisualElement()
            {
                Name = "Draggable",
                IsClipping = false,
                Transform = new(200, 200, 450, 60)
                {
                    Anchor = Anchor.Top | Anchor.Left,
                    FixedWidth = false,
                    FixedHeight = true,
                    ValidateOnAnchor = true,
                },
                Style = new()
                {
                    BackColor = new(255, 255, 255, 255),
                    BackgroundPathEffect = SKPathEffect.CreateDiscrete(20, 1, 0),
                    Border = new()
                    {
                        Color = new(0, 0, 0, 70),
                        Width = 1f,
                        Roundness = 3,
                        PathEffect = SKPathEffect.CreateDiscrete(20, 1, 0)
                    },
                    Text = new()
                    {
                        Alignment = TextAlign.Center,
                        Color = new(25, 25, 25, 255),
                        Size = 24,
                        Spacing = 20,
                        Padding = 20,
                        Weight = 450,
                        PathEffect = SKPathEffect.CreateDiscrete(3, 0.5f, 0)
                    },
                },
                Text = "Hold to Move"
            };

            AnchorTop = new VisualElement()
            {
                Name = "Anchor Top",
                Transform = new((450 / 2) - 20, -8, 40, 16)
                {
                    Anchor = Anchor.Top,
                    FixedSize = true,
                },
                Style = new()
                {
                    BackColor = SKColors.Gray,
                    BackgroundPathEffect = SKPathEffect.CreateDiscrete(5, 1, 0),
                    Border = new()
                    {
                        Color = new(150, 150, 150),
                        Width = 1,
                        Roundness = 2,
                        PathEffect = SKPathEffect.CreateDiscrete(5, 1, 0)
                    },
                },
            };

            AnchorBottom = new VisualElement()
            {
                Name = "Anchor Bottom",
                Transform = new((450 / 2) - 20, 52, 40, 16)
                {
                    Anchor = Anchor.Bottom,
                    FixedSize = true,
                },
                Style = new()
                {
                    BackColor = new(255, 255, 255, 255),
                    BackgroundPathEffect = SKPathEffect.CreateDiscrete(5, 1, 0),
                    Border = new()
                    {
                        Color = new(150, 150, 150),
                        Width = 1,
                        Roundness = 2,
                        PathEffect = SKPathEffect.CreateDiscrete(5, 1, 0)
                    },
                },
            };

            AnchorLeft = new VisualElement()
            {
                Name = "Anchor Left",
                Transform = new(-8, 10, 16, 40)
                {
                    Anchor = Anchor.Left,
                    FixedSize = true,
                },
                Style = new()
                {
                    BackColor = SKColors.Gray,
                    BackgroundPathEffect = SKPathEffect.CreateDiscrete(5, 1, 0),
                    Border = new()
                    {
                        Color = new(150, 150, 150),
                        Width = 1,
                        Roundness = 2,
                        PathEffect = SKPathEffect.CreateDiscrete(5, 1, 0)
                    },
                },
            };

            AnchorRight = new VisualElement()
            {
                Name = "Anchor Right",
                Transform = new(442, 10, 16, 40)
                {
                    Anchor = Anchor.Right,
                    FixedSize = true,
                },
                Style = new()
                {
                    BackColor = new(255, 255, 255, 255),
                    BackgroundPathEffect = SKPathEffect.CreateDiscrete(5, 1, 0),
                    Border = new()
                    {
                        Color = new(150, 150, 150),
                        Width = 1,
                        Roundness = 2,
                        PathEffect = SKPathEffect.CreateDiscrete(5, 1, 0)
                    },
                },
            };

            AnchorTop.Events.OnMouseClick += (_, _) => HandleAnchorClick(AnchorTop, Anchor.Top);
            AnchorBottom.Events.OnMouseClick += (_, _) => HandleAnchorClick(AnchorBottom, Anchor.Bottom);
            AnchorLeft.Events.OnMouseClick += (_, _) => HandleAnchorClick(AnchorLeft, Anchor.Left);
            AnchorRight.Events.OnMouseClick += (_, _) => HandleAnchorClick(AnchorRight, Anchor.Right);

            Draggable.Events.OnMouseDown += DraggableMouseDown;
            Draggable.Events.OnMouseUp += DraggableMouseUp;

            AddElement(Draggable);

            Draggable.AddChild(AnchorTop);
            Draggable.AddChild(AnchorBottom);
            Draggable.AddChild(AnchorLeft);
            Draggable.AddChild(AnchorRight);
        }

        private void HandleAnchorClick(VisualElement anchorEl, Anchor anchor)
        {
            if (Draggable.Transform.Anchor.HasFlag(anchor))
            {
                anchorEl.Style.BackColor = SKColors.White;
                Draggable.Transform.Anchor &= ~anchor;
            }
            else
            {
                anchorEl.Style.BackColor = SKColors.Gray;
                Draggable.Transform.Anchor |= anchor;
            }
        }

        private void DraggableMouseDown(object obj, MouseEventArgs args)
        {
            Draggable.Transform.X -= 5;
            Draggable.Transform.Width += 10;
            Draggable.Transform.Y -= 5;
            Draggable.Transform.Height += 10;

            args.Relative.X += 5;
            args.Relative.Y += 5;

            dragPoint = args.Relative;
            isDragged = true;

            Draggable.Style.Border.Color = SKColors.Black;
            Draggable.Style.Border.Width = 2;

            Draggable.Text = "Moving...";

            Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Default);
        }

        private void DraggableMouseUp(object obj, MouseEventArgs args)
        {
            Draggable.Transform.X += 5;
            Draggable.Transform.Width -= 10;
            Draggable.Transform.Y += 5;
            Draggable.Transform.Height -= 10;

            isDragged = false;

            Draggable.Style.Border.Color = new(0, 0, 0, 70);
            Draggable.Style.Border.Width = 1f;

            Draggable.Text = "Hold to Move";
            Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Default);
        }

        private void HandleMove(object obj, MouseEventArgs args)
        {
            if (!isDragged) return;

            this.RenderChanges(() =>
            {
                Draggable.Transform.X = args.Global.X - dragPoint.X;
                Draggable.Transform.Y = args.Global.Y - dragPoint.Y;
            });
        }
    }
}