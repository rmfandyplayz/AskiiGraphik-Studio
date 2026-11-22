<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0069: Move tool clicks, constraints and whole-layer selection

Status: Accepted

## Context

After testing 4.6, the owner asked for four follow-ups (step 4.8). A click with the Move tool on an unselected cell should deselect, when the button comes up, as Rectangle Select's click does, while a drag from there still moves the selection. During a drag:
- Shift keeps the move to left and right or up and down, whichever the pointer has gone further;
- Ctrl (Cmd on a Mac) allows those and diagonals that look like 45°;
- Ctrl+Shift allows those and diagonals of one column per row.

These are measured from where that drag began, and change the moment the keys do. Moving the whole layer should select it. In the Layers window's rename box, arrow keys the box can't use should do nothing rather than end the rename.

## Decision

- **A click deselects** when the press was on an unselected cell while something was selected, and the pointer never left that cell. When the button comes up, the move under way ends where its cells are, and a Deselect step follows, as Rectangle Select's click makes. Like the rest of the Move tool, it does nothing on a locked or hidden layer.
- **`MoveConstraint` keeps a drag to a few directions.** It takes the drag's distance in whole cells, as an unconstrained drag does. The direction is whichever allowed one the drag is nearest as it looks on screen, using the cell's width-to-height ratio (ADR 0047). Each direction takes the angles nearer it than the next one.
  - Along an axis, the cells move as far as the pointer has gone along it.
  - On a diagonal that looks like 45°, they move by the average of how far the pointer has gone across and down on screen. The side whose cells are bigger on screen is rounded first, and the other follows, so the cells stay as near the line as whole cells can.
  - On a diagonal of one column per row, they move by the pointer's distance along that diagonal on screen, rounded to whole steps.
- **The canvas reports Ctrl, or Cmd on macOS, going down or up** through `ITool.ModifiersChanged`, as it already did for Shift and Alt (ADR 0060). The Move tool then moves the cells at once, from the pointer's last cell.
- **Every move sets the selection** in its step. With nothing selected, the selection is the whole canvas, moved with the cells and cut to the canvas. So moving the whole layer selects it as the cells first move, and Undo or Esc takes the selection away again. A drag on an empty layer selects too, so it shows where the layer went.
- **The rename box is a `LayerNameBox`,** a `TextBox` that marks any arrow key handled after the box has had its turn. An arrow the box used has moved the caret. One it couldn't use no longer goes on to the list, which would have picked another row and ended the rename.

## Alternatives

- **Choosing Shift's axis in cells rather than on screen:** columns are about half as wide as rows are tall, so a drag that looks mostly downward would often go sideways.
- **Measuring from the pointer's exact position rather than whole cells:** no visible difference, and constrained drags would then differ from unconstrained ones near cell edges.
- **Selecting at the press rather than when the cells first move:** a click would leave a step.
- **Catching arrows on the list before the box sees them:** that needs the box's own rules about when it can use an arrow, such as the caret's place, the selection and modifiers.

## Consequences

- The Move tool's clicks now behave like Rectangle Select's, so clicking away from a selection with either tool clears it.
- Tools that follow modifiers during a gesture now also hear about Ctrl and Cmd. Rectangle and Ellipse Select ignore them.
- After moving the whole layer, the next drag moves just what's selected, which is the whole layer where it now is.
