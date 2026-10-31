// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// Where the cells go when a canvas is resized with an anchor (ARCHITECTURE §5.7, ADR 0011): how
/// far they move, which survive, and which are cropped.
/// </summary>
public sealed record CanvasResizePlan
{
    private CanvasResizePlan(CanvasSize oldSize, CanvasSize newSize, CellPoint contentOffset)
    {
        OldSize = oldSize;
        NewSize = newSize;
        ContentOffset = contentOffset;
        KeptRegion = oldSize.Bounds.Intersect(newSize.Bounds.Offset(contentOffset.Negate()));
        CroppedRegions = SubtractFromBounds(oldSize.Bounds, KeptRegion);
    }

    /// <summary>The size before the resize.</summary>
    public CanvasSize OldSize { get; }

    /// <summary>The size after the resize.</summary>
    public CanvasSize NewSize { get; }

    /// <summary>
    /// How far every cell moves: the old cell at (x, y) ends up at
    /// (x + <c>ContentOffset.X</c>, y + <c>ContentOffset.Y</c>). Negative values mean cells were
    /// cropped at the left or top.
    /// </summary>
    public CellPoint ContentOffset { get; }

    /// <summary>The cells that survive, in old-canvas coordinates.</summary>
    public CellRect KeptRegion { get; }

    /// <summary>
    /// The cells that are cropped, in old-canvas coordinates, as up to four non-overlapping rects.
    /// Empty when the canvas only grows.
    /// </summary>
    public IReadOnlyList<CellRect> CroppedRegions { get; }

    /// <summary>Plans a resize from <paramref name="oldSize"/> to <paramref name="newSize"/> that keeps <paramref name="anchor"/> in place.</summary>
    /// <remarks>
    /// When a centered side changes by an odd number of cells, the extra cell goes to the right or
    /// bottom. Resizing back with the same anchor exactly undoes the move.
    /// </remarks>
    public static CanvasResizePlan Create(CanvasSize oldSize, CanvasSize newSize, ResizeAnchor anchor)
    {
        var offset = new CellPoint(
            AxisOffset(oldSize.Width, newSize.Width, HorizontalPosition(anchor)),
            AxisOffset(oldSize.Height, newSize.Height, VerticalPosition(anchor)));

        return new CanvasResizePlan(oldSize, newSize, offset);
    }

    private static int AxisOffset(int oldLength, int newLength, AxisPosition position)
    {
        var growth = newLength - oldLength;
        return position switch
        {
            AxisPosition.Start => 0,
            // Integer division rounds toward zero, so growing and then shrinking by the same
            // amount moves the cells back exactly.
            AxisPosition.Middle => growth / 2,
            AxisPosition.End => growth,
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
        };
    }

    private static AxisPosition HorizontalPosition(ResizeAnchor anchor) => anchor switch
    {
        ResizeAnchor.TopLeft or ResizeAnchor.Left or ResizeAnchor.BottomLeft => AxisPosition.Start,
        ResizeAnchor.Top or ResizeAnchor.Center or ResizeAnchor.Bottom => AxisPosition.Middle,
        ResizeAnchor.TopRight or ResizeAnchor.Right or ResizeAnchor.BottomRight => AxisPosition.End,
        _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null)
    };

    private static AxisPosition VerticalPosition(ResizeAnchor anchor) => anchor switch
    {
        ResizeAnchor.TopLeft or ResizeAnchor.Top or ResizeAnchor.TopRight => AxisPosition.Start,
        ResizeAnchor.Left or ResizeAnchor.Center or ResizeAnchor.Right => AxisPosition.Middle,
        ResizeAnchor.BottomLeft or ResizeAnchor.Bottom or ResizeAnchor.BottomRight => AxisPosition.End,
        _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null)
    };

    /// <summary>Splits the part of <paramref name="bounds"/> outside <paramref name="kept"/> into top, bottom, left and right strips.</summary>
    private static List<CellRect> SubtractFromBounds(CellRect bounds, CellRect kept)
    {
        if (kept.IsEmpty)
            return [bounds];

        var strips = new List<CellRect>
        {
            CellRect.FromEdges(bounds.Left, bounds.Top, bounds.Right, kept.Top),
            CellRect.FromEdges(bounds.Left, kept.Bottom, bounds.Right, bounds.Bottom),
            CellRect.FromEdges(bounds.Left, kept.Top, kept.Left, kept.Bottom),
            CellRect.FromEdges(kept.Right, kept.Top, bounds.Right, kept.Bottom)
        };
        strips.RemoveAll(strip => strip.IsEmpty);
        return strips;
    }

    private enum AxisPosition
    {
        Start,
        Middle,
        End
    }
}
