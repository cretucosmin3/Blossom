using System;
using Blossom.Core;
using Blossom.Core.Design;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class Modal : VisualElement
{
    private string _title = "Modal";
    private readonly Stack _card;
    private readonly VisualElement _titleElement;
    private readonly Button _closeBtn;
    private readonly Button _cancelBtn;
    private readonly Button _confirmBtn;

    public Stack ContentContainer { get; }

    public float CardWidth { get; set; } = 420f;
    public float CardHeight { get; set; } = 280f;
    public bool CloseOnBackdropClick { get; set; } = true;

    private View? _overlayHost;

    public bool IsOpen
    {
        get => Visible;
        set
        {
            if (value) Show();
            else Close();
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
        ReceivesKeyboard = true;

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

        _card = new Stack
        {
            Orientation = Orientation.Vertical,
            Gap = 12,
            Padding = new Thickness(18)
        };
        DemoLayout.Panel(_card, new SKColor(38, 38, 38), 10f);

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
        _closeBtn.MinWidth = 30;
        _closeBtn.MinHeight = 30;
        _closeBtn.Transform.Width = 30;
        _closeBtn.Transform.Height = 30;
        _closeBtn.Clicked += Close;

        var header = new Stack
        {
            Orientation = Orientation.Horizontal,
            Gap = 8,
            Align = LayoutAlign.Center
        };
        header.Transform.Height = 30;
        header.AddChild(_titleElement);
        header.AddChild(_closeBtn);
        Stack.SetGrow(_titleElement, 1f);

        ContentContainer = new Stack
        {
            Name = $"{Name}_Content",
            Orientation = Orientation.Vertical,
            Gap = 10
        };
        ContentContainer.Style.BackColor = SKColors.Transparent;
        ContentContainer.Style.Border.Width = 0;
        ContentContainer.Style.Shadow = null!;
        Stack.SetGrow(ContentContainer, 1f);

        _cancelBtn = new Button("Cancel", new SKColor(58, 58, 58))
        {
            Name = $"{Name}_CancelBtn"
        };
        _cancelBtn.MinWidth = 96;
        _cancelBtn.Transform.Width = 96;
        _cancelBtn.Transform.Height = 36;
        _cancelBtn.Clicked += Close;

        _confirmBtn = new Button("Confirm", new SKColor(100, 100, 100))
        {
            Name = $"{Name}_ConfirmBtn"
        };
        _confirmBtn.MinWidth = 96;
        _confirmBtn.Transform.Width = 96;
        _confirmBtn.Transform.Height = 36;
        _confirmBtn.Clicked += () =>
        {
            Confirmed?.Invoke();
            if (IsOpen)
                Close();
        };

        var footer = new Stack
        {
            Orientation = Orientation.Horizontal,
            Gap = 10,
            Align = LayoutAlign.Center
        };
        footer.Transform.Height = 36;
        footer.AddChild(DemoLayout.GrowSpacer());
        footer.AddChild(_cancelBtn);
        footer.AddChild(_confirmBtn);

        AddChild(_card);
        _card.AddChild(header);
        _card.AddChild(ContentContainer);
        _card.AddChild(footer);

        Events.OnClick += (target, args) =>
        {
            if (!IsOpen || !CloseOnBackdropClick) return;
            if (!_card.Transform.Computed.RectF.Contains(args.Global.X, args.Global.Y))
            {
                Close();
            }
        };

        Events.OnKeyDown += e =>
        {
            if (!IsOpen) return;
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        };
    }

    public void AddContent(VisualElement child)
    {
        ContentContainer.AddChild(child);
    }

    public void Show()
    {
        if (!Visible)
        {
            Visible = true;
            InvalidateLayout();
            InvalidatePaint();
        }

        var view = ParentView ?? _overlayHost;
        if (view != null)
        {
            _overlayHost = view;
            view.PushOverlay(this, new OverlayOptions
            {
                BlockHitsUnderneath = true
            });
        }
    }

    public void Hide()
    {
        Close();
    }

    public void Close()
    {
        var view = ParentView ?? _overlayHost;
        view?.PopOverlay(this);

        if (Visible)
        {
            Visible = false;
            Closed?.Invoke();
            InvalidatePaint();
        }
    }

    protected override void LayoutChildren()
    {
        float originX = Transform.Computed.X;
        float originY = Transform.Computed.Y;
        float w = Math.Max(1f, Transform.Width);
        float h = Math.Max(1f, Transform.Height);

        float cardW = Math.Min(w - 40f, CardWidth);
        float prefH = _card.GetPreferredSize(cardW, 0).Height;
        float cardH = Math.Min(h - 40f, Math.Max(CardHeight, prefH));
        float cardX = originX + Math.Max(0, (w - cardW) / 2f);
        float cardY = originY + Math.Max(0, (h - cardH) / 2f);
        _card.Transform.SetAbsoluteFrame(cardX, cardY, cardW, cardH);
    }
}
