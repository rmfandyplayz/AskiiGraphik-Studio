// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Layouts;

public sealed class WindowEditsTests
{
    private const double Tolerance = 0.001;

    private static readonly DockingLayout _twoWidgets =
        Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]));

    [Fact]
    public void FloatingAWindow_TakesItOffItsSide()
    {
        var layout = LayoutEdits.FloatWindow(_twoWidgets, "a", new LayoutPoint(10, 20), 240, 300);

        Assert.Equal(["b"], Stack(layout, SideEdge.Right));
        Assert.Equal(new FloatingWindow("a", new LayoutPoint(10, 20), 240, 300), Assert.Single(layout.FloatingWindows));
    }

    [Fact]
    public void FloatingTheLastWindowOnASide_EmptiesTheSide()
    {
        var layout = LayoutEdits.FloatWindow(Widgets(SideEdge.Left, new WindowGroup(["a"])), "a", default, 240, 300);

        Assert.Null(layout.Side(SideEdge.Left));
        Assert.True(layout.IsWindowOpen("a"));
    }

    [Fact]
    public void FloatingAWindow_PutsItInFrontOfTheOthers()
    {
        var layout = LayoutEdits.FloatWindow(_twoWidgets, "a", default, 240, 300);
        layout = LayoutEdits.FloatWindow(layout, "b", default, 240, 300);
        layout = LayoutEdits.FloatWindow(layout, "a", new LayoutPoint(5, 5), 240, 300);

        Assert.Equal(["b", "a"], layout.FloatingWindows.Select(window => window.WindowId));
    }

    [Fact]
    public void BringingAFloatingWindowToTheFront_ChangesOnlyItsPlaceInTheOrder()
    {
        var layout = LayoutEdits.FloatWindow(_twoWidgets, "a", new LayoutPoint(1, 2), 240, 300);
        layout = LayoutEdits.FloatWindow(layout, "b", new LayoutPoint(3, 4), 240, 300);

        var raised = LayoutEdits.BringFloatingWindowToFront(layout, "a");

        Assert.Equal(["b", "a"], raised.FloatingWindows.Select(window => window.WindowId));
        Assert.Equal(new LayoutPoint(1, 2), raised.FloatingWindowNamed("a")!.Position);
    }

    [Fact]
    public void BringingTheFrontWindowToTheFront_ChangesNothing()
    {
        var layout = LayoutEdits.FloatWindow(_twoWidgets, "a", default, 240, 300);

        Assert.Same(layout, LayoutEdits.BringFloatingWindowToFront(layout, "a"));
        Assert.Same(layout, LayoutEdits.BringFloatingWindowToFront(layout, "b"));
    }

    [Fact]
    public void ClosingAFloatingWindow_PutsItBackWhereItWasWhenReopened()
    {
        var floating = LayoutEdits.FloatWindow(_twoWidgets, "a", new LayoutPoint(10, 20), 300, 400, isTitleOnly: true);

        var closed = LayoutEdits.CloseWindow(floating, "a");
        Assert.False(closed.IsWindowOpen("a"));
        Assert.Empty(closed.FloatingWindows);

        var reopened = LayoutEdits.OpenWindow(closed, "a", 240);
        Assert.Equal(new FloatingWindow("a", new LayoutPoint(10, 20), 300, 400, true), reopened.FloatingWindowNamed("a"));
        Assert.Empty(reopened.ClosedWindows);
    }

    [Theory]
    [InlineData(0, new[] { "c", "a", "b" })]
    [InlineData(1, new[] { "a", "c", "b" })]
    [InlineData(2, new[] { "a", "b", "c" })]
    public void AWindowArrivingInAStack_GoesWhereItWasDropped(int position, string[] expected)
    {
        var layout = LayoutEdits.MoveWindowIntoStack(_twoWidgets, "c", SideEdge.Right, position);

        Assert.Equal(expected, Stack(layout, SideEdge.Right));
    }

    [Fact]
    public void AWindowArrivingInAStack_TakesAFairShare()
    {
        var layout = LayoutEdits.MoveWindowIntoStack(_twoWidgets, "c", SideEdge.Right, 2);

        var shares = layout.Side(SideEdge.Right)!.Columns[0].Groups.Select(group => group.Share).ToList();
        Assert.All(shares, share => Assert.Equal(shares[0], share, Tolerance));
    }

    [Fact]
    public void AWindowArrivingInAStack_TakesFromTheLargestFirst()
    {
        var uneven = Widgets(SideEdge.Right, new WindowGroup(["a"], 6), new WindowGroup(["b"], 2));

        var groups = LayoutEdits.MoveWindowIntoStack(uneven, "c", SideEdge.Right, 2).Side(SideEdge.Right)!.Columns[0].Groups;

        var fair = 8.0 / 3;
        Assert.Equal(8 - fair - 2, groups[0].Share, Tolerance);
        Assert.Equal(2, groups[1].Share, Tolerance);
        Assert.Equal(fair, groups[2].Share, Tolerance);
    }

    [Fact]
    public void AWindowArrivingInAStack_LeavesTitleOnlyWindowsAsTheyAre()
    {
        var stack = Widgets(SideEdge.Right, new WindowGroup(["a"], 1, true), new WindowGroup(["b"], 2));

        var groups = LayoutEdits.MoveWindowIntoStack(stack, "c", SideEdge.Right, 1).Side(SideEdge.Right)!.Columns[0].Groups;

        Assert.True(groups[0].IsTitleOnly);
        Assert.Equal(1, groups[0].Share);
        Assert.Equal(1, groups[1].Share, Tolerance);
        Assert.Equal(1, groups[2].Share, Tolerance);
    }

    [Fact]
    public void ATitleOnlyWindow_LandsWithItsContentsShowing()
    {
        var stack = Widgets(SideEdge.Right, new WindowGroup(["a"], 1, true), new WindowGroup(["b"]));

        var groups = LayoutEdits.MoveWindowIntoStack(stack, "a", SideEdge.Right, 1).Side(SideEdge.Right)!.Columns[0].Groups;

        Assert.Equal(["b", "a"], groups.Select(group => group.WindowIds[0]));
        Assert.False(groups[1].IsTitleOnly);
    }

    [Fact]
    public void AFullStack_TakesNoMoreWindows()
    {
        var full = Widgets(SideEdge.Right,
            new WindowGroup(["a"]), new WindowGroup(["b"]), new WindowGroup(["c"]), new WindowGroup(["d"]));

        Assert.Same(full, LayoutEdits.MoveWindowIntoStack(full, "e", SideEdge.Right, 0));
    }

    [Fact]
    public void AWindowMovedWithinAFullStack_JustChangesPlace()
    {
        var full = Widgets(SideEdge.Right,
            new WindowGroup(["a"]), new WindowGroup(["b"]), new WindowGroup(["c"]), new WindowGroup(["d"]));

        var moved = LayoutEdits.MoveWindowIntoStack(full, "a", SideEdge.Right, 3);

        Assert.Equal(["b", "c", "d", "a"], Stack(moved, SideEdge.Right));
    }

    [Fact]
    public void AnEmptySide_TakesAWindowAtTheWidthAskedFor()
    {
        var layout = LayoutEdits.MoveWindowToEmptySide(_twoWidgets, "a", SideEdge.Left, 300);

        Assert.Equal(["a"], Stack(layout, SideEdge.Left));
        Assert.Equal(300, layout.Side(SideEdge.Left)!.Columns[0].Width);
        Assert.Equal(["b"], Stack(layout, SideEdge.Right));
    }

    [Fact]
    public void ASideWithWindows_IsntEmpty()
    {
        Assert.Same(_twoWidgets, LayoutEdits.MoveWindowToEmptySide(_twoWidgets, "c", SideEdge.Right, 300));
    }

    [Fact]
    public void TurningIntoAWidget_GoesToTheOnlySideWithRoom()
    {
        var full = Widgets(SideEdge.Right,
            new WindowGroup(["a"]), new WindowGroup(["b"]), new WindowGroup(["c"]), new WindowGroup(["d"]));
        var floating = LayoutEdits.FloatWindow(full, "e", default, 240, 300);

        var layout = LayoutEdits.TurnIntoWidget(floating, "e", 240);

        Assert.Equal(["e"], Stack(layout, SideEdge.Left));
        Assert.Empty(layout.FloatingWindows);
    }

    [Fact]
    public void TurningIntoAWidget_PrefersTheSideWithNoToolbarAtItsMiddle()
    {
        var layout = new DockingLayout([], [new ToolbarPlacement("tools", ToolbarAnchor.RightMiddle, ToolbarOrientation.Vertical)], [])
        {
            FloatingWindows = [new FloatingWindow("a", default, 240, 300)],
        };

        Assert.Equal(["a"], Stack(LayoutEdits.TurnIntoWidget(layout, "a", 240), SideEdge.Left));
    }

    [Fact]
    public void ACornerToolbar_DoesntCountWhenChoosingASide()
    {
        var layout = new DockingLayout([], [new ToolbarPlacement("file", ToolbarAnchor.TopRight, ToolbarOrientation.Vertical)], [])
        {
            FloatingWindows = [new FloatingWindow("a", default, 240, 300)],
        };

        Assert.Equal(["a"], Stack(LayoutEdits.TurnIntoWidget(layout, "a", 240), SideEdge.Right));
    }

    [Fact]
    public void TurningIntoAWidget_ThenPrefersTheSideWithFewerWindows()
    {
        var floating = LayoutEdits.FloatWindow(_twoWidgets, "c", default, 240, 300);

        Assert.Equal(["c"], Stack(LayoutEdits.TurnIntoWidget(floating, "c", 240), SideEdge.Left));
    }

    [Fact]
    public void TurningIntoAWidget_ThenPrefersTheRight()
    {
        var layout = new DockingLayout([], [], []) { FloatingWindows = [new FloatingWindow("a", default, 240, 300)] };

        Assert.Equal(["a"], Stack(LayoutEdits.TurnIntoWidget(layout, "a", 240), SideEdge.Right));
    }

    [Fact]
    public void TurningIntoAWidget_NeedsASideWithRoom()
    {
        var full = new DockingLayout(
            [
                new SideLayout(SideEdge.Left, SideKind.Widgets, [new SideColumn(240, [new WindowGroup(["a"]), new WindowGroup(["b"]), new WindowGroup(["c"]), new WindowGroup(["d"])])]),
                new SideLayout(SideEdge.Right, SideKind.Dock, [new SideColumn(240, [new WindowGroup(["e"])])]),
            ],
            [],
            [])
        {
            FloatingWindows = [new FloatingWindow("f", default, 240, 300)],
        };

        Assert.False(LayoutEdits.CanTurnIntoWidget(full, "f"));
        Assert.Same(full, LayoutEdits.TurnIntoWidget(full, "f", 240));
    }

    [Fact]
    public void AnyMoveOfAnyWindow_KeepsTheRules_AndTheWindowOpenExactlyOnce()
    {
        var moves = Gen.Int[0, 3];

        Gen.Select(Valid, Gen.OneOf(Gen.Const(SideEdge.Left), Gen.Const(SideEdge.Right)), moves, Gen.Int[0, 4]).Sample(
            (layout, edge, move, position) =>
            {
                foreach (var windowId in layout.OpenWindowIds().ToList())
                {
                    var moved = move switch
                    {
                        0 => LayoutEdits.FloatWindow(layout, windowId, new LayoutPoint(10, 10), 200, 200),
                        1 => LayoutEdits.MoveWindowIntoStack(layout, windowId, edge, position),
                        2 => LayoutEdits.MoveWindowToEmptySide(layout, windowId, edge, 240),
                        _ => LayoutEdits.BringFloatingWindowToFront(layout, windowId),
                    };

                    Assert.Equal(1, moved.OpenWindowIds().Count(id => id == windowId));
                    Assert.Equal(Text(moved), Text(LayoutRepair.Repair(moved, Catalog)));
                }
            });
    }

    [Fact]
    public void AnyClosedWindow_ComesBackOpen_WhenThereIsRoom()
    {
        Valid.Sample(layout =>
        {
            foreach (var windowId in layout.FloatingWindows.Select(window => window.WindowId).ToList())
            {
                var back = LayoutEdits.OpenWindow(LayoutEdits.CloseWindow(layout, windowId), windowId, 240);

                Assert.Equal(layout.FloatingWindowNamed(windowId), back.FloatingWindowNamed(windowId));
            }
        });
    }

    private static List<string> Stack(DockingLayout layout, SideEdge edge) =>
        [.. layout.Side(edge)!.Columns[0].Groups.Select(group => group.WindowIds[0])];
}
