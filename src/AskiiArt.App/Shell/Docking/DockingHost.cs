// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics;
using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AskiiArt.App.Shell.Docking;

/// <summary>A toolbar's view has just been made.</summary>
internal sealed class ToolbarFrameCreatedEventArgs(string toolbarId, ToolbarFrame frame) : EventArgs
{
    public string ToolbarId { get; } = toolbarId;

    public ToolbarFrame Frame { get; } = frame;
}

/// <summary>
/// Puts windows and toolbars where <see cref="LayoutSolver"/> says. It has no background, so any spot
/// without a window or toolbar isn't hit by the pointer, and clicks reach the canvas beneath (ADR 0035).
/// All the rules are in the solver: this only measures views and places them.
/// </summary>
internal sealed class DockingHost : Panel
{
    // From back to front (docs/DOCKING.md §2). The gaps leave room for sub-toolbars and floating windows, and
    // floating toolbars add their place in the layout's list to theirs.
    private const int WindowLayer = 0;
    private const int ToolbarLayer = 100;
    private const int FloatingToolbarLayer = 200;
    private const int DropZoneLayer = 1000;
    private const int DraggedLayer = 1100;

    // Closer than this, the landing place is just the toolbar's own shadow, so there's no point showing it.
    private const double SameSpotTolerance = 0.5;

    private sealed class ToolbarDragDisplay(string toolbarId, LayoutPoint grab, LayoutInsets safeArea)
    {
        public string ToolbarId { get; } = toolbarId;

        public LayoutPoint Grab { get; } = grab;

        public LayoutInsets SafeAreaAtStart { get; } = safeArea;

        public Dictionary<ToolbarAnchor, (DropZoneView View, LayoutRect Bounds)> Zones { get; } = [];

        public DropZoneView Landing { get; } = new() { IsActive = true, ZIndex = DropZoneLayer };

        public Point Pointer { get; set; }
    }

    private readonly Dictionary<string, WindowFrame> _windows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ToolbarFrame> _toolbars = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LayoutSize> _toolbarSizes = new(StringComparer.Ordinal);
    private readonly LayoutMotions _motions = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    private DockingLayout? _layout;
    private ShellCatalog? _catalog;
    private ShellLayoutMetrics? _metrics;
    private SolvedLayout? _solved;
    private LayoutSize _available;
    private bool _animateNextArrange;
    private bool _isFrameRequested;
    private ToolbarDragDisplay? _drag;

    /// <summary>Raised when a toolbar's view is made, which can happen again if it's hidden and shown.</summary>
    public event EventHandler<ToolbarFrameCreatedEventArgs>? ToolbarFrameCreated;

    /// <summary>
    /// How much of each edge windows and toolbars take from the canvas. It holds still during a drag, so the art
    /// doesn't slide around under the preview (docs/DOCKING.md §8).
    /// </summary>
    public LayoutInsets SafeArea => _drag?.SafeAreaAtStart ?? _solved?.SafeArea ?? default;

    /// <summary>Shows a layout. A window or toolbar that was already showing keeps its view.</summary>
    /// <param name="layout">What to show.</param>
    /// <param name="catalog">The windows and toolbars there are.</param>
    /// <param name="metrics">The theme's sizes.</param>
    /// <param name="animate">Whether toolbars slide to their new places, or snap there.</param>
    public void Show(DockingLayout layout, ShellCatalog catalog, ShellLayoutMetrics metrics, bool animate = false)
    {
        _layout = layout;
        _catalog = catalog;
        _metrics = metrics;
        _animateNextArrange |= animate;

        var windowIds = layout.OpenWindowIds().Where(catalog.Windows.ContainsKey).ToHashSet(StringComparer.Ordinal);
        RemoveAllExcept(_windows, windowIds);

        foreach (var windowId in windowIds.Where(id => !_windows.ContainsKey(id)))
        {
            var entry = catalog.Windows[windowId];
            var frame = new WindowFrame { Title = entry.Title, Body = entry.CreateView(), ZIndex = WindowLayer };
            _windows.Add(windowId, frame);
            Children.Add(frame);
        }

        var showing = layout.Toolbars
            .Where(toolbar => toolbar.IsVisible && catalog.Toolbars.ContainsKey(toolbar.ToolbarId))
            .ToList();
        var toolbarIds = showing.Select(toolbar => toolbar.ToolbarId).ToHashSet(StringComparer.Ordinal);

        foreach (var toolbarId in _toolbars.Keys.Where(id => !toolbarIds.Contains(id)))
            _motions.Forget(toolbarId);

        RemoveAllExcept(_toolbars, toolbarIds);

        for (var index = 0; index < showing.Count; index++)
        {
            var frame = ToolbarFrameFor(showing[index].ToolbarId);
            frame.Orientation = showing[index].Orientation;
            frame.ZIndex = LayerOf(showing[index], index);
        }

        InvalidateMeasure();
    }

    /// <summary>The sizes things have right now, as they would go to the solver.</summary>
    public LayoutMeasurements CurrentMeasurements() =>
        new(_available, _catalog!.MinimumWindowHeights,
            new Dictionary<string, LayoutSize>(_toolbarSizes, StringComparer.Ordinal));

    /// <summary>
    /// Like <see cref="CurrentMeasurements"/>, with a toolbar that isn't showing yet measured too. Its view is
    /// made if it needs to be, and is kept for when it does show.
    /// </summary>
    public LayoutMeasurements MeasurementsIncluding(ToolbarPlacement toolbar)
    {
        var frame = ToolbarFrameFor(toolbar.ToolbarId);
        frame.Orientation = toolbar.Orientation;
        frame.Measure(Size.Infinity);

        var measurements = CurrentMeasurements();
        var sizes = new Dictionary<string, LayoutSize>(measurements.ToolbarSizes, StringComparer.Ordinal)
        {
            [toolbar.ToolbarId] = new LayoutSize(frame.DesiredSize.Width, frame.DesiredSize.Height),
        };

        return measurements with { ToolbarSizes = sizes };
    }

    /// <summary>Shows the drop zones, and lets the toolbar follow the pointer.</summary>
    /// <param name="toolbarId">The toolbar being dragged.</param>
    /// <param name="grab">Where on the toolbar the pointer holds it, from 0 to 1 along each axis.</param>
    /// <param name="zones">The drop zones.</param>
    public void BeginToolbarDrag(string toolbarId, LayoutPoint grab, IReadOnlyList<ToolbarDropZone> zones)
    {
        var drag = new ToolbarDragDisplay(toolbarId, grab, SafeArea);
        foreach (var zone in zones)
        {
            var view = new DropZoneView { ZIndex = DropZoneLayer };
            drag.Zones[zone.Anchor] = (view, zone.Bounds);
            Children.Add(view);
        }

        Children.Add(drag.Landing);
        _toolbars[toolbarId].ZIndex = DraggedLayer;
        _toolbars[toolbarId].IsDragged = true;
        _drag = drag;
    }

    /// <summary>Moves the dragged toolbar, and shows what a drop would do.</summary>
    /// <param name="pointer">Where the pointer is.</param>
    /// <param name="zone">The anchor whose zone it's over, if any.</param>
    /// <param name="preview">The layout a drop would give.</param>
    public void MoveToolbarDrag(Point pointer, ToolbarAnchor? zone, DockingLayout preview)
    {
        if (_drag is not { } drag)
            return;

        drag.Pointer = pointer;
        foreach (var (anchor, entry) in drag.Zones)
            entry.View.IsActive = anchor == zone;

        Show(preview, _catalog!, _metrics!, animate: true);
    }

    /// <summary>Takes away the drop zones. The next <see cref="Show"/> says where everything goes.</summary>
    public void EndToolbarDrag()
    {
        if (_drag is not { } drag)
            return;

        foreach (var (view, _) in drag.Zones.Values)
            Children.Remove(view);

        Children.Remove(drag.Landing);
        _toolbars[drag.ToolbarId].IsDragged = false;
        _drag = null;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var available = new LayoutSize(FiniteOrZero(availableSize.Width), FiniteOrZero(availableSize.Height));
        if (_layout is not { } layout || _catalog is not { } catalog || _metrics is not { } metrics)
            return new Size(available.Width, available.Height);

        // Unlimited first, to learn the size each toolbar wants. The solver then decides what it gets.
        _available = available;
        _toolbarSizes.Clear();
        foreach (var (toolbarId, view) in _toolbars)
        {
            view.Measure(Size.Infinity);
            _toolbarSizes[toolbarId] = new LayoutSize(view.DesiredSize.Width, view.DesiredSize.Height);
        }

        var measurements = new LayoutMeasurements(available, catalog.MinimumWindowHeights, _toolbarSizes);
        _solved = LayoutSolver.Solve(layout, measurements, metrics.Docking);

        foreach (var group in _solved.WindowGroups)
        {
            if (!_windows.TryGetValue(group.WindowIds[0], out var frame))
                continue;

            frame.IsTitleOnly = group.IsTitleOnly;
            frame.Measure(new Size(group.Bounds.Width, group.Bounds.Height));
        }

        // A dragged toolbar keeps the size it wants, whatever it would get if it landed.
        foreach (var toolbar in _solved.Toolbars.Where(toolbar => toolbar.ToolbarId != _drag?.ToolbarId))
            _toolbars[toolbar.ToolbarId].Measure(new Size(toolbar.Bounds.Width, toolbar.Bounds.Height));

        if (_drag is { } drag)
        {
            drag.Landing.Measure(availableSize);
            foreach (var (view, _) in drag.Zones.Values)
                view.Measure(availableSize);
        }

        return new Size(available.Width, available.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_solved is not { } solved || _metrics is not { } metrics)
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

        var bounds = solved.Toolbars.ToDictionary(toolbar => toolbar.ToolbarId, toolbar => toolbar.Bounds,
            StringComparer.Ordinal);
        foreach (var (toolbarId, view) in _toolbars)
        {
            if (toolbarId == _drag?.ToolbarId)
            {
                view.Arrange(ToRect(PlaceDragged(_drag, finalSize, bounds.GetValueOrDefault(toolbarId))));
            }
            else if (bounds.TryGetValue(toolbarId, out var target))
            {
                view.Arrange(ToRect(_motions.Advance(toolbarId, target, _clock.Elapsed, metrics.MotionDuration,
                    _animateNextArrange)));
            }
            else
            {
                // Left out for lack of room. It stays measured, so it's noticed when there's room again, but has
                // no size, so it can't be seen or clicked.
                _motions.Forget(toolbarId);
                view.Arrange(default);
            }
        }

        _animateNextArrange = false;
        if (_motions.IsMoving)
            RequestNextFrame();

        return finalSize;
    }

    private LayoutRect PlaceDragged(ToolbarDragDisplay drag, Size finalSize, LayoutRect landing)
    {
        var size = _toolbarSizes.GetValueOrDefault(drag.ToolbarId);
        var x = drag.Pointer.X - drag.Grab.X * size.Width;
        var y = drag.Pointer.Y - drag.Grab.Y * size.Height;
        var rect = new LayoutRect(
            Math.Max(0, Math.Min(x, finalSize.Width - size.Width)),
            Math.Max(0, Math.Min(y, finalSize.Height - size.Height)),
            size.Width,
            size.Height);

        _motions.Place(drag.ToolbarId, rect);

        var isUnderneath = Math.Abs(landing.X - rect.X) < SameSpotTolerance &&
                           Math.Abs(landing.Y - rect.Y) < SameSpotTolerance;
        drag.Landing.Arrange(isUnderneath ? default : ToRect(landing));

        foreach (var (view, zoneBounds) in drag.Zones.Values)
            view.Arrange(ToRect(zoneBounds));

        return rect;
    }

    private void RequestNextFrame()
    {
        if (_isFrameRequested || TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        _isFrameRequested = true;
        topLevel.RequestAnimationFrame(_ =>
        {
            _isFrameRequested = false;
            InvalidateArrange();
        });
    }

    private ToolbarFrame ToolbarFrameFor(string toolbarId)
    {
        if (_toolbars.TryGetValue(toolbarId, out var existing))
            return existing;

        var entry = _catalog!.Toolbars[toolbarId];
        var frame = new ToolbarFrame
        {
            Bar = entry.CreateView(),
            WheelStep = _metrics!.ToolbarWheelStep,
            FadeLength = _metrics.ToolbarFadeLength,
        };

        if (entry.CreateMenu?.Invoke() is { } menu)
        {
            frame.ContextMenu = menu;

            // From here, because a toolbar's own Hide item takes the toolbar out of the window.
            menu.Closed += (_, _) => RaiseEvent(new RoutedEventArgs(PanelEvents.DoneTypingEvent));
        }

        _toolbars.Add(toolbarId, frame);
        Children.Add(frame);
        ToolbarFrameCreated?.Invoke(this, new ToolbarFrameCreatedEventArgs(toolbarId, frame));

        return frame;
    }

    private int LayerOf(ToolbarPlacement toolbar, int index)
    {
        if (toolbar.ToolbarId == _drag?.ToolbarId)
            return DraggedLayer;

        return toolbar.IsFloating ? FloatingToolbarLayer + index : ToolbarLayer;
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
