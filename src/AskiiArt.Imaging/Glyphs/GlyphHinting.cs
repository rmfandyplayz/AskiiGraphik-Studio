// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Imaging.Glyphs;

/// <summary>How strongly glyph outlines are snapped to the pixel grid when rasterized.</summary>
public enum GlyphHinting
{
    /// <summary>No snapping: the outline is drawn exactly as designed.</summary>
    None = 0,

    /// <summary>Snaps vertically only, which sharpens horizontal strokes without distorting shapes.</summary>
    Slight = 1,

    /// <summary>Snaps in both directions.</summary>
    Normal = 2,

    /// <summary>Snaps as hard as the platform allows.</summary>
    Full = 3
}
