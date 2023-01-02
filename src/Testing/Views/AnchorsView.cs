// using System.Globalization;
// using System.Security.Cryptography;
// using System.Numerics;
// using System.Net.Mime;
// using System;
// using System.Diagnostics;
// using Blossom.Core;
// using Blossom.Core.Input;
// using Blossom.Core.Visual;
// using System.Drawing;
// using System.Threading;
// using SkiaSharp;
// using System.Linq;
// using System.Collections.Generic;

// namespace Blossom.Testing
// {
//     public class AnchorsView : View
//     {
//         private bool isDragged = false;
//         private Vector2 dragPoint;
//         VisualElement Draggable;

//         VisualElement AnchorTop;
//         VisualElement AnchorBottom;
//         VisualElement AnchorLeft;
//         VisualElement AnchorRight;

//         VisualElement TestElement;

//         public AnchorsView() : base("AnchorsView View")
//         {
//             this.Events.OnMouseMove += HandleMove;

//             this.Events.OnKeyDown += (int key) =>
//             {
//                 switch (key)
//                 {
//                     case 328: // UP
//                         HandleAnchorClick(AnchorTop, Anchor.Top);
//                         break;
//                     case 336: // DOWN
//                         HandleAnchorClick(AnchorBottom, Anchor.Bottom);
//                         break;
//                     case 331: // LEFT
//                         HandleAnchorClick(AnchorLeft, Anchor.Left);
//                         break;
//                     case 333: // RIGHT
//                         HandleAnchorClick(AnchorRight, Anchor.Right);
//                         break;
//                 }
//             };
//         }

//         public override void Main()
//         {
//             ElementStyle AnchorStyle = new()
//             {
//                 BackColor = new(200, 50, 35, 120),
//                 Border = new()
//                 {
//                     Roundness = 5,
//                     Width = 2f,
//                     Color = new(255, 255, 255, 255)
//                 },
//                 Shadow = new()
//                 {
//                     Color = new(0, 0, 0, 160),
//                     SpreadX = 5,
//                     SpreadY = 5,
//                 }
//             };

//             Draggable = new VisualElement()
//             {
//                 Name = "Draggable",
//                 Transform = new(200, 200, 450, 60)
//                 {
//                     Anchor = Anchor.Top | Anchor.Left,
//                     FixedWidth = false,
//                     FixedHeight = true,
//                     ValidateOnAnchor = true,
//                 },
//                 Style = new()
//                 {
//                     BackColor = new(255, 255, 255, 255),
//                     IsClipping = false,
//                     Border = new()
//                     {
//                         Roundness = 5,
//                         Width = 0.5f,
//                         Color = new(0, 0, 0, 25)
//                     },
//                     Text = new()
//                     {
//                         Alignment = TextAlign.Center,
//                         Color = new(200, 0, 0, 200),
//                         Size = 22,
//                         Spacing = 20,
//                         Padding = 20,
//                         Weight = 450,
//                     },
//                     Shadow = new()
//                     {
//                         Color = new(0, 0, 0, 35),
//                         SpreadX = 4,
//                         SpreadY = 4,
//                         OffsetY = 3
//                     }
//                 },
//                 Text = "Hold to Move"
//             };

//             AnchorTop = new VisualElement()
//             {
//                 Name = "Anchor Top",
//                 Transform = new((450 / 2) - 20, -8, 40, 16)
//                 {
//                     Anchor = Anchor.Top,
//                     FixedSize = true,
//                 },
//                 Style = new()
//                 {
//                     BackColor = SKColors.IndianRed,
//                     Border = new()
//                     {
//                         Roundness = 5,
//                     },
//                     Shadow = new()
//                     {
//                         Color = new(0, 0, 0, 25),
//                         SpreadX = 5,
//                         SpreadY = 5,
//                         OffsetY = 1
//                     }
//                 },
//             };

//             AnchorBottom = new VisualElement()
//             {
//                 Name = "Anchor Bottom",
//                 Transform = new((450 / 2) - 20, 52, 40, 16)
//                 {
//                     Anchor = Anchor.Bottom,
//                     FixedSize = true,
//                 },
//                 Style = new()
//                 {
//                     BackColor = new(255, 255, 255, 255),
//                     Border = new()
//                     {
//                         Roundness = 5,
//                     },
//                     Shadow = new()
//                     {
//                         Color = new(0, 0, 0, 25),
//                         SpreadX = 5,
//                         SpreadY = 5,
//                         OffsetY = 1
//                     }
//                 },
//             };

//             AnchorLeft = new VisualElement()
//             {
//                 Name = "Anchor Left",
//                 Transform = new(-8, 10, 16, 40)
//                 {
//                     Anchor = Anchor.Left,
//                     FixedSize = true,
//                 },
//                 Style = new()
//                 {
//                     BackColor = SKColors.IndianRed,
//                     Border = new()
//                     {
//                         Roundness = 5,
//                     },
//                     Shadow = new()
//                     {
//                         Color = new(0, 0, 0, 25),
//                         SpreadX = 5,
//                         SpreadY = 5,
//                         OffsetY = 1
//                     }
//                 },
//             };

//             AnchorRight = new VisualElement()
//             {
//                 Name = "Anchor Right",
//                 Transform = new(442, 10, 16, 40)
//                 {
//                     Anchor = Anchor.Right,
//                     FixedSize = true,
//                 },
//                 Style = new()
//                 {
//                     BackColor = new(255, 255, 255, 255),
//                     Border = new()
//                     {
//                         Roundness = 5,
//                         Width = 2f,
//                         // Color = new(0, 0, 0, 50)
//                     },
//                     Shadow = new()
//                     {
//                         Color = new(0, 0, 0, 25),
//                         SpreadX = 5,
//                         SpreadY = 5,
//                         OffsetY = 1
//                     }
//                 },
//             };

//             TestElement = new VisualElement() // Test
//             {
//                 Name = "Test Element",
//                 Text = "Testing element",
//                 Transform = new(50, 50, 80, 40)
//                 {
//                     Anchor = Anchor.Left | Anchor.Top,
//                     FixedSize = true,
//                 },
//                 Style = new()
//                 {
//                     BackColor = SKColors.HotPink,
//                     Border = new()
//                     {
//                         Roundness = 5,
//                         Width = 10f,
//                         Color = new(0, 0, 0, 50)
//                     },
//                 },
//             };

//             AnchorTop.Events.OnMouseClick += (_, _) => HandleAnchorClick(AnchorTop, Anchor.Top);
//             AnchorBottom.Events.OnMouseClick += (_, _) => HandleAnchorClick(AnchorBottom, Anchor.Bottom);
//             AnchorLeft.Events.OnMouseClick += (_, _) => HandleAnchorClick(AnchorLeft, Anchor.Left);
//             AnchorRight.Events.OnMouseClick += (_, _) => HandleAnchorClick(AnchorRight, Anchor.Right);

//             Draggable.Events.OnMouseDown += DraggableMouseDown;
//             Draggable.Events.OnMouseUp += DraggableMouseUp;

//             Draggable.AddChild(AnchorTop);
//             Draggable.AddChild(AnchorBottom);
//             Draggable.AddChild(AnchorLeft);
//             Draggable.AddChild(AnchorRight);

//             this.AddElement(Draggable);
//             this.AddElement(AnchorBottom);
//             this.AddElement(AnchorTop);
//             this.AddElement(AnchorLeft);
//             this.AddElement(AnchorRight);
//             this.AddElement(TestElement); // Test
//         }

//         private void HandleAnchorClick(VisualElement anchorEl, Anchor anchor)
//         {
//             if (Draggable.Transform.Anchor.HasFlag(anchor))
//             {
//                 anchorEl.Style.BackColor = SKColors.White;
//                 Draggable.Transform.Anchor &= ~anchor;
//             }
//             else
//             {
//                 anchorEl.Style.BackColor = SKColors.IndianRed;
//                 Draggable.Transform.Anchor |= anchor;
//             }
//         }

//         private void DraggableMouseDown(object obj, MouseEventArgs args)
//         {
//             Draggable.Transform.X -= 5;
//             Draggable.Transform.Width += 10;
//             Draggable.Transform.Y -= 5;
//             Draggable.Transform.Height += 10;

//             args.Relative.X += 5;
//             args.Relative.Y += 5;

//             dragPoint = args.Relative;
//             isDragged = true;

//             Draggable.Style.Border.Color = SKColors.IndianRed;
//             Draggable.Style.Border.Width = 2;

//             Draggable.Style.Shadow = new()
//             {
//                 Color = new(0, 0, 0, 0),
//                 SpreadX = 4,
//                 SpreadY = 4,
//                 OffsetY = 3
//             };

//             Draggable.Text = "Moving...";

//             Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Default);
//         }

//         private void DraggableMouseUp(object obj, MouseEventArgs args)
//         {
//             Draggable.Transform.X += 5;
//             Draggable.Transform.Width -= 10;
//             Draggable.Transform.Y += 5;
//             Draggable.Transform.Height -= 10;

//             isDragged = false;

//             Draggable.Style.Border.Color = new(0, 0, 0, 25);
//             Draggable.Style.Border.Width = 0.5f;

//             Draggable.Style.Shadow = new()
//             {
//                 Color = new(0, 0, 0, 35),
//                 SpreadX = 4,
//                 SpreadY = 4,
//                 OffsetY = 3
//             };

//             Draggable.Text = "Hold to Move";
//             Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Default);
//         }

//         private void HandleMove(object obj, MouseEventArgs args)
//         {
//             if (!isDragged) return;

//             this.RenderChanges(() =>
//             {
//                 Draggable.Transform.X = args.Global.X - dragPoint.X;
//                 Draggable.Transform.Y = args.Global.Y - dragPoint.Y;
//             });
//         }
//     }
// }