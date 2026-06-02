using System;
namespace Blossom.Core.Visual;
using System.Collections.Generic;

public class ElementStyle : IDisposable
{
    internal List<VisualElement> AssignedElements = new();
    private SkiaSharp.SKColor _BackColor = new(0, 0, 0, 0);
    private SkiaSharp.SKPathEffect _BackgroundPathEffect;

    private BackgroundShaderType _BackgroundShader = BackgroundShaderType.None;
    private SkiaSharp.SKColor _BackgroundShaderColor = SkiaSharp.SKColors.Transparent;
    private BorderEffectType _BorderEffect = BorderEffectType.None;
    private float _BorderEffectSpeed = 1f;
    private float _BorderEffectAmount = 5f;
    private float _BackdropBlur = 0f;

    public TextStyle Text { get; set; }
    public BorderStyle Border { get; set; }
    public ShadowStyle Shadow { get; set; }

    public BackgroundShaderType BackgroundShader
    {
        get => _BackgroundShader;
        set
        {
            _BackgroundShader = value;
            ScheduleRender();
        }
    }

    public SkiaSharp.SKColor BackgroundShaderColor
    {
        get => _BackgroundShaderColor;
        set
        {
            _BackgroundShaderColor = value;
            ScheduleRender();
        }
    }

    public BorderEffectType BorderEffect
    {
        get => _BorderEffect;
        set
        {
            _BorderEffect = value;
            ScheduleRender();
        }
    }

    public float BorderEffectSpeed
    {
        get => _BorderEffectSpeed;
        set
        {
            _BorderEffectSpeed = value;
            ScheduleRender();
        }
    }

    public float BorderEffectAmount
    {
        get => _BorderEffectAmount;
        set
        {
            _BorderEffectAmount = value;
            ScheduleRender();
        }
    }

    public float BackdropBlur
    {
        get => _BackdropBlur;
        set
        {
            _BackdropBlur = value;
            ScheduleRender();
        }
    }

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