// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Dragging;

/// <summary>The size a window gets when it comes off a side to float.</summary>
public sealed class WindowFloatingTests
{
    private const double Offset = 24;

    private static readonly LayoutSize _window = new(1000, 700);

    // The right side's stack is 700 less a margin of 8 at each end.
    private const double StackLength = 684;

    private static readonly DockingLayout _aloneOnTheRight = Widgets(SideEdge.Right, new WindowGroup(["a"]));

    private static readonly DockingLayout _twoOnTheRight =
        Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]));

    [Fact]
    public void AWindowAloneOnItsSide_ThatsNeverFloated_FloatsAtHalfItsHeight()
    {
        Assert.Equal(new LayoutSize(240, StackLength / 2), SizeWhenFloated(_aloneOnTheRight, "a"));
    }

    [Fact]
    public void AWindowAloneOnItsSide_FloatsAtTheSizeItLastFloatedAt()
    {
        var remembered = _aloneOnTheRight with { FloatingSizes = [new FloatingSize("a", 300, 200)] };

        Assert.Equal(new LayoutSize(300, 200), SizeWhenFloated(remembered, "a"));
    }

    [Fact]
    public void AWindowWithOthersOnItsSide_FloatsAtTheSizeItIs()
    {
        var size = SizeWhenFloated(_twoOnTheRight with { FloatingSizes = [new FloatingSize("a", 300, 200)] }, "a");

        Assert.Equal(new LayoutSize(240, (StackLength - Metrics.WidgetGap) / 2), size);
    }

    [Fact]
    public void HalfItsHeight_IsNeverLessThanItsMinimum()
    {
        var size = WindowFloating.SizeWhenFloated(_aloneOnTheRight, "a", new LayoutRect(0, 0, 240, 150), false,
            Measure(_window), Metrics);

        Assert.Equal(Metrics.DefaultMinimumWindowLength, size.Height);
    }

    [Fact]
    public void AFloatingWindowThatGoesOntoASide_ItsSizeIsRemembered()
    {
        var floating = LayoutEdits.FloatWindow(new DockingLayout([], [], []), "x", new LayoutPoint(100, 100), 300, 250);

        var docked = LayoutEdits.MoveWindowToEmptySide(floating, "x", SideEdge.Left, 300);

        Assert.Equal(new FloatingSize("x", 300, 250), docked.FloatingSizeOf("x"));
    }

    [Fact]
    public void AClosedFloatingWindow_ItsSizeIsRememberedToo()
    {
        var floating = LayoutEdits.FloatWindow(new DockingLayout([], [], []), "x", new LayoutPoint(100, 100), 300, 250);

        Assert.Equal(new FloatingSize("x", 300, 250), LayoutEdits.CloseWindow(floating, "x").FloatingSizeOf("x"));
    }

    [Fact]
    public void TurningIntoAWidgetOnAnEmptySide_KeepsTheFloatingWindowsWidth()
    {
        var floating = LayoutEdits.FloatWindow(new DockingLayout([], [], []), "x", new LayoutPoint(100, 100), 310, 250);

        var widget = LayoutEdits.TurnIntoWidget(floating, "x", Metrics.DefaultWindowWidth);

        Assert.Equal(310, widget.Side(SideEdge.Right)!.Columns[0].Width);
    }

    [Fact]
    public void FloatingFromTheMenu_OnTheRight_KeepsItsRightEdgeJustIn()
    {
        var floated = WindowFloating.FromMenu(_twoOnTheRight, "a", Measure(_window), Metrics, Offset);
        var before = WhereItIs(_twoOnTheRight, "a");
        var after = WhereItIs(floated, "a");

        Assert.Equal(before.Right - Offset, after.Right);
        Assert.Equal(before.Y + Offset, after.Y);
        Assert.Equal(before.Height, after.Height);
    }

    [Fact]
    public void FloatingFromTheMenu_AWindowAloneOnItsSide_GetsHalfItsHeight()
    {
        var floated = WindowFloating.FromMenu(_aloneOnTheRight, "a", Measure(_window), Metrics, Offset);

        Assert.Equal(StackLength / 2, floated.FloatingWindowNamed("a")!.Height);
    }

    [Fact]
    public void FloatingFromTheMenu_AFloatingWindow_ChangesNothing()
    {
        var floating = LayoutEdits.FloatWindow(new DockingLayout([], [], []), "x", new LayoutPoint(100, 100), 300, 250);

        Assert.Same(floating, WindowFloating.FromMenu(floating, "x", Measure(_window), Metrics, Offset));
    }

    private static LayoutSize SizeWhenFloated(DockingLayout layout, string windowId)
    {
        var place = LayoutSolver.Solve(layout, Measure(_window), Metrics).PlaceOfWindow(windowId)!.Value;
        return WindowFloating.SizeWhenFloated(layout, windowId, place.Bounds, place.IsTitleOnly, Measure(_window), Metrics);
    }

    private static LayoutRect WhereItIs(DockingLayout layout, string windowId) =>
        LayoutSolver.Solve(layout, Measure(_window), Metrics).PlaceOfWindow(windowId)!.Value.Bounds;
}
