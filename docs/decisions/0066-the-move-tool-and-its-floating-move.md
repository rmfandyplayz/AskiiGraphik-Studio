<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0066: The Move tool and its floating move

Status: Accepted

> Partly replaced by [ADR 0070](0070-each-move-tool-gesture-is-its-own-step.md): each drag or run of arrow presses is now its own Move step, an undo or redo of them doesn't end the move, Enter also deselects, and Esc puts back only a drag under way. The rest still stands.

## Context

Step 4.5 adds the Move tool on V. ARCHITECTURE §10 left the details for this step. Before it began, the owner chose:
- a move lasts across drags and nudges, and anything else ends it;
- Esc puts the cells back;
- empty cells are see-through;
- arrow keys nudge by 1 cell and Shift+arrow by 10;
- Alt+drag copies;
- with nothing selected, a drag moves the whole layer;
- a drag can start anywhere.

The owner also asked for the Hand tool to be the tool at launch, with Esc as its only key.

## Decision

- **The document always shows the move.** A `FloatingMove` takes the active layer's selected cells that hold something, or all of them when nothing is selected, as they were when the move began. Each new offset takes back the move's last step (`CommandHistory.CancelNewestStep`) and executes a new one built from those cells. So the cells pass over other art without harming it, and the whole move is one undo step named Move, which the renderer, saving and History already understand. There is no separate floating layer to draw or to forget to commit.
- **The step:** a `MoveCellsCommand`, which empties the cells where they were (not for a copy) and writes them where they land, plus a `SetSelectionCommand` for the moved outline when something was selected. Cells that land off the canvas aren't written, but come back if a later offset brings them on; when the move ends they're gone. Empty cells don't move, so they never cover anything.
- **It ends** when the history changes in a way the move didn't make, which covers an undo, another tool's edit and a selection change. It also ends when another layer becomes active, when Enter is pressed, or when another tool is chosen. A save doesn't change the history's counts, so a move carries on across a save. Esc while moving cancels the step, and the next Esc picks the Hand tool (ADR 0041).
- **Alt at the press** always starts a new move that copies, even during a move, so Alt-dragging again stamps another copy.
- **The Move tool writes outside the selection**, an exception to ADR 0065's clipping, since moving the selected cells out of the selection is its whole point. It does nothing on a locked or hidden layer.
- **Speed:** a whole 500 × 500 layer makes a new step for every cell the pointer reaches. `MoveCellsCommand` works on plain arrays and writes through `Layer.SetCellUnreported`, then reports the rect holding every changed cell once (`Layer.ReportChangedCells`). Reporting each cell cost about 280 ns, or about 350 ms per step; now a step takes about 3 ms.
- **Keys:** V picks the Move tool. The Hand tool has no letter, so H is free, and Esc still picks it. The Hand tool is also active at launch. `ToolKeymap` gives Shift+arrow the new `ToolKey.FarLeft` and its three siblings on every platform.

## Alternatives

- **A floating layer above the art,** committed at the end: one more thing for the renderer, saving, undo and every other tool to deal with, for the same result on screen.
- **Each offset added to one growing step:** undoing it would have to replay every offset, and a move back to the start would still leave a step.
- **Ending the move when the pointer is let go:** each drag would be its own step, and the cells would harm the art they pass over between drags.

## Consequences

- Undo, redo and History see one step per move, which shows as Move.
- 4.6's Paste can float its cells the same way, from cells it brings rather than ones it takes.
- The Tools toolbar has one more button. At 1920 logical pixels its text buttons need about 50 more than the toolbar gets, so it scrolls until the icon buttons arrive.
- Moving just the outline, without the cells, is parked.

Updated 2026-11-21: Paste floats its cells this way (ADR 0067); that step is named Paste rather than Move.

Updated 2026-11-22: since 4.8 (ADR 0069), every move sets the selection in its step, so moving the whole layer selects it; a click on an unselected cell deselects; and Shift, Ctrl and Ctrl+Shift keep a drag to a few directions.

Updated 2026-11-22: ADR 0070 makes each drag or run of arrow presses its own step, keeps the cells floating across undos and redos of them, and changes Enter and Esc. What the Decision and Consequences say about one step per move, an undo ending the move, and Esc no longer holds.

Updated 2026-11-22: since 4.9 (ADR 0071), the moved selection can reach into a zone past the canvas, where the move stops, and stays there when the move ends; only the cells off the canvas are lost.

Updated 2026-11-22: since 4.9b (ADR 0072), the move stops only where the selection would no longer touch the zone, so it can go wholly off the canvas. The floating move and the arrows now live in `CellMover`, which the selection tools' arrows use too (ADR 0073).
