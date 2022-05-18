using Kara.Core.Visual;

public class BorderStyle
{
    internal VisualElement ElementRef;

    public BorderStyle() { }

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
            ElementRef?.ScheduleRender();
        }
    }

    public SkiaSharp.SKColor Color
    {
        get => _Color;
        set
        {
            _Color = value;
            ElementRef?.ScheduleRender();
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

            ElementRef?.ScheduleRender();
        }
    }

    public float RTopLeft
    {
        get => _RTopLeft;
        set
        {
            _RTopLeft = value;
            ElementRef?.ScheduleRender();
        }
    }
}