// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>What a canvas frame draws on top of the document.</summary>
/// <param name="IsGridVisible">Whether to draw the cell grid (when the cells are big enough).</param>
/// <param name="HoveredCell">The cell under the pointer, or null.</param>
/// <param name="Caret">The text caret's cell, or null.</param>
/// <param name="CaretThickness">The caret outline's thickness, in device pixels.</param>
internal readonly record struct CanvasOverlays(
    bool IsGridVisible,
    CellPoint? HoveredCell,
    CellPoint? Caret = null,
    int CaretThickness = 1);
