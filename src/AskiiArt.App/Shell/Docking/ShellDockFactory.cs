// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// Creates the shell's default layout and its panels (ADR 0025), and keeps layouts within its rules:
/// nothing lands on the canvas's dock, and side panels keep a fixed size however they got docked.
/// </summary>
internal sealed class ShellDockFactory(ShellLayoutMetrics metrics) : Factory
{
    public override IRootDock CreateLayout()
    {
        var whole = new ProportionalDock
        {
            Orientation = Orientation.Vertical,
            IsCollapsable = false,
            VisibleDockables = CreateList<IDockable>(CreateToolbarRow(), CreateMainRow()),
        };

        var root = CreateRootDock();
        root.FloatingWindowHostMode = DockFloatingWindowHostMode.Managed;
        root.VisibleDockables = CreateList<IDockable>(whole);
        root.DefaultDockable = whole;
        root.ActiveDockable = whole;
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
    /// False when a saved layout lacks the canvas's dock or holds panels the shell doesn't have, for
    /// example because it came from another version.
    /// </summary>
    public static bool IsUsable(IRootDock root)
    {
        var dockables = AllDockables(root).ToList();

        return dockables.Any(dockable => dockable.Id == ShellDockIds.CanvasDock) &&
               dockables.OfType<ITool>().All(panel => ShellDockIds.IsPanel(panel.Id));
    }

    /// <summary>Shows titles in the current language; a saved layout has the old language's.</summary>
    public static void RefreshPanelTitles(IRootDock root)
    {
        foreach (var dockable in AllDockables(root).Where(dockable => dockable is ITool))
            dockable.Title = ShellDockIds.PanelTitle(dockable.Id);
    }

    /// <summary>
    /// Puts a panel back. The toolbar goes along the top; another panel goes beside a docked panel,
    /// or at the side of the canvas if there's none.
    /// </summary>
    public void OpenPanel(IRootDock root, string panelId)
    {
        if (panelId == ShellDockIds.ToolbarPanel)
        {
            OpenToolbar(root);
            return;
        }

        var panel = CreatePanel(panelId);
        var host = Descendants(root).OfType<IToolDock>()
            .FirstOrDefault(dock => dock.VisibleDockables?.Any(IsMovablePanel) == true);

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

    private void OpenToolbar(IRootDock root)
    {
        var row = CreateToolbarRow();
        InitDockable(row, null);
        SplitToDock((IDock)root.VisibleDockables![0], row, DockOperation.Top);
        ApplySideSizes(root);
    }

    private static bool IsMovablePanel(IDockable dockable) =>
        dockable is ITool && dockable.Id != ShellDockIds.ToolbarPanel;

    public override void OnDockableDocked(IDockable? dockable, DockOperation operation)
    {
        base.OnDockableDocked(dockable, operation);

        if (dockable is not null && FindRoot(dockable, _ => true) is { } root)
            ApplySideSizes(root);
    }

    // Dock's menu offers Float on a panel that already floats alone. Floating it again would only
    // make a new window, a little smaller than the last.
    public override void FloatDockable(IDockable dockable, DockWindowOptions? options)
    {
        if (!IsFloatingAlone(dockable))
            base.FloatDockable(dockable, options);
    }

    private bool IsFloatingAlone(IDockable dockable) =>
        FindRoot(dockable, _ => true) is { } root && !ContainsCanvasDock(root) &&
        Descendants(root).Count(descendant => descendant is ITool) == 1;

    // A new split doesn't know the shell's sizes. Without this, a panel dropped beside the canvas
    // would take half of the canvas's space. Only the maximums are set: the canvas's dock takes what's
    // left, and Dock passes a dock's minimum on to the controls inside it, which then overflow it.
    private void ApplySideSizes(IRootDock root)
    {
        foreach (var parent in Descendants(root).OfType<IProportionalDock>())
        {
            foreach (var child in parent.VisibleDockables ?? [])
            {
                if (child is IProportionalDockSplitter || ContainsCanvasDock(child))
                    continue;

                if (parent.Orientation == Orientation.Horizontal && double.IsNaN(child.MaxWidth))
                    child.MaxWidth = metrics.SideWidth;

                // Inside the side column, panels share its height. Beside the canvas, they don't.
                var isBesideCanvas = parent.VisibleDockables?.Any(ContainsCanvasDock) == true;
                if (parent.Orientation == Orientation.Vertical && isBesideCanvas && double.IsNaN(child.MaxHeight))
                    child.MaxHeight = HoldsOnlyTheToolbar(child) ? metrics.ToolbarHeight : metrics.SideWidth;
            }
        }
    }

    private static bool HoldsOnlyTheToolbar(IDockable dockable) =>
        Descendants(dockable).OfType<ITool>().All(tool => tool.Id == ShellDockIds.ToolbarPanel);

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

    private ToolDock CreateToolbarRow()
    {
        var row = CreateSideDock(CreatePanel(ShellDockIds.ToolbarPanel));
        row.Alignment = Alignment.Top;
        row.MaxHeight = metrics.ToolbarHeight;
        return row;
    }

    private ProportionalDock CreateMainRow()
    {
        // With CanDrop off, Dock shows no targets at all over this dock, not even the window-edge
        // ones. So it accepts drops, and its docking group is what keeps panels off it: it offers no
        // target of its own, and releasing a panel over it floats the panel.
        var canvas = new ToolDock
        {
            Id = ShellDockIds.CanvasDock,
            DockGroup = ShellDockIds.CanvasDockGroup,
            IsCollapsable = false,
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
}
