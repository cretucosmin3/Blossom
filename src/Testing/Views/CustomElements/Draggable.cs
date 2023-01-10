using System;
using System.Numerics;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Testing.CustomElements;

public class Draggable : VisualElement
{
    private bool isDragged = false;
    private Vector2 dragPoint;
    private readonly float InflationWhenDragged = 4f;

    public Action<VisualElement> OnDragged;
    public Action<VisualElement> OnDropped;

    public Draggable()
    {
        Style = new()
        {
            BackColor = new(235, 235, 235, 255),
            IsClipping = false,
            Border = new()
            {
                Roundness = 5,
                Width = 1f,
                Color = new(0, 0, 0, 255)
            },
            Shadow = new()
            {
                Color = new(0, 0, 0, 0),
                SpreadX = 4,
                SpreadY = 4,
                OffsetY = 6
            },
            Text = new()
            {
                Size = 20,
                Color = SKColors.Black,
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
        Transform.X -= InflationWhenDragged / 2f;
        Transform.Width += InflationWhenDragged;
        Transform.Y -= InflationWhenDragged / 2f;
        Transform.Height += InflationWhenDragged;

        args.Relative.X += InflationWhenDragged / 2f;
        args.Relative.Y += InflationWhenDragged / 2f;

        dragPoint = args.Relative;
        isDragged = true;

        // Style.Border.Color = SKColors.Black;
        // Style.Border.Width = 2;

        // Style.Shadow.Color = new(0, 0, 0, 35);

        Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Hand);
    }

    private void DraggableMouseUp(object obj, MouseEventArgs args)
    {
        Transform.X += InflationWhenDragged / 2f;
        Transform.Width -= InflationWhenDragged;
        Transform.Y += InflationWhenDragged / 2f;
        Transform.Height -= InflationWhenDragged;

        isDragged = false;

        // Style.Shadow.Color = new(0, 0, 0, 0);

        // Style.Border.Color = new(0, 0, 0, 255);
        // Style.Border.Width = 1f;

        Browser.ChangeCursor(Silk.NET.Input.StandardCursor.Default);
        OnDropped?.Invoke(this);
    }
}