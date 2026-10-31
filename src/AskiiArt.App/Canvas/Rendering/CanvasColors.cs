// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>The colours a canvas frame is painted with, taken from the theme tokens.</summary>
/// <param name="Pasteboard">Around the canvas.</param>
/// <param name="Paper">The canvas's background.</param>
/// <param name="Ink">The glyphs.</param>
/// <param name="GridLine">The cell grid, when it's shown.</param>
/// <param name="Hover">Over the cell under the pointer.</param>
/// <param name="FallbackGlyph">Behind a glyph drawn with a fallback font.</param>
/// <param name="PlaceholderGlyph">Behind a glyph no font has.</param>
internal readonly record struct CanvasColors(
    SKColor Pasteboard,
    SKColor Paper,
    SKColor Ink,
    SKColor GridLine,
    SKColor Hover,
    SKColor FallbackGlyph,
    SKColor PlaceholderGlyph);
