<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0071: Selections past the canvas edge

Status: Accepted

## Context

In ADR 0066 a selection moved off the canvas was cut to it, so its outline closed along the canvas edge and a shape moved off and back lost its cells. After 4.6's testing the owner asked for step 4.9:
- a move can take the selection past the edge, into a zone 10 columns wide left and right and as far on screen above and below;
- arrows and drags stop at the zone's outer edge, and a drag slides along it;
- off the canvas only the outline shows, at half strength;
- selection tools and commands work on the canvas only and drop the part off it, which shows at 15% while one is in use;
- a selection all off the canvas still counts: drawing paints nothing, Move moves it, and Copy, Cut and Delete are greyed out;
- the outline stays where a move left it.

## Decision

- **The mask can hold cells past the canvas.** `SelectionMask` keeps its canvas size, but its bounds may reach outside it. Only `MovedWithin(canvasSize, offset, area)` makes such a mask, keeping the cells inside `area`. Every other way of building one, the public `SelectionMaskBuilder` included, still crops to the canvas, so the selection tools can't make one by accident.
- **The zone** (`SelectionZone.Around`) reaches 10 columns past the left and right edges, and `RowsPastCanvas` rows above and below: 10 columns' width on screen in rows, rounded, at least 1 (5 rows when cells are twice as tall as wide).
- **Moves:** `FloatingMove` builds each step's selection with `MovedWithin` the zone, and `Limit` clamps an offset on each axis so the selection's sides stay inside the zone. A shape wider than the zone can move only as far as it still covers it, and a gesture can always go back to where it began. Cells landing off the canvas are still lost when the move ends; only the selection stays.
- **Canvas-only work:** `CutToCanvas()` gives the part on the canvas, cached. Selection tools combine from it (`SelectionPreview`), and Invert, Grow, Shrink, Clear, Copy and text export use it. `HasCellsOnCanvas` greys out Copy, Cut, Delete, Grow, Shrink, Copy as Text and Copy as ANSI when nothing selected is on the canvas. `IsEmpty` still counts cells past it, so drawing tools clip to nothing and the whole canvas is hatched.
- **Drawing:** the outline is split at the canvas edge. The part past it is drawn at half strength with running dashes, or at 15% and still while a selection tool's gesture would drop it. Hatching stays on the canvas. The Move tool's cursor shows over selected cells past the canvas.

## Alternatives

- **Keep the selection on the canvas and remember the moved shape apart:** two shapes to keep in step through undo and redo, for what is still one selection.
- **Let every mask reach past the canvas:** every selection tool, command and exporter would have to crop for itself, and a missed one would select or copy cells that don't exist.
- **Copy the whole document, or nothing, for an all-off selection:** either surprises; greying out shows there's nothing to copy.

## Consequences

- A resize keeps the cells that land on the new canvas, including ones that were past the old one, and drops the rest.
- With a canvas bigger than the window, the 32-pixel canvas margin can't show the whole zone (open question to the owner).
- The opacities, rounding and greyed-out commands beyond the three named are guesses (TEMPORARY-UI).
