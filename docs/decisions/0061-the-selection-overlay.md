<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0061: The selection overlay

Status: Accepted

## Context

After testing step 4.2 the owner asked for the selection to show the way painting apps show it: hatching over what isn't selected, a live preview of every selection tool's result, Subtract and Intersect in their own colours, a faint line showing how a lasso would close, and markers on the Polygonal Lasso's corners. That became step 4.2b, and the owner settled its details on 2026-11-20.

## Decision

- **What the canvas shows** is a `SelectionDisplay` (App), made from the session's selection and, during a gesture, the tool's `SelectionPreview` (Core): the selection before, the mode and the shape, with the result and the cells it would deselect worked out when first asked for. A preview always shows what letting go now would give, by the same rule `SelectionCombiner` finishes with.
  - **Replace:** the result, with a running outline.
  - **Add:** the old selection's and the shape's outlines faint, under the result's running outline, so lines that end up inside it fade.
  - **Subtract:** the old selection's outline faint, a red outline running round the shape, and the cells it would deselect hatched in red.
  - **Intersect:** a faint blue outline round the shape, a blue outline running round what stays, and the cells it would deselect hatched in blue. Blue rather than green, so it never reads as red to people with red-green colour blindness.
  - Only bright outlines run. Faint ones stay still, in faded colours.
- **Hatching:** every cell outside the result, and outside the cells being deselected, gets thin lines rising to the right, a dark one beside a light one so they show on any art. Cells being deselected get lines falling the other way, in the mode's colours. There's no hatching while nothing would be selected. The lines keep their size on screen at every zoom, and repeat from the canvas's corner, so they stay put on the art while it scrolls, without lining up with the cells.
- **Drawing it:** like the cell backgrounds (ADR 0038), each hatching is an alpha image with a pixel per visible cell, filled from the masks a run of cells at a time (`SelectionMask.RunsInRow`), plus a small tile of lines. The painter draws the tile through the cell image with one blended shader, so the cost follows the visible cells, not the size of the selection.
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
