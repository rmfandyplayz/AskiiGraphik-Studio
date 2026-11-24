<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0068: The PATS brush (perceptually accurate text-based shapes)

Status: Accepted

> Partly replaced by [ADR 0075](0075-the-pats-brush-settled.md): each edge cell is also judged with the cells around it, and a cell whose glyph wouldn't clearly show its edge moves as a whole by the tip's weight at its middle, as before 4.7. The rest still stands.

## Context

After testing 4.5, the owner found that the Brush's edge cells on the Fine ramp often had their ink on the wrong side, such as `` `` `` and `..` along a circle's top where `..;;..` belongs, and that square edges were off too. Each cell took the glyph whose darkness matched the tip's weight at the cell's middle, so where the shape sits in the cell was lost. Step 4.7 picks edge glyphs by where the shape sits and how much of the cell it covers. It's always on, hard tips included, and uses only the ramp's glyphs. The owner accepts glyphs that are less predictable in return for truer shapes. The matching is to be shared with M6's structural matcher (ARCHITECTURE §17).

## Decision

- **`GlyphShape` (Core) is where a glyph's ink lies:** the cell cut into 4 × 8 equal parts, and how much of each the ink covers. `IGlyphDensityProvider.GetShape` gives it. `GlyphDensityMeter` measures it with the density, in the same drawing at 64 px per em, splitting each pixel along a part boundary between the two parts by area, so every part is exactly 1/32 of the cell and a shape's average is the glyph's density.
- **`BrushFalloff` also weighs each part of a cell** (2 × 2 points per part) wherever the weight changes across it by more than 0.05 (`EvenSpread`). These cells are left even: those wholly in the fully weighted middle; every cell of a tip with no middle cell (none with all four neighbours in it, as with tips one or two cells tall), which is all edge, so shaping would only lighten it; and the cell or cells the footprint's middle touches, so a small fully soft tip still paints its middle fully (ADR 0049). `BrushTipSource.FalloffWith` keeps the falloff until the size, shape, font or softness changes.
- **`FootprintSweep.CoverWeighted` gives each cell's parts their greatest weight** over a stroke's stamps, and the lightest part as the cell's weight. A stamp that paints a cell evenly and outweighs every part of a shaped stamp leaves it even.
- **`RampStroke` moves each part on its own.** A part moves once per stroke, by the greatest weight that has reached it, as whole cells did (ADRs 0049, 0054). An even cell moves as before. A shaped cell's target is each part's darkness moved by its weight, and the cell gets the level, between where it was and the target's level, that `RampShapeMatcher` picks. The Soft Eraser's at-least-one-level rule uses the parts' average move.
- **`RampShapeMatcher` (Core, public, for M6 too) picks a level for a target of 32 part intensities.** A level's score is how far its intensity is from the target's average, plus 4 times (`ShapeWeight`) what's left of the target's unevenness once the glyph's own unevenness, scaled to its level's intensity, is taken from it. The glyph's pattern is scaled down as far as fits best, between 0 and 1, never up: a bold glyph isn't blamed for being bolder than a faint edge, and one whose ink lies the wrong way shows nothing of the edge. Both sides are first blurred once by 1-2-1 across and down, as the eye blurs thin strokes. The lowest score wins, the darker on a tie. An even target gets the level nearest its intensity. The search starts at the level nearest in tone and stops each way once the tone gap alone scores worse than the best so far.
- **The weights were tuned by eye** on the bundled fonts' Fine and Standard ramps, with Blocks, Braille and the letters-and-digits ramp checked: a soft circle's top on Fine gets `._______.`, its bottom `'""""'`, its sides `(`, `{`, `}`, `)`. Standard barely changes.

## Alternatives

- **Plain least squares against the glyph's real ink:** bold glyphs lost to faint edges, and Standard's soft fade collapsed to two or three glyphs. A minimum scale above 0 did the same. Without the blur, `|` won far too often. A shape weight of 1 left backticks on circles' tops.
- **Shaping only where the tip is soft, or as an option:** the owner wanted it always on.
- **Finer parts or a real image metric (SSIM):** slower on every stroke, with nothing visibly better at brush sizes; M6 can use the matcher's targets with its own metric.

## Consequences

- Edge glyphs follow the shape, at some cost in predictability, and a soft tip's whole-cell tone is now the average over each cell rather than the weight at its middle.
- A sliver of shape in the top or bottom eighth of a cell, where few glyphs reach, falls back to the nearest tone, whichever side its ink is on.
- Strokes cost more. Measured over a 153-move stroke on a 500 × 200 canvas, the slowest move of a fully soft size-100 tip went from 2.1 to 3.4 ms in Release and from 7.7 to 13.7 ms in Debug; the first stroke with a new big soft tip takes about 6 ms (25 ms in Debug) to weigh it.
- M6's image conversion can reuse `GlyphShape` and `RampShapeMatcher` with targets taken from the image.

Updated 2026-11-23: step 4.10a adds temporary variants beside this brush for the owner to compare, and this brush stays the default (ADR 0074).

Updated 2026-11-23: step 4.10b settles the brush on judging edge cells with their neighbours and the whole-cell fallback (ADR 0075).
