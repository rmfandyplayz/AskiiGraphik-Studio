<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.2: History panel, manual test

**Build:** working tree
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 10 min

## What changed

The History panel now lists your undo steps by name, oldest first, below a "Start" row. The current step is highlighted, and undone steps are dimmed. Clicking a row jumps to that point, using ordinary undo and redo.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. If History isn't showing, choose Window → Reset Layout.

## Tests

### T1: Steps appear with names

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Start the app and look at History | One row, "Start", highlighted |
| 2 | Click the canvas and type a word | A "Typing" row appears and is highlighted |
| 3 | Wait a couple of seconds, then type another word | A second "Typing" row appears, because the pause ended the first burst |
| 4 | Choose Document → Resize Canvas and change the size | A "Resize Canvas" row appears |
| 5 | Choose Document → Font and pick another font | A "Change Font" row appears |
| 6 | Choose Document → ASCII Only, and confirm if asked | A "Change Character Set" row appears |

- [ ] Pass  - [ ] Fail. Notes:

### T2: The list follows undo and redo

**Also on macOS:** Yes, because undo uses Cmd there

| # | Do this | Expected result |
|---|---|---|
| 1 | Make a few steps as in T1, then press Ctrl+Z (Cmd+Z on a Mac) twice | The highlight moves up two rows, and the two rows below it are dimmed |
| 2 | Press Ctrl+Y (Cmd+Shift+Z on a Mac) | The highlight moves down one row, and that row is no longer dimmed |
| 3 | Use Edit → Undo and the toolbar's Undo and Redo | The list follows them the same way |
| 4 | Undo twice, then type something | The dimmed rows disappear, replaced by a new "Typing" row |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Clicking a row jumps there

**Also on macOS:** Yes, because focus differs there

| # | Do this | Expected result |
|---|---|---|
| 1 | Make at least four steps, then click an earlier row | The canvas goes back to how it looked after that step. Later rows are dimmed |
| 2 | Click a later, dimmed row | The canvas moves forward to that step |
| 3 | Click "Start" | The document is back to how it was before the first step |
| 4 | Click the last row, then type a letter without clicking the canvas | Everything is back, and the letter lands at the caret on the canvas |
| 5 | Jump across a Resize Canvas row, back and forward | The canvas size and its contents come back correctly |
| 6 | Press Ctrl+Z after a jump | It undoes one more step, from wherever you jumped to |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Saving and the unsaved mark

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Type something and save. The title has no `*` | |
| 2 | Type more | The title shows `*` |
| 3 | Click the row you saved at | The `*` goes away |
| 4 | Click a later row | The `*` comes back |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Wherever the panel is

**Also on macOS:** Yes, because floating panels use a different window setup there

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag History over the canvas and release, so it floats | The list is the same, with the same row highlighted |
| 2 | Type, undo and redo while it floats | The list follows along |
| 3 | Click a row in the floating panel | The canvas jumps there |
| 4 | Drag it back onto Layers as a tab, switch to the Layers tab, make a few edits, and switch back | The list shows those edits |
| 5 | Close History with Window → History, make some edits, and reopen it | The list shows those edits |

- [ ] Pass  - [ ] Fail. Notes:

### T6: A new or opened document

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make a few steps, then choose File → New and accept | The list is back to "Start" alone |
| 2 | Make a few steps, save, and open that file again | The list is back to "Start" alone. History isn't saved in the file |

- [ ] Pass  - [ ] Fail. Notes:

### T7: A long list

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make enough steps to fill the panel: type, then press an arrow key, about 40 times | A scrollbar appears in the panel, and the newest row stays in view |
| 2 | Press Ctrl+Z until the highlight moves off the top of the visible rows | The list scrolls to keep it in view |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Clicking rows very quickly, or holding Ctrl+Z while the list scrolls.
- Undoing or jumping in the middle of a typing burst, then carrying on typing.
- Jumping with the document zoomed in, and with a big canvas.

## Known limitations (not bugs)

- The panel's look is placeholder: a plain list.
- Layer rows such as "Add Layer" are ready, but won't appear until the Layers panel arrives in step 2.3.
- Backspace and Delete count as "Typing", since they merge into the same burst and undo as one step.
- When the history runs over its memory budget, the oldest rows disappear, and "Start" then means the oldest point you can still return to.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2.2, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
