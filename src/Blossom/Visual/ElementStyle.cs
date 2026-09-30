using System;
using Blossom.Core;
namespace Blossom.Core.Visual;
using System.Collections.Generic;

public class ElementStyle : IDisposable
{
    internal List<VisualElement> AssignedElements = new();

    public ElementStyle()
    {
        Text = new TextStyle();
        Border = new BorderStyle { Width = 0 };
        Shadow = new ShadowStyle();
    }
    private SkiaSharp.SKColor _BackColor = new(0, 0, 0, 0);
    private SkiaSharp.SKPathEffect _BackgroundPathEffect;

    private BackgroundShaderType _BackgroundShader = BackgroundShaderType.None;
    private SkiaSharp.SKColor _BackgroundShaderColor = SkiaSharp.SKColors.Transparent;
    private BorderEffectType _BorderEffect = BorderEffectType.None;
    private float _BorderEffectSpeed = 1f;
    private float _BorderEffectAmount = 5f;
    private float _BackdropBlur = 0f;
    private EffectRenderMode _ShaderRenderMode = EffectRenderMode.OnDemand;
    private float _ShaderSpeed = 1f;
    private TransitionEffectType _TransitionType = TransitionEffectType.None;
    private float _TransitionProgress = 1.0f;

    private TextStyle _text;
    public TextStyle Text
    {
        get => _text;
        set
        {
            _text = value;
            if (_text != null) _text.StyleContext = this;
            ScheduleRender();
        }
    }

    private BorderStyle _border;
    public BorderStyle Border
    {
        get => _border;
        set
        {
            _border = value;
            if (_border != null) _border.StyleContext = this;
            ScheduleRender();
        }
    }

    private ShadowStyle _shadow;
    public ShadowStyle Shadow
    {
        get => _shadow;
        set
        {
            _shadow = value;
            if (_shadow != null) _shadow.StyleContext = this;
            ScheduleRender();
        }
    }

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
            if (_ShaderRenderMode == value) return;
            _ShaderRenderMode = value;
            ScheduleRender();
        }
    }

    /// <summary>
    /// Multiplier for time-based background shaders. 1 is the authored speed; larger values run the effect faster.
    /// </summary>
    [BuilderProperty("Shader Speed", "Effects", min: 0f, max: 120f, step: 0.25f)]
    public float ShaderSpeed
    {
        get => _ShaderSpeed;
        set
        {
            float next = Math.Max(0f, value);
            if (Math.Abs(_ShaderSpeed - next) < 0.0001f) return;
            _ShaderSpeed = next;
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
            if (_BackColor == value) return;
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