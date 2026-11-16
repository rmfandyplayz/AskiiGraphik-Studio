// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Footprints;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>What a canvas frame draws on top of the document.</summary>
/// <param name="IsGridVisible">Whether to draw the cell grid (when the cells are big enough).</param>
/// <param name="HoveredCell">The cell under the pointer, or null.</param>
/// <param name="Caret">The text caret's cell, or null.</param>
/// <param name="CaretThickness">The caret and brush outlines' thickness, in device pixels.</param>
/// <param name="Brush">The brush tip under the pointer, or null. It's drawn in place of the hovered cell.</param>
internal readonly record struct CanvasOverlays(
    bool IsGridVisible,
    CellPoint? HoveredCell,
    CellPoint? Caret = null,
    int CaretThickness = 1,
    BrushOutline? Brush = null);

/// <summary>
/// A brush tip's footprint, with its top-left at <paramref name="TopLeft"/>, which may be off the canvas.
/// </summary>
internal readonly record struct BrushOutline(BrushFootprint Footprint, CellPoint TopLeft);
