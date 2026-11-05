// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Solving;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// An invisible strip in a gap between widgets or along a side's inner edge. It has a background so that it catches the
/// pointer instead of letting it through to the canvas, because dragging it resizes the windows.
/// </summary>
internal sealed class StackResizeHandleView : Border
{
    /// <param name="id">Which handle it is.</param>
    public StackResizeHandleView(StackResizeHandleId id)
    {
        Id = id;
        Background = Brushes.Transparent;
        Cursor = new Cursor(id.Gap is null ? StandardCursorType.SizeWestEast : StandardCursorType.SizeNorthSouth);
    }

    /// <summary>Which handle it is.</summary>
    public StackResizeHandleId Id { get; }
}
