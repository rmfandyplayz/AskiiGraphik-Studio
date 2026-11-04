// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using Avalonia;
using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// Puts windows and toolbars where <see cref="LayoutSolver"/> says. It has no background, so any spot
/// without a window or toolbar isn't hit by the pointer, and clicks reach the canvas beneath (ADR 0035).
/// All the rules are in the solver: this only measures views and places them.
/// </summary>
internal sealed class DockingHost : Panel
{
    private const int WindowLayer = 0;
    private const int ToolbarLayer = 1;

    private readonly Dictionary<string, WindowFrame> _windows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Control> _toolbars = new(StringComparer.Ordinal);

    private DockingLayout? _layout;
    private ShellCatalog? _catalog;
    private DockingMetrics? _metrics;
    private SolvedLayout? _solved;

    /// <summary>How much of each edge windows and toolbars take from the canvas.</summary>
    public LayoutInsets SafeArea => _solved?.SafeArea ?? default;

    /// <summary>Shows a layout. A window or toolbar that was already showing keeps its view.</summary>
    public void Show(DockingLayout layout, ShellCatalog catalog, DockingMetrics metrics)
    {
        _layout = layout;
        _catalog = catalog;
        _metrics = metrics;

        var windowIds = layout.OpenWindowIds().Where(catalog.Windows.ContainsKey).ToHashSet(StringComparer.Ordinal);
        var toolbarIds = layout.Toolbars
            .Where(toolbar => toolbar.IsVisible && catalog.Toolbars.ContainsKey(toolbar.ToolbarId))
            .Select(toolbar => toolbar.ToolbarId)
            .ToHashSet(StringComparer.Ordinal);

        RemoveAllExcept(_windows, windowIds);
        RemoveAllExcept(_toolbars, toolbarIds);

        foreach (var windowId in windowIds.Where(id => !_windows.ContainsKey(id)))
        {
            var entry = catalog.Windows[windowId];
            var frame = new WindowFrame { Title = entry.Title, Body = entry.CreateView(), ZIndex = WindowLayer };
            _windows.Add(windowId, frame);
            Children.Add(frame);
        }

        foreach (var toolbarId in toolbarIds.Where(id => !_toolbars.ContainsKey(id)))
        {
            var view = catalog.Toolbars[toolbarId].CreateView();
            view.ZIndex = ToolbarLayer;
            _toolbars.Add(toolbarId, view);
            Children.Add(view);
        }

        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var available = new LayoutSize(FiniteOrZero(availableSize.Width), FiniteOrZero(availableSize.Height));
        if (_layout is not { } layout || _catalog is not { } catalog || _metrics is not { } metrics)
            return new Size(available.Width, available.Height);

        // Unlimited first, to learn the size each toolbar wants. The solver then decides what it gets.
        var toolbarSizes = new Dictionary<string, LayoutSize>(StringComparer.Ordinal);
        foreach (var (toolbarId, view) in _toolbars)
        {
            view.Measure(Size.Infinity);
            toolbarSizes[toolbarId] = new LayoutSize(view.DesiredSize.Width, view.DesiredSize.Height);
        }

        var measurements = new LayoutMeasurements(available, catalog.MinimumWindowHeights, toolbarSizes);
        _solved = LayoutSolver.Solve(layout, measurements, metrics);

        foreach (var group in _solved.WindowGroups)
        {
            if (!_windows.TryGetValue(group.WindowIds[0], out var frame))
                continue;

            frame.IsTitleOnly = group.IsTitleOnly;
            frame.Measure(new Size(group.Bounds.Width, group.Bounds.Height));
        }

        foreach (var toolbar in _solved.Toolbars)
            _toolbars[toolbar.ToolbarId].Measure(new Size(toolbar.Bounds.Width, toolbar.Bounds.Height));

        return new Size(available.Width, available.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_solved is not { } solved)
            return finalSize;

        foreach (var group in solved.WindowGroups)
        {
            // Only the first of a group's tabs shows until docks arrive in step 2.5f.
            for (var tab = 0; tab < group.WindowIds.Count; tab++)
            {
                if (!_windows.TryGetValue(group.WindowIds[tab], out var frame))
                    continue;

                frame.IsVisible = tab == 0;
                frame.Arrange(ToRect(group.Bounds));
            }
        }

        // A toolbar the solver left out for lack of room stays measured, so it's noticed when there's room again,
        // but has no size, so it can't be seen or clicked.
        var bounds = solved.Toolbars.ToDictionary(toolbar => toolbar.ToolbarId, toolbar => ToRect(toolbar.Bounds), StringComparer.Ordinal);
        foreach (var (toolbarId, view) in _toolbars)
            view.Arrange(bounds.GetValueOrDefault(toolbarId));

        return finalSize;
    }

    private void RemoveAllExcept<TView>(Dictionary<string, TView> views, HashSet<string> keep)
        where TView : Control
    {
        foreach (var id in views.Keys.Where(id => !keep.Contains(id)).ToList())
        {
            Children.Remove(views[id]);
            views.Remove(id);
        }
    }

    private static Rect ToRect(LayoutRect bounds) => new(bounds.X, bounds.Y, bounds.Width, bounds.Height);

    private static double FiniteOrZero(double size) => double.IsFinite(size) ? size : 0;
}
