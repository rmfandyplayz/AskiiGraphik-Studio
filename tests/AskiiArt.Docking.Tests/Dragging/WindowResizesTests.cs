// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Dragging;

public sealed class WindowResizesTests
{
    private const double Tolerance = 0.001;
    private const double Title = 28;
    private const double Minimum = 100;

    private static readonly LayoutSize _window = new(1000, 700);

    private static readonly DockingLayout _twoOnTheRight =
        Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]));

    [Fact]
    public void DraggingARightSidesInnerEdgeLeft_WidensIt()
    {
        var resize = new SideWidthResize(_twoOnTheRight, SideEdge.Right, Measure(_window), Metrics);

        var after = resize.LayoutAfter(new LayoutPoint(-50, 10));

        Assert.Equal(290, after.Side(SideEdge.Right)!.Columns[0].Width);
    }

    [Fact]
    public void DraggingALeftSidesInnerEdgeRight_WidensIt()
    {
        var layout = Widgets(SideEdge.Left, new WindowGroup(["a"]));
        var resize = new SideWidthResize(layout, SideEdge.Left, Measure(_window), Metrics);

        Assert.Equal(290, resize.LayoutAfter(new LayoutPoint(50, 0)).Side(SideEdge.Left)!.Columns[0].Width);
    }

    [Fact]
    public void ASide_StopsAtTheNarrowestWidth()
    {
        var resize = new SideWidthResize(_twoOnTheRight, SideEdge.Right, Measure(_window), Metrics);

        var after = resize.LayoutAfter(new LayoutPoint(5000, 0));

        Assert.Equal(Metrics.MinimumWindowWidth, after.Side(SideEdge.Right)!.Columns[0].Width);
    }

    [Fact]
    public void ASide_StopsBeforeTheCanvasRegionGetsTooNarrow()
    {
        var resize = new SideWidthResize(_twoOnTheRight, SideEdge.Right, Measure(_window), Metrics);

        var after = resize.LayoutAfter(new LayoutPoint(-5000, 0));

        var region = LayoutSolver.Solve(after, Measure(_window), Metrics).CanvasRegion;
        Assert.Equal(Metrics.MinimumCanvasRegionWidth, region.Width, Tolerance);
    }

    [Fact]
    public void ThatSidesWidth_LeavesTheOtherSideAlone()
    {
        var both = _twoOnTheRight with
        {
            Sides = [.. _twoOnTheRight.Sides, new SideLayout(SideEdge.Left, SideKind.Widgets, [new SideColumn(200, [new WindowGroup(["c"])])])],
        };
        var resize = new SideWidthResize(both, SideEdge.Right, Measure(_window), Metrics);

        Assert.Equal(200, resize.LayoutAfter(new LayoutPoint(-50, 0)).Side(SideEdge.Left)!.Columns[0].Width);
    }

    [Fact]
    public void DraggingAGapDown_GrowsTheWindowAbove_AndShrinksTheOneBelow()
    {
        var resize = new StackGapResize(_twoOnTheRight, SideEdge.Right, 0, Measure(_window), Metrics);
        var before = Lengths(_twoOnTheRight);

        var after = Lengths(resize.LayoutAfter(new LayoutPoint(40, 30)));

        Assert.Equal(before[0] + 30, after[0], Tolerance);
        Assert.Equal(before[1] - 30, after[1], Tolerance);
    }

    [Fact]
    public void DraggingAGapUp_GrowsTheWindowBelow_AndShrinksTheOneAbove()
    {
        var resize = new StackGapResize(_twoOnTheRight, SideEdge.Right, 0, Measure(_window), Metrics);
        var before = Lengths(_twoOnTheRight);

        var after = Lengths(resize.LayoutAfter(new LayoutPoint(0, -30)));

        Assert.Equal(before[0] - 30, after[0], Tolerance);
        Assert.Equal(before[1] + 30, after[1], Tolerance);
    }

    [Fact]
    public void AWindowStopsAtItsMinimum_ThenSnapsToItsTitle_WithNothingInBetween()
    {
        var entries = Entries((300, false), (300, false));
        var toTheMinimum = 300 - Minimum;
        var halfwayToTheTitle = (Minimum - Title) / 2;

        var atMinimum = StackLengths.AfterMovingGap(entries, 0, toTheMinimum, Title);
        Assert.Equal(Minimum, atMinimum[1].Length, Tolerance);

        var stuck = StackLengths.AfterMovingGap(entries, 0, toTheMinimum + halfwayToTheTitle - 1, Title);
        Assert.Equal(Minimum, stuck[1].Length, Tolerance);
        Assert.False(stuck[1].IsTitleOnly);

        var snapped = StackLengths.AfterMovingGap(entries, 0, toTheMinimum + halfwayToTheTitle, Title);
        Assert.Equal(Title, snapped[1].Length, Tolerance);
        Assert.True(snapped[1].IsTitleOnly);
        Assert.Equal(600 - Title, snapped[0].Length, Tolerance);
    }

    [Fact]
    public void OnceOneIsTitleOnly_TheNextOneGivesWay()
    {
        var entries = Entries((200, false), (Title, true), (300, false));

        var after = StackLengths.AfterMovingGap(entries, 0, 50, Title);

        Assert.Equal(250, after[0].Length, Tolerance);
        Assert.Equal(Title, after[1].Length, Tolerance);
        Assert.Equal(250, after[2].Length, Tolerance);
    }

    [Fact]
    public void WhenTheNearestHasSnapped_TheOneAfterIt_GivesWayToo()
    {
        var entries = Entries((200, false), (Minimum, false), (300, false));

        var after = StackLengths.AfterMovingGap(entries, 0, 100, Title);

        Assert.True(after[1].IsTitleOnly);
        Assert.Equal(300 - (100 - (Minimum - Title)), after[2].Length, Tolerance);
        Assert.Equal(300, after[0].Length, Tolerance);
    }

    [Fact]
    public void DraggingATitleOnlyWindowBack_SnapsItToItsMinimum()
    {
        var entries = Entries((Title, true), (500, false));
        var halfway = (Minimum - Title) / 2;

        var notYet = StackLengths.AfterMovingGap(entries, 0, halfway - 1, Title);
        Assert.True(notYet[0].IsTitleOnly);
        Assert.Equal(500, notYet[1].Length, Tolerance);

        var opened = StackLengths.AfterMovingGap(entries, 0, halfway, Title);
        Assert.False(opened[0].IsTitleOnly);
        Assert.Equal(Minimum, opened[0].Length, Tolerance);
        Assert.Equal(500 + Title - Minimum, opened[1].Length, Tolerance);
    }

    [Fact]
    public void ATitleOnlyWindowWithNoRoomToOpen_StaysTitleOnly()
    {
        var entries = Entries((Title, true), (Title, true));

        Assert.All(StackLengths.AfterMovingGap(entries, 0, 200, Title), entry => Assert.True(entry.IsTitleOnly));
    }

    [Fact]
    public void WhenEverythingElseIsTitleOnly_TheWindowStopsGrowing()
    {
        var entries = Entries((300, false), (Minimum, false));

        var after = StackLengths.AfterMovingGap(entries, 0, 2000, Title);

        Assert.True(after[1].IsTitleOnly);
        Assert.Equal(300 + Minimum - Title, after[0].Length, Tolerance);
    }

    [Fact]
    public void AnyGapDraggedAnyDistance_KeepsTheStacksLength_AndEveryWindowTitleOnlyOrAtLeastItsMinimum()
    {
        var lengths = Gen.Double[Minimum, 400].List[2, 4];

        Gen.Select(lengths, Gen.Int[0, 2], Gen.Double[-1000, 1000]).Sample((list, gap, moved) =>
        {
            var entries = list.Select(length => new StackLengths.Entry(length, false, Minimum)).ToList();

            var after = StackLengths.AfterMovingGap(entries, gap, moved, Title);

            Assert.Equal(entries.Sum(entry => entry.Length), after.Sum(entry => entry.Length), Tolerance);
            Assert.Contains(after, entry => !entry.IsTitleOnly);
            Assert.All(after, entry =>
            {
                if (entry.IsTitleOnly)
                    Assert.Equal(Title, entry.Length, Tolerance);
                else
                    Assert.True(entry.Length >= Minimum - Tolerance);
            });
        });
    }

    [Fact]
    public void AGapsResize_SolvesToExactlyWhatItAskedFor()
    {
        var three = Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]), new WindowGroup(["c"]));
        var before = Lengths(three);

        var after = Lengths(new StackGapResize(three, SideEdge.Right, 1, Measure(_window), Metrics).LayoutAfter(new LayoutPoint(0, -40)));

        Assert.Equal(before[0], after[0], Tolerance);
        Assert.Equal(before[1] - 40, after[1], Tolerance);
        Assert.Equal(before[2] + 40, after[2], Tolerance);
    }

    [Fact]
    public void DraggingAFloatingWindowsRightEdge_WidensIt()
    {
        var after = Resized(WindowEdges.Right, new LayoutPoint(50, 10));

        Assert.Equal(new FloatingWindow("x", new LayoutPoint(100, 100), 350, 300), after);
    }

    [Fact]
    public void DraggingAFloatingWindowsLeftEdge_MovesItAndWidensIt()
    {
        var after = Resized(WindowEdges.Left, new LayoutPoint(-50, 10));

        Assert.Equal(new FloatingWindow("x", new LayoutPoint(50, 100), 350, 300), after);
    }

    [Fact]
    public void DraggingACorner_MovesBothEdges()
    {
        var after = Resized(WindowEdges.Top | WindowEdges.Left, new LayoutPoint(10, 20));

        Assert.Equal(new FloatingWindow("x", new LayoutPoint(110, 120), 290, 280), after);
    }

    [Fact]
    public void AFloatingWindow_StopsAtTheNarrowestWidth()
    {
        Assert.Equal(Metrics.MinimumWindowWidth, Resized(WindowEdges.Right, new LayoutPoint(-2000, 0)).Width);

        var fromTheLeft = Resized(WindowEdges.Left, new LayoutPoint(2000, 0));
        Assert.Equal(Metrics.MinimumWindowWidth, fromTheLeft.Width);
        Assert.Equal(400 - Metrics.MinimumWindowWidth, fromTheLeft.Position.X);
    }

    [Fact]
    public void AFloatingWindow_CantBeResizedPastTheMarginInsideTheAppWindow()
    {
        var margin = Metrics.Margin;

        Assert.Equal(margin, Resized(WindowEdges.Left, new LayoutPoint(-5000, 0)).Position.X);
        Assert.Equal(_window.Height - margin - 100, Resized(WindowEdges.Bottom, new LayoutPoint(0, 5000)).Height);
        Assert.Equal(margin, Resized(WindowEdges.Top, new LayoutPoint(0, -5000)).Position.Y);
    }

    [Fact]
    public void AFloatingWindow_GetsNoWiderThanASidesWindowsCan()
    {
        var widest = _window.Width - Metrics.MinimumCanvasRegionWidth - Metrics.Margin;

        Assert.Equal(widest, Resized(WindowEdges.Right, new LayoutPoint(5000, 0)).Width);
    }

    [Fact]
    public void AFloatingWindowWiderThanTheWidest_DoesntJumpNarrower_ButCantGrow()
    {
        var wide = LayoutEdits.FloatWindow(new DockingLayout([], [], []), "x", new LayoutPoint(20, 100), 900, 300);
        var resize = new FloatingWindowResize(wide, "x", WindowEdges.Right, Measure(_window), Metrics);

        Assert.Equal(900, resize.LayoutAfter(new LayoutPoint(0, 0)).FloatingWindowNamed("x")!.Width);
        Assert.Equal(900, resize.LayoutAfter(new LayoutPoint(50, 0)).FloatingWindowNamed("x")!.Width);
        Assert.Equal(850, resize.LayoutAfter(new LayoutPoint(-50, 0)).FloatingWindowNamed("x")!.Width);
    }

    [Fact]
    public void AFloatingWindow_StopsAtItsMinimumHeight_ThenSnapsToItsTitle()
    {
        var toTheMinimum = Minimum - 300;
        var halfway = (Minimum - Title) / 2;

        var stuck = Resized(WindowEdges.Bottom, new LayoutPoint(0, toTheMinimum - halfway + 1));
        Assert.Equal(Minimum, stuck.Height);
        Assert.False(stuck.IsTitleOnly);

        var snapped = Resized(WindowEdges.Bottom, new LayoutPoint(0, toTheMinimum - halfway - 1));
        Assert.True(snapped.IsTitleOnly);

        // It remembers its height, but there's only a title to show.
        Assert.Equal(new LayoutPoint(100, 100), snapped.Position);
        Assert.Equal(300, snapped.Height);
    }

    [Fact]
    public void SnappingToItsTitleFromTheTopEdge_LeavesItsBottomEdgeWhereItWas()
    {
        var snapped = Resized(WindowEdges.Top, new LayoutPoint(0, 1000));

        Assert.True(snapped.IsTitleOnly);
        Assert.Equal(400 - Title, snapped.Position.Y);
    }

    [Fact]
    public void DraggingATitleOnlyFloatingWindowOpen_SnapsItToItsMinimum()
    {
        var layout = FloatingLayout(isTitleOnly: true);
        var resize = new FloatingWindowResize(layout, "x", WindowEdges.Bottom, Measure(_window), Metrics);
        var halfway = (Minimum - Title) / 2;

        Assert.True(resize.LayoutAfter(new LayoutPoint(0, halfway - 1)).FloatingWindowNamed("x")!.IsTitleOnly);

        var opened = resize.LayoutAfter(new LayoutPoint(0, halfway)).FloatingWindowNamed("x")!;
        Assert.False(opened.IsTitleOnly);
        Assert.Equal(Minimum, opened.Height);
    }

    [Fact]
    public void WideningATitleOnlyFloatingWindow_LeavesItTitleOnly()
    {
        var layout = FloatingLayout(isTitleOnly: true);
        var resize = new FloatingWindowResize(layout, "x", WindowEdges.Right, Measure(_window), Metrics);

        var after = resize.LayoutAfter(new LayoutPoint(40, 0)).FloatingWindowNamed("x")!;

        Assert.True(after.IsTitleOnly);
        Assert.Equal(340, after.Width);
    }

    [Fact]
    public void AnyResizeOfAFloatingWindow_KeepsItInside_AndAtLeastItsSmallestSize()
    {
        var edges = Gen.Int[1, 15];

        Gen.Select(edges, Gen.Double[-1500, 1500], Gen.Double[-1500, 1500], Gen.Bool).Sample((flags, x, y, isTitleOnly) =>
        {
            var layout = FloatingLayout(isTitleOnly);
            var resize = new FloatingWindowResize(layout, "x", (WindowEdges)flags, Measure(_window), Metrics);

            var after = resize.LayoutAfter(new LayoutPoint(x, y));
            var bounds = LayoutSolver.Solve(after, Measure(_window), Metrics).FloatingWindows.Single().Bounds;

            Assert.True(bounds.X >= -Tolerance && bounds.Right <= _window.Width + Tolerance);
            Assert.True(bounds.Y >= -Tolerance && bounds.Bottom <= _window.Height + Tolerance);
            Assert.True(bounds.Width >= Metrics.MinimumWindowWidth - Tolerance);
            Assert.True(bounds.Height >= Metrics.TitleLength - Tolerance);
        });
    }

    private static IReadOnlyList<StackLengths.Entry> Entries(params (double Length, bool IsTitleOnly)[] entries) =>
        [.. entries.Select(entry => new StackLengths.Entry(entry.Length, entry.IsTitleOnly, Minimum))];

    private static List<double> Lengths(DockingLayout layout) =>
    [
        .. LayoutSolver.Solve(layout, Measure(_window), Metrics).WindowGroups
            .OrderBy(group => group.Bounds.Y)
            .Select(group => group.Bounds.Height),
    ];

    private static DockingLayout FloatingLayout(bool isTitleOnly = false) =>
        LayoutEdits.FloatWindow(new DockingLayout([], [], []), "x", new LayoutPoint(100, 100), 300, 300, isTitleOnly);

    private static FloatingWindow Resized(WindowEdges edges, LayoutPoint moved)
    {
        var resize = new FloatingWindowResize(FloatingLayout(), "x", edges, Measure(_window), Metrics);
        return resize.LayoutAfter(moved).FloatingWindowNamed("x")!;
    }
}
