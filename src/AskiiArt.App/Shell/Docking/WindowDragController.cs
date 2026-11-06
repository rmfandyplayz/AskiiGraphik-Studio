// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using Avalonia;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// Connects the pointer to a <see cref="WindowDrag"/>. All the rules are in there: this only shows what it says. The
/// layout only changes when the drag ends, so closing the app window during a drag saves the layout from before it.
/// </summary>
/// <param name="host">Where the windows are.</param>
/// <param name="metrics">The theme's sizes.</param>
/// <param name="currentLayout">The layout as it stands, if there's a window.</param>
/// <param name="finish">Takes the layout the drag ended with, which is the old one if it was cancelled.</param>
internal sealed class WindowDragController(
    DockingHost host,
    ShellLayoutMetrics metrics,
    Func<DockingLayout?> currentLayout,
    Action<DockingLayout> finish)
{
    private WindowDrag? _drag;
    private PointerDragTracker? _tracker;
    private LayoutPoint _position;
    private DragCancelListener? _cancelListener;

    /// <summary>Lets the window's title row start drags.</summary>
    public void Watch(string windowId, WindowFrame frame)
    {
        var tracker = new PointerDragTracker(frame.TitleHandle, host, metrics.DragThreshold);
        tracker.Started += (_, e) => Begin(windowId, frame, tracker, e);
        tracker.Moved += (_, e) => MoveTo(e.Position);
        tracker.Dropped += (_, _) => Drop();
        tracker.Cancelled += (_, _) => GoBack();
    }

    /// <summary>Cancels the drag, if there is one.</summary>
    public void Cancel() => _tracker?.Cancel();

    private void Begin(string windowId, WindowFrame frame, PointerDragTracker tracker, PointerDragEventArgs e)
    {
        if (currentLayout() is not { } layout)
            return;

        var bounds = frame.Bounds;
        var grab = new LayoutPoint(
            Fraction(e.PressedAt.X - bounds.X, bounds.Width),
            Fraction(e.PressedAt.Y - bounds.Y, bounds.Height));

        _drag = new WindowDrag(layout, windowId, grab, host.CurrentMeasurements(), metrics.Docking);
        _tracker = tracker;

        host.BeginWindowDrag(windowId, _drag);
        _cancelListener ??= new DragCancelListener(host, Cancel);
        _cancelListener.Start();
        MoveTo(e.Position);
    }

    private void MoveTo(Point position)
    {
        if (_drag is not { } drag)
            return;

        _position = new LayoutPoint(position.X, position.Y);
        host.MoveWindowDrag(drag.PreviewAt(_position));
    }

    private void Drop()
    {
        if (_drag is not { } drag)
            return;

        End();
        finish(drag.LayoutAt(_position));
    }

    private void GoBack()
    {
        if (_drag is not { } drag)
            return;

        End();
        finish(drag.Start);
    }

    private void End()
    {
        _cancelListener?.Stop();
        host.EndWindowDrag();
        _drag = null;
        _tracker = null;
    }

    private static double Fraction(double part, double whole) =>
        whole > 0 ? Math.Clamp(part / whole, 0, 1) : 0;
}
