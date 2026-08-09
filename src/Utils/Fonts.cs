using System;
using System.Collections.Generic;
using System.IO;
using Blossom.Utils.Arrays;
using SkiaSharp;

namespace Blossom.Utils
{
    public static class Fonts
    {
        private static readonly Dictionary<string, SKTypeface> _fontCache = new(StringComparer.OrdinalIgnoreCase);
        private static SKTypeface _defaultRobotoMedium;
        private static SKTypeface _defaultRobotoLight;

        static Fonts()
        {
            LoadBundledFonts();
        }

        private static void LoadBundledFonts()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fontsDir = Path.Combine(baseDir, "assets", "fonts");
                if (!Directory.Exists(fontsDir))
                {
                    fontsDir = Path.Combine(Directory.GetCurrentDirectory(), "assets", "fonts");
                }

                if (Directory.Exists(fontsDir))
                {
                    string mediumPath = Path.Combine(fontsDir, "roboto.medium.ttf");
                    if (File.Exists(mediumPath))
                    {
                        _defaultRobotoMedium = SKTypeface.FromFile(mediumPath);
                        if (_defaultRobotoMedium != null)
                        {
                            _fontCache["Roboto-Medium"] = _defaultRobotoMedium;
                            _fontCache["Roboto"] = _defaultRobotoMedium;
                            _fontCache["Arimo"] = _defaultRobotoMedium;
                        }
                    }

                    string lightPath = Path.Combine(fontsDir, "roboto.light.ttf");
                    if (File.Exists(lightPath))
                    {
                        _defaultRobotoLight = SKTypeface.FromFile(lightPath);
                        if (_defaultRobotoLight != null)
                        {
                            _fontCache["Roboto-Light"] = _defaultRobotoLight;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Could not load bundled fonts: {ex.Message}");
            }
        }

        public static SKTypeface GetTypeface(string fontName, int weight = 400, int width = 0, SKFontStyleSlant slant = SKFontStyleSlant.Upright)
        {
            if (string.IsNullOrWhiteSpace(fontName))
            {
                fontName = "Roboto";
            }

            if (_fontCache.TryGetValue(fontName, out var cached))
            {
                return cached;
            }

            if (fontName.Equals("Arimo", StringComparison.OrdinalIgnoreCase) ||
                fontName.Equals("Roboto", StringComparison.OrdinalIgnoreCase) ||
                fontName.Equals("Sans-Serif", StringComparison.OrdinalIgnoreCase))
            {
                if (weight <= 300 && _defaultRobotoLight != null)
                    return _defaultRobotoLight;
                if (_defaultRobotoMedium != null)
                    return _defaultRobotoMedium;
            }

            try
            {
                var tf = SKTypeface.FromFamilyName(fontName, new SKFontStyle(weight, width, slant));
                if (tf != null && !tf.FamilyName.Equals("Dialog", StringComparison.OrdinalIgnoreCase))
                {
                    return tf;
                }
            }
            catch { }

            return (weight <= 300 && _defaultRobotoLight != null) ? _defaultRobotoLight : (_defaultRobotoMedium ?? SKTypeface.Default);
        }

        public static V2 Measure(SKPaint paint, string text)
        {
            var textWidth = paint.MeasureText(text);
            var textHeight = paint.TextSize - paint.FontMetrics.Descent;
            return new Arrays.V2(textWidth, textHeight);
        }
    }
}