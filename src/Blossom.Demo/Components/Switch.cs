using System;
using Blossom.Core.Visual;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class Switch : VisualElement
{
    private bool _isOn;
    private string? _label;
    private readonly VisualElement _track;
    private readonly VisualElement _thumb;
    private readonly VisualElement _labelElement;

    public Action<bool>? Changed;
    public Action<bool>? OnToggled;

    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (_isOn != value)
            {
                _isOn = value;
                UpdateVisualState();
                Changed?.Invoke(_isOn);
                OnToggled?.Invoke(_isOn);
            }
        }
    }

    public string? Label
    {
        get => _label;
        set
        {
            _label = value;
            _labelElement.Text = value ?? "";
            _labelElement.Visible = !string.IsNullOrEmpty(value);
            InvalidateLayout();
        }
    }

    public Switch(string? label = null, bool initialOn = false)
    {
        Name = $"Switch_{(string.IsNullOrEmpty(label) ? "Item" : label)}";
        _label = label;
        _isOn = initialOn;

        Cursor = StandardCursor.Hand;

        Style = new ElementStyle
        {
            BackColor = SKColors.Transparent,
            Border = new BorderStyle { Width = 0, Color = SKColors.Transparent }
        };

        // Track capsule
        _track = new VisualElement
        {
            Name = $"{Name}_Track",
            IsClickthrough = true,
            Style = new ElementStyle
            {
                BackColor = new SKColor(58, 58, 58), // Gray 700
                Border = new BorderStyle
                {
                    Width = 1,
                    Color = new SKColor(82, 82, 82), // Gray 600
                    Roundness = 11
                }
            }
        };

        // Thumb knob
        _thumb = new VisualElement
        {
            Name = $"{Name}_Thumb",
            IsClickthrough = true,
            Style = new ElementStyle
            {
                BackColor = new SKColor(220, 220, 220), // Gray 200
                Border = new BorderStyle
                {
                    Width = 0,
                    Roundness = 8
                },
                Shadow = new ShadowStyle
                {
                    Color = SKColors.Black.WithAlpha(60),
                    SpreadX = 0,
                    SpreadY = 1,
                    OffsetX = 0,
                    OffsetY = 1
                }
            }
        };

        _labelElement = new VisualElement
        {
            Name = $"{Name}_Label",
            IsClickthrough = true,
            Text = label ?? "",
            Visible = !string.IsNullOrEmpty(label),
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = new SKColor(220, 220, 220), // Gray 200
                    Size = 13,
                    Weight = 500,
                    Alignment = TextAlign.Left
                }
            }
        };

        AddChild(_track);
        AddChild(_thumb);
        AddChild(_labelElement);

        UpdateVisualState();

        Events.OnMouseEnter += (s) =>
        {
            if (!EffectiveInteractive) return;
            if (!_isOn)
            {
                _track.Style.Border.Color = new SKColor(170, 170, 170); // Gray accent
                InvalidatePaint();
            }
        };

        Events.OnMouseLeave += (s) =>
        {
            UpdateVisualState();
        };

        Events.OnClick += (target, args) =>
        {
            if (!EffectiveInteractive) return;
            IsOn = !IsOn;
        };
    }

    protected override void LayoutChildren()
    {
        float originX = Transform.Computed.X;
        float originY = Transform.Computed.Y;

        float trackW = 40f;
        float trackH = 22f;
        float trackY = originY + Math.Max(0, (Transform.Height - trackH) / 2f);

        _track.Transform.SetAbsoluteFrame(originX + Padding.Left, trackY, trackW, trackH);

        float thumbSize = 16f;
        float thumbPad = 3f;
        float thumbX = _isOn
            ? (originX + Padding.Left + trackW - thumbSize - thumbPad)
            : (originX + Padding.Left + thumbPad);
        float thumbY = trackY + (trackH - thumbSize) / 2f;
        _thumb.Transform.SetAbsoluteFrame(thumbX, thumbY, thumbSize, thumbSize);

        if (_labelElement.Visible)
        {
            float textX = originX + Padding.Left + trackW + 8f;
            float textW = Math.Max(0, Transform.Width - (Padding.Left + trackW + 8f + Padding.Right));
            _labelElement.Transform.SetAbsoluteFrame(textX, originY, textW, Math.Max(1f, Transform.Height));
        }
    }

    private void UpdateVisualState()
    {
        if (_isOn)
        {
            _track.Style.BackColor = new SKColor(100, 100, 100); // Gray accent
            _track.Style.Border.Color = new SKColor(170, 170, 170); // Gray accent
            _thumb.Style.BackColor = SKColors.White;
        }
        else
        {
            _track.Style.BackColor = new SKColor(58, 58, 58); // Gray 700
            _track.Style.Border.Color = new SKColor(82, 82, 82); // Gray 600
            _thumb.Style.BackColor = new SKColor(200, 200, 200); // Gray 300
        }
        // Reposition thumb without a full ancestor layout storm
        InvalidateLayout();
        InvalidatePaint();
    }
}
