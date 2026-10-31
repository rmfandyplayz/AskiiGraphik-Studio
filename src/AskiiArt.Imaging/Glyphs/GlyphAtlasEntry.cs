// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using SkiaSharp;

namespace AskiiArt.Imaging.Glyphs;

/// <summary>Where a glyph sits in a <see cref="GlyphAtlas"/>.</summary>
/// <param name="PageIndex">The page bitmap that holds the glyph (see <see cref="GlyphAtlas.GetPage"/>).</param>
/// <param name="Bounds">The glyph's cell-sized rectangle on that page, in pixels.</param>
/// <param name="Source">What the glyph was drawn from; anything but the primary font flags the cell.</param>
public readonly record struct GlyphAtlasEntry(int PageIndex, SKRectI Bounds, GlyphSource Source);
