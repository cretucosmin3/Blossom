using System;
using Blossom.Core.Visual;
using Blossom.Primitives;
using Blossom.Testing.Components;
using SkiaSharp;

namespace Blossom.Testing.Frame;

/// <summary>Sidebar entry: <see cref="ButtonBase"/> + icon + label + optional live count.</summary>
public sealed class NavItem : ButtonBase
{
    private readonly Icon _icon;
    private readonly VisualElement _label;
    private readonly VisualElement _count;
    private bool _active;

    public NavItem(string text, string icon)
    {
        Name = $"Nav_{text}";
        Transform.Height = 36;
        _icon = new Icon(icon, 17f);
        _label = new VisualElement { Text = text, IsClickthrough = true };
        _label.Style.Text.Size = 13.5f;
        _label.Style.Text.Weight = 500;
        _label.Style.Text.Alignment = TextAlign.Left;
        _count = new VisualElement { IsClickthrough = true, Visible = false };
        _count.Style.Text.Size = 11.5f;
        _count.Style.Text.Weight = 600;
        _count.Style.Text.Alignment = TextAlign.Center;

        AddChild(_icon);
        AddChild(_label);
        AddChild(_count);
        this.UseStyle("nav-item");
    }

    public bool Active
    {
        get => _active;
        set
        {
            if (_active == value) return;
            _active = value;
            this.UseStyle("nav-item");
        }
    }

    public string Count
    {
        set
        {
            _count.Text = value ?? "";
            _count.Visible = _count.Text.Length > 0;
            InvalidateLayout();
        }
    }

    public void Colour(SKColor content, SKColor countText, SKColor countBack)
    {
        _icon.Color = content;
        _label.Style.Text.Color = content;
        _count.Style.Text.Color = countText;
        _count.Style.BackColor = countBack;
        _count.Style.Border.Roundness = 9f;
    }

    protected override void OnStateChanged() => this.UseStyle("nav-item");

    protected override void LayoutChildren()
    {
        float x = Transform.AbsoluteX, y = Transform.AbsoluteY, h = Transform.Height;
        _icon.Transform.SetAbsoluteFrame(x + 11f, MathF.Round(y + (h - 17f) / 2f), 17f, 17f);
        float countW = 0f;
        if (_count.Visible)
        {
            countW = Math.Max(22f, _count.GetPreferredSize(0, 0).Width + 12f);
            _count.Transform.SetAbsoluteFrame(x + Transform.Width - countW - 8f, MathF.Round(y + (h - 18f) / 2f), countW, 18f);
        }
        _label.Transform.SetAbsoluteFrame(x + 38f, y, Math.Max(0f, Transform.Width - 46f - countW), h);
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight) => new(maxWidth > 0 ? maxWidth : 200f, 36f);
}
