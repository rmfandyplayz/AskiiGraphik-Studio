// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using Avalonia;

namespace AskiiArt.App.Canvas;

/// <summary>
/// Where the canvas sits inside the canvas control, all in device pixels: which cells are visible,
/// which cell is under a point, and how scrolling and zooming move things. Immutable, and pure
/// maths, so it's tested without a window.
/// </summary>
/// <remarks>
/// The scrollable content is the canvas plus <see cref="Margin"/> of empty space on every side.
/// Along an axis where that content fits in the viewport, the canvas is centred and can't scroll.
/// </remarks>
internal sealed record CanvasViewport
{
    public CanvasViewport(CanvasSize canvasSize, PixelSize cellSize, PixelSize viewportSize, int margin,
        PixelPoint scrollOffset)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cellSize.Width, 1, nameof(cellSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(cellSize.Height, 1, nameof(cellSize));
        ArgumentOutOfRangeException.ThrowIfNegative(margin);

        CanvasSize = canvasSize;
        CellSize = cellSize;
        ViewportSize = new PixelSize(Math.Max(0, viewportSize.Width), Math.Max(0, viewportSize.Height));
        Margin = margin;

        var maximum = MaximumScrollOffset;
        ScrollOffset = new PixelPoint(Math.Clamp(scrollOffset.X, 0, maximum.X), Math.Clamp(scrollOffset.Y, 0, maximum.Y));
    }

    public CanvasSize CanvasSize { get; }

    public PixelSize CellSize { get; }

    public PixelSize ViewportSize { get; }

    /// <summary>The empty space around the canvas when it's scrolled to an edge.</summary>
    public int Margin { get; }

    /// <summary>How far the content is scrolled, between zero and <see cref="MaximumScrollOffset"/>.</summary>
    public PixelPoint ScrollOffset { get; }

    public PixelSize CanvasPixelSize => new(CanvasSize.Width * CellSize.Width, CanvasSize.Height * CellSize.Height);

    /// <summary>The scrollable size: the canvas plus its margin on every side.</summary>
    public PixelSize ContentSize => new(CanvasPixelSize.Width + 2 * Margin, CanvasPixelSize.Height + 2 * Margin);

    public PixelPoint MaximumScrollOffset =>
        new(Math.Max(0, ContentSize.Width - ViewportSize.Width), Math.Max(0, ContentSize.Height - ViewportSize.Height));

    /// <summary>The canvas's top-left corner, relative to the viewport's.</summary>
    public PixelPoint CanvasOrigin => new(
        OriginAlong(ContentSize.Width, ViewportSize.Width, CanvasPixelSize.Width, ScrollOffset.X),
        OriginAlong(ContentSize.Height, ViewportSize.Height, CanvasPixelSize.Height, ScrollOffset.Y));

    /// <summary>The canvas's rectangle, relative to the viewport.</summary>
    public PixelRect CanvasBounds => new(CanvasOrigin, CanvasPixelSize);

    /// <summary>The cells that are at least partly inside the viewport; empty if none are.</summary>
    public CellRect VisibleCells
    {
        get
        {
            var origin = CanvasOrigin;
            var left = Math.Max(0, FloorDivide(-origin.X, CellSize.Width));
            var top = Math.Max(0, FloorDivide(-origin.Y, CellSize.Height));
            var right = Math.Min(CanvasSize.Width, CeilingDivide(ViewportSize.Width - origin.X, CellSize.Width));
            var bottom = Math.Min(CanvasSize.Height, CeilingDivide(ViewportSize.Height - origin.Y, CellSize.Height));

            if (right <= left || bottom <= top)
                return CellRect.Empty;

            return CellRect.FromEdges(left, top, right, bottom);
        }
    }

    /// <summary>The cell under <paramref name="point"/> (relative to the viewport), or null outside the canvas.</summary>
    public CellPoint? CellAt(Point point)
    {
        var origin = CanvasOrigin;
        var x = (int)Math.Floor((point.X - origin.X) / CellSize.Width);
        var y = (int)Math.Floor((point.Y - origin.Y) / CellSize.Height);
        var cell = new CellPoint(x, y);

        return CanvasSize.Contains(cell) ? cell : null;
    }

    /// <summary>The rectangle of <paramref name="cell"/>, relative to the viewport.</summary>
    public PixelRect GetCellBounds(CellPoint cell)
    {
        var origin = CanvasOrigin;
        return new PixelRect(origin.X + cell.X * CellSize.Width, origin.Y + cell.Y * CellSize.Height,
            CellSize.Width, CellSize.Height);
    }

    public CanvasViewport WithViewportSize(PixelSize viewportSize) =>
        new(CanvasSize, CellSize, viewportSize, Margin, ScrollOffset);

    public CanvasViewport WithCanvasSize(CanvasSize canvasSize) =>
        new(canvasSize, CellSize, ViewportSize, Margin, ScrollOffset);

    public CanvasViewport WithMargin(int margin) =>
        new(CanvasSize, CellSize, ViewportSize, margin, ScrollOffset);

    public CanvasViewport ScrolledTo(PixelPoint scrollOffset) =>
        new(CanvasSize, CellSize, ViewportSize, Margin, scrollOffset);

    public CanvasViewport ScrolledBy(int deltaX, int deltaY) =>
        ScrolledTo(new PixelPoint(ScrollOffset.X + deltaX, ScrollOffset.Y + deltaY));

    /// <summary>
    /// Changes the cell size (a zoom) while keeping the spot of the canvas under
    /// <paramref name="anchor"/> where it is, as far as scrolling allows.
    /// </summary>
    public CanvasViewport Zoomed(PixelSize cellSize, Point anchor)
    {
        // Where the anchor is on the canvas, in cells (fractional, and possibly off the canvas).
        var origin = CanvasOrigin;
        var cellX = (anchor.X - origin.X) / CellSize.Width;
        var cellY = (anchor.Y - origin.Y) / CellSize.Height;

        // The scroll offset that puts that same spot back under the anchor at the new size.
        var scrollX = (int)Math.Round(Margin - (anchor.X - cellX * cellSize.Width));
        var scrollY = (int)Math.Round(Margin - (anchor.Y - cellY * cellSize.Height));

        return new CanvasViewport(CanvasSize, cellSize, ViewportSize, Margin, new PixelPoint(scrollX, scrollY));
    }

    private int OriginAlong(int contentLength, int viewportLength, int canvasLength, int scrollOffset)
    {
        if (contentLength <= viewportLength)
            return (viewportLength - canvasLength) / 2;

        return Margin - scrollOffset;
    }

    private static int FloorDivide(int value, int divisor) => (int)Math.Floor((double)value / divisor);

    private static int CeilingDivide(int value, int divisor) => (int)Math.Ceiling((double)value / divisor);
}
