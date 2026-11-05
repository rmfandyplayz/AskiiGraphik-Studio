// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Layouts;

public sealed class LayoutEditsTests
{
    [Theory]
    [InlineData(ToolbarAnchor.TopMiddle, ToolbarOrientation.Horizontal, SubToolbarSide.BelowOrRight)]
    [InlineData(ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal, SubToolbarSide.AboveOrLeft)]
    [InlineData(ToolbarAnchor.LeftMiddle, ToolbarOrientation.Vertical, SubToolbarSide.BelowOrRight)]
    [InlineData(ToolbarAnchor.RightMiddle, ToolbarOrientation.Vertical, SubToolbarSide.AboveOrLeft)]
    [InlineData(ToolbarAnchor.TopRight, ToolbarOrientation.Vertical, SubToolbarSide.AboveOrLeft)]
    [InlineData(ToolbarAnchor.BottomLeft, ToolbarOrientation.Horizontal, SubToolbarSide.AboveOrLeft)]
    public void FloatingAToolbar_RemembersWhichSideFacedTheCanvas(ToolbarAnchor anchor, ToolbarOrientation orientation,
        SubToolbarSide expected)
    {
        var layout = new DockingLayout([], [new ToolbarPlacement("a", anchor, orientation)], []);

        var floated = LayoutEdits.FloatToolbar(layout, "a", new LayoutPoint(10, 10));

        Assert.Equal(expected, floated.Toolbars[0].SubToolbarSide);
    }

    [Fact]
    public void MovingAFloatingToolbar_KeepsTheSideItRemembers()
    {
        var layout = new DockingLayout([], [new ToolbarPlacement("a", ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal)], []);
        var floated = LayoutEdits.FloatToolbar(layout, "a", new LayoutPoint(10, 10));

        var moved = LayoutEdits.FloatToolbar(floated, "a", new LayoutPoint(50, 50));

        Assert.Equal(SubToolbarSide.AboveOrLeft, moved.Toolbars[0].SubToolbarSide);
    }

    [Fact]
    public void TurningAFloatingToolbar_KeepsTheSideItRemembers()
    {
        var layout = new DockingLayout([], [new ToolbarPlacement("a", ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal)], []);
        var floated = LayoutEdits.FloatToolbar(layout, "a", new LayoutPoint(10, 10));

        var turned = LayoutEdits.SetToolbarOrientation(floated, "a", ToolbarOrientation.Vertical);

        Assert.Equal(SubToolbarSide.AboveOrLeft, turned.Toolbars[0].SubToolbarSide);
    }

    [Fact]
    public void AToolbarFloatedBecauseItsAnchorWasTaken_RemembersTheSideToo()
    {
        var layout = new DockingLayout([],
        [
            new ToolbarPlacement("a", ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal, IsVisible: false),
            new ToolbarPlacement("b", ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal),
        ], []);

        var shown = LayoutEdits.ShowToolbar(layout, "a", new LayoutPoint(10, 10));

        Assert.True(shown.Toolbars[0].IsFloating);
        Assert.Equal(SubToolbarSide.AboveOrLeft, shown.Toolbars[0].SubToolbarSide);
    }

    private const double DefaultWidth = 240;

    private static readonly DockingLayout _twoWidgets =
        Widgets(SideEdge.Right, new WindowGroup(["a"], Share: 2), new WindowGroup(["b"]));

    [Fact]
    public void ClosingAWindow_RemembersWhereItWas()
    {
        var layout = LayoutEdits.CloseWindow(_twoWidgets, "a");

        Assert.False(layout.IsWindowOpen("a"));
        Assert.True(layout.IsWindowOpen("b"));
        Assert.Equal(new ClosedWindow("a", SideEdge.Right, 0, 2, 240), Assert.Single(layout.ClosedWindows));
    }

    [Fact]
    public void ReopeningAWindow_PutsItBackWhereItWas()
    {
        var closed = LayoutEdits.CloseWindow(_twoWidgets, "a");

        var reopened = LayoutEdits.OpenWindow(closed, "a", DefaultWidth);

        Assert.Equal(Text(_twoWidgets), Text(reopened));
    }

    [Fact]
    public void ClosingEveryWindowOnASide_EmptiesIt_AndReopeningRestoresItsWidth()
    {
        var layout = LayoutEdits.CloseWindow(Widgets(SideEdge.Left, new WindowGroup(["a"])), "a");
        Assert.Null(layout.Side(SideEdge.Left));

        var reopened = LayoutEdits.OpenWindow(layout, "a", DefaultWidth + 100);

        Assert.Equal(240, reopened.Side(SideEdge.Left)!.Columns[0].Width);
    }

    [Fact]
    public void AWindowWithNoRememberedPlace_JoinsTheBottomOfTheRightSide()
    {
        var layout = LayoutEdits.OpenWindow(_twoWidgets, "c", DefaultWidth);

        var groups = layout.Side(SideEdge.Right)!.Columns[0].Groups;
        Assert.Equal(["a", "b", "c"], groups.Select(group => group.WindowIds[0]));
    }

    [Fact]
    public void AWindowWhosePlaceIsFull_GoesToTheOtherSide()
    {
        var full = Widgets(SideEdge.Right,
            new WindowGroup(["a"]), new WindowGroup(["b"]), new WindowGroup(["c"]), new WindowGroup(["d"]));

        var layout = LayoutEdits.OpenWindow(full, "e", DefaultWidth);

        Assert.Equal("e", layout.Side(SideEdge.Left)!.Columns[0].Groups[0].WindowIds[0]);
    }

    [Fact]
    public void ClosingTheOnlyOpenWidget_OpensAnother_SoAStackAlwaysShowsSomething()
    {
        var layout = Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"], IsTitleOnly: true));

        var closed = LayoutEdits.CloseWindow(layout, "a");

        Assert.False(closed.Side(SideEdge.Right)!.Columns[0].Groups[0].IsTitleOnly);
    }

    [Fact]
    public void OpeningAnOpenWindow_AndClosingAClosedOne_DoNothing()
    {
        Assert.Same(_twoWidgets, LayoutEdits.OpenWindow(_twoWidgets, "a", DefaultWidth));
        Assert.Same(_twoWidgets, LayoutEdits.CloseWindow(_twoWidgets, "z"));
    }

    [Fact]
    public void HidingAToolbar_KeepsItsPlace_AndShowingItPutsItBack()
    {
        var layout = new DockingLayout([],
            [new ToolbarPlacement("file", ToolbarAnchor.BottomLeft, ToolbarOrientation.Vertical)], []);

        var hidden = LayoutEdits.HideToolbar(layout, "file");
        Assert.False(hidden.IsToolbarVisible("file"));

        Assert.Equal(Text(layout), Text(LayoutEdits.ShowToolbar(hidden, "file", default)));
    }

    [Fact]
    public void ShowingAToolbarWhoseAnchorIsTaken_FloatsItThere()
    {
        var layout = new DockingLayout([],
        [
            new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal, IsVisible: false),
            new ToolbarPlacement("edit", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal),
        ], []);

        var shown = LayoutEdits.ShowToolbar(layout, "file", new LayoutPoint(8, 8));

        Assert.True(shown.IsToolbarVisible("file"));
        Assert.Equal(new LayoutPoint(8, 8), shown.Toolbars[0].FloatingAt);
        Assert.Equal(ToolbarAnchor.TopLeft, shown.Toolbars[1].Anchor);
    }

    [Fact]
    public void AFloatingToolbar_HiddenAndShown_FloatsWhereItWas()
    {
        var layout = new DockingLayout([],
            [new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Vertical, FloatingAt: new LayoutPoint(300, 200))], []);

        var shown = LayoutEdits.ShowToolbar(LayoutEdits.HideToolbar(layout, "file"), "file", new LayoutPoint(1, 1));

        Assert.Equal(Text(layout), Text(shown));
    }

    [Theory]
    [InlineData(ToolbarAnchor.TopMiddle, ToolbarOrientation.Horizontal)]
    [InlineData(ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal)]
    [InlineData(ToolbarAnchor.LeftMiddle, ToolbarOrientation.Vertical)]
    [InlineData(ToolbarAnchor.RightMiddle, ToolbarOrientation.Vertical)]
    public void MovingAToolbarToAnEdgeMiddle_SetsItsDirection(ToolbarAnchor anchor, ToolbarOrientation expected)
    {
        var wrong = expected == ToolbarOrientation.Horizontal ? ToolbarOrientation.Vertical : ToolbarOrientation.Horizontal;
        var layout = new DockingLayout([], [new ToolbarPlacement("file", ToolbarAnchor.TopLeft, wrong)], []);

        var moved = LayoutEdits.MoveToolbarToAnchor(layout, "file", anchor);

        Assert.Equal((anchor, expected), (moved.Toolbars[0].Anchor, moved.Toolbars[0].Orientation));
    }

    [Theory]
    [InlineData(ToolbarOrientation.Horizontal)]
    [InlineData(ToolbarOrientation.Vertical)]
    public void MovingAToolbarToACorner_KeepsItsDirection(ToolbarOrientation orientation)
    {
        var layout = new DockingLayout([], [new ToolbarPlacement("file", ToolbarAnchor.TopLeft, orientation)], []);

        var moved = LayoutEdits.MoveToolbarToAnchor(layout, "file", ToolbarAnchor.BottomRight);

        Assert.Equal((ToolbarAnchor.BottomRight, orientation), (moved.Toolbars[0].Anchor, moved.Toolbars[0].Orientation));
    }

    [Fact]
    public void MovingAToolbarToATakenAnchor_DoesNothing()
    {
        var layout = new DockingLayout([],
        [
            new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal),
            new ToolbarPlacement("edit", ToolbarAnchor.TopRight, ToolbarOrientation.Horizontal),
        ], []);

        Assert.Same(layout, LayoutEdits.MoveToolbarToAnchor(layout, "file", ToolbarAnchor.TopRight));
    }

    [Fact]
    public void FloatingAToolbar_KeepsItsDirection_AndPutsItLast_SoItsDrawnOnTop()
    {
        var layout = new DockingLayout([],
        [
            new ToolbarPlacement("file", ToolbarAnchor.LeftMiddle, ToolbarOrientation.Vertical),
            new ToolbarPlacement("edit", ToolbarAnchor.TopRight, ToolbarOrientation.Horizontal),
        ], []);

        var floated = LayoutEdits.FloatToolbar(layout, "file", new LayoutPoint(300, 200));

        Assert.Equal(["edit", "file"], floated.Toolbars.Select(toolbar => toolbar.ToolbarId));
        Assert.Equal(ToolbarOrientation.Vertical, floated.Toolbars[1].Orientation);
        Assert.Equal(new LayoutPoint(300, 200), floated.Toolbars[1].FloatingAt);
    }

    [Fact]
    public void AToolbarLeavingFloating_ForgetsWhereItFloated()
    {
        var layout = new DockingLayout([],
            [new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal, FloatingAt: new LayoutPoint(300, 200))], []);

        Assert.False(LayoutEdits.MoveToolbarToAnchor(layout, "file", ToolbarAnchor.TopRight).Toolbars[0].IsFloating);
    }

    [Theory]
    [InlineData(ToolbarAnchor.TopLeft, false, true)]
    [InlineData(ToolbarAnchor.BottomRight, false, true)]
    [InlineData(ToolbarAnchor.TopMiddle, false, false)]
    [InlineData(ToolbarAnchor.LeftMiddle, false, false)]
    [InlineData(ToolbarAnchor.TopMiddle, true, true)]
    public void AToolbarsDirection_CanBeChosen_OnACornerOrFloating(ToolbarAnchor anchor, bool isFloating, bool canChoose)
    {
        var toolbar = LayoutEdits.Placed(new ToolbarPlacement("file", anchor, ToolbarOrientation.Horizontal), anchor);
        var placed = isFloating ? toolbar with { FloatingAt = new LayoutPoint(1, 1) } : toolbar;
        var layout = new DockingLayout([], [placed], []);

        var horizontal = LayoutEdits.SetToolbarOrientation(layout, "file", ToolbarOrientation.Horizontal).Toolbars[0];
        var vertical = LayoutEdits.SetToolbarOrientation(layout, "file", ToolbarOrientation.Vertical).Toolbars[0];

        Assert.Equal(canChoose, placed.CanChooseOrientation);
        Assert.Equal(canChoose, horizontal.Orientation != vertical.Orientation);
    }

    [Fact]
    public void AnySequenceOfEdits_LosesNothing_AndKeepsTheRules()
    {
        var steps = Gen.Select(
            Gen.Int[0, 6],
            Gen.OneOfConst(WindowIds),
            Gen.OneOfConst("file", "tools", "edit", "view"),
            Gen.Enum<ToolbarAnchor>(),
            Gen.Enum<ToolbarOrientation>(),
            Gen.Double[-500, 3000]);

        Gen.Select(Valid, steps.List[0, 30]).Sample((start, edits) =>
        {
            var layout = start;
            foreach (var (kind, windowId, toolbarId, anchor, orientation, position) in edits)
            {
                layout = kind switch
                {
                    0 => LayoutEdits.CloseWindow(layout, windowId),
                    1 => LayoutEdits.OpenWindow(layout, windowId, DefaultWidth),
                    2 => LayoutEdits.HideToolbar(layout, toolbarId),
                    3 => LayoutEdits.ShowToolbar(layout, toolbarId, new LayoutPoint(position, position)),
                    4 => LayoutEdits.MoveToolbarToAnchor(layout, toolbarId, anchor),
                    5 => LayoutEdits.FloatToolbar(layout, toolbarId, new LayoutPoint(position, position)),
                    _ => LayoutEdits.SetToolbarOrientation(layout, toolbarId, orientation),
                };

                AssertEveryWindowIsInOnePlace(layout);
                Assert.Equal(Text(layout), Text(LayoutRepair.Repair(layout, Catalog)));
            }
        });
    }

    internal static void AssertEveryWindowIsInOnePlace(DockingLayout layout)
    {
        var places = layout.OpenWindowIds().Concat(layout.ClosedWindows.Select(closed => closed.WindowId)).ToList();

        Assert.Equal(WindowIds.Order(StringComparer.Ordinal), places.Order(StringComparer.Ordinal));
    }
}
