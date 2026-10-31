<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0012: Commands and undo history

Status: Accepted

## Context

ARCHITECTURE §9 asks for undo through commands that store only what they touch, one command per gesture, coalescing of typing, composite commands, a history memory budget (no step limit) and dirty state from the saved position. The code shape was left open.

## Decision

- **`IDocumentCommand`** has `Apply(Document)`, `Revert(Document)` and `EstimatedMemoryInBytes`. Commands are data, not bound to a document. Some capture their "before" state in `Apply` (old index, old properties, cropped cells), so `Revert` is only valid right after `Apply`; the history guarantees that. `Apply` validates before changing anything.
- **`CommandHistory`** runs every command. Each execute, undo and redo runs inside `Document.BeginChanges()`, so one step raises one `Document.Changed` event. It raises `StateChanged` for the UI.
- **Coalescing:** `Execute(command, group)` takes an optional `CoalescingGroup`, compared by identity. A command merges into the previous step only if both have the same group, the previous step is still the newest, and its command is an `IMergeableCommand` that accepts. Undo, redo, `MarkSaved` and any other command stop merging. The caller starts a new group when the caret jumps, the tool changes or the user pauses; that keeps clocks out of Core.
- **Memory budget:** default 256 MB, set in the constructor. After each execute, the oldest steps are dropped until the total fits, but **the newest step is always kept**, even when it alone is over budget. Each command's estimate is read once, after it was applied.
- **Unsaved changes:** the history remembers the position of the last save. `HasUnsavedChanges` is true when the current position differs. If the saved step is discarded (by executing after undo, or by the budget), the saved state is unreachable and the document stays unsaved until the next save.
- **Cell edits:** `CellEditBuilder` collects a tool's changes on one layer, reading each cell's "before" from the layer the first time it's set. `Build()` drops cells set to their current value and returns null when nothing changes. A `CellEditCommand` holds one layer id plus (index, before, after) per cell. Merging keeps each cell's first "before" and last "after"; a merge that cancels out leaves an empty, but still undoable, step.
- **Commands don't check layer locks.** Tools respect locks (§8.1); undo must always be able to restore cells.
- **Other commands:** `CompositeCommand` (applies in order, reverts in reverse, and reverts what it already applied if a part throws), `ResizeCanvasCommand` (stores only the occupied cropped cells), `AddLayerCommand` (the layer id is chosen at construction so redo and later commands agree on it), `RemoveLayerCommand` (keeps the removed layer object; refuses a frame's last layer, ADR 0010), `MoveLayerCommand`, `SetLayerPropertiesCommand`.
- **Property test:** CsCheck generates random command sequences (including coalesced and composite ones) and checks that undo passes back through every earlier state to the original, and redo returns to the end.

## Alternatives

- **Commands bound to a document at construction.** One less parameter, but a command could then be run against the wrong document.
- **The history deciding when typing pauses.** Needs a clock in Core, and only the text tool knows about caret jumps anyway.
- **A fixed number of undo steps.** ARCHITECTURE asks for a memory budget instead, since one resize can cost more than a thousand keystrokes.
- **Dropping the newest step when it alone is over budget.** The user couldn't undo what they just did.

## Consequences

- `Apply` and `Revert` are public so commands can be composed and tested, which means App code could call them directly and skip the history. It must not: App code always goes through `CommandHistory.Execute`.
- The text tool (1.5) creates a new `CoalescingGroup` at each caret jump, pause or tool change.
- The history panel (M2) will need a display name per command; that's added to `IDocumentCommand` then.
- Paste (M4) writes cells on a layer that doesn't exist yet, so it will need a way to make a `CellEditCommand` without reading "before" from the document.
