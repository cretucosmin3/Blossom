using System.Threading;
using System;
using System.Numerics;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.CustomElements;

public class Button : VisualElement
{
    private int counter;
    private bool IsHovered;
    public SKColor DownColor { get; set; } = new(215, 225, 225, 255);
    public SKColor UpColor { get; set; } = SKColors.White;
    public SKColor HoverColor { get; set; } = new(235, 235, 235, 255);

    public Button()
    {
        Style = new()
        {
            BackColor = UpColor,
            Border = new()
            {
                Roundness = 3f
            },
            Shadow = new()
            {
                Color = new(0, 0, 0, 75),
                SpreadX = 4,
                SpreadY = 4,
                OffsetY = 4,
                OffsetX = 0
            },
            Text = new()
            {
                Size = 24,
                Weight = 400,
                Color = SKColors.DarkSlateGray,
            }
        };
    }

    public override void AddedToView()
    {
        Events.OnMouseDown += OnMouseDown;
        Events.OnMouseUp += OnMouseUp;
        Events.OnMouseEnter += OnMouseEnter;
        Events.OnMouseLeave += OnMouseLeave;
    }

    private void OnMouseDown(object obj, MouseEventArgs args)
    {
        Style.BackColor = DownColor;
        counter++;
        Text = $"{counter}";
    }

    private void OnMouseUp(object obj, MouseEventArgs args)
    {
        Style.BackColor = IsHovered ? HoverColor : UpColor;
    }

    private void OnMouseEnter(VisualElement el)
    {
        IsHovered = true;
        Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Hand);
        Style.BackColor = HoverColor;

        Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Default);
    }

    private void OnMouseLeave(VisualElement el)
    {
        IsHovered = false;
        Style.BackColor = UpColor;

        Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Default);
    }
}