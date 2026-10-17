using System;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Reactive;
using ReactiveLab.State;
using ReactiveLab.Ui;
using SkiaSharp;

namespace ReactiveLab.Pages;

/// <summary>
/// One board card, built once per item by <see cref="For.Each"/>. Each visible field has its own
/// binding, and each re-run bumps <see cref="Diag.CardUpdates"/> and the card's own counter
/// (shown as "N upd"), so you can see exactly which cards an operation touched.
/// </summary>
public sealed class CardView : VisualElement
{
    public const float CardHeight = 82f;

    private readonly VisualElement _strip;
    private readonly VisualElement _title;
    private readonly StarToggle _star;
    private readonly VisualElement _points;
    private readonly VisualElement _meta;
    private readonly ActionStrip _actions;
    // Written by every field binding, read only by the meta line ("N upd"); not counted itself.
    private readonly Signal<int> _updates = ReactiveEngine.CreateSignal(0);
    private bool _built;
    private bool _hovered;

    public CardView(Item item)
    {
        Name = $"Card_{item.Id}";
        Style.Border.Roundness = 8;
        Style.Border.Width = 1;

        _strip = new VisualElement { IsClickthrough = true };
        _strip.Style.Border.Roundness = 2;
        _title = UiKit.Text("", 13f, 600);
        _title.Style.Text.Overflow = Blossom.Core.Visual.Enums.TextOverflow.Ellipsis;
        _points = UiKit.Text("", 11.5f, 600);
        _points.Style.Text.Alignment = TextAlign.Center;
        _points.Style.Border.Roundness = 9;
        _meta = UiKit.Text("", 11f, muted: true);
        _meta.Style.Text.Overflow = Blossom.Core.Visual.Enums.TextOverflow.Ellipsis;
        _star = new StarToggle();
        _star.CheckedChanged += on =>
        {
            if (on != item.Starred.Peek())
                Ops.Run($"Star #{item.Id}", () => item.Starred.Value = on);
        };

        _actions = new ActionStrip(
            ("‹", () => Ops.Run($"Move #{item.Id} left", () => Board.Move(item, -1))),
            ("›", () => Ops.Run($"Move #{item.Id} right", () => Board.Move(item, +1))),
            ("+", () => Ops.Run($"#{item.Id} points +1", () => Board.AddPoints(item, +1))),
            ("−", () => Ops.Run($"#{item.Id} points −1", () => Board.AddPoints(item, -1))),
            ("×", () => Ops.Run($"Delete #{item.Id}", () => Board.Remove(item))));

        AddChild(_strip);
        AddChild(_title);
        AddChild(_star);
        AddChild(_points);
        AddChild(_meta);
        AddChild(_actions);

        // One binding per field: a title edit re-runs only the title binding of this one card.
        _title.Bind(t => { Touch(); t.Text = item.Title.Value; });
        _points.Bind(p =>
        {
            Touch();
            var pal = Look.Current.Value;
            p.Text = $"{item.Points.Value} pts";
            p.Style.BackColor = pal.Accent.WithAlpha(40);
            p.Style.Text.Color = pal.Dark ? pal.AccentHover : pal.Accent;
        });
        _strip.Bind(s => { Touch(); s.Style.BackColor = Look.Current.Value.PriorityColour(item.Priority.Value); });
        _star.Bind(s => { Touch(); s.IsChecked = item.Starred.Value; });
        _meta.Bind(m => m.Text = $"#{item.Id} · {item.Priority.Value} · {_updates.Value} upd");
        _actions.Bind(a =>
        {
            a.SetEnabled(0, item.Stage.Value != Stage.Todo);
            a.SetEnabled(1, item.Stage.Value != Stage.Done);
        });
        this.Bind(c =>
        {
            Touch();
            _ = item.Selected.Value;
            _ = Look.Current.Value;
            ApplySurface(item.Selected.Peek());
        });

        _built = true;

        Events.OnMouseEnter += _ => { _hovered = true; ApplySurface(item.Selected.Peek()); };
        Events.OnMouseLeave += _ => { _hovered = false; ApplySurface(item.Selected.Peek()); };
        Events.OnMouseDown += (_, e) =>
        {
            if (e.Button == 0) Board.Select(item);
        };
    }

    /// <summary>Counts re-runs only: the first run of each binding happens while the card is built.</summary>
    private void Touch()
    {
        if (!_built) return;
        _updates.Value = _updates.Peek() + 1;
        Diag.CardUpdates++;
    }

    private void ApplySurface(bool selected)
    {
        var p = Look.Current.Peek();
        Style.BackColor = _hovered ? p.SurfaceHover : p.Surface;
        Style.Border.Color = selected ? p.Accent : p.Border;
        Style.Border.Width = selected ? 2 : 1;
        InvalidatePaint();
    }

    protected override void LayoutChildren()
    {
        float x = Transform.AbsoluteX, y = Transform.AbsoluteY, w = Transform.Width, h = Transform.Height;
        _strip.Transform.SetAbsoluteFrame(x + 6, y + 8, 3, h - 16);
        _star.Transform.SetAbsoluteFrame(x + w - 28, y + 6, 22, 22);
        _title.Transform.SetAbsoluteFrame(x + 16, y + 6, Math.Max(0, w - 16 - 32), 22);
        _meta.Transform.SetAbsoluteFrame(x + 16, y + 28, Math.Max(0, w - 16 - 10), 16);

        float rowY = y + h - 30;
        float stripW = _actions.GetPreferredSize(0, 0).Width;
        _actions.Transform.SetAbsoluteFrame(x + w - 6 - stripW, rowY, stripW, ActionStrip.Slot);
        _points.Transform.SetAbsoluteFrame(x + 14, rowY + 3, 44, 18);
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
        => new(maxWidth > 0 ? maxWidth : Transform.Width, CardHeight);
}

/// <summary>Painted five-point star on <see cref="ToggleBase"/> (no font glyph needed).</summary>
public sealed class StarToggle : ToggleBase
{
    public StarToggle()
    {
        this.Bind(s => { _ = Look.Current.Value; s.InvalidatePaint(); });
    }

    protected override void OnStateChanged() => InvalidatePaint();

    protected override void Paint(SKCanvas c)
    {
        var p = Look.Current.Peek();
        float cx = W / 2, cy = H / 2 + 0.5f, outer = Math.Min(W, H) / 2 - 2, inner = outer * 0.45f;
        using var path = new SKPath();
        for (int i = 0; i < 10; i++)
        {
            float r = i % 2 == 0 ? outer : inner;
            float a = -MathF.PI / 2 + i * MathF.PI / 5;
            float px = cx + r * MathF.Cos(a), py = cy + r * MathF.Sin(a);
            if (i == 0) path.MoveTo(px, py); else path.LineTo(px, py);
        }
        path.Close();
        using var paint = new SKPaint { IsAntialias = true };
        if (IsChecked)
        {
            paint.Color = p.Warning;
            c.DrawPath(path, paint);
        }
        else
        {
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 1.4f;
            paint.Color = IsHovered ? p.Text : p.Muted;
            c.DrawPath(path, paint);
        }
    }
}
