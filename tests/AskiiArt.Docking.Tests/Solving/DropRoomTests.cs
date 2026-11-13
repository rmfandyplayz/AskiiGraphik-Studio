// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Solving;

/// <summary>The room the solver leaves for a drop's marker while a window is held (docs/DOCKING.md §5.2).</summary>
public sealed class DropRoomTests
{
    private const double Tolerance = 0.001;

    private static readonly LayoutSize _window = new(1280, 720);

    private static SolvedLayout Solve(DockingLayout layout, DropRoom room) =>
        LayoutSolver.Solve(layout, Measure(_window) with { DropRoom = room }, Metrics);

    [Fact]
    public void InAWidgetStack_TheWindowsBesideItGiveHalfEach_AndTheMarkerFillsTheRoom()
    {
        var layout = Widgets(SideEdge.Right, Tabs("a"), Tabs("b"));

        var solved = Solve(layout, new DropRoom(DropRoomKind.Stack, SideEdge.Right, 48, Position: 1));

        Assert.Equal(new LayoutRect(1032, 8, 240, 320), solved.PlaceOfWindow("a")!.Value.Bounds);
        Assert.Equal(new LayoutRect(1032, 392, 240, 320), solved.PlaceOfWindow("b")!.Value.Bounds);
        Assert.Equal(new LayoutRect(1032, 336, 240, 48), solved.DropMarker);
    }

    [Fact]
    public void OnAnEmptySide_ItOpensBetweenTheMargins_AndTheOtherSideStaysPut()
    {
        var layout = Widgets(SideEdge.Right, Tabs("a"));
        var before = LayoutSolver.Solve(layout, Measure(_window), Metrics);

        var solved = Solve(layout, new DropRoom(DropRoomKind.EmptySide, SideEdge.Left, 48));

        Assert.Equal(new LayoutRect(8, 8, 48, 704), solved.DropMarker);
        Assert.Equal(before.PlaceOfWindow("a"), solved.PlaceOfWindow("a"));
    }

    [Fact]
    public void AtTheOuterEdgeOfALeftDock_EveryColumnMovesRight()
    {
        var layout = Dock(SideEdge.Left, Column(Tabs("a")), Column(Tabs("b")));

        var solved = Solve(layout, new DropRoom(DropRoomKind.Column, SideEdge.Left, 28, Column: 0));

        Assert.Equal(new LayoutRect(28, 0, 240, 720), solved.PlaceOfWindow("a")!.Value.Bounds);
        Assert.Equal(new LayoutRect(268, 0, 240, 720), solved.PlaceOfWindow("b")!.Value.Bounds);
        Assert.Equal(new LayoutRect(4, 4, 20, 712), solved.DropMarker);
    }

    [Fact]
    public void WithNoRoom_ThereIsNoMarker()
    {
        var solved = LayoutSolver.Solve(Widgets(SideEdge.Right, Tabs("a")), Measure(_window), Metrics);

        Assert.Null(solved.DropMarker);
    }

    [Fact]
    public void ForAnyHeldWindowAndPointer_TheMarkerStaysInside_AndOverlapsNoWindow()
    {
        Gen.Select(Valid, WindowSizes, Gen.Int[0, 100], Gen.Double[0, 1], Gen.Double[0, 1])
            .Sample((layout, size, pick, across, down) =>
            {
                var windowIds = layout.OpenWindowIds().ToList();
                if (windowIds.Count == 0)
                    return;

                var measurements = Measure(size);
                var drag = new WindowDrag(layout, windowIds[pick % windowIds.Count], new LayoutPoint(0.1, 0.05),
                    measurements, Metrics);
                var preview = drag.PreviewAt(new LayoutPoint(size.Width * across, size.Height * down));
                if (preview.Room is null)
                    return;

                var solved = LayoutSolver.Solve(preview.Layout, measurements with { DropRoom = preview.Room },
                    Metrics);
                var marker = Assert.NotNull(solved.DropMarker);

                Assert.True(marker.X >= -Tolerance && marker.Y >= -Tolerance, $"{marker} starts outside {size}.");
                Assert.True(marker.Right <= size.Width + Tolerance && marker.Bottom <= size.Height + Tolerance,
                    $"{marker} ends outside {size}.");
                Assert.All(solved.WindowGroups, group => Assert.False(Overlap(marker, group.Bounds),
                    $"{marker} overlaps {group.Bounds}."));
            });
    }

    private static bool Overlap(LayoutRect a, LayoutRect b) =>
        a.X < b.Right - Tolerance && b.X < a.Right - Tolerance && a.Y < b.Bottom - Tolerance && b.Y < a.Bottom - Tolerance;
}
