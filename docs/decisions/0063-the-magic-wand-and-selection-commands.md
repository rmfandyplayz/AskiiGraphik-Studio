<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0063: The Magic Wand and the selection commands

Status: Accepted

## Context

Step 4.3 adds the Magic Wand and the rest of ARCHITECTURE §10's selection commands. The owner settled the details on 2026-11-20: a match mode dropdown with lightness among the modes, a tolerance slider that a sideways drag also moves, two-button toggles for contiguous or global and for the active layer or all visible layers, 4-way joins, Grow and Shrink by one cell with corners, the occupied-cells command split in two, the wand 5th in the Select group with W picking it too, and Invert on Cmd+Shift+I for now.

## Decision

- **Matching** (`MagicWandMatch`), against the clicked cell. A blank cell is an empty one or a space.
  - **Same Glyph:** the same character; blanks match blanks. Tolerance doesn't apply.
  - **Lightness:** the glyph's ink density in the document's font (ADR 0045), within the tolerance as a share of 0 to 1. A blank counts as no ink.
  - **Text Color** and **Background Color:** each of red, green and blue within the tolerance as a share of 255. An ANSI colour counts as its VGA colour. The default text colour, and no background, match only themselves. A blank cell's text colour doesn't show, so blanks match only blanks.
- **Contiguous** joins cells along their sides only, filled a run at a time (a scanline fill). **Global** takes every matching cell. **Visible Layers** compares the composite, from the one compositor (§5).
- **One sample per click** (`MagicWandSample`): the press reads the cells a row at a time into one key per cell, so a drag only compares and fills again. An undo or redo during the click (a document or active-layer change) takes it again. The last shape is kept for its tolerance.
- **The click:** the press previews at once through `SelectionCombiner` (ADRs 0060, 0061). Dragging sideways changes the tolerance by 1% per 2 logical pixels from the press (`PixelsPerToleranceStep`), written to the shared option values so the slider follows. The canvas passes a cell's width on screen (`ToolPointerInput.CellWidthOnScreen`). Letting go is one Magic Wand step; Esc or switching tools cancels and puts the tolerance back. A press off the canvas is a click on nothing.
- **Tool letters:** a `ToolGroup` can give its tools letters of their own (`ToolLetters`). The registry rejects a letter used twice and a letter for another group's tool.
- **Commands** (`SelectionEdits`), each an undo step, and none when nothing would change:
  - **Invert:** the canvas minus the selection.
  - **Grow:** adds every cell touching the selection, corners included.
  - **Shrink:** Invert, Grow, Invert, so the canvas's edges don't count as unselected and Select All stays whole. Grow and Shrink are available only while something is selected.
  - **Select Cells with Glyphs** (other than a space) and **Select Cells with Background Color**, on the active layer, in place of the selection.

## Alternatives

- **8-way joins:** ASCII lines are often one cell wide, so a fill would leak across diagonal gaps. The owner chose 4-way.
- **Colour distance in a perceptual space (CIELAB), or straight-line RGB distance:** both are harder to predict from a percentage than a per-channel limit. Either can come later behind the same option.
- **Reading the layer again at every drag step:** with visible layers, every pointer move would composite the whole canvas.
- **Shrinking from the canvas's edges too:** Select All then Shrink would eat the border. It can become an option when Grow and Shrink get their dialogs.

## Consequences

- A click holds 4 bytes per cell while it lasts: 1 MB at 500 × 500, 64 MB at the 4096 × 4096 maximum. Measured on 500 × 500 in a Debug build, a click takes 5 to 18 ms, within §21's 100 ms.
- A tool needing screen distances now gets the cell width from the canvas, as the Polygonal Lasso's markers get their reach.
