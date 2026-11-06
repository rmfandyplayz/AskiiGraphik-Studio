// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Dragging;

/// <summary>Double-clicking a window's title squeezes it to its title, or opens it again.</summary>
public sealed class TitleOnlyToggleTests
{
    private const double Tolerance = 0.001;

    private static readonly LayoutSize _window = new(1000, 700);

    // The right side's stack is 700 less a margin of 8 at each end, and its gaps are 8.
    private const double StackLength = 684;

    [Fact]
    public void AFloatingWindow_SqueezesToItsTitle_AndOpensAgainAtTheSameHeight()
    {
        var floating = LayoutEdits.FloatWindow(new DockingLayout([], [], []), "x", new LayoutPoint(100, 100), 300, 250);

        var squeezed = Toggle(floating, "x");
        Assert.True(squeezed.FloatingWindowNamed("x")!.IsTitleOnly);

        var opened = Toggle(squeezed, "x");
        Assert.False(opened.FloatingWindowNamed("x")!.IsTitleOnly);
        Assert.Equal(250, opened.FloatingWindowNamed("x")!.Height);
    }

    [Fact]
    public void AWidget_SqueezesToItsTitle_RememberingHowLongItWas_AndTheOthersTakeTheRoom()
    {
        var stack = Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]));
        var half = (StackLength - Metrics.WidgetGap) / 2;

        var squeezed = Toggle(stack, "a");

        var group = squeezed.Side(SideEdge.Right)!.Columns[0].Groups[0];
        Assert.True(group.IsTitleOnly);
        Assert.Equal(half, group.OpenLength!.Value, Tolerance);
        AssertClose([Metrics.TitleLength, StackLength - Metrics.WidgetGap - Metrics.TitleLength], Lengths(squeezed));
    }

    [Fact]
    public void AWidget_OpensAgainToTheLengthItHad()
    {
        var stack = Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]));

        var opened = Toggle(Toggle(stack, "a"), "a");

        AssertClose(Lengths(stack), Lengths(opened));
        Assert.False(opened.Side(SideEdge.Right)!.Columns[0].Groups[0].IsTitleOnly);
    }

    [Fact]
    public void AWidgetWithNoLengthToGoBackTo_OpensAtItsMinimum()
    {
        var stack = Widgets(SideEdge.Right, new WindowGroup(["a"], 1, true), new WindowGroup(["b"]));

        var opened = Toggle(stack, "a");

        Assert.Equal(Metrics.DefaultMinimumWindowLength, Lengths(opened)[0], Tolerance);
    }

    [Fact]
    public void TheRoomComesFromTheLargestWindowFirst()
    {
        var stack = Widgets(SideEdge.Right,
            new WindowGroup(["a"], 1, true, 200), new WindowGroup(["b"], 3), new WindowGroup(["c"], 1));
        var before = Lengths(stack);

        var opened = Toggle(stack, "a");

        AssertClose([200, before[1] - (200 - Metrics.TitleLength), before[2]], Lengths(opened));
    }

    [Fact]
    public void TheOthersGiveWayOnlyDownToTheirMinimums()
    {
        var stack = Widgets(SideEdge.Right,
            new WindowGroup(["a"], 1, true, 600), new WindowGroup(["b"]), new WindowGroup(["c"]));
        var minimum = Metrics.DefaultMinimumWindowLength;

        var opened = Toggle(stack, "a");

        AssertClose([StackLength - Metrics.WidgetGap * 2 - minimum * 2, minimum, minimum], Lengths(opened));
    }

    [Fact]
    public void TheOnlyWindowShowingItsContents_DoesntSqueeze()
    {
        var stack = Widgets(SideEdge.Right, new WindowGroup(["a"], 1, true), new WindowGroup(["b"]));

        var alone = Widgets(SideEdge.Right, new WindowGroup(["a"]));

        Assert.Same(stack, Toggle(stack, "b"));
        Assert.Same(alone, Toggle(alone, "a"));
    }

    [Fact]
    public void AWindowThatIsntOpen_ChangesNothing()
    {
        var stack = Widgets(SideEdge.Right, new WindowGroup(["a"]));

        Assert.Same(stack, Toggle(stack, "nowhere"));
    }

    [Fact]
    public void DraggingAGapUntilAWindowIsATitle_RemembersHowLongItWas()
    {
        var stack = Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]));
        var before = Lengths(stack);

        var resize = new StackGapResize(stack, SideEdge.Right, 0, Measure(_window), Metrics);
        var squeezed = resize.LayoutAfter(new LayoutPoint(0, 5000));

        var group = squeezed.Side(SideEdge.Right)!.Columns[0].Groups[1];
        Assert.True(group.IsTitleOnly);
        Assert.Equal(before[1], group.OpenLength!.Value, Tolerance);
    }

    [Fact]
    public void AnyToggleOfAnyWindow_KeepsTheRules()
    {
        Gen.Select(Valid, WindowSizes).Sample((layout, size) =>
        {
            foreach (var windowId in layout.OpenWindowIds())
            {
                var toggled = TitleOnlyToggle.Toggle(layout, windowId, Measure(size), Metrics);
                Assert.Equal(TestLayouts.Text(toggled), TestLayouts.Text(LayoutRepair.Repair(toggled, Catalog)));
            }
        });
    }

    private static void AssertClose(List<double> expected, List<double> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++)
            Assert.Equal(expected[index], actual[index], Tolerance);
    }

    private static DockingLayout Toggle(DockingLayout layout, string windowId) =>
        TitleOnlyToggle.Toggle(layout, windowId, Measure(_window), Metrics);

    private static List<double> Lengths(DockingLayout layout) =>
    [
        .. LayoutSolver.Solve(layout, Measure(_window), Metrics).WindowGroups
            .OrderBy(group => group.Bounds.Y)
            .Select(group => group.Bounds.Height),
    ];
}
