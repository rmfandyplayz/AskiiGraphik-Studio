// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Imaging.Glyphs;

/// <summary>
/// What a cell's glyph was drawn from (ADR 0013). Anything other than <see cref="PrimaryFont"/>
/// means the document's font lacks the glyph, and the canvas flags the cell as an error.
/// </summary>
public enum GlyphSource
{
    /// <summary>The document's own font has the glyph.</summary>
    PrimaryFont = 0,

    /// <summary>The document's font lacks the glyph, so another installed font drew it.</summary>
    FallbackFont = 1,

    /// <summary>No available font has the glyph, so a placeholder box was drawn instead.</summary>
    Placeholder = 2
}
