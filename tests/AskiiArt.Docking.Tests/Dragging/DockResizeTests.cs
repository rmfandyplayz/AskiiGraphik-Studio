// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Dragging;

public sealed class DockResizeTests
{
    private const double Tolerance = 0.001;

    private static readonly LayoutSize _window = new(1000, 700);

    // On the right: column 0 from x 760 to 1000 holds "a" (y 0 to 350) over "b" (y 350 to 700), and column 1 from x 520
    // to 760 holds "d".
    private static readonly DockingLayout _dock = Dock(SideEdge.Right, Column(Tabs("a"), Tabs("b")), Column(Tabs("d")));

    [Fact]
    public void ADock_HasAHandleOnEveryBorder()
    {
        Assert.Equal(
            [
                new StackResizeHandleId(SideEdge.Right, 0, null),
                new StackResizeHandleId(SideEdge.Right, 0, 0),
                new StackResizeHandleId(SideEdge.Right, 1, null),
            ],
            StackResizeHandles.IdsIn(_dock));
    }

    [Fact]
    public void ADocksHandles_StraddleItsBorders()
    {
        var handles = StackResizeHandles.For(LayoutSolver.Solve(_dock, Measure(_window), Metrics), Metrics)
            .ToDictionary(handle => handle.Id, handle => handle.Bounds);

        Assert.Equal(new LayoutRect(757, 0, 6, 700), handles[new StackResizeHandleId(SideEdge.Right, 0, null)]);
        Assert.Equal(new LayoutRect(760, 347, 240, 6), handles[new StackResizeHandleId(SideEdge.Right, 0, 0)]);
        Assert.Equal(new LayoutRect(517, 0, 6, 700), handles[new StackResizeHandleId(SideEdge.Right, 1, null)]);
    }

    [Fact]
    public void DraggingTheBorderBetweenColumns_ResizesOnlyTheOuterOne()
    {
        var resize = new ColumnWidthResize(_dock, SideEdge.Right, 0, Measure(_window), Metrics);

        var after = resize.LayoutAfter(new LayoutPoint(-50, 0));

        Assert.Equal([290, 240], after.Side(SideEdge.Right)!.Columns.Select(column => column.Width));
    }

    [Fact]
    public void AColumn_GrowsOnlyUntilTheCanvasRegionIsAtItsMinimum()
    {
        var resize = new ColumnWidthResize(_dock, SideEdge.Right, 1, Measure(_window), Metrics);

        var after = resize.LayoutAfter(new LayoutPoint(-1000, 0));

        // 1000 wide, less the canvas region's 320 and the other column's 240.
        Assert.Equal(440, after.Side(SideEdge.Right)!.Columns[1].Width, Tolerance);
    }

    [Fact]
    public void AColumn_ShrinksOnlyToTheNarrowestAWindowCanBe()
    {
        var resize = new ColumnWidthResize(_dock, SideEdge.Right, 1, Measure(_window), Metrics);

        Assert.Equal(Metrics.MinimumWindowWidth, resize.LayoutAfter(new LayoutPoint(1000, 0)).Side(SideEdge.Right)!.Columns[1].Width);
    }

    [Fact]
    public void ResizingAColumnThatIsntThere_IsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ColumnWidthResize(_dock, SideEdge.Right, 2, Measure(_window), Metrics));
    }

    [Fact]
    public void DraggingTheBorderBetweenTwoDockWindows_ResizesOnlyThoseTwo()
    {
        var resize = new StackGapResize(_dock, SideEdge.Right, 0, 0, Measure(_window), Metrics);

        var after = LayoutSolver.Solve(resize.LayoutAfter(new LayoutPoint(0, 50)), Measure(_window), Metrics);

        Assert.Equal(400, after.PlaceOfWindow("a")!.Value.Bounds.Height, Tolerance);
        Assert.Equal(300, after.PlaceOfWindow("b")!.Value.Bounds.Height, Tolerance);
        Assert.Equal(700, after.PlaceOfWindow("d")!.Value.Bounds.Height, Tolerance);
    }

    [Fact]
    public void ADockWindowSqueezedBelowItsMinimum_SnapsToItsTabStrip()
    {
        var resize = new StackGapResize(_dock, SideEdge.Right, 0, 0, Measure(_window), Metrics);

        var after = resize.LayoutAfter(new LayoutPoint(0, 400));

        Assert.True(after.GroupHolding("b")!.IsTitleOnly);
    }
}
