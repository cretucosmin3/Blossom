using System;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using SkiaSharp;
using System.Collections.Generic;
using System.Diagnostics;

namespace Blossom.Testing;

public class DrawingView : View
{
    List<VisualElement> ColorSelectors = new();
    VisualElement Reset;
    VisualElement Clear;

    private readonly Stopwatch ColorPickerTimer = new();
    private VisualElement LastColorClicked;

    private readonly List<VisualElement> DrawBlocks = new();
    private readonly Dictionary<string, SKColor> ColorsToPick = new() {
            {"White", SKColors.White},
            {"Black", SKColors.Black},
        };

    private VisualElement PreviousPicker;

    private SKColor ColorToDraw = SKColors.DimGray;

    public DrawingView() : base("Drawing View")
    {
        BackColor = new(35, 35, 35, 255);

        PopulateColorsToSelect();
    }

    private void PopulateColorsToSelect()
    {
        for (int i = 0; i < 30; i++)
        {
            var R = Random.Shared.Next(255);
            var G = Random.Shared.Next(255);
            var B = Random.Shared.Next(255);
            ColorsToPick.Add($"{R} {G} {B}", new SKColor((byte)R, (byte)G, (byte)B));
        }
    }

    public override void Main()
    {
        float totalWidth = Browser.window.Size.X;
        float totalHeight = Browser.window.Size.Y - 50;
        const float columns = 74;
        const float rows = 37;
        const float margin = .5f;

        totalWidth -= margin * 2f;
        totalHeight -= margin * 2f;

        float rectWidth = (totalWidth - ((columns - 1) * margin)) / columns;
        float rectHeight = (totalHeight - ((rows - 1) * margin)) / rows;

        for (float row = 0f; row < rows; row++)
        {
            for (float col = 0f; col < columns; col++)
            {
                float x = col * (rectWidth + margin + margin);
                float y = (row * (rectHeight + margin + margin)) + 45;

                var NewElement = new VisualElement()
                {
                    Name = $"element {row}:{col}",
                    IsClipping = false,
                    Transform = new(x, y, rectWidth, rectHeight),
                    Style = new()
                    {
                        BackColor = new(255, 255, 255, 255),
                        Border = new()
                        {
                            Roundness = 0,
                            Width = 1f,
                            Color = new(0, 0, 0, 10)
                        },
                    },
                };

                NewElement.Events.OnMouseEnter += OnMouseEnter;
                NewElement.Events.OnMouseLeave += OnMouseLeave;
                NewElement.Events.OnMouseClick += OnMouseClick;

                DrawBlocks.Add(NewElement);
            }
        }

        foreach (var element in DrawBlocks)
        {
            AddElement(element);
        }

        DoColorSelectors();

        PerformColorSelect(ColorSelectors[1]);
    }

    public void DoColorSelectors()
    {
        float incrementalX = 225;
        foreach (var (name, color) in ColorsToPick)
        {
            var newColorPicker = new VisualElement()
            {
                Name = name,
                IsClipping = false,
                Transform = new(incrementalX, 15, 40, 40)
                {
                    Anchor = Anchor.Left | Anchor.Top,
                    FixedWidth = true,
                    FixedHeight = true
                },
                Style = new()
                {
                    BackColor = color,
                    Border = new()
                    {
                        Roundness = 2,
                        Width = 4,
                        Color = new(0, 0, 0, 0)
                    },
                },
            };

            incrementalX += 40 + 10;

            newColorPicker.Events.OnMouseClick += OnColorSelectorClick;
            newColorPicker.Events.OnMouseDown += (el, _) =>
            {
                ColorPickerTimer.Restart();
                LastColorClicked = (VisualElement)el;
            };

            newColorPicker.Events.OnMouseUp += (el, _) =>
            {
                var target = (VisualElement)el;
                if (LastColorClicked != el) return;

                Console.WriteLine(target.Name + " Left");
                var color = target.Style.BackColor;
                ColorPickerTimer.Stop();
                var elapsedMs = ColorPickerTimer.ElapsedMilliseconds;

                if (elapsedMs > 1500)
                {
                    ClearAll(color);
                }
            };

            AddElement(newColorPicker);

            ColorSelectors.Add(newColorPicker);
        }

        Reset = new VisualElement()
        {
            Name = "Reset",
            IsClipping = false,
            Transform = new(120, 15, 40, 40)
            {
                Anchor = Anchor.Left | Anchor.Top,
                FixedWidth = true,
                FixedHeight = true
            },
            Style = new()
            {
                BackColor = SKColors.IndianRed,
                Border = new()
                {
                    Roundness = 2,
                    Width = 2f,
                    Color = new(0, 0, 0, 70)
                },
                Text = new()
                {
                    Size = 16,
                    Alignment = TextAlign.Center,
                    Weight = 700,
                    Color = SKColors.White
                }
            },
            Text = "R"
        };

        Clear = new VisualElement()
        {
            Name = "Clear",
            IsClipping = false,
            Transform = new(10, 15, 100, 40)
            {
                Anchor = Anchor.Left | Anchor.Top,
                FixedWidth = true,
                FixedHeight = true
            },
            Style = new()
            {
                BackColor = SKColors.White,
                Border = new()
                {
                    Roundness = 2,
                    Width = 2f,
                    Color = new(0, 0, 0, 70)
                },
                Text = new()
                {
                    Size = 16,
                    Alignment = TextAlign.Center,
                    Weight = 700,
                    Color = new(0, 0, 0, 160)
                }
            },
            Text = "Clear"
        };

        Reset.Events.OnMouseClick += OnResetClicked;
        Clear.Events.OnMouseClick += OnClearClicked;

        AddElement(Reset);
        AddElement(Clear);
    }

    public void OnColorSelectorClick(object obj, MouseEventArgs args)
    {
        var target = (VisualElement)obj;
        PerformColorSelect(target);
    }

    public void PerformColorSelect(VisualElement target)
    {
        var NameOfColor = target.Name;
        ColorToDraw = ColorsToPick[NameOfColor];

        target.Style.Border.Width = 5;
        target.Style.Border.Color = new(255, 255, 255, 255);

        if (PreviousPicker != null && PreviousPicker != target)
        {
            PreviousPicker.Style.Border.Width = 1;
            PreviousPicker.Style.Border.Color = new(0, 0, 0, 10);
        }

        PreviousPicker = target;
    }

    public void OnMouseEnter(VisualElement el)
    {
        el.Style.Border.Width = 2;
        el.Style.Border.Color = ColorToDraw;

        if (Events.IsMouseDown(0))
        {
            MarkAsDrawn(el);
        }
    }

    public void OnMouseLeave(VisualElement el)
    {
        el.Style.Border.Width = 1;
        el.Style.Border.Color = new(0, 0, 0, 10);
    }

    public void OnMouseClick(object target, Core.Input.MouseEventArgs e)
    {
        var element = (VisualElement)target;

        MarkAsDrawn(element);
    }

    public void OnResetClicked(object obj, MouseEventArgs args)
    {
        ColorsToPick.Clear();

        ColorsToPick.Add("White", SKColors.White);
        ColorsToPick.Add("Black", SKColors.Black);

        PopulateColorsToSelect();

        int x = 0;
        foreach (var (name, color) in ColorsToPick)
        {
            ColorSelectors[x].Name = name;
            ColorSelectors[x].Style.BackColor = color;
            x++;
        }
    }

    public void OnClearClicked(object obj, MouseEventArgs args)
    {
        foreach (var cell in DrawBlocks)
        {
            cell.Style.BackColor = SKColors.White;
            cell.Style.Border.Width = 1;
            cell.Style.Border.Color = new(0, 0, 0, 10);
        }
    }

    public void MarkAsDrawn(VisualElement element)
    {
        element.Style.BackColor = ColorToDraw;
        element.Style.Border.Width = 1;
        element.Style.Border.Color = new(0, 0, 0, 10);
    }

    public void ClearAll(SKColor colorToReset)
    {
        foreach (var cell in DrawBlocks)
        {
            cell.Style.BackColor = colorToReset;
            cell.Style.Border.Width = 1;
            cell.Style.Border.Color = new(0, 0, 0, 10);
        }
    }
}