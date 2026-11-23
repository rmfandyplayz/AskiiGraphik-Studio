<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0073: Arrows in the selection tools

Status: Accepted

## Context

After 4.9 the owner asked that the arrow keys move the selected cells in every selection tool, just as they do in the Move tool, without switching to it, so a selection can be nudged straight after it's made.

## Decision

- **One mover.** The Move tool's floating move and arrow handling moved out into `CellMover` (Core, `Tools/Moving`): it owns the `FloatingMove`, the run of arrow presses (`NudgeRun`), following the history and the active layer, pastes, and Enter's "end the move and deselect". The Move tool keeps only its drags on top, and a `GestureInterrupted` event tells it when an undo, redo or the move ending cuts a drag short.
- **Selection tools.** Rectangle and Ellipse Select (through `BoxSelectGesture`), the Lasso, the Polygonal Lasso and the Magic Wand each hold a `SelectionArrows`, which wraps a `CellMover`:
  - with something selected and no gesture under way, the arrows and Shift+arrows move the selected cells exactly as the Move tool's do: one cell or ten, runs of presses as one Move step, the cells floating across undos and redos of those steps;
  - with nothing selected they do nothing (the Move tool would move the whole layer);
  - during a drag, while the Polygonal Lasso has corners down, or while the Magic Wand's button is held, they're swallowed and do nothing;
  - Enter ends a move they made and deselects; with no such move, Enter does nothing in a selection tool;
  - a selection gesture starting, another tool, another layer or any other change ends the move where its cells are;
  - on a locked or hidden layer they do nothing, as in the Move tool.
- The tool stays as it is: the Move tool isn't picked.

## Alternatives

- **Pick the Move tool when an arrow is pressed:** simpler, but the owner wants to stay in the selection tool.
- **A copy of the Move tool's arrow code in each selection tool:** five copies of the history-following rules to keep in step.

## Consequences

- Each selection tool now follows the history and the active layer for as long as it's the tool, as the Move tool does.
- Enter doing nothing with no move under way is a guess (TEMPORARY-UI).
