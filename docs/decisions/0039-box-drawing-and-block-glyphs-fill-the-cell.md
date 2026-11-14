<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0039: Box-drawing and block glyphs fill the cell

Status: Accepted

## Context

Step 2b.2's test found thin lines and gaps between neighbouring box-drawing and block characters at most zoom levels, on Windows and macOS. They had been there since step 1.3, and coloured backgrounds made them easy to see. These glyphs were drawn like letters: from the font, at its size, on the baseline (ADR 0014). But a cell is whole pixels, because `CellMetrics` rounds the line height, baseline and advance, and hinting moves edges too. So `█`'s edges landed a fraction of a pixel inside the cell, or a whole pixel short. Fonts differ as well. In JetBrains Mono and Fira Code, `█` is exactly the line box; in IBM Plex Mono it sits 0.075 em lower, and its `│` often missed the top of the cell. Drawn unhinted and unsnapped, a line thinner than a pixel spreads over two and looks faded.

## Decision

Box drawing, block elements and Symbols for Legacy Computing (U+2500–259F, U+1FB00–1FBFF), from the document font or a fallback, are drawn from their outlines by `CellFitting` instead of as text.

- **Fit:** the unhinted outline is stretched so the drawing font's `█` covers the cell exactly. A font without a plausible `█` uses its line box instead. Every glyph of a font gets the same stretch, so they meet at the cell's edges at every size.
- **Snap** (when hinting is on, which is the default): box-drawing lines move onto whole pixels. `StrokeSnap` takes the strokes of the font's straight lines (`─ ━ ═` and `│ ┃ ║`) and gives each stroke edge one whole-pixel position, moving it as little as possible while every stroke, and the gap in a double line, stays at least a pixel wide. Everything between edges stretches to follow. Glyphs are built from the same edges, so lines stay continuous from glyph to glyph.
- Block elements, sextants, wedges and eighths round their corners to whole pixels. A corner inside the cell never rounds onto the cell's edge, so `▏` stays visible in a small cell. The shades (`░▒▓`) are fitted but not snapped.
- The fitting and snapping are our own code and the shapes are filled by Skia, so these glyphs look the same on every OS. Bitmap glyphs have no outline and are drawn as before.

## Alternatives

- **Drawing these characters ourselves,** as Windows Terminal, kitty and WezTerm do: exact and crisp, but the shapes would be ours rather than the font's, and there are several hundred to write. It stays in reserve if a font misbehaves.
- **Snapping each stroke on its own:** simpler, but fonts share edges between lines (a double line's inner edges are often the single line's outer ones). Two strokes then pulled one edge two ways, and double lines collapsed at small sizes.
- **Rounding the cell differently, or using hinted outlines:** neither makes `█` match the cell in every font, and hinted outlines differ by OS.

## Consequences

- Box-drawing glyphs keep each font's style and stroke weight. At small sizes, a line may sit up to half a pixel from where the font puts it, and light and heavy lines can look the same at the smallest zoom.
- With `GlyphHinting.None`, these glyphs are fitted but not snapped.
- Tests check every bundled font at the canvas's zoom sizes: blocks fill to the cell's edges, lines meet the next cell, lines are solid or blank in every pixel, double lines stay double, and half blocks split the cell on a whole pixel.
