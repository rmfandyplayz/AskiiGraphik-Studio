// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Dragging;

public sealed class DockDragTests
{
    private static readonly LayoutSize _window = new(1000, 700);
    private static readonly LayoutPoint _heldAtTheTopLeft = new(0.1, 0.05);

    // On the right, from the app window's edge: column 0 from x 760 to 1000 holds "a" (y 0 to 350) over tabs "b" and "c"
    // (y 350 to 700), and column 1 from x 520 to 760 holds "d". That leaves room for one more column of 160 to 200.
    // "x" floats over the canvas.
    private static readonly DockingLayout _dock = LayoutEdits.FloatWindow(
        Dock(SideEdge.Right, Column(Tabs("a"), Tabs("b", "c")), Column(Tabs("d"))),
        "x",
        new LayoutPoint(100, 100),
        200,
        200);

    [Fact]
    public void DroppedInTheMiddleOfADockWindow_ItBecomesATab_AtTheEnd()
    {
        var dropped = Drag(_dock, "x").LayoutAt(new LayoutPoint(880, 175));

        Assert.Equal(["a", "x"], dropped.GroupHolding("x")!.WindowIds);
        Assert.Equal("x", dropped.GroupHolding("x")!.Selected);
    }

    [Fact]
    public void DroppedOnATabStrip_ItGoesBetweenTheTabsNearestThePointer()
    {
        // With the tabs' lengths unknown, "b" and "c" share the strip, so their middles are at 820 and 940.
        Assert.Equal(["x", "b", "c"], Drag(_dock, "x").LayoutAt(new LayoutPoint(800, 360)).GroupHolding("x")!.WindowIds);
        Assert.Equal(["b", "x", "c"], Drag(_dock, "x").LayoutAt(new LayoutPoint(900, 360)).GroupHolding("x")!.WindowIds);
    }

    [Fact]
    public void DroppedOnATabStrip_UsesTheTabsMeasuredLengths()
    {
        var measured = Measure(_window) with { TabLengths = new Dictionary<string, double> { ["b"] = 60, ["c"] = 60 } };
        var drag = new WindowDrag(_dock, "x", _heldAtTheTopLeft, measured, Metrics);

        Assert.Equal(["b", "c", "x"], drag.LayoutAt(new LayoutPoint(870, 360)).GroupHolding("x")!.WindowIds);
    }

    [Fact]
    public void DroppedOnATopOrBottomBand_ItStacksAboveOrBelow()
    {
        var above = Drag(_dock, "x").LayoutAt(new LayoutPoint(880, 50));
        var below = Drag(_dock, "x").LayoutAt(new LayoutPoint(880, 330));

        Assert.Equal([["x"], ["a"], ["b", "c"]], Groups(above, 0));
        Assert.Equal([["a"], ["x"], ["b", "c"]], Groups(below, 0));
    }

    [Fact]
    public void DroppedOnTheOuterBand_ItBecomesAColumnOutsideThatOne()
    {
        var dropped = Drag(_dock, "x").LayoutAt(new LayoutPoint(960, 175));

        Assert.Equal(new WindowPlace(SideEdge.Right, SideKind.Dock, 0, 0, 0), dropped.PlaceOf("x"));
        Assert.Equal(3, dropped.Side(SideEdge.Right)!.Columns.Count);
    }

    [Fact]
    public void DroppedOnTheInnerBand_ItBecomesAColumnInsideThatOne_KeepingItsWidthAsFarAsItFits()
    {
        var dropped = Drag(_dock, "x").LayoutAt(new LayoutPoint(530, 300));

        Assert.Equal(new WindowPlace(SideEdge.Right, SideKind.Dock, 2, 0, 0), dropped.PlaceOf("x"));
        Assert.Equal(200, dropped.Side(SideEdge.Right)!.Columns[2].Width);
    }

    [Fact]
    public void DroppedOnTheEdgeStripOfADock_ItBecomesAColumnAtTheVeryEdge()
    {
        var drag = Drag(_dock, "x");
        var atTheEdge = new LayoutPoint(_window.Width - 5, 600);

        Assert.Equal(WindowDropKind.SideEdge, drag.ZoneAt(atTheEdge)!.Kind);
        Assert.Equal(new WindowPlace(SideEdge.Right, SideKind.Dock, 0, 0, 0), drag.LayoutAt(atTheEdge).PlaceOf("x"));
    }

    [Fact]
    public void DroppedOnTheEdgeStripOfAWidgetSide_ItMakesItADock_WithTheWidgetsBesideIt()
    {
        var widgets = LayoutEdits.FloatWindow(Widgets(SideEdge.Right, Tabs("a"), Tabs("b")), "x", new LayoutPoint(100, 100), 200, 200);

        var dropped = Drag(widgets, "x").LayoutAt(new LayoutPoint(_window.Width - 5, 300));

        var side = dropped.Side(SideEdge.Right)!;
        Assert.Equal(SideKind.Dock, side.Kind);
        Assert.Equal([["x"], ["a", "b"]], side.Columns.Select(column => column.Groups.SelectMany(group => group.WindowIds)));
    }

    [Fact]
    public void AWidgetSideHoldingFour_StillTakesAColumnAtItsEdge()
    {
        var full = LayoutEdits.FloatWindow(
            Widgets(SideEdge.Right, Tabs("a"), Tabs("b"), Tabs("c"), Tabs("d")), "x", new LayoutPoint(100, 100), 200, 200);

        var dropped = Drag(full, "x").LayoutAt(new LayoutPoint(_window.Width - 5, 300));

        Assert.Equal(WindowKind.Dock, dropped.KindOf("x"));
    }

    [Fact]
    public void WithNoRoomForAColumn_NoZoneMakesOne()
    {
        var narrow = new WindowDrag(_dock, "x", _heldAtTheTopLeft, Measure(new LayoutSize(800, 700)), Metrics);

        Assert.DoesNotContain(narrow.Zones, zone => zone.Kind is WindowDropKind.SideEdge or WindowDropKind.DockColumn);
    }

    [Fact]
    public void ATabDroppedInTheMiddleOfItsOwnWindow_StaysWhereItWas()
    {
        var drag = Drag(_dock, "b");

        Assert.Same(drag.Start, drag.LayoutAt(new LayoutPoint(880, 550)));
    }

    [Fact]
    public void ATabDroppedElsewhereOnItsOwnStrip_MovesAmongTheTabs()
    {
        // Without "b", "c" has the strip to itself, so its middle is at 880.
        var dropped = Drag(_dock, "b").LayoutAt(new LayoutPoint(950, 360));

        Assert.Equal(["c", "b"], dropped.GroupHolding("b")!.WindowIds);
    }

    [Fact]
    public void AWindowDroppedBackWhereItWasInItsColumn_ChangesNothing()
    {
        // Without "a", "b" and "c" fill the column, and the top band of their window is where "a" was.
        var drag = Drag(_dock, "a");

        Assert.Same(drag.Start, drag.LayoutAt(new LayoutPoint(880, 50)));
    }

    [Fact]
    public void AColumnOfOneDroppedBackWhereItWas_ChangesNothing()
    {
        // Without "d", the inner band of the window beside it is where its column was.
        var drag = Drag(_dock, "d");

        Assert.Same(drag.Start, drag.LayoutAt(new LayoutPoint(770, 175)));
    }

    [Fact]
    public void ALoneDockWindow_BecomesAWidget_JustInsideItsInnerEdge()
    {
        var alone = Dock(SideEdge.Right, Column(Tabs("a")));
        var drag = Drag(alone, "a");

        var widget = drag.LayoutAt(new LayoutPoint(770, 300));

        Assert.Equal(WindowKind.Widget, widget.KindOf("a"));
        Assert.Same(drag.Start, drag.LayoutAt(new LayoutPoint(900, 300)));
    }

    [Fact]
    public void AnyDockDropOfAnyWindow_KeepsTheRules()
    {
        for (var x = 0; x < _window.Width; x += 10)
        {
            for (var y = 0; y < _window.Height; y += 25)
            {
                foreach (var windowId in _dock.OpenWindowIds())
                {
                    var dropped = Drag(_dock, windowId).LayoutAt(new LayoutPoint(x, y));

                    Assert.Equal(1, dropped.OpenWindowIds().Count(id => id == windowId));
                    Assert.Equal(Text(dropped), Text(LayoutRepair.Repair(dropped, Catalog with { WindowIds = ["a", "b", "c", "d", "x"], DefaultToolbars = [] })));
                }
            }
        }
    }

    private static WindowDrag Drag(DockingLayout layout, string windowId) =>
        new(layout, windowId, _heldAtTheTopLeft, Measure(_window), Metrics);

    private static List<IReadOnlyList<string>> Groups(DockingLayout layout, int column) =>
        [.. layout.Side(SideEdge.Right)!.Columns[column].Groups.Select(group => group.WindowIds)];
}
