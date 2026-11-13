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
    public void DroppedOnEitherSideOfTheBorderBetweenTwoColumns_ItBecomesAColumnBetweenThem()
    {
        foreach (var x in new[] { 755.0, 765.0 })
        {
            var dropped = Drag(_dock, "x").LayoutAt(new LayoutPoint(x, 175));

            Assert.Equal(new WindowPlace(SideEdge.Right, SideKind.Dock, 1, 0, 0), dropped.PlaceOf("x"));
            Assert.Equal(3, dropped.Side(SideEdge.Right)!.Columns.Count);
        }
    }

    [Fact]
    public void TheZoneOnTheBorderBetweenTwoColumns_RunsTheFullHeight_OverTheBandsAndTabStrips()
    {
        var drag = Drag(_dock, "x");

        foreach (var y in new[] { 5.0, 175, 340, 360, 695 })
        {
            var zone = drag.ZoneAt(new LayoutPoint(765, y))!;
            Assert.Equal(WindowDropKind.DockColumn, zone.Kind);
            Assert.Equal(1, zone.Column);
        }
    }

    [Fact]
    public void NearAWindowsOuterSide_PastTheEdgeStrip_ItBecomesATab()
    {
        var dropped = Drag(_dock, "x").LayoutAt(new LayoutPoint(960, 175));

        Assert.Equal(["a", "x"], dropped.GroupHolding("x")!.WindowIds);
    }

    [Fact]
    public void BetweenTwoStackedWindows_TheThinZoneIsJustAboveTheBorder()
    {
        var drag = Drag(_dock, "x");

        // "a" ends at 350, where the tab strip of "b" and "c" begins.
        Assert.Equal(WindowDropKind.Tab, drag.ZoneAt(new LayoutPoint(880, 320))!.Kind);
        Assert.Equal(WindowDropKind.DockStack, drag.ZoneAt(new LayoutPoint(880, 340))!.Kind);
        Assert.Equal(1, drag.ZoneAt(new LayoutPoint(880, 340))!.Position);
        Assert.Equal(WindowDropKind.Tab, drag.ZoneAt(new LayoutPoint(880, 355))!.Kind);
    }

    [Fact]
    public void OnlyTheColumnsFirstWindowHasABandAtTheTop_AndOnlyItsLastOneAtTheBottom()
    {
        var drag = Drag(_dock, "x");
        var belowTheLowerStrip = 350 + Metrics.TitleLength + 5;

        Assert.Equal(WindowDropKind.Tab, drag.ZoneAt(new LayoutPoint(880, belowTheLowerStrip))!.Kind);
        Assert.Equal(WindowDropKind.Tab, drag.ZoneAt(new LayoutPoint(880, 310))!.Kind);
        Assert.Equal(WindowDropKind.DockStack, drag.ZoneAt(new LayoutPoint(880, 660))!.Kind);
        Assert.Equal(2, drag.ZoneAt(new LayoutPoint(880, 660))!.Position);
    }

    [Fact]
    public void OverADockWindow_ItsContentsStay_AndATabShowsWhereTheWindowWouldGo()
    {
        var drag = Drag(_dock, "x");

        var middle = drag.PreviewAt(new LayoutPoint(880, 175));
        Assert.Null(middle.Marker);
        Assert.Equal(new IncomingTab("a", 1, new LayoutRect(760, 0, 240, 350)), middle.IncomingTab);
        Assert.Equal([["a"], ["b", "c"]], Groups(middle.Layout, 0));

        var strip = drag.PreviewAt(new LayoutPoint(800, 360));
        Assert.Equal(new IncomingTab("b", 0, new LayoutRect(760, 350, 240, 350)), strip.IncomingTab);
    }

    [Fact]
    public void OnABorderInADock_TheMarkerIsThin_ElsewhereAsDeepAsTheBands_AndTheSameDistanceFromEverything()
    {
        var drag = Drag(_dock, "x");
        var thin = Metrics.ThinDropMarkerThickness;
        var band = Metrics.DockBandLength;
        var padding = Metrics.DropMarkerPadding;

        LayoutRect? MarkerAt(double x, double y) => drag.PreviewAt(new LayoutPoint(x, y)).Marker;

        // Columns: at the app window's edge, between the two, and past the inner one.
        Assert.Equal(new LayoutRect(1000 - padding - thin, padding, thin, 700 - padding * 2), MarkerAt(995, 600));
        Assert.Equal(new LayoutRect(760 - thin / 2, padding, thin, 700 - padding * 2), MarkerAt(765, 175));
        Assert.Equal(new LayoutRect(520 - padding - thin, padding, thin, 700 - padding * 2), MarkerAt(530, 300));

        // Stacking: at the column's top, between "a" and the tabs, and at its bottom.
        Assert.Equal(new LayoutRect(760 + padding, padding, 240 - padding * 2, band), MarkerAt(880, 50));
        Assert.Equal(new LayoutRect(760 + padding, 350 - thin / 2, 240 - padding * 2, thin), MarkerAt(880, 340));
        Assert.Equal(new LayoutRect(760 + padding, 700 - padding - band, 240 - padding * 2, band), MarkerAt(880, 690));
    }

    [Fact]
    public void BetweenTwoColumns_BothGiveUpHalfTheRoom_AndTheDockKeepsItsWidth()
    {
        var solved = SolvedWithRoom(Drag(_dock, "x"), new LayoutPoint(765, 175));

        Assert.Equal(new LayoutRect(760 + 14, 0, 226, 350), solved.PlaceOfWindow("a")!.Value.Bounds);
        Assert.Equal(new LayoutRect(520, 0, 226, 700), solved.PlaceOfWindow("d")!.Value.Bounds);
    }

    [Fact]
    public void BetweenTwoColumnsAtTheirNarrowest_TheDockGrowsInstead()
    {
        var narrow = LayoutEdits.FloatWindow(
            Dock(SideEdge.Right, new SideColumn(160, [Tabs("a")]), new SideColumn(160, [Tabs("d")])),
            "x", new LayoutPoint(100, 100), 200, 200);

        var solved = SolvedWithRoom(Drag(narrow, "x"), new LayoutPoint(845, 300));

        Assert.Equal(new LayoutRect(840, 0, 160, 700), solved.PlaceOfWindow("a")!.Value.Bounds);
        Assert.Equal(new LayoutRect(840 - 28 - 160, 0, 160, 700), solved.PlaceOfWindow("d")!.Value.Bounds);
    }

    [Fact]
    public void AtADocksOuterEdge_TheWholeDockMovesIn()
    {
        var solved = SolvedWithRoom(Drag(_dock, "x"), new LayoutPoint(995, 600));

        Assert.Equal(1000 - 28 - 240, solved.PlaceOfWindow("a")!.Value.Bounds.X);
        Assert.Equal(1000 - 28 - 480, solved.PlaceOfWindow("d")!.Value.Bounds.X);
    }

    [Fact]
    public void AtTheTopOfAColumn_ItsFirstWindowIsPushedDown_AndAtTheBottom_ItsLastIsPushedUp()
    {
        var drag = Drag(_dock, "x");

        var top = SolvedWithRoom(drag, new LayoutPoint(880, 50));
        Assert.Equal(new LayoutRect(760, 56, 240, 294), top.PlaceOfWindow("a")!.Value.Bounds);
        Assert.Equal(new LayoutRect(760, 350, 240, 350), top.PlaceOfWindow("b")!.Value.Bounds);

        var bottom = SolvedWithRoom(drag, new LayoutPoint(880, 690));
        Assert.Equal(new LayoutRect(760, 0, 240, 350), bottom.PlaceOfWindow("a")!.Value.Bounds);
        Assert.Equal(new LayoutRect(760, 350, 240, 294), bottom.PlaceOfWindow("b")!.Value.Bounds);
    }

    [Fact]
    public void BetweenTwoStackedWindows_BothGiveUpHalfTheRoom()
    {
        var solved = SolvedWithRoom(Drag(_dock, "x"), new LayoutPoint(880, 340));

        Assert.Equal(new LayoutRect(760, 0, 240, 336), solved.PlaceOfWindow("a")!.Value.Bounds);
        Assert.Equal(new LayoutRect(760, 364, 240, 336), solved.PlaceOfWindow("b")!.Value.Bounds);
    }

    [Fact]
    public void OverATab_NoRoomIsMade()
    {
        var preview = Drag(_dock, "x").PreviewAt(new LayoutPoint(880, 175));

        Assert.Null(preview.Room);
        Assert.Null(preview.Marker);
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
    public void ALoneDockWindow_HasOnlyItsSidesEdgeStrip_WhichPutsItBack_AndFloatsAnywhereElseThere()
    {
        var alone = Dock(SideEdge.Right, Column(Tabs("a")));
        var drag = Drag(alone, "a");

        Assert.Equal([WindowDropKind.OwnPlace], drag.Zones.Where(zone => zone.Edge == SideEdge.Right).Select(zone => zone.Kind));
        Assert.Same(drag.Start, drag.LayoutAt(new LayoutPoint(995, 300)));
        Assert.NotNull(drag.LayoutAt(new LayoutPoint(770, 300)).FloatingWindowNamed("a"));
        Assert.NotNull(drag.LayoutAt(new LayoutPoint(900, 300)).FloatingWindowNamed("a"));
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

    // What the app shows while the pointer is there: the layout without the window, making room for the marker.
    private static SolvedLayout SolvedWithRoom(WindowDrag drag, LayoutPoint at)
    {
        var preview = drag.PreviewAt(at);
        return LayoutSolver.Solve(preview.Layout, Measure(_window) with { DropRoom = preview.Room }, Metrics);
    }

    private static WindowDrag Drag(DockingLayout layout, string windowId) =>
        new(layout, windowId, _heldAtTheTopLeft, Measure(_window), Metrics);

    private static List<IReadOnlyList<string>> Groups(DockingLayout layout, int column) =>
        [.. layout.Side(SideEdge.Right)!.Columns[column].Groups.Select(group => group.WindowIds)];
}
