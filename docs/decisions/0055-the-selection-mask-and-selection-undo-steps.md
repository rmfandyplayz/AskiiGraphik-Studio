<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0055: The selection mask and selection undo steps

Status: Accepted

## Context

Step 4.1 starts M4 (Selection) with the mask, Rectangle Select, Select All and Deselect. ARCHITECTURE §10 asked for a mask the size of the canvas in the editing session, and §23 asked whether selection changes are undo steps. The owner (2026-11-19) chose: selection changes are undo steps, but they don't make the document unsaved, since selections aren't saved; the shape selection tools share one group on M and the Magic Wand gets W; the Move tool takes V in 4.5, when the Hand tool moves to H and Esc keeps going to the Hand tool; and Resize Canvas moves the selection with the art.

## Decision

- **`SelectionMask` is immutable.** It knows its canvas size and the tight bounds of its cells, and stores bits (64 cells per `ulong`) only for those bounds, or none at all when every cell in them is selected. So Select All on a 4000 × 4000 canvas costs a few bytes, undo steps can share masks, and two masks with the same cells are equal. `SelectionMaskBuilder` makes any other shape. `Edges` (cached) is the outline as runs of `CellEdge`, found with whole-word bit operations; `CellEdge` moved from the brush footprint into `Core/Documents` so both share it.
- **The session owns the selection.** `EditingSession.Selection` is a `SessionSelection`: the current mask and a `Changed` event. It isn't part of the document and isn't saved. New starts with nothing selected.
- **Selection steps don't dirty the document.** `IDocumentCommand.ChangesDocument` defaults to true; `SetSelectionCommand` says false, and a `CompositeCommand` says true if any of its commands does. `CommandHistory.HasUnsavedChanges` is true when some step between the saved position and now changes the document. When steps after a save are dropped (by executing after undo, or by the memory budget) and they only changed the selection, the saved state is still reachable.
- **`SelectionEdits`** makes each change one step (`Select`, `SelectAll`, `Deselect`), and makes none when the selection wouldn't change.
- **Resize Canvas** is `EditingSession.ResizeCanvas`: one composite step of the resize and the selection moved by the same offset, cropped to the new canvas. Undo restores both.
- **Rectangle Select** (`askiiart.select.rectangle`, group M) drags from cell to cell, both included and kept to the canvas, replacing the selection on release as one step. A click without reaching another cell deselects. While dragging, the tool's overlay carries the pending mask, which the canvas outlines instead of the selection. Esc cancels a drag; any other Esc switches to the Hand tool (ADR 0041). It works on locked and hidden layers. Modifiers are ignored until combine modes arrive (4.2).
- **The outline** is drawn after the grid, as strips inside the selected cells, in black dashes on white counted from the canvas corner so they don't crawl when the view scrolls. It doesn't animate, so an idle canvas stays idle. Only edges in view are drawn.

## Alternatives

- **A bitset the size of the canvas** (§10's wording): 2 MB per 4000 × 4000 mask, and each undo step would need its own or a diff.
- **Selection changes outside undo:** simpler, but the owner wants them in History, as in image editors.
- **Selection steps that dirty the document:** closing would ask to save a document whose content never changed.
- **Clearing the selection on resize:** losing it would surprise, and undo would have to bring it back anyway.
- **Marching ants:** a timer would keep the canvas redrawing; the look is the owner's to design later.

## Consequences

- Every later selection tool and operation (4.2–4.6) only builds a mask and calls `SelectionEdits.Select`.
- The outline is a flat list of edges walked each frame; a very ragged Magic Wand selection (4.3) may need measuring.
- On macOS, Cmd+A goes through the menu bar; whether it still reaches a focused text box is checked by hand (ADR 0056).

Updated 2026-11-20: at the owner's request the outline's dashes now run clockwise round the selection (marching ants), at a speed set by a theme token. They move a whole device pixel at a time: while there's an outline, the canvas asks for animation frames and repaints only when the dashes have moved, and with no selection it goes idle again. Cmd+A in a text box is now handled as ADR 0057 says.
