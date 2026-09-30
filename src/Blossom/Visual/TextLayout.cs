using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Blossom.Core.Visual.Enums;
using Blossom.Utils;
using SkiaSharp;

namespace Blossom.Core.Visual;

/// <summary>
/// Word-aware, emoji-capable text layout used by <see cref="VisualElement"/> and <see cref="RichBox"/>.
/// Produces positioned runs that can be clipped or ellipsized to a box.
/// </summary>
public sealed class TextLayout
{
    public const string Ellipsis = "…";

    public readonly struct Run
    {
        public Run(string text, float x, float y, float width, float ascent, float descent, SKTypeface typeface, float size, SKColor color)
        {
            Text = text;
            X = x;
            Y = y;
            Width = width;
            Ascent = ascent;
            Descent = descent;
            Typeface = typeface;
            Size = size;
            Color = color;
        }

        public string Text { get; }
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Ascent { get; }
        public float Descent { get; }
        public SKTypeface Typeface { get; }
        public float Size { get; }
        public SKColor Color { get; }
    }

    public List<Run> Runs { get; } = new();
    public float Width { get; private set; }
    public float Height { get; private set; }

    private readonly List<GraphemePlacement> _graphemes = new();

    /// <summary>Number of grapheme clusters in the laid-out text (caret stops = count + 1 including end).</summary>
    public int GraphemeCount => _graphemes.Count;

    public static TextLayout Build(
        IReadOnlyList<TextSpan> spans,
        SKFont baseFont,
        SKColor baseColor,
        float maxWidth,
        float maxHeight,
        TextOverflow overflow,
        int maxLines,
        float lineHeightMul = 1.15f)
    {
        var layout = new TextLayout();
        if (baseFont == null || spans == null || spans.Count == 0)
            return layout;

        if (maxWidth <= 0) maxWidth = float.MaxValue;
        if (maxHeight <= 0) maxHeight = float.MaxValue;
        if (maxLines <= 0) maxLines = 1;

        var tokens = Tokenize(spans, baseFont, baseColor);
        if (tokens.Count == 0)
            return layout;

        bool wrap = overflow == TextOverflow.Wrap
            || overflow == TextOverflow.Ellipsis
            || overflow == TextOverflow.Clip
            || maxLines > 1
            || (overflow != TextOverflow.Visible && maxWidth < float.MaxValue / 4f);
        bool ellipsis = overflow == TextOverflow.Ellipsis;

        var lines = new List<List<GlyphTok>>();
        var current = new List<GlyphTok>();
        float lineW = 0f;
        bool stoppedEarly = false;

        void FlushLine()
        {
            if (current.Count == 0)
                return;
            lines.Add(current);
            current = new List<GlyphTok>();
            lineW = 0f;
        }

        for (int i = 0; i < tokens.Count; i++)
        {
            var tok = tokens[i];
            if (tok.IsNewline)
            {
                if (current.Count == 0)
                    current.Add(tok.AsEmptyLine(baseFont, baseColor));
                FlushLine();
                if (lines.Count >= maxLines)
                {
                    stoppedEarly = i < tokens.Count - 1;
                    break;
                }
                continue;
            }

            if (tok.IsSpace && current.Count == 0)
                continue;

            float w = tok.Width;
            bool overflowLine = wrap && lineW + w > maxWidth && current.Count > 0;
            if (overflowLine)
            {
                FlushLine();
                if (lines.Count >= maxLines)
                {
                    stoppedEarly = true;
                    break;
                }
                if (tok.IsSpace)
                    continue;
            }

            if (wrap && tok.Width > maxWidth && !tok.IsSpace)
            {
                foreach (var piece in BreakToken(tok, maxWidth, baseFont))
                {
                    if (lineW + piece.Width > maxWidth && current.Count > 0)
                    {
                        FlushLine();
                        if (lines.Count >= maxLines)
                        {
                            stoppedEarly = true;
                            break;
                        }
                    }
                    if (lines.Count >= maxLines)
                        break;
                    current.Add(piece);
                    lineW += piece.Width;
                }
                if (lines.Count >= maxLines && i < tokens.Count - 1)
                {
                    stoppedEarly = true;
                    break;
                }
                continue;
            }

            current.Add(tok);
            lineW += w;
        }
        FlushLine();

        if (lines.Count == 0)
            return layout;

        if (lines.Count > maxLines)
            lines.RemoveRange(maxLines, lines.Count - maxLines);

        if (ellipsis && (stoppedEarly || lineWouldOverflow(tokens, maxWidth, maxLines)))
            ApplyEllipsis(lines, maxWidth, baseFont, baseColor);

        float y = 0f;
        float maxW = 0f;
        int lineIndex = 0;
        foreach (var line in lines)
        {
            float x = 0f;
            float ascent = 0f;
            float descent = 0f;
            foreach (var tok in line)
            {
                if (tok.Width <= 0 && string.IsNullOrEmpty(tok.Text))
                    continue;
                using var measure = FontOf(tok.Typeface, tok.Size, baseFont);
                var m = measure.Metrics;
                ascent = Math.Max(ascent, -m.Ascent);
                descent = Math.Max(descent, m.Descent);
            }
            if (ascent <= 0)
            {
                var m = baseFont.Metrics;
                ascent = -m.Ascent;
                descent = m.Descent;
            }

            float lineH = (ascent + descent) * lineHeightMul;
            if (y + lineH > maxHeight + 0.5f && layout.Runs.Count > 0)
                break;

            float baseline = y + ascent;
            x = 0f;
            foreach (var tok in line)
            {
                if (string.IsNullOrEmpty(tok.Text))
                    continue;
                layout.Runs.Add(new Run(tok.Text, x, baseline, tok.Width, ascent, descent, tok.Typeface, tok.Size, tok.Color));
                layout.AddGraphemes(tok, x, baseline, ascent, descent, lineIndex, baseFont);
                x += tok.Width;
            }
            maxW = Math.Max(maxW, x);
            y += lineH;
            lineIndex++;
        }

        layout.Width = maxW;
        layout.Height = y;
        return layout;
    }

    public static TextLayout Build(
        string text,
        SKFont font,
        SKColor color,
        float maxWidth,
        float maxHeight,
        TextOverflow overflow,
        int maxLines,
        float lineHeightMul = 1.15f)
    {
        var spans = new List<TextSpan>(1);
        if (!string.IsNullOrEmpty(text))
            spans.Add(new TextSpan(text));
        return Build(spans, font, color, maxWidth, maxHeight, overflow, maxLines, lineHeightMul);
    }

    /// <summary>
    /// Grapheme caret index closest to <paramref name="x"/>, <paramref name="y"/> in layout-local coordinates.
    /// Uses a 50% glyph split; Y picks the nearest line.
    /// </summary>
    public int CaretIndexFromPoint(float x, float y)
    {
        int n = _graphemes.Count;
        if (n == 0)
            return 0;

        int bestLine = _graphemes[0].Line;
        float bestDist = float.MaxValue;
        int line = int.MinValue;
        for (int i = 0; i < n; i++)
        {
            var g = _graphemes[i];
            if (g.Line == line)
                continue;
            line = g.Line;
            float top = g.Baseline - g.Ascent;
            float bottom = g.Baseline + g.Descent;
            float dist = y < top ? top - y : (y > bottom ? y - bottom : 0f);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestLine = line;
            }
        }

        int lastOnLine = -1;
        for (int i = 0; i < n; i++)
        {
            var g = _graphemes[i];
            if (g.Line != bestLine)
                continue;
            lastOnLine = i;
            if (x < g.X + g.Width * 0.5f)
                return i;
        }

        return lastOnLine >= 0 ? lastOnLine + 1 : n;
    }

    /// <summary>
    /// Zero-width caret rectangle in layout-local coordinates for the grapheme boundary at
    /// <paramref name="graphemeIndex"/> (0..GraphemeCount).
    /// </summary>
    public SKRect CaretRect(int graphemeIndex)
    {
        int n = _graphemes.Count;
        if (n == 0)
            return SKRect.Empty;

        int i = Math.Clamp(graphemeIndex, 0, n);
        if (i < n)
        {
            var g = _graphemes[i];
            float top = g.Baseline - g.Ascent;
            return new SKRect(g.X, top, g.X, top + g.Ascent + g.Descent);
        }

        var last = _graphemes[n - 1];
        float lastTop = last.Baseline - last.Ascent;
        float x = last.X + last.Width;
        return new SKRect(x, lastTop, x, lastTop + last.Ascent + last.Descent);
    }

    /// <summary>Moves a grapheme caret by <paramref name="delta"/> clusters, clamped to [0, GraphemeCount].</summary>
    public int MoveByGrapheme(int index, int delta)
    {
        int n = GraphemeCount;
        if (n == 0)
            return 0;
        long next = (long)index + delta;
        if (next < 0) return 0;
        if (next > n) return n;
        return (int)next;
    }

    /// <summary>
    /// Moves a grapheme caret by word. Positive <paramref name="direction"/> is forward;
    /// negative is backward. Magnitude is the number of word steps.
    /// </summary>
    public int MoveByWord(int index, int direction)
    {
        int n = GraphemeCount;
        if (n == 0 || direction == 0)
            return n == 0 ? 0 : Math.Clamp(index, 0, n);

        index = Math.Clamp(index, 0, n);
        int steps = Math.Abs(direction);
        bool forward = direction > 0;
        for (int s = 0; s < steps; s++)
            index = MoveOneWord(index, forward);
        return index;
    }

    private int MoveOneWord(int index, bool forward)
    {
        int n = GraphemeCount;
        if (forward)
        {
            int i = index;
            while (i < n && !IsWordSeparator(i)) i++;
            while (i < n && IsWordSeparator(i)) i++;
            return i;
        }

        int j = index;
        if (j > 0) j--;
        while (j > 0 && IsWordSeparator(j)) j--;
        while (j > 0 && !IsWordSeparator(j - 1)) j--;
        return j;
    }

    private bool IsWordSeparator(int graphemeIndex)
    {
        if ((uint)graphemeIndex >= (uint)_graphemes.Count)
            return true;
        string cluster = _graphemes[graphemeIndex].Cluster;
        if (string.IsNullOrEmpty(cluster))
            return true;
        foreach (var rune in cluster.EnumerateRunes())
        {
            if (!Rune.IsWhiteSpace(rune) && rune.Value != '\n' && rune.Value != '\r')
                return false;
        }
        return true;
    }

    private void AddGraphemes(GlyphTok tok, float runX, float baseline, float ascent, float descent, int line, SKFont baseFont)
    {
        var clusters = Clusters(tok.Text);
        if (clusters.Count == 0)
            return;

        using var font = FontOf(tok.Typeface, tok.Size, baseFont);
        float x = runX;
        for (int i = 0; i < clusters.Count; i++)
        {
            string cluster = clusters[i];
            float w = i == clusters.Count - 1
                ? Math.Max(0f, (runX + tok.Width) - x)
                : font.MeasureText(cluster);
            _graphemes.Add(new GraphemePlacement(cluster, x, baseline, w, ascent, descent, line));
            x += w;
        }
    }

    private readonly struct GraphemePlacement
    {
        public GraphemePlacement(string cluster, float x, float baseline, float width, float ascent, float descent, int line)
        {
            Cluster = cluster;
            X = x;
            Baseline = baseline;
            Width = width;
            Ascent = ascent;
            Descent = descent;
            Line = line;
        }

        public string Cluster { get; }
        public float X { get; }
        public float Baseline { get; }
        public float Width { get; }
        public float Ascent { get; }
        public float Descent { get; }
        public int Line { get; }
    }

    private static bool lineWouldOverflow(List<GlyphTok> tokens, float maxWidth, int maxLines)
    {
        float w = 0f;
        int lines = 1;
        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.IsNewline)
            {
                lines++;
                w = 0f;
                if (lines > maxLines) return true;
                continue;
            }
            if (t.IsSpace && w == 0f) continue;
            if (w + t.Width > maxWidth && w > 0f)
            {
                lines++;
                w = t.IsSpace ? 0f : t.Width;
                if (lines > maxLines) return true;
            }
            else
                w += t.Width;
        }
        return false;
    }

    private static void ApplyEllipsis(List<List<GlyphTok>> lines, float maxWidth, SKFont baseFont, SKColor baseColor)
    {
        if (lines.Count == 0)
            return;
        var last = lines[^1];
        var ell = MakeTok(Ellipsis, baseFont.Typeface, baseFont.Size, baseColor, baseFont);
        float avail = maxWidth - ell.Width;
        if (avail < 0) avail = 0;

        float w = 0f;
        int keep = 0;
        for (int i = 0; i < last.Count; i++)
        {
            if (w + last[i].Width > avail)
                break;
            w += last[i].Width;
            keep++;
        }
        if (keep < last.Count)
            last.RemoveRange(keep, last.Count - keep);
        while (last.Count > 0 && last[^1].IsSpace)
            last.RemoveAt(last.Count - 1);
        last.Add(ell);
    }

    private static List<GlyphTok> BreakToken(GlyphTok tok, float maxWidth, SKFont baseFont)
    {
        var parts = new List<GlyphTok>();
        var clusters = Clusters(tok.Text);
        var current = "";
        SKTypeface? tf = tok.Typeface;
        float size = tok.Size;
        SKColor color = tok.Color;
        foreach (var c in clusters)
        {
            string next = current + c;
            var piece = MakeTok(next, tf, size, color, baseFont);
            if (piece.Width > maxWidth && current.Length > 0)
            {
                parts.Add(MakeTok(current, tf, size, color, baseFont));
                current = c;
            }
            else
                current = next;
        }
        if (current.Length > 0)
            parts.Add(MakeTok(current, tf, size, color, baseFont));
        return parts;
    }

    private static List<GlyphTok> Tokenize(IReadOnlyList<TextSpan> spans, SKFont baseFont, SKColor baseColor)
    {
        var list = new List<GlyphTok>();
        foreach (var span in spans)
        {
            if (span == null || string.IsNullOrEmpty(span.Text))
                continue;
            float size = span.Size ?? baseFont.Size;
            SKColor color = span.Color ?? baseColor;
            SKTypeface primary = span.Typeface ?? ResolveWeight(baseFont.Typeface, span.Weight);

            int i = 0;
            string s = span.Text;
            while (i < s.Length)
            {
                if (s[i] == '\r')
                {
                    i++;
                    if (i < s.Length && s[i] == '\n') i++;
                    list.Add(GlyphTok.Newline());
                    continue;
                }
                if (s[i] == '\n')
                {
                    i++;
                    list.Add(GlyphTok.Newline());
                    continue;
                }

                if (char.IsWhiteSpace(s[i]) && s[i] != '\u00A0')
                {
                    int start = i;
                    while (i < s.Length && char.IsWhiteSpace(s[i]) && s[i] != '\n' && s[i] != '\r' && s[i] != '\u00A0')
                        i++;
                    string ws = s[start..i];
                    list.Add(MakeTok(ws, primary, size, color, baseFont, isSpace: true));
                    continue;
                }

                int wordStart = i;
                while (i < s.Length && !IsBreak(s, i))
                    i += ClusterLen(s, i);
                string word = s[wordStart..i];
                foreach (var run in SplitByFont(word, primary, size, color, baseFont))
                    list.Add(run);
            }
        }
        return list;
    }

    private static bool IsBreak(string s, int i)
    {
        char c = s[i];
        if (c == '\n' || c == '\r') return true;
        return char.IsWhiteSpace(c) && c != '\u00A0';
    }

    private static int ClusterLen(string s, int i)
    {
        var e = StringInfo.GetTextElementEnumerator(s, i);
        if (!e.MoveNext())
            return 1;
        string el = e.GetTextElement();
        return Math.Max(1, el.Length);
    }

    private static List<string> Clusters(string s)
    {
        var list = new List<string>();
        var e = StringInfo.GetTextElementEnumerator(s);
        while (e.MoveNext())
            list.Add(e.GetTextElement());
        return list;
    }

    private static List<GlyphTok> SplitByFont(string word, SKTypeface primary, float size, SKColor color, SKFont baseFont)
    {
        var result = new List<GlyphTok>();
        if (string.IsNullOrEmpty(word))
            return result;

        var clusters = Clusters(word);
        SKTypeface? currentTf = null;
        string acc = "";
        foreach (var c in clusters)
        {
            int cp = Codepoint(c);
            var tf = Fonts.ResolveForCodepoint(primary, cp);
            if (currentTf == null)
                currentTf = tf;
            if (!ReferenceEquals(tf, currentTf) && acc.Length > 0)
            {
                result.Add(MakeTok(acc, currentTf, size, color, baseFont));
                acc = "";
                currentTf = tf;
            }
            acc += c;
        }
        if (acc.Length > 0)
            result.Add(MakeTok(acc, currentTf ?? primary, size, color, baseFont));
        return result;
    }

    private static int Codepoint(string cluster)
    {
        if (string.IsNullOrEmpty(cluster))
            return 0;
        return char.ConvertToUtf32(cluster, 0);
    }

    private static SKTypeface ResolveWeight(SKTypeface? primary, int? weight)
    {
        if (primary == null)
            return Fonts.GetTypeface("sans-serif");
        if (!weight.HasValue || weight.Value == primary.FontWeight)
            return primary;
        return Fonts.GetTypeface(primary.FamilyName, weight.Value);
    }

    private static SKFont FontOf(SKTypeface? typeface, float size, SKFont prototype)
    {
        var tf = typeface ?? prototype.Typeface ?? SKTypeface.Default;
        return new SKFont(tf, size, 1f, 0f)
        {
            Subpixel = prototype.Subpixel,
            Edging = prototype.Edging,
            Hinting = prototype.Hinting,
        };
    }

    private static GlyphTok MakeTok(string text, SKTypeface? tf, float size, SKColor color, SKFont baseFont, bool isSpace = false)
    {
        tf ??= baseFont.Typeface ?? SKTypeface.Default;
        using var font = FontOf(tf, size, baseFont);
        float w = font.MeasureText(text);
        var m = font.Metrics;
        return new GlyphTok(text, w, -m.Ascent, m.Descent, tf, size, color, isSpace, isNewline: false);
    }

    private readonly struct GlyphTok
    {
        public GlyphTok(string text, float width, float ascent, float descent, SKTypeface typeface, float size, SKColor color, bool isSpace, bool isNewline)
        {
            Text = text;
            Width = width;
            Ascent = ascent;
            Descent = descent;
            Typeface = typeface;
            Size = size;
            Color = color;
            IsSpace = isSpace;
            IsNewline = isNewline;
        }

        public string Text { get; }
        public float Width { get; }
        public float Ascent { get; }
        public float Descent { get; }
        public SKTypeface Typeface { get; }
        public float Size { get; }
        public SKColor Color { get; }
        public bool IsSpace { get; }
        public bool IsNewline { get; }

        public static GlyphTok Newline() =>
            new("\n", 0, 0, 0, SKTypeface.Default, 0, SKColors.Transparent, false, true);

        public GlyphTok AsEmptyLine(SKFont font, SKColor color)
        {
            var m = font.Metrics;
            return new GlyphTok("", 0, -m.Ascent, m.Descent, font.Typeface ?? SKTypeface.Default, font.Size, color, false, false);
        }
    }
}
