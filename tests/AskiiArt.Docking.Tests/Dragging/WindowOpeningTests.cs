// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Dragging;

public sealed class WindowOpeningTests
{
    private const double CascadeOffset = 24;

    private static readonly LayoutMeasurements _available = Measure(new LayoutSize(1200, 800));

    // Each side takes 8 of margin and 200 of column, so the canvas region runs from 208 to 992, and nothing can take a
    // new window but floating.
    private static readonly DockingLayout _bothSidesFull = new(
        [
            new SideLayout(SideEdge.Left, SideKind.Widgets, [new SideColumn(200, [.. "abcd".Select(Group)])]),
            new SideLayout(SideEdge.Right, SideKind.Widgets, [new SideColumn(200, [.. "efgh".Select(Group)])]),
        ],
        [],
        []);

    [Fact]
    public void ANewWindow_WithNowhereElse_FloatsCentredOnTheCanvasRegion_AtASidesWidthAndHalfTheHeight()
    {
        var opened = Open(_bothSidesFull);

        Assert.Equal(new FloatingWindow("n", new LayoutPoint(480, 200), 240, 400), opened.FloatingWindowNamed("n"));
    }

    [Fact]
    public void ANewWindow_ThatHasFloatedBefore_FloatsAtThatSize()
    {
        var layout = _bothSidesFull with { FloatingSizes = [new FloatingSize("n", 300, 250)] };

        var opened = Open(layout);

        Assert.Equal(new FloatingWindow("n", new LayoutPoint(450, 275), 300, 250), opened.FloatingWindowNamed("n"));
    }

    [Fact]
    public void ANewWindow_WhereOthersAlreadyFloat_MovesDownAndRightUntilItsClear()
    {
        var layout = LayoutEdits.FloatWindow(_bothSidesFull, "x", new LayoutPoint(480, 200), 240, 400);
        layout = LayoutEdits.FloatWindow(layout, "y", new LayoutPoint(504, 224), 240, 400);

        var opened = Open(layout);

        Assert.Equal(new LayoutPoint(528, 248), opened.FloatingWindowNamed("n")!.Position);
    }

    [Fact]
    public void AReopenedWindow_WithNowhereElse_Floats_AndIsNoLongerClosed()
    {
        var layout = _bothSidesFull with { ClosedWindows = [new ClosedWindow("n", SideEdge.Right, 0, 1, 200)] };

        var opened = Open(layout);

        Assert.NotNull(opened.FloatingWindowNamed("n"));
        Assert.Empty(opened.ClosedWindows);
    }

    [Fact]
    public void ANewWindow_WithASideThatHasRoom_DoesntFloat()
    {
        var opened = Open(Widgets(SideEdge.Right, Tabs("a")));

        Assert.Empty(opened.FloatingWindows);
        Assert.Equal(WindowKind.Widget, opened.KindOf("n"));
    }

    private static DockingLayout Open(DockingLayout layout) =>
        WindowOpening.Open(layout, "n", _available, Metrics, CascadeOffset);

    private static WindowGroup Group(char windowId) => new([windowId.ToString()]);
}
