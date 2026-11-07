// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Layouts;

public sealed class DockEditsTests
{
    private const double Tolerance = 0.001;

    // A dock on the right: column 0 at the app window's edge holds "a" over tabs "b" and "c", and column 1 holds "d".
    private static readonly DockingLayout _dock = Dock(
        SideEdge.Right,
        Column(Tabs("a"), Tabs("b", "c")),
        Column(Tabs("d")));

    [Fact]
    public void AWindowsKindAndPlace_ComeFromWhereItIs()
    {
        var layout = LayoutEdits.FloatWindow(_dock, "e", default, 240, 300);

        Assert.Equal(WindowKind.Dock, layout.KindOf("c"));
        Assert.Equal(new WindowPlace(SideEdge.Right, SideKind.Dock, 0, 1, 1), layout.PlaceOf("c"));
        Assert.Equal(WindowKind.Floating, layout.KindOf("e"));
        Assert.Null(layout.PlaceOf("e"));
        Assert.Equal(WindowKind.Widget, Widgets(SideEdge.Left, Tabs("x")).KindOf("x"));
        Assert.Null(layout.KindOf("closed"));
    }

    [Fact]
    public void AGroupShowsItsFirstTab_UntilAnotherIsSelected()
    {
        Assert.Equal("b", _dock.GroupHolding("c")!.Selected);

        var layout = LayoutEdits.SelectTab(_dock, "c");

        Assert.Equal("c", layout.GroupHolding("b")!.Selected);
    }

    [Fact]
    public void SelectingAWidget_ChangesNothing()
    {
        var widgets = Widgets(SideEdge.Left, Tabs("x"));

        Assert.Same(widgets, LayoutEdits.SelectTab(widgets, "x"));
    }

    [Fact]
    public void WhenTheTabShowingLeaves_TheOneThatTookItsPlaceShows()
    {
        var showingB = LayoutEdits.SelectTab(Dock(SideEdge.Right, Column(Tabs("a", "b", "c"))), "b");

        Assert.Equal("c", LayoutEdits.CloseWindow(showingB, "b").GroupHolding("a")!.Selected);
    }

    [Fact]
    public void WhenTheLastTabShowingLeaves_TheOneBeforeItShows()
    {
        var showingC = LayoutEdits.SelectTab(Dock(SideEdge.Right, Column(Tabs("a", "b", "c"))), "c");

        Assert.Equal("b", LayoutEdits.CloseWindow(showingC, "c").GroupHolding("a")!.Selected);
    }

    [Fact]
    public void ANewColumnAtTheEdgeOfAWidgetSide_MakesItADock_WithTheWidgetsBesideIt()
    {
        var widgets = Widgets(SideEdge.Left, new WindowGroup(["a"], Share: 2), new WindowGroup(["b"], IsTitleOnly: true));
        var layout = LayoutEdits.FloatWindow(widgets, "e", default, 240, 300);

        var docked = LayoutEdits.MoveWindowToNewColumn(layout, "e", SideEdge.Left, 0, 200);

        var side = docked.Side(SideEdge.Left)!;
        Assert.Equal(SideKind.Dock, side.Kind);
        Assert.Equal([200, 240], side.Columns.Select(column => column.Width));
        Assert.Equal(["e"], Windows(side.Columns[0]));
        Assert.Empty(docked.FloatingWindows);
        Assert.Equal(ColumnText(widgets.Side(SideEdge.Left)!.Columns[0]), ColumnText(side.Columns[1]));
    }

    [Fact]
    public void ANewColumn_GoesAmongTheColumnsThereWouldBeWithoutIt()
    {
        var docked = LayoutEdits.MoveWindowToNewColumn(_dock, "a", SideEdge.Right, 2, 180);

        var columns = docked.Side(SideEdge.Right)!.Columns;
        Assert.Equal([["b", "c"], ["d"], ["a"]], columns.Select(Windows));
        Assert.Equal(180, columns[2].Width);
    }

    [Fact]
    public void ANewColumnOnAnEmptySide_IsADockOfOne()
    {
        var docked = LayoutEdits.MoveWindowToNewColumn(_dock, "d", SideEdge.Left, 0, 200);

        Assert.Equal(SideKind.Dock, docked.Side(SideEdge.Left)!.Kind);
        Assert.Equal(["d"], Windows(docked.Side(SideEdge.Left)!.Columns[0]));
        Assert.Single(docked.Side(SideEdge.Right)!.Columns);
    }

    [Fact]
    public void AWindowArrivingInAColumn_TakesAFairShare()
    {
        var layout = LayoutEdits.MoveWindowIntoColumn(_dock, "d", SideEdge.Right, 0, 1);

        var column = layout.Side(SideEdge.Right)!.Columns[0];
        Assert.Equal([["a"], ["d"], ["b", "c"]], column.Groups.Select(group => group.WindowIds));
        Assert.Equal(2, column.Groups.Sum(group => group.Share), Tolerance);
        Assert.All(column.Groups, group => Assert.Equal(2.0 / 3, group.Share, Tolerance));
    }

    [Fact]
    public void AWindowArrivingAsATab_GoesWhereItWasDropped_AndShows()
    {
        var layout = LayoutEdits.MoveWindowIntoTabs(_dock, "d", SideEdge.Right, 0, 1, 1);

        var group = layout.GroupHolding("d")!;
        Assert.Equal(["b", "d", "c"], group.WindowIds);
        Assert.Equal("d", group.Selected);
        Assert.Single(layout.Side(SideEdge.Right)!.Columns);
    }

    [Fact]
    public void ATabMovedWithinItsGroup_GoesAmongTheOthers()
    {
        var layout = LayoutEdits.MoveWindowIntoTabs(_dock, "b", SideEdge.Right, 0, 1, 1);

        Assert.Equal(["c", "b"], layout.GroupHolding("b")!.WindowIds);
    }

    [Fact]
    public void ATabDroppedWhereThereIsNoGroup_ChangesNothing()
    {
        Assert.Same(_dock, LayoutEdits.MoveWindowIntoTabs(_dock, "a", SideEdge.Right, 0, 5, 0));
        Assert.Same(_dock, LayoutEdits.MoveWindowIntoTabs(_dock, "a", SideEdge.Left, 0, 0, 0));
        Assert.Same(_dock, LayoutEdits.MoveWindowIntoColumn(_dock, "a", SideEdge.Right, 4, 0));
    }

    [Fact]
    public void ADockCanBecomeWidgets_OnlyWithOneColumnOfAtMostFourSingleWindows()
    {
        Assert.True(LayoutEdits.CanBecomeWidgets(Dock(SideEdge.Left, Column(Tabs("a"), Tabs("b"), Tabs("c"), Tabs("d"))).Sides[0]));
        Assert.False(LayoutEdits.CanBecomeWidgets(Dock(SideEdge.Left, Column(Tabs("a"), Tabs("b"), Tabs("c"), Tabs("d"), Tabs("e"))).Sides[0]));
        Assert.False(LayoutEdits.CanBecomeWidgets(Dock(SideEdge.Left, Column(Tabs("a", "b"))).Sides[0]));
        Assert.False(LayoutEdits.CanBecomeWidgets(_dock.Sides[0]));
    }

    [Fact]
    public void TurningADockWindowIntoAWidget_ConvertsItsSide_WhenItCan()
    {
        var dock = Dock(SideEdge.Left, Column(new WindowGroup(["a"], Share: 3), new WindowGroup(["b"], IsTitleOnly: true)));

        Assert.True(LayoutEdits.CanTurnIntoWidget(dock, "b"));
        var widgets = LayoutEdits.TurnIntoWidget(dock, "b", 240);

        Assert.Equal(SideKind.Widgets, widgets.Side(SideEdge.Left)!.Kind);
        Assert.Equal(ColumnText(dock.Side(SideEdge.Left)!.Columns[0]), ColumnText(widgets.Side(SideEdge.Left)!.Columns[0]));
    }

    [Fact]
    public void TurningADockWindowIntoAWidget_DoesNothing_WhenItsSideCant()
    {
        Assert.False(LayoutEdits.CanTurnIntoWidget(_dock, "a"));
        Assert.Same(_dock, LayoutEdits.TurnIntoWidget(_dock, "a", 240));
    }

    [Fact]
    public void AWidget_CantTurnIntoAWidget()
    {
        Assert.False(LayoutEdits.CanTurnIntoWidget(Widgets(SideEdge.Left, Tabs("a")), "a"));
    }

    [Fact]
    public void ASideTurnedIntoADockAndBack_IsAsItWas()
    {
        Valid.Sample(layout =>
        {
            foreach (var side in layout.Sides.Where(side => side.Kind == SideKind.Widgets))
            {
                var dock = LayoutEdits.TurnSideIntoDock(layout, side.Edge);
                Assert.Equal(SideKind.Dock, dock.Side(side.Edge)!.Kind);
                Assert.Equal(Text(layout), Text(LayoutEdits.TurnSideIntoWidgets(dock, side.Edge)));
            }
        });
    }

    [Fact]
    public void AClosedTab_ReopensBesideTheTabItWasWith()
    {
        var moved = LayoutEdits.MoveWindowIntoColumn(LayoutEdits.CloseWindow(_dock, "c"), "b", SideEdge.Right, 1, 0);

        var reopened = LayoutEdits.OpenWindow(moved, "c", 240);

        Assert.Equal(["b", "c"], reopened.GroupHolding("c")!.WindowIds);
        Assert.Equal(1, reopened.PlaceOf("c")!.Value.Column);
        Assert.Equal("c", reopened.GroupHolding("c")!.Selected);
    }

    [Fact]
    public void AClosedDockWindow_ReopensWhereItWasInItsColumn()
    {
        var reopened = LayoutEdits.OpenWindow(LayoutEdits.CloseWindow(_dock, "a"), "a", 240);

        Assert.Equal(Text(_dock), Text(reopened));
    }

    [Fact]
    public void AClosedDockWindowsColumn_ComesBack_IfItHasGone()
    {
        var reopened = LayoutEdits.OpenWindow(LayoutEdits.CloseWindow(_dock, "d"), "d", 300);

        Assert.Equal(Text(_dock), Text(reopened));
    }

    [Fact]
    public void ARemembered_TabOrColumnThatMakesNoSense_IsRepaired()
    {
        var group = new WindowGroup(["w0", "w1"], SelectedWindowId: "w5");
        var closed = new ClosedWindow("w2", SideEdge.Right, 0, 1, 240, Column: -3, TabmateId: "gone");
        var layout = new DockingLayout([new SideLayout(SideEdge.Right, SideKind.Dock, [Column(group)])], [], [closed]);

        var repaired = LayoutRepair.Repair(layout, Catalog);

        Assert.Null(repaired.GroupHolding("w0")!.SelectedWindowId);
        var remembered = repaired.ClosedWindows.Single(window => window.WindowId == "w2");
        Assert.Equal(0, remembered.Column);
        Assert.Null(remembered.TabmateId);
    }

    [Fact]
    public void AnyDockMoveOfAnyWindow_KeepsTheRules_AndTheWindowOpenExactlyOnce()
    {
        Gen.Select(Valid, Gen.OneOf(Gen.Const(SideEdge.Left), Gen.Const(SideEdge.Right)), Gen.Int[0, 5], Gen.Int[0, 3],
            Gen.Int[0, 3]).Sample((layout, edge, move, column, position) =>
            {
                foreach (var windowId in layout.OpenWindowIds().ToList())
                {
                    var moved = move switch
                    {
                        0 => LayoutEdits.MoveWindowToNewColumn(layout, windowId, edge, column, 200),
                        1 => LayoutEdits.MoveWindowIntoColumn(layout, windowId, edge, column, position),
                        2 => LayoutEdits.MoveWindowIntoTabs(layout, windowId, edge, column, position, position),
                        3 => LayoutEdits.TurnSideIntoDock(layout, edge),
                        4 => LayoutEdits.TurnSideIntoWidgets(layout, edge),
                        _ => LayoutEdits.SelectTab(layout, windowId),
                    };

                    Assert.Equal(1, moved.OpenWindowIds().Count(id => id == windowId));
                    Assert.Equal(Text(moved), Text(LayoutRepair.Repair(moved, Catalog)));
                }
            });
    }

    [Fact]
    public void AnyClosedSideWindow_ComesBackOpen_WhenItsSideIsUnchanged()
    {
        Valid.Sample(layout =>
        {
            foreach (var windowId in layout.Sides.Where(side => side.Kind == SideKind.Dock)
                         .SelectMany(side => side.Columns).SelectMany(Windows).ToList())
            {
                var back = LayoutEdits.OpenWindow(LayoutEdits.CloseWindow(layout, windowId), windowId, 240);

                Assert.True(back.IsWindowOpen(windowId));
                Assert.Equal(layout.PlaceOf(windowId)!.Value.Edge, back.PlaceOf(windowId)!.Value.Edge);
                Assert.Equal(Text(back), Text(LayoutRepair.Repair(back, Catalog)));
            }
        });
    }

    private static string ColumnText(SideColumn column) => Text(Dock(SideEdge.Left, column));

    private static List<string> Windows(SideColumn column) => [.. column.Groups.SelectMany(group => group.WindowIds)];
}
