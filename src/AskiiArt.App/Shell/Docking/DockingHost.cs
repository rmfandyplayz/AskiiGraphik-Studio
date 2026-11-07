// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics;
using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using Avalonia;
using Avalonia.Animation;
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
internal sealed partial class DockingHost : Panel
{
    // From back to front (docs/DOCKING.md §2). Floating toolbars and windows add their place in the layout's list to
    // their layer. A floating toolbar's sub-toolbar shares its layer.
    private const int WindowLayer = 0;
    private const int StackResizeHandleLayer = 1;
    private const int ToolbarLayer = 100;
    private const int SubToolbarLayer = 150;
    private const int FloatingToolbarLayer = 200;
    private const int FloatingWindowLayer = 500;
    private const int DropZoneLayer = 1000;
    private const int LandingLayer = 1050;
    private const int DraggedLayer = 1100;

    // What the sub-toolbar goes by among the things that move. No toolbar's id looks like this.
    private const string SubToolbarMotion = "sub-toolbar";

    private sealed class ToolbarDragDisplay(string toolbarId, LayoutPoint grab, LayoutInsets safeArea,
        ToolbarLandingPreview landing, ToolbarLandingPreview? subToolbarLanding)
    {
        public string ToolbarId { get; } = toolbarId;

        public LayoutPoint Grab { get; } = grab;

        public LayoutInsets SafeAreaAtStart { get; } = safeArea;

        public Dictionary<ToolbarAnchor, (DropZoneView View, LayoutRect Bounds)> Zones { get; } = [];

        public ToolbarLandingPreview Landing { get; } = landing;

        /// <summary>Where the dragged toolbar's sub-toolbar would land, if it has one.</summary>
        public ToolbarLandingPreview? SubToolbarLanding { get; } = subToolbarLanding;

        public Point Pointer { get; set; }

        public LayoutRect HeldBounds { get; set; }

        public ToolbarDragPreview? Preview { get; set; }
    }

    private sealed record SubToolbarDisplay(string ParentToolbarId, ToolbarFrame Frame, Func<Control> CreateView,
        ToolbarShapes Shapes)
    {
        public SubToolbarRequest Request => new(ParentToolbarId, Shapes);
    }

    private readonly Dictionary<string, WindowFrame> _windows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ToolbarFrame> _toolbars = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LayoutSize> _toolbarSizes = new(StringComparer.Ordinal);
    private readonly LayoutMotions _motions = new();
    private readonly List<(Control View, TimeSpan GoneAt)> _fadingOut = [];
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    private DockingLayout? _layout;
    private ShellCatalog? _catalog;
    private ShellLayoutMetrics? _metrics;
    private SolvedLayout? _solved;
    private LayoutSize _available;
    private bool _animateNextArrange;
    private string? _justDroppedToolbarId;
    private bool _isFrameRequested;
    private ToolbarDragDisplay? _drag;
    private SubToolbarDisplay? _subToolbar;

    /// <summary>Raised when a toolbar's view is made, which can happen again if it's hidden and shown.</summary>
    public event EventHandler<ToolbarFrameCreatedEventArgs>? ToolbarFrameCreated;

    /// <summary>
    /// How much of each edge windows and toolbars take from the canvas. It holds still during a drag, so the art
    /// doesn't slide around under the preview (docs/DOCKING.md §8).
    /// </summary>
    public LayoutInsets SafeArea =>
        _drag?.SafeAreaAtStart ?? _windowDrag?.SafeAreaAtStart ?? _safeAreaDuringResize ?? _solved?.SafeArea ?? default;

    /// <summary>Where everything is right now, if the host has been laid out.</summary>
    public SolvedLayout? Solved => _solved;

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

        ShowWindows(layout, catalog);

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

            if (_subToolbar is { } subToolbar && subToolbar.ParentToolbarId == showing[index].ToolbarId)
            {
                subToolbar.Frame.Orientation = showing[index].Orientation;
                subToolbar.Frame.ZIndex = frame.ZIndex == ToolbarLayer ? SubToolbarLayer : frame.ZIndex;
            }
        }

        if (_drag?.SubToolbarLanding is { } landing)
            landing.Toolbar.Orientation = OrientationIn(layout, _drag.ToolbarId);

        InvalidateMeasure();
    }

    /// <summary>Shows a sub-toolbar, in place of any other, which pushes other toolbars out of its way.</summary>
    /// <param name="parentToolbarId">The toolbar it belongs to.</param>
    /// <param name="createView">Makes its buttons. It needs more than one view of them, as a toolbar does in a drag.</param>
    public void ShowSubToolbar(string parentToolbarId, Func<Control> createView)
    {
        RemoveSubToolbar();

        // A copy tells it the size it wants each way round, so that the one showing never has to turn to find out.
        var measured = NewToolbarFrame(createView(), showsGrips: false);
        Children.Add(measured);
        var shapes = measured.MeasureBothWays();
        Children.Remove(measured);

        var frame = NewToolbarFrame(createView(), showsGrips: false);
        FadeOpacity(frame);
        Children.Add(frame);

        _subToolbar = new SubToolbarDisplay(parentToolbarId, frame, createView, shapes);
        Show(_layout!, _catalog!, _metrics!, animate: true);
    }

    /// <summary>Takes away the sub-toolbar, and whatever it pushed moves back.</summary>
    public void HideSubToolbar()
    {
        if (_subToolbar is null)
            return;

        RemoveSubToolbar();
        Show(_layout!, _catalog!, _metrics!, animate: true);
    }

    /// <summary>The sizes things have right now, as they would go to the solver.</summary>
    public LayoutMeasurements CurrentMeasurements() =>
        new(_available, _catalog!.MinimumWindowHeights,
            new Dictionary<string, LayoutSize>(_toolbarSizes, StringComparer.Ordinal), _subToolbar?.Request);

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

    /// <summary>Lets the toolbar follow the pointer, and readies the preview of where it would land.</summary>
    /// <param name="toolbarId">The toolbar being dragged.</param>
    /// <param name="grab">Where on the toolbar the pointer holds it, from 0 to 1 along each axis.</param>
    /// <returns>The size it wants each way round, since it could turn during the drag.</returns>
    public ToolbarShapes BeginToolbarDrag(string toolbarId, LayoutPoint grab)
    {
        var landing = new ToolbarLandingPreview(NewToolbarFrame(_catalog!.Toolbars[toolbarId].CreateView()), _metrics!)
        {
            ZIndex = LandingLayer,
        };

        Children.Add(landing);
        var subToolbarLanding = NewSubToolbarLanding(toolbarId);
        _drag = new ToolbarDragDisplay(toolbarId, grab, SafeArea, landing, subToolbarLanding);
        _toolbars[toolbarId].ZIndex = DraggedLayer;
        _toolbars[toolbarId].IsDragged = true;
        SetOthersHidden(toolbarId, isHidden: true);

        return landing.MeasureBothWays();
    }

    /// <summary>Shows the drag's drop zones.</summary>
    public void ShowDropZones(ToolbarDrag toolbarDrag)
    {
        if (_drag is not { } drag)
            return;

        foreach (var zone in toolbarDrag.Zones)
        {
            var view = new DropZoneView(_metrics!.MotionDuration, _metrics.DropZoneDimmedOpacity) { ZIndex = DropZoneLayer };
            drag.Zones[zone.Anchor] = (view, zone.Bounds);
            Children.Add(view);
        }
    }

    /// <summary>Moves the dragged toolbar, and shows what a drop would do.</summary>
    /// <param name="pointer">Where the pointer is.</param>
    /// <param name="preview">What a drop would do.</param>
    public void MoveToolbarDrag(Point pointer, ToolbarDragPreview preview)
    {
        if (_drag is not { } drag)
            return;

        drag.Pointer = pointer;
        drag.Preview = preview;
        foreach (var (anchor, entry) in drag.Zones)
        {
            entry.View.IsOutlined = preview.Zone?.Anchor == anchor;
            entry.View.IsDimmed = preview.DimmedZones.Contains(anchor);
        }

        drag.Landing.IsVisible = OutlineBounds(preview) is not null;
        drag.Landing.ShowsCopy = preview.PushedBackTo is not null;
        drag.Landing.Toolbar.Orientation = OrientationIn(preview.Layout, drag.ToolbarId);

        var isAnchoring = preview.Zone is not null;
        _toolbars[drag.ToolbarId].IsAnchoring = isAnchoring;
        if (drag.SubToolbarLanding is { } subToolbarLanding)
        {
            _subToolbar!.Frame.IsAnchoring = isAnchoring;
            subToolbarLanding.IsVisible = preview.PushedBackTo is not null && preview.SubToolbar is not null;
        }

        Show(preview.Layout, _catalog!, _metrics!, animate: true);
    }

    /// <summary>
    /// Fades the drop zones away and the other toolbars back. The next <see cref="Show"/> says where everything goes,
    /// and only the toolbar that was held slides there.
    /// </summary>
    public void EndToolbarDrag()
    {
        if (_drag is not { } drag)
            return;

        foreach (var (view, _) in drag.Zones.Values)
            FadeOutAndRemove(view);

        FadeOutAndRemove(drag.Landing);
        if (drag.SubToolbarLanding is { } subToolbarLanding)
        {
            FadeOutAndRemove(subToolbarLanding);
            if (_subToolbar is { } subToolbar)
                subToolbar.Frame.IsAnchoring = false;
        }

        _toolbars[drag.ToolbarId].IsDragged = false;
        _toolbars[drag.ToolbarId].IsAnchoring = false;
        SetOthersHidden(drag.ToolbarId, isHidden: false);
        _justDroppedToolbarId = drag.ToolbarId;
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

        var measurements = new LayoutMeasurements(available, catalog.MinimumWindowHeights, _toolbarSizes,
            _subToolbar?.Request);
        _solved = LayoutSolver.Solve(layout, measurements, metrics.Docking);

        MeasureWindows(_solved, availableSize);

        // A dragged toolbar keeps the size it wants, whatever it would get if it landed.
        foreach (var toolbar in _solved.Toolbars.Where(toolbar => toolbar.ToolbarId != _drag?.ToolbarId))
            _toolbars[toolbar.ToolbarId].Measure(new Size(toolbar.Bounds.Width, toolbar.Bounds.Height));

        var subToolbarSize = SizeOf(_subToolbar is { } subToolbar ? WhereItGoes(subToolbar)?.Bounds : null);
        _subToolbar?.Frame.Measure(subToolbarSize);

        if (_drag is { } drag)
        {
            drag.Landing.Measure(SizeOf(OutlineBounds(drag.Preview)));
            drag.SubToolbarLanding?.Measure(subToolbarSize);
            foreach (var (view, _) in drag.Zones.Values)
                view.Measure(availableSize);
        }

        return new Size(available.Width, available.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_solved is not { } solved || _metrics is not { } metrics)
            return finalSize;

        ArrangeWindows(solved, metrics);

        var bounds = solved.Toolbars.ToDictionary(toolbar => toolbar.ToolbarId, toolbar => toolbar.Bounds,
            StringComparer.Ordinal);
        foreach (var (toolbarId, view) in _toolbars)
        {
            if (toolbarId == _drag?.ToolbarId)
            {
                view.Arrange(ToRect(PlaceDragged(_drag)));
            }
            else if (bounds.TryGetValue(toolbarId, out var target))
            {
                view.Arrange(ToRect(_motions.Advance(toolbarId, target, _clock.Elapsed, metrics.MotionDuration,
                    Slides(toolbarId))));
            }
            else
            {
                // Left out for lack of room. It stays measured, so it's noticed when there's room again, but has
                // no size, so it can't be seen or clicked.
                _motions.Forget(toolbarId);
                view.Arrange(default);
            }
        }

        if (_subToolbar is { } subToolbar)
            ArrangeSubToolbar(subToolbar, metrics);

        _animateNextArrange = false;
        _justDroppedToolbarId = null;
        if (_motions.IsMoving || _fadingOut.Count > 0)
            RequestNextFrame();

        return finalSize;
    }

    // It follows the pointer anywhere, even past the app window's edge, as a window does. Where it would land instead
    // shows as the landing preview.
    private LayoutRect PlaceDragged(ToolbarDragDisplay drag)
    {
        var size = _toolbarSizes.GetValueOrDefault(drag.ToolbarId);
        var rect = new LayoutRect(
            drag.Pointer.X - drag.Grab.X * size.Width,
            drag.Pointer.Y - drag.Grab.Y * size.Height,
            size.Width,
            size.Height);

        _motions.Place(drag.ToolbarId, rect);
        drag.HeldBounds = rect;

        drag.Landing.Arrange(ToRect(OutlineBounds(drag.Preview) ?? default));
        foreach (var (view, zoneBounds) in drag.Zones.Values)
            view.Arrange(ToRect(zoneBounds));

        return rect;
    }

    private void ArrangeSubToolbar(SubToolbarDisplay subToolbar, ShellLayoutMetrics metrics)
    {
        if (WhereItGoes(subToolbar) is not { } target)
        {
            // No room for it. It has no size, so it can't be seen or clicked.
            _motions.Forget(SubToolbarMotion);
            subToolbar.Frame.Arrange(default);
            _drag?.SubToolbarLanding?.Arrange(default);
            return;
        }

        if (_drag is { SubToolbarLanding: { } landing } drag)
        {
            // It goes with the toolbar, on the side it would land on.
            var size = new LayoutSize(target.Bounds.Width, target.Bounds.Height);
            var held = SubToolbarGeometry.Beside(drag.HeldBounds, size, target.Orientation, target.Side,
                metrics.Docking.ToolbarGap);

            _motions.Place(SubToolbarMotion, held);
            subToolbar.Frame.Arrange(ToRect(held));
            landing.Arrange(ToRect(target.Bounds));
            return;
        }

        subToolbar.Frame.Arrange(ToRect(_motions.Advance(SubToolbarMotion, target.Bounds, _clock.Elapsed,
            metrics.MotionDuration, Slides(subToolbar.ParentToolbarId))));
    }

    // Toolbars out of sight for a drag just go where they belong, and fade back there when it ends. Only the one that
    // was held slides to where it landed.
    private bool Slides(string toolbarId) =>
        _animateNextArrange && _drag is null && (_justDroppedToolbarId is null || _justDroppedToolbarId == toolbarId);

    // A dragged toolbar's goes where the drop would put it. Any other's goes among what's showing.
    private SolvedSubToolbar? WhereItGoes(SubToolbarDisplay subToolbar) =>
        _drag?.ToolbarId == subToolbar.ParentToolbarId ? _drag.Preview?.SubToolbar : _solved?.SubToolbar;

    private void SetOthersHidden(string heldToolbarId, bool isHidden)
    {
        foreach (var (toolbarId, frame) in _toolbars)
        {
            if (toolbarId != heldToolbarId)
                frame.IsHiddenForDrag = isHidden;
        }

        if (_subToolbar is { } subToolbar && subToolbar.ParentToolbarId != heldToolbarId)
            subToolbar.Frame.IsHiddenForDrag = isHidden;
    }

    private void FadeOutAndRemove(Control view)
    {
        view.Opacity = 0;

        var duration = _metrics!.MotionDuration;
        if (duration <= TimeSpan.Zero)
        {
            Children.Remove(view);
            return;
        }

        _fadingOut.Add((view, _clock.Elapsed + duration));
        RequestNextFrame();
    }

    private void RemoveWhatHasFadedOut()
    {
        foreach (var (view, _) in _fadingOut.Where(entry => entry.GoneAt <= _clock.Elapsed))
            Children.Remove(view);

        _fadingOut.RemoveAll(entry => !Children.Contains(entry.View));
    }

    private static LayoutRect? OutlineBounds(ToolbarDragPreview? preview) =>
        preview?.Zone?.Bounds ?? preview?.PushedBackTo;

    private static Size SizeOf(LayoutRect? bounds) =>
        bounds is { } rect ? new Size(rect.Width, rect.Height) : default;

    private ToolbarLandingPreview? NewSubToolbarLanding(string toolbarId)
    {
        if (_subToolbar is not { } subToolbar || subToolbar.ParentToolbarId != toolbarId)
            return null;

        var landing = new ToolbarLandingPreview(NewToolbarFrame(subToolbar.CreateView(), showsGrips: false), _metrics!)
        {
            ZIndex = LandingLayer,
            IsVisible = false,
        };

        landing.Toolbar.Orientation = subToolbar.Frame.Orientation;
        Children.Add(landing);
        return landing;
    }

    private void RemoveSubToolbar()
    {
        if (_subToolbar is not { } subToolbar)
            return;

        Children.Remove(subToolbar.Frame);
        _motions.Forget(SubToolbarMotion);
        _subToolbar = null;
    }

    private void RequestNextFrame()
    {
        if (_isFrameRequested || TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        _isFrameRequested = true;
        topLevel.RequestAnimationFrame(_ =>
        {
            _isFrameRequested = false;
            RemoveWhatHasFadedOut();
            InvalidateArrange();
        });
    }

    private ToolbarFrame ToolbarFrameFor(string toolbarId)
    {
        if (_toolbars.TryGetValue(toolbarId, out var existing))
            return existing;

        var entry = _catalog!.Toolbars[toolbarId];
        var frame = NewToolbarFrame(entry.CreateView());
        FadeOpacity(frame);

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

    private ToolbarFrame NewToolbarFrame(Control bar, bool showsGrips = true) => new()
    {
        Bar = bar,
        WheelStep = _metrics!.ToolbarWheelStep,
        FadeLength = _metrics.ToolbarFadeLength,
        ShowsGrips = showsGrips,
    };

    private void FadeOpacity(ToolbarFrame frame) =>
        frame.Transitions = [new DoubleTransition { Property = OpacityProperty, Duration = _metrics!.MotionDuration }];

    private static ToolbarOrientation OrientationIn(DockingLayout layout, string toolbarId) =>
        layout.Toolbars.FirstOrDefault(toolbar => toolbar.ToolbarId == toolbarId)?.Orientation
        ?? ToolbarOrientation.Horizontal;

    private int LayerOf(ToolbarPlacement toolbar, int index)
    {
        if (toolbar.ToolbarId == _drag?.ToolbarId)
            return DraggedLayer;

        return toolbar.IsFloating ? FloatingToolbarLayer + index : ToolbarLayer;
    }

    private void RemoveAllExcept<TKey, TView>(Dictionary<TKey, TView> views, HashSet<TKey> keep)
        where TKey : notnull
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
