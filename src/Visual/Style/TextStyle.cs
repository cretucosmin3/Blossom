namespace Rux.Core.Visual;

using SkiaSharp;

public class TextStyle
{
    internal ElementStyle StyleContext;
    public readonly SKPaint Paint;

    private int _Spacing = 2;
    private float _Size = 18f;
    private int _Weight = 100;
    private int _Width = 0;
    private float _Padding = 0f;
    private TextAlign _Alignment = TextAlign.Center;
    private SKColor _Color;
    private string _FontName = "DejaVu Sans Mono";

    public Shadow Shadow;

    public TextStyle()
    {
        Paint = new SKPaint()
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Left,
            TextSize = _Size,
            SubpixelText = true,
            Typeface = SKTypeface.FromFamilyName(_FontName, _Weight, _Width, SKFontStyleSlant.Italic),
        };
    }

    private void RedoFont()
    {
        var typeFace = SKTypeface.FromFamilyName(_FontName,
            new SKFontStyle(_Weight, _Width, SKFontStyleSlant.Upright)
        );

        Paint.Typeface = typeFace;
        Paint.TextSize = _Size;

        StyleContext?.ScheduleRender();
    }

    public int Spacing
    {
        get => _Spacing;
        set
        {
            _Spacing = value;
            StyleContext?.ScheduleRender();
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

    public float Padding
    {
        get => _Padding;
        set
        {
            _Padding = value;
            StyleContext?.ScheduleRender();
        }
    }

    public TextAlign Alignment
    {
        get => _Alignment;
        set
        {
            _Alignment = value;
            StyleContext?.ScheduleRender();
        }
    }

    public SKColor Color
    {
        get => _Color;
        set
        {
            _Color = value;
            Paint.Color = value;
            StyleContext?.ScheduleRender();
        }
    }

    public string Font
    {
        get => _FontName;
        set
        {
            _FontName = value;
            RedoFont();

            var font = SKTypeface.FromFamilyName(value, _Weight, _Width, SKFontStyleSlant.Upright);
        }
    }
}