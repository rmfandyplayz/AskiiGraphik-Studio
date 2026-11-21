// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Footprints;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>What a canvas frame draws on top of the document.</summary>
/// <param name="IsGridVisible">Whether to draw the cell grid (when the cells are big enough).</param>
/// <param name="HoveredCell">The cell under the pointer, or null.</param>
/// <param name="Caret">The text caret's cell, or null.</param>
/// <param name="CaretThickness">The caret and brush outlines' thickness, in device pixels.</param>
/// <param name="Brush">The brush tip under the pointer, or null. It's drawn in place of the hovered cell.</param>
/// <param name="Selection">
/// What to show of the selection, or of what a selection tool's gesture would leave: its hatching and outlines.
/// Null for none.
/// </param>
/// <param name="SelectionThickness">The selection outline's thickness, in device pixels.</param>
/// <param name="SelectionDashLength">The length of each dash of the selection outline, in device pixels.</param>
/// <param name="SelectionMotion">How far the bright outlines' dashes and the hatching have moved.</param>
/// <param name="ToolPath">
/// A line the tool shows through these points in cells, such as a lasso's path, or null for none. It's drawn as
/// thick as the selection outline.
/// </param>
/// <param name="ShowsToolPathClosing">True to show faintly how the tool's line would close.</param>
/// <param name="ToolMarkers">The tool's markers, such as the Polygonal Lasso's corners, or null for none.</param>
/// <param name="Look">How the hatching, the mode colours and the markers look, in device pixels.</param>
internal readonly record struct CanvasOverlays(
    bool IsGridVisible,
    CellPoint? HoveredCell,
    CellPoint? Caret = null,
    int CaretThickness = 1,
    BrushOutline? Brush = null,
    SelectionDisplay? Selection = null,
    int SelectionThickness = 1,
    int SelectionDashLength = 4,
    SelectionMotion SelectionMotion = default,
    IReadOnlyList<CanvasPoint>? ToolPath = null,
    bool ShowsToolPathClosing = false,
    IReadOnlyList<ToolMarker>? ToolMarkers = null,
    SelectionLook Look = default);

/// <summary>
/// A brush tip's footprint, with its top-left at <paramref name="TopLeft"/>, which may be off the canvas.
/// </summary>
internal readonly record struct BrushOutline(BrushFootprint Footprint, CellPoint TopLeft);
