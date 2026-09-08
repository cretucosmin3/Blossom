using System;
using System.Collections.Generic;
using System.IO;
using Blossom.Utils.Arrays;
using SkiaSharp;

namespace Blossom.Utils
{
    public static class Fonts
    {
        private const string MediumResource = "Blossom.assets.fonts.roboto.medium.ttf";
        private const string LightResource = "Blossom.assets.fonts.roboto.light.ttf";

        private static readonly Dictionary<string, SKTypeface> _fontCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<SKData> _embeddedFontData = new();
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
                string? fontsDir = FindFontsDirectory();
                if (fontsDir != null)
                {
                    TryLoadFromFile(Path.Combine(fontsDir, "roboto.medium.ttf"), isLight: false);
                    TryLoadFromFile(Path.Combine(fontsDir, "roboto.light.ttf"), isLight: true);
                }

                if (_defaultRobotoMedium == null)
                    TryLoadFromEmbedded(MediumResource, isLight: false);
                if (_defaultRobotoLight == null)
                    TryLoadFromEmbedded(LightResource, isLight: true);

                if (_defaultRobotoMedium != null)
                {
                    _fontCache["Roboto-Medium"] = _defaultRobotoMedium;
                    _fontCache["Roboto"] = _defaultRobotoMedium;
                    _fontCache["Arimo"] = _defaultRobotoMedium;
                }

                if (_defaultRobotoLight != null)
                    _fontCache["Roboto-Light"] = _defaultRobotoLight;

                if (_defaultRobotoMedium == null && _defaultRobotoLight == null)
                    Log.Warning("Bundled Roboto fonts were not found; text will use the Skia default typeface.");
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not load bundled fonts: {ex.Message}");
            }
        }

        private static string? FindFontsDirectory()
        {
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "fonts"),
                Path.Combine(Directory.GetCurrentDirectory(), "assets", "fonts"),
            };

            foreach (var dir in candidates)
            {
                if (Directory.Exists(dir))
                    return dir;
            }

            return null;
        }

        private static void TryLoadFromFile(string path, bool isLight)
        {
            if (!File.Exists(path))
                return;

            var typeface = SKTypeface.FromFile(path);
            if (typeface == null)
                return;

            AssignDefault(typeface, isLight);
        }

        private static void TryLoadFromEmbedded(string resourceName, bool isLight)
        {
            var assembly = typeof(Fonts).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return;

            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            var data = SKData.CreateCopy(copy.ToArray());
            _embeddedFontData.Add(data);

            var typeface = SKTypeface.FromData(data);
            if (typeface == null)
            {
                data.Dispose();
                _embeddedFontData.Remove(data);
                return;
            }

            AssignDefault(typeface, isLight);
        }

        private static void AssignDefault(SKTypeface typeface, bool isLight)
        {
            if (isLight)
                _defaultRobotoLight = typeface;
            else
                _defaultRobotoMedium = typeface;
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
