using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class Switch : VisualElement
{
    private bool _isOn;
    private readonly VisualElement _capsule;
    private readonly VisualElement _knob;
    private readonly VisualElement _labelText;

    public Action<bool>? OnToggled;

    [BuilderProperty("Is On", "Switch")]
    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (_isOn != value)
            {
                _isOn = value;
                UpdateVisualState();
                OnToggled?.Invoke(_isOn);
            }
        }
    }

    public Switch(string label, bool initialOn = false)
    {
        Name = $"Switch_{label}_{Guid.NewGuid().ToString().Substring(0, 4)}";
        _isOn = initialOn;

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
            Transform = new Transform(54, 0, 150, 24)
            {
                Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
            }
        };

        // Capsule background (pill shape)
        _capsule = new VisualElement
        {
            Name = $"{Name}_Capsule",
            Style = new ElementStyle
            {
                BackColor = new SKColor(30, 41, 59),
                Border = new BorderStyle
                {
                    Width = 1f,
                    Color = new SKColor(71, 85, 105),
                    Roundness = 12f
                }
            },
            Transform = new Transform(0, 0, 44, 24)
            {
                FixedWidth = true,
                FixedHeight = true
            }
        };

        // Knob (circle shape)
        _knob = new VisualElement
        {
            Name = $"{Name}_Knob",
            Style = new ElementStyle
            {
                BackColor = new SKColor(226, 232, 240),
                Border = new BorderStyle { Width = 0, Roundness = 9f }
            },
            Transform = new Transform(3, 3, 18, 18)
            {
                FixedWidth = true,
                FixedHeight = true
            }
        };

        AddChild(_capsule);
        _capsule.AddChild(_knob);
        AddChild(_labelText);

        UpdateVisualState();

        Events.OnMouseEnter += (s) =>
        {
            _capsule.Style.Border.Color = new SKColor(56, 189, 248);
            Transform.ScaleX = 1.02f;
            Transform.ScaleY = 1.02f;
        };

        Events.OnMouseLeave += (s) =>
        {
            _capsule.Style.Border.Color = new SKColor(71, 85, 105);
            Transform.ScaleX = 1.0f;
            Transform.ScaleY = 1.0f;
        };

        Events.OnMouseUp += (s, e) =>
        {
            IsOn = !IsOn;
        };
    }

    public override void AddedToView()
    {
        base.AddedToView();
        UpdateVisualState();
        Transform.OnChanged += (t) => UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (_isOn)
        {
            _capsule.Style.BackColor = new SKColor(34, 197, 94); // Green when active
            _knob.Transform.X = Transform.X + 23; // Move right
            _knob.Style.BackColor = SKColors.White;
        }
        else
        {
            _capsule.Style.BackColor = new SKColor(30, 41, 59); // Slate-800 when off
            _knob.Transform.X = Transform.X + 3; // Move left
            _knob.Style.BackColor = new SKColor(226, 232, 240);
        }
    }
}
