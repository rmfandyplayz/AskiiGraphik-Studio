// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Imaging.Glyphs;

/// <summary>
/// Settings that change how glyphs are rasterized (ADR 0014). Glyph atlases are cached per
/// (font, pixel size, options), so two equal option records share an atlas.
/// </summary>
public sealed record GlyphRenderOptions
{
    /// <summary>Smoothed grey-scale glyphs with slight hinting.</summary>
    public static GlyphRenderOptions Default { get; } = new();

    /// <summary>
    /// Whether glyph edges are smoothed (grey-scale coverage). When false, every pixel is either
    /// fully inked or blank.
    /// </summary>
    public bool Antialias { get; init; } = true;

    /// <summary>How strongly outlines snap to the pixel grid.</summary>
    public GlyphHinting Hinting
    {
        get;
        init
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, null);

            field = value;
        }
    } = GlyphHinting.Slight;
}
