using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Blossom.Utils.Arrays;
using SkiaSharp;

namespace Blossom.Utils
{
    public static class Fonts
    {
        private const string MediumResource = "Blossom.assets.fonts.roboto.medium.ttf";
        private const string LightResource = "Blossom.assets.fonts.roboto.light.ttf";

        private static readonly object _sync = new();
        private static readonly List<SKTypeface> _registered = new();
        private static readonly Dictionary<string, SKTypeface> _byCacheKey = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, SKTypeface> _exactCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _loggedFallbacks = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<SKData> _ownedData = new();
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

            try
            {
                AssignDefault(RegisterFromFile(path), isLight);
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not register font '{path}': {ex.Message}");
            }
        }

        private static void TryLoadFromEmbedded(string resourceName, bool isLight)
        {
            var assembly = typeof(Fonts).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return;

            try
            {
                AssignDefault(RegisterFromStream(stream, resourceName), isLight);
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not register embedded font '{resourceName}': {ex.Message}");
            }
        }

        private static void AssignDefault(SKTypeface typeface, bool isLight)
        {
            if (isLight)
                _defaultRobotoLight = typeface;
            else
                _defaultRobotoMedium = typeface;
        }

        /// <summary>Loads a face from disk and adds it to the app font registry.</summary>
        public static SKTypeface RegisterFromFile(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            var full = Path.GetFullPath(path);
            if (!File.Exists(full))
                throw new FileNotFoundException("Font file not found.", full);

            lock (_sync)
            {
                if (_byCacheKey.TryGetValue(full, out var existing))
                    return existing;

                var typeface = SKTypeface.FromFile(full);
                if (typeface == null)
                    throw new InvalidOperationException($"Could not create a typeface from '{full}'.");

                AddRegistered(typeface, full);
                return typeface;
            }
        }

        /// <summary>Copies <paramref name="stream"/> and registers the face. Reuses <paramref name="cacheKey"/> if already registered.</summary>
        public static SKTypeface RegisterFromStream(Stream stream, string? cacheKey = null)
        {
            ArgumentNullException.ThrowIfNull(stream);

            lock (_sync)
            {
                if (cacheKey != null && _byCacheKey.TryGetValue(cacheKey, out var existing))
                    return existing;
            }

            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            return RegisterFromData(copy.GetBuffer().AsSpan(0, (int)copy.Length), cacheKey);
        }

        /// <summary>Copies <paramref name="data"/> and registers the face. Reuses <paramref name="cacheKey"/> if already registered.</summary>
        public static SKTypeface RegisterFromData(ReadOnlySpan<byte> data, string? cacheKey = null)
        {
            if (data.IsEmpty)
                throw new ArgumentException("Font data is empty.", nameof(data));

            lock (_sync)
            {
                if (cacheKey != null && _byCacheKey.TryGetValue(cacheKey, out var existing))
                    return existing;

                var skData = SKData.CreateCopy(data);
                var typeface = SKTypeface.FromData(skData);
                if (typeface == null)
                {
                    skData.Dispose();
                    throw new InvalidOperationException("Could not create a typeface from the provided font data.");
                }

                _ownedData.Add(skData);
                AddRegistered(typeface, cacheKey);
                return typeface;
            }
        }

        /// <summary>
        /// True only when a registered face or <see cref="SKFontManager.MatchFamily(string, SKFontStyle)"/>
        /// yields a typeface whose <see cref="SKTypeface.FamilyName"/> equals <paramref name="familyName"/>.
        /// </summary>
        public static bool TryGetExact(string familyName, [NotNullWhen(true)] out SKTypeface? typeface)
        {
            typeface = null;
            if (string.IsNullOrWhiteSpace(familyName) || IsGenericFamily(familyName))
                return false;

            lock (_sync)
            {
                typeface = FindRegistered(familyName, SKFontStyle.Normal);
                if (typeface != null)
                    return true;
            }

            var matched = MatchExact(familyName, SKFontStyle.Normal);
            if (matched == null)
                return false;

            typeface = matched;
            return true;
        }

        public static SKTypeface GetTypeface(string fontName, int weight = 400, int width = 5, SKFontStyleSlant slant = SKFontStyleSlant.Upright)
        {
            if (width <= 0)
                width = 5; // SKFontStyleWidth.Normal
            if (weight <= 0)
                weight = 400;

            if (string.IsNullOrWhiteSpace(fontName))
                fontName = "sans-serif";

            string cacheKey = $"{fontName}_{weight}_{width}_{(int)slant}";
            lock (_sync)
            {
                if (_exactCache.TryGetValue(cacheKey, out var cached))
                    return cached;
            }

            var style = new SKFontStyle(weight, width, slant);
            var families = fontName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            bool sawGeneric = false;

            foreach (var family in families)
            {
                if (IsGenericFamily(family))
                {
                    sawGeneric = true;
                    continue;
                }

                SKTypeface? registered;
                lock (_sync)
                    registered = FindRegistered(family, style);
                if (registered != null)
                {
                    CacheExact(cacheKey, registered);
                    return registered;
                }

                var matched = MatchExact(family, style);
                if (matched != null)
                {
                    CacheExact(cacheKey, matched);
                    return matched;
                }
            }

            if (sawGeneric)
            {
                var generic = SKTypeface.Default ?? DefaultFace();
                CacheExact(cacheKey, generic);
                return generic;
            }

            var fallback = DefaultFace();
            LogFallback(fontName, fallback);
            return fallback;
        }

        /// <summary>Never returns an empty <see cref="SKFont"/>. Null <paramref name="typeface"/> uses the bundled/default face.</summary>
        public static SKFont CreateFont(SKTypeface typeface, float size)
        {
            var tf = typeface ?? DefaultFace();
            return new SKFont(tf, size, 1f, 0f)
            {
                Subpixel = true,
                Edging = SKFontEdging.SubpixelAntialias,
                Hinting = SKFontHinting.Normal,
            };
        }

        /// <summary>
        /// Registered faces, then exact <see cref="TryGetExact"/>, then <see cref="GetTypeface"/> fallback
        /// (MatchFamily, then bundled Roboto / <see cref="SKTypeface.Default"/>). Fallback is logged.
        /// </summary>
        public static SKFont CreateFont(string familyName, float size)
        {
            if (!string.IsNullOrWhiteSpace(familyName) && TryGetExact(familyName, out var exact))
                return CreateFont(exact, size);

            return CreateFont(GetTypeface(familyName), size);
        }

        public static V2 Measure(SKFont font, string text)
        {
            var textWidth = font.MeasureText(text);
            var textHeight = font.Size - font.Metrics.Descent;
            return new Arrays.V2(textWidth, textHeight);
        }

        public static V2 Measure(SKFont font, ReadOnlySpan<char> text)
        {
            var textWidth = font.MeasureText(text);
            var textHeight = font.Size - font.Metrics.Descent;
            return new Arrays.V2(textWidth, textHeight);
        }

        private static readonly List<SKTypeface> _fallbackFaces = new();
        private static bool _fallbacksLoaded;

        /// <summary>
        /// Typeface that can draw <paramref name="codepoint"/>, falling back to emoji / symbol fonts
        /// when <paramref name="primary"/> does not contain the glyph.
        /// </summary>
        public static SKTypeface ResolveForCodepoint(SKTypeface? primary, int codepoint)
        {
            primary ??= DefaultFace();
            if (HasGlyph(primary, codepoint))
                return primary;

            lock (_sync)
            {
                for (int i = 0; i < _registered.Count; i++)
                {
                    var tf = _registered[i];
                    if (tf != null && !ReferenceEquals(tf, primary) && HasGlyph(tf, codepoint))
                        return tf;
                }
            }

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
                using var font = new SKFont(typeface, 12f, 1f, 0f);
                return font.ContainsGlyph(codepoint);
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

            lock (_sync)
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
                        var tf = MatchExact(family, SKFontStyle.Normal);
                        if (tf != null && !_fallbackFaces.Exists(x => FamilyEquals(x.FamilyName, tf.FamilyName)))
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
                        if (tf != null && !_fallbackFaces.Exists(x => FamilyEquals(x.FamilyName, tf.FamilyName)))
                            _fallbackFaces.Add(tf);
                    }
                    catch { }
                }
            }
        }

        private static void AddRegistered(SKTypeface typeface, string? cacheKey)
        {
            _registered.Add(typeface);
            if (!string.IsNullOrEmpty(cacheKey))
                _byCacheKey[cacheKey] = typeface;
        }

        private static SKTypeface? FindRegistered(string familyName, SKFontStyle style)
        {
            SKTypeface? best = null;
            int bestScore = int.MaxValue;
            for (int i = 0; i < _registered.Count; i++)
            {
                var tf = _registered[i];
                if (tf == null || !FamilyEquals(tf.FamilyName, familyName))
                    continue;

                int score = StyleDistance(tf, style);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = tf;
                }
            }
            return best;
        }

        private static int StyleDistance(SKTypeface typeface, SKFontStyle style)
        {
            int dw = Math.Abs(typeface.FontWeight - style.Weight);
            int dwidth = Math.Abs((int)typeface.FontWidth - style.Width) * 10;
            int dslant = typeface.FontSlant == style.Slant ? 0 : 1000;
            return dw + dwidth + dslant;
        }

        private static SKTypeface? MatchExact(string familyName, SKFontStyle style)
        {
            try
            {
                var tf = SKFontManager.Default.MatchFamily(familyName, style);
                if (tf == null)
                    tf = SKFontManager.Default.MatchFamily(familyName);
                if (tf == null)
                    return null;
                if (!FamilyEquals(tf.FamilyName, familyName))
                    return null;
                if (FamilyEquals(tf.FamilyName, "Dialog") && !FamilyEquals(familyName, "Dialog"))
                    return null;
                return tf;
            }
            catch
            {
                return null;
            }
        }

        private static void CacheExact(string cacheKey, SKTypeface typeface)
        {
            lock (_sync)
                _exactCache[cacheKey] = typeface;
        }

        private static SKTypeface DefaultFace() =>
            _defaultRobotoMedium ?? _defaultRobotoLight ?? SKTypeface.Default;

        private static bool IsGenericFamily(string family) =>
            family.Equals("sans-serif", StringComparison.OrdinalIgnoreCase) ||
            family.Equals("serif", StringComparison.OrdinalIgnoreCase) ||
            family.Equals("monospace", StringComparison.OrdinalIgnoreCase) ||
            family.Equals("cursive", StringComparison.OrdinalIgnoreCase) ||
            family.Equals("fantasy", StringComparison.OrdinalIgnoreCase) ||
            family.Equals("system-ui", StringComparison.OrdinalIgnoreCase);

        private static bool FamilyEquals(string? a, string? b) =>
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static void LogFallback(string requested, SKTypeface used)
        {
            var usedName = used.FamilyName ?? "(default)";
            if (FamilyEquals(requested, usedName))
                return;

            lock (_sync)
            {
                if (!_loggedFallbacks.Add(requested))
                    return;
            }

            Log.Warning($"Font fallback: '{requested}' → '{usedName}'");
        }
    }
}
