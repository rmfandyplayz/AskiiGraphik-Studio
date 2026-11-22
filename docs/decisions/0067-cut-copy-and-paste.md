<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0067: Cut, Copy and Paste

Status: Accepted

## Context

Step 4.6 adds Cut, Copy and Paste to the Edit menu. ARCHITECTURE §10 left them to this step. Before it began, the owner chose: Copy and Cut take the active layer's selected cells and are greyed out with nothing selected; a paste lands centred on the pointer, or where it was copied from when the pointer isn't over the canvas or too little of it would land; it floats as a Move-tool move; spaces in other apps' text are empty cells; Ctrl+V at the Text tool's caret types the text in; characters the document can't hold leave their cells empty. From 4.5's testing, the Move tool's cursor should show over the cells it would move.

## Decision

- **A `CellClip` (Core) is what's copied:** the selection's bounding rectangle of cells, its shape, and where its corner was (none for text from another app). `CellClip.FittedTo` takes out characters the document's character set can't hold, keeping their backgrounds, and converts colours as switching the colour mode does.
- **The clipboard holds one item in two formats:** plain text for other apps (unselected and empty cells as spaces, this OS's line breaks), and `AskiiArt.Cells`, the app's own: versioned JSON with the origin, the shape as row runs, and the cells as a one-layer compressed document in Base64 (`CellClipFormat`), so the file format's code reads and checks them. Paste prefers the cells and falls back to the text (`PastedText`): any line break splits rows, a tab pads to the next multiple of 8 columns, escape codes and other control characters are dropped, spaces become empty cells. Pasting ANSI colours waits for M7.
- **Placement (`PastePlacement`):** the clip's middle goes on the pointer if a cell with something in it would land on the canvas; else where it was copied from, if that lands something; else the middle of the view. The canvas control reports the pointer's position in cells, and null when the pointer leaves it, which covers menus, part windows and other apps.
- **The paste floats** (`MoveTool.Paste`, `FloatingMove.Paste`): the Move tool is chosen and a move is started from the clip's cells, as a copy whose cells come from the clip, already selected. Drags and nudges carry it on, and the step is named Paste. Esc takes the paste away and puts the old selection back.
- **Cut** is Copy then `SelectionContents.Clear` as one step named Cut; the selection stays. Copy works on a locked layer but not on a hidden one; Cut and Paste need a layer that's neither.
- **At the Text tool's caret**, Paste types the clipboard's text from the caret, each line starting under the first, clipped to the selection like typing. Cells past the edges or with characters the document can't hold are skipped, keeping the text's shape. It's one Paste step of its own.
- **Copy as Text and Copy as ANSI** copy the selection's bounding rectangle while anything is selected, unselected cells as spaces; the whole canvas otherwise.
- **The move cursor:** `ITool.ShowsMoveCursorAt` (false by default) lets the Move tool ask for it over the selected cells, or anywhere on the canvas when nothing is selected, unless the layer is locked or hidden. It's the pan cursor's shape: the four-way arrows on Windows and Linux, the open hand on macOS, where Avalonia's four-way arrows are a crosshair.

## Alternatives

- **A paste on a new layer:** the owner chose floating on the active layer, which also needs no new layer machinery.
- **Our own text-like format for the cells:** a second encoding to write, read and validate, where the document format already does all three.
- **Greying out Paste when the clipboard is empty:** Avalonia raises no clipboard-changed event, so the menu would have to poll; Paste with nothing usable does nothing instead.

## Consequences

- Copying between two open windows of the app, or between documents, keeps colours and shape.
- A clip's JSON grows with the selection: the compressed cells plus a third for Base64.
- Paste at the pointer needs the pointer over the canvas; Edit → Paste from the menu bar always uses the fallback.

Updated 2026-11-22: since ADR 0070, a paste is a Paste step, and each later drag or run of arrow presses is a Move step of its own. Esc no longer takes a paste away; undoing the Paste step does.
