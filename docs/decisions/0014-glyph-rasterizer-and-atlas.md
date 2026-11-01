<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0014: Glyph rasterizer and atlas

Status: Accepted

## Context

ARCHITECTURE Principle 4 asks for one rasterizer shared by the canvas, PNG export and glyph-density measurement, and §11 asks for a glyph atlas cached per (font, pixel size, render options), rasterized at device-pixel size and never scaled. How the canvas draws the atlas is step 1.4's decision.

## Decision

- **`GlyphRasterizer`** is the one rasterizer. It draws a single rune into one cell of any `SKCanvas`, in any ink colour, so the atlas, PNG export and the font sheets all use the same code. Each glyph is drawn alone (no shaping, so no ligatures) and **clipped to its cell**: redrawing a cell never affects its neighbours, which keeps dirty-region redraws simple.
- **Placement:** the document font's glyphs sit on the cell's baseline, centred horizontally by advance. A fallback glyph that doesn't fit is drawn at a smaller font size (never a scaled bitmap) and centred by its ink.
- **Too-wide glyphs:** a document-font glyph whose ink reaches more than 5% of the cell width past either side (like `↔` in JetBrains Mono and Fira Code, about 1.3 cells wide) is squeezed sideways from its outline to fit, keeping its height. The decision is measured unhinted, from the glyph's outline and relative to the cell, so it's the same at every size and on every OS. Box-drawing, block and legacy-computing characters (U+2500–259F, U+1FB00–1FBFF) are never squeezed: they reach past the edges on purpose so neighbours join, and clipping is right for them.
- **Smoothing:** grey-scale anti-aliasing with slight hinting by default; never LCD (colour) smoothing, because atlas glyphs are later drawn in any colour on any background, where LCD fringes show. Sub-pixel positioning is off so glyphs land on whole pixels. `GlyphRenderOptions` (anti-aliasing on/off, hinting level) is part of the atlas key.
- **`GlyphAtlas`:** one per (font, pixel size, options). Glyphs are rasterized on first use into cell-sized slots on alpha-only (`Alpha8`) page bitmaps of about 1024 × 1024 pixels; the alpha is the coverage, so the canvas can tint it. Each entry records its page, rectangle and `GlyphSource`.
- **`GlyphAtlasCache`:** keeps the 8 most recently used atlases (one per zoom level and font in practice) and disposes the least recently used. Callers fetch the atlas each time they draw rather than holding on to it.
- **Not thread-safe:** rasterizers and atlases belong to one thread. Background work (export, image conversion) creates its own.
- **Font sheets:** a test draws a sheet per bundled font into `artifacts/font-sheets/` (ignored by git), and CI uploads them for each OS. They're not snapshot-compared: each OS anti-aliases slightly differently.

## Alternatives

- **Packing glyphs by their ink size** instead of cell-sized slots: less memory, but more bookkeeping, and cells are small anyway.
- **RGBA atlas pages:** simpler to draw with Avalonia's `DrawingContext`, but four times the memory and the colour is baked in. Step 1.4 can convert if its measurements say so.
- **Letting glyphs overflow their cell** (like some terminals): italic overhangs would show, but every redraw would need its neighbours too.

## Consequences

- Pages reach the screen through a Skia lease, as copies refreshed by each page's version (ADR 0015).
- Glyph-density measurement (M3) and PNG export (M7) draw through `GlyphRasterizer`.

Updated 2026-10-31: pages have a version (step 1.4, ADR 0015); added the sideways squeeze for too-wide glyphs, after the owner found `↔` cut off in the step 1.3 font sheets.
Updated 2026-10-31: ink bounds come from glyph outlines (step 1.5). Skia's glyph metrics on macOS are rounded to whole pixels and padded, which squeezed `←` there and failed CI.
