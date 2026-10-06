using System;
using System.Collections.Generic;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Primitives;

/// <summary>
/// Base class for custom-drawn interactive controls: tracks hover/pressed state,
/// provides an <see cref="Enabled"/> toggle, and delegates painting through <see cref="Paint"/>
/// in element-local coordinates.
/// </summary>
public class Control : VisualElement
{
    private bool _enabled = true;

    protected Control()
    {
        Style = new ElementStyle();
        Events.OnMouseEnter += _ => SetHover(true);
        Events.OnMouseLeave += _ => { SetHover(false); SetPressed(false); };
        Events.OnMouseDown += (_, e) => { if (e.Button == 0) SetPressed(true); };
        Events.OnMouseUp += (_, _) => SetPressed(false);
    }

    /// <summary>True when the mouse pointer is within the bounds of this control.</summary>
    public bool IsHovered { get; private set; }

    /// <summary>True when the primary mouse button is pressed on this control.</summary>
    public bool IsPressed { get; private set; }

    /// <summary>Disabled controls ignore user input and clear active interaction states.</summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
                return;
            _enabled = value;
            Interactive = value;
            if (!value)
            {
                IsHovered = false;
                IsPressed = false;
            }
            OnStateChanged();
            InvalidatePaint();
        }
    }

    /// <summary>Computed width of this control in local layout coordinates.</summary>
    public float W => Transform.Computed.Width;

    /// <summary>Computed height of this control in local layout coordinates.</summary>
    public float H => Transform.Computed.Height;

    /// <summary>Paints the control on the Skia canvas in element-local coordinates (0, 0, W, H). Optional when using child elements.</summary>
    protected virtual void Paint(SKCanvas c) { }

    protected override void OnAfterStyleDraw(List<DrawCommand> cmds) =>
        cmds.Add(new DrawCallbackCommand(c => { if (W > 0 && H > 0) Paint(c); }));

    protected void SetHover(bool value)
    {
        if (IsHovered == value)
            return;
        IsHovered = value;
        OnHoverChanged();
        OnStateChanged();
        InvalidatePaint();
    }

    protected void SetPressed(bool value)
    {
        if (IsPressed == value)
            return;
        IsPressed = value;
        OnStateChanged();
        InvalidatePaint();
    }

    /// <summary>Invoked when <see cref="IsHovered"/> changes.</summary>
    protected virtual void OnHoverChanged() { }

    /// <summary>Invoked when any interaction state (<see cref="IsHovered"/>, <see cref="IsPressed"/>, <see cref="Enabled"/>) changes.</summary>
    protected virtual void OnStateChanged() { }

    /// <summary>Sets <paramref name="field"/> and requests a repaint when the value changed.</summary>
    protected bool SetAndPaint<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        InvalidatePaint();
        return true;
    }
}
