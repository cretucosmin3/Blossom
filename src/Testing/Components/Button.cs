using System;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using Silk.NET.Input;
using SkiaSharp;

namespace Blossom.Testing.Components;

public class Button : VisualElement
{
    private SKColor _normalColor;
    private SKColor _hoverColor;
    private SKColor _pressColor;
    private string _label = "";
    private bool _isHovered = false;
    private bool _isPressed = false;

    public Action? Clicked;
    public Action? OnClick;

    public string Label
    {
        get => _label;
        set
        {
            _label = value;
            Text = value;
            InvalidatePaint();
        }
    }

    public SKColor NormalColor
    {
        get => _normalColor;
        set
        {
            _normalColor = value;
            UpdateColors();
            ApplyCurrentVisualState();
        }
    }

    public Button(string text = "Button", SKColor? color = null)
    {
        Name = $"Button_{text}";
        _label = text;
        Text = text;
        _normalColor = color ?? new SKColor(58, 58, 58); // Gray 700
        UpdateColors();

        Cursor = StandardCursor.Hand;

        Style = new ElementStyle
        {
            BackColor = _normalColor,
            Border = new BorderStyle
            {
                Width = 1,
                Color = new SKColor(82, 82, 82), // Gray 600
                Roundness = 6
            },
            Shadow = new ShadowStyle
            {
                Color = SKColors.Black.WithAlpha(40),
                SpreadX = 0,
                SpreadY = 1,
                OffsetX = 0,
                OffsetY = 1
            },
            Text = new TextStyle
            {
                Color = SKColors.White,
                Size = 13,
                Weight = 600,
                Alignment = TextAlign.Center,
                Padding = 0
            }
        };

        Events.OnMouseEnter += (s) =>
        {
            _isHovered = true;
            ApplyCurrentVisualState();
        };

        Events.OnMouseLeave += (s) =>
        {
            _isHovered = false;
            _isPressed = false;
            ApplyCurrentVisualState();
        };

        Events.OnMouseDown += (s, e) =>
        {
            if (e.Button == 0)
            {
                _isPressed = true;
                e.Handled = true;
                ApplyCurrentVisualState();
            }
        };

        Events.OnMouseUp += (s, e) =>
        {
            if (e.Button == 0)
            {
                _isPressed = false;
                ApplyCurrentVisualState();
            }
        };

        Events.OnClick += (target, args) =>
        {
            if (!EffectiveInteractive) return;
            Clicked?.Invoke();
            OnClick?.Invoke();
        };
    }

    private void ApplyCurrentVisualState()
    {
        if (!EffectiveInteractive)
        {
            Style.BackColor = _normalColor.WithAlpha(120);
            Cursor = StandardCursor.Default;
        }
        else if (_isPressed)
        {
            Style.BackColor = _pressColor;
            Cursor = StandardCursor.Hand;
        }
        else if (_isHovered)
        {
            Style.BackColor = _hoverColor;
            Cursor = StandardCursor.Hand;
        }
        else
        {
            Style.BackColor = _normalColor;
            Cursor = StandardCursor.Hand;
        }
        InvalidatePaint();
    }

    private void UpdateColors()
    {
        _hoverColor = new SKColor(
            (byte)Math.Min(255, _normalColor.Red + 25),
            (byte)Math.Min(255, _normalColor.Green + 25),
            (byte)Math.Min(255, _normalColor.Blue + 25),
            _normalColor.Alpha);

        _pressColor = new SKColor(
            (byte)Math.Max(0, _normalColor.Red - 25),
            (byte)Math.Max(0, _normalColor.Green - 25),
            (byte)Math.Max(0, _normalColor.Blue - 25),
            _normalColor.Alpha);
    }
}
