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
    private readonly float InflationWhenDragged = 6f;

    public Action<VisualElement> OnDragged;
    public Action<VisualElement> OnDropped;

    public Draggable()
    {
        Style = new()
        {
            BackColor = new(245, 245, 245, 255),
            IsClipping = false,
            Border = new()
            {
                Roundness = 5,
                Width = 3f,
                Color = new(0, 0, 0, 255),
            },
            Shadow = new()
            {
                Color = new(0, 0, 0, 0),
                SpreadX = 1,
                SpreadY = 1,
                OffsetY = 6,
                OffsetX = 1
            },
            Text = new()
            {
                Size = 42,
                Weight = 900,
                Color = new(0, 0, 0, 150),
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

        ParentView.RenderChanges(() =>
        {
            Transform.X = args.Global.X - dragPoint.X;
            Transform.Y = args.Global.Y - dragPoint.Y;
        });

        OnDragged?.Invoke(this);
    }

    private void DraggableMouseDown(object obj, MouseEventArgs args)
    {
        isDragged = true;

        Transform.X -= InflationWhenDragged / 2f;
        Transform.Width += InflationWhenDragged;
        Transform.Y -= InflationWhenDragged / 2f;
        Transform.Height += InflationWhenDragged;

        args.Relative.X += InflationWhenDragged / 2f;
        args.Relative.Y += InflationWhenDragged / 2f;

        dragPoint = args.Relative;

        Style.BackColor = SKColors.AliceBlue;

        Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Hand);
    }

    private void DraggableMouseUp(object obj, MouseEventArgs args)
    {
        if (!isDragged) return;
        isDragged = false;

        Transform.X += InflationWhenDragged / 2f;
        Transform.Width -= InflationWhenDragged;
        Transform.Y += InflationWhenDragged / 2f;
        Transform.Height -= InflationWhenDragged;

        Style.BackColor = new(245, 245, 245, 255);

        Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Default);
        OnDropped?.Invoke(this);
    }
}