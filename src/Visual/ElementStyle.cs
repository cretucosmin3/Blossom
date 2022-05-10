using System.Net.Mime;
using System;
using Kara.Core.Visual;

public class ElementStyle
{
    internal VisualElement _ElementRef;
    public TextStyle Text { get; set; }
    public BorderStyle Border { get; set; }

    internal VisualElement ElementRef
    {
        get => _ElementRef;
        set
        {
            _ElementRef = value;
            Text.ElementRef = value;
            Border.ElementRef = value;
        }
    }

    public ElementStyle() { }

    private SkiaSharp.SKColor _BackColor = new(0, 0, 0, 0);

    public SkiaSharp.SKColor BackColor
    {
        get => _BackColor;
        set
        {
            _BackColor = value;
            ElementRef?.ScheduleRender();
        }
    }
}