// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// Creates the shell's default layout and its panels (ADR 0025), and keeps layouts within its rules:
/// the menu bar, toolbar and status bar stay put, and side panels keep a fixed size however they got
/// docked. On macOS the menu bar lives in the system menu bar, so there's no panel for it.
/// </summary>
internal sealed class ShellDockFactory(ShellLayoutMetrics metrics, bool hasMenuBarPanel) : Factory
{
    public override IRootDock CreateLayout()
    {
        var rows = new List<IDockable>();
        if (hasMenuBarPanel)
            rows.Add(CreateFixedRow(ShellDockIds.MenuBarDock, ShellDockIds.MenuBarPanel, metrics.MenuBarHeight));

        rows.Add(CreateFixedRow(ShellDockIds.ToolbarDock, ShellDockIds.ToolbarPanel, metrics.ToolbarHeight));
        rows.Add(CreateMiddleRow());
        rows.Add(CreateFixedRow(ShellDockIds.StatusBarDock, ShellDockIds.StatusBarPanel, metrics.StatusBarHeight));

        var main = new ProportionalDock
        {
            Orientation = Orientation.Vertical,
            VisibleDockables = CreateList([.. rows]),
        };

        var root = CreateRootDock();
        root.FloatingWindowHostMode = DockFloatingWindowHostMode.Managed;
        root.VisibleDockables = CreateList<IDockable>(main);
        root.DefaultDockable = main;
        root.ActiveDockable = main;
        return root;
    }

    /// <summary>A movable panel, ready to go into a dock.</summary>
    public static Tool CreatePanel(string panelId) => new()
    {
        Id = panelId,
        Title = ShellDockIds.PanelTitle(panelId),
        CanPin = false,
        CanDockAsDocument = false,
    };

    /// <summary>Every dockable in the layout, floating ones included.</summary>
    public static IEnumerable<IDockable> AllDockables(IRootDock root)
    {
        foreach (var dockable in Descendants(root))
            yield return dockable;

        if (root.Windows is null)
            yield break;

        foreach (var window in root.Windows)
        {
            if (window.Layout is null)
                continue;

            foreach (var dockable in Descendants(window.Layout))
                yield return dockable;
        }
    }

    /// <summary>
    /// False when a saved layout lacks something the shell can't do without, for example because
    /// it came from an older version.
    /// </summary>
    public bool IsUsable(IRootDock root)
    {
        var ids = AllDockables(root).Select(dockable => dockable.Id).ToHashSet(StringComparer.Ordinal);

        return ids.Contains(ShellDockIds.CanvasDock) && ids.Contains(ShellDockIds.ToolbarPanel) &&
               ids.Contains(ShellDockIds.StatusBarPanel) && ids.Contains(ShellDockIds.MenuBarPanel) == hasMenuBarPanel;
    }

    /// <summary>Shows titles in the current language; a saved layout has the old language's.</summary>
    public static void RefreshPanelTitles(IRootDock root)
    {
        foreach (var dockable in AllDockables(root).Where(dockable => dockable is ITool))
            dockable.Title = ShellDockIds.PanelTitle(dockable.Id);
    }

    /// <summary>Puts a panel back: beside another movable panel, or at the side of the canvas if there's none.</summary>
    public void OpenPanel(IRootDock root, string panelId)
    {
        var panel = CreatePanel(panelId);
        var host = AllDockables(root).OfType<IToolDock>()
            .FirstOrDefault(dock => dock.CanDrop && dock.VisibleDockables is { Count: > 0 });

        if (host is not null)
        {
            AddDockable(host, panel);
            InitDockable(panel, host);
            SetActiveDockable(panel);
            SetFocusedDockable(host, panel);
            return;
        }

        var side = CreateSideDock(panel);
        InitDockable(side, null);
        var canvas = AllDockables(root).OfType<IToolDock>().First(dock => dock.Id == ShellDockIds.CanvasDock);
        SplitToDock(canvas, side, DockOperation.Right);

        ApplySideSizes(root);
    }

    public override void OnDockableDocked(IDockable? dockable, DockOperation operation)
    {
        base.OnDockableDocked(dockable, operation);

        if (dockable is not null && FindRoot(dockable, _ => true) is { } root)
            ApplySideSizes(root);
    }

    // A new split doesn't know the shell's sizes. Without this, a panel dropped beside the canvas
    // would take half of the canvas's space.
    private void ApplySideSizes(IRootDock root)
    {
        foreach (var parent in Descendants(root).OfType<IProportionalDock>())
        {
            foreach (var child in parent.VisibleDockables ?? [])
            {
                if (child is IProportionalDockSplitter || ContainsCanvasDock(child))
                    continue;

                if (parent.Orientation == Orientation.Horizontal && double.IsNaN(child.MaxWidth))
                    child.MinWidth = child.MaxWidth = metrics.SideWidth;

                // Inside the side column, panels share its height. Beside the canvas, they don't.
                var isBesideCanvas = parent.VisibleDockables?.Any(ContainsCanvasDock) == true;
                if (parent.Orientation == Orientation.Vertical && isBesideCanvas && double.IsNaN(child.MaxHeight))
                    child.MinHeight = child.MaxHeight = metrics.SideWidth;
            }
        }
    }

    private static bool ContainsCanvasDock(IDockable dockable) =>
        dockable.Id == ShellDockIds.CanvasDock ||
        Descendants(dockable).Any(child => child.Id == ShellDockIds.CanvasDock);

    private static IEnumerable<IDockable> Descendants(IDockable dockable)
    {
        if (dockable is not IDock { VisibleDockables: { } children })
            yield break;

        foreach (var child in children)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }

    private ProportionalDock CreateMiddleRow()
    {
        var canvas = new ToolDock
        {
            Id = ShellDockIds.CanvasDock,
            IsCollapsable = false,
            CanDrop = false,
            CanDrag = false,
            CanFloat = false,
            CanClose = false,
        };

        var layers = CreateSideDock(CreatePanel(ShellDockIds.LayersPanel));
        var history = CreateSideDock(CreatePanel(ShellDockIds.HistoryPanel));
        var side = new ProportionalDock
        {
            Orientation = Orientation.Vertical,
            MinWidth = metrics.SideWidth,
            MaxWidth = metrics.SideWidth,
            VisibleDockables = CreateList<IDockable>(layers, new ProportionalDockSplitter(), history),
        };

        return new ProportionalDock
        {
            Orientation = Orientation.Horizontal,
            IsCollapsable = false,
            VisibleDockables = CreateList<IDockable>(canvas, side),
        };
    }

    private ToolDock CreateSideDock(Tool panel) => new()
    {
        Alignment = Alignment.Right,
        VisibleDockables = CreateList<IDockable>(panel),
        ActiveDockable = panel,
    };

    private ToolDock CreateFixedRow(string dockId, string panelId, double height)
    {
        var panel = new Tool
        {
            Id = panelId,
            Title = ShellDockIds.PanelTitle(panelId),
            CanClose = false,
            CanDrag = false,
            CanDrop = false,
            CanFloat = false,
            CanPin = false,
            CanDockAsDocument = false,
        };

        return new ToolDock
        {
            Id = dockId,
            GripMode = GripMode.Hidden,
            IsCollapsable = false,
            CanClose = false,
            CanDrag = false,
            CanDrop = false,
            CanFloat = false,
            CanPin = false,
            MinHeight = height,
            MaxHeight = height,
            VisibleDockables = CreateList<IDockable>(panel),
            ActiveDockable = panel,
        };
    }
}
