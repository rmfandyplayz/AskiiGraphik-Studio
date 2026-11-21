<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0061: The selection overlay

Status: Accepted

## Context

After testing step 4.2 the owner asked for the selection to show the way painting apps show it: hatching over what isn't selected, a live preview of every selection tool's result, Subtract and Intersect in their own colours, a faint line showing how a lasso would close, and markers on the Polygonal Lasso's corners. That became step 4.2b, and the owner settled its details on 2026-11-20.

## Decision

- **What the canvas shows** is a `SelectionDisplay` (App), made from the session's selection and, during a gesture, the tool's `SelectionPreview` (Core): the selection before, the mode and the shape, with the result and the cells it would deselect worked out when first asked for. A preview always shows what letting go now would give, by the same rule `SelectionCombiner` finishes with. When an undo or redo changes the selection during a gesture, `SelectionCombiner` hands the tool a new preview at once, so the canvas doesn't wait for the pointer to move, and the gesture carries on with the selection the undo brought back. Outlines come in three strengths (`SelectionOutlineStrength`): bright and running for lines that letting go would keep, faint for the shape's other lines, and fainter still (vanishing) for the old selection's.
  - **Replace:** the result's running outline, over the old one vanishing.
  - **Add:** the result's running outline, over the shape's faint one and the old one vanishing, so lines that end up inside it fade.
  - **Subtract:** a red outline running round the shape, over the old one, faint where it stays and vanishing where it goes. The cells it would deselect are hatched in red.
  - **Intersect:** a blue outline running round what stays, over the shape's faint blue one and the old one vanishing. The cells it would deselect are hatched in blue. Blue rather than green, so it never reads as red to people with red-green colour blindness.
  - Only bright outlines run. The others stay still, in faded colours.
- **A shape with no cells**, such as one drawn off the canvas, combines like any other, so Intersect with it deselects everything, as a shape over unselected cells would. Only a click makes no shape (`null`), and leaves the selection alone outside Replace: a box drag that never leaves its first cell, or a lasso or Polygonal Lasso with fewer than three points. A gesture that leaves nothing selected is a Deselect step.
- **Hatching:** every cell outside the result, and outside the cells being deselected, gets lines rising to the right, a dark one beside a light one so they show on any art. Cells being deselected get lines falling the other way, in the mode's colours. Hatching shows whenever something is selected or was before the gesture, so it stays while a shape would leave nothing. The lines keep their size on screen at every zoom, and repeat from the canvas's corner, so they stay put on the art while it scrolls, without lining up with the cells.
- **Moving hatching:** the hatching over what isn't selected moves to the top left at the outline's dash speed. During Subtract and Intersect it stands still while theirs moves to the bottom left, also at the dash speed, and then carries on from where it stopped (`PausableClock`). The tile is shifted along a row, which looks like lines at 45° moving across themselves, by part of a pixel at a time so it glides (`SelectionMotion`, ADR 0062). The grid shows at 40% under the first hatching and at 70% under the others.
- **Drawing it:** like the cell backgrounds (ADR 0038), each hatching is an alpha image with a pixel per visible cell, filled from the masks a run of cells at a time (`SelectionMask.RunsInRow`), plus a small tile of lines. The painter draws the tile through the cell image with one blended shader, so the cost follows the visible cells, not the size of the selection. The grid is drawn through a third such image (`GridShade`) that holds how strongly it shows over each cell.
- **The lassos** show a faint dashed line from the path's end back to its start (`ToolOverlay.ShowsPathClosing`) and the live preview. The shape is worked out only when the canvas draws the overlay, at most once per overlay.
- **The Polygonal Lasso's markers** (`ToolOverlay.Markers`): small, slightly faded dots on its corners, and a bigger marker on the first, all the same size on screen at every zoom. With three corners or more, a click on the first corner's marker finishes the shape, which replaces ADR 0060's click in the first corner's cell. While the pointer is on it, its colours swap round and the line snaps to it. Only the canvas knows how big a cell is on screen, so it tells the tool how far a marker reaches, in cells across and down (`ToolPointerInput.MarkerReach`).
- **The look** (colours, sizes and opacities) comes from theme tokens, read into a `SelectionLook`.

## Alternatives

- **Hatching lined up with the cells:** it would zoom with the art, so it'd be too dense zoomed out and too sparse zoomed in.
- **A tint over what isn't selected:** it changes the art's colours, which an artist needs to see as they are.
- **Finishing on the first corner's cell** (ADR 0060): the target shrinks to a few pixels zoomed out, and a click meant for a corner next to the first could finish the shape.
- **An offscreen layer per hatching:** simpler to read, but it costs a buffer the size of the view every frame.
- **Working out the lassos' previews on every pointer move:** filling a polygon costs rows × corners, which is wasted on frames that are never drawn.

## Consequences

- The overlay's cost grows with the visible cells and the outlines' edges. M10's profiling pass checks it on the owner's machines.
- The canvas reads the look from the application's resources, as the shell's metrics are read, and again only when the theme changes.
- The canvas repaints whenever the hatching has taken a step, which while anything is selected is nearly every frame (ADR 0062).

Updated 2026-11-20: after the owner's visual check, outlines got a third strength for the old selection's lines, the hatching moves and stays while a gesture would leave nothing, the grid shows less under it, the closing line is fainter, and a shape with no cells combines like any other.

Updated 2026-11-20 (step 4.2c): every hatching moves at the dash speed, in part-pixel steps with thicker lines (ADR 0062), and a preview follows an undo or redo made during the gesture.

Updated 2026-11-20 (step 4.3): the hatching is dark and light stripes side by side with no gaps, and Subtract's and Intersect's dashes are lighter than their hatching, in colours of their own (ADR 0064).
