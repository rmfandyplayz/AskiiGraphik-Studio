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

    // Step 4.10a's temporary choice of how the Brush and the Soft Eraser pick edge glyphs (ADR 0074).
    public const string PatsUnshaped = "askiiart.debug.pats.unshaped";
    public const string PatsShipped = "askiiart.debug.pats.shipped";
    public const string PatsIdea1 = "askiiart.debug.pats.idea1";
    public const string PatsIdea2 = "askiiart.debug.pats.idea2";
    public const string PatsIdea3 = "askiiart.debug.pats.idea3";
    public const string PatsIdeas1And3 = "askiiart.debug.pats.ideas1And3";
    public const string PatsIdeas1And2And3 = "askiiart.debug.pats.ideas1And2And3";
    public const string PatsIdea3Fixed = "askiiart.debug.pats.idea3Fixed";
    public const string PatsIdeas1And2And3Fixed = "askiiart.debug.pats.ideas1And2And3Fixed";
}
