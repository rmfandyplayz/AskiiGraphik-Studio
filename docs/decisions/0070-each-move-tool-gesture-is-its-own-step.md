<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0070: Each Move-tool gesture is its own undo step

Status: Accepted

## Context

In ADR 0066 a whole Move-tool move was one undo step, ended by whatever else the user did next. After testing 4.8, the owner found it odd that some other action decided where a move's step ended. They asked for the usual rule:
- each drag is its own Move step;
- a run of arrow presses is one step, ended by a pause of 0.4 s or by a turn after more than two presses one way, so pressing up and right in turn to go diagonally is one step and two presses count as a slip;
- each Shift+arrow is a step of its own;
- Enter completes the move and deselects;
- all of this applies to pastes too.

They agreed to measure the 0.4 s from when the last arrow key was let go, so holding a key down stays one step. They also accepted my proposal that Esc puts back only a drag under way.

## Decision

- **The cells still float** (ADR 0066), across the move's own steps and undos and redos of them. `FloatingMove` keeps one state for each of its steps: where the cells are, what lies under them there, and the history's newest step while the document shows that state. Each gesture executes a Move step from the state where it began, and takes it back and builds it again as it goes. Its `MoveCellsCommand` lifts the cells from where that state has them, putting back what lay under them, and lands them.
- **Following the history:** after any change the move didn't make, `FloatingMove.FollowHistory` looks for the history's newest step (`CommandHistory.NewestStep`) among its states. An undo or redo of the move's own steps lands on one of them, and the move carries on from there. Anything else lands on none, and the move ends where its cells are. A save changes no step, so it ends nothing, not even a run of presses.
- **Arrow runs** (`NudgeRun`): a press carries the run on unless it comes 0.4 s or more after the last arrow was let go while none is held, or it turns after three or more presses one way. The canvas reports arrow key-ups to tools (`ITool.ArrowReleased`). A drag, Shift+arrow, Enter, an undo or redo, or another tool ends the run.
- **Enter** ends the move and deselects, as a Deselect step; with no move, it just deselects. Undoing it brings the selection back, but the cells no longer float.
- **Esc** during a drag puts the cells back where that drag found them, leaving no step. Any other Esc isn't the Move tool's, so the App picks the Hand tool (ADR 0041) and the cells stay where they are.
- **A paste** is a Paste step, and each later drag or run is a Move step. Undoing the Paste step takes the paste away.

## Alternatives

- **Keep the whole move as one step and only split it on time:** two drags in quick succession would still merge, which is what the owner found odd.
- **Steps that carry the whole move from where it began:** undoing one would have to redo the ones before it, and the steps would stop making sense once the history drops old ones under its memory budget. Each step here only needs the state before it.
- **Let undo end the float:** an undo and a new drag would harm the art the cells had passed over.
- **Esc puts back the whole move, as before:** it would take back several steps at once, so one key would undo more than the user can see in History.

## Consequences

- History shows one Move step per drag or run, so a long move takes several undos to take back.
- The 0.4 s and the three presses are guesses (TEMPORARY-UI).
- In the App the pause is timed with the system clock, so the one App test of it waits a real 0.45 s.

Updated 2026-11-22: since 4.9b (ADR 0073), the runs of arrow presses live in `CellMover`, and the selection tools' arrows make the same steps.
