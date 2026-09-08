using System;
using Blossom.Core.Visual;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>
/// Horizontal value slider with optional label. Uses pointer capture for drag.
/// </summary>
public class Slider : VisualElement
{
    private float _value;
    private float _min;
    private float _max;
    private string _label = "";
    private bool _isDragging;

    private readonly VisualElement _labelElement;
    private readonly VisualElement _valueElement;
    private readonly VisualElement _track;
    private readonly VisualElement _fill;
    private readonly VisualElement _handle;

    public Action<float>? Changed;
    public Action<float>? OnValueChanged;

    public float Min
    {
        get => _min;
        set
        {
            _min = value;
            if (_max < _min) _max = _min;
            Value = _value;
            InvalidateLayout();
        }
    }

    public float Max
    {
        get => _max;
        set
        {
            _max = value;
            if (_min > _max) _min = _max;
            Value = _value;
            InvalidateLayout();
        }
    }

    public float Value
    {
        get => _value;
        set
        {
            float clamped = Math.Clamp(value, _min, _max);
            if (Math.Abs(_value - clamped) < 0.0001f) return;
            _value = clamped;
            UpdateValueLabel();
            InvalidateLayout();
            Changed?.Invoke(_value);
            OnValueChanged?.Invoke(_value);
        }
    }

    public string Label
    {
        get => _label;
        set
        {
            _label = value ?? "";
            _labelElement.Text = _label;
            _labelElement.Visible = !string.IsNullOrEmpty(_label);
            InvalidateLayout();
        }
    }

    /// <summary>Optional format for the value readout, e.g. "{0:0}" or "{0:0.0}".</summary>
    public string ValueFormat { get; set; } = "{0:0}";

    public Slider(string label = "", float min = 0f, float max = 100f, float initial = 50f)
    {
        Name = $"Slider_{(string.IsNullOrEmpty(label) ? Guid.NewGuid().ToString("N")[..6] : label)}";
        _label = label ?? "";
        _min = min;
        _max = max <= min ? min + 1f : max;
        _value = Math.Clamp(initial, _min, _max);
        Cursor = StandardCursor.Hand;

        Style = new ElementStyle
        {
            BackColor = SKColors.Transparent,
            Border = new BorderStyle { Width = 0, Color = SKColors.Transparent }
        };

        _labelElement = new VisualElement
        {
            Name = $"{Name}_Label",
            Text = _label,
            IsClickthrough = true,
            Visible = !string.IsNullOrEmpty(_label),
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = new SKColor(200, 200, 200),
                    Size = 12,
                    Weight = 500,
                    Alignment = TextAlign.Left
                }
            }
        };

        _valueElement = new VisualElement
        {
            Name = $"{Name}_Value",
            IsClickthrough = true,
            Style = new ElementStyle
            {
                Text = new TextStyle
                {
                    Color = new SKColor(163, 163, 163),
                    Size = 12,
                    Weight = 500,
                    Alignment = TextAlign.Right
                }
            }
        };

        _track = new VisualElement
        {
            Name = $"{Name}_Track",
            IsClickthrough = true,
            Style = new ElementStyle
            {
                BackColor = new SKColor(58, 58, 58),
                Border = new BorderStyle
                {
                    Width = 0,
                    Color = SKColors.Transparent,
                    Roundness = 3
                }
            }
        };

        _fill = new VisualElement
        {
            Name = $"{Name}_Fill",
            IsClickthrough = true,
            Style = new ElementStyle
            {
                BackColor = new SKColor(140, 140, 140),
                Border = new BorderStyle
                {
                    Width = 0,
                    Color = SKColors.Transparent,
                    Roundness = 3
                }
            }
        };

        _handle = new VisualElement
        {
            Name = $"{Name}_Handle",
            IsClickthrough = true,
            Style = new ElementStyle
            {
                BackColor = new SKColor(220, 220, 220),
                Border = new BorderStyle
                {
                    Width = 1,
                    Color = new SKColor(100, 100, 100),
                    Roundness = 8
                },
                Shadow = new ShadowStyle
                {
                    Color = SKColors.Black.WithAlpha(50),
                    SpreadX = 2,
                    SpreadY = 2,
                    OffsetX = 0,
                    OffsetY = 1
                }
            }
        };

        AddChild(_labelElement);
        AddChild(_valueElement);
        AddChild(_track);
        AddChild(_fill);
        AddChild(_handle);
        UpdateValueLabel();

        Events.OnMouseDown += (s, e) =>
        {
            if (e.Button != 0 || !EffectiveInteractive) return;
            _isDragging = true;
            CapturePointer();
            e.Handled = true;
            SetValueFromGlobal(e.Global.X);
        };

        Events.OnMouseMove += (s, e) =>
        {
            if (!_isDragging) return;
            e.Handled = true;
            SetValueFromGlobal(e.Global.X);
        };

        Events.OnMouseUp += (s, e) =>
        {
            if (e.Button != 0 || !_isDragging) return;
            _isDragging = false;
            ReleasePointer();
            e.Handled = true;
        };

        Events.OnMouseEnter += _ =>
        {
            if (!EffectiveInteractive) return;
            _handle.Style.BackColor = SKColors.White;
            InvalidatePaint();
        };

        Events.OnMouseLeave += _ =>
        {
            if (_isDragging) return;
            _handle.Style.BackColor = new SKColor(220, 220, 220);
            InvalidatePaint();
        };
    }

    private void UpdateValueLabel()
    {
        try
        {
            _valueElement.Text = string.Format(ValueFormat, _value);
        }
        catch
        {
            _valueElement.Text = _value.ToString("0");
        }
    }

    private void SetValueFromGlobal(float globalX)
    {
        // Track is laid out in absolute coords; map X into [min, max]
        float trackX = _track.Transform.Computed.X;
        float trackW = Math.Max(1f, _track.Transform.Computed.Width);
        float t = Math.Clamp((globalX - trackX) / trackW, 0f, 1f);
        Value = _min + t * (_max - _min);
    }

    protected override void LayoutChildren()
    {
        float x = Transform.Computed.X;
        float y = Transform.Computed.Y;
        float w = Math.Max(1f, Transform.Width);
        float h = Math.Max(1f, Transform.Height);

        float labelH = 18f;
        bool showLabel = _labelElement.Visible;
        float headerH = showLabel ? labelH : 0f;

        if (showLabel)
        {
            _labelElement.Transform.SetAbsoluteFrame(x, y, Math.Max(40f, w - 48f), labelH);
            _valueElement.Transform.SetAbsoluteFrame(x + w - 48f, y, 48f, labelH);
            _valueElement.Visible = true;
        }
        else
        {
            _valueElement.Visible = false;
        }

        float trackH = 6f;
        float trackY = y + headerH + Math.Max(0, (h - headerH - trackH) / 2f);
        float trackX = x;
        float trackW = w;

        _track.Transform.SetAbsoluteFrame(trackX, trackY, trackW, trackH);

        float range = Math.Max(0.0001f, _max - _min);
        float t = Math.Clamp((_value - _min) / range, 0f, 1f);
        float fillW = Math.Max(0f, trackW * t);
        _fill.Transform.SetAbsoluteFrame(trackX, trackY, fillW, trackH);

        float handleSize = 16f;
        float handleX = trackX + fillW - handleSize / 2f;
        handleX = Math.Clamp(handleX, trackX, trackX + trackW - handleSize);
        float handleY = trackY + (trackH - handleSize) / 2f;
        _handle.Transform.SetAbsoluteFrame(handleX, handleY, handleSize, handleSize);
    }
}
