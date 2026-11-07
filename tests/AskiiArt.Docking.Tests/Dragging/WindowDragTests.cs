// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Dragging;

public sealed class WindowDragTests
{
    private const double Tolerance = 0.001;

    private static readonly LayoutSize _window = new(1000, 700);
    private static readonly LayoutPoint _heldAtTheTopLeft = new(0.1, 0.05);
    private static readonly LayoutPoint _middleOfNowhere = new(450, 350);

    // Across the right side's widgets, which are 240 wide with a margin of 8.
    private const double OverTheRightSide = 880;

    private static readonly DockingLayout _twoOnTheRight =
        Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]));

    private static readonly DockingLayout _oneOnTheRightAndOneFloating = LayoutEdits.FloatWindow(
        Widgets(SideEdge.Right, new WindowGroup(["a"])), "x", new LayoutPoint(300, 300), 200, 200);

    private static readonly DockingLayout _twoOnTheRightAndOneFloating =
        LayoutEdits.FloatWindow(_twoOnTheRight, "x", new LayoutPoint(300, 300), 200, 200);

    [Fact]
    public void Example1_DroppedOnTheLowerPartOfALoneWindow_ItGoesBelow_AndThePlaceholderIsWhereItLands()
    {
        var drag = Drag(_oneOnTheRightAndOneFloating, "x");
        var lowerPart = new LayoutPoint(OverTheRightSide, 600);

        var preview = drag.PreviewAt(lowerPart);

        Assert.Equal(["a", "x"], Stack(preview.Layout, SideEdge.Right));
        Assert.Equal(WhereItIs(preview.Layout, "x"), preview.Placeholder);
        Assert.Equal(TestLayouts.Text(preview.Layout), TestLayouts.Text(drag.LayoutAt(lowerPart)));
        Assert.True(WhereItIs(preview.Layout, "a").Height < _window.Height - Metrics.Margin * 2);
    }

    [Fact]
    public void DroppedOnTheUpperPartOfAWindow_ItGoesAbove()
    {
        var dropped = Drag(_oneOnTheRightAndOneFloating, "x").LayoutAt(new LayoutPoint(OverTheRightSide, 100));

        Assert.Equal(["x", "a"], Stack(dropped, SideEdge.Right));
    }

    [Fact]
    public void Example2_DroppedBetweenTwoWindows_ItGoesBetweenThem()
    {
        var drag = Drag(_twoOnTheRightAndOneFloating, "x");
        var inTheGap = new LayoutPoint(OverTheRightSide, _window.Height / 2);

        Assert.Equal(["a", "x", "b"], Stack(drag.LayoutAt(inTheGap), SideEdge.Right));
    }

    [Fact]
    public void Example3_ATitleOnlyWindowStaysThatWay_AndTheOtherGivesUpTheSpace()
    {
        var layout = LayoutEdits.FloatWindow(
            Widgets(SideEdge.Right, new WindowGroup(["a"], 1, true), new WindowGroup(["b"])), "x", new LayoutPoint(300, 300), 200, 200);
        var drag = Drag(layout, "x");
        var overTheLowerWindow = new LayoutPoint(OverTheRightSide, 100);

        var preview = drag.PreviewAt(overTheLowerWindow);

        Assert.Equal(["a", "x", "b"], Stack(preview.Layout, SideEdge.Right));
        Assert.Equal(Metrics.TitleLength, WhereItIs(preview.Layout, "a").Height, Tolerance);
    }

    [Fact]
    public void Example4_OnAnEmptySidesEdge_ItOpensALittle_AndADropGivesItTheFullWidth()
    {
        var layout = _twoOnTheRight with { Toolbars = [new ToolbarPlacement("tools", ToolbarAnchor.LeftMiddle, ToolbarOrientation.Vertical)] };
        var drag = Drag(layout, "a");
        var atTheEdge = new LayoutPoint(5, 350);

        var preview = drag.PreviewAt(atTheEdge);

        Assert.Equal(WindowDropKind.EmptySide, preview.Zone!.Kind);
        Assert.Equal(Metrics.WindowEdgePlaceholderWidth, preview.Placeholder!.Value.Width, Tolerance);
        Assert.Equal(Metrics.Margin, preview.Placeholder.Value.X);

        var toolbar = LayoutSolver.Solve(preview.Layout, Measure(_window), Metrics).Toolbars.Single();
        Assert.True(toolbar.Bounds.X >= preview.Placeholder.Value.Right);

        var dropped = drag.LayoutAt(atTheEdge);
        Assert.Equal(["a"], Stack(dropped, SideEdge.Left));
        Assert.Equal(Metrics.DefaultWindowWidth, dropped.Side(SideEdge.Left)!.Columns[0].Width);
    }

    [Fact]
    public void APointerPastTheAppWindowsEdge_CountsAsAtThatEdge()
    {
        var drag = Drag(_twoOnTheRight, "a");

        Assert.Equal(WindowDropKind.EmptySide, drag.ZoneAt(new LayoutPoint(-200, 350))!.Kind);
    }

    [Fact]
    public void APointerPastTheAppWindowsEdge_BesideAStack_FloatsTheWindowBackInside()
    {
        var drag = Drag(_twoOnTheRightAndOneFloating, "x");
        var pastTheBottomRight = new LayoutPoint(_window.Width + 100, _window.Height + 50);

        Assert.Null(drag.ZoneAt(pastTheBottomRight));
        Assert.Null(drag.ZoneAt(new LayoutPoint(_window.Width + 100, 350)));

        var floating = drag.LayoutAt(pastTheBottomRight).FloatingWindowNamed("x")!;
        var area = LayoutSolver.FloatingWindowArea(_window, Metrics);
        Assert.Equal(area.Right - 200, floating.Position.X, Tolerance);
        Assert.Equal(area.Bottom - 200, floating.Position.Y, Tolerance);
    }

    [Fact]
    public void TheHeldWindow_FollowsThePointerPastTheEdge_AndTheLandingShowsWhereItGoesBack()
    {
        var drag = Drag(_twoOnTheRightAndOneFloating, "x");
        var outside = new LayoutPoint(_window.Width + 100, _window.Height + 50);

        var preview = drag.PreviewAt(outside);

        Assert.Equal(drag.HeldAt(outside), preview.Held);
        Assert.Equal(outside.X - _heldAtTheTopLeft.X * 200, preview.Held.X, Tolerance);
        Assert.Equal(WhereItIs(preview.Layout, "x"), preview.Landing);
    }

    [Fact]
    public void WhereItLandsWhereItsHeld_TheresNoLandingToShow()
    {
        var preview = Drag(_twoOnTheRightAndOneFloating, "x").PreviewAt(_middleOfNowhere);

        Assert.Null(preview.Landing);
        Assert.Equal(WhereItIs(preview.Layout, "x"), preview.Held);
    }

    [Fact]
    public void OverAZone_TheresNoLandingToShow()
    {
        Assert.Null(Drag(_twoOnTheRightAndOneFloating, "x").PreviewAt(new LayoutPoint(OverTheRightSide, 100)).Landing);
    }

    [Fact]
    public void AWindowAloneOnItsSide_IsHeldAtTheSizeItWouldFloatAt_AndLandsAtIt()
    {
        var alone = Widgets(SideEdge.Right, new WindowGroup(["a"])) with { FloatingSizes = [new FloatingSize("a", 300, 220)] };
        var drag = Drag(alone, "a");

        var floating = drag.LayoutAt(_middleOfNowhere).FloatingWindowNamed("a")!;

        Assert.Equal(new LayoutSize(300, 220), drag.HeldSize);
        Assert.Equal(300, floating.Width);
        Assert.Equal(220, floating.Height);
    }

    [Fact]
    public void AFloatingWindowDroppedOnAnEmptySide_KeepsItsWidth()
    {
        var layout = LayoutEdits.FloatWindow(_twoOnTheRight, "x", new LayoutPoint(300, 300), 310, 200);

        var dropped = Drag(layout, "x").LayoutAt(new LayoutPoint(5, 350));

        Assert.Equal(310, dropped.Side(SideEdge.Left)!.Columns[0].Width);
    }

    [Fact]
    public void ASideWithWindows_HasAnEdgeStripThatMakesADock_RatherThanAWidget()
    {
        var zones = Drag(_twoOnTheRight, "a").Zones.Where(zone => zone.Edge == SideEdge.Right).ToList();

        Assert.DoesNotContain(zones, zone => zone.Kind == WindowDropKind.EmptySide);
        Assert.Equal(WindowDropKind.SideEdge, zones[0].Kind);
    }

    [Fact]
    public void ThereIsNoGapBetweenAStacksZones_FromOneEndOfTheSideToTheOther()
    {
        var drag = Drag(_twoOnTheRightAndOneFloating, "x");

        for (var y = 0; y < _window.Height; y++)
            Assert.NotNull(drag.ZoneAt(new LayoutPoint(_window.Width - 1, y)));
    }

    [Fact]
    public void AFullSide_TakesNoMoreWindows_SoItFloats()
    {
        var full = LayoutEdits.FloatWindow(
            Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]), new WindowGroup(["c"]), new WindowGroup(["d"])),
            "x", new LayoutPoint(300, 300), 200, 200);
        var drag = Drag(full, "x");
        var overTheSide = new LayoutPoint(OverTheRightSide, 300);

        Assert.Null(drag.ZoneAt(overTheSide));
        Assert.True(drag.LayoutAt(overTheSide).FloatingWindowNamed("x") is not null);
    }

    [Fact]
    public void AWindowFromAFullSide_CanStillMoveWithinIt()
    {
        var full = Widgets(SideEdge.Right, new WindowGroup(["a"]), new WindowGroup(["b"]), new WindowGroup(["c"]), new WindowGroup(["d"]));
        var drag = Drag(full, "a");

        var dropped = drag.LayoutAt(new LayoutPoint(OverTheRightSide, 690));

        Assert.Equal(["b", "c", "d", "a"], Stack(dropped, SideEdge.Right));
    }

    [Fact]
    public void ItsOwnPlaceInAStack_ChangesNothing()
    {
        var uneven = Widgets(SideEdge.Right, new WindowGroup(["a"], 3), new WindowGroup(["b"], 1));
        var drag = Drag(uneven, "a");

        var preview = drag.PreviewAt(new LayoutPoint(OverTheRightSide, 100));

        Assert.Equal(TestLayouts.Text(uneven), TestLayouts.Text(preview.Layout));
        Assert.Equal(WhereItIs(uneven, "a"), preview.Placeholder);
    }

    [Fact]
    public void AloneOnItsSide_ItsOwnPlaceChangesNothing()
    {
        var alone = Widgets(SideEdge.Right, new WindowGroup(["a"]));
        var drag = Drag(alone, "a");

        var zone = drag.ZoneAt(new LayoutPoint(OverTheRightSide, 300));

        Assert.Equal(WindowDropKind.OwnPlace, zone!.Kind);
        Assert.Equal(TestLayouts.Text(alone), TestLayouts.Text(drag.LayoutAt(new LayoutPoint(OverTheRightSide, 300))));
    }

    [Fact]
    public void AnywhereElse_ItFloats_KeepingItsSize_WhereItWasHeld()
    {
        var drag = Drag(_twoOnTheRight, "a");
        var size = drag.HeldSize;

        var floating = drag.LayoutAt(_middleOfNowhere).FloatingWindowNamed("a")!;

        Assert.Equal(_middleOfNowhere.X - _heldAtTheTopLeft.X * size.Width, floating.Position.X, Tolerance);
        Assert.Equal(_middleOfNowhere.Y - _heldAtTheTopLeft.Y * size.Height, floating.Position.Y, Tolerance);
        Assert.Equal(size.Width, floating.Width);
        Assert.Equal(size.Height, floating.Height);
        Assert.Null(drag.PreviewAt(_middleOfNowhere).Placeholder);
    }

    [Fact]
    public void TheLastWindowDraggedOffASide_EmptiesIt()
    {
        var drag = Drag(Widgets(SideEdge.Right, new WindowGroup(["a"])), "a");

        Assert.Null(drag.PreviewAt(_middleOfNowhere).Layout.Side(SideEdge.Right));
    }

    [Fact]
    public void ADropFarOutsideTheAppWindow_StaysInsideTheMargin()
    {
        var floating = Drag(_twoOnTheRight, "a").LayoutAt(new LayoutPoint(_window.Width / 2, -500)).FloatingWindowNamed("a")!;

        Assert.Equal(Metrics.Margin, floating.Position.Y);
    }

    [Fact]
    public void ATitleOnlyWidget_LandsFloatingWithANormalHeight()
    {
        var stack = Widgets(SideEdge.Right, new WindowGroup(["a"], 1, true), new WindowGroup(["b"]));

        var floating = Drag(stack, "a").LayoutAt(_middleOfNowhere).FloatingWindowNamed("a")!;

        Assert.False(floating.IsTitleOnly);
        Assert.Equal(Metrics.DefaultFloatingWindowHeight, floating.Height);
    }

    [Fact]
    public void ATitleOnlyFloatingWindowThatsOnlyMoved_StaysTitleOnly()
    {
        var layout = LayoutEdits.FloatWindow(_twoOnTheRight, "x", new LayoutPoint(300, 300), 200, 200, isTitleOnly: true);

        var floating = Drag(layout, "x").LayoutAt(_middleOfNowhere).FloatingWindowNamed("x")!;

        Assert.True(floating.IsTitleOnly);
        Assert.Equal(200, floating.Height);
    }

    [Fact]
    public void ATitleOnlyFloatingWindowThatLandsOnASide_ShowsItsContents()
    {
        var layout = LayoutEdits.FloatWindow(_twoOnTheRight, "x", new LayoutPoint(300, 300), 200, 200, isTitleOnly: true);

        var dropped = Drag(layout, "x").LayoutAt(new LayoutPoint(OverTheRightSide, 100));

        Assert.False(dropped.Side(SideEdge.Right)!.Columns[0].Groups.Single(group => group.WindowIds[0] == "x").IsTitleOnly);
    }

    [Fact]
    public void OverAnotherFloatingWindow_ItFloatsOnTop_ThoughASideIsUnderneath()
    {
        var layout = LayoutEdits.FloatWindow(_twoOnTheRight, "x", new LayoutPoint(800, 100), 150, 150);
        layout = LayoutEdits.FloatWindow(layout, "y", new LayoutPoint(300, 300), 150, 150);
        var drag = Drag(layout, "y");
        var overX = new LayoutPoint(850, 150);

        Assert.Null(drag.ZoneAt(overX));
        Assert.Equal(["x", "y"], drag.LayoutAt(overX).FloatingWindows.Select(window => window.WindowId));
    }

    [Fact]
    public void TheZones_DontMove_WhenAPreviewMakesRoom()
    {
        var drag = Drag(_oneOnTheRightAndOneFloating, "x");
        var before = drag.Zones.ToList();

        drag.PreviewAt(new LayoutPoint(OverTheRightSide, 600));

        Assert.Equal(before, drag.Zones);
    }

    [Fact]
    public void Dragging_NeverChangesWhereItStarted()
    {
        var drag = Drag(_twoOnTheRight, "a");
        var before = TestLayouts.Text(_twoOnTheRight);

        drag.PreviewAt(_middleOfNowhere);
        drag.LayoutAt(new LayoutPoint(OverTheRightSide, 600));

        Assert.Equal(before, TestLayouts.Text(drag.Start));
    }

    [Fact]
    public void OnlyAnOpenWindow_CanBeDragged()
    {
        Assert.Throws<ArgumentException>(() => Drag(_twoOnTheRight, "nowhere"));
    }

    [Fact]
    public void AnyDropOfAnyWindow_KeepsTheRules_AndTheWindowOpenExactlyOnce()
    {
        var positions = Gen.Select(Gen.Double[-100, 1100], Gen.Double[-100, 800], (x, y) => new LayoutPoint(x, y));

        Gen.Select(Valid, positions).Sample((layout, position) =>
        {
            foreach (var windowId in layout.OpenWindowIds().ToList())
            {
                var drag = Drag(layout, windowId);
                var dropped = drag.LayoutAt(position);
                var preview = drag.PreviewAt(position);

                Assert.Equal(1, dropped.OpenWindowIds().Count(id => id == windowId));
                Assert.Equal(TestLayouts.Text(dropped), TestLayouts.Text(LayoutRepair.Repair(dropped, Catalog)));
                Assert.Equal(1, preview.Layout.OpenWindowIds().Count(id => id == windowId));
            }
        });
    }

    private static WindowDrag Drag(DockingLayout layout, string windowId) =>
        new(layout, windowId, _heldAtTheTopLeft, Measure(_window), Metrics);

    private static List<string> Stack(DockingLayout layout, SideEdge edge) =>
        [.. layout.Side(edge)!.Columns[0].Groups.Select(group => group.WindowIds[0])];

    private static LayoutRect WhereItIs(DockingLayout layout, string windowId) =>
        LayoutSolver.Solve(layout, Measure(_window), Metrics).PlaceOfWindow(windowId)!.Value.Bounds;
}
