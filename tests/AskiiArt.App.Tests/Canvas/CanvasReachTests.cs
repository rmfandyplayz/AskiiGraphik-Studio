// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;
using Avalonia;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>How far the canvas view can scroll, and how it keeps still while things move (ADR 0072).</summary>
public sealed class CanvasReachTests
{
    // 10 × 5 cells of 10 × 20 pixels: half as wide as tall, so the zone is 10 columns and 5 rows past each edge.
    private static readonly CanvasSize _canvas = new(10, 5);
    private static readonly PixelSize _cellSize = new(10, 20);
    private static readonly CellRect _zone = CellRect.FromEdges(-10, -5, 20, 10);

    [Fact]
    public void TheReach_IsTheCanvasAndItsZone_WhileTheSelectionStaysInside()
    {
        Assert.Equal(_zone, CanvasReach.Of(_canvas, null, 0.5));
        Assert.Equal(_zone, CanvasReach.Of(_canvas, Moved(new CellPoint(-8, 0)), 0.5));
    }

    [Fact]
    public void ASelectionPastTheZone_GrowsTheReach_WithAsMuchRoomAroundIt()
    {
        // The two cells end up at columns 22 and 23, rows 1 and 2.
        var reach = CanvasReach.Of(_canvas, Moved(new CellPoint(22, 1)), 0.5);

        Assert.Equal(CellRect.FromEdges(-10, -5, 34, 10), reach);
    }

    [Fact]
    public void TheView_ScrollsAsFarAsTheReach_PlusTheMargin()
    {
        // 300 × 100 pixels of zone, 316 × 116 with the margin, in a 200 × 100 view.
        var viewport = Create(new PixelSize(200, 100), reach: _zone);

        Assert.Equal(new PixelRect(-108, -108, 316, 316), viewport.ContentBounds);
        Assert.Equal(new PixelPoint(116, 216), viewport.MaximumScrollOffset);
        Assert.Equal(new PixelPoint(108, 108), viewport.ScrolledTo(default).CanvasOrigin);
    }

    [Fact]
    public void AReachThatFits_KeepsTheCanvasCentred_UnlessThatWouldCutItOff()
    {
        var centred = Create(new PixelSize(600, 600), reach: _zone);
        Assert.Equal(new PixelPoint(250, 250), centred.CanvasOrigin);

        // A reach 200 pixels to the right of the zone still fits, so the canvas moves left just enough to show it.
        var widened = Create(new PixelSize(600, 600), reach: CellRect.FromEdges(-10, -5, 40, 10));
        Assert.Equal(new PixelPoint(600 - 8 - 400, 250), widened.CanvasOrigin);
    }

    [Fact]
    public void HoldingTheView_KeepsTheCanvasWhereItIs_HoweverTheReachChanges()
    {
        var viewport = Create(new PixelSize(200, 100), reach: _zone).ScrolledTo(new PixelPoint(116, 216));
        var origin = viewport.CanvasOrigin;

        var held = viewport.HeldAt(origin).WithReach(new CellRect(0, 0, 10, 5));
        Assert.Equal(origin, held.CanvasOrigin);

        var released = held.Released();
        Assert.NotEqual(origin, released.CanvasOrigin);
        Assert.Equal(released.ScrolledTo(new PixelPoint(9999, 9999)).CanvasOrigin, released.CanvasOrigin);
    }

    [Fact]
    public void Zooming_KeepsTheSpotUnderTheAnchor_WithTheReach()
    {
        var viewport = Create(new PixelSize(200, 100), reach: _zone).ScrolledTo(new PixelPoint(50, 50));
        var anchor = new Point(100, 50);
        var before = viewport.CanvasPointAt(anchor);

        var zoomed = viewport.Zoomed(new PixelSize(20, 40), anchor);

        Assert.Equal(before.X, zoomed.CanvasPointAt(anchor).X, 6);
        Assert.Equal(before.Y, zoomed.CanvasPointAt(anchor).Y, 6);
    }

    [Fact]
    public void AGlide_StartsQuickly_SlowsDown_AndEndsWhereItWasGoing()
    {
        var glide = new ViewGlide(new PixelPoint(0, 0), new PixelPoint(100, -100), TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100));

        Assert.Equal(new PixelPoint(0, 0), glide.OriginAt(TimeSpan.FromSeconds(1)));
        Assert.True(glide.OriginAt(TimeSpan.FromSeconds(1.05)).X > 50);
        Assert.False(glide.IsOverAt(TimeSpan.FromSeconds(1.05)));
        Assert.Equal(new PixelPoint(100, -100), glide.OriginAt(TimeSpan.FromSeconds(1.1)));
        Assert.True(glide.IsOverAt(TimeSpan.FromSeconds(1.1)));
    }

    // The top-left 2 × 2 cells, moved as the Move tool would.
    private static SelectionMask Moved(CellPoint offset) =>
        SelectionMask.Rectangle(_canvas, new CellRect(0, 0, 2, 2))
            .MovedWithin(_canvas, offset, new CellRect(-100, -100, 200, 200));

    private static CanvasViewport Create(PixelSize viewportSize, CellRect reach) =>
        new(_canvas, _cellSize, viewportSize, 8, default, reach: reach);
}
