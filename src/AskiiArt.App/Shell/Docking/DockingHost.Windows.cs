// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AskiiArt.App.Shell.Docking;

/// <summary>A window's view has just been made.</summary>
internal sealed class WindowFrameCreatedEventArgs(string windowId, WindowFrame frame) : EventArgs
{
    public string WindowId { get; } = windowId;

    public WindowFrame Frame { get; } = frame;
}

/// <summary>A handle that resizes a side's windows has just been made.</summary>
internal sealed class StackResizeHandleCreatedEventArgs(StackResizeHandleId id, Control handle) : EventArgs
{
    public StackResizeHandleId Id { get; } = id;

    public Control Handle { get; } = handle;
}

internal sealed partial class DockingHost
{
    // What the placeholder goes by among the things that move. No window's or toolbar's id looks like this.
    private const string PlaceholderMotion = "window-placeholder";

    private sealed class WindowDragDisplay(string windowId, LayoutPoint grab, LayoutSize heldSize, LayoutInsets safeArea,
        WindowPlaceholderView placeholder)
    {
        public string WindowId { get; } = windowId;

        public LayoutPoint Grab { get; } = grab;

        public LayoutSize HeldSize { get; } = heldSize;

        public LayoutInsets SafeAreaAtStart { get; } = safeArea;

        public WindowPlaceholderView Placeholder { get; } = placeholder;

        public List<(DropZoneView View, WindowDropZone Zone)> Zones { get; } = [];

        public Point Pointer { get; set; }

        public WindowDragPreview? Preview { get; set; }
    }

    private readonly Dictionary<StackResizeHandleId, StackResizeHandleView> _stackResizeHandles = [];
    private WindowDragDisplay? _windowDrag;
    private LayoutInsets? _safeAreaDuringResize;

    /// <summary>Raised when a window's view is made, which can happen again if it's closed and reopened.</summary>
    public event EventHandler<WindowFrameCreatedEventArgs>? WindowFrameCreated;

    /// <summary>Raised when a handle for resizing a side's windows is made.</summary>
    public event EventHandler<StackResizeHandleCreatedEventArgs>? StackResizeHandleCreated;

    /// <summary>Lets the window follow the pointer, and shows the empty sides' strips.</summary>
    /// <param name="windowId">The window being dragged.</param>
    /// <param name="grab">Where on the window the pointer holds it, from 0 to 1 along each axis.</param>
    /// <param name="windowDrag">What the drag can do.</param>
    public void BeginWindowDrag(string windowId, LayoutPoint grab, WindowDrag windowDrag)
    {
        var placeholder = new WindowPlaceholderView(_metrics!) { ZIndex = DropZoneLayer, IsVisible = false };
        Children.Add(placeholder);

        var drag = new WindowDragDisplay(windowId, grab, windowDrag.HeldSize, SafeArea, placeholder);
        foreach (var zone in windowDrag.Zones.Where(zone => zone.Kind == WindowDropKind.EmptySide))
        {
            var view = new DropZoneView(_metrics!.MotionDuration, _metrics.DropZoneDimmedOpacity) { ZIndex = DropZoneLayer };
            drag.Zones.Add((view, zone));
            Children.Add(view);
        }

        _windowDrag = drag;
        _windows[windowId].ZIndex = DraggedLayer;
    }

    /// <summary>Moves the dragged window, and shows what a drop would do.</summary>
    /// <param name="pointer">Where the pointer is.</param>
    /// <param name="preview">What a drop would do.</param>
    public void MoveWindowDrag(Point pointer, WindowDragPreview preview)
    {
        if (_windowDrag is not { } drag)
            return;

        drag.Pointer = pointer;
        drag.Preview = preview;
        drag.Placeholder.IsVisible = preview.Placeholder is not null;
        if (preview.Placeholder is null)
            _motions.Forget(PlaceholderMotion);

        foreach (var (view, zone) in drag.Zones)
            view.IsOutlined = preview.Zone == zone;

        _windows[drag.WindowId].IsAnchoring = preview.Zone is not null;
        Show(preview.Layout, _catalog!, _metrics!, animate: true);
    }

    /// <summary>Takes away the placeholder and strips. The next <see cref="Show"/> says where everything goes.</summary>
    public void EndWindowDrag()
    {
        if (_windowDrag is not { } drag)
            return;

        foreach (var (view, _) in drag.Zones)
            FadeOutAndRemove(view);

        FadeOutAndRemove(drag.Placeholder);
        _motions.Forget(PlaceholderMotion);
        _windows[drag.WindowId].IsAnchoring = false;
        _windowDrag = null;
    }

    /// <summary>Holds the canvas still while the user resizes something, until <see cref="EndWindowResize"/>.</summary>
    public void BeginWindowResize() => _safeAreaDuringResize = SafeArea;

    /// <summary>Shows what a resize would do, with no sliding, so that nothing trails behind the pointer.</summary>
    public void ShowWindowResize(DockingLayout layout) => Show(layout, _catalog!, _metrics!);

    /// <summary>Lets the canvas move again.</summary>
    public void EndWindowResize() => _safeAreaDuringResize = null;

    private static string WindowMotion(string windowId) => "window:" + windowId;

    private void ShowWindows(DockingLayout layout, ShellCatalog catalog)
    {
        var windowIds = layout.OpenWindowIds().Where(catalog.Windows.ContainsKey).ToHashSet(StringComparer.Ordinal);

        foreach (var windowId in _windows.Keys.Where(id => !windowIds.Contains(id)))
            _motions.Forget(WindowMotion(windowId));

        RemoveAllExcept(_windows, windowIds);

        foreach (var windowId in windowIds.Where(id => !_windows.ContainsKey(id)))
            AddWindowFrame(windowId, catalog.Windows[windowId]);

        foreach (var (windowId, frame) in _windows)
        {
            frame.IsFloating = false;
            frame.ZIndex = windowId == _windowDrag?.WindowId ? DraggedLayer : WindowLayer;
        }

        for (var index = 0; index < layout.FloatingWindows.Count; index++)
        {
            if (!_windows.TryGetValue(layout.FloatingWindows[index].WindowId, out var frame))
                continue;

            frame.IsFloating = true;
            if (layout.FloatingWindows[index].WindowId != _windowDrag?.WindowId)
                frame.ZIndex = FloatingWindowLayer + index;
        }

        ShowStackResizeHandles(layout);
    }

    private void AddWindowFrame(string windowId, ShellWindowEntry entry)
    {
        var frame = new WindowFrame { Title = entry.Title, Body = entry.CreateView(), ZIndex = WindowLayer };
        frame.Transitions = [new DoubleTransition { Property = OpacityProperty, Duration = _metrics!.MotionDuration }];
        frame.SetCloseCommand(entry.CloseCommand, windowId);

        if (entry.CreateMenu?.Invoke() is { } menu)
        {
            frame.TitleMenu = menu;

            // From here, because a window's own Close item takes the window out of the app window.
            menu.Closed += (_, _) => RaiseEvent(new RoutedEventArgs(PanelEvents.DoneTypingEvent));
        }

        _windows.Add(windowId, frame);
        Children.Add(frame);
        WindowFrameCreated?.Invoke(this, new WindowFrameCreatedEventArgs(windowId, frame));
    }

    private void ShowStackResizeHandles(DockingLayout layout)
    {
        var ids = StackResizeHandles.IdsIn(layout).ToHashSet();
        RemoveAllExcept(_stackResizeHandles, ids);

        foreach (var id in ids.Where(id => !_stackResizeHandles.ContainsKey(id)))
        {
            var handle = new StackResizeHandleView(id) { ZIndex = StackResizeHandleLayer };
            _stackResizeHandles.Add(id, handle);
            Children.Add(handle);
            StackResizeHandleCreated?.Invoke(this, new StackResizeHandleCreatedEventArgs(id, handle));
        }
    }

    private void MeasureWindows(SolvedLayout solved, Size availableSize)
    {
        foreach (var group in solved.WindowGroups)
            MeasureWindow(group.WindowIds[0], group.Bounds, group.IsTitleOnly);

        foreach (var window in solved.FloatingWindows)
            MeasureWindow(window.WindowId, window.Bounds, window.IsTitleOnly);

        foreach (var handle in _stackResizeHandles.Values)
            handle.Measure(availableSize);

        if (_windowDrag is not { } drag)
            return;

        // A dragged window keeps its size and its contents, whatever it would get if it landed.
        _windows[drag.WindowId].Measure(new Size(drag.HeldSize.Width, drag.HeldSize.Height));
        drag.Placeholder.Measure(SizeOf(drag.Preview?.Placeholder));
        foreach (var (view, zone) in drag.Zones)
            view.Measure(SizeOf(zone.Bounds));
    }

    private void MeasureWindow(string windowId, LayoutRect bounds, bool isTitleOnly)
    {
        if (windowId == _windowDrag?.WindowId || !_windows.TryGetValue(windowId, out var frame))
            return;

        frame.IsTitleOnly = isTitleOnly;
        frame.Measure(new Size(bounds.Width, bounds.Height));
    }

    private void ArrangeWindows(SolvedLayout solved, ShellLayoutMetrics metrics, Size finalSize)
    {
        foreach (var group in solved.WindowGroups)
        {
            // Only the first of a group's tabs shows until docks arrive in step 2.5f.
            for (var tab = 0; tab < group.WindowIds.Count; tab++)
            {
                if (!_windows.TryGetValue(group.WindowIds[tab], out var frame))
                    continue;

                frame.IsVisible = tab == 0;
                ArrangeWindow(group.WindowIds[tab], frame, group.Bounds, metrics);
            }
        }

        foreach (var window in solved.FloatingWindows)
        {
            if (!_windows.TryGetValue(window.WindowId, out var frame))
                continue;

            frame.IsVisible = true;
            ArrangeWindow(window.WindowId, frame, window.Bounds, metrics);
        }

        foreach (var handle in StackResizeHandles.For(solved, metrics.Docking))
        {
            if (_stackResizeHandles.TryGetValue(handle.Id, out var view))
                view.Arrange(ToRect(handle.Bounds));
        }

        if (_windowDrag is { } drag)
            ArrangeWindowDrag(drag, metrics, finalSize);
    }

    private void ArrangeWindow(string windowId, WindowFrame frame, LayoutRect bounds, ShellLayoutMetrics metrics)
    {
        if (windowId == _windowDrag?.WindowId)
            return;

        var motion = WindowMotion(windowId);
        frame.Arrange(ToRect(_motions.Advance(motion, bounds, _clock.Elapsed, metrics.MotionDuration, Slides(motion))));
    }

    private void ArrangeWindowDrag(WindowDragDisplay drag, ShellLayoutMetrics metrics, Size finalSize)
    {
        var size = drag.HeldSize;
        var held = new LayoutRect(
            Math.Max(0, Math.Min(drag.Pointer.X - drag.Grab.X * size.Width, finalSize.Width - size.Width)),
            Math.Max(0, Math.Min(drag.Pointer.Y - drag.Grab.Y * size.Height, finalSize.Height - size.Height)),
            size.Width,
            size.Height);

        _motions.Place(WindowMotion(drag.WindowId), held);
        _windows[drag.WindowId].Arrange(ToRect(held));

        foreach (var (view, zone) in drag.Zones)
            view.Arrange(ToRect(zone.Bounds));

        if (drag.Preview?.Placeholder is { } placeholder)
        {
            drag.Placeholder.Arrange(ToRect(_motions.Advance(PlaceholderMotion, placeholder, _clock.Elapsed,
                metrics.MotionDuration, _animateNextArrange)));
        }
    }
}
