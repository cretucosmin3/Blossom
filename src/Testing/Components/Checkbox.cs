using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class Checkbox : VisualElement
{
    private bool _isChecked;
    private readonly VisualElement _box;
    private readonly VisualElement _labelText;
    
    public Action<bool>? OnCheckedChanged;

    [BuilderProperty("Is Checked", "Checkbox")]
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                UpdateVisualState();
                OnCheckedChanged?.Invoke(_isChecked);
            }
        }
    }

    public SKColor TextColor
    {
        get => _labelText.Style.Text.Color;
        set
        {
            _labelText.Style.Text.Color = value;
            _labelText.ScheduleRender();
        }
    }

    public Checkbox(string label, bool initialChecked = false)
    {
        Name = $"Checkbox_{label}_{Guid.NewGuid().ToString().Substring(0, 4)}";
        _isChecked = initialChecked;

        // Container properties
        Style = new ElementStyle
        {
            BackColor = SKColors.Transparent,
            Border = new BorderStyle { Width = 0, Color = SKColors.Transparent }
        };

        // Text label
        _labelText = new VisualElement
        {
            Name = $"{Name}_Label",
            Text = label,
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = new SKColor(226, 232, 240), // light slate
                    Size = 14,
                    Weight = 600,
                    Alignment = TextAlign.Left
                }
            },
            Transform = new Transform(28, 0, 150, 24)
            {
                Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
            }
        };

        // Visual checkbox square
        _box = new VisualElement
        {
            Name = $"{Name}_Box",
            Style = new ElementStyle
            {
                Border = new BorderStyle
                {
                    Width = 1.5f,
                    Color = new SKColor(148, 163, 184), // Slate 400
                    Roundness = 4
                }
            },
            Transform = new Transform(0, 2, 20, 20)
            {
                FixedWidth = true,
                FixedHeight = true
            }
        };

        AddChild(_box);
        AddChild(_labelText);

        UpdateVisualState();

        // Mouse events
        Events.OnMouseEnter += (s) =>
        {
            _box.Style.Border.Color = new SKColor(9, 9, 11); // Midnight Black
            Transform.ScaleX = 1.02f;
            Transform.ScaleY = 1.02f;
        };

        Events.OnMouseLeave += (s) =>
        {
            _box.Style.Border.Color = _isChecked ? new SKColor(9, 9, 11) : new SKColor(148, 163, 184);
            Transform.ScaleX = 1.0f;
            Transform.ScaleY = 1.0f;
        };

        Events.OnMouseUp += (s, e) =>
        {
            IsChecked = !IsChecked;
        };
    }

    private void UpdateVisualState()
    {
        if (_isChecked)
        {
            _box.Style.BackColor = new SKColor(9, 9, 11, 220); // Midnight Black
            _box.Style.Border.Color = new SKColor(9, 9, 11);
            _box.Style.Shadow = new ShadowStyle
            {
                Color = new SKColor(9, 9, 11, 50),
                SpreadX = 4,
                SpreadY = 4
            };
        }
        else
        {
            _box.Style.BackColor = SKColors.Transparent;
            _box.Style.Border.Color = new SKColor(148, 163, 184);
            _box.Style.Shadow = null!;
        }
    }
}
