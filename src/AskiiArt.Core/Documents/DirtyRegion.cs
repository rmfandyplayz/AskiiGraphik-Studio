// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// The changed cells of one layer, kept as a short list of rects (ADR 0010).
/// </summary>
/// <remarks>
/// Two rects merge only when their bounding rect covers no extra cells, so typing along a row
/// becomes one rect while scattered edits stay separate. Past <see cref="MaximumRectCount"/>
/// rects, everything collapses into one bounding rect: views then redraw a little more than
/// needed, but adding a cell stays cheap.
/// </remarks>
internal sealed class DirtyRegion
{
    public const int MaximumRectCount = 16;

    private readonly List<CellRect> _rects = [];

    public void Add(CellRect rect)
    {
        if (rect.IsEmpty)
            return;

        var merged = rect;
        var mergedAny = true;
        while (mergedAny)
        {
            mergedAny = false;
            for (var index = _rects.Count - 1; index >= 0; index--)
            {
                var existing = _rects[index];
                if (existing.Contains(merged))
                    return;

                if (UnionIsExact(existing, merged))
                {
                    merged = merged.Union(existing);
                    _rects.RemoveAt(index);
                    mergedAny = true;
                }
            }
        }

        _rects.Add(merged);

        if (_rects.Count > MaximumRectCount)
            CollapseToBounds();
    }

    public List<CellRect> ToList() => [.. _rects];

    /// <summary>True when the bounding rect of the two covers exactly the cells of one or the other.</summary>
    private static bool UnionIsExact(CellRect first, CellRect second)
    {
        // Rects that neither overlap nor touch can't have an exact union.
        if (first.Left > second.Right || second.Left > first.Right || first.Top > second.Bottom || second.Top > first.Bottom)
            return false;

        var coveredCells = first.Area + second.Area - first.Intersect(second).Area;
        return first.Union(second).Area == coveredCells;
    }

    private void CollapseToBounds()
    {
        var bounds = CellRect.Empty;
        foreach (var rect in _rects)
            bounds = bounds.Union(rect);

        _rects.Clear();
        _rects.Add(bounds);
    }
}
