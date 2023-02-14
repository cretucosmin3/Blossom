using System;
namespace Blossom.Core.Visual;
using System.Collections.Generic;

public class ElementStyle : IDisposable
{
    internal List<VisualElement> AssignedElements = new();
    private SkiaSharp.SKColor _BackColor = new(0, 0, 0, 0);
    private SkiaSharp.SKPathEffect _BackgroundPathEffect;

    public TextStyle Text { get; set; }
    public BorderStyle Border { get; set; }
    public ShadowStyle Shadow { get; set; }

    internal void AssignElement(VisualElement element)
    {
        AssignedElements.Add(element);

        if (Text is not null) Text.StyleContext = this;
        if (Border is not null) Border.StyleContext = this;
        if (Shadow is not null) Shadow.StyleContext = this;
    }

    internal void UnassignElement(ref VisualElement element)
    {
        AssignedElements.Remove(element);
    }

    internal void ScheduleRender()
    {
        foreach (var element in AssignedElements)
            element.ScheduleRender();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Border?.Dispose();
        Shadow?.Dispose();
    }

    public SkiaSharp.SKColor BackColor
    {
        get => _BackColor;
        set
        {
            _BackColor = value;
            ScheduleRender();
        }
    }

    public SkiaSharp.SKPathEffect BackgroundPathEffect
    {
        get => _BackgroundPathEffect;
        set
        {
            _BackgroundPathEffect = value;
            ScheduleRender();
        }
    }
}