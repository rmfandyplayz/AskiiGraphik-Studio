// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Input;

namespace AskiiArt.App.Canvas;

/// <summary>The pointer's shape while the canvas pans, or while the chosen tool pans with a left-drag.</summary>
internal static class PanCursor
{
    /// <summary>
    /// Avalonia has no open hand of its own: its Hand is the pointing finger shown over links. On a Mac its DragMove
    /// is the system's open hand. Elsewhere DragMove is the drag-and-drop arrow, so the four-way move arrows stand in.
    /// </summary>
    public static StandardCursorType TypeFor(bool isMacOS) =>
        isMacOS ? StandardCursorType.DragMove : StandardCursorType.SizeAll;
}
