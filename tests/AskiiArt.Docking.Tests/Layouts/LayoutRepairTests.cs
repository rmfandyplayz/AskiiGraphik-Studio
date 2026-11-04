// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Layouts;

public sealed class LayoutRepairTests
{
    private static readonly DockingCatalog _small = new(["a", "b"],
        [new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal)], DefaultSideWidth: 240);

    private static readonly DockingLayout _empty = new([], [], []);

    [Fact]
    public void AWindowTheAppNoLongerHas_IsSkipped()
    {
        var layout = LayoutRepair.Repair(Widgets(SideEdge.Right, new WindowGroup(["gone"]), new WindowGroup(["a"])), _small);

        Assert.False(layout.IsWindowOpen("gone"));
        Assert.True(layout.IsWindowOpen("a"));
    }

    [Fact]
    public void AWindowMissingFromTheLayout_CountsAsNew_AndOpens()
    {
        var layout = LayoutRepair.Repair(_empty, _small);

        Assert.True(layout.IsWindowOpen("a"));
        Assert.True(layout.IsWindowOpen("b"));
    }

    [Fact]
    public void AClosedWindow_StaysClosed()
    {
        var layout = _empty with { ClosedWindows = [new ClosedWindow("a", SideEdge.Left, 0, 1, 240)] };

        var repaired = LayoutRepair.Repair(layout, _small);

        Assert.False(repaired.IsWindowOpen("a"));
        Assert.True(repaired.IsWindowOpen("b"));
    }

    [Fact]
    public void AWindowInTwoPlaces_KeepsTheFirst()
    {
        var layout = Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["a"]), new WindowGroup(["b"]));

        var repaired = LayoutRepair.Repair(layout, _small);

        Assert.Equal(["a", "b"], repaired.OpenWindowIds());
    }

    [Fact]
    public void AWidgetSide_GetsOneColumn_OfSingleWindows()
    {
        var layout = new DockingLayout(
            [
                new SideLayout(SideEdge.Right, SideKind.Widgets,
                [
                    new SideColumn(300, [new WindowGroup(["a", "b"])]),
                    new SideColumn(100, [new WindowGroup(["c"])]),
                ]),
            ],
            [],
            []);

        var repaired = LayoutRepair.Repair(layout, new DockingCatalog(["a", "b", "c"], [], 240));

        var column = Assert.Single(repaired.Side(SideEdge.Right)!.Columns);
        Assert.Equal(300, column.Width);
        Assert.Equal(["a", "b", "c"], column.Groups.Select(group => Assert.Single(group.WindowIds)));
    }

    [Fact]
    public void MoreThanFourWidgets_AreTrimmed_AndTheRestGoWhereThereIsRoom()
    {
        var layout = Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]), new WindowGroup(["c"]),
            new WindowGroup(["d"]), new WindowGroup(["e"]));

        var repaired = LayoutRepair.Repair(layout, new DockingCatalog(["a", "b", "c", "d", "e"], [], 240));

        Assert.Equal(4, repaired.Side(SideEdge.Right)!.Columns[0].Groups.Count);
        Assert.True(repaired.Side(SideEdge.Left)!.Columns[0].Groups[0].WindowIds[0] == "e");
    }

    [Fact]
    public void WithNoRoomAnywhere_AWindowCountsAsClosed()
    {
        var bothDocks = new DockingLayout(
            [
                new SideLayout(SideEdge.Left, SideKind.Dock, [new SideColumn(200, [new WindowGroup(["a"])])]),
                new SideLayout(SideEdge.Right, SideKind.Dock, [new SideColumn(200, [new WindowGroup(["b"])])]),
            ],
            [],
            []);

        var repaired = LayoutRepair.Repair(bothDocks, new DockingCatalog(["a", "b", "c"], [], 240));

        Assert.Equal("c", Assert.Single(repaired.ClosedWindows).WindowId);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-5)]
    [InlineData(0)]
    public void SillySizes_FallBackToSensibleOnes(double silly)
    {
        var layout = new DockingLayout(
            [new SideLayout(SideEdge.Right, SideKind.Widgets, [new SideColumn(silly, [new WindowGroup(["a"], silly)])])],
            [],
            []);

        var column = LayoutRepair.Repair(layout, _small).Side(SideEdge.Right)!.Columns[0];

        Assert.Equal(240, column.Width);
        Assert.Equal(1, column.Groups[0].Share);
    }

    [Fact]
    public void AStackOfNothingButTitles_OpensItsLastWindow()
    {
        var layout = Widgets(SideEdge.Right, new WindowGroup(["a"], IsTitleOnly: true), new WindowGroup(["b"], IsTitleOnly: true));

        var groups = LayoutRepair.Repair(layout, _small).Side(SideEdge.Right)!.Columns[0].Groups;

        Assert.True(groups[0].IsTitleOnly);
        Assert.False(groups[1].IsTitleOnly);
    }

    [Fact]
    public void AToolbarTheAppHas_AndIsMissing_GoesToItsDefaultPlace()
    {
        var repaired = LayoutRepair.Repair(_empty, _small);

        Assert.Equal(_small.DefaultToolbars, repaired.Toolbars);
    }

    [Fact]
    public void AToolbarTheAppNoLongerHas_IsSkipped()
    {
        var layout = _empty with
        {
            Toolbars = [new ToolbarPlacement("gone", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal)],
        };

        Assert.DoesNotContain(LayoutRepair.Repair(layout, _small).Toolbars, toolbar => toolbar.ToolbarId == "gone");
    }

    [Fact]
    public void TwoToolbarsOnOneAnchor_AreSeparated()
    {
        var layout = _empty with
        {
            Toolbars =
            [
                new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal),
                new ToolbarPlacement("edit", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal),
            ],
        };

        var anchors = LayoutRepair.Repair(layout, Catalog).Toolbars
            .Where(toolbar => toolbar.IsVisible)
            .Select(toolbar => toolbar.Anchor)
            .ToList();

        Assert.Equal(anchors.Count, anchors.Distinct().Count());
    }

    [Theory]
    [InlineData(ToolbarAnchor.TopMiddle, ToolbarOrientation.Horizontal)]
    [InlineData(ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal)]
    [InlineData(ToolbarAnchor.LeftMiddle, ToolbarOrientation.Vertical)]
    [InlineData(ToolbarAnchor.RightMiddle, ToolbarOrientation.Vertical)]
    public void AToolbarOnAnEdgeMiddle_FollowsItsEdge(ToolbarAnchor anchor, ToolbarOrientation expected)
    {
        var wrong = expected == ToolbarOrientation.Horizontal ? ToolbarOrientation.Vertical : ToolbarOrientation.Horizontal;
        var layout = _empty with { Toolbars = [new ToolbarPlacement("file", anchor, wrong)] };

        Assert.Equal(expected, LayoutRepair.Repair(layout, _small).Toolbars[0].Orientation);
    }

    [Fact]
    public void AToolbarOnACorner_KeepsItsDirection()
    {
        var layout = _empty with
        {
            Toolbars = [new ToolbarPlacement("file", ToolbarAnchor.BottomLeft, ToolbarOrientation.Vertical)],
        };

        Assert.Equal(ToolbarOrientation.Vertical, LayoutRepair.Repair(layout, _small).Toolbars[0].Orientation);
    }

    [Fact]
    public void AnyLayout_ComesOutKeepingTheRules()
    {
        Anything.Sample(layout =>
        {
            var repaired = LayoutRepair.Repair(layout, Catalog);

            LayoutEditsTests.AssertEveryWindowIsInOnePlace(repaired);
            Assert.All(repaired.Sides.Where(side => side.Kind == SideKind.Widgets), side =>
            {
                var column = Assert.Single(side.Columns);
                Assert.InRange(column.Groups.Count, 1, DockingLayout.MaximumWidgetsOnASide);
                Assert.All(column.Groups, group => Assert.Single(group.WindowIds));
            });
            Assert.All(repaired.Sides.SelectMany(side => side.Columns), column =>
            {
                Assert.True(column.Width > 0);
                Assert.Contains(column.Groups, group => !group.IsTitleOnly);
                Assert.All(column.Groups, group => Assert.True(group.Share > 0));
            });

            var anchors = repaired.Toolbars.Where(toolbar => toolbar.IsVisible).Select(toolbar => toolbar.Anchor).ToList();
            Assert.Equal(anchors.Count, anchors.Distinct().Count());
        });
    }

    [Fact]
    public void RepairingARepairedLayout_ChangesNothing()
    {
        Valid.Sample(layout => Assert.Equal(Text(layout), Text(LayoutRepair.Repair(layout, Catalog))));
    }
}
