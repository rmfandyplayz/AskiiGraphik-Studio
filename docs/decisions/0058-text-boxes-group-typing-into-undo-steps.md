<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0058: Text boxes group typing into undo steps

Status: Accepted

## Context

Avalonia's `TextBox` keeps its own undo: one state per change, and only ten of them (`UndoLimit`), so Cmd+Z took typing away one character at a time and stopped after nine. After testing step 4.1 the owner (2026-11-20) asked for steps split by a pause of 0.8 to 1.2 seconds, and asked whether matching the pause to typing speed was worth it.

## Decision

- A style in App.axaml turns Avalonia's undo off (`IsUndoEnabled="False"`) and the attached `TypingUndo` behaviour on, for every text box.
- `TypingHistory` keeps each step as the text and selection before and after (`TextBoxState`). A change carries on the current step when it follows on from the last one (typing just after it, or Backspace or Delete next to it: `TextChange.FollowsOn`) within 1 second. A longer pause, moving the caret, switching between typing and deleting, and any other change (a paste, a cut, typing over a selection) start a new step.
- Undo brings the old text back with the restored characters selected; Redo puts the caret after the change.
- Each text box keeps up to 1000 steps and drops the oldest first.
- Only changes made while the box has the keyboard and can be edited are steps. Text the app puts in the box (another ramp chosen, say) starts its history afresh.
- The clock is a `TimeProvider`, so tests control the pause.

## Alternatives

- **Raise `UndoLimit` and keep Avalonia's undo:** still a step per character.
- **A pause that follows typing speed:** one Undo would take away different amounts for no visible reason, and speed changes within a burst. Text editors use fixed rules like these.
- **The canvas Text tool's 2-second pause (ADR 0017):** the owner asked for about a second here, and then for the Text tool to use 1 second too, so the two match.

## Consequences

- The history belongs to the text box, so a window that's closed and opened again starts with none.
- Typing anywhere ends an undo step after the same 1-second pause.
- Typing over a selection starts a step, and the typing straight after it carries it on, so one Undo brings the selection back (changed after the owner's retest: the replacement was a step of its own).
