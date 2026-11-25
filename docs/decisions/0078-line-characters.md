<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0078: Line characters

Status: Accepted

> Partly replaced by [ADR 0079](0079-the-line-tool-and-the-shapes-group.md): Standard by step stays as a style of its own beside Standard, and the comparison sheets are gone. The rest still stands.

## Context

M5's lines and shapes need a character in every cell they pass through. Step 5.1 (once 4.10c) asked whether they choose characters by shape, reusing the glyph measurements the Brush uses (ADR 0068). The owner chose, before 5.1 began, to offer every way as a Style option that later becomes a dropdown on Tool Options: **Standard** (characters by direction), **Precise** (characters by measured shape) and **Single character**. The Brush's own edge shaping (ADR 0075) stays the Brush's. 5.1 builds the picking and comparison sheets for the owner to judge; the Line tool comes in 5.2.

## Decision

- **Paths.** `LinePath` (Core, `Tools/Lines`) is a line, a rectangle, an ellipse or any path of straight pieces in canvas coordinates. Rectangles and ellipses run through the middles of their outermost cells, so they fill their rect exactly; an ellipse is cut into pieces of a quarter cell or less.
- **Cells.** `LinePathCells` takes one cell to a column where a piece runs more across than down, and one to a row elsewhere: the cell the piece is in where it crosses that column's (or row's) middle, as Bresenham's line takes them. An open path's end cells are always in. Cells that don't touch are joined with Bresenham's line, and each cell counts once. A point where the path turns more than 45° on screen makes its cell a corner. Every style uses the same cells, so the styles differ only in characters.
- **Standard.** Each cell gets the character whose angle on screen is nearest the path's direction there: `-` flat, `|` upright, and `\` and `/` at the angle from corner to corner of a cell, which depends on the font's cell shape. Corners are `+`.
- **Standard by step.** Each cell gets the character for the step from the cell before it to the cell after it, so shallow lines show each step as `\` or `/`. First a candidate to replace Standard, now a style of its own (ADR 0079).
- **Precise.** `PreciseLineMatcher` measures, in each cell, how much of each of the 32 parts (ADR 0068) a stroke an eighth of a cell thick along the path covers, sampling each part at 4 × 4 points with distances as they look on screen. Both that and each candidate's measured ink are blurred 1-2-1, as `RampShapeMatcher` does, and the candidate whose ink lies most like the stroke wins: the highest cosine similarity, which ignores how much ink each has. The candidates are ``- _ . , ' ` " | / \``, those the document's character set allows and that show ink in its font. Corners are `+`, as in Standard. An open path's ends are measured as if it went on, so its end cells look crossed right through. With no usable candidate, Precise draws Standard.
- **Centring.** Glyphs ink the middle of their cells across, so where a path runs more down than across, it's measured centred across each cell. Glyphs do ink their cells at many heights (`_ . - ' "`), so a gentle slope keeps its height; a slope from half a row to one row per column is centred up and down by more the steeper it is, since it otherwise clips cells' corners.
- **Sheets.** `LineComparisonSheets` (Imaging.Tests, explicit only) draws the same lines (every angle from 5° to 85°, and a star every 15°) and shapes (rectangles, round and wide ellipses, waves, a curve and a spiral) in each style, in all four bundled fonts, as PNGs in `.local/line-sheets`, with each style also cut out on its own in `rows/`. 5.2 removed it.

## Alternatives

- **The Brush's `RampShapeMatcher`:** it matches tone and soft edges on a ramp, not where a thin stroke runs, and blends with what's there.
- **`(` and `)` and `+` as Precise candidates:** tried. `+` won on flat lines, since its bar is wider than `-`'s in most fonts, and `( )` made straight upright lines wobble where they ran off-centre.
- **Precise choosing its own cells** (for example two cells where a stroke crosses between rows): it would draw thicker, broken lines, and the styles would no longer swap cell for cell.

## Consequences

- Precise depends on the font: the bundled fonts pick differently in places, such as `,` for `.`, and a font loaded from a file may pick differently again.
- Steep lines in Precise look much like Standard, since no common ASCII character inks one side of its cell.
- Finding a cell's nearby pieces allocates; ADR 0079 records what a pick costs.

Updated 2026-11-24: Standard by step is kept as a style, the sheets are removed, and the cost is measured (ADR 0079).
