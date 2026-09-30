using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Blossom.Core.Visual;
using SkiaSharp;

namespace Blossom.Primitives;

/// <summary>
/// Places and sizes children into tracks. Tree order flows row-major into empty cells;
/// <see cref="SetCell"/> pins a child. Default cell align is Stretch (the grid sizes the child).
/// </summary>
public class Grid : LayoutElement
{
    private static readonly ConditionalWeakTable<VisualElement, CellBox> Cells = new();

    private List<LayoutLength> _columns = new() { LayoutLength.Star(1f) };
    private List<LayoutLength> _rows = new();
    private float _columnGap;
    private float _rowGap;
    private LayoutAlign _align = LayoutAlign.Stretch;

    public IReadOnlyList<LayoutLength> ColumnTracks => _columns;
    public IReadOnlyList<LayoutLength> RowTracks => _rows;

    /// <summary>Comma-separated tracks, e.g. <c>"200, *, 120"</c>.</summary>
    public string Columns
    {
        get => string.Join(", ", _columns);
        set
        {
            var parsed = LayoutLength.ParseList(value ?? "");
            _columns = parsed.Count > 0 ? parsed : new List<LayoutLength> { LayoutLength.Star(1f) };
            InvalidateLayout();
        }
    }

    /// <summary>Comma-separated tracks, e.g. <c>"Auto, *"</c>. Empty grows Auto rows as children flow.</summary>
    public string Rows
    {
        get => string.Join(", ", _rows);
        set
        {
            _rows = LayoutLength.ParseList(value ?? "");
            InvalidateLayout();
        }
    }

    public float ColumnGap
    {
        get => _columnGap;
        set
        {
            if (_columnGap == value) return;
            _columnGap = Math.Max(0f, value);
            InvalidateLayout();
        }
    }

    public float RowGap
    {
        get => _rowGap;
        set
        {
            if (_rowGap == value) return;
            _rowGap = Math.Max(0f, value);
            InvalidateLayout();
        }
    }

    public LayoutAlign Align
    {
        get => _align;
        set
        {
            if (_align == value) return;
            _align = value;
            InvalidateLayout();
        }
    }

    public static void SetCell(VisualElement element, int column, int row, int columnSpan = 1, int rowSpan = 1)
    {
        if (element == null) return;
        var spec = new CellBox
        {
            Column = Math.Max(0, column),
            Row = Math.Max(0, row),
            ColumnSpan = Math.Max(1, columnSpan),
            RowSpan = Math.Max(1, rowSpan),
            Assigned = true
        };
        if (Cells.TryGetValue(element, out var existing))
        {
            existing.Column = spec.Column;
            existing.Row = spec.Row;
            existing.ColumnSpan = spec.ColumnSpan;
            existing.RowSpan = spec.RowSpan;
            existing.Assigned = true;
        }
        else
        {
            Cells.Add(element, spec);
        }

        (element.Parent as VisualElement)?.InvalidateLayout();
    }

    public static bool TryGetCell(VisualElement element, out int column, out int row, out int columnSpan, out int rowSpan)
    {
        column = row = 0;
        columnSpan = rowSpan = 1;
        if (element == null || !Cells.TryGetValue(element, out var box) || !box.Assigned)
            return false;
        column = box.Column;
        row = box.Row;
        columnSpan = box.ColumnSpan;
        rowSpan = box.RowSpan;
        return true;
    }

    protected override void LayoutChildren()
    {
        var kids = new List<VisualElement>();
        foreach (var child in ParticipatingChildren())
            kids.Add(child);
        if (kids.Count == 0) return;

        int nCols = Math.Max(1, _columns.Count);
        var colTracks = new List<LayoutLength>(_columns);
        if (colTracks.Count == 0)
            colTracks.Add(LayoutLength.Star(1f));

        var rowTracks = new List<LayoutLength>(_rows);
        var placements = new CellBox[kids.Count];
        var occupied = new HashSet<(int c, int r)>();

        int NeededRows()
        {
            int max = rowTracks.Count;
            for (int i = 0; i < kids.Count; i++)
            {
                var p = placements[i];
                if (p != null)
                    max = Math.Max(max, p.Row + p.RowSpan);
            }
            return Math.Max(1, max);
        }

        void Occupy(CellBox p)
        {
            for (int c = p.Column; c < p.Column + p.ColumnSpan; c++)
            for (int r = p.Row; r < p.Row + p.RowSpan; r++)
                occupied.Add((c, r));
        }

        bool Free(int col, int row, int colSpan, int rowSpan)
        {
            for (int c = col; c < col + colSpan; c++)
            for (int r = row; r < row + rowSpan; r++)
            {
                if (occupied.Contains((c, r)))
                    return false;
            }
            return true;
        }

        CellBox FlowNext()
        {
            int rows = Math.Max(rowTracks.Count, 1);
            for (int r = 0; ; r++)
            {
                if (r >= rows)
                {
                    rowTracks.Add(LayoutLength.Star(1f));
                    rows = rowTracks.Count;
                }
                for (int c = 0; c < nCols; c++)
                {
                    if (Free(c, r, 1, 1))
                    {
                        var p = new CellBox { Column = c, Row = r, ColumnSpan = 1, RowSpan = 1, Assigned = true };
                        Occupy(p);
                        return p;
                    }
                }
            }
        }

        for (int i = 0; i < kids.Count; i++)
        {
            if (TryGetCell(kids[i], out int col, out int row, out int cs, out int rs))
            {
                col = Math.Min(col, nCols - 1);
                cs = Math.Min(cs, nCols - col);
                while (row + rs > rowTracks.Count)
                    rowTracks.Add(LayoutLength.Star(1f));
                var p = new CellBox { Column = col, Row = row, ColumnSpan = cs, RowSpan = rs, Assigned = true };
                placements[i] = p;
                Occupy(p);
            }
        }

        for (int i = 0; i < kids.Count; i++)
        {
            if (placements[i] != null) continue;
            placements[i] = FlowNext();
        }

        int nRows = NeededRows();
        while (rowTracks.Count < nRows)
            rowTracks.Add(LayoutLength.Star(1f));

        float innerW = Math.Max(0f, Transform.Width - Padding.Horizontal);
        float innerH = Math.Max(0f, Transform.Height - Padding.Vertical);
        var colSizes = ResolveTracks(colTracks, innerW, ColumnGap, kids, placements, isColumn: true);
        var rowSizes = ResolveTracks(rowTracks, innerH, RowGap, kids, placements, isColumn: false);

        float originX = Transform.AbsoluteX + Padding.Left;
        float originY = Transform.AbsoluteY + Padding.Top;

        for (int i = 0; i < kids.Count; i++)
        {
            var p = placements[i];
            float x = originX + Offset(colSizes, ColumnGap, p.Column);
            float y = originY + Offset(rowSizes, RowGap, p.Row);
            float w = SpanSize(colSizes, ColumnGap, p.Column, p.ColumnSpan);
            float h = SpanSize(rowSizes, RowGap, p.Row, p.RowSpan);
            float prefW = PreferredWidth(kids[i], w, h);
            float prefH = PreferredHeight(kids[i], w, h);
            AlignInSlot(kids[i], x, y, w, h, prefW, prefH, Align, Align);
        }
    }

    public override SKSize GetPreferredSize(float maxWidth, float maxHeight)
    {
        int n = 0;
        foreach (var _ in ParticipatingChildren())
            n++;

        float w = Transform.Width;
        float h = Transform.Height;
        if (w <= 0) w = maxWidth > 0 ? maxWidth : 0;
        if (h <= 0) h = maxHeight > 0 ? maxHeight : 0;
        w += Padding.Horizontal;
        h += Padding.Vertical;
        if (n == 0)
            return new SKSize(w, h);

        if (MinWidth.HasValue) w = Math.Max(w, MinWidth.Value);
        if (MinHeight.HasValue) h = Math.Max(h, MinHeight.Value);
        return new SKSize(w, h);
    }

    private float[] ResolveTracks(List<LayoutLength> tracks, float available, float gap,
        List<VisualElement> kids, CellBox[] placements, bool isColumn)
    {
        int n = tracks.Count;
        var sizes = new float[n];
        float gapTotal = gap * Math.Max(0, n - 1);
        float leftover = Math.Max(0f, available - gapTotal);
        float starSum = 0f;

        for (int t = 0; t < n; t++)
        {
            var len = tracks[t];
            if (len.Unit == LayoutUnit.Pixel)
            {
                sizes[t] = len.Value;
                leftover -= sizes[t];
            }
            else if (len.Unit == LayoutUnit.Star)
            {
                starSum += Math.Max(0f, len.Value);
            }
            else
            {
                float max = 0f;
                for (int i = 0; i < kids.Count; i++)
                {
                    var p = placements[i];
                    int start = isColumn ? p.Column : p.Row;
                    int span = isColumn ? p.ColumnSpan : p.RowSpan;
                    if (start != t || span != 1) continue;
                    max = Math.Max(max, isColumn
                        ? PreferredWidth(kids[i], leftover, 0f)
                        : PreferredHeight(kids[i], 0f, leftover));
                }
                sizes[t] = max;
                leftover -= sizes[t];
            }
        }

        leftover = Math.Max(0f, leftover);
        if (starSum > 0f)
        {
            for (int t = 0; t < n; t++)
            {
                if (tracks[t].Unit == LayoutUnit.Star)
                    sizes[t] = leftover * (tracks[t].Value / starSum);
            }
        }

        return sizes;
    }

    private static float Offset(float[] sizes, float gap, int index)
    {
        float x = 0f;
        for (int i = 0; i < index; i++)
            x += sizes[i] + gap;
        return x;
    }

    private static float SpanSize(float[] sizes, float gap, int start, int span)
    {
        float s = 0f;
        for (int i = 0; i < span; i++)
        {
            int idx = start + i;
            if (idx >= 0 && idx < sizes.Length)
                s += sizes[idx];
            if (i > 0)
                s += gap;
        }
        return s;
    }

    private sealed class CellBox
    {
        public int Column;
        public int Row;
        public int ColumnSpan = 1;
        public int RowSpan = 1;
        public bool Assigned;
    }
}
