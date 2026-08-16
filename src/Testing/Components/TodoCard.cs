using System;
using Blossom.Core.Visual;
using Blossom.Testing.Models;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class TodoCard : Container
{
    public TodoItem Item { get; }
    private readonly Checkbox _checkbox;
    private readonly VisualElement _titleElement;
    private readonly Button _editBtn;
    private readonly Button _deleteBtn;

    public Action<TodoItem>? StatusChanged;
    public Action<TodoItem>? EditRequested;
    public Action<TodoItem>? DeleteRequested;

    public Action<TodoCard, System.Numerics.Vector2, float, float>? DragStarted;
    public Action<TodoCard, System.Numerics.Vector2>? DragMoved;
    public Action<TodoCard, System.Numerics.Vector2>? DragDropped;

    private bool _isPointerDown = false;
    private bool _isDragging = false;
    private System.Numerics.Vector2 _mouseDownPos;
    private float _grabOffsetX;
    private float _grabOffsetY;

    public bool IsDragging => _isDragging;

    public TodoCard(TodoItem item) : base(new SKColor(58, 58, 58), 6f) // Gray 700
    {
        Item = item;
        Name = $"Card_{item.Id.ToString()[..6]}";
        Cursor = StandardCursor.Hand;

        Style.Border = new BorderStyle
        {
            Width = 1,
            Color = new SKColor(82, 82, 82), // Gray 600
            Roundness = 6
        };
        Style.Shadow = new ShadowStyle
        {
            Color = SKColors.Black.WithAlpha(40),
            SpreadX = 0,
            SpreadY = 1,
            OffsetX = 0,
            OffsetY = 1
        };

        _checkbox = new Checkbox("", item.IsDone)
        {
            Name = $"{Name}_Check"
        };
        _checkbox.Changed += (isChecked) =>
        {
            Item.IsDone = isChecked;
            StatusChanged?.Invoke(Item);
        };

        _titleElement = new VisualElement
        {
            Name = $"{Name}_Title",
            Text = item.Title,
            IsClickthrough = true,
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = item.IsDone ? new SKColor(163, 163, 163) : SKColors.White,
                    Size = 13,
                    Weight = 500,
                    Alignment = TextAlign.Left
                }
            }
        };

        // ASCII only — bundled Roboto has no ✎/✕ glyphs (would render blank)
        _editBtn = new Button("...", new SKColor(82, 82, 82))
        {
            Name = $"{Name}_EditBtn"
        };
        _editBtn.Style.Text.Size = 11;
        _editBtn.Clicked += () =>
        {
            EditRequested?.Invoke(Item);
        };

        _deleteBtn = new Button("x", new SKColor(82, 82, 82))
        {
            Name = $"{Name}_DelBtn"
        };
        _deleteBtn.Style.Text.Size = 13;
        _deleteBtn.Clicked += () =>
        {
            DeleteRequested?.Invoke(Item);
        };

        AddChild(_checkbox);
        AddChild(_titleElement);
        AddChild(_editBtn);
        AddChild(_deleteBtn);

        UpdateVisualState();

        Events.OnMouseDown += (s, args) =>
        {
            if (args.Button != 0 || !EffectiveInteractive) return;
            _isPointerDown = true;
            _isDragging = false;
            _mouseDownPos = args.Global;
            _grabOffsetX = args.Global.X - Transform.Computed.X;
            _grabOffsetY = args.Global.Y - Transform.Computed.Y;
        };

        Events.OnMouseMove += (s, args) =>
        {
            if (_isPointerDown && !_isDragging)
            {
                float dx = args.Global.X - _mouseDownPos.X;
                float dy = args.Global.Y - _mouseDownPos.Y;
                if (MathF.Sqrt(dx * dx + dy * dy) >= 6f)
                {
                    _isDragging = true;
                    DragStarted?.Invoke(this, args.Global, _grabOffsetX, _grabOffsetY);
                }
            }
            else if (_isDragging)
            {
                DragMoved?.Invoke(this, args.Global);
            }
        };

        Events.OnMouseUp += (s, args) =>
        {
            if (args.Button != 0) return;

            if (_isDragging)
            {
                _isDragging = false;
                _isPointerDown = false;
                args.Handled = true;
                DragDropped?.Invoke(this, args.Global);
            }
            else if (_isPointerDown)
            {
                _isPointerDown = false;
                EditRequested?.Invoke(Item);
            }
        };

        Events.OnMouseEnter += (s) =>
        {
            if (!EffectiveInteractive || _isDragging) return;
            Style.Border.Color = new SKColor(170, 170, 170); // Gray accent
            InvalidatePaint();
        };

        Events.OnMouseLeave += (s) =>
        {
            if (_isDragging) return;
            UpdateVisualState();
        };
    }

    public void UpdateVisualState()
    {
        if (Item.IsDone)
        {
            Style.BackColor = new SKColor(38, 38, 38); // Gray 800
            Style.Border.Color = new SKColor(58, 58, 58); // Gray 700
            _titleElement.Style.Text.Color = new SKColor(163, 163, 163); // Gray 400
        }
        else
        {
            Style.BackColor = new SKColor(58, 58, 58); // Gray 700
            Style.Border.Color = new SKColor(82, 82, 82); // Gray 600
            _titleElement.Style.Text.Color = SKColors.White;
        }
        Style.Shadow = new ShadowStyle
        {
            Color = SKColors.Black.WithAlpha(40),
            SpreadX = 0,
            SpreadY = 1,
            OffsetX = 0,
            OffsetY = 1
        };
        _titleElement.Text = Item.Title;
        _checkbox.IsChecked = Item.IsDone;
        InvalidatePaint();
    }

    protected override void LayoutChildren()
    {
        float x = Transform.Computed.X;
        float y = Transform.Computed.Y;
        float cardW = Math.Max(1f, Transform.Width);
        float cardH = Math.Max(1f, Transform.Height);

        float cbSize = 20f;
        float cbX = x + 10f;
        float cbY = y + Math.Max(0, (cardH - cbSize) / 2f);
        _checkbox.Transform.SetAbsoluteFrame(cbX, cbY, cbSize, cbSize);

        float btnSize = Math.Min(28f, Math.Max(22f, cardH - 14f));
        float delX = x + cardW - 8f - btnSize;
        float delY = y + Math.Max(0, (cardH - btnSize) / 2f);
        _deleteBtn.Transform.SetAbsoluteFrame(delX, delY, btnSize, btnSize);

        float editX = delX - 6f - btnSize;
        _editBtn.Transform.SetAbsoluteFrame(editX, delY, btnSize, btnSize);

        float textX = cbX + cbSize + 10f;
        float textW = Math.Max(20f, editX - 8f - textX);
        _titleElement.Transform.SetAbsoluteFrame(textX, y, textW, cardH);
    }
}
