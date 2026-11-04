// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AskiiArt.App.Shell.Docking;

/// <summary>Where a drag has got to, in the coordinates the tracker was given.</summary>
internal sealed class PointerDragEventArgs(Point pressedAt, Point position) : EventArgs
{
    /// <summary>Where the button went down.</summary>
    public Point PressedAt { get; } = pressedAt;

    /// <summary>Where the pointer is now.</summary>
    public Point Position { get; } = position;
}

/// <summary>
/// Turns the left button on a handle into a drag: it starts once the pointer has moved far enough, so a plain
/// click isn't a drag, and it ends with a drop or a cancel.
/// </summary>
internal sealed class PointerDragTracker
{
    private readonly Visual _coordinates;
    private readonly double _threshold;

    private IPointer? _pointer;
    private Point _pressedAt;
    private bool _isDragging;

    public PointerDragTracker(Control handle, Visual coordinates, double threshold)
    {
        _coordinates = coordinates;
        _threshold = threshold;

        handle.PointerPressed += OnPressed;
        handle.PointerMoved += OnMoved;
        handle.PointerReleased += OnReleased;
        handle.PointerCaptureLost += OnCaptureLost;
    }

    /// <summary>The pointer has moved far enough to count as a drag.</summary>
    public event EventHandler<PointerDragEventArgs>? Started;

    /// <summary>The pointer moved during the drag.</summary>
    public event EventHandler<PointerDragEventArgs>? Moved;

    /// <summary>The button came up during the drag.</summary>
    public event EventHandler? Dropped;

    /// <summary>The drag ended some other way: it was cancelled, or the pointer was taken away.</summary>
    public event EventHandler? Cancelled;

    /// <summary>Ends the drag, if there is one, without a drop.</summary>
    public void Cancel()
    {
        var pointer = _pointer;
        var wasDragging = _isDragging;
        Reset();

        pointer?.Capture(null);
        if (wasDragging)
            Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control handle || !e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed)
            return;

        _pointer = e.Pointer;
        _pressedAt = e.GetPosition(_coordinates);
        e.Pointer.Capture(handle);
        e.Handled = true;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer)
            return;

        var position = e.GetPosition(_coordinates);
        if (!_isDragging)
        {
            if (Point.Distance(position, _pressedAt) < _threshold)
                return;

            _isDragging = true;
            Started?.Invoke(this, new PointerDragEventArgs(_pressedAt, position));
            return;
        }

        Moved?.Invoke(this, new PointerDragEventArgs(_pressedAt, position));
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer)
            return;

        var wasDragging = _isDragging;
        Reset();

        if (wasDragging)
            Dropped?.Invoke(this, EventArgs.Empty);
    }

    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_pointer == e.Pointer)
            Cancel();
    }

    private void Reset()
    {
        _pointer = null;
        _isDragging = false;
    }
}
