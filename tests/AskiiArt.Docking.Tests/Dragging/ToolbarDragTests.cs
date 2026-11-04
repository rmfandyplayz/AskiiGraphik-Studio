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
        var zones = Drag(_layout, "file").Zones.ToDictionary(zone => zone.Anchor, zone => zone.Bounds);
        var (margin, corner) = (Metrics.Margin, Metrics.ToolbarZoneCornerSize);

        Assert.Equal(new LayoutRect(margin, margin, corner, corner), zones[ToolbarAnchor.TopLeft]);
        Assert.Equal(_window.Width - margin, zones[ToolbarAnchor.BottomRight].Right, Tolerance);
        Assert.Equal(_window.Height - margin, zones[ToolbarAnchor.BottomRight].Bottom, Tolerance);

        var left = zones[ToolbarAnchor.LeftMiddle];
        Assert.Equal(margin, left.X);
        Assert.Equal(Metrics.ToolbarZoneEdgeThickness, left.Width);
        Assert.True(left.Y > zones[ToolbarAnchor.TopLeft].Bottom);
        Assert.True(left.Bottom < zones[ToolbarAnchor.BottomLeft].Y);
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
    public void AnyDragOfAnyLayout_KeepsTheRules()
    {
        var positions = Gen.Select(Gen.Double[-100, 1100], Gen.Double[-100, 800], (x, y) => new LayoutPoint(x, y));

        Gen.Select(Valid, Gen.OneOfConst("file", "tools", "edit", "view"), positions).Sample((layout, toolbarId, position) =>
        {
            var measurements = Measure(_window, Sizes(layout, _ => 200));
            var dragged = new ToolbarDrag(layout, toolbarId, _heldAtTheLeftEnd, measurements, Metrics).LayoutAt(position);

            Assert.Equal(TestLayouts.Text(dragged), TestLayouts.Text(LayoutRepair.Repair(dragged, Catalog)));
            Assert.True(dragged.IsToolbarVisible(toolbarId));
        });
    }

    private static ToolbarDrag Drag(DockingLayout layout, string toolbarId) =>
        new(layout, toolbarId, _heldAtTheLeftEnd, Measure(_window), Metrics);

    private static ToolbarPlacement Toolbar(DockingLayout layout, string toolbarId) =>
        layout.Toolbars.Single(toolbar => toolbar.ToolbarId == toolbarId);

    private static LayoutPoint Center(ToolbarDrag drag, ToolbarAnchor anchor)
    {
        var bounds = drag.Zones.Single(zone => zone.Anchor == anchor).Bounds;
        return new LayoutPoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }
}
