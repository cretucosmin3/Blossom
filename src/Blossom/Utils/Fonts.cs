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

        public static SKTypeface GetTypeface(string fontName, int weight = 400, int width = 5, SKFontStyleSlant slant = SKFontStyleSlant.Upright)
        {
            if (width <= 0)
                width = 5; // SKFontStyleWidth.Normal
            if (weight <= 0)
                weight = 400;

            if (string.IsNullOrWhiteSpace(fontName))
            {
                fontName = "Liberation Sans, Noto Sans, sans-serif";
            }

            string cacheKey = $"{fontName}_{weight}_{width}_{(int)slant}";
            if (_fontCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var style = new SKFontStyle(weight, width, slant);

            // 1. Try requested font family or comma-separated family names in order
            var families = fontName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var family in families)
            {
                if (family.Equals("sans-serif", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var tf = SKTypeface.FromFamilyName(family, style);
                    if (tf != null && !tf.FamilyName.Equals("Dialog", StringComparison.OrdinalIgnoreCase))
                    {
                        _fontCache[cacheKey] = tf;
                        return tf;
                    }
                }
                catch { }
            }

            // 2. Try common high-quality system UI fonts
            string[] systemFallbacks = ["Liberation Sans", "Noto Sans", "DejaVu Sans", "Inter", "Segoe UI", "Ubuntu", "Cantarell", "sans-serif"];
            foreach (var fallbackName in systemFallbacks)
            {
                try
                {
                    var tf = SKTypeface.FromFamilyName(fallbackName, style);
                    if (tf != null && !tf.FamilyName.Equals("Dialog", StringComparison.OrdinalIgnoreCase))
                    {
                        _fontCache[cacheKey] = tf;
                        return tf;
                    }
                }
                catch { }
            }

            // 3. Fallback to default system typeface
            try
            {
                var tf = SKTypeface.FromFamilyName(null, style);
                if (tf != null && !tf.FamilyName.Equals("Dialog", StringComparison.OrdinalIgnoreCase))
                {
                    _fontCache[cacheKey] = tf;
                    return tf;
                }
            }
            catch { }

            // 4. Bundled fallback
            var fallback = (weight <= 300 && _defaultRobotoLight != null) ? _defaultRobotoLight : (_defaultRobotoMedium ?? SKTypeface.Default);
            _fontCache[cacheKey] = fallback;
            return fallback;
        }

        public static V2 Measure(SKPaint paint, string text)
        {
            var textWidth = paint.MeasureText(text);
            var textHeight = paint.TextSize - paint.FontMetrics.Descent;
            return new Arrays.V2(textWidth, textHeight);
        }

        private static readonly List<SKTypeface> _fallbackFaces = new();
        private static bool _fallbacksLoaded;
        private static readonly object _fallbacksLock = new();

        /// <summary>
        /// Typeface that can draw <paramref name="codepoint"/>, falling back to emoji / symbol fonts
        /// when <paramref name="primary"/> does not contain the glyph.
        /// </summary>
        public static SKTypeface ResolveForCodepoint(SKTypeface? primary, int codepoint)
        {
            primary ??= _defaultRobotoMedium ?? SKTypeface.Default;
            if (HasGlyph(primary, codepoint))
                return primary;

            if (!_fallbacksLoaded)
                EnsureFallbacks();

            for (int i = 0; i < _fallbackFaces.Count; i++)
            {
                var tf = _fallbackFaces[i];
                if (tf != null && HasGlyph(tf, codepoint))
                    return tf;
            }
            return primary;
        }

        public static bool HasGlyph(SKTypeface typeface, int codepoint)
        {
            if (typeface == null || codepoint <= 0)
                return false;

            // Never use color emoji fonts on Linux: SkiaSharp FreeType build lacks PNG support and crashes
            string family = typeface.FamilyName ?? "";
            if (family.Contains("Color", StringComparison.OrdinalIgnoreCase) ||
                family.Contains("Emoji", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                string s = char.ConvertFromUtf32(codepoint);
                ushort[] glyphs = typeface.GetGlyphs(s);
                if (glyphs == null || glyphs.Length == 0)
                    return false;
                for (int i = 0; i < glyphs.Length; i++)
                {
                    if (glyphs[i] == 0)
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Eagerly loads system symbol fallback fonts. Must run at startup before active GPU draw calls.
        /// </summary>
        public static void EnsureFallbacks()
        {
            if (_fallbacksLoaded)
                return;

            lock (_fallbacksLock)
            {
                if (_fallbacksLoaded)
                    return;
                _fallbacksLoaded = true;

                string[] families =
                {
                    "DejaVu Sans",
                    "Noto Sans Symbols 2",
                    "Noto Sans Symbols",
                    "Symbola"
                };
                foreach (var family in families)
                {
                    try
                    {
                        var tf = SKTypeface.FromFamilyName(family);
                        if (tf != null && !tf.FamilyName.Equals("Dialog", StringComparison.OrdinalIgnoreCase)
                            && !_fallbackFaces.Exists(x => x.FamilyName == tf.FamilyName))
                            _fallbackFaces.Add(tf);
                    }
                    catch { }
                }

                string[] files =
                {
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                    "/usr/share/fonts/truetype/ancient-scripts/Symbola.ttf",
                    "/usr/share/fonts/truetype/symbola/Symbola.ttf"
                };
                foreach (var path in files)
                {
                    try
                    {
                        if (!File.Exists(path))
                            continue;
                        var tf = SKTypeface.FromFile(path);
                        if (tf != null && !_fallbackFaces.Exists(x => x.FamilyName == tf.FamilyName))
                            _fallbackFaces.Add(tf);
                    }
                    catch { }
                }
            }
        }
    }
}
