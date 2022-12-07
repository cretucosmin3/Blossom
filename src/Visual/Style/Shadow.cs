namespace Rux.Core.Visual;
using System.Numerics;

public class ShadowStyle : StyleProperty
{
    private float _OffsetX = 0;
    private float _OffsetY = 0;
    private float _SpreadX = 0;
    private float _SpreadY = 0;
    private SkiaSharp.SKColor _Color = new(0, 0, 0, 0);

    internal bool HasValidValues()
    {
        return _SpreadX + _SpreadY > 0 && _Color.Alpha > 0;
    }

    public float OffsetX
    {
        get => _OffsetX;
        set
        {
            _OffsetX = value;
            TriggerRender();
        }
    }

    public float OffsetY
    {
        get => _OffsetY;
        set
        {
            _OffsetY = value;
            TriggerRender();
        }
    }

    public float SpreadX
    {
        get => _SpreadX;
        set
        {
            _SpreadX = value;
            TriggerRender();
        }
    }

    public float SpreadY
    {
        get => _SpreadY;
        set
        {
            _SpreadY = value;
            TriggerRender();
        }
    }

    public SkiaSharp.SKColor Color
    {
        get { return _Color; }
        set
        {
            _Color = value;
            TriggerRender();
        }
    }
}