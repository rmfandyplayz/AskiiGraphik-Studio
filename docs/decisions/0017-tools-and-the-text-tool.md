<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0017: Tools and the text tool

Status: Accepted

## Context

Step 1.5 adds the first tool. ARCHITECTURE §8.1 describes tools as Core classes that get input in cell terms and change the document only through commands, and §9 says a burst of typing coalesces into one undo step. Space + drag pans the canvas (ADR 0015), which clashes with typing a space. The owner's direction: the app will have a "draw mode" (brushes, Photoshop-style) and a "text mode", where text mode is just another tool; while it's active, Space types a space and Space + drag doesn't pan.

## Decision

- **`EditingSession`** (Core) holds what tools edit through: the document, its `CommandHistory`, the active layer (the top layer until layers have a UI in M2) and a `TimeProvider` clock, so tests control time.
- **`ITool`** gets pointer input as `ToolPointerInput` (cell, position within the cell, modifiers, pressure), keys as `ToolKey`, and typed text as strings. It exposes a `ToolOverlay` (for now just the caret) that the canvas draws on top, and `AllowsSpacePanning`. Tools are created per session from a `ToolDefinition` in the `ToolRegistry`, registered through DI. The canvas control translates Avalonia input and gives the tool whatever it doesn't use for zooming and panning.
- **Space:** when the active tool doesn't allow Space panning, the canvas leaves Space unhandled, so it arrives as typed text (on macOS a handled key never becomes text). Middle-drag still pans.
- **The text tool** (`askiiart.text`), the only tool until M3, so always active. Its behaviour is a temporary choice until the owner specifies it (§23):
  - A click places the caret on a cell. Typing **overwrites** that cell and moves right; nothing shifts sideways, since that would wreck the art beside the text. At the right edge the caret stays in the last column.
  - Backspace after typing empties the cell before the caret and moves into it. When the caret was put on its cell instead (a click, an arrow key, Home, Enter), at the left edge, or stuck at the right edge after typing, Backspace first empties the caret's own cell. Delete empties the caret's cell. Both make cells **empty**, not spaces (§5.4), and do nothing on a locked layer.
  - Enter moves to the next row, under the start of the current **block** of text. A block starts at a click; typing with the caret moved by the keys (but still on the block's rows, between its start and just past its right end) continues it, and typing or Enter anywhere else starts a new block at the caret. Arrows move one cell; Home goes to the first column, End to just past the row's last glyph. Esc removes the caret.
  - Each keystroke is a command; a burst shares a `CoalescingGroup`. A burst ends when the caret moves by any means but typing, or after a 2-second pause.
- **Undo and Redo** are actions (Cmd+Z; Cmd+Shift+Z and Cmd+Y) in a new Edit menu.

## Alternatives

- **Space pans unless a caret is placed:** keeps Space + drag alive now, but makes Space mean two things inside one tool. The owner chose a rule per tool.
- **Insert mode** (typing shifts the row right): normal in text editors, destructive in a drawing.
- **One command per burst, committed when it ends:** the history would have to wait on the tool, and undo during a burst would be ambiguous. Coalescing gives the same undo steps.

## Consequences

- Space + drag stays in the canvas, used again once a drawing tool (M3) allows it.
- `ToolDescriptor` has no shortcut yet. Single-key tool shortcuts arrive with the second tool, and must not fire while the text tool is typing.
- There's no IME composition support yet (Chinese, Japanese and Korean input methods); typed text comes from Avalonia's plain text input.

Updated 2026-10-31: from the owner's 1.5 testing, Backspace erases a placed caret's own cell first, and Enter returns to the start of the current text block instead of the last clicked column.
