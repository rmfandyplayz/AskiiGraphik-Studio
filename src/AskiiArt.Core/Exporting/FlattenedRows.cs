// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Exporting;

/// <summary>
/// The visible layers flattened a row at a time for the text exports: the whole canvas, or with a selection only its
/// bounds, the cells in them that aren't selected left empty (ADR 0067).
/// </summary>
internal static class FlattenedRows
{
    /// <summary>
    /// The cells a text export covers: while anything is selected, the bounds of its cells on the canvas, which are
    /// none when it all lies past the canvas's edges; else the canvas.
    /// </summary>
    public static CellRect RegionOf(Document document, SelectionMask? selection) =>
        selection is { IsEmpty: false } ? selection.CutToCanvas().Bounds : document.CanvasSize.Bounds;

    /// <summary>Flattens row <paramref name="y"/> of <paramref name="region"/> into <paramref name="row"/>.</summary>
    public static void Read(Document document, SelectionMask? selection, CellRect region, int y, Span<Cell> row)
    {
        Compositor.CompositeRegion(document.ActiveFrame, new CellRect(region.Left, y, region.Width, 1), row);
        if (selection is not { IsEmpty: false })
            return;

        for (var x = 0; x < region.Width; x++)
        {
            if (!selection.Contains(new CellPoint(region.Left + x, y)))
                row[x] = Cell.Empty;
        }
    }
}
