// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Docking.Layouts;

/// <summary>What exists, for repairing a layout that may be from another version or hand-edited.</summary>
/// <param name="WindowIds">Every window the app has.</param>
/// <param name="DefaultToolbars">Every toolbar the app has, in its default place.</param>
/// <param name="DefaultSideWidth">The width of a side that has none.</param>
public sealed record DockingCatalog(
    IReadOnlyList<string> WindowIds,
    IReadOnlyList<ToolbarPlacement> DefaultToolbars,
    double DefaultSideWidth);

/// <summary>
/// Turns any layout into one that keeps the rules, changing as little as possible. Repairing a layout that
/// already keeps them changes nothing. Afterwards every window the app has is either open or closed exactly
/// once, so a window missing from the saved layout counts as new, and opens.
/// </summary>
public static class LayoutRepair
{
    /// <summary>The layout, with anything invalid put right.</summary>
    public static DockingLayout Repair(DockingLayout layout, DockingCatalog catalog)
    {
        var known = catalog.WindowIds.ToHashSet(StringComparer.Ordinal);
        var placed = new HashSet<string>(StringComparer.Ordinal);
        var sides = new List<SideLayout>();

        foreach (var side in layout.Sides.Where(side => side is not null))
        {
            if (!Enum.IsDefined(side.Edge) || !Enum.IsDefined(side.Kind) || sides.Any(other => other.Edge == side.Edge))
                continue;

            if (RepairSide(side, known, placed, catalog.DefaultSideWidth) is { } repaired)
                sides.Add(repaired);
        }

        var floating = RepairFloatingWindows(layout, known, placed, catalog.DefaultSideWidth);
        var closed = RepairClosedWindows(layout, known, placed, catalog.DefaultSideWidth);
        var result = new DockingLayout(sides, RepairToolbars(layout, catalog), closed) { FloatingWindows = floating };
        return OpenMissingWindows(result, catalog);
    }

    private static SideLayout? RepairSide(SideLayout side, HashSet<string> known, HashSet<string> placed,
        double defaultWidth)
    {
        var columns = side.Kind == SideKind.Widgets
            ? RepairWidgetColumn(side, known, placed, defaultWidth)
            : RepairDockColumns(side, known, placed, defaultWidth);

        return columns.Count == 0 ? null : side with { Columns = columns };
    }

    private static List<SideColumn> RepairDockColumns(SideLayout side, HashSet<string> known, HashSet<string> placed,
        double defaultWidth)
    {
        var columns = new List<SideColumn>();
        foreach (var column in side.Columns.Where(column => column is not null))
        {
            var groups = column.Groups
                .Where(group => group is not null)
                .Select(group => group with
                {
                    WindowIds = [.. group.WindowIds.Where(id => TryPlace(id, known, placed))],
                    Share = ValidSize(group.Share, 1),
                })
                .Where(group => group.WindowIds.Count > 0)
                .ToList();

            if (groups.Count > 0)
                columns.Add(new SideColumn(ValidSize(column.Width, defaultWidth), LayoutEdits.WithOneGroupOpen(groups)));
        }

        return columns;
    }

    // A widget side has one column of single windows. Any more than it can hold aren't marked as placed, so
    // they count as missing and go wherever there's room.
    private static List<SideColumn> RepairWidgetColumn(SideLayout side, HashSet<string> known, HashSet<string> placed,
        double defaultWidth)
    {
        var groups = new List<WindowGroup>();
        var columns = side.Columns.Where(column => column is not null).ToList();

        foreach (var group in columns.SelectMany(column => column.Groups).Where(group => group is not null))
        {
            foreach (var windowId in group.WindowIds)
            {
                if (groups.Count < DockingLayout.MaximumWidgetsOnASide && TryPlace(windowId, known, placed))
                    groups.Add(new WindowGroup([windowId], ValidSize(group.Share, 1), group.IsTitleOnly));
            }
        }

        return groups.Count == 0
            ? []
            : [new SideColumn(ValidSize(columns[0].Width, defaultWidth), LayoutEdits.WithOneGroupOpen(groups))];
    }

    private static bool TryPlace(string windowId, HashSet<string> known, HashSet<string> placed) =>
        windowId is not null && known.Contains(windowId) && placed.Add(windowId);

    private static List<FloatingWindow> RepairFloatingWindows(DockingLayout layout, HashSet<string> known,
        HashSet<string> placed, double defaultSize) =>
    [
        .. layout.FloatingWindows
            .Where(window => window is not null && TryPlace(window.WindowId, known, placed))
            .Select(window => Repaired(window, defaultSize)),
    ];

    // Being far outside the window is fine, because the solver moves it just enough to stay inside.
    private static FloatingWindow Repaired(FloatingWindow window, double defaultSize) => window with
    {
        Position = double.IsFinite(window.Position.X) && double.IsFinite(window.Position.Y) ? window.Position : default,
        Width = ValidSize(window.Width, defaultSize),
        Height = ValidSize(window.Height, defaultSize),
    };

    private static List<ClosedWindow> RepairClosedWindows(DockingLayout layout, HashSet<string> known,
        HashSet<string> placed, double defaultWidth)
    {
        var closed = new List<ClosedWindow>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var window in layout.ClosedWindows.Where(window => window is not null))
        {
            if (window.WindowId is null || !known.Contains(window.WindowId) || placed.Contains(window.WindowId) ||
                !seen.Add(window.WindowId))
            {
                continue;
            }

            closed.Add(window with
            {
                Edge = Enum.IsDefined(window.Edge) ? window.Edge : SideEdge.Right,
                Position = Math.Max(0, window.Position),
                Share = ValidSize(window.Share, 1),
                SideWidth = ValidSize(window.SideWidth, defaultWidth),
                WasFloating = window.WasFloating is { } floating
                    ? Repaired(floating with { WindowId = window.WindowId }, defaultWidth)
                    : null,
            });
        }

        return closed;
    }

    private static DockingLayout OpenMissingWindows(DockingLayout layout, DockingCatalog catalog)
    {
        foreach (var windowId in catalog.WindowIds.Distinct(StringComparer.Ordinal))
        {
            if (layout.IsWindowOpen(windowId) || layout.ClosedWindows.Any(closed => closed.WindowId == windowId))
                continue;

            var opened = LayoutEdits.OpenWindow(layout, windowId, catalog.DefaultSideWidth);
            if (!opened.IsWindowOpen(windowId))
            {
                var noRoom = new ClosedWindow(windowId, SideEdge.Right, int.MaxValue, 1, catalog.DefaultSideWidth);
                opened = layout with { ClosedWindows = [.. layout.ClosedWindows, noRoom] };
            }

            layout = opened;
        }

        return layout;
    }

    private static List<ToolbarPlacement> RepairToolbars(DockingLayout layout, DockingCatalog catalog)
    {
        var defaults = catalog.DefaultToolbars.ToDictionary(toolbar => toolbar.ToolbarId, StringComparer.Ordinal);
        var saved = layout.Toolbars.Where(toolbar => toolbar?.ToolbarId is not null && defaults.ContainsKey(toolbar.ToolbarId));
        var missing = catalog.DefaultToolbars.Where(toolbar =>
            !layout.Toolbars.Any(other => other is not null && other.ToolbarId == toolbar.ToolbarId));

        var repaired = new List<ToolbarPlacement>();
        var taken = new HashSet<ToolbarAnchor>();

        foreach (var toolbar in saved.Concat(missing))
        {
            if (repaired.Any(other => other.ToolbarId == toolbar.ToolbarId))
                continue;

            repaired.Add(RepairToolbar(toolbar, defaults[toolbar.ToolbarId], taken));
        }

        return repaired;
    }

    private static ToolbarPlacement RepairToolbar(ToolbarPlacement toolbar, ToolbarPlacement fallback,
        HashSet<ToolbarAnchor> taken)
    {
        var anchor = Enum.IsDefined(toolbar.Anchor) ? toolbar.Anchor : fallback.Anchor;
        var orientation = Enum.IsDefined(toolbar.Orientation) ? toolbar.Orientation : fallback.Orientation;
        var side = Enum.IsDefined(toolbar.SubToolbarSide) ? toolbar.SubToolbarSide : fallback.SubToolbarSide;

        // A floating toolbar holds no anchor and chooses its own direction. Being far outside the window is fine,
        // because the solver moves it just enough to stay inside.
        if (toolbar.FloatingAt is { } position && double.IsFinite(position.X) && double.IsFinite(position.Y))
            return toolbar with { Anchor = anchor, Orientation = orientation, SubToolbarSide = side };

        var valid = toolbar with
        {
            Anchor = anchor,
            Orientation = ToolbarAnchors.RequiredOrientation(anchor) ?? orientation,
            FloatingAt = null,
            SubToolbarSide = side,
        };

        if (!valid.IsVisible)
            return valid;

        if (taken.Add(anchor))
            return valid;

        foreach (var free in ToolbarAnchors.All)
        {
            if (taken.Add(free))
                return LayoutEdits.Placed(valid, free);
        }

        return valid with { IsVisible = false };
    }

    private static double ValidSize(double size, double fallback) =>
        double.IsFinite(size) && size > 0 ? size : fallback;
}
