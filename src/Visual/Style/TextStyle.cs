using System;
using System.Numerics;
using Kara.Core.Visual;

public class TextStyle
{
    internal VisualElement ElementRef;

    public TextStyle() { }

    private int _Spacing = 2;
    private float _Size = 18f;
    private float _Padding = 0f;
    private TextAlign _Alignment = TextAlign.Center;
    private SkiaSharp.SKColor _Color;
    public Shadow Shadow = null;
    public bool HasChanged = true;

    public int Spacing
    {
        get => _Spacing;
        set
        {
            _Spacing = value;
            HasChanged = true;
            //! #render
        }
    }

    public float Size
    {
        get => _Size;
        set
        {
            _Size = value;
            HasChanged = true;
            //! #render
        }
    }

    public float Padding
    {
        get => _Padding;
        set
        {
            _Padding = value;
            HasChanged = true;
            //! #render
        }
    }

    public TextAlign Alignment
    {
        get => _Alignment;
        set
        {
            _Alignment = value;
            HasChanged = true;
            //! #render
        }
    }

    public SkiaSharp.SKColor Color
    {
        get => _Color;
        set
        {
            _Color = value;
            HasChanged = true;
            //! #render
        }
    }

    private string _FontName = "sans";
    public string Font
    {
        get => _FontName;
        set
        {
            _FontName = value;
            //! #render
            //! TextFont = Fonts.Get(value);
        }
    }
}