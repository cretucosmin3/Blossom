
// using System.Runtime.CompilerServices;
// using System.Globalization;
// using Blossom.Core;
// using Blossom.Core.Input;
// using Blossom.Core.Visual;
// using SkiaSharp;
// using System.Collections.Generic;
// using System.Numerics;
// using System;

// namespace Blossom.Testing;

// public class LoadView : View
// {
//     VisualElement GraySelector;
//     VisualElement BlackSelector;
//     VisualElement PinkSelector;
//     VisualElement RedSelector;
//     VisualElement YellowSelector;
//     VisualElement BlueSelector;
//     VisualElement Clear;
//     VisualElement RendersLabel;

//     private readonly List<VisualElement> DrawBlocks = new();
//     private readonly Dictionary<string, SKColor> ColorsToPick = new() {
//             {"Gray", SKColors.DimGray},
//             {"Black", new(20, 20, 20, 255)},
//             {"Pink", SKColors.HotPink},
//             {"Red", SKColors.Red},
//             {"Yellow", SKColors.Yellow},
//             {"Blue", SKColors.Blue},
//         };

//     private VisualElement PreviousPicker;

//     private SKColor ColorToDraw = SKColors.DimGray;

//     public LoadView() : base("LoadView View")
//     {
//         this.Events.OnKeyType += OnKeyPress;
//     }

//     public void OnKeyPress(char key)
//     {
//         switch (key)
//         {
//             case '1':
//                 OnColorSelectorClick(GraySelector, default);
//                 break;
//             case '2':
//                 OnColorSelectorClick(BlackSelector, default);
//                 break;
//             case '3':
//                 OnColorSelectorClick(PinkSelector, default);
//                 break;
//             case '4':
//                 OnColorSelectorClick(RedSelector, default);
//                 break;
//             case '5':
//                 OnColorSelectorClick(YellowSelector, default);
//                 break;
//             case '6':
//                 OnColorSelectorClick(BlueSelector, default);
//                 break;
//         }
//     }

//     public override void Main()
//     {
//         float totalWidth = Browser.window.Size.X;
//         float totalHeight = Browser.window.Size.Y - 80;
//         const float columns = 68;
//         const float rows = 42;
//         const float margin = 0.5f;

//         totalWidth -= margin * 2f;
//         totalHeight -= margin * 2f;

//         float rectWidth = (totalWidth - ((columns - 1) * margin)) / columns;
//         float rectHeight = (totalHeight - ((rows - 1) * margin)) / rows;

//         for (float row = 0f; row < rows; row++)
//         {
//             for (float col = 0f; col < columns; col++)
//             {
//                 float x = col * (rectWidth + margin + margin);
//                 float y = row * (rectHeight + margin + margin) + 60;

//                 var NewElement = new VisualElement()
//                 {
//                     Name = $"element {row}:{col}",
//                     Transform = new(x, y, rectWidth, rectHeight),
//                     Style = new()
//                     {
//                         IsClipping = true,
//                         // BackColor = new(22, 22, 22, 45),
//                         BackColor = new(0, 0, 0, 0),
//                         Border = new()
//                         {
//                             Roundness = 0,
//                             Width = 1f,
//                             Color = new(0, 0, 0, 10)
//                         },
//                     },
//                 };

//                 NewElement.Events.OnMouseEnter += OnMouseEnter;
//                 NewElement.Events.OnMouseLeave += OnMouseLeave;
//                 NewElement.Events.OnMouseClick += OnMouseClick;

//                 DrawBlocks.Add(NewElement);
//             }
//         }

//         foreach (var element in DrawBlocks)
//         {
//             AddElement(element);
//         }
//         DoColorSelectors();

//         RendersLabel = new()
//         {
//             Name = "Renders",
//             Transform = new(600, 10, 40, 40)
//             {
//                 Anchor = Anchor.Right,
//                 FixedWidth = true,
//                 FixedHeight = true
//             },
//             Style = new()
//             {
//                 IsClipping = true,
//                 Text = new()
//                 {
//                     Size = 18,
//                     Color = SKColors.Black,
//                     Alignment = TextAlign.Center
//                 }
//             },
//         };

//         AddElement(RendersLabel);

//         Browser.OnRenderRequired += () =>
//         {
//             Browser.SkipCountingNextRender = true;
//             RendersLabel.Text = $"Renders: {Browser.TotalRenders}";
//         };
//     }

//     public void DoColorSelectors()
//     {
//         GraySelector = new VisualElement()
//         {
//             Name = "Gray",
//             Transform = new(100, 10, 40, 40)
//             {
//                 Anchor = Anchor.Left,
//                 FixedWidth = true,
//                 FixedHeight = true
//             },
//             Style = new()
//             {
//                 IsClipping = true,
//                 BackColor = new(100, 100, 100, 255),
//                 Border = new()
//                 {
//                     Roundness = 5,
//                     Width = 3,
//                     Color = new(0, 0, 0, 100)
//                 },
//             },
//         };

//         BlackSelector = new VisualElement()
//         {
//             Name = "Black",
//             Transform = new(100 + 40 + 10, 10, 40, 40)
//             {
//                 Anchor = Anchor.Left,
//                 FixedWidth = true,
//                 FixedHeight = true
//             },
//             Style = new()
//             {
//                 IsClipping = true,
//                 BackColor = new(20, 20, 20, 255),
//                 Border = new()
//                 {
//                     Roundness = 2,
//                     Width = 2f,
//                     Color = new(0, 0, 0, 50)
//                 },
//             },
//         };

//         PinkSelector = new VisualElement()
//         {
//             Name = "Pink",
//             Transform = new(100 + 80 + 20, 10, 40, 40)
//             {
//                 Anchor = Anchor.Left,
//                 FixedWidth = true,
//                 FixedHeight = true
//             },
//             Style = new()
//             {
//                 IsClipping = true,
//                 BackColor = SKColors.HotPink,
//                 Border = new()
//                 {
//                     Roundness = 2,
//                     Width = 2f,
//                     Color = new(0, 0, 0, 50)
//                 },
//             },
//         };
//         RedSelector = new VisualElement()
//         {
//             Name = "Red",
//             Transform = new(100 + 120 + 30, 10, 40, 40)
//             {
//                 Anchor = Anchor.Left,
//                 FixedWidth = true,
//                 FixedHeight = true
//             },
//             Style = new()
//             {
//                 IsClipping = true,
//                 BackColor = SKColors.Red,
//                 Border = new()
//                 {
//                     Roundness = 2,
//                     Width = 2f,
//                     Color = new(0, 0, 0, 50)
//                 },
//             },
//         };

//         YellowSelector = new VisualElement()
//         {
//             Name = "Yellow",
//             Transform = new(100 + 160 + 40, 10, 40, 40)
//             {
//                 Anchor = Anchor.Left,
//                 FixedWidth = true,
//                 FixedHeight = true
//             },
//             Style = new()
//             {
//                 IsClipping = true,
//                 BackColor = SKColors.Yellow,
//                 Border = new()
//                 {
//                     Roundness = 2,
//                     Width = 2f,
//                     Color = new(0, 0, 0, 50)
//                 },
//             },
//         };

//         BlueSelector = new VisualElement()
//         {
//             Name = "Blue",
//             Transform = new(100 + 200 + 50, 10, 40, 40)
//             {
//                 Anchor = Anchor.Left,
//                 FixedWidth = true,
//                 FixedHeight = true
//             },
//             Style = new()
//             {
//                 IsClipping = true,
//                 BackColor = SKColors.Blue,
//                 Border = new()
//                 {
//                     Roundness = 2,
//                     Width = 2f,
//                     Color = new(0, 0, 0, 50)
//                 },
//             },
//         };

//         Clear = new VisualElement()
//         {
//             Name = "Clear",
//             Transform = new(100 + 240 + 60, 10, 120, 40)
//             {
//                 Anchor = Anchor.Left,
//                 FixedWidth = true,
//                 FixedHeight = true
//             },
//             Style = new()
//             {
//                 IsClipping = true,
//                 BackColor = SKColors.White,
//                 Border = new()
//                 {
//                     Roundness = 2,
//                     Width = 2f,
//                     Color = new(0, 0, 0, 70)
//                 },
//                 Text = new()
//                 {
//                     Size = 16,
//                     Alignment = TextAlign.Center,
//                     Weight = 700,
//                     Color = new(0, 0, 0, 160)
//                 }
//             },
//             Text = "Clear"
//         };

//         GraySelector.Events.OnMouseClick += OnColorSelectorClick;
//         BlackSelector.Events.OnMouseClick += OnColorSelectorClick;
//         PinkSelector.Events.OnMouseClick += OnColorSelectorClick;
//         RedSelector.Events.OnMouseClick += OnColorSelectorClick;
//         YellowSelector.Events.OnMouseClick += OnColorSelectorClick;
//         BlueSelector.Events.OnMouseClick += OnColorSelectorClick;
//         Clear.Events.OnMouseClick += OnClearClicked;

//         AddElement(GraySelector);
//         AddElement(BlackSelector);
//         AddElement(PinkSelector);
//         AddElement(RedSelector);
//         AddElement(YellowSelector);
//         AddElement(BlueSelector);
//         AddElement(Clear);
//     }

//     public void OnColorSelectorClick(object obj, MouseEventArgs args)
//     {
//         var target = (VisualElement)obj;
//         var NameOfColor = target.Name;
//         ColorToDraw = ColorsToPick[NameOfColor];

//         target.Style.Border.Width = 3;
//         target.Style.Border.Color = new(0, 0, 0, 100);

//         if (PreviousPicker != null)
//         {
//             PreviousPicker.Style.Border.Width = 1;
//             PreviousPicker.Style.Border.Color = new(0, 0, 0, 10);
//         }

//         PreviousPicker = target;
//     }

//     public void OnMouseEnter(VisualElement el)
//     {
//         el.Style.Border.Width = 2;
//         el.Style.Border.Color = ColorToDraw;

//         if (Events.isMouseDown(0))
//         {
//             MarkAsDrawn(el);
//         }
//     }

//     public void OnMouseLeave(VisualElement el)
//     {
//         el.Style.Border.Width = 1;
//         el.Style.Border.Color = new(0, 0, 0, 10);
//     }

//     public void OnMouseClick(object target, Core.Input.MouseEventArgs e)
//     {
//         var element = (VisualElement)target;

//         if (element.Style.BackColor.Alpha == 0)
//             MarkAsDrawn(element);
//         else
//             MarkAsClear(element);
//     }

//     public void OnClearClicked(object obj, MouseEventArgs args)
//     {
//         foreach (var cell in DrawBlocks)
//         {
//             MarkAsClear(cell);
//         }
//     }

//     public void MarkAsDrawn(VisualElement element)
//     {
//         element.Style.BackColor = ColorToDraw;
//         element.Style.Border.Width = 1;
//         element.Style.Border.Color = new(0, 0, 0, 10);
//     }

//     private void MarkAsClear(VisualElement element)
//     {
//         element.Style.BackColor = new(0, 0, 0, 0);
//         element.Style.Border.Width = 1;
//         element.Style.Border.Color = new(0, 0, 0, 10);
//     }
// }