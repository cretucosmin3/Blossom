using Blossom.Core.Visual;
namespace Blossom.Core.Visual;

public class BorderStyle : StyleProperty
{
    private SkiaSharp.SKColor _Color;

    private float _Width = 2;
    private float _Roundness = 0f;
    private float _RTopLeft = 0f;
    private float _RTopRight = 0f;
    private float _RBottomLeft = 0f;
    private float _RBottomRight = 0f;

    public float Width
    {
        get => _Width;
        set
        {
            _Width = value;
            TriggerRender();
        }
    }

    public SkiaSharp.SKColor Color
    {
        get => _Color;
        set
        {
            _Color = value;
            TriggerRender();
        }
    }

    public float Roundness
    {
        get => _Roundness;
        set
        {
            _Roundness = value;
            _RTopLeft = value;
            _RTopRight = value;
            _RBottomLeft = value;
            _RBottomRight = value;

            TriggerRender();
        }
    }

    public float RoundnessTopLeft
    {
        get => _RTopLeft;
        set
        {
            _RTopLeft = value;
            TriggerRender();
        }
    }

    public float RoundnessTopRight
    {
        get => _RTopRight;
        set
        {
            _RTopRight = value;
            TriggerRender();
        }
    }

    public float RoundnessBottomLeft
    {
        get => _RBottomLeft;
        set
        {
            _RBottomLeft = value;
            TriggerRender();
        }
    }

    public float RoundnessBottomRight
    {
        get => _RBottomRight;
        set
        {
            _RBottomRight = value;
            TriggerRender();
        }
    }
}