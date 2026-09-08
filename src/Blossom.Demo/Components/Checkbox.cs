using System;
using Blossom.Core.Visual;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class Checkbox : VisualElement
{
    private bool _isChecked;
    private string _label = "";
    private readonly VisualElement _box;
    private readonly VisualElement _labelElement;

    public Action<bool>? Changed;
    public Action<bool>? OnCheckedChanged;

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                UpdateVisualState();
                Changed?.Invoke(_isChecked);
                OnCheckedChanged?.Invoke(_isChecked);
            }
        }
    }

    public string Label
    {
        get => _label;
        set
        {
            _label = value;
            _labelElement.Text = value;
            InvalidatePaint();
        }
    }

    public Checkbox(string label = "Checkbox", bool initialChecked = false)
    {
        Name = $"Checkbox_{label}";
        _label = label;
        _isChecked = initialChecked;

        Cursor = StandardCursor.Hand;

        Style = new ElementStyle
        {
            BackColor = SKColors.Transparent,
            Border = new BorderStyle { Width = 0, Color = SKColors.Transparent }
        };

        _box = new VisualElement
        {
            Name = $"{Name}_Box",
            IsClickthrough = true,
            Style = new ElementStyle
            {
                BackColor = SKColors.Transparent,
                Border = new BorderStyle
                {
                    Width = 1.5f,
                    Color = new SKColor(120, 120, 120), // Gray 500
                    Roundness = 4
                },
                Text = new TextStyle
                {
                    Color = SKColors.White,
                    Size = 12,
                    Weight = 700,
                    Alignment = TextAlign.Center
                }
            }
        };

        _labelElement = new VisualElement
        {
            Name = $"{Name}_Label",
            IsClickthrough = true,
            Text = label,
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

        AddChild(_box);
        AddChild(_labelElement);

        UpdateVisualState();

        Events.OnMouseDown += (s, e) =>
        {
            if (e.Button == 0)
            {
                e.Handled = true;
            }
        };

        Events.OnMouseEnter += (s) =>
        {
            if (!EffectiveInteractive) return;
            if (!_isChecked)
            {
                _box.Style.Border.Color = new SKColor(170, 170, 170); // Gray accent
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
            IsChecked = !IsChecked;
        };
    }

    protected override void LayoutChildren()
    {
        float originX = Transform.Computed.X;
        float originY = Transform.Computed.Y;
        float boxSize = 18f;
        float boxY = originY + Math.Max(0, (Transform.Height - boxSize) / 2f);

        _box.Transform.SetAbsoluteFrame(originX + Padding.Left, boxY, boxSize, boxSize);

        float textX = originX + Padding.Left + boxSize + 8f;
        float textW = Math.Max(0, Transform.Width - (Padding.Left + boxSize + 8f + Padding.Right));
        _labelElement.Transform.SetAbsoluteFrame(textX, originY, textW, Math.Max(1f, Transform.Height));
        _labelElement.Visible = !string.IsNullOrEmpty(_label);
    }

    private void UpdateVisualState()
    {
        if (_isChecked)
        {
            _box.Style.BackColor = new SKColor(100, 100, 100); // Gray accent
            _box.Style.Border.Color = new SKColor(170, 170, 170); // Gray accent
            // ASCII "v" — bundled Roboto has no ✓ glyph
            _box.Text = "v";
        }
        else
        {
            _box.Style.BackColor = new SKColor(23, 23, 23, 180); // Gray 900
            _box.Style.Border.Color = new SKColor(120, 120, 120); // Gray 500
            _box.Text = "";
        }
        InvalidatePaint();
    }
}
