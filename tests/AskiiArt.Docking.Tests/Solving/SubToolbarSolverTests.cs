// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Solving;

public sealed class SubToolbarSolverTests
{
    private const double Tolerance = 0.001;
    private const double Gap = 8;
    private const double Margin = 8;

    private static readonly LayoutSize _window = new(1000, 700);
    private static readonly ToolbarShapes _short = new(new LayoutSize(100, 30), new LayoutSize(30, 100));
    private static readonly ToolbarShapes _long = new(new LayoutSize(900, 30), new LayoutSize(30, 900));

    [Theory]
    [InlineData(ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal, SubToolbarSide.BelowOrRight)]
    [InlineData(ToolbarAnchor.TopMiddle, ToolbarOrientation.Horizontal, SubToolbarSide.BelowOrRight)]
    [InlineData(ToolbarAnchor.TopRight, ToolbarOrientation.Horizontal, SubToolbarSide.BelowOrRight)]
    [InlineData(ToolbarAnchor.BottomLeft, ToolbarOrientation.Horizontal, SubToolbarSide.AboveOrLeft)]
    [InlineData(ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal, SubToolbarSide.AboveOrLeft)]
    [InlineData(ToolbarAnchor.BottomRight, ToolbarOrientation.Horizontal, SubToolbarSide.AboveOrLeft)]
    [InlineData(ToolbarAnchor.TopLeft, ToolbarOrientation.Vertical, SubToolbarSide.BelowOrRight)]
    [InlineData(ToolbarAnchor.LeftMiddle, ToolbarOrientation.Vertical, SubToolbarSide.BelowOrRight)]
    [InlineData(ToolbarAnchor.BottomLeft, ToolbarOrientation.Vertical, SubToolbarSide.BelowOrRight)]
    [InlineData(ToolbarAnchor.TopRight, ToolbarOrientation.Vertical, SubToolbarSide.AboveOrLeft)]
    [InlineData(ToolbarAnchor.RightMiddle, ToolbarOrientation.Vertical, SubToolbarSide.AboveOrLeft)]
    [InlineData(ToolbarAnchor.BottomRight, ToolbarOrientation.Vertical, SubToolbarSide.AboveOrLeft)]
    public void ItSitsOnTheCanvasSideOfItsParent_AGapAway(ToolbarAnchor anchor, ToolbarOrientation orientation,
        SubToolbarSide expected)
    {
        var solved = Solve([Toolbar("file", anchor, orientation)], "file", _short);

        var sub = solved.SubToolbar!;
        var parent = BoundsOf(solved, "file");
        Assert.Equal(expected, sub.Side);
        Assert.Equal(orientation, sub.Orientation);
        Assert.Equal(expected, ToolbarAnchors.CanvasSide(anchor, orientation));

        var distance = (orientation, expected) switch
        {
            (ToolbarOrientation.Horizontal, SubToolbarSide.BelowOrRight) => sub.Bounds.Y - parent.Bottom,
            (ToolbarOrientation.Horizontal, _) => parent.Y - sub.Bounds.Bottom,
            (_, SubToolbarSide.BelowOrRight) => sub.Bounds.X - parent.Right,
            _ => parent.X - sub.Bounds.Right,
        };
        Assert.Equal(Gap, distance, Tolerance);
    }

    [Fact]
    public void ACornerToolbars_IsAlignedToTheCorner()
    {
        var left = Solve([Toolbar("file", ToolbarAnchor.TopLeft)], "file", _short);
        var right = Solve([Toolbar("file", ToolbarAnchor.TopRight)], "file", _short);

        Assert.Equal(BoundsOf(left, "file").X, left.SubToolbar!.Bounds.X, Tolerance);
        Assert.Equal(BoundsOf(right, "file").Right, right.SubToolbar!.Bounds.Right, Tolerance);
    }

    [Fact]
    public void AVerticalCornerToolbars_IsAlignedToTheCorner()
    {
        var top = Solve([Toolbar("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Vertical)], "file", _short);
        var bottom = Solve([Toolbar("file", ToolbarAnchor.BottomLeft, ToolbarOrientation.Vertical)], "file", _short);

        Assert.Equal(BoundsOf(top, "file").Y, top.SubToolbar!.Bounds.Y, Tolerance);
        Assert.Equal(BoundsOf(bottom, "file").Bottom, bottom.SubToolbar!.Bounds.Bottom, Tolerance);
    }

    [Fact]
    public void AMiddleToolbars_IsCentredOnIt()
    {
        var solved = Solve([Toolbar("file", ToolbarAnchor.TopMiddle)], "file", _short);

        var parent = BoundsOf(solved, "file");
        var sub = solved.SubToolbar!.Bounds;
        Assert.Equal(parent.X + parent.Width / 2, sub.X + sub.Width / 2, Tolerance);
    }

    [Fact]
    public void ItKeepsTheLengthItWants_IfThatsAllowed()
    {
        var solved = Solve([Toolbar("file", ToolbarAnchor.TopLeft)], "file", _short);

        Assert.Equal(100, solved.SubToolbar!.Bounds.Width, Tolerance);
    }

    [Fact]
    public void ItGetsAtMostHalfAgainItsParentsLength_WhenItsParentHasRoom()
    {
        var solved = Solve([Toolbar("file", ToolbarAnchor.TopLeft)], "file", _long);

        Assert.Equal(BoundsOf(solved, "file").Width * 1.5, solved.SubToolbar!.Bounds.Width, Tolerance);
    }

    [Fact]
    public void ItGetsAtMostItsParentsLength_WhenItsParentIsFightingForSpace()
    {
        var sizes = new Dictionary<string, LayoutSize>(StringComparer.Ordinal)
        {
            ["file"] = new(800, 40),
            ["edit"] = new(800, 40),
        };
        var layout = Layout(Toolbar("file", ToolbarAnchor.TopLeft), Toolbar("edit", ToolbarAnchor.TopRight));
        var measurements = Measure(_window, sizes) with { SubToolbar = new SubToolbarRequest("file", _long) };

        var solved = LayoutSolver.Solve(layout, measurements, Metrics);

        var parent = BoundsOf(solved, "file");
        Assert.True(parent.Width < 800);
        Assert.Equal(parent.Width, solved.SubToolbar!.Bounds.Width, Tolerance);
    }

    [Fact]
    public void ItPushesAToolbarOnTheNeighbouringEdge()
    {
        // The owner's mockup: top-right horizontal, right-middle vertical.
        var sizes = new Dictionary<string, LayoutSize>(StringComparer.Ordinal)
        {
            ["file"] = new(150, 40),
            ["edit"] = new(40, 620),
        };
        var layout = Layout(Toolbar("file", ToolbarAnchor.TopRight),
            Toolbar("edit", ToolbarAnchor.RightMiddle, ToolbarOrientation.Vertical));

        var without = LayoutSolver.Solve(layout, Measure(_window, sizes), Metrics);
        var with = LayoutSolver.Solve(layout,
            Measure(_window, sizes) with { SubToolbar = new SubToolbarRequest("file", _short) }, Metrics);

        var sub = with.SubToolbar!.Bounds;
        var pushed = BoundsOf(with, "edit");
        Assert.Equal(sub.Bottom + Gap, pushed.Y, Tolerance);
        Assert.True(pushed.Y > BoundsOf(without, "edit").Y);
    }

    [Fact]
    public void ThePushedToolbar_OnlyShortensIfItNoLongerFits()
    {
        var sizes = new Dictionary<string, LayoutSize>(StringComparer.Ordinal)
        {
            ["file"] = new(150, 40),
            ["edit"] = new(40, 300),
        };
        var layout = Layout(Toolbar("file", ToolbarAnchor.TopRight),
            Toolbar("edit", ToolbarAnchor.RightMiddle, ToolbarOrientation.Vertical));
        var fits = LayoutSolver.Solve(layout,
            Measure(_window, sizes) with { SubToolbar = new SubToolbarRequest("file", _short) }, Metrics);
        Assert.Equal(300, BoundsOf(fits, "edit").Height, Tolerance);

        sizes["edit"] = new LayoutSize(40, 680);
        var tooLong = LayoutSolver.Solve(layout,
            Measure(_window, sizes) with { SubToolbar = new SubToolbarRequest("file", _short) }, Metrics);

        Assert.True(BoundsOf(tooLong, "edit").Height < 680);
        Assert.Equal(_window.Height - Margin, BoundsOf(tooLong, "edit").Bottom, Tolerance);
    }

    [Fact]
    public void AVerticalParents_PushesTheTopEdgesToolbars()
    {
        var sizes = new Dictionary<string, LayoutSize>(StringComparer.Ordinal)
        {
            ["file"] = new(40, 150),
            ["edit"] = new(900, 40),
        };
        var layout = Layout(Toolbar("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Vertical),
            Toolbar("edit", ToolbarAnchor.TopMiddle));

        var solved = LayoutSolver.Solve(layout,
            Measure(_window, sizes) with { SubToolbar = new SubToolbarRequest("file", _short) }, Metrics);

        var sub = solved.SubToolbar!.Bounds;
        Assert.True(sub.X > BoundsOf(solved, "file").Right);
        Assert.Equal(sub.Right + Gap, BoundsOf(solved, "edit").X, Tolerance);
    }

    [Fact]
    public void ItStaysOutOfACornerThatsBeenTaken()
    {
        var sizes = new Dictionary<string, LayoutSize>(StringComparer.Ordinal)
        {
            ["file"] = new(800, 40),
            ["edit"] = new(40, 300),
        };
        var layout = Layout(Toolbar("file", ToolbarAnchor.TopMiddle),
            Toolbar("edit", ToolbarAnchor.TopRight, ToolbarOrientation.Vertical));

        var solved = LayoutSolver.Solve(layout,
            Measure(_window, sizes) with { SubToolbar = new SubToolbarRequest("file", _long) }, Metrics);

        Assert.True(solved.SubToolbar!.Bounds.Right < BoundsOf(solved, "edit").X);
    }

    [Fact]
    public void ItGoesPastAThickerNeighbourItWouldOverlap()
    {
        var sizes = new Dictionary<string, LayoutSize>(StringComparer.Ordinal)
        {
            ["file"] = new(200, 40),
            ["edit"] = new(200, 80),
        };
        var layout = Layout(Toolbar("file", ToolbarAnchor.TopLeft), Toolbar("edit", ToolbarAnchor.TopMiddle));
        var wide = new ToolbarShapes(new LayoutSize(290, 30), new LayoutSize(30, 290));
        var narrowWindow = new LayoutSize(600, 700);

        var solved = LayoutSolver.Solve(layout,
            Measure(narrowWindow, sizes) with { SubToolbar = new SubToolbarRequest("file", wide) }, Metrics);

        Assert.False(solved.SubToolbar!.Bounds.Intersects(BoundsOf(solved, "edit")));
    }

    [Fact]
    public void ItNeverMovesTheCanvas()
    {
        var layout = Layout(Toolbar("file", ToolbarAnchor.TopLeft), Toolbar("view", ToolbarAnchor.BottomRight));

        var without = LayoutSolver.Solve(layout, Measure(_window), Metrics);
        var with = LayoutSolver.Solve(layout,
            Measure(_window) with { SubToolbar = new SubToolbarRequest("file", _long) }, Metrics);

        Assert.NotNull(with.SubToolbar);
        Assert.Equal(without.SafeArea, with.SafeArea);
    }

    [Fact]
    public void AHiddenParentHasNone()
    {
        var solved = Solve([Toolbar("file", ToolbarAnchor.TopLeft) with { IsVisible = false }], "file", _short);

        Assert.Null(solved.SubToolbar);
    }

    [Fact]
    public void ThereIsNoneWithoutARequest()
    {
        var solved = LayoutSolver.Solve(Layout(Toolbar("file", ToolbarAnchor.TopLeft)), Measure(_window), Metrics);

        Assert.Null(solved.SubToolbar);
    }

    [Fact]
    public void AFloatingToolbars_GoesOnTheSideItRemembers()
    {
        var above = Floating(new LayoutPoint(300, 300), SubToolbarSide.AboveOrLeft);
        var below = Floating(new LayoutPoint(300, 300), SubToolbarSide.BelowOrRight);

        var solvedAbove = Solve([above], "file", _short);
        var solvedBelow = Solve([below], "file", _short);

        Assert.Equal(BoundsOf(solvedAbove, "file").Y - Gap, solvedAbove.SubToolbar!.Bounds.Bottom, Tolerance);
        Assert.Equal(BoundsOf(solvedBelow, "file").Bottom + Gap, solvedBelow.SubToolbar!.Bounds.Y, Tolerance);
    }

    [Fact]
    public void AVerticalFloatingToolbars_GoesLeftOrRight()
    {
        var left = Floating(new LayoutPoint(300, 300), SubToolbarSide.AboveOrLeft) with { Orientation = ToolbarOrientation.Vertical };
        var right = left with { SubToolbarSide = SubToolbarSide.BelowOrRight };

        var solvedLeft = Solve([left], "file", _short);
        var solvedRight = Solve([right], "file", _short);

        Assert.Equal(BoundsOf(solvedLeft, "file").X - Gap, solvedLeft.SubToolbar!.Bounds.Right, Tolerance);
        Assert.Equal(BoundsOf(solvedRight, "file").Right + Gap, solvedRight.SubToolbar!.Bounds.X, Tolerance);
    }

    [Fact]
    public void AFloatingToolbars_FlipsWhenThereIsNoRoom_WithoutForgettingTheSide()
    {
        var atTheBottom = Floating(new LayoutPoint(300, 650), SubToolbarSide.BelowOrRight);
        var inTheMiddle = atTheBottom with { FloatingAt = new LayoutPoint(300, 300) };

        var flipped = Solve([atTheBottom], "file", _short).SubToolbar!;
        var backAgain = Solve([inTheMiddle], "file", _short).SubToolbar!;

        Assert.Equal(SubToolbarSide.AboveOrLeft, flipped.Side);
        Assert.Equal(SubToolbarSide.BelowOrRight, backAgain.Side);
    }

    [Fact]
    public void AFloatingToolbars_StaysInsideAlongItsLength()
    {
        var solved = Solve([Floating(new LayoutPoint(-200, 300), SubToolbarSide.BelowOrRight)], "file", _long);

        Assert.Equal(Margin, solved.SubToolbar!.Bounds.X, Tolerance);
    }

    [Fact]
    public void AFloatingToolbars_MovesNothing()
    {
        var other = Toolbar("edit", ToolbarAnchor.TopMiddle);
        var parent = Floating(new LayoutPoint(400, 8), SubToolbarSide.BelowOrRight);

        var without = LayoutSolver.Solve(Layout(parent, other), Measure(_window), Metrics);
        var with = LayoutSolver.Solve(Layout(parent, other),
            Measure(_window) with { SubToolbar = new SubToolbarRequest("file", _long) }, Metrics);

        Assert.Equal(BoundsOf(without, "edit"), BoundsOf(with, "edit"));
    }

    [Fact]
    public void WithNoRoomEitherWay_ItIsLeftOut()
    {
        var shortWindow = new LayoutSize(1000, 60);

        var solved = LayoutSolver.Solve(Layout(Toolbar("file", ToolbarAnchor.TopLeft)),
            Measure(shortWindow) with { SubToolbar = new SubToolbarRequest("file", _short) }, Metrics);

        Assert.Null(solved.SubToolbar);
    }

    [Fact]
    public void ForAnyLayout_ItStaysInside_OverlapsNoAnchoredToolbar_AndLeavesTheCanvasAlone()
    {
        var names = Gen.OneOfConst("file", "tools", "edit", "view");

        Gen.Select(Valid, names, WindowSizes, Gen.Double[20, 2000], Gen.Double[20, 2000]).Sample(
            (layout, parent, window, length, subLength) =>
            {
                var sizes = Sizes(layout, _ => length);
                var shapes = new ToolbarShapes(new LayoutSize(subLength, 40), new LayoutSize(40, subLength));

                var without = LayoutSolver.Solve(layout, Measure(window, sizes), Metrics);
                var with = LayoutSolver.Solve(layout,
                    Measure(window, sizes) with { SubToolbar = new SubToolbarRequest(parent, shapes) }, Metrics);

                Assert.Equal(without.SafeArea, with.SafeArea);

                var anchored = with.Toolbars.Where(toolbar => toolbar.Anchor is not null).ToList();
                foreach (var toolbar in anchored)
                    Assert.DoesNotContain(anchored, other => other != toolbar && other.Bounds.Intersects(toolbar.Bounds));

                if (with.SubToolbar is not { } sub)
                    return;

                var inner = new LayoutRect(with.CanvasRegion.X + Metrics.Margin, with.CanvasRegion.Y + Metrics.Margin,
                    with.CanvasRegion.Width - Metrics.Margin * 2, with.CanvasRegion.Height - Metrics.Margin * 2);

                Assert.True(sub.Bounds.X >= inner.X - Tolerance && sub.Bounds.Right <= inner.Right + Tolerance);
                Assert.True(sub.Bounds.Y >= inner.Y - Tolerance && sub.Bounds.Bottom <= inner.Bottom + Tolerance);

                if (anchored.Any(toolbar => toolbar.ToolbarId == parent))
                    Assert.DoesNotContain(anchored, toolbar => toolbar.Bounds.Intersects(sub.Bounds));
            });
    }

    private static ToolbarPlacement Toolbar(string toolbarId, ToolbarAnchor anchor,
        ToolbarOrientation orientation = ToolbarOrientation.Horizontal) =>
        new(toolbarId, anchor, orientation);

    private static ToolbarPlacement Floating(LayoutPoint at, SubToolbarSide side) =>
        new("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal, FloatingAt: at, SubToolbarSide: side);

    private static DockingLayout Layout(params ToolbarPlacement[] toolbars) => new([], toolbars, []);

    private static SolvedLayout Solve(IReadOnlyList<ToolbarPlacement> toolbars, string parent, ToolbarShapes shapes)
    {
        var layout = new DockingLayout([], toolbars, []);
        var sizes = Sizes(layout, _ => 200);
        return LayoutSolver.Solve(layout,
            Measure(_window, sizes) with { SubToolbar = new SubToolbarRequest(parent, shapes) }, Metrics);
    }

    private static LayoutRect BoundsOf(SolvedLayout solved, string toolbarId) =>
        solved.Toolbars.Single(toolbar => toolbar.ToolbarId == toolbarId).Bounds;
}
