using System;
using Blossom.Utils.Arrays;
using SkiaSharp;

namespace Blossom.Utils
{
    public static class Fonts
    {
        public static V2 Measure(SKPaint paint, string text)
        {
            var textWidth = paint.MeasureText(text);
            var textHeight = paint.TextSize - paint.FontMetrics.Descent;
            return new Arrays.V2(textWidth, textHeight);
        }
    }
}