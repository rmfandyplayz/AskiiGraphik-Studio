// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.Core.Documents;
using Avalonia;

namespace AskiiArt.App.Tests.Canvas;

public sealed class ZoomGlideTests
{
    private static readonly TimeSpan _start = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _duration = TimeSpan.FromMilliseconds(200);

    // 100 x 50 cells of 10 x 20 pixels in a 400 x 300 view, with cell (20, 10) in the middle.
    private static readonly CanvasViewport _from = View(new PixelSize(10, 20), new CanvasPoint(20, 10));

    // The same canvas zoomed out to cells of 5 x 10, with cell (60, 30) in the middle.
    private static readonly CanvasViewport _to = View(new PixelSize(5, 10), new CanvasPoint(60, 30));

    private static readonly ZoomGlide _glide = new(_from, _start, _duration);

    [Fact]
    public void AtTheStart_TheDrawingShowsTheViewAsItWas()
    {
        var transform = _glide.TransformAt(_start, _to);

        foreach (var spot in new[] { new CanvasPoint(0, 0), new CanvasPoint(20, 10), new CanvasPoint(73.5, 41) })
            AssertClose(OnScreen(_from, spot), transform.Apply(OnScreen(_to, spot)));
    }

    [Fact]
    public void OnceOver_TheDrawingIsTheViewAsItEnds()
    {
        Assert.Equal(ViewTransform.Identity, _glide.TransformAt(_start + _duration, _to));
        Assert.Equal(ViewTransform.Identity, _glide.TransformAt(_start + _duration * 3, _to));
        Assert.Equal((_to, ViewTransform.Identity), _glide.FrameAt(_start + _duration, _to));
    }

    // Halfway through the time, the ease-out has gone 7/8 of the way: the cells are 10 x 20 times (1/2)^(7/8) and the
    // spot in the middle of the view is 7/8 of the way from (20, 10) to (60, 30).
    [Fact]
    public void PartWay_TheCellSizeAndTheSpotInTheMiddleAreBetweenTheStartsAndTheEnds()
    {
        var transform = _glide.TransformAt(_start + _duration / 2, _to);
        var shrink = Math.Pow(0.5, 7.0 / 8);

        Assert.Equal(10 * shrink / 5, transform.ScaleX, 1e-9);
        Assert.Equal(20 * shrink / 10, transform.ScaleY, 1e-9);
        var middle = transform.Apply(OnScreen(_to, new CanvasPoint(55, 27.5)));
        AssertClose(_to.UsableCentre, middle);
    }

    [Fact]
    public void AGlideThatKeepsTheCellSize_ShiftsByWholePixelsOnly()
    {
        var to = View(new PixelSize(10, 20), new CanvasPoint(50, 30));
        var glide = new ZoomGlide(_from, _start, _duration);

        var transform = glide.TransformAt(_start + _duration / 3, to);

        Assert.False(transform.IsScaled);
        Assert.Equal(Math.Round(transform.OffsetX), transform.OffsetX);
        Assert.Equal(Math.Round(transform.OffsetY), transform.OffsetY);
    }

    // The frame's own view is drawn, then scaled and shifted: it puts every spot where the end's view would, and covers
    // the whole screen.
    [Fact]
    public void AFrame_DrawsAViewThatCoversTheScreen_AndPutsEachSpotWhereTheGlideHasIt()
    {
        foreach (var now in new[] { _start, _start + _duration / 4, _start + _duration * 0.9 })
        {
            var expected = _glide.TransformAt(now, _to);
            var (drawn, transform) = _glide.FrameAt(now, _to);

            Assert.Equal(_to.CellSize, drawn.CellSize);
            foreach (var spot in new[] { new CanvasPoint(0, 0), new CanvasPoint(60, 30), new CanvasPoint(99, 49) })
                AssertClose(expected.Apply(OnScreen(_to, spot)), transform.Apply(OnScreen(drawn, spot)));

            var topLeft = transform.Apply(default);
            var bottomRight = transform.Apply(new Point(drawn.ViewportSize.Width, drawn.ViewportSize.Height));
            Assert.True(topLeft.X <= 0 && topLeft.Y <= 0, $"{topLeft} at {now}");
            Assert.True(bottomRight.X >= _to.ViewportSize.Width && bottomRight.Y >= _to.ViewportSize.Height,
                $"{bottomRight} at {now}");
        }
    }

    private static CanvasViewport View(PixelSize cellSize, CanvasPoint middle)
    {
        var view = new CanvasViewport(new CanvasSize(100, 50), cellSize, new PixelSize(400, 300), 0, default);
        var origin = new PixelPoint((int)Math.Round(200 - middle.X * cellSize.Width),
            (int)Math.Round(150 - middle.Y * cellSize.Height));
        return view.HeldAt(origin).Released();
    }

    private static Point OnScreen(CanvasViewport view, CanvasPoint spot) =>
        new(view.CanvasOrigin.X + spot.X * view.CellSize.Width, view.CanvasOrigin.Y + spot.Y * view.CellSize.Height);

    private static void AssertClose(Point expected, Point actual)
    {
        Assert.Equal(expected.X, actual.X, 1e-6);
        Assert.Equal(expected.Y, actual.Y, 1e-6);
    }
}
