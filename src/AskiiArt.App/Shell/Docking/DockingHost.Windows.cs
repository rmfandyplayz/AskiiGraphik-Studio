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

    private sealed class WindowDragDisplay(string windowId, LayoutSize heldSize, LayoutInsets safeArea,
        WindowPlaceholderView placeholder, WindowLandingPreview landing)
    {
        public string WindowId { get; } = windowId;

        public LayoutSize HeldSize { get; } = heldSize;

        public LayoutInsets SafeAreaAtStart { get; } = safeArea;

        public WindowPlaceholderView Placeholder { get; } = placeholder;

        public WindowLandingPreview Landing { get; } = landing;

        public List<(DropZoneView View, WindowDropZone Zone)> Zones { get; } = [];

        public WindowDragPreview? Preview { get; set; }
    }

    private readonly Dictionary<StackResizeHandleId, StackResizeHandleView> _stackResizeHandles = [];
    private WindowDragDisplay? _windowDrag;
    private LayoutInsets? _safeAreaDuringResize;

    /// <summary>Raised when a window's view is made, which can happen again if it's closed and reopened.</summary>
    public event EventHandler<WindowFrameCreatedEventArgs>? WindowFrameCreated;

    /// <summary>Raised when a handle for resizing a side's windows is made.</summary>
    public event EventHandler<StackResizeHandleCreatedEventArgs>? StackResizeHandleCreated;

    /// <summary>
    /// Lets the window follow the pointer, and shows the empty sides' strips. No window shows its buttons until the drag
    /// ends, because windows moving under the pointer would make them flicker.
    /// </summary>
    /// <param name="windowId">The window being dragged.</param>
    /// <param name="windowDrag">What the drag can do.</param>
    public void BeginWindowDrag(string windowId, WindowDrag windowDrag)
    {
        var placeholder = new WindowPlaceholderView(_metrics!) { ZIndex = DropZoneLayer, IsVisible = false };
        var landing = new WindowLandingPreview(_metrics!) { ZIndex = LandingLayer, IsVisible = false };
        Children.Add(placeholder);
        Children.Add(landing);

        var drag = new WindowDragDisplay(windowId, windowDrag.HeldSize, SafeArea, placeholder, landing);
        foreach (var zone in windowDrag.Zones.Where(zone => zone.Kind == WindowDropKind.EmptySide))
        {
            var view = new DropZoneView(_metrics!.MotionDuration, _metrics.DropZoneDimmedOpacity) { ZIndex = DropZoneLayer };
            drag.Zones.Add((view, zone));
            Children.Add(view);
        }

        _windowDrag = drag;
        _windows[windowId].ZIndex = DraggedLayer;
        SetWindowButtonsHidden(true);
    }

    /// <summary>Moves the dragged window, and shows what a drop would do.</summary>
    /// <param name="preview">What a drop would do, and where the window is held.</param>
    public void MoveWindowDrag(WindowDragPreview preview)
    {
        if (_windowDrag is not { } drag)
            return;

        drag.Preview = preview;
        drag.Landing.IsVisible = preview.Landing is not null;
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
        FadeOutAndRemove(drag.Landing);
        _motions.Forget(PlaceholderMotion);
        _windows[drag.WindowId].IsAnchoring = false;
        _windowDrag = null;
        SetWindowButtonsHidden(false);
    }

    /// <summary>
    /// Holds the canvas still while the user resizes something, until <see cref="EndWindowResize"/>. No window shows its
    /// buttons meanwhile, as during a drag.
    /// </summary>
    public void BeginWindowResize()
    {
        _safeAreaDuringResize = SafeArea;
        SetWindowButtonsHidden(true);
    }

    /// <summary>Shows what a resize would do, with no sliding, so that nothing trails behind the pointer.</summary>
    public void ShowWindowResize(DockingLayout layout) => Show(layout, _catalog!, _metrics!);

    /// <summary>Lets the canvas move again.</summary>
    public void EndWindowResize()
    {
        _safeAreaDuringResize = null;
        SetWindowButtonsHidden(false);
    }

    private static string WindowMotion(string windowId) => "window:" + windowId;

    private void SetWindowButtonsHidden(bool areHidden)
    {
        foreach (var frame in _windows.Values)
            frame.AreButtonsHidden = areHidden;
    }

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
        drag.Landing.Measure(SizeOf(drag.Preview?.Landing));
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

    private void ArrangeWindows(SolvedLayout solved, ShellLayoutMetrics metrics)
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

        if (_windowDrag is { Preview: not null } drag)
            ArrangeWindowDrag(drag, drag.Preview, metrics);
    }

    private void ArrangeWindow(string windowId, WindowFrame frame, LayoutRect bounds, ShellLayoutMetrics metrics)
    {
        if (windowId == _windowDrag?.WindowId)
            return;

        var motion = WindowMotion(windowId);
        frame.Arrange(ToRect(_motions.Advance(motion, bounds, _clock.Elapsed, metrics.MotionDuration, Slides(motion))));
    }

    // The window follows the pointer even past the app window's edge, as a toolbar does, and the landing preview shows
    // where a drop would float it back to.
    private void ArrangeWindowDrag(WindowDragDisplay drag, WindowDragPreview preview, ShellLayoutMetrics metrics)
    {
        _motions.Place(WindowMotion(drag.WindowId), preview.Held);
        _windows[drag.WindowId].Arrange(ToRect(preview.Held));

        foreach (var (view, zone) in drag.Zones)
            view.Arrange(ToRect(zone.Bounds));

        drag.Landing.Arrange(ToRect(preview.Landing ?? default));

        if (preview.Placeholder is { } placeholder)
        {
            drag.Placeholder.Arrange(ToRect(_motions.Advance(PlaceholderMotion, placeholder, _clock.Elapsed,
                metrics.MotionDuration, _animateNextArrange)));
        }
    }
}
