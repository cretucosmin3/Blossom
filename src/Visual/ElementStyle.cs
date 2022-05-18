namespace Kara.Core.Visual;
using System.Collections.Generic;

public class ElementStyle
{
    internal List<VisualElement> AssignedElements = new List<VisualElement>();

    public TextStyle Text { get; set; } = new();
    public BorderStyle Border { get; set; } = new();

    public ElementStyle() { }

    internal void AssignElement(VisualElement element)
    {
        AssignedElements.Add(element);
        Text.StyleContext = this;
        Border.StyleContext = this;
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