// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Editing;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.RampStrokes;

namespace AskiiArt.Core.Tools;

/// <summary>
/// What a new tool works with: the document's editing session, and the colours and tool settings shared by every
/// document.
/// </summary>
/// <param name="Session">The editing session the tool changes.</param>
/// <param name="Colors">The current FG and BG colours, the same for every open document (ADR 0042).</param>
/// <param name="Settings">The tools' option values, the same for every open document (ADR 0047).</param>
/// <param name="CellWidthToHeight">
/// A cell's width divided by its height in the document's font, now. The font can change while the tool is in use.
/// </param>
/// <param name="Densities">
/// How dark each glyph looks in the document's font, now (ADR 0045). Ask each time, as the font can change.
/// </param>
/// <param name="Pats">
/// Which way the Brush and the Soft Eraser pick edge glyphs, for step 4.10a's comparison (ADR 0074); null for the
/// shipped one. Temporary.
/// </param>
public sealed record ToolContext(
    EditingSession Session,
    PaintColors Colors,
    ToolSettings Settings,
    Func<double> CellWidthToHeight,
    Func<IGlyphDensityProvider> Densities,
    Func<PatsVariant>? Pats = null);
