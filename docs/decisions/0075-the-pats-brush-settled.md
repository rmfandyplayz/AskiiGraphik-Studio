<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0075: The PATS brush settled

Status: Accepted

## Context

Step 4.10a let the owner compare ways of picking edge glyphs (ADR 0074). The owner chose idea 3 with the fix as the brush's own, and kept 1, 2 and 3 with the fix beside it in the Debug menu through 4.10b's testing, then kept only idea 3 with the fix, which drew squares better. In 4.10a's testing, idea 3 stuttered with big soft tips. ARCHITECTURE §21 asks for brush strokes that feel instant.

## Decision

- **Neighbours.** After each move of a stroke, every shaped cell the stroke has reached, within one cell of a cell the move raised, is picked again with the cells around it, in row order, twice over. `RampShapeMatcher` judges it across a window of 8 × 16 parts: the cell and the half of each neighbour nearest it. Only where each cell's ink strays from that cell's own average counts, so tone stays each cell's own business. Neighbours the stroke reached use what the stroke means them to look like as their target, and every other neighbour uses how it looks now, so art already on the layer counts. A neighbour's look is scaled down to fit its target. Only cells the stroke paints, within the selection, change.
- **The fallback.** `Match` also says whether the glyph shows at least 25% (`ClearShare`) of how the target strays from its average. Where it doesn't, the cell moves as a whole by the tip's weight at its middle, as before 4.7. `FootprintSweep` keeps each shaped cell's greatest middle weight (`WeightedCell.MiddleWeight`) for this.
- **Removed:** `PatsVariant`, every other variant (4.6's and 4.7's brushes, ideas 1 and 2, and 1, 2 and 3 with the fix, which 4.10b's testing kept as a Debug-menu choice called Strict), the Debug menu's Brush Edges items, and the code that drew the comparison sheets. They stay in git history (commits 7fa3be9 and 2803f7f).
- **Speed, with the same output.** Each level's glyph is blurred into an empty window once, when the matcher is made. Because the blur is linear, a cell's window score is then a dot product over the 60 parts the glyph's blur reaches, plus a sum of squares worked out once per cell. The second pass picks a cell again only if a neighbour's glyph changed after its last pick, and a pick that changes nothing writes nothing. A 153-move stroke gave the same cells, cell for cell, before and after.

## Alternatives

- **Keeping Strict as an option:** the owner preferred one brush; other ways of painting could come back as optional plugins in M9.
- **Fewer passes, or skipping the first pick of cells that are picked again:** faster, but changes the look the owner chose, and Release builds don't need it.
- **SIMD through System.Numerics.Tensors:** a new package, and not needed.

## Consequences

- With a fully soft size-100 tip on Fine, over 153 moves on a 500 × 200 canvas, the slowest move is about 5 ms in Release, against about 3 ms for 4.7's brush, and about 14 ms in Debug, down from 24 ms before the speed-ups. That's inside a 60 fps frame, but Debug has little room left, so M10 may still want to look.
- A ramp whose glyphs are all even moves every cell as a whole by the weight at its middle, as before 4.7, so Core tests with even fake glyphs expect 4.6's results again.
- The fallback's glyph is the level's, whichever side its ink is on, so stray marks such as backticks round soft squares on Fine can remain.
- Edge glyphs near the tip may change as the stroke grows.
- Faint soft strokes can still show a gap along their top edge (4.10b's testing, on Fine). The owner accepted that as part of a rule that fits most cases.

Updated 2026-11-24: after 4.10b's testing the owner kept only idea 3 with the fix, so Strict and the Debug choice were removed.
