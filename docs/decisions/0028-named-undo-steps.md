<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0028: Named undo steps and the History panel

Status: Accepted

## Context

Step 2.2 shows undo steps by name, and clicking one jumps to that point using ordinary undo and redo. Commands had no names. Core holds no user-visible text (ARCHITECTURE §14.5), and one command type can mean different things: a cell edit might be typing or, later, erasing.

## Decision

- **Every command names itself:** `IDocumentCommand.StepNameKey` returns a key from `UndoStepNames`, and each key is an entry in the app's `Strings.resx`. A test checks that every key has text. The property is required, so a new command can't forget it.
- **Cell edits are named by whoever builds them:** `CellEditBuilder.Build(stepNameKey)`, with a generic default. A merged step keeps the first command's name, so a burst that starts with typing stays "Typing". A `CompositeCommand` takes a name, or its first command's.
- **`CommandHistory.JumpTo(undoCount)`** runs one ordinary undo or redo per step and reports the change once. The document sees the same changes as repeated undos, so nothing else has to know about jumps, and each step keeps its own change batch. Crossing a canvas resize needs no special handling.
- **`HistoryPanelViewModel`** lives as long as the app, follows the active document's history, and reuses its row objects. The panel's view can be rebuilt freely, which happens when it's reopened and on each new Mac window.
- **The list is a `ListBox`** whose selected index is two-way bound to the current row. It brings virtualization, a highlight and keeping the current row in view. Setting the row jumps there, and the -1 that a list reports while its rows change is ignored.

## Alternatives

- **Passing a name to `CommandHistory.Execute`:** every caller would have to supply one, and a command in a composite would have none.
- **Names as user-visible strings in Core:** Core would need the app's translations.
- **An enum of step names:** a future plugin's command couldn't add its own. A key can point at the plugin's own text.
- **Reverting several steps in one change batch:** slightly fewer change events, but a batch spanning a resize would have to describe regions in two canvas sizes.

## Consequences

- A jump across many steps raises one document change event per step. Painting waits for the next frame, so the cost is small, though a very long jump does that much compositing work.
- The panel rebuilds its row data on each history change, in time proportional to the number of steps. That's fine at realistic lengths; it's worth a look if very long histories ever feel slow while the panel is open.
- History isn't saved in files.
