<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.3: Layers panel, manual test

**Build:** working tree
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 15 min

## What changed

The Layers panel lists your layers, top one first, with the active one highlighted. Typing goes to the active layer. You can add, duplicate, delete, rename, reorder, hide, lock and merge layers, from the panel's buttons or the new Layer menu. Every change is one undo step with its own name in History.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. If Layers isn't showing, choose Window → Reset Layout. Keep History open too: it shows each step's name.

Each row has two checkboxes: the first is **visible**, the second is **locked**. Hover over one to see which is which.

## Tests

### T1: Layers and the active layer

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Start the app | One row, "Layer 1", highlighted. Delete, Move Up, Move Down and Merge Down are greyed out |
| 2 | Click New | "Layer 2" appears above "Layer 1" and is highlighted. History shows "Add Layer" |
| 3 | Click the canvas at the top left and type `aaaa`, then click the "Layer 1" row | "Layer 1" is highlighted |
| 4 | Without clicking the canvas, type `bb` | `bb` appears right after `aaaa`. History shows a new "Typing" |
| 5 | Untick "Layer 2"'s first checkbox, then tick it again | `aaaa` disappears while `bb` stays, because `bb` went to "Layer 1". Then `aaaa` comes back |
| 6 | Click New while "Layer 1" is highlighted | "Layer 3" appears between the other two, and is highlighted |
| 7 | Click rows and type on the canvas a few times | Each row you click highlights, and the keyboard keeps typing on the canvas without another click |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Hiding and locking

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make two layers with different text in the same place, then untick the top layer's first checkbox | Its text disappears and the lower layer's shows. Its name turns grey. History shows "Show or Hide Layer". The highlighted row doesn't change |
| 2 | Click that row, then type on the canvas | Nothing is typed: a hidden layer can't be edited |
| 3 | Tick the checkbox again | The text is back |
| 4 | Tick a layer's second checkbox, click its row, then type on the canvas | Nothing is typed. History shows "Lock or Unlock Layer" |
| 5 | With the locked layer highlighted, look at the buttons | Delete and Merge Down are greyed out |
| 6 | Untick the lock | Typing works again |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Rename

**Also on macOS:** Yes, because text boxes and focus differ there

| # | Do this | Expected result |
|---|---|---|
| 1 | Double-click a row's name | A text box opens with the name selected |
| 2 | Type `Sky` and press Enter | The row says "Sky". History shows "Rename Layer" |
| 3 | Type a letter without clicking anything | It lands at the caret on the canvas |
| 4 | Click Rename, type `Sea`, and press Esc | The name stays "Sky", and typing goes back to the canvas |
| 5 | Choose Layer → Rename, type `Sea`, and click on the canvas | The row says "Sea" |
| 6 | Rename a layer to nothing, or only spaces, and press Enter | The name doesn't change |
| 7 | Close Layers with Window → Layers, then choose Layer → Rename | Nothing happens |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Reordering

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make two layers with different text in the same place, and highlight the top one | Move Up is greyed out |
| 2 | Click Move Down | The rows swap, the same layer stays highlighted, and the other layer's text now shows on top. History shows "Move Layer" |
| 3 | Click Move Up | They swap back |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Duplicate, delete and merge

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Type on a layer, then click Duplicate | A row named "… copy" appears just above it and is highlighted. The picture doesn't change. History shows "Duplicate Layer" |
| 2 | Type over the text on the copy, then hide the copy | The original's text is unchanged |
| 3 | Show the copy again and click Delete | The copy goes away, and the layer below it is highlighted. History shows "Delete Layer" |
| 4 | Make two layers with overlapping text, highlight the top one, and click Merge Down | One row is left, with the lower layer's name, highlighted. The picture doesn't change. History shows "Merge Down" |
| 5 | Try Merge Down with the upper or lower layer hidden or locked | It's greyed out |
| 6 | Try Delete with one layer left | It's greyed out |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Undo and redo

**Also on macOS:** Yes, because undo uses Cmd there

| # | Do this | Expected result |
|---|---|---|
| 1 | Do each of these in turn, and press Ctrl+Z (Cmd+Z on a Mac) after each: New, Duplicate, Delete, Rename, Move Down, hide, lock, Merge Down | Each one is undone completely, including the picture and the rows |
| 2 | Press Ctrl+Y (Cmd+Shift+Z on a Mac) after each undo | It comes back |
| 3 | Merge two layers, then undo | Both layers are back with their own text |
| 4 | Click an earlier row in History, then a later one | The Layers panel follows the jump in both directions |
| 5 | Add a layer, type on it, then click "Start" in History | Every added layer is gone, leaving the one you started with, highlighted |
| 6 | Click the last row in History | All the layers and their text are back |

- [ ] Pass  - [ ] Fail. Notes:

### T7: Saving and reopening

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make three layers with text, rename one, hide one and lock one, reorder them, then save | The title has no `*` |
| 2 | Hide or rename a layer | The title shows `*` |
| 3 | Undo that, then open the saved file again | The layers come back in the same order, with the same names, text, hidden and locked states. The top one is highlighted |
| 4 | Choose File → Export as Text and look at the file, or use Edit → Copy as Text | Only the visible layers' text is there, with upper layers winning |

- [ ] Pass  - [ ] Fail. Notes:

### T8: Wherever the panel is

**Also on macOS:** Yes, because floating panels use a different window setup there

| # | Do this | Expected result |
|---|---|---|
| 1 | Float Layers by dragging it over the canvas and releasing | The same rows show, with the same one highlighted |
| 2 | Click rows, tick checkboxes and use the buttons while it floats | They all work, and typing stays with the canvas |
| 3 | Close Layers with Window → Layers, use the Layer menu's New, and reopen it | The new layer is in the list |
| 4 | Choose File → New and accept | One row again |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Starting a rename, then undoing or clicking another row.
- Many layers, enough to scroll the list.
- Merging on a big canvas (Debug → Open Sample Document).
- A very long layer name.

## Known limitations (not bugs)

- The panel's look is placeholder: plain checkboxes and text buttons that wrap to several rows.
- Reordering is by buttons. Dragging rows would be a design choice for later.
- The Layer menu's items have no shortcuts yet.
- Which layer is active isn't saved or undone. Undoing a delete doesn't reselect the restored layer.
- Canvas-wide tools and the space option arrive in step 2.4, so layers only come alive through typing for now.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2.3, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
