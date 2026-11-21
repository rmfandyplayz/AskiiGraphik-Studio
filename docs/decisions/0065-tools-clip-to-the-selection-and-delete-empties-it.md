<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0065: Tools clip to the selection, and Delete empties it

Status: Accepted

## Context

ARCHITECTURE §10 says every drawing tool clips to the selection when there is one, and step 4.4 adds a Delete that empties the selected cells. Eight tools write cells today (the Brush, FG Brush, BG Brush, Character Brush, Eraser, Soft Eraser, BG Eraser and Text tool), and more come in M5 and plugins later, so each tool doing its own check would be easy to forget. The owner chose, before 4.4 began: the Text tool clips but its caret moves on; Delete and Backspace edit text while the Text tool's caret shows; Delete empties everything in a cell and keeps the selection; and Delete is greyed out with nothing selected or on a locked or hidden layer.

## Decision

- **One place clips:** `CellEditBuilder` takes an optional mask and leaves out any cell outside it. Tools get their builder from `EditingSession.EditCellsInSelection`, which passes the selection while something is selected. A tool doesn't know about clipping: what it would have written outside simply isn't recorded, so a stroke that only touches unselected cells makes no undo step. Commands that aren't tool edits, such as Merge Down and the sample documents, still use a plain builder.
- **Text:** characters typed outside the selection aren't written, but the caret moves on as if they were, and Backspace and Delete outside it erase nothing while the caret still moves. Typing stays predictable: the caret never jumps over or stops at the selection's edge.
- **Delete:** `SelectionContents.Clear` empties every selected cell of the active layer, character, colours and background alike, as one undo step named Delete, and leaves the selection as it was. It does nothing when nothing is selected or the active layer is locked or hidden, and makes no step when the cells are empty already.
- **Keys:** the Edit menu's Delete is an action with no shortcut. The Delete and Backspace keys reach it through the canvas view's key handler, only for keys the active tool didn't use. The Text tool uses them while its caret shows, so they keep editing text there, and in text boxes they never reach the canvas at all.

## Alternatives

- **Each tool checks the selection:** eight copies of one rule, and the next tool could forget it.
- **Clipping when the command runs:** the undo step would hold cells that never change, and a stroke outside the selection would still make a step.
- **Delete and Backspace as window-wide shortcuts:** they'd fire while typing in a text box (ADR 0057's problem), or would need every text box to stop them.
- **The text caret stopping at the selection's edge:** typing would act differently near a selection than anywhere else, which the owner turned down.

## Consequences

- A new tool that writes through `EditCellsInSelection` clips for free. One that makes its own `CellEditBuilder` doesn't, so tool code should always ask the session.
- 4.5's Move tool and 4.6's Cut can build on `SelectionContents` for taking the selected cells away.
