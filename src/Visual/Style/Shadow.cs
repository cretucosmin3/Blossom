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
    private SkiaSharp.SKColor _Color = new(0, 0, 0, 0);
    private SKImageFilter _Filter;

    public ShadowStyle()
    {
        RedoFilter();
    }

    public bool HasValidValues()
    {
        return _SpreadX + _SpreadY > 0 && _Color.Alpha > 0;
    }

    private void RedoFilter()
    {
        this._Filter?.Dispose();

        if (HasValidValues())
        {
            this._Filter = SKImageFilter.CreateDropShadow(
                    OffsetX, OffsetY, SpreadX, SpreadY, Color,
                    null, null);
        }
    }

    public void Dispose()
    {
        _Filter.Dispose();
    }

    public SKImageFilter Filter { get => _Filter; }

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

    public SkiaSharp.SKColor Color
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
}