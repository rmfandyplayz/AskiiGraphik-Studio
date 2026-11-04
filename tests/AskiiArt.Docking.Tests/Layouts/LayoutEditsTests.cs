// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Layouts;

public sealed class LayoutEditsTests
{
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

        var hidden = LayoutEdits.SetToolbarVisible(layout, "file", isVisible: false);
        Assert.False(hidden.IsToolbarVisible("file"));

        Assert.Equal(Text(layout), Text(LayoutEdits.SetToolbarVisible(hidden, "file", isVisible: true)));
    }

    [Fact]
    public void ShowingAToolbarWhoseAnchorIsTaken_MovesItToAFreeOne()
    {
        var layout = new DockingLayout([],
        [
            new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal, IsVisible: false),
            new ToolbarPlacement("edit", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal),
        ], []);

        var shown = LayoutEdits.SetToolbarVisible(layout, "file", isVisible: true);

        Assert.True(shown.IsToolbarVisible("file"));
        Assert.Equal(ToolbarAnchor.TopMiddle, shown.Toolbars[0].Anchor);
    }

    [Fact]
    public void AnySequenceOfEdits_LosesNothing_AndKeepsTheRules()
    {
        var steps = Gen.Select(Gen.Int[0, 2], Gen.OneOfConst(WindowIds), Gen.OneOfConst("file", "tools", "edit", "view"), Gen.Bool);

        Gen.Select(Valid, steps.List[0, 30]).Sample((start, edits) =>
        {
            var layout = start;
            foreach (var (kind, windowId, toolbarId, isVisible) in edits)
            {
                layout = kind switch
                {
                    0 => LayoutEdits.CloseWindow(layout, windowId),
                    1 => LayoutEdits.OpenWindow(layout, windowId, DefaultWidth),
                    _ => LayoutEdits.SetToolbarVisible(layout, toolbarId, isVisible),
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
