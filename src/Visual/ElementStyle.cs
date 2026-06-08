using System;
using Blossom.Core;
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
    private EffectRenderMode _ShaderRenderMode = EffectRenderMode.OnDemand;
    private TransitionEffectType _TransitionType = TransitionEffectType.None;
    private float _TransitionProgress = 1.0f;

    public TextStyle Text { get; set; }
    public BorderStyle Border { get; set; }
    public ShadowStyle Shadow { get; set; }

    [BuilderProperty("Background Shader", "Effects")]
    public BackgroundShaderType BackgroundShader
    {
        get => _BackgroundShader;
        set
        {
            _BackgroundShader = value;
            ScheduleRender();
        }
    }

    [BuilderProperty("Shader Color", "Effects")]
    public SkiaSharp.SKColor BackgroundShaderColor
    {
        get => _BackgroundShaderColor;
        set
        {
            _BackgroundShaderColor = value;
            ScheduleRender();
        }
    }

    [BuilderProperty("Border Effect", "Effects")]
    public BorderEffectType BorderEffect
    {
        get => _BorderEffect;
        set
        {
            _BorderEffect = value;
            ScheduleRender();
        }
    }

    [BuilderProperty("Border Effect Speed", "Effects", min: 0f, max: 20f, step: 0.1f)]
    public float BorderEffectSpeed
    {
        get => _BorderEffectSpeed;
        set
        {
            _BorderEffectSpeed = value;
            ScheduleRender();
        }
    }

    [BuilderProperty("Border Effect Amount", "Effects", min: 0f, max: 50f, step: 0.5f)]
    public float BorderEffectAmount
    {
        get => _BorderEffectAmount;
        set
        {
            _BorderEffectAmount = value;
            ScheduleRender();
        }
    }

    [BuilderProperty("Backdrop Blur", "Effects", min: 0f, max: 50f, step: 0.5f)]
    public float BackdropBlur
    {
        get => _BackdropBlur;
        set
        {
            _BackdropBlur = value;
            ScheduleRender();
        }
    }

    public EffectRenderMode ShaderRenderMode
    {
        get => _ShaderRenderMode;
        set
        {
            _ShaderRenderMode = value;
            ScheduleRender();
        }
    }

    [BuilderProperty("Transition Type", "Effects")]
    public TransitionEffectType TransitionType
    {
        get => _TransitionType;
        set
        {
            _TransitionType = value;
            ScheduleRender();
        }
    }

    [BuilderProperty("Transition Progress", "Effects", min: 0f, max: 1f, step: 0.01f)]
    public float TransitionProgress
    {
        get => _TransitionProgress;
        set
        {
            _TransitionProgress = Math.Clamp(value, 0f, 1f);
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
        {
            element.ClearRenderCache();
            element.ScheduleRender();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Border?.Dispose();
        Shadow?.Dispose();
    }

    [BuilderProperty("Background Color", "Style")]
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