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
    private bool _filterDirty = true;

    private SKImageFilter? _filter;
    public SKImageFilter? Filter
    {
        get
        {
            UpdateFilterIfNeeded();
            return _filter;
        }
    }

    private readonly SKPaint _paint;
    public SKPaint Paint
    {
        get
        {
            UpdateFilterIfNeeded();
            return _paint;
        }
    }

    public ShadowStyle()
    {
        _paint = new SKPaint()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
    }

    public ShadowStyle(float oX, float oY, float sX, float sY, SKColor color)
    {
        _paint = new SKPaint()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        _OffsetX = oX;
        _OffsetY = oY;
        _SpreadX = sX;
        _SpreadY = sY;
        _paint.Color = color;
    }

    public float OffsetX
    {
        get => _OffsetX;
        set
        {
            if (_OffsetX != value)
            {
                _OffsetX = value;
                _filterDirty = true;
                TriggerChange();
                TriggerRender();
            }
        }
    }

    public float OffsetY
    {
        get => _OffsetY;
        set
        {
            if (_OffsetY != value)
            {
                _OffsetY = value;
                _filterDirty = true;
                TriggerChange();
                TriggerRender();
            }
        }
    }

    public float SpreadX
    {
        get => _SpreadX;
        set
        {
            if (_SpreadX != value)
            {
                _SpreadX = value;
                _filterDirty = true;
                TriggerChange();
                TriggerRender();
            }
        }
    }

    public float SpreadY
    {
        get => _SpreadY;
        set
        {
            if (_SpreadY != value)
            {
                _SpreadY = value;
                _filterDirty = true;
                TriggerChange();
                TriggerRender();
            }
        }
    }

    public SKColor Color
    {
        get { return _paint.Color; }
        set
        {
            if (_paint.Color != value)
            {
                _paint.Color = value;
                _filterDirty = true;
                TriggerChange();
                TriggerRender();
            }
        }
    }

    public bool HasValidValues()
    {
        return _OffsetX + _OffsetY + _SpreadX + _SpreadY > 0 && _paint.Color.Alpha > 0;
    }

    private void UpdateFilterIfNeeded()
    {
        if (_filterDirty)
        {
            _filter?.Dispose();
            _filter = null;

            if (HasValidValues())
            {
                _filter = SKImageFilter.CreateDropShadow(
                    _OffsetX, _OffsetY, _SpreadX, _SpreadY, Color,
                    null, null);
                _paint.ImageFilter = _filter;
            }
            else
            {
                _paint.ImageFilter = null;
            }
            _filterDirty = false;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _filter?.Dispose();
        _paint.Dispose();
    }
}