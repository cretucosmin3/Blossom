using System;
using System.Security.Cryptography;
using SkiaSharp;

namespace Blossom.Core.Visual;

public class TextStyle : StyleProperty, IDisposable
{
    public readonly SKPaint Paint;

    private int _Spacing = 2;
    private float _Size = 18f;
    private int _Weight = 100;
    private int _Width = 0;
    private float _Padding = 0f;
    private SKPathEffect _PathEffect = null;
    private TextAlign _Alignment = TextAlign.Center;
    private SKColor _Color;
    private string _FontName = "Arimo";

    public ShadowStyle _Shadow;

    public TextStyle()
    {
        Paint = new SKPaint()
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Left,
            TextSize = _Size,
            SubpixelText = true,
            Typeface = SKTypeface.FromFamilyName(_FontName, _Weight, _Width, SKFontStyleSlant.Upright),
        };
    }

    private void RedoFont()
    {
        var typeFace = SKTypeface.FromFamilyName(_FontName,
            new SKFontStyle(_Weight, _Width, SKFontStyleSlant.Upright)
        );

        if (_Shadow?.Filter != null)
        {
            Paint.ImageFilter = _Shadow.Filter;
        }

        Paint.IsAntialias = true;
        Paint.Typeface = typeFace;
        Paint.TextSize = _Size;

        // if (_PathEffect != null)
        Paint.PathEffect = _PathEffect;

        TriggerRender();
    }

    public int Spacing
    {
        get => _Spacing;
        set
        {
            _Spacing = value;
            RedoFont();
        }
    }

    public float Size
    {
        get => _Size;
        set
        {
            _Size = value;
            RedoFont();
        }
    }

    public int Weight
    {
        get => _Weight;
        set
        {
            _Weight = value;
            RedoFont();
        }
    }

    public float Padding
    {
        get => _Padding;
        set
        {
            _Padding = value;
            TriggerRender();
        }
    }

    public TextAlign Alignment
    {
        get => _Alignment;
        set
        {
            _Alignment = value;
            TriggerRender();
        }
    }

    public SKColor Color
    {
        get => _Color;
        set
        {
            _Color = value;
            Paint.Color = value;
            TriggerRender();
        }
    }

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