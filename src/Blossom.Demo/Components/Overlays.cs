using System;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Reactive;
using Blossom.Testing.State;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>
/// Modal dialog on the view's overlay stack (<see cref="View.PushOverlay"/>): blocks hits underneath,
/// takes the keyboard, closes on Escape or a click on the backdrop. The backdrop blurs the page.
/// </summary>
public sealed class Dialog : VisualElement
{
    private readonly Stack _card;
    private View? _host;
    private float _fade;

    public Dialog(string title, string? description = null, float width = 440f)
    {
        CardWidth = width;
        ReceivesKeyboard = true;
        this.UseStyle("dialog-backdrop");

        _card = new Stack { Orientation = Orientation.Vertical, Gap = 18, Padding = new Thickness(24) };
        _card.UseStyle("dialog");
        _card.Transform.TransformOriginX = 0.5f;
        _card.Transform.TransformOriginY = 0.5f;

        var header = Ui.Column(6, Ui.Text(title, "text.title"));
        if (description != null)
            header.AddChild(Ui.Paragraph(description, "text.muted"));
        _card.AddChild(header);

        Body = Ui.Column(12);
        _card.AddChild(Body);

        Footer = Ui.Row(8, Ui.Spacer());
        _card.AddChild(Footer);

        AddChild(_card);
        Layout.SetManual(_card, true);

        Events.OnKeyDown += (KeyEvent e) =>
        {
            if (e.Key == Key.Escape) { Close(); e.Handled = true; }
            else if (e.Key == Key.Enter && Submit != null) { Submit(); e.Handled = true; }
        };
        Events.OnMouseDown += (_, e) =>
        {
            var r = _card.Transform;
            bool inside = e.Global.X >= r.AbsoluteX && e.Global.X <= r.AbsoluteX + r.Width &&
                          e.Global.Y >= r.AbsoluteY && e.Global.Y <= r.AbsoluteY + r.Height;
            if (!inside) { Close(); e.Handled = true; }
        };
    }

    public float CardWidth { get; set; }
    public Stack Body { get; }
    public Stack Footer { get; }
    public bool IsOpen => _host != null;

    public event Action? Closed;

    /// <summary>Enter anywhere in the dialog (outside a field that handles it) runs this.</summary>
    public Action? Submit { get; set; }

    public Button AddAction(string text, ButtonVariant variant, Action? onClick = null, bool closes = true)
    {
        var button = new Button(text, variant: variant);
        button.Clicked += () =>
        {
            onClick?.Invoke();
            if (closes) Close();
        };
        Footer.AddChild(button);
        return button;
    }

    public void Open(View view)
    {
        if (_host != null) return;
        _host = view;
        _fade = 0f;
        Opacity = 0f;
        ResizeToView(Shell.ClientWidth, Shell.ClientHeight);
        Shell.ClientResized += ResizeToView;
        view.PushOverlay(this, new OverlayOptions { BlockHitsUnderneath = true });
    }

    public void Close()
    {
        if (_host == null) return;
        Shell.ClientResized -= ResizeToView;
        _host.PopOverlay(this);
        _host = null;
        Closed?.Invoke();
    }

    private void ResizeToView(int width, int height)
    {
        Transform.SetAbsoluteFrame(0, 0, width, height);
        InvalidateLayout();
    }

    protected override void OnUpdate(float dt)
    {
        if (_fade >= 1f) return;
        _fade = Math.Min(1f, _fade + dt / 0.16f);
        Opacity = _fade;
        float scale = 0.96f + 0.04f * _fade;
        _card.Transform.ScaleX = scale;
        _card.Transform.ScaleY = scale;
        InvalidatePaint();
    }

    protected override void LayoutChildren()
    {
        float w = Math.Min(CardWidth, Transform.Width - 32f);
        float h = _card.GetPreferredSize(w, 0).Height;
        float x = MathF.Round((Transform.Width - w) / 2f);
        float y = MathF.Round(Math.Max(16f, (Transform.Height - h) / 2f - 20f));
        _card.Transform.SetAbsoluteFrame(Transform.AbsoluteX + x, Transform.AbsoluteY + y, w, h);
    }
}

/// <summary>One toast card; dismissed by its close button or <see cref="AppState.Notify"/>'s timer.</summary>
public sealed class Toast : Stack
{
    public Toast(ToastItem item)
    {
        Orientation = Orientation.Horizontal;
        Gap = 12;
        Align = LayoutAlign.Start;
        Padding = new Thickness(14, 12, 10, 12);
        this.UseStyle("toast");

        var icon = new Icon(item.Tone switch
        {
            Tone.Success => "check-circle",
            Tone.Danger => "alert",
            Tone.Warning => "alert",
            _ => "info"
        }, 18f);
        icon.Bind(i => i.Color = Styling.Look.Current.Value.Colour(item.Tone switch
        {
            Tone.Success => "success-text",
            Tone.Danger => "danger-text",
            Tone.Warning => "warning-text",
            Tone.Info => "info-text",
            _ => "primary-text"
        }));

        var text = Ui.Column(3, Ui.Text(item.Title, "text.strong"), Ui.Paragraph(item.Message, "text.muted"));
        Stack.SetGrow(text, 1f);

        var close = Button.IconOnly("x", ButtonVariant.Ghost, ButtonSize.Sm);
        close.Clicked += () => AppState.Dismiss(item.Id);

        AddChild(icon);
        AddChild(text);
        AddChild(close);
    }
}

/// <summary>Circular accent picker swatch, painted on <see cref="ButtonBase"/>.</summary>
public sealed class Swatch : ButtonBase
{
    private readonly SKColor _colour;
    private bool _selected;

    public Swatch(SKColor colour)
    {
        _colour = colour;
        Transform.Width = 22;
        Transform.Height = 22;
    }

    public SKColor RingColor { get; set; }

    public bool Selected
    {
        get => _selected;
        set => SetAndPaint(ref _selected, value);
    }

    protected override void OnStateChanged() => InvalidatePaint();

    protected override void Paint(SKCanvas c)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = _colour };
        float cx = W / 2f, cy = H / 2f;
        c.DrawCircle(cx, cy, IsHovered || _selected ? 7.5f : 7f, paint);
        if (_selected)
        {
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 2f;
            paint.Color = RingColor;
            c.DrawCircle(cx, cy, 10f, paint);
        }
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight) => new(22, 22);
}
