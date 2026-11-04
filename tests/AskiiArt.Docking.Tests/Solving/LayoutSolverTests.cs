// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Solving;

public sealed class LayoutSolverTests
{
    private const double Tolerance = 0.001;

    private static readonly LayoutSize _window = new(1280, 720);

    private static DockingLayout WithToolbars(DockingLayout layout, params ToolbarPlacement[] toolbars) =>
        layout with { Toolbars = toolbars };

    [Fact]
    public void AWidgetStack_FillsItsSideBetweenTheMargins()
    {
        var layout = Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]));

        var solved = LayoutSolver.Solve(layout, Measure(_window), Metrics);

        var top = solved.WindowGroups[0].Bounds;
        var bottom = solved.WindowGroups[1].Bounds;
        Assert.Equal(new LayoutRect(1280 - 8 - 240, 8, 240, 348), top);
        Assert.Equal(new LayoutRect(1280 - 8 - 240, 364, 240, 348), bottom);
    }

    [Fact]
    public void ADockSide_TouchesTheEdges_WithNoGaps()
    {
        var layout = new DockingLayout(
            [
                new SideLayout(SideEdge.Left, SideKind.Dock,
                [
                    new SideColumn(200, [new WindowGroup(["a"]), new WindowGroup(["b"])]),
                    new SideColumn(100, [new WindowGroup(["c"])]),
                ]),
            ],
            [],
            []);

        var solved = LayoutSolver.Solve(layout, Measure(_window), Metrics);

        Assert.Equal(new LayoutRect(0, 0, 200, 360), solved.WindowGroups[0].Bounds);
        Assert.Equal(new LayoutRect(0, 360, 200, 360), solved.WindowGroups[1].Bounds);
        Assert.Equal(new LayoutRect(200, 0, 100, 720), solved.WindowGroups[2].Bounds);
        Assert.Equal(300, solved.CanvasRegion.X);
    }

    [Theory]
    [InlineData(ToolbarAnchor.TopLeft, 8, 8)]
    [InlineData(ToolbarAnchor.TopMiddle, 590, 8)]
    [InlineData(ToolbarAnchor.TopRight, 1172, 8)]
    [InlineData(ToolbarAnchor.BottomLeft, 8, 672)]
    [InlineData(ToolbarAnchor.BottomMiddle, 590, 672)]
    [InlineData(ToolbarAnchor.BottomRight, 1172, 672)]
    public void AHorizontalToolbar_SitsOnItsAnchor_InsideTheMargin(ToolbarAnchor anchor, double x, double y)
    {
        var layout = WithToolbars(new DockingLayout([], [], []),
            new ToolbarPlacement("tools", anchor, ToolbarOrientation.Horizontal));

        var toolbar = Assert.Single(LayoutSolver.Solve(layout, Measure(_window, One("tools", 100, 40)), Metrics).Toolbars);

        Assert.Equal(new LayoutRect(x, y, 100, 40), toolbar.Bounds);
    }

    [Theory]
    [InlineData(ToolbarAnchor.TopLeft, 8, 8)]
    [InlineData(ToolbarAnchor.BottomLeft, 8, 612)]
    [InlineData(ToolbarAnchor.LeftMiddle, 8, 310)]
    [InlineData(ToolbarAnchor.TopRight, 1232, 8)]
    [InlineData(ToolbarAnchor.BottomRight, 1232, 612)]
    [InlineData(ToolbarAnchor.RightMiddle, 1232, 310)]
    public void AVerticalToolbar_SitsOnItsAnchor_InsideTheMargin(ToolbarAnchor anchor, double x, double y)
    {
        var layout = WithToolbars(new DockingLayout([], [], []),
            new ToolbarPlacement("tools", anchor, ToolbarOrientation.Vertical));

        var toolbar = Assert.Single(LayoutSolver.Solve(layout, Measure(_window, One("tools", 40, 100)), Metrics).Toolbars);

        Assert.Equal(new LayoutRect(x, y, 40, 100), toolbar.Bounds);
    }

    [Fact]
    public void AMiddleToolbar_StaysCentred_WhileThereIsRoom()
    {
        var layout = TopEdge(("a", ToolbarAnchor.TopLeft), ("b", ToolbarAnchor.TopMiddle), ("c", ToolbarAnchor.TopRight));
        var sizes = Lengths(("a", 100), ("b", 100), ("c", 100));

        var toolbars = LayoutSolver.Solve(layout, Measure(_window, sizes), Metrics).Toolbars;

        Assert.Equal(590, Named(toolbars, "b").X);
    }

    [Fact]
    public void AMiddleToolbar_MovesAside_WhenACornerToolbarReachesIt()
    {
        var layout = TopEdge(("a", ToolbarAnchor.TopLeft), ("b", ToolbarAnchor.TopMiddle));
        var sizes = Lengths(("a", 700), ("b", 100));

        var toolbars = LayoutSolver.Solve(layout, Measure(_window, sizes), Metrics).Toolbars;

        Assert.Equal(new LayoutRect(8, 8, 700, 40), Named(toolbars, "a"));
        Assert.Equal(new LayoutRect(716, 8, 100, 40), Named(toolbars, "b"));
    }

    [Fact]
    public void ToolbarsTooLongForTheirEdge_ShareIt_AndTheMiddleOneGetsABitMore()
    {
        var layout = TopEdge(("a", ToolbarAnchor.TopLeft), ("b", ToolbarAnchor.TopMiddle), ("c", ToolbarAnchor.TopRight));
        var sizes = Lengths(("a", 2000), ("b", 2000), ("c", 2000));

        var toolbars = LayoutSolver.Solve(layout, Measure(_window, sizes), Metrics).Toolbars;

        var (a, b, c) = (Named(toolbars, "a"), Named(toolbars, "b"), Named(toolbars, "c"));
        Assert.Equal(a.Width, c.Width, tolerance: Tolerance);
        Assert.Equal(a.Width * 1.25, b.Width, tolerance: Tolerance);
        Assert.Equal(a.Right + 8, b.X, tolerance: Tolerance);
        Assert.Equal(b.Right + 8, c.X, tolerance: Tolerance);
        Assert.Equal(1272, c.Right, tolerance: Tolerance);
    }

    [Fact]
    public void AShortToolbar_KeepsItsSize_AndTheLongOnesShareTheRest()
    {
        var layout = TopEdge(("a", ToolbarAnchor.TopLeft), ("b", ToolbarAnchor.TopMiddle), ("c", ToolbarAnchor.TopRight));
        var sizes = Lengths(("a", 100), ("b", 2000), ("c", 2000));

        var toolbars = LayoutSolver.Solve(layout, Measure(_window, sizes), Metrics).Toolbars;

        var (a, b, c) = (Named(toolbars, "a"), Named(toolbars, "b"), Named(toolbars, "c"));
        Assert.Equal(100, a.Width);
        Assert.Equal(c.Width * 1.25, b.Width, tolerance: Tolerance);
    }

    [Fact]
    public void AToolbarOnTheNeighbouringEdge_StartsAfterACornerToolbar()
    {
        // The owner's mockup: a top-right horizontal toolbar pushes a right-middle vertical one down.
        var layout = new DockingLayout([],
        [
            new ToolbarPlacement("corner", ToolbarAnchor.TopRight, ToolbarOrientation.Horizontal),
            new ToolbarPlacement("middle", ToolbarAnchor.RightMiddle, ToolbarOrientation.Vertical),
        ], []);
        var sizes = new Dictionary<string, LayoutSize> { ["corner"] = new(300, 40), ["middle"] = new(40, 2000) };

        var toolbars = LayoutSolver.Solve(layout, Measure(_window, sizes), Metrics).Toolbars;

        var (corner, middle) = (Named(toolbars, "corner"), Named(toolbars, "middle"));
        Assert.Equal(corner.Bottom + 8, middle.Y, tolerance: Tolerance);
        Assert.Equal(712, middle.Bottom, tolerance: Tolerance);
    }

    [Fact]
    public void AVerticalCornerToolbar_PushesTheHorizontalEdgeAlong()
    {
        // The owner's other mockup: a bottom-left vertical toolbar pushes the bottom-middle one right.
        var layout = new DockingLayout([],
        [
            new ToolbarPlacement("corner", ToolbarAnchor.BottomLeft, ToolbarOrientation.Vertical),
            new ToolbarPlacement("middle", ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal),
        ], []);
        var sizes = new Dictionary<string, LayoutSize> { ["corner"] = new(40, 300), ["middle"] = new(2000, 40) };

        var toolbars = LayoutSolver.Solve(layout, Measure(_window, sizes), Metrics).Toolbars;

        Assert.Equal(Named(toolbars, "corner").Right + 8, Named(toolbars, "middle").X, tolerance: Tolerance);
    }

    [Fact]
    public void TwoMiddleToolbarsReachingTheSameCorner_DontOverlap_AndTheHorizontalOneGetsIt()
    {
        var layout = new DockingLayout([],
        [
            new ToolbarPlacement("top", ToolbarAnchor.TopMiddle, ToolbarOrientation.Horizontal),
            new ToolbarPlacement("left", ToolbarAnchor.LeftMiddle, ToolbarOrientation.Vertical),
        ], []);
        var sizes = new Dictionary<string, LayoutSize> { ["top"] = new(5000, 40), ["left"] = new(40, 5000) };

        var toolbars = LayoutSolver.Solve(layout, Measure(_window, sizes), Metrics).Toolbars;

        Assert.Equal(8, Named(toolbars, "top").X);
        Assert.Equal(Named(toolbars, "top").Bottom + 8, Named(toolbars, "left").Y, tolerance: Tolerance);
    }

    [Fact]
    public void WithNoRoomForEveryToolbar_CornersGoFirst_AndComeBackWithTheRoom()
    {
        var layout = TopEdge(("a", ToolbarAnchor.TopLeft), ("b", ToolbarAnchor.TopMiddle), ("c", ToolbarAnchor.TopRight));
        var sizes = Lengths(("a", 2000), ("b", 2000), ("c", 2000));
        var cramped = new DockingMetrics(8, 8, 28, 100, 0, 8, MinimumToolbarLength: 150, 1.25);

        var narrow = LayoutSolver.Solve(layout, Measure(new LayoutSize(340, 700), sizes), cramped).Toolbars;
        var wide = LayoutSolver.Solve(layout, Measure(new LayoutSize(1000, 700), sizes), cramped).Toolbars;

        Assert.Equal(["a", "b"], narrow.Select(toolbar => toolbar.ToolbarId).Order(StringComparer.Ordinal));
        Assert.Equal(3, wide.Count);
    }

    [Fact]
    public void Toolbars_LiveBetweenTheSides()
    {
        var layout = WithToolbars(Widgets(SideEdge.Right, new WindowGroup(["a"])),
            new ToolbarPlacement("view", ToolbarAnchor.BottomRight, ToolbarOrientation.Horizontal));

        var solved = LayoutSolver.Solve(layout, Measure(_window), Metrics);

        var toolbar = Assert.Single(solved.Toolbars).Bounds;
        Assert.Equal(solved.WindowGroups[0].Bounds.X - 8, toolbar.Right, tolerance: Tolerance);
    }

    [Fact]
    public void AToolbarAndAWidget_HaveTheSameTopAndBottomEdges()
    {
        var layout = WithToolbars(Widgets(SideEdge.Right, new WindowGroup(["a"])),
            new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal),
            new ToolbarPlacement("view", ToolbarAnchor.BottomRight, ToolbarOrientation.Horizontal));

        var solved = LayoutSolver.Solve(layout, Measure(_window), Metrics);

        var widget = solved.WindowGroups[0].Bounds;
        Assert.Equal(widget.Y, solved.Toolbars[0].Bounds.Y);
        Assert.Equal(widget.Bottom, solved.Toolbars[1].Bounds.Bottom);
    }

    [Fact]
    public void TheSafeArea_CountsSides_AndToolbarsAlongTheirOwnEdge()
    {
        var layout = WithToolbars(Widgets(SideEdge.Right, new WindowGroup(["a"])),
            new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal),
            new ToolbarPlacement("tools", ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal));

        var safeArea = LayoutSolver.Solve(layout, Measure(_window), Metrics).SafeArea;

        Assert.Equal(new LayoutInsets(0, 48, 248, 48), safeArea);
    }

    [Theory]
    [InlineData(ToolbarAnchor.TopLeft, 48, 0)]
    [InlineData(ToolbarAnchor.BottomLeft, 48, 0)]
    [InlineData(ToolbarAnchor.LeftMiddle, 48, 0)]
    [InlineData(ToolbarAnchor.TopRight, 0, 48)]
    [InlineData(ToolbarAnchor.RightMiddle, 0, 48)]
    public void AVerticalToolbar_TakesFromTheSideItLiesAlong(ToolbarAnchor anchor, double left, double right)
    {
        var layout = WithToolbars(new DockingLayout([], [], []),
            new ToolbarPlacement("tools", anchor, ToolbarOrientation.Vertical));

        var safeArea = LayoutSolver.Solve(layout, Measure(_window, One("tools", 40, 100)), Metrics).SafeArea;

        Assert.Equal(new LayoutInsets(left, 0, right, 0), safeArea);
    }

    [Fact]
    public void AHiddenToolbar_TakesNothing()
    {
        var layout = WithToolbars(new DockingLayout([], [], []),
            new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal, IsVisible: false));

        var solved = LayoutSolver.Solve(layout, Measure(_window), Metrics);

        Assert.Empty(solved.Toolbars);
        Assert.Equal(default, solved.SafeArea);
    }

    [Fact]
    public void SidesTooWideForTheWindow_ShrinkTogether_AndLeaveTheCanvasRegionItsMinimum()
    {
        var layout = new DockingLayout(
            [
                new SideLayout(SideEdge.Left, SideKind.Dock, [new SideColumn(1000, [new WindowGroup(["a"])])]),
                new SideLayout(SideEdge.Right, SideKind.Dock, [new SideColumn(1000, [new WindowGroup(["b"])])]),
            ],
            [],
            []);

        var solved = LayoutSolver.Solve(layout, Measure(new LayoutSize(1000, 700)), Metrics);

        Assert.Equal(Metrics.MinimumCanvasRegionWidth, solved.CanvasRegion.Width, tolerance: Tolerance);
        Assert.Equal(340, solved.WindowGroups[0].Bounds.Width, tolerance: Tolerance);
        Assert.Equal(340, solved.WindowGroups[1].Bounds.Width, tolerance: Tolerance);
    }

    [Fact]
    public void Solving_DoesntChangeWhatTheUserAskedFor()
    {
        var layout = Widgets(SideEdge.Right, new WindowGroup(["a"], Share: 2), new WindowGroup(["b"]));
        var before = Text(layout);

        LayoutSolver.Solve(layout, Measure(new LayoutSize(400, 100)), Metrics);

        Assert.Equal(before, Text(layout));
    }

    [Fact]
    public void AnyLayoutAndWindow_KeepTheRules()
    {
        Gen.Select(Valid, WindowSizes, Gen.Double[30, 3000].Array[4]).Sample((layout, size, lengths) =>
        {
            var sizes = Sizes(layout, id => lengths[Array.IndexOf(["file", "tools", "edit", "view"], id)]);
            var solved = LayoutSolver.Solve(layout, Measure(size, sizes), Metrics);
            var windows = solved.WindowGroups.Select(group => group.Bounds).ToList();
            var toolbars = solved.Toolbars.Select(toolbar => toolbar.Bounds).ToList();

            Assert.All(windows.Concat(toolbars), bounds => AssertInside(bounds, size));
            AssertNoneOverlap(windows);
            AssertNoneOverlap(toolbars);

            // Toolbars stay in the canvas region, so they never touch a window.
            Assert.All(toolbars, toolbar =>
            {
                Assert.True(toolbar.X >= solved.CanvasRegion.X - Tolerance);
                Assert.True(toolbar.Right <= solved.CanvasRegion.Right + Tolerance);
            });

            // ...and never pass a window's top or bottom edge (docs/DOCKING.md §2).
            foreach (var window in windows.Where(window => window.Width > 0))
            {
                Assert.All(toolbars, toolbar => Assert.True(toolbar.Y >= StackTop(solved, window) - Tolerance));
                Assert.All(toolbars, toolbar => Assert.True(toolbar.Bottom <= StackBottom(solved, window) + Tolerance));
            }
        });
    }

    private static DockingLayout TopEdge(params (string Id, ToolbarAnchor Anchor)[] toolbars) => new(
        [],
        [.. toolbars.Select(toolbar => new ToolbarPlacement(toolbar.Id, toolbar.Anchor, ToolbarOrientation.Horizontal))],
        []);

    private static Dictionary<string, LayoutSize> One(string id, double width, double height) =>
        new() { [id] = new LayoutSize(width, height) };

    private static Dictionary<string, LayoutSize> Lengths(params (string Id, double Length)[] toolbars) =>
        toolbars.ToDictionary(toolbar => toolbar.Id, toolbar => new LayoutSize(toolbar.Length, 40));

    private static LayoutRect Named(IReadOnlyList<SolvedToolbar> toolbars, string id) =>
        toolbars.Single(toolbar => toolbar.ToolbarId == id).Bounds;

    private static double StackTop(SolvedLayout solved, LayoutRect window) =>
        solved.WindowGroups.Where(group => group.Bounds.X == window.X).Min(group => group.Bounds.Y);

    private static double StackBottom(SolvedLayout solved, LayoutRect window) =>
        solved.WindowGroups.Where(group => group.Bounds.X == window.X).Max(group => group.Bounds.Bottom);

    private static void AssertInside(LayoutRect bounds, LayoutSize size)
    {
        Assert.True(bounds.X >= -Tolerance && bounds.Y >= -Tolerance, $"{bounds} starts outside {size}.");
        Assert.True(bounds.Right <= size.Width + Tolerance && bounds.Bottom <= size.Height + Tolerance,
            $"{bounds} ends outside {size}.");
        Assert.True(bounds.Width >= 0 && bounds.Height >= 0, $"{bounds} has a negative size.");
    }

    private static void AssertNoneOverlap(List<LayoutRect> rectangles)
    {
        for (var first = 0; first < rectangles.Count; first++)
        {
            for (var second = first + 1; second < rectangles.Count; second++)
            {
                var (a, b) = (rectangles[first], rectangles[second]);
                var overlaps = a.X < b.Right - Tolerance && b.X < a.Right - Tolerance &&
                               a.Y < b.Bottom - Tolerance && b.Y < a.Bottom - Tolerance;
                Assert.False(overlaps, $"{a} overlaps {b}.");
            }
        }
    }
}
