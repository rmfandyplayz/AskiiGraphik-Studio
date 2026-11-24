// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Canvas;

/// <summary>Ids of the canvas's view actions.</summary>
internal static class CanvasActionIds
{
    public const string ZoomIn = "askiiart.view.zoomIn";
    public const string ZoomOut = "askiiart.view.zoomOut";
    public const string ActualSize = "askiiart.view.actualSize";
    public const string ShowGrid = "askiiart.view.showGrid";
    public const string ShowFrameRate = "askiiart.debug.showFrameRate";
    public const string MouseActsAsPen = "askiiart.debug.mouseActsAsPen";

    // How strictly the Brush and the Soft Eraser keep edge glyphs' ink on the painted side (ADR 0075), until the
    // owner settles on one.
    public const string BrushEdgesStandard = "askiiart.debug.brushEdges.standard";
    public const string BrushEdgesStrict = "askiiart.debug.brushEdges.strict";
}
