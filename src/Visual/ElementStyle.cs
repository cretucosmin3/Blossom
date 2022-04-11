using System;
using Kara.Core.Visual;

public class ElementStyle
{
    internal VisualElement ElementRef;

    public ElementStyle() { }

    public TextStyle Text { get; set; }

    private float _BorderWidth = 0f;
    private SkiaSharp.SKColor _BorderColor = new(0, 0, 0, 0);
    private float _Roundness = 0f;
    private SkiaSharp.SKColor _BackColor = new(0, 0, 0, 0);

    public float BorderWidth
    {
        get => _BorderWidth;
        set
        {
            _BorderWidth = value;
            //! #render
        }
    }

    public SkiaSharp.SKColor BorderColor
    {
        get => _BorderColor;
        set
        {
            _BorderColor = value;
            //! #render
        }
    }

    public float Roundness
    {
        get => _Roundness;
        set
        {
            _Roundness = value;
            //! #render
        }
    }


    public SkiaSharp.SKColor BackColor
    {
        get => _BackColor;
        set
        {
            _BackColor = value;
            //! #render
        }
    }
}