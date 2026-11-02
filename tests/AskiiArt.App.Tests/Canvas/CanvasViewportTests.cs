// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.Core.Documents;
using Avalonia;

namespace AskiiArt.App.Tests.Canvas;

public sealed class CanvasViewportTests
{
    private static PixelSize CellSize => new(10, 20);

    [Fact]
    public void CanvasSmallerThanTheViewport_IsCentredAndCannotScroll()
    {
        // 10 x 5 cells of 10 x 20 = 100 x 100 pixels, plus a margin of 8 on each side.
        var viewport = Create(new CanvasSize(10, 5), new PixelSize(300, 200), scrollOffset: new PixelPoint(50, 50));

        Assert.Equal(new PixelPoint(100, 50), viewport.CanvasOrigin);
        Assert.Equal(new PixelPoint(0, 0), viewport.ScrollOffset);
        Assert.Equal(new PixelPoint(0, 0), viewport.MaximumScrollOffset);
        Assert.Equal(new CellRect(0, 0, 10, 5), viewport.VisibleCells);
    }

    [Fact]
    public void CanvasLargerThanTheViewport_ScrollsWithinItsMargin()
    {
        // 1000 x 2000 pixels of canvas, 1016 x 2016 with the margin.
        var viewport = Create(new CanvasSize(100, 100), new PixelSize(300, 200));

        Assert.Equal(new PixelPoint(8, 8), viewport.CanvasOrigin);
        Assert.Equal(new PixelPoint(716, 1816), viewport.MaximumScrollOffset);
        Assert.Equal(new PixelPoint(8 - 716, 8 - 1816), viewport.ScrolledTo(new PixelPoint(9999, 9999)).CanvasOrigin);
        Assert.Equal(new PixelPoint(0, 0), viewport.ScrolledBy(-5, -5).ScrollOffset);
    }

    [Fact]
    public void CanvasThatFitsBetweenTheInsets_IsCentredBetweenThem_AndCannotScroll()
    {
        // 100 x 100 pixels of canvas; 200 x 150 pixels left between the insets.
        var viewport = new CanvasViewport(new CanvasSize(10, 5), CellSize, new PixelSize(300, 200), 8, default,
            new CanvasInsets(20, 10, 80, 40));

        Assert.Equal(new PixelSize(200, 150), viewport.UsableSize);
        Assert.Equal(new PixelPoint(70, 35), viewport.CanvasOrigin);
        Assert.Equal(new PixelPoint(0, 0), viewport.MaximumScrollOffset);
        Assert.Equal(new Point(120, 85), viewport.UsableCentre);
    }

    [Fact]
    public void CanvasLargerThanTheInsetsLeaveFree_CanBringEveryEdgeClearOfThem()
    {
        var insets = new CanvasInsets(20, 10, 80, 40);
        var viewport = new CanvasViewport(new CanvasSize(100, 100), CellSize, new PixelSize(300, 200), 8, default, insets);

        // Scrolled to the start, the canvas starts just past the left and top insets.
        Assert.Equal(new PixelPoint(28, 18), viewport.CanvasOrigin);

        // Scrolled to the end, it stops just before the right and bottom insets.
        var end = viewport.ScrolledTo(viewport.MaximumScrollOffset);
        Assert.Equal(300 - 80 - 8, end.CanvasBounds.Right);
        Assert.Equal(200 - 40 - 8, end.CanvasBounds.Bottom);

        // On the way it passes under them, so cells under the insets still get drawn.
        Assert.Equal(0, viewport.ScrolledTo(new PixelPoint(28, 18)).VisibleCells.Left);
    }

    [Fact]
    public void ZoomingWithInsets_KeepsTheAnchorSpotInPlace()
    {
        var viewport = new CanvasViewport(new CanvasSize(100, 100), CellSize, new PixelSize(300, 200), 8,
            new PixelPoint(100, 100), new CanvasInsets(20, 10, 80, 40));
        var anchor = viewport.UsableCentre;
        var column = (anchor.X - viewport.CanvasOrigin.X) / viewport.CellSize.Width;

        var zoomed = viewport.Zoomed(new PixelSize(20, 40), anchor);

        Assert.Equal(anchor.X, zoomed.CanvasOrigin.X + column * zoomed.CellSize.Width, tolerance: 1);
    }

    [Fact]
    public void WithInsets_KeepsThemThroughOtherChanges()
    {
        var insets = new CanvasInsets(1, 2, 3, 4);
        var viewport = Create(new CanvasSize(10, 5), new PixelSize(300, 200)).WithInsets(insets);

        Assert.Equal(insets, viewport.WithMargin(0).WithViewportSize(new PixelSize(50, 50)).WithCanvasSize(new CanvasSize(3, 3)).Insets);
        Assert.Equal(insets, viewport.ScrolledBy(1, 1).Zoomed(new PixelSize(20, 40), default).Insets);
    }

    [Fact]
    public void VisibleCells_IncludesPartlyVisibleCells()
    {
        // Scrolled so the canvas starts 15 pixels left of and 25 pixels above the viewport.
        var viewport = Create(new CanvasSize(100, 100), new PixelSize(300, 200), scrollOffset: new PixelPoint(23, 33));

        Assert.Equal(new PixelPoint(-15, -25), viewport.CanvasOrigin);
        // Columns 1 (half hidden) to 31 (half shown); rows 1 to 11.
        Assert.Equal(CellRect.FromEdges(1, 1, 32, 12), viewport.VisibleCells);
    }

    [Fact]
    public void VisibleCells_EmptyViewport_IsEmpty()
    {
        var viewport = Create(new CanvasSize(100, 100), new PixelSize(0, 0));

        Assert.True(viewport.VisibleCells.IsEmpty);
    }

    [Theory]
    [InlineData(100, 50, 0, 0)] // the canvas's top-left pixel
    [InlineData(109.9, 69.9, 0, 0)]
    [InlineData(110, 70, 1, 1)]
    [InlineData(199.5, 149.5, 9, 4)] // the bottom-right pixel
    public void CellAt_InsideTheCanvas_FindsTheCell(double x, double y, int expectedX, int expectedY)
    {
        var viewport = Create(new CanvasSize(10, 5), new PixelSize(300, 200));

        Assert.Equal(new CellPoint(expectedX, expectedY), viewport.CellAt(new Point(x, y)));
    }

    [Theory]
    [InlineData(99.5, 60)]
    [InlineData(200, 60)]
    [InlineData(150, 49)]
    [InlineData(150, 150)]
    public void CellAt_OutsideTheCanvas_IsNull(double x, double y)
    {
        var viewport = Create(new CanvasSize(10, 5), new PixelSize(300, 200));

        Assert.Null(viewport.CellAt(new Point(x, y)));
    }

    [Fact]
    public void GetCellBounds_IsTheCellsRectangleInTheViewport()
    {
        var viewport = Create(new CanvasSize(10, 5), new PixelSize(300, 200));

        Assert.Equal(new PixelRect(130, 90, 10, 20), viewport.GetCellBounds(new CellPoint(3, 2)));
    }

    [Fact]
    public void Zoomed_KeepsTheCellUnderTheAnchorInPlace()
    {
        var viewport = Create(new CanvasSize(200, 200), new PixelSize(400, 300), scrollOffset: new PixelPoint(500, 700));
        var anchor = new Point(123, 77);
        var cellBefore = viewport.CellAt(anchor);

        var zoomed = viewport.Zoomed(new PixelSize(20, 40), anchor);

        Assert.Equal(new PixelSize(20, 40), zoomed.CellSize);
        Assert.Equal(cellBefore, zoomed.CellAt(anchor));
        // Same spot within the cell too: the anchor was 3/10 into its cell's width before, so it
        // is 6/20 in now.
        var boundsAfter = zoomed.GetCellBounds(cellBefore!.Value);
        Assert.Equal(viewport.GetCellBounds(cellBefore.Value).X * 1.0 - anchor.X, (boundsAfter.X - anchor.X) / 2.0, 0.5);
    }

    [Fact]
    public void Zoomed_OutUntilTheCanvasFits_CentresIt()
    {
        var viewport = Create(new CanvasSize(20, 10), new PixelSize(300, 200), scrollOffset: new PixelPoint(40, 40));

        var zoomed = viewport.Zoomed(new PixelSize(5, 10), new Point(10, 10));

        Assert.Equal(new PixelPoint(100, 50), zoomed.CanvasOrigin);
    }

    [Fact]
    public void WithViewportSize_ClampsTheScrollOffset()
    {
        var viewport = Create(new CanvasSize(100, 100), new PixelSize(300, 200)).ScrolledTo(new PixelPoint(716, 1816));

        var larger = viewport.WithViewportSize(new PixelSize(1016, 1000));

        Assert.Equal(new PixelPoint(0, 1016), larger.ScrollOffset);
    }

    [Fact]
    public void Constructor_CellSmallerThanOnePixel_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CanvasViewport(new CanvasSize(1, 1), new PixelSize(0, 1), new PixelSize(10, 10), 0, default));
    }

    private static CanvasViewport Create(CanvasSize canvasSize, PixelSize viewportSize,
        PixelPoint scrollOffset = default) =>
        new(canvasSize, CellSize, viewportSize, margin: 8, scrollOffset);
}
