// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using Avalonia;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>A straight piece of the selection's outline, in device pixels, drawn dashed along its length.</summary>
/// <param name="Rect">The strip, along the inside of the selected cells.</param>
/// <param name="Side">Which side of the selected cells it lies along.</param>
internal readonly record struct SelectionStrip(PixelRect Rect, CellEdgeSide Side)
{
    /// <summary>True when it runs across, along a row; false when it runs down a column.</summary>
    public bool IsHorizontal => Side is CellEdgeSide.Top or CellEdgeSide.Bottom;

    /// <summary>
    /// True when its dashes run right or down as they move, false for left or up. They run clockwise around the
    /// selected cells: right along a top edge, down a right edge, left along a bottom edge and up a left edge.
    /// </summary>
    public bool RunsForward => Side is CellEdgeSide.Top or CellEdgeSide.Right;
}
