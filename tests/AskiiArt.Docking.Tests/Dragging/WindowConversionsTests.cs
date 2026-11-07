// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Dragging;

public sealed class WindowConversionsTests
{
    private static readonly LayoutMeasurements _wide = Measure(new LayoutSize(1000, 800));

    [Fact]
    public void TurningAWidgetIntoADock_ConvertsItsWholeSide()
    {
        var widgets = Widgets(SideEdge.Left, new WindowGroup(["a"], Share: 2), new WindowGroup(["b"]));

        Assert.True(WindowConversions.CanTurnIntoDock(widgets, "b", _wide, Metrics));
        var dock = WindowConversions.TurnIntoDock(widgets, "b", _wide, Metrics);

        Assert.Equal(SideKind.Dock, dock.Side(SideEdge.Left)!.Kind);
        Assert.Equal(Text(widgets with { Sides = [] }), Text(dock with { Sides = [] }));
    }

    [Fact]
    public void ADockWindow_CantTurnIntoADock()
    {
        var dock = Dock(SideEdge.Left, Column(Tabs("a")));

        Assert.False(WindowConversions.CanTurnIntoDock(dock, "a", _wide, Metrics));
        Assert.Same(dock, WindowConversions.TurnIntoDock(dock, "a", _wide, Metrics));
    }

    [Fact]
    public void AFloatingWindowTurnedIntoADock_JoinsAnExistingDock_AsAColumnAtItsOuterEdge()
    {
        var layout = WithFloating(
            new DockingLayout(
                [
                    new SideLayout(SideEdge.Left, SideKind.Dock, [Column(Tabs("a"))]),
                    new SideLayout(SideEdge.Right, SideKind.Widgets, [new SideColumn(160, [Tabs("b")])]),
                ],
                [],
                []),
            "f",
            200);

        var docked = WindowConversions.TurnIntoDock(layout, "f", _wide, Metrics);

        Assert.Equal(new WindowPlace(SideEdge.Left, SideKind.Dock, 0, 0, 0), docked.PlaceOf("f"));
        Assert.Equal([200, 240], docked.Side(SideEdge.Left)!.Columns.Select(column => column.Width));
        Assert.Equal(SideKind.Widgets, docked.Side(SideEdge.Right)!.Kind);
    }

    [Fact]
    public void AFloatingWindowTurnedIntoADock_WithNoDock_ConvertsTheSideTheWidgetRulesChoose()
    {
        // The left has no toolbar at its middle, so the widget rules choose it.
        var layout = WithFloating(Widgets(SideEdge.Left, Tabs("a"), Tabs("b")), "f", 200) with
        {
            Toolbars = [new ToolbarPlacement("tools", ToolbarAnchor.RightMiddle, ToolbarOrientation.Vertical)],
        };

        var docked = WindowConversions.TurnIntoDock(layout, "f", _wide, Metrics);

        var side = docked.Side(SideEdge.Left)!;
        Assert.Equal(SideKind.Dock, side.Kind);
        Assert.Equal([["f"], ["a", "b"]], side.Columns.Select(column => column.Groups.SelectMany(group => group.WindowIds)));
    }

    [Fact]
    public void AFloatingWindowTurnedIntoADock_KeepsItsWidth_AsFarAsTheRoomAllows()
    {
        var layout = WithFloating(Dock(SideEdge.Right, Column(Tabs("a"))), "f", 900);

        var docked = WindowConversions.TurnIntoDock(layout, "f", _wide, Metrics);

        // 1000 wide, less the canvas region's 320 and the other column's 240.
        Assert.Equal(440, docked.Side(SideEdge.Right)!.Columns[0].Width, 0.001);
    }

    [Fact]
    public void AFloatingWindow_CantTurnIntoADock_WithoutRoomForAColumn()
    {
        var layout = WithFloating(
            new DockingLayout(
                [
                    new SideLayout(SideEdge.Left, SideKind.Dock, [Column(Tabs("a"))]),
                    new SideLayout(SideEdge.Right, SideKind.Widgets, [Column(Tabs("b"))]),
                ],
                [],
                []),
            "f",
            200);
        var narrow = Measure(new LayoutSize(900, 800));

        Assert.False(WindowConversions.CanTurnIntoDock(layout, "f", narrow, Metrics));
        Assert.Same(layout, WindowConversions.TurnIntoDock(layout, "f", narrow, Metrics));
    }

    [Fact]
    public void ANewColumn_FitsOnlyWhileTheCanvasRegionKeepsItsMinimum()
    {
        var layout = new DockingLayout(
            [
                new SideLayout(SideEdge.Left, SideKind.Dock, [Column(Tabs("a"))]),
                new SideLayout(SideEdge.Right, SideKind.Widgets, [Column(Tabs("b"))]),
            ],
            [],
            []);

        // The right's widgets keep their margin unless they become the dock.
        Assert.Equal(1000 - 320 - 480 - 8, LayoutSolver.RoomForANewColumn(layout, SideEdge.Left, 1000, Metrics), 0.001);
        Assert.Equal(1000 - 320 - 480, LayoutSolver.RoomForANewColumn(layout, SideEdge.Right, 1000, Metrics), 0.001);
        Assert.True(LayoutSolver.HasRoomForANewColumn(layout, SideEdge.Left, 1000, Metrics));
        Assert.False(LayoutSolver.HasRoomForANewColumn(layout, SideEdge.Left, 900, Metrics));
    }

    private static DockingLayout WithFloating(DockingLayout layout, string windowId, double width) =>
        LayoutEdits.FloatWindow(layout, windowId, new LayoutPoint(300, 100), width, 300);
}
