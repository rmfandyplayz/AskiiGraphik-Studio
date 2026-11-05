// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using Avalonia;
using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// Connects the pointer to an <see cref="IWindowResize"/>. All the rules are in there. The layout only changes when the
/// drag ends, so closing the app window during one saves the layout from before it.
/// </summary>
/// <param name="host">Where the windows are.</param>
/// <param name="currentLayout">The layout as it stands, if there's a window.</param>
/// <param name="finish">Takes the layout the resize ended with, which is the old one if it was cancelled.</param>
internal sealed class WindowResizeController(
    DockingHost host,
    Func<DockingLayout?> currentLayout,
    Action<DockingLayout> finish)
{
    // A resize follows the pointer from the very first move, with nothing else that a press there could mean.
    private const double NoThreshold = 0;

    private IWindowResize? _resize;
    private PointerDragTracker? _tracker;
    private Point _pressedAt;
    private LayoutPoint _moved;
    private DragCancelListener? _cancelListener;

    /// <summary>Lets a handle start resizes.</summary>
    /// <param name="handle">What's dragged.</param>
    /// <param name="createResize">Works out what the drag does, or says null if it can't do anything right now.</param>
    public void Watch(Control handle, Func<DockingLayout, LayoutMeasurements, IWindowResize?> createResize)
    {
        var tracker = new PointerDragTracker(handle, host, NoThreshold);
        tracker.Started += (_, e) => Begin(tracker, createResize, e);
        tracker.Moved += (_, e) => MoveTo(e.Position);
        tracker.Dropped += (_, _) => Finish(cancelled: false);
        tracker.Cancelled += (_, _) => Finish(cancelled: true);
    }

    /// <summary>Cancels the resize, if there is one.</summary>
    public void Cancel() => _tracker?.Cancel();

    private void Begin(PointerDragTracker tracker, Func<DockingLayout, LayoutMeasurements, IWindowResize?> createResize,
        PointerDragEventArgs e)
    {
        if (currentLayout() is not { } layout || createResize(layout, host.CurrentMeasurements()) is not { } resize)
            return;

        _resize = resize;
        _tracker = tracker;
        _pressedAt = e.PressedAt;

        host.BeginWindowResize();
        _cancelListener ??= new DragCancelListener(host, Cancel);
        _cancelListener.Start();
        MoveTo(e.Position);
    }

    private void MoveTo(Point position)
    {
        if (_resize is not { } resize)
            return;

        _moved = new LayoutPoint(position.X - _pressedAt.X, position.Y - _pressedAt.Y);
        host.ShowWindowResize(resize.LayoutAfter(_moved));
    }

    private void Finish(bool cancelled)
    {
        if (_resize is not { } resize)
            return;

        _cancelListener?.Stop();
        host.EndWindowResize();
        _resize = null;
        _tracker = null;

        finish(cancelled ? resize.Start : resize.LayoutAfter(_moved));
    }
}
