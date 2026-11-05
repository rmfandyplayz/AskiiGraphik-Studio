// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Dragging;

public sealed class ToolbarDragTests
{
    private const double Tolerance = 0.001;

    private static readonly LayoutSize _window = new(1000, 700);
    private static readonly LayoutPoint _heldAtTheLeftEnd = new(0, 0.5);
    private static readonly LayoutPoint _middleOfNowhere = new(450, 350);

    private static readonly DockingLayout _layout = new(
        [],
        [
            new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal),
            new ToolbarPlacement("tools", ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal),
        ],
        []);

    // A toolbar wider when it runs up and down than it is tall when it runs across, as text buttons make them.
    private static readonly ToolbarShapes _differentEachWay = new(new LayoutSize(150, 40), new LayoutSize(90, 200));

    private static readonly DockingLayout _crowded = new(
        [],
        [
            new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal),
            new ToolbarPlacement("edit", ToolbarAnchor.TopRight, ToolbarOrientation.Horizontal),
            new ToolbarPlacement("tools", ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal),
        ],
        []);

    [Fact]
    public void TheZones_AreEveryFreeAnchor_AndTheToolbarsOwn()
    {
        var zones = Drag(_layout, "file").Zones.Select(zone => zone.Anchor);

        Assert.Equal(
            [
                ToolbarAnchor.TopLeft,
                ToolbarAnchor.TopMiddle,
                ToolbarAnchor.TopRight,
                ToolbarAnchor.RightMiddle,
                ToolbarAnchor.BottomRight,
                ToolbarAnchor.BottomLeft,
                ToolbarAnchor.LeftMiddle,
            ],
            zones);
    }

    [Fact]
    public void AHiddenToolbar_LeavesItsAnchorFree()
    {
        var layout = LayoutEdits.HideToolbar(_layout, "tools");

        Assert.Contains(Drag(layout, "file").Zones, zone => zone.Anchor == ToolbarAnchor.BottomMiddle);
    }

    [Fact]
    public void AFloatingToolbar_LeavesTheAnchorItLastHadFree()
    {
        var layout = LayoutEdits.FloatToolbar(_layout, "tools", _middleOfNowhere);

        Assert.Contains(Drag(layout, "file").Zones, zone => zone.Anchor == ToolbarAnchor.BottomMiddle);
    }

    [Fact]
    public void CornerZones_AreInTheCorners_AndEdgeZones_RunBetweenThem()
    {
        var zones = ZonesOf(Drag(_layout, "file"));
        var (margin, gap, side) = (Metrics.Margin, Metrics.ToolbarGap, ToolbarSizes["file"].Height * 2);

        Assert.Equal(new LayoutRect(margin, margin, side, side), zones[ToolbarAnchor.TopLeft]);
        Assert.Equal(_window.Width - margin, zones[ToolbarAnchor.BottomRight].Right, Tolerance);
        Assert.Equal(_window.Height - margin, zones[ToolbarAnchor.BottomRight].Bottom, Tolerance);

        var left = zones[ToolbarAnchor.LeftMiddle];
        Assert.Equal(margin, left.X);
        Assert.Equal(zones[ToolbarAnchor.TopLeft].Bottom + gap, left.Y, Tolerance);
        Assert.Equal(zones[ToolbarAnchor.BottomLeft].Y - gap, left.Bottom, Tolerance);
    }

    [Fact]
    public void AZone_IsAsThickAsTheToolbarWouldBeThere()
    {
        var zones = ZonesOf(Drag(_layout, "file", _differentEachWay));

        Assert.Equal(40, zones[ToolbarAnchor.TopMiddle].Height);
        Assert.Equal(90, zones[ToolbarAnchor.RightMiddle].Width);
        Assert.Equal(90, zones[ToolbarAnchor.LeftMiddle].Width);
    }

    [Fact]
    public void ACornerZone_IsASquareWithRoomForAStripEachWay_HoweverTheToolbarRuns()
    {
        var vertical = LayoutEdits.SetToolbarOrientation(_layout, "file", ToolbarOrientation.Vertical);

        var whenHorizontal = ZonesOf(Drag(_layout, "file", _differentEachWay))[ToolbarAnchor.BottomLeft];
        var whenVertical = ZonesOf(Drag(vertical, "file", _differentEachWay))[ToolbarAnchor.BottomLeft];

        Assert.Equal((130.0, 130.0), (whenHorizontal.Width, whenHorizontal.Height));
        Assert.Equal((130.0, 130.0), (whenVertical.Width, whenVertical.Height));
    }

    [Fact]
    public void ACornerZone_IsNeverThinnerThanTheStripsBesideIt()
    {
        var zones = ZonesOf(Drag(_layout, "file", _differentEachWay));
        var corner = zones[ToolbarAnchor.TopRight];

        Assert.True(corner.Width >= zones[ToolbarAnchor.RightMiddle].Width);
        Assert.True(corner.Height >= zones[ToolbarAnchor.TopMiddle].Height);
    }

    [Fact]
    public void CornerZonesInASmallRegion_StopShortOfEachOther()
    {
        var measurements = Measure(new LayoutSize(400, 150));
        var drag = new ToolbarDrag(_layout, "file", _heldAtTheLeftEnd, measurements, _differentEachWay, Metrics);
        var zones = ZonesOf(drag);

        Assert.True(zones[ToolbarAnchor.TopLeft].Bottom < zones[ToolbarAnchor.BottomLeft].Y);
        Assert.True(zones[ToolbarAnchor.TopLeft].Right < zones[ToolbarAnchor.TopRight].X);
    }

    [Fact]
    public void AStrip_StopsAGapShortOfTheToolbarsBesideIt()
    {
        var drag = Drag(_crowded, "tools");

        var top = ZonesOf(drag)[ToolbarAnchor.TopMiddle];

        var file = Shown(drag, "file");
        var edit = Shown(drag, "edit");
        Assert.Equal(file.Right + Metrics.ToolbarGap, top.X, Tolerance);
        Assert.Equal(edit.X - Metrics.ToolbarGap, top.Right, Tolerance);
    }

    [Fact]
    public void ToolbarsTooLongForAStrip_ShrinkForIt_AndGetTheirLengthBackAfterTheDrop()
    {
        var sizes = new Dictionary<string, LayoutSize>(ToolbarSizes, StringComparer.Ordinal)
        {
            ["file"] = new(900, 40),
            ["edit"] = new(900, 40),
        };
        var measurements = Measure(_window, sizes);
        var drag = new ToolbarDrag(_crowded, "tools", _heldAtTheLeftEnd, measurements, Shapes("tools"), Metrics);

        var top = ZonesOf(drag)[ToolbarAnchor.TopMiddle];
        var innerWidth = _window.Width - Metrics.Margin * 2;
        Assert.True(top.Width >= innerWidth * Metrics.ToolbarZoneMinimumShare - Tolerance);

        var preview = drag.PreviewAt(_middleOfNowhere);
        var shown = LayoutSolver.Solve(drag.Others, measurements, Metrics, drag.ReservedSpots).Toolbars;
        var dropped = LayoutSolver.Solve(preview.Layout, measurements, Metrics).Toolbars;

        var file = shown.Single(toolbar => toolbar.ToolbarId == "file").Bounds;
        Assert.True(file.Right < top.X);
        Assert.True(dropped.Single(toolbar => toolbar.ToolbarId == "file").Bounds.Width > file.Width);
    }

    [Fact]
    public void AnyDragOfAnyLayout_KeepsItsZonesClearOfEachOther_AndOfTheToolbars()
    {
        Gen.Select(Valid, Gen.OneOfConst("file", "tools", "edit", "view"), WindowSizes, Gen.Double[20, 2000]).Sample(
            (layout, toolbarId, window, length) =>
            {
                var measurements = Measure(window, Sizes(layout, _ => length));
                var shapes = new ToolbarShapes(new LayoutSize(length, 40), new LayoutSize(40, length));
                var drag = new ToolbarDrag(layout, toolbarId, _heldAtTheLeftEnd, measurements, shapes, Metrics);

                var anchored = LayoutSolver.Solve(drag.Others, measurements, Metrics, drag.ReservedSpots).Toolbars
                    .Where(toolbar => toolbar.Anchor is not null);

                foreach (var zone in drag.Zones)
                {
                    Assert.DoesNotContain(anchored, toolbar => toolbar.Bounds.Intersects(zone.Bounds));
                    Assert.DoesNotContain(drag.Zones, other => other != zone && other.Bounds.Intersects(zone.Bounds));
                }
            });
    }

    [Fact]
    public void FloatingFreely_NeedsNoPreviewOfTheLanding()
    {
        var preview = Drag(_layout, "file").PreviewAt(_middleOfNowhere);

        Assert.Null(preview.Zone);
        Assert.Null(preview.PushedBackTo);
        Assert.Empty(preview.CoveredZones);
    }

    [Fact]
    public void BeingPushedBackInside_ShowsWhereItWouldLand_AndZonesThereGetOutOfTheWay()
    {
        var layout = _layout with
        {
            Sides = [new SideLayout(SideEdge.Right, SideKind.Widgets, [new SideColumn(240, [new WindowGroup(["a"])])])],
        };
        var drag = Drag(layout, "file");

        var preview = drag.PreviewAt(new LayoutPoint(950, 350));

        var landing = preview.PushedBackTo!.Value;
        Assert.Null(preview.Zone);
        Assert.Equal(_window.Width - 240 - 8 - 8, landing.Right, Tolerance);
        Assert.Contains(ToolbarAnchor.RightMiddle, preview.CoveredZones);
        Assert.DoesNotContain(ToolbarAnchor.LeftMiddle, preview.CoveredZones);
    }

    [Fact]
    public void OnAZone_ThePreviewIsThatZone_AndNotWhereTheToolbarWouldLand()
    {
        var drag = Drag(_layout, "file", _differentEachWay);
        var strip = ZonesOf(drag)[ToolbarAnchor.LeftMiddle];

        var preview = drag.PreviewAt(Center(drag, ToolbarAnchor.LeftMiddle));

        Assert.Equal(strip, preview.Zone!.Bounds);
        Assert.Null(preview.PushedBackTo);
        Assert.Equal(ToolbarAnchor.LeftMiddle, Toolbar(preview.Layout, "file").Anchor);
        Assert.Equal(ToolbarOrientation.Vertical, Toolbar(preview.Layout, "file").Orientation);
    }

    [Fact]
    public void OnAZone_OnlyThatZoneGetsOutOfTheWay()
    {
        var drag = Drag(_layout, "file");

        var preview = drag.PreviewAt(Center(drag, ToolbarAnchor.TopRight));

        Assert.Equal([ToolbarAnchor.TopRight], preview.CoveredZones);
    }

    [Fact]
    public void WhereverThePointerIs_EveryZoneKeepsItsRoom()
    {
        var drag = Drag(_crowded, "tools");

        Assert.Equal(drag.Zones.Count, drag.ReservedSpots.Count);
        Assert.Equal(drag.Zones.Select(zone => zone.Anchor), drag.ReservedSpots.Select(spot => spot.Anchor));
    }

    [Fact]
    public void TheToolbarsAround_AreTheSameWithTheToolbarOverAZoneOrNot()
    {
        var measurements = Measure(_window);
        var drag = Drag(_crowded, "tools");

        var shown = LayoutSolver.Solve(drag.Others, measurements, Metrics, drag.ReservedSpots);

        Assert.DoesNotContain(shown.Toolbars, toolbar => toolbar.ToolbarId == "tools");
        Assert.Equal(drag.Zones.Count, shown.ReservedSpots.Count);
        Assert.All(drag.Zones, zone => Assert.Equal(zone.Bounds, shown.ReservedSpots[zone.Anchor]));
    }

    [Fact]
    public void TheZones_StayInTheCanvasRegion()
    {
        var layout = _layout with
        {
            Sides = [new SideLayout(SideEdge.Right, SideKind.Widgets, [new SideColumn(240, [new WindowGroup(["a"])])])],
        };

        Assert.All(Drag(layout, "file").Zones, zone => Assert.True(zone.Bounds.Right <= _window.Width - 240 - 8));
    }

    [Fact]
    public void DroppingOnAnEdgeMiddle_SetsItsDirection()
    {
        var drag = Drag(_layout, "file");

        var dropped = drag.LayoutAt(Center(drag, ToolbarAnchor.LeftMiddle));

        var file = Toolbar(dropped, "file");
        Assert.Equal(ToolbarAnchor.LeftMiddle, file.Anchor);
        Assert.Equal(ToolbarOrientation.Vertical, file.Orientation);
        Assert.False(file.IsFloating);
    }

    [Fact]
    public void DroppingOnACorner_KeepsItsDirection()
    {
        var layout = LayoutEdits.SetToolbarOrientation(_layout, "file", ToolbarOrientation.Vertical);
        var drag = Drag(layout, "file");

        var dropped = drag.LayoutAt(Center(drag, ToolbarAnchor.BottomRight));

        var file = Toolbar(dropped, "file");
        Assert.Equal(ToolbarAnchor.BottomRight, file.Anchor);
        Assert.Equal(ToolbarOrientation.Vertical, file.Orientation);
    }

    [Fact]
    public void DroppingAnywhereElse_FloatsItWhereThePointerHoldsIt()
    {
        var dropped = Drag(_layout, "file").LayoutAt(_middleOfNowhere);

        var file = Toolbar(dropped, "file");
        Assert.True(file.IsFloating);
        Assert.Equal(ToolbarOrientation.Horizontal, file.Orientation);

        // Held at its left end and halfway down.
        var size = ToolbarSizes["file"];
        Assert.Equal(new LayoutPoint(_middleOfNowhere.X, _middleOfNowhere.Y - size.Height / 2), file.FloatingAt);
    }

    [Fact]
    public void DroppingOnAnOccupiedAnchor_Floats()
    {
        var dropped = Drag(_layout, "file").LayoutAt(new LayoutPoint(_window.Width / 2, _window.Height - 20));

        Assert.True(Toolbar(dropped, "file").IsFloating);
        Assert.Equal(ToolbarAnchor.BottomMiddle, Toolbar(dropped, "tools").Anchor);
    }

    [Fact]
    public void AToolbarDroppedOutsideTheCanvasRegion_SavesWhereItLanded()
    {
        var layout = _layout with
        {
            Sides = [new SideLayout(SideEdge.Right, SideKind.Widgets, [new SideColumn(240, [new WindowGroup(["a"])])])],
        };

        var dropped = Drag(layout, "file").LayoutAt(new LayoutPoint(950, 350));

        var at = Toolbar(dropped, "file").FloatingAt!.Value;
        Assert.Equal(_window.Width - 240 - 8 - 8 - ToolbarSizes["file"].Width, at.X, Tolerance);
    }

    [Fact]
    public void TheToolbarOnItsOwnAnchor_StaysThere()
    {
        var drag = Drag(_layout, "tools");

        var dropped = drag.LayoutAt(Center(drag, ToolbarAnchor.BottomMiddle));

        Assert.Equal(TestLayouts.Text(_layout), TestLayouts.Text(dropped));
    }

    [Fact]
    public void AFloatingToolbarDroppedOnAZone_StopsFloating()
    {
        var layout = LayoutEdits.FloatToolbar(_layout, "file", _middleOfNowhere);
        var drag = Drag(layout, "file");

        var dropped = drag.LayoutAt(Center(drag, ToolbarAnchor.TopRight));

        var file = Toolbar(dropped, "file");
        Assert.False(file.IsFloating);
        Assert.Equal(ToolbarAnchor.TopRight, file.Anchor);
    }

    [Fact]
    public void ThePreview_NeverChangesWhereTheDragStarted()
    {
        var drag = Drag(_layout, "file");
        var before = TestLayouts.Text(_layout);

        drag.LayoutAt(_middleOfNowhere);
        drag.LayoutAt(Center(drag, ToolbarAnchor.LeftMiddle));

        Assert.Equal(before, TestLayouts.Text(drag.Start));
    }

    [Fact]
    public void ADraggedToolbarsSubToolbar_ComesWithThePreview()
    {
        var drag = DragWithSubToolbar(_layout, parent: "file", dragged: "file");

        var preview = drag.PreviewAt(_middleOfNowhere);

        Assert.Equal("file", preview.SubToolbar!.ParentToolbarId);
        Assert.Equal(SubToolbarSide.BelowOrRight, preview.SubToolbar.Side);
    }

    [Fact]
    public void AnotherToolbarsSubToolbar_IsntPartOfThePreview()
    {
        var drag = DragWithSubToolbar(_layout, parent: "tools", dragged: "file");

        Assert.Null(drag.PreviewAt(_middleOfNowhere).SubToolbar);
    }

    [Fact]
    public void AZoneUnderAPushedBackSubToolbar_GetsOutOfTheWay()
    {
        var layout = _layout with
        {
            Sides = [new SideLayout(SideEdge.Right, SideKind.Widgets, [new SideColumn(240, [new WindowGroup(["a"])])])],
        };

        // Pushed back to the bottom of the region, its sub-toolbar goes above it, so it reaches further up.
        var drag = DragWithSubToolbar(layout, parent: "file", dragged: "file");
        var preview = drag.PreviewAt(new LayoutPoint(950, 575));

        var sub = preview.SubToolbar!.Bounds;
        var landing = preview.PushedBackTo!.Value;
        var onlyUnderTheSubToolbar = drag.Zones.Where(zone => zone.Bounds.Intersects(sub) && !zone.Bounds.Intersects(landing));

        Assert.All(onlyUnderTheSubToolbar, zone => Assert.Contains(zone.Anchor, preview.CoveredZones));
        Assert.Contains(ToolbarAnchor.RightMiddle, preview.CoveredZones);
    }

    [Fact]
    public void AnyDragWithASubToolbar_KeepsZonesClearOfItAndOfTheToolbars()
    {
        var names = Gen.OneOfConst("file", "tools", "edit", "view");

        Gen.Select(Valid, names, names, WindowSizes, Gen.Double[20, 900]).Sample(
            (layout, parent, dragged, window, length) =>
            {
                var shapes = new ToolbarShapes(new LayoutSize(length, 40), new LayoutSize(40, length));
                var measurements = Measure(window, Sizes(layout, _ => length)) with
                {
                    SubToolbar = new SubToolbarRequest(parent, new ToolbarShapes(new LayoutSize(300, 40), new LayoutSize(40, 300))),
                };
                var drag = new ToolbarDrag(layout, dragged, _heldAtTheLeftEnd, measurements, shapes, Metrics);

                var shown = LayoutSolver.Solve(drag.Others, measurements, Metrics, drag.ReservedSpots);
                var anchored = shown.Toolbars.Where(toolbar => toolbar.Anchor is not null).ToList();

                foreach (var zone in drag.Zones)
                {
                    Assert.DoesNotContain(anchored, toolbar => toolbar.Bounds.Intersects(zone.Bounds));

                    var parentIsAnchored = anchored.Any(toolbar => toolbar.ToolbarId == parent);
                    if (shown.SubToolbar is { } sub && parentIsAnchored)
                        Assert.False(sub.Bounds.Intersects(zone.Bounds));
                }
            });
    }

    [Fact]
    public void AnyDragOfAnyLayout_KeepsTheRules()
    {
        var positions = Gen.Select(Gen.Double[-100, 1100], Gen.Double[-100, 800], (x, y) => new LayoutPoint(x, y));

        Gen.Select(Valid, Gen.OneOfConst("file", "tools", "edit", "view"), positions).Sample((layout, toolbarId, position) =>
        {
            var measurements = Measure(_window, Sizes(layout, _ => 200));
            var shapes = new ToolbarShapes(new LayoutSize(200, 40), new LayoutSize(40, 200));
            var dragged = new ToolbarDrag(layout, toolbarId, _heldAtTheLeftEnd, measurements, shapes, Metrics)
                .LayoutAt(position);

            Assert.Equal(TestLayouts.Text(dragged), TestLayouts.Text(LayoutRepair.Repair(dragged, Catalog)));
            Assert.True(dragged.IsToolbarVisible(toolbarId));
        });
    }

    private static ToolbarDrag DragWithSubToolbar(DockingLayout layout, string parent, string dragged)
    {
        var shapes = new ToolbarShapes(new LayoutSize(100, 30), new LayoutSize(30, 100));
        var measurements = Measure(_window) with { SubToolbar = new SubToolbarRequest(parent, shapes) };
        return new ToolbarDrag(layout, dragged, _heldAtTheLeftEnd, measurements, Shapes(dragged), Metrics);
    }

    private static ToolbarDrag Drag(DockingLayout layout, string toolbarId) =>
        Drag(layout, toolbarId, Shapes(toolbarId));

    private static ToolbarDrag Drag(DockingLayout layout, string toolbarId, ToolbarShapes shapes) =>
        new(layout, toolbarId, _heldAtTheLeftEnd, Measure(_window), shapes, Metrics);

    private static ToolbarShapes Shapes(string toolbarId)
    {
        var size = ToolbarSizes[toolbarId];
        return new ToolbarShapes(size, new LayoutSize(size.Height, size.Width));
    }

    private static Dictionary<ToolbarAnchor, LayoutRect> ZonesOf(ToolbarDrag drag) =>
        drag.Zones.ToDictionary(zone => zone.Anchor, zone => zone.Bounds);

    private static LayoutRect Shown(ToolbarDrag drag, string toolbarId) =>
        LayoutSolver.Solve(drag.Others, Measure(_window), Metrics, drag.ReservedSpots).Toolbars
            .Single(toolbar => toolbar.ToolbarId == toolbarId).Bounds;

    private static ToolbarPlacement Toolbar(DockingLayout layout, string toolbarId) =>
        layout.Toolbars.Single(toolbar => toolbar.ToolbarId == toolbarId);

    private static LayoutPoint Center(ToolbarDrag drag, ToolbarAnchor anchor)
    {
        var bounds = drag.Zones.Single(zone => zone.Anchor == anchor).Bounds;
        return new LayoutPoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }
}
