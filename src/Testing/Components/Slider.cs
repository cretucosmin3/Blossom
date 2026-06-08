using System;
using Blossom.Core;
using Blossom.Core.Visual;
using Blossom.Core.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class Slider : VisualElement
{
    private float _value;
    private float _min = 0f;
    private float _max = 100f;
    private readonly VisualElement _track;
    private readonly VisualElement _fill;
    private readonly VisualElement _handle;
    private bool _isDragging;

    public Action<float>? OnValueChanged;

    [BuilderProperty("Current Value", "Slider")]
    public float Value
    {
        get => _value;
        set
        {
            float clamped = Math.Clamp(value, _min, _max);
            if (_value != clamped)
            {
                _value = clamped;
                UpdateVisualState();
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    [BuilderProperty("Minimum Value", "Slider")]
    public float Min
    {
        get => _min;
        set
        {
            _min = value;
            Value = _value; // Re-clamp
            UpdateVisualState();
        }
    }

    [BuilderProperty("Maximum Value", "Slider")]
    public float Max
    {
        get => _max;
        set
        {
            _max = value;
            Value = _value; // Re-clamp
            UpdateVisualState();
        }
    }

    public Slider(float min = 0f, float max = 100f, float initialValue = 50f)
    {
        Name = $"Slider_{Guid.NewGuid().ToString().Substring(0, 4)}";
        _min = min;
        _max = max;
        _value = initialValue;

        // Container properties
        Style = new ElementStyle
        {
            BackColor = SKColors.Transparent,
            Border = new BorderStyle { Width = 0, Color = SKColors.Transparent }
        };

        // Slider track
        _track = new VisualElement
        {
            Name = $"{Name}_Track",
            Style = new ElementStyle
            {
                BackColor = new SKColor(30, 41, 59, 180), // Dark slate
                Border = new BorderStyle { Width = 1, Color = new SKColor(71, 85, 105), Roundness = 3 }
            },
            Transform = new Transform(0, 8, 200, 6)
            {
                Anchor = Anchor.Left | Anchor.Right | Anchor.Top
            }
        };

        // Fill track
        _fill = new VisualElement
        {
            Name = $"{Name}_Fill",
            Style = new ElementStyle
            {
                BackColor = new SKColor(236, 72, 153), // Hot pink
                Border = new BorderStyle { Width = 0, Roundness = 3 }
            },
            Transform = new Transform(0, 8, 100, 6)
            {
                Anchor = Anchor.Left | Anchor.Top
            }
        };

        // Handle
        _handle = new VisualElement
        {
            Name = $"{Name}_Handle",
            Style = new ElementStyle
            {
                BackColor = SKColors.White,
                Border = new BorderStyle { Width = 1, Color = new SKColor(236, 72, 153), Roundness = 8 },
                Shadow = new ShadowStyle { Color = new SKColor(236, 72, 153, 100), SpreadX = 4, SpreadY = 4 }
            },
            Transform = new Transform(90, 3, 16, 16)
            {
                FixedWidth = true,
                FixedHeight = true
            }
        };

        AddChild(_track);
        AddChild(_fill);
        AddChild(_handle);

        UpdateVisualState();

        // Mouse events on slider container for click & drag
        Events.OnMouseDown += (s, e) =>
        {
            _isDragging = true;
            UpdateValueFromMouse(e.Global);
        };

        Events.OnMouseMove += (s, e) =>
        {
            if (_isDragging)
            {
                UpdateValueFromMouse(e.Global);
            }
        };

        Events.OnMouseUp += (s, e) =>
        {
            _isDragging = false;
        };

        Events.OnMouseEnter += (s) =>
        {
            _handle.Style.Border.Color = new SKColor(56, 189, 248); // Switch border to Cyan on hover
        };

        Events.OnMouseLeave += (s) =>
        {
            _handle.Style.Border.Color = new SKColor(236, 72, 153);
            _isDragging = false;
        };
    }

    private void UpdateValueFromMouse(System.Numerics.Vector2 mousePos)
    {
        var localPos = PointToClient(mousePos.X, mousePos.Y);
        float percent = Math.Clamp(localPos.X / Transform.Width, 0f, 1f);
        float newValue = _min + percent * (_max - _min);
        Value = newValue;
    }

    private void UpdateVisualState()
    {
        float percent = (_max - _min) == 0 ? 0 : (_value - _min) / (_max - _min);
        float sliderWidth = Transform.Width > 0 ? Transform.Width : 200f;
        
        float fillWidth = percent * sliderWidth;
        _fill.Transform.Width = fillWidth;

        float handleX = fillWidth - (_handle.Transform.Width / 2f);
        _handle.Transform.X = Transform.X + Math.Clamp(handleX, 0f, sliderWidth - _handle.Transform.Width);
    }

    public override void AddedToView()
    {
        base.AddedToView();
        UpdateVisualState();
        Transform.OnChanged += (t) => UpdateVisualState();
    }
}
