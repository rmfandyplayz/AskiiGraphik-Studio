<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0074: PATS comparison variants

Status: Accepted

## Context

At 4.7's first look, faint soft strokes kept backticks along their tops, with a gap below them (ADR 0068). The owner chose to compare three ideas, alone and together, before settling the brush (step 4.10a), and then to keep one and remove the rest (4.10b). This ADR records the temporary code that lets the owner compare them.

## Decision

- **Variants.** `PatsVariant` (Core, `Tools/RampStrokes`) names one way to pick edge glyphs. `PatsVariant.All` lists, in order: 4.6 (no shaping: each cell takes the tip's weight at its middle, through `BrushFalloff.Create(..., shapesEdges: false)`), 4.7 (the shipped PATS, the default), 1, 2, 3, 1+3 and 1+2+3. `ToolContext.Pats` passes the choice to the Brush and the Soft Eraser, and each stroke reads it as it starts.
- **Idea 1, shape counts more when faint.** Where a cell's average target is under 0.5, the shape term's weight is multiplied by (0.5 / average)², at most 25.
- **Idea 2, the wrong side is left empty.** A level is ruled out when the target is uneven, its glyph is uneven, and the glyph's strays disagree with the target's. Uneven means a stray weight over 5% of 32 × intensity². If every level is ruled out, the cell takes the lightest level it may.
- **Idea 3, neighbouring cells.** After each move of the stroke, every shaped cell the stroke has reached within one cell of a cell the move raised is picked again, twice over, in row order. Each is judged across a window of 8 × 16 parts: the cell and the half of each neighbour nearest it. Each part of the window counts how far its cell's ink strays from that cell's own average, so tone stays each cell's own business and only where the ink lies crosses cell edges. Neighbours the stroke reached use what the stroke means them to look like as the target. Every other neighbour, painted or empty, uses how it looks now. A neighbour's look is scaled down to fit, just as a glyph in a cell of its own is. Both windows are blurred 1-2-1 and compared, counted as 32 parts' worth. Only cells the stroke paints, within the selection, change.
- **The fix** (after 4.10a's testing, in variants 3+fix and 1+2+3+fix). A shaped cell whose chosen glyph shows under 25% of how its target strays from its average is weighed as a whole at its middle, as 4.6 weighed it. `FootprintSweep` keeps each shaped cell's greatest middle weight for this. Straight sides, which few glyphs can show, and cells the tip only grazes then look as they did in 4.6, while edges a glyph shows well stay shaped. A bar of 50% left almost nothing shaped.
- **Sheets.** `PatsComparisonSheets` (Imaging.Tests, run only with `--explicit only`) paints the same strokes with every variant into PNGs in `.local/pats-sheets`. Since the fix, the sheets compare 4.6, 3, 3+fix, 1+2+3 and 1+2+3+fix, and add a size-7 stamp and line. Each sheet is one font (JetBrains Mono or Roboto Mono), one ramp (Fine or Standard) and one softness (0, 50 or 100%), with intensities of 25, 50 and 100% across.
- **Debug menu.** Nine "Brush Edges" items, ticked like radio items, set `CanvasViewModel.Pats`. The choice resets to 4.7 at each start.

## Alternatives

- **Choose by reasoning alone:** the ideas interact, and only the owner's eye can judge the result.
- **Settings-file switch:** slower to try than a menu item while painting.
- **Idea 3 comparing tone across cells too:** tried first. A cell's tone made up for its neighbours' tone, which turned soft interiors into noise.

## Consequences

- `RampShapeMatcher` and `RampStroke` carry code paths that 4.10b removes, all but the chosen one. With the default variant, the brush paints exactly as 4.7's did.
- Idea 3 costs more on every move. 4.10b measures whichever variant is chosen against ARCHITECTURE §21's budget.
- Tips under three cells tall still aren't shaped (ADR 0068), so the sheets draw the diagonal and the curve with a size-6 tip.

Updated 2026-11-23: added the fix and its two variants after 4.10a's testing.
