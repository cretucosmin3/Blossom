namespace Rux.Core.Visual;
using System.Collections.Generic;

public class ElementStyle
{
    internal List<VisualElement> AssignedElements = new List<VisualElement>();

    public TextStyle Text { get; set; }
    public BorderStyle Border { get; set; } = new();
    public ShadowStyle Shadow { get; set; } = new();

    public ElementStyle() { }

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

    private SkiaSharp.SKColor _BackColor = new(0, 0, 0, 0);
    public SkiaSharp.SKColor BackColor
    {
        get => _BackColor;
        set
        {
            _BackColor = value;
            ScheduleRender();
        }
    }
}