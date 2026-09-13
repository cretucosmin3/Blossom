using System;
using System.Collections.Generic;
using System.Globalization;
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

    public static TextLayout Build(
        IReadOnlyList<TextSpan> spans,
        SKPaint basePaint,
        float maxWidth,
        float maxHeight,
        TextOverflow overflow,
        int maxLines,
        float lineHeightMul = 1.15f)
    {
        var layout = new TextLayout();
        if (basePaint == null || spans == null || spans.Count == 0)
            return layout;

        if (maxWidth <= 0) maxWidth = float.MaxValue;
        if (maxHeight <= 0) maxHeight = float.MaxValue;
        if (maxLines <= 0) maxLines = 1;

        var tokens = Tokenize(spans, basePaint);
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
                    current.Add(tok.AsEmptyLine(basePaint));
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
                foreach (var piece in BreakToken(tok, maxWidth, basePaint))
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
            ApplyEllipsis(lines, maxWidth, basePaint);

        float y = 0f;
        float maxW = 0f;
        using var measure = basePaint.Clone();
        foreach (var line in lines)
        {
            float x = 0f;
            float ascent = 0f;
            float descent = 0f;
            foreach (var tok in line)
            {
                if (tok.Width <= 0 && string.IsNullOrEmpty(tok.Text))
                    continue;
                measure.Typeface = tok.Typeface;
                measure.TextSize = tok.Size;
                measure.Color = tok.Color;
                var m = measure.FontMetrics;
                ascent = Math.Max(ascent, -m.Ascent);
                descent = Math.Max(descent, m.Descent);
            }
            if (ascent <= 0)
            {
                var m = basePaint.FontMetrics;
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
                x += tok.Width;
            }
            maxW = Math.Max(maxW, x);
            y += lineH;
        }

        layout.Width = maxW;
        layout.Height = y;
        return layout;
    }

    public static TextLayout Build(
        string text,
        SKPaint paint,
        float maxWidth,
        float maxHeight,
        TextOverflow overflow,
        int maxLines,
        float lineHeightMul = 1.15f)
    {
        var spans = new List<TextSpan>(1);
        if (!string.IsNullOrEmpty(text))
            spans.Add(new TextSpan(text));
        return Build(spans, paint, maxWidth, maxHeight, overflow, maxLines, lineHeightMul);
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

    private static void ApplyEllipsis(List<List<GlyphTok>> lines, float maxWidth, SKPaint basePaint)
    {
        if (lines.Count == 0)
            return;
        var last = lines[^1];
        var ell = MakeTok(Ellipsis, basePaint.Typeface, basePaint.TextSize, basePaint.Color, basePaint);
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

    private static List<GlyphTok> BreakToken(GlyphTok tok, float maxWidth, SKPaint basePaint)
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
            var piece = MakeTok(next, tf, size, color, basePaint);
            if (piece.Width > maxWidth && current.Length > 0)
            {
                parts.Add(MakeTok(current, tf, size, color, basePaint));
                current = c;
            }
            else
                current = next;
        }
        if (current.Length > 0)
            parts.Add(MakeTok(current, tf, size, color, basePaint));
        return parts;
    }

    private static List<GlyphTok> Tokenize(IReadOnlyList<TextSpan> spans, SKPaint basePaint)
    {
        var list = new List<GlyphTok>();
        foreach (var span in spans)
        {
            if (span == null || string.IsNullOrEmpty(span.Text))
                continue;
            float size = span.Size ?? basePaint.TextSize;
            SKColor color = span.Color ?? basePaint.Color;
            SKTypeface primary = span.Typeface ?? ResolveWeight(basePaint.Typeface, span.Weight);

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
                    list.Add(MakeTok(ws, primary, size, color, basePaint, isSpace: true));
                    continue;
                }

                int wordStart = i;
                while (i < s.Length && !IsBreak(s, i))
                    i += ClusterLen(s, i);
                string word = s[wordStart..i];
                foreach (var run in SplitByFont(word, primary, size, color, basePaint))
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

    private static List<GlyphTok> SplitByFont(string word, SKTypeface primary, float size, SKColor color, SKPaint basePaint)
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
                result.Add(MakeTok(acc, currentTf, size, color, basePaint));
                acc = "";
                currentTf = tf;
            }
            acc += c;
        }
        if (acc.Length > 0)
            result.Add(MakeTok(acc, currentTf ?? primary, size, color, basePaint));
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

    private static GlyphTok MakeTok(string text, SKTypeface? tf, float size, SKColor color, SKPaint basePaint, bool isSpace = false)
    {
        tf ??= basePaint.Typeface ?? SKTypeface.Default;
        using var p = basePaint.Clone();
        p.Typeface = tf;
        p.TextSize = size;
        p.Color = color;
        float w = p.MeasureText(text);
        var m = p.FontMetrics;
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

        public GlyphTok AsEmptyLine(SKPaint paint)
        {
            var m = paint.FontMetrics;
            return new GlyphTok("", 0, -m.Ascent, m.Descent, paint.Typeface ?? SKTypeface.Default, paint.TextSize, paint.Color, false, false);
        }
    }
}
