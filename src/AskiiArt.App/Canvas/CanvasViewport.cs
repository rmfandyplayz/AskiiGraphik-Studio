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
/// The scrollable content is <see cref="Reach"/>, the cells the view can go to, plus <see cref="Margin"/> of empty space
/// on every side, and whatever <see cref="Held"/> keeps in reach. Along an axis where that content fits in the usable area
/// (the viewport minus <see cref="Insets"/>), the canvas is centred in that area, as far as the content still fits, and
/// can't scroll. Otherwise it scrolls far enough that every cell of the reach can reach the usable area, and passes
/// under the insets on the way (ADRs 0035, 0072).
/// </remarks>
internal sealed record CanvasViewport
{
    public CanvasViewport(CanvasSize canvasSize, PixelSize cellSize, PixelSize viewportSize, int margin,
        PixelPoint scrollOffset, CanvasInsets insets = default, CellRect? reach = null, PixelRect held = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cellSize.Width, 1, nameof(cellSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(cellSize.Height, 1, nameof(cellSize));
        ArgumentOutOfRangeException.ThrowIfNegative(margin);

        CanvasSize = canvasSize;
        CellSize = cellSize;
        ViewportSize = new PixelSize(Math.Max(0, viewportSize.Width), Math.Max(0, viewportSize.Height));
        Margin = margin;
        Insets = insets;
        Reach = reach ?? canvasSize.Bounds;
        Held = held;
        ContentBounds = ContentBoundsOf(Reach, cellSize, margin, held);

        var maximum = MaximumScrollOffset;
        ScrollOffset = new PixelPoint(Math.Clamp(scrollOffset.X, 0, maximum.X),
            Math.Clamp(scrollOffset.Y, 0, maximum.Y));
    }

    public CanvasSize CanvasSize { get; }

    public PixelSize CellSize { get; }

    public PixelSize ViewportSize { get; }

    /// <summary>The empty space around <see cref="Reach"/> when it's scrolled to an edge.</summary>
    public int Margin { get; }

    /// <summary>
    /// The cells the view can scroll to: the canvas, the zone around it, and a selection past that with room around it
    /// (ADR 0072). The canvas alone unless given.
    /// </summary>
    public CellRect Reach { get; }

    /// <summary>
    /// Device pixels, from the canvas's top-left corner, that stay in reach whatever <see cref="Reach"/> is, so the view
    /// can stay where it is while something moves; empty when nothing is held.
    /// </summary>
    public PixelRect Held { get; }

    /// <summary>The scrollable content, in device pixels from the canvas's top-left corner.</summary>
    public PixelRect ContentBounds { get; }

    /// <summary>The parts of the viewport that windows and toolbars cover.</summary>
    public CanvasInsets Insets { get; }

    /// <summary>The viewport without its <see cref="Insets"/>: what the canvas is centred in and scrolled into.</summary>
    public PixelSize UsableSize => new(
        Math.Max(0, ViewportSize.Width - Insets.Left - Insets.Right),
        Math.Max(0, ViewportSize.Height - Insets.Top - Insets.Bottom));

    /// <summary>The middle of the usable area, relative to the viewport. Zooming without a pointer zooms around it.</summary>
    public Point UsableCentre => new(Insets.Left + UsableSize.Width / 2.0, Insets.Top + UsableSize.Height / 2.0);

    /// <summary>How far the content is scrolled, between zero and <see cref="MaximumScrollOffset"/>.</summary>
    public PixelPoint ScrollOffset { get; }

    public PixelSize CanvasPixelSize => new(CanvasSize.Width * CellSize.Width, CanvasSize.Height * CellSize.Height);

    /// <summary>The scrollable size: the reach plus its margin on every side, and what's held.</summary>
    public PixelSize ContentSize => ContentBounds.Size;

    public PixelPoint MaximumScrollOffset =>
        new(Math.Max(0, ContentSize.Width - UsableSize.Width), Math.Max(0, ContentSize.Height - UsableSize.Height));

    /// <summary>The canvas's top-left corner, relative to the viewport's.</summary>
    public PixelPoint CanvasOrigin => new(
        OriginAlong(ContentBounds.X, ContentBounds.Width, UsableSize.Width, CanvasPixelSize.Width, ScrollOffset.X,
            Insets.Left),
        OriginAlong(ContentBounds.Y, ContentBounds.Height, UsableSize.Height, CanvasPixelSize.Height, ScrollOffset.Y,
            Insets.Top));

    /// <summary>The usable area, in device pixels from the canvas's top-left corner.</summary>
    public PixelRect UsableOnCanvas
    {
        get
        {
            var origin = CanvasOrigin;
            return new PixelRect(Insets.Left - origin.X, Insets.Top - origin.Y, UsableSize.Width, UsableSize.Height);
        }
    }

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

    /// <summary>
    /// The cells at least partly inside the viewport, also those past the canvas's edges, where a moved selection's
    /// outline can show (ADR 0071); empty if none are.
    /// </summary>
    public CellRect VisibleCellsAroundCanvas
    {
        get
        {
            var origin = CanvasOrigin;
            var left = FloorDivide(-origin.X, CellSize.Width);
            var top = FloorDivide(-origin.Y, CellSize.Height);
            var right = CeilingDivide(ViewportSize.Width - origin.X, CellSize.Width);
            var bottom = CeilingDivide(ViewportSize.Height - origin.Y, CellSize.Height);

            if (right <= left || bottom <= top)
                return CellRect.Empty;

            return CellRect.FromEdges(left, top, right, bottom);
        }
    }

    /// <summary>
    /// The spot of the canvas under <paramref name="point"/> (relative to the viewport), in cells from its top-left
    /// corner, with the fraction of a cell. Outside the canvas too.
    /// </summary>
    public CanvasPoint CanvasPointAt(Point point)
    {
        var origin = CanvasOrigin;
        return new CanvasPoint((point.X - origin.X) / CellSize.Width, (point.Y - origin.Y) / CellSize.Height);
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
        new(CanvasSize, CellSize, viewportSize, Margin, ScrollOffset, Insets, Reach, Held);

    public CanvasViewport WithCanvasSize(CanvasSize canvasSize) =>
        new(canvasSize, CellSize, ViewportSize, Margin, ScrollOffset, Insets, Reach, Held);

    public CanvasViewport WithMargin(int margin) =>
        new(CanvasSize, CellSize, ViewportSize, margin, ScrollOffset, Insets, Reach, Held);

    public CanvasViewport WithInsets(CanvasInsets insets) =>
        new(CanvasSize, CellSize, ViewportSize, Margin, ScrollOffset, insets, Reach, Held);

    public CanvasViewport ScrolledTo(PixelPoint scrollOffset) =>
        new(CanvasSize, CellSize, ViewportSize, Margin, scrollOffset, Insets, Reach, Held);

    /// <summary>The same view with <paramref name="reach"/>, keeping the canvas where it is as far as the content allows.</summary>
    public CanvasViewport WithReach(CellRect reach) => Placed(reach, Held, CanvasOrigin);

    /// <summary>
    /// The view with the canvas's top-left corner at <paramref name="canvasOrigin"/>, holding the usable area there in
    /// reach, so it goes there whatever the reach is.
    /// </summary>
    public CanvasViewport HeldAt(PixelPoint canvasOrigin)
    {
        var usable = new PixelRect(Insets.Left - canvasOrigin.X, Insets.Top - canvasOrigin.Y, UsableSize.Width,
            UsableSize.Height);
        return Placed(Reach, usable, canvasOrigin);
    }

    /// <summary>The view holding nothing, keeping the canvas where it is as far as the reach then allows.</summary>
    public CanvasViewport Released() => Placed(Reach, default, CanvasOrigin);

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
        var content = ContentBoundsOf(Reach, cellSize, Margin, default);
        var scrollX = (int)Math.Round(Insets.Left - content.X - (anchor.X - cellX * cellSize.Width));
        var scrollY = (int)Math.Round(Insets.Top - content.Y - (anchor.Y - cellY * cellSize.Height));

        return new CanvasViewport(CanvasSize, cellSize, ViewportSize, Margin, new PixelPoint(scrollX, scrollY), Insets,
            Reach);
    }

    // A viewport with the content of reach and held, scrolled so the canvas's corner is at canvasOrigin if it can be.
    private CanvasViewport Placed(CellRect reach, PixelRect held, PixelPoint canvasOrigin)
    {
        var content = ContentBoundsOf(reach, CellSize, Margin, held);
        var scroll = new PixelPoint(Insets.Left - content.X - canvasOrigin.X, Insets.Top - content.Y - canvasOrigin.Y);
        return new CanvasViewport(CanvasSize, CellSize, ViewportSize, Margin, scroll, Insets, reach, held);
    }

    private static PixelRect ContentBoundsOf(CellRect reach, PixelSize cellSize, int margin, PixelRect held)
    {
        var content = new PixelRect(reach.Left * cellSize.Width - margin, reach.Top * cellSize.Height - margin,
            reach.Width * cellSize.Width + 2 * margin, reach.Height * cellSize.Height + 2 * margin);
        if (held.Width <= 0 || held.Height <= 0)
            return content;

        return content.Union(held);
    }

    // Where the canvas starts along one axis. Content that fits keeps the canvas in the middle, moved only as far as the
    // content needs to stay in the usable area; content that doesn't fit scrolls.
    private static int OriginAlong(int contentStart, int contentLength, int usableLength, int canvasLength,
        int scrollOffset, int insetBefore)
    {
        if (contentLength > usableLength)
            return insetBefore - contentStart - scrollOffset;

        var centred = insetBefore + (usableLength - canvasLength) / 2;
        return Math.Clamp(centred, insetBefore - contentStart,
            insetBefore + usableLength - (contentStart + contentLength));
    }

    private static int FloorDivide(int value, int divisor) => (int)Math.Floor((double)value / divisor);

    private static int CeilingDivide(int value, int divisor) => (int)Math.Ceiling((double)value / divisor);
}
