using System;
using Blossom.Core.Design;
using Blossom.Core.Visual;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class Modal : VisualElement
{
    private string _title = "Modal";
    private readonly Container _card;
    private readonly VisualElement _titleElement;
    private readonly Button _closeBtn;
    private readonly Button _cancelBtn;
    private readonly Button _confirmBtn;

    public Container ContentContainer { get; }

    public float CardWidth { get; set; } = 420f;
    public float CardHeight { get; set; } = 280f;
    public bool CloseOnBackdropClick { get; set; } = true;

    public bool IsOpen
    {
        get => Visible;
        set
        {
            if (Visible != value)
            {
                Visible = value;
                if (Visible)
                {
                    InvalidateLayout();
                    InvalidatePaint();
                }
                else
                {
                    Closed?.Invoke();
                    InvalidatePaint();
                }
            }
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value ?? "";
            _titleElement.Text = _title;
            InvalidatePaint();
        }
    }

    public Button PrimaryButton => _confirmBtn;
    public Button SecondaryButton => _cancelBtn;

    public Action? Closed;
    public Action? Confirmed;

    public Modal(string title = "Modal")
    {
        Name = $"Modal_{title}";
        _title = title;
        ZIndex = 1000;
        Visible = false;

        Transform = new Transform(0, 0, DesignCanvas.DefaultDesignWidth, DesignCanvas.DefaultDesignHeight)
        {
            Anchor = Anchor.Left | Anchor.Right | Anchor.Top | Anchor.Bottom
        };

        // Dark dimming backdrop
        Style = new ElementStyle
        {
            BackColor = new SKColor(0, 0, 0, 180),
            Border = new BorderStyle { Width = 0, Color = SKColors.Transparent }
        };

        _card = new Container(new SKColor(38, 38, 38), 10f); // Gray 800 card

        _titleElement = new VisualElement
        {
            Name = $"{Name}_Title",
            Text = title,
            IsClickthrough = true,
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = SKColors.White,
                    Size = 16,
                    Weight = 700,
                    Alignment = TextAlign.Left
                }
            }
        };

        // ASCII only — bundled Roboto has no ✕ glyph
        _closeBtn = new Button("x", new SKColor(58, 58, 58))
        {
            Name = $"{Name}_CloseBtn"
        };
        _closeBtn.Clicked += Close;

        ContentContainer = new Container(SKColors.Transparent, 0f)
        {
            Name = $"{Name}_Content",
            Style = new ElementStyle
            {
                BackColor = SKColors.Transparent,
                Border = new BorderStyle { Width = 0, Color = SKColors.Transparent },
                Shadow = null!
            }
        };

        _cancelBtn = new Button("Cancel", new SKColor(58, 58, 58))
        {
            Name = $"{Name}_CancelBtn"
        };
        _cancelBtn.Clicked += Close;

        _confirmBtn = new Button("Confirm", new SKColor(100, 100, 100)) // Gray accent
        {
            Name = $"{Name}_ConfirmBtn"
        };
        _confirmBtn.Clicked += () =>
        {
            Confirmed?.Invoke();
            // Confirmed handlers may close themselves; default still closes
            if (IsOpen)
                Close();
        };

        AddChild(_card);
        _card.AddChild(_titleElement);
        _card.AddChild(_closeBtn);
        _card.AddChild(ContentContainer);
        _card.AddChild(_cancelBtn);
        _card.AddChild(_confirmBtn);

        Events.OnClick += (target, args) =>
        {
            if (!IsOpen || !CloseOnBackdropClick) return;
            if (!_card.Transform.Computed.RectF.Contains(args.Global.X, args.Global.Y))
            {
                Close();
            }
        };

        Events.OnKeyDown += (k) =>
        {
            if (!IsOpen) return;
            if ((Key)k == Key.Escape)
            {
                Close();
            }
        };
    }

    public void AddContent(VisualElement child)
    {
        ContentContainer.AddChild(child);
    }

    public void Show()
    {
        IsOpen = true;
    }

    public void Hide()
    {
        IsOpen = false;
    }

    public void Close()
    {
        if (ParentView?.ActiveKeyboardElement != null)
        {
            ParentView.SetActiveKeyboardElement(null);
        }
        IsOpen = false;
    }

    protected override void LayoutChildren()
    {
        float originX = Transform.Computed.X;
        float originY = Transform.Computed.Y;
        float w = Math.Max(1f, Transform.Width);
        float h = Math.Max(1f, Transform.Height);

        // Calculate required content height from visible children
        float requiredContentH = 8f;
        foreach (var child in ContentContainer.Children)
        {
            if (child == null || !child.Visible) continue;
            float childH = child.Transform.Height > 1f ? child.Transform.Height : 38f;
            requiredContentH += childH + 10f;
        }

        const float pad = 18f;
        const float headerH = 40f;
        const float btnH = 36f;
        const float btnW = 96f;
        float footerH = btnH + 16f;

        float minCardH = pad * 2 + headerH + requiredContentH + footerH;
        float desiredH = Math.Max(CardHeight, minCardH);

        float cardW = Math.Min(w - 40f, CardWidth);
        float cardH = Math.Min(h - 40f, desiredH);
        float cardX = originX + Math.Max(0, (w - cardW) / 2f);
        float cardY = originY + Math.Max(0, (h - cardH) / 2f);

        _card.Transform.SetAbsoluteFrame(cardX, cardY, cardW, cardH);

        _titleElement.Transform.SetAbsoluteFrame(cardX + pad, cardY + pad, Math.Max(40f, cardW - pad * 2 - 36f), 28f);
        _closeBtn.Transform.SetAbsoluteFrame(cardX + cardW - pad - 30f, cardY + pad, 30f, 30f);

        float footerY = cardY + cardH - pad - btnH;
        _confirmBtn.Transform.SetAbsoluteFrame(cardX + cardW - pad - btnW, footerY, btnW, btnH);
        if (_cancelBtn.Visible)
        {
            _cancelBtn.Transform.SetAbsoluteFrame(cardX + cardW - pad - btnW * 2 - 10f, footerY, btnW, btnH);
        }

        float contentX = cardX + pad;
        float contentY = cardY + pad + headerH;
        float contentW = Math.Max(40f, cardW - pad * 2);
        float contentH = Math.Max(40f, footerY - contentY - 12f);
        ContentContainer.Transform.SetAbsoluteFrame(contentX, contentY, contentW, contentH);

        // Stack content children vertically inside the content box
        float y = contentY + 4f;
        foreach (var child in ContentContainer.Children)
        {
            if (child == null || !child.Visible) continue;
            float childH = child.Transform.Height > 1f ? child.Transform.Height : 38f;
            float childW = contentW;
            child.Transform.SetAbsoluteFrame(contentX, y, childW, childH);
            y += childH + 10f;
        }
    }
}
