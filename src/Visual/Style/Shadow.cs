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
    private SKColor _Color = new(0, 0, 0, 0);

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
        _Color = color;
        RedoFilter();
    }

    public SKImageFilter Filter { get; private set; }
    internal SKPaint ShadowPaint { get; } = new SKPaint();

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
        get { return _Color; }
        set
        {
            _Color = value;
            RedoFilter();
            TriggerChange();
            TriggerRender();
        }
    }

    public bool HasValidValues()
    {
        return _SpreadX + _SpreadY > 0 && _Color.Alpha > 0;
    }

    private void RedoFilter()
    {
        Filter?.Dispose();

        if (HasValidValues())
        {
            Filter = SKImageFilter.CreateDropShadow(
                OffsetX, OffsetY, SpreadX, SpreadY, Color,
                null, null);
        }
    }

    public void Dispose()
    {
        Filter.Dispose();
    }
}