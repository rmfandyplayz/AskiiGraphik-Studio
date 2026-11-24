// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.RampStrokes;

/// <summary>
/// How strictly the Brush and the Soft Eraser keep an edge glyph's ink on the side the tip painted (ADR 0075). Only a
/// Debug-menu switch chooses it for now, while the owner tries both.
/// </summary>
public enum EdgeStrictness
{
    /// <summary>Each edge cell is judged with the cells around it. The default; 4.10a's "3 with the fix".</summary>
    Standard,

    /// <summary>
    /// As <see cref="Standard"/>, and where the ink lies counts for more the fainter the edge, and a glyph inked on the
    /// wrong side is never picked; 4.10a's "1, 2 and 3 with the fix".
    /// </summary>
    Strict
}
