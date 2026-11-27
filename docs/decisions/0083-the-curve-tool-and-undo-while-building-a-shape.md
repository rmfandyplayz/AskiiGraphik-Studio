<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0083: The Curve tool, and Undo while building a shape

Status: Accepted

## Context

Step 5.5 adds the Curve tool. The owner asked for it to work like Illustrator's pen tool and look like the Polygonal Lasso, with an anchor's handles shown faintly unless it's the selected anchor or under the pointer. In the planning the owner also agreed:

- how clicks, drags, Alt and Shift place and edit anchors;
- how the curve finishes and is thrown away;
- that Ctrl+Z (Cmd+Z) while drawing takes the last anchor away, as Backspace does, in the Polygonal Lasso too.

Until now, Undo always went straight to the history, which no tool could intercept.

## Decision

- **Placement.**
  - The Curve is the Shapes group's second tool, so Rectangle and Ellipse move to 3 and 4.
  - A click places a **smooth** anchor in the middle of the cell. Its handles follow its neighbours as a Catmull-Rom spline's do: a sixth of the way from the anchor before to the one after, and recomputed as anchors are added or moved. An open curve's ends count themselves as their missing neighbour.
  - Dragging from the press, once the pointer leaves the marker's reach, pulls out the new anchor's handles, **mirrored**. Alt+click places a corner (no handles), and Alt during any handle drag **unlinks** the pair for good.
  - Shift keeps a new anchor in line with the last, using the Line tool's constraints (`MoveConstraint`, Ctrl included). Shift keeps a dragged handle to 45° steps as they look on screen. Anchors sit on cell middles; handle ends go anywhere.
- **Editing before it's finished.**
  - A press takes hold of what's under it, in this order: an anchor's marker, a handle's end, an anchor's cell.
  - Dragging an anchor moves it with its handles. Dragging a handle reshapes the curve, and the other handle stays its mirror image unless Alt unlinks them. A smooth anchor whose handle is dragged becomes mirrored.
  - The selected anchor is the newest, or the one last pressed.
- **Finishing.**
  - Enter or a double-click finishes the curve (the double-click's first click places its anchor). A click on the first anchor closes the curve, once it has two anchors.
  - Switching tools keeps the curve as it is. Esc throws it away. Backspace takes the last anchor away; taking away the only one throws the curve away.
  - A curve of one anchor draws nothing.
- **Drawing.**
  - Each piece between anchors is a cubic Bézier (`CurveShape`), flattened into short straight pieces for `LinePath`, so the curve takes cells, characters, Include joins and the selection's clipping exactly as the Line tool does.
  - The curve is real cells while it's built (`LiveShape`), so a finished curve is one step. A piece of curve follows the pointer to where a click would place the next anchor, unless the pointer is on an anchor.
  - The options and colours are read at each redraw, so changing the Style mid-curve shows at once.
- **The overlay.** `ToolOverlay.Handles` lists `ToolHandle`s, each strong or faint. The canvas draws each as a line to a small square, at theme-token opacities (0.9 and 0.2). Anchors use the Polygonal Lasso's markers; the first is the finishing one once the curve can close.
- **Undo while building a shape.** `ITool.TakesUndo` (default false) and `ITool.Undo()`. While a tool takes Undo, the Edit menu's Undo calls the tool, Undo is enabled even with nothing in the history, and Redo is disabled. `EditActions` follows the active tool's `OverlayChanged`, since taking Undo changes only along with the overlay. The Curve and the Polygonal Lasso take Undo while they draw. The History window still changes the history directly. A curve whose step that undoes ends where it is.

## Alternatives

- **Undo as a `ToolKey`:** the shortcut is the action system's and rebindable in M8, and the menu item wouldn't reach the tool.
- **Centripetal Catmull-Rom:** it overshoots less when anchors are unevenly spaced, but it's more involved for a default the owner can reshape by dragging.
- **Handles at a third of the way to each neighbour:** rounder at first, but it bulges between close anchors.

## Consequences

- The newest smooth anchor's handles shift as the pointer moves, since the piece to the pointer counts as its next neighbour.
- The Table and Line brush steps can use `CurveShape` and `ToolHandle` if they need curves.
