using System;
using System.Security.Cryptography;
using Blossom.Core;
using Blossom.Core.Visual.Enums;
using SkiaSharp;

namespace Blossom.Core.Visual;

public class TextStyle : StyleProperty, IDisposable
{
    public readonly SKPaint Paint;

    private int _Spacing = 2;
    private float _Size = 18f;
    private int _Weight = 400;
    private int _Width = 5; // SKFontStyleWidth.Normal
    private float _Padding = 0f;
    private SKPathEffect _PathEffect = null;
    private TextAlign _Alignment = TextAlign.Center;
    private TextOverflow _Overflow = TextOverflow.Visible;
    private int _MaxLines = 1;
    private SKColor _Color;
    private string _FontName = "Liberation Sans, Noto Sans, sans-serif";

    public ShadowStyle _Shadow;

    public TextStyle()
    {
        Paint = new SKPaint()
        {
            IsAntialias = true,
            SubpixelText = true,
            LcdRenderText = true,
            HintingLevel = SKPaintHinting.Normal,
            TextAlign = SKTextAlign.Left,
            TextSize = _Size,
            Typeface = Blossom.Utils.Fonts.GetTypeface(_FontName, _Weight, _Width, SKFontStyleSlant.Upright),
        };
    }

    private void RedoFont()
    {
        var typeFace = Blossom.Utils.Fonts.GetTypeface(_FontName, _Weight, _Width, SKFontStyleSlant.Upright);

        if (_Shadow?.Filter != null)
        {
            Paint.ImageFilter = _Shadow.Filter;
        }

        Paint.IsAntialias = true;
        Paint.SubpixelText = true;
        Paint.LcdRenderText = true;
        Paint.HintingLevel = SKPaintHinting.Normal;
        Paint.Typeface = typeFace;
        Paint.TextSize = _Size;

        // if (_PathEffect != null)
        Paint.PathEffect = _PathEffect;

        TriggerRender();
    }

    [BuilderProperty("Text Spacing", "Text", min: 0f, max: 20f, step: 1f)]
    public int Spacing
    {
        get => _Spacing;
        set
        {
            _Spacing = value;
            RedoFont();
        }
    }

    [BuilderProperty("Text Size", "Text", min: 6f, max: 120f, step: 1f)]
    public float Size
    {
        get => _Size;
        set
        {
            if (Math.Abs(_Size - value) < 0.01f) return;
            _Size = value;
            RedoFont();
        }
    }

    [BuilderProperty("Font Weight", "Text", min: 100f, max: 900f, step: 100f)]
    public int Weight
    {
        get => _Weight;
        set
        {
            if (_Weight == value) return;
            _Weight = value;
            RedoFont();
        }
    }

    [BuilderProperty("Text Padding", "Text", min: 0f, max: 100f, step: 1f)]
    public float Padding
    {
        get => _Padding;
        set
        {
            _Padding = value;
            TriggerRender();
        }
    }

    [BuilderProperty("Text Alignment", "Text")]
    public TextAlign Alignment
    {
        get => _Alignment;
        set
        {
            _Alignment = value;
            TriggerRender();
        }
    }

    /// <summary>How text that does not fit the element is handled (visible, clip, or ellipsis).</summary>
    [BuilderProperty("Text Overflow", "Text")]
    public TextOverflow Overflow
    {
        get => _Overflow;
        set
        {
            if (_Overflow == value) return;
            _Overflow = value;
            TriggerRender();
        }
    }

    /// <summary>Maximum wrapped lines. 1 = single line (default). Used with <see cref="Overflow"/>.</summary>
    [BuilderProperty("Max Lines", "Text", min: 1f, max: 32f, step: 1f)]
    public int MaxLines
    {
        get => _MaxLines;
        set
        {
            int v = Math.Clamp(value, 1, 64);
            if (_MaxLines == v) return;
            _MaxLines = v;
            TriggerRender();
        }
    }

    [BuilderProperty("Text Color", "Text")]
    public SKColor Color
    {
        get => _Color;
        set
        {
            if (_Color == value) return;
            _Color = value;
            Paint.Color = value;
            TriggerRender();
        }
    }

    [BuilderProperty("Font Family", "Text")]
    public string Font
    {
        get => _FontName;
        set
        {
            _FontName = value;
            RedoFont();
        }
    }

    public ShadowStyle Shadow
    {
        get => _Shadow;
        set
        {
            _Shadow?.Dispose();
            _Shadow = value;

            RedoFont();
            _Shadow.OnChanged += RedoFont;
        }
    }

    public SKPathEffect PathEffect
    {
        get => _PathEffect;
        set
        {
            _PathEffect = value;
            RedoFont();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _PathEffect?.Dispose();
        _Shadow?.Dispose();

    }
}