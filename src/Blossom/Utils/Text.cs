using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;

namespace Blossom.Utils;

/// <summary>
/// Typography and text measurement helpers: binary-search string truncation with ellipsis,
/// word wrapping, typographic cap-height baseline calculation, and per-codepoint font fallback.
/// </summary>
public static class Text
{
    public const string Ellipsis = "…";

    /// <summary>
    /// Computes the typographic baseline Y coordinate that vertically centers
    /// the cap-height of <paramref name="font"/> on <paramref name="centerY"/>.
    /// </summary>
    public static float Baseline(SKFont font, float centerY)
    {
        SKFontMetrics m = font.Metrics;
        float cap = m.CapHeight > 0 ? m.CapHeight : -m.Ascent * 0.7f;
        return MathF.Round(centerY + cap / 2f);
    }

    /// <summary>
    /// Truncates <paramref name="text"/> so that it fits within <paramref name="maxWidth"/>,
    /// appending an ellipsis ("…") if truncated. Uses binary search for high performance.
    /// </summary>
    public static string Ellipsize(string text, SKFont font, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
            return string.Empty;

        if (MeasureWidth(text, font) <= maxWidth)
            return text;

        float ellipsisWidth = MeasureWidth(Ellipsis, font);
        if (ellipsisWidth > maxWidth)
            return string.Empty;

        int lo = 0, hi = text.Length;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            int cut = SafeCut(text, mid);
            if (MeasureWidth(text[..cut], font) + ellipsisWidth <= maxWidth)
                lo = mid;
            else
                hi = mid - 1;
        }

        return text[..SafeCut(text, lo)].TrimEnd() + Ellipsis;
    }

    /// <summary>
    /// Greedy word wrap into at most <paramref name="maxLines"/> lines (the last line is ellipsized if text overflows).
    /// Hard-breaks words that are individually wider than <paramref name="maxWidth"/>.
    /// </summary>
    public static List<string> Wrap(string text, SKFont font, float maxWidth, int maxLines = int.MaxValue)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
            return lines;

        foreach (string paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = new StringBuilder();
            foreach (string word in paragraph.Split(' '))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && MeasureWidth(candidate, font) > maxWidth)
                {
                    lines.Add(line.ToString());
                    line.Clear().Append(word);
                }
                else
                {
                    line.Clear().Append(candidate);
                }

                // A single word wider than the line (fingerprints, paths): hard-break it.
                while (MeasureWidth(line.ToString(), font) > maxWidth && line.Length > 1)
                {
                    string s = line.ToString();
                    int fit = s.Length - 1;
                    while (fit > 1 && MeasureWidth(s[..fit], font) > maxWidth)
                        fit--;
                    lines.Add(s[..fit]);
                    line.Clear().Append(s[fit..]);
                }
            }
            lines.Add(line.ToString());
        }

        if (lines.Count > maxLines)
        {
            lines.RemoveRange(maxLines, lines.Count - maxLines);
            lines[^1] = Ellipsize(lines[^1] + " " + Ellipsis, font, maxWidth);
        }

        return lines;
    }

    /// <summary>
    /// Measures the total visual advance width of <paramref name="text"/> using <paramref name="font"/>,
    /// accounting for fallback typefaces if necessary.
    /// </summary>
    public static float MeasureWidth(string text, SKFont font)
    {
        if (string.IsNullOrEmpty(text))
            return 0f;
        if (IsSimple(text))
            return font.MeasureText(text);

        float width = 0f;
        foreach ((string run, SKTypeface face) in Runs(text, font.Typeface))
        {
            using var runFont = new SKFont(face, font.Size)
            {
                Subpixel = font.Subpixel,
                Edging = font.Edging,
                Hinting = font.Hinting
            };
            width += runFont.MeasureText(run);
        }
        return width;
    }

    /// <summary>
    /// Returns true if <paramref name="text"/> contains only basic Latin and common punctuation characters
    /// that do not require multi-font fallback inspection.
    /// </summary>
    public static bool IsSimple(string text)
    {
        foreach (char ch in text)
        {
            if (ch > 'ɏ' && ch != '…' && ch != '•' && ch != '·' && ch != '–' && ch != '—' && ch != '×')
                return false;
        }
        return true;
    }

    /// <summary>
    /// Splits text into runs that each render with one typeface (primary or a symbol/emoji fallback from <see cref="Fonts"/>).
    /// </summary>
    public static IEnumerable<(string Run, SKTypeface Face)> Runs(string text, SKTypeface? primary)
    {
        SKTypeface defaultFace = primary ?? Fonts.GetTypeface("sans-serif");
        var run = new StringBuilder();
        SKTypeface? runFace = null;

        for (int i = 0; i < text.Length; i++)
        {
            int cp = char.IsSurrogatePair(text, i) ? char.ConvertToUtf32(text[i], text[i + 1]) : text[i];
            int units = cp > 0xFFFF ? 2 : 1;
            SKTypeface face = cp < 0x250 ? defaultFace : Fonts.ResolveForCodepoint(defaultFace, cp);

            if (runFace is not null && face != runFace)
            {
                yield return (run.ToString(), runFace);
                run.Clear();
            }

            runFace = face;
            run.Append(text, i, units);
            i += units - 1;
        }

        if (run.Length > 0 && runFace is not null)
            yield return (run.ToString(), runFace);
    }

    /// <summary>Prevents cutting across surrogate pairs in UTF-16 strings.</summary>
    public static int SafeCut(string text, int index) =>
        index > 0 && index < text.Length && char.IsLowSurrogate(text[index]) ? index - 1 : index;
}
