using System;
using System.Numerics;
using SkiaSharp;
namespace Blossom.Core.Visual;

public class ShadowStyle : StyleProperty, IDisposable
{
    private float _OffsetX = 0;
    private float _OffsetY = 0;
    private float _SpreadX = 0;
    private float _SpreadY = 0;

    public ShadowStyle()
    {
        RedoFilter();
    }

    public ShadowStyle(float oX, float oY, float sX, float sY, SKColor color)
    {
        _OffsetX = oX;
        _OffsetY = oY;
        _SpreadX = sX;
        _SpreadY = sY;
        Paint.Color = color;
        RedoFilter();
    }

    public SKImageFilter Filter { get; private set; }
    internal SKPaint Paint { get; } = new SKPaint()
    {
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    public float OffsetX
    {
        get => _OffsetX;
        set
        {
            _OffsetX = value;
            RedoFilter();
            TriggerChange();
            TriggerRender();
        }
    }

    public float OffsetY
    {
        get => _OffsetY;
        set
        {
            _OffsetY = value;
            RedoFilter();
            TriggerChange();
            TriggerRender();
        }
    }

    public float SpreadX
    {
        get => _SpreadX;
        set
        {
            _SpreadX = value;
            RedoFilter();
            TriggerChange();
            TriggerRender();
        }
    }

    public float SpreadY
    {
        get => _SpreadY;
        set
        {
            _SpreadY = value;
            RedoFilter();
            TriggerChange();
            TriggerRender();
        }
    }

    public SKColor Color
    {
        get { return Paint.Color; }
        set
        {
            Paint.Color = value;
            RedoFilter();
            TriggerChange();
            TriggerRender();
        }
    }

    public bool HasValidValues()
    {
        return _OffsetX + OffsetY + _SpreadX + _SpreadY > 0 && Paint.Color.Alpha > 0;
    }

    private void RedoFilter()
    {
        Filter?.Dispose();

        if (HasValidValues())
        {
            Filter = SKImageFilter.CreateDropShadow(
                OffsetX, OffsetY, SpreadX, SpreadY, Color,
                null, null);

            Paint.ImageFilter = Filter;
        }
    }

    public void Dispose()
    {
        Filter.Dispose();
        Paint.Dispose();
    }
}