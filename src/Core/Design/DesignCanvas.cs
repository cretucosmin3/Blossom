using System;
using System.Numerics;

namespace Blossom.Core.Design;

/// <summary>
/// Declared authoring canvas dimensions for a layout root (View or Plugin).
/// <para>
/// All widths, heights, offsets, and positions declared on or measured against a DesignCanvas
/// are expressed in logical <b>design units</b> (<see cref="Ru"/>), not raw window or device pixels.
/// </para>
/// </summary>
public sealed class DesignCanvas
{
    /// <summary>
    /// Default width for new design canvases (1000 design units).
    /// </summary>
    public const float DefaultDesignWidth = 1000f;

    /// <summary>
    /// Default height for new design canvases (1000 design units).
    /// </summary>
    public const float DefaultDesignHeight = 1000f;

    /// <summary>
    /// Event raised whenever design canvas dimensions change.
    /// </summary>
    public event Action? Changed;

    private float _width = DefaultDesignWidth;
    private float _height = DefaultDesignHeight;

    /// <summary>
    /// Authoring width in design units (<see cref="Ru"/>).
    /// </summary>
    public float Width
    {
        get => _width;
        set
        {
            float val = Math.Max(1f, value);
            if (Math.Abs(_width - val) > 0.001f)
            {
                _width = val;
                Changed?.Invoke();
            }
        }
    }

    /// <summary>
    /// Authoring height in design units (<see cref="Ru"/>).
    /// </summary>
    public float Height
    {
        get => _height;
        set
        {
            float val = Math.Max(1f, value);
            if (Math.Abs(_height - val) > 0.001f)
            {
                _height = val;
                Changed?.Invoke();
            }
        }
    }

    /// <summary>
    /// Authoring width in design units (<see cref="Ru"/>). Alias for <see cref="Width"/>.
    /// </summary>
    public float DesignWidth
    {
        get => _width;
        set => Width = value;
    }

    /// <summary>
    /// Authoring height in design units (<see cref="Ru"/>). Alias for <see cref="Height"/>.
    /// </summary>
    public float DesignHeight
    {
        get => _height;
        set => Height = value;
    }

    public DesignCanvas()
    {
    }

    public DesignCanvas(float width, float height)
    {
        _width = Math.Max(1f, width);
        _height = Math.Max(1f, height);
    }

    /// <summary>
    /// Maps a window coordinate to a design canvas coordinate (identity under product model).
    /// </summary>
    public Vector2 PointToDesign(float winX, float winY, float winW, float winH) => new(winX, winY);

    /// <summary>
    /// Maps a design canvas coordinate to a window coordinate (identity under product model).
    /// </summary>
    public Vector2 PointToWindow(float desX, float desY, float winW, float winH) => new(desX, desY);

    /// <summary>
    /// Maps a slot coordinate to a plugin design canvas coordinate (identity under reflow).
    /// </summary>
    public Vector2 PointToPluginDesign(float slotX, float slotY, float slotW, float slotH) => new(slotX, slotY);

    /// <summary>
    /// Maps a plugin design canvas coordinate to a slot coordinate (identity under reflow).
    /// </summary>
    public Vector2 PointToSlot(float desX, float desY, float slotW, float slotH) => new(desX, desY);
}
