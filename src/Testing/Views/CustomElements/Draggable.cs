using System.Threading;
using System;
using System.Numerics;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.CustomElements;

public class Draggable : VisualElement
{
    private bool isDragged = false;
    private Vector2 dragPoint;

    public SKColor DownColor { get; set; } = new(255, 100, 57);
    public SKColor UpColor { get; set; } = new(252, 80, 27);

    public Action<VisualElement> OnDragged;
    public Action<VisualElement> OnDropped;

    public Draggable()
    {
        Style = new()
        {
            BackColor = UpColor,
            Border = new()
            {
                Roundness = 15f
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
                Color = SKColors.White,
                Shadow = new()
                {
                    Color = new(0, 0, 0, 75),
                    SpreadX = 2,
                    SpreadY = 2,
                    OffsetY = 3,
                    OffsetX = 0
                },
            }
        };
    }

    public override void AddedToView()
    {
        Events.OnMouseDown += DraggableMouseDown;
        Events.OnMouseUp += DraggableMouseUp;
        ParentView.Events.OnMouseMove += DraggableMouseMove;
    }

    private void DraggableMouseMove(object obj, MouseEventArgs args)
    {
        if (!isDragged) return;

        var parentX = Parent != null ? Parent.Transform.Computed.X : 0;
        var parentY = Parent != null ? Parent.Transform.Computed.Y : 0;

        ParentView.RenderChanges(() =>
        {
            Transform.X = args.Relative.X - dragPoint.X - parentX;
            Transform.Y = args.Relative.Y - dragPoint.Y - parentY;
        });

        OnDragged?.Invoke(this);
    }

    private void DraggableMouseDown(object obj, MouseEventArgs args)
    {
        isDragged = true;
        dragPoint = args.Relative;

        Style.BackColor = DownColor;

        Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Hand);
    }

    private void DraggableMouseUp(object obj, MouseEventArgs args)
    {
        if (!isDragged) return;

        isDragged = false;
        Style.BackColor = UpColor;

        Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Default);
        OnDropped?.Invoke(this);
    }
}