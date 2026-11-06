// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Solving;

public sealed class FloatingWindowsAndHandlesTests
{
    private const double Tolerance = 0.001;

    private static readonly LayoutSize _window = new(1000, 700);

    private static readonly DockingLayout _twoOnTheRight =
        Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]));

    [Fact]
    public void AFloatingWindow_GoesWhereItWasPut()
    {
        var bounds = Place(new FloatingWindow("x", new LayoutPoint(50, 60), 300, 200));

        Assert.Equal(new LayoutRect(50, 60, 300, 200), bounds);
    }

    [Fact]
    public void AFloatingWindow_MovesJustEnoughToStayInsideTheMargin_WithoutChangingWhatsStored()
    {
        var wanted = new FloatingWindow("x", new LayoutPoint(900, 650), 300, 200);

        var margin = Metrics.Margin;
        Assert.Equal(new LayoutRect(1000 - margin - 300, 700 - margin - 200, 300, 200), Place(wanted));
    }

    [Fact]
    public void AFloatingWindowBiggerThanTheAppWindow_FillsItUpToTheMargin()
    {
        var margin = Metrics.Margin;
        Assert.Equal(new LayoutRect(margin, margin, 1000 - margin * 2, 700 - margin * 2),
            Place(new FloatingWindow("x", new LayoutPoint(-50, -50), 3000, 3000)));
    }

    [Fact]
    public void TheAreaForFloatingWindows_NeverTurnsInsideOut()
    {
        var area = LayoutSolver.FloatingWindowArea(new LayoutSize(10, 4), Metrics);

        Assert.True(area.Width >= 0);
        Assert.True(area.Height >= 0);
    }

    [Fact]
    public void ATitleOnlyFloatingWindow_IsJustItsTitle()
    {
        Assert.Equal(Metrics.TitleLength, Place(new FloatingWindow("x", new LayoutPoint(5, 5), 300, 200, true)).Height);
    }

    [Fact]
    public void AFloatingWindow_TakesNothingFromTheSidesOrTheCanvas()
    {
        var floating = LayoutEdits.FloatWindow(new DockingLayout([], [], []), "x", new LayoutPoint(0, 0), 300, 200);

        var solved = LayoutSolver.Solve(floating, Measure(_window), Metrics);

        Assert.Equal(_window.Width, solved.CanvasRegion.Width);
        Assert.Equal(default, solved.SafeArea);
    }

    [Fact]
    public void PlaceOfWindow_FindsWidgetsAndFloatingWindows_AndNothingElse()
    {
        var layout = LayoutEdits.FloatWindow(_twoOnTheRight, "x", new LayoutPoint(5, 5), 300, 200, isTitleOnly: true);
        var solved = LayoutSolver.Solve(layout, Measure(_window), Metrics);

        Assert.False(solved.PlaceOfWindow("a")!.Value.IsTitleOnly);
        Assert.True(solved.PlaceOfWindow("x")!.Value.IsTitleOnly);
        Assert.Null(solved.PlaceOfWindow("nowhere"));
    }

    [Fact]
    public void AnyFloatingWindowInAnyAppWindow_StaysInside()
    {
        Gen.Select(Valid, WindowSizes).Sample((layout, window) =>
        {
            foreach (var floating in LayoutSolver.Solve(layout, Measure(window), Metrics).FloatingWindows)
            {
                Assert.True(floating.Bounds.X >= -Tolerance && floating.Bounds.Right <= window.Width + Tolerance);
                Assert.True(floating.Bounds.Y >= -Tolerance && floating.Bounds.Bottom <= window.Height + Tolerance);
            }
        });
    }

    [Fact]
    public void ASideHasAHandleAlongItsInnerEdge_AndOneInEachGap()
    {
        var ids = StackResizeHandles.IdsIn(_twoOnTheRight).ToList();

        Assert.Equal(
            [new StackResizeHandleId(SideEdge.Right, null), new StackResizeHandleId(SideEdge.Right, 0)],
            ids);
    }

    [Fact]
    public void ALayoutWithNoSides_HasNoHandles()
    {
        Assert.Empty(StackResizeHandles.IdsIn(new DockingLayout([], [], [])));
    }

    [Fact]
    public void AGapsHandle_FillsTheGap()
    {
        var solved = LayoutSolver.Solve(_twoOnTheRight, Measure(_window), Metrics);
        var upper = solved.PlaceOfWindow("a")!.Value.Bounds;
        var lower = solved.PlaceOfWindow("b")!.Value.Bounds;

        var handle = StackResizeHandles.For(solved, Metrics).Single(other => other.Id.Gap == 0);

        Assert.Equal(new LayoutRect(upper.X, upper.Bottom, upper.Width, lower.Y - upper.Bottom), handle.Bounds);
    }

    [Theory]
    [InlineData(SideEdge.Right)]
    [InlineData(SideEdge.Left)]
    public void AnInnerEdgesHandle_LiesInTheMarginBesideTheWindows(SideEdge edge)
    {
        var solved = LayoutSolver.Solve(Widgets(edge, new WindowGroup(["a"])), Measure(_window), Metrics);
        var window = solved.PlaceOfWindow("a")!.Value.Bounds;

        var handle = StackResizeHandles.For(solved, Metrics).Single();

        var x = edge == SideEdge.Right ? window.X - Metrics.Margin : window.Right;
        Assert.Equal(new LayoutRect(x, window.Y, Metrics.Margin, window.Height), handle.Bounds);
    }

    [Fact]
    public void AnInnerEdgesHandle_IsClearOfEveryAnchoredToolbar()
    {
        Gen.Select(Valid, WindowSizes).Sample((layout, window) =>
        {
            var solved = LayoutSolver.Solve(layout, Measure(window, Sizes(layout, _ => 300)), Metrics);

            foreach (var handle in StackResizeHandles.For(solved, Metrics).Where(handle => handle.Id.Gap is null))
            {
                // Only just inside it, because arithmetic with fractions can leave them touching by next to nothing.
                var inside = new LayoutRect(handle.Bounds.X + Tolerance, handle.Bounds.Y, handle.Bounds.Width - Tolerance * 2, handle.Bounds.Height);
                Assert.DoesNotContain(solved.Toolbars.Where(toolbar => toolbar.Anchor is not null),
                    toolbar => toolbar.Bounds.Intersects(inside));
            }
        });
    }

    [Fact]
    public void ALayoutSavedBeforeFloatingWindowsExisted_StillLoads()
    {
        const string saved = """{"sides":[],"toolbars":[],"closedWindows":[{"windowId":"a","edge":"Right","position":0,"share":1,"sideWidth":240}]}""";

        var layout = LayoutJson.TryRead(saved)!;

        Assert.Empty(layout.FloatingWindows);
        Assert.Null(layout.ClosedWindows[0].WasFloating);
    }

    [Fact]
    public void FloatingWindows_SurviveSavingAndLoading()
    {
        var layout = LayoutEdits.FloatWindow(_twoOnTheRight, "x", new LayoutPoint(5, 6), 300, 200, isTitleOnly: true);
        layout = LayoutEdits.CloseWindow(LayoutEdits.FloatWindow(layout, "y", new LayoutPoint(7, 8), 100, 110), "y");

        var loaded = LayoutJson.TryRead(LayoutJson.Write(layout))!;

        Assert.Equal(layout.FloatingWindowNamed("x"), loaded.FloatingWindowNamed("x"));
        Assert.Equal(new LayoutPoint(7, 8), loaded.ClosedWindows.Single().WasFloating!.Position);
    }

    [Fact]
    public void RepairingFloatingWindows_DropsUnknownOnes_AndFixesSillyNumbers()
    {
        var layout = new DockingLayout([], [], [])
        {
            FloatingWindows =
            [
                new FloatingWindow("gone", default, 100, 100),
                new FloatingWindow("w0", new LayoutPoint(double.NaN, 5), -5, 0),
                new FloatingWindow("w0", new LayoutPoint(1, 1), 100, 100),
            ],
        };

        var repaired = LayoutRepair.Repair(layout, Catalog);

        var floating = Assert.Single(repaired.FloatingWindows);
        Assert.Equal(new FloatingWindow("w0", default, Catalog.DefaultSideWidth, Catalog.DefaultSideWidth), floating);
    }

    [Fact]
    public void AWindowBothOnASideAndFloating_StaysOnTheSide()
    {
        var layout = _twoOnTheRight with { FloatingWindows = [new FloatingWindow("w0", default, 100, 100)] };
        var catalog = new DockingCatalog(["a", "b", "w0"], [], 240);
        var both = layout with { Sides = [new SideLayout(SideEdge.Right, SideKind.Widgets, [new SideColumn(240, [new WindowGroup(["w0"])])])] };

        var repaired = LayoutRepair.Repair(both, catalog);

        Assert.Empty(repaired.FloatingWindows);
        Assert.Equal(1, repaired.OpenWindowIds().Count(id => id == "w0"));
    }

    private static LayoutRect Place(FloatingWindow window)
    {
        var layout = new DockingLayout([], [], []) { FloatingWindows = [window] };
        return LayoutSolver.Solve(layout, Measure(_window), Metrics).FloatingWindows.Single().Bounds;
    }
}
