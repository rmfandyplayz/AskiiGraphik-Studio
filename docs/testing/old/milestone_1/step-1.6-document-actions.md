<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 1.6: Document actions, manual test

**Build:** working tree (not pushed yet)
**Test on:** - [ ] Windows  - [ ] macOS  (Linux is covered by CI only)
**Time needed:** about 30 min

## What changed

You can start a new document (**File → New**), resize the canvas around an anchor (**Document → Resize Canvas**) and change the document's font (**Document → Font**), including fonts installed on your computer and font files. A plain toolbar under the menus has buttons for these and for undo and zoom. **ASCII Only** moved to the Document menu. The caret can now jump several cells at a time.

It also has the fixes from your 1.5 testing: zooming out no longer drifts to the bottom right, Backspace erases the cell you put the caret on, and Enter follows the text you're typing.

Shortcuts below say **Ctrl** and **Alt**. On a Mac, see the macOS column or use **Cmd** for Ctrl.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. The app still opens with the 500 × 500 sample document.

## Tests

### T1: New

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **Ctrl+N** (or click **New…** on the toolbar) | The New Document dialog opens, with the cursor in Width. Width is 80, Height 25, and **ASCII only** is selected. |
| 2 | Click **OK** | The canvas becomes an empty 80 × 25 document. The status bar says `80 × 25 cells`, `ASCII only` and `JetBrains Mono`. Undo is greyed out. |
| 3 | Click a cell and type `é` then `a` | Only `a` is typed. |
| 4 | Press Ctrl+N, type 600 in Height | An orange warning appears: canvases bigger than 500 × 500 can use a lot of memory. OK still works. |
| 5 | Clear the Width box | OK is greyed out. Type 120 again: OK works again. |
| 6 | Choose **Unicode**, then OK | The status bar says `Unicode`, and typing `é` works. |
| 7 | Press Ctrl+N, then **Cancel** (or Esc) | Nothing changes. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Resize Canvas

| # | Do this | Expected result |
|---|---|---|
| 1 | Make a new 20 × 10 document (Unicode or ASCII) and type `abc` near the top left | |
| 2 | Choose **Document → Resize Canvas…** | The dialog shows 20 and 10, and the centre arrow (•) of the anchor grid is selected. Hovering an arrow shows its name. |
| 3 | Set Width to 30, pick the **top-left** arrow (↖), OK | The canvas is 30 × 10, with the new columns on the right; `abc` didn't move. |
| 4 | Press Ctrl+Z | The canvas is 20 × 10 again. |
| 5 | Resize to 30 × 10 with the **right** arrow (→) | The new columns are on the left: `abc` moved 10 cells right. |
| 6 | Open Resize Canvas, set Width to 5 | An orange note says cells outside the new size will be removed. Click OK with the **left** arrow (←): only the leftmost 5 columns are kept. |
| 7 | Press Ctrl+Z | The canvas and everything cut off come back. |
| 8 | Open Resize Canvas and click OK without changing anything | Nothing happens, and there's no new undo step. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Font

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Document → Font…** | The font picker opens with JetBrains Mono selected. The four bundled fonts come first, marked "(comes with the app)", then your installed fonts. |
| 2 | Type `mono` in the search box | Only fonts with "mono" in the name are listed. |
| 3 | Pick **Fira Code (comes with the app)**, OK | The canvas redraws in Fira Code; the cells change size to match. The status bar says `Fira Code`. |
| 4 | Press Ctrl+Z | Back to JetBrains Mono. Ctrl+Y brings Fira Code back. |
| 5 | Open the picker, search for and select a proportional font such as **Arial** (Windows) or **Helvetica** (macOS) | An orange warning says its characters aren't all the same width. OK still works, and the grid keeps one width per cell. |
| 6 | Pick an installed monospaced font such as **Consolas** (Windows) or **Menlo** (macOS) | No warning. OK draws the canvas in it. |
| 7 | Click **Load from File…** and pick a `.ttf` or `.otf` file that isn't installed | The font is added, marked "(from a file)", and selected. OK uses it. |
| 8 | Load a file of a font that's already in the list (for example one of the bundled fonts, from `src/AskiiArt.Imaging/Fonts/Bundled/`) | A note says a font with that name is already available and that one is used; it's selected. |
| 9 | Load a file that isn't a font (rename a `.txt` to `.ttf`) | A message says the file couldn't be loaded. Nothing else changes. |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Menus and toolbar

| # | Do this | Expected result |
|---|---|---|
| 1 | Look at the menu bar | Windows: File (New, Quit), Edit (Undo, Redo), Document (Resize Canvas, Font, ASCII Only), View, Help. macOS: the same, but File has only New, and Quit is in the app menu. |
| 2 | Click toolbar buttons: Zoom In, Zoom Out, Actual Size | They zoom like the shortcuts. |
| 3 | Click **Show Grid** on the toolbar, then use **View → Show Grid** | The toolbar button stays pressed while the grid is on, and the menu's check mark agrees with it. |
| 4 | Click a cell, click **Undo** on the toolbar, then type | The typing still goes to the canvas: toolbar buttons don't take the keyboard. |
| 5 | **Document → ASCII Only** | Works as in 1.5 (asks first when it would remove characters). |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Faster caret moves

Click a cell in an empty area first. Watch the caret after each key.

| # | Windows | macOS | Expected result |
|---|---|---|---|
| 1 | Ctrl+Right | Option+Right | The caret jumps 3 cells right (Left, Up and Down too). |
| 2 | Alt+Right | Cmd+Option+Right | The caret jumps 10 cells (all four directions). |
| 3 | Ctrl+Home, then Ctrl+End | Cmd+Up, then Cmd+Down | The caret goes to the top of its column, then the bottom row. |
| 4 | Home, End | Cmd+Left, Cmd+Right | As Home and End in 1.5 (row start, just past the row's text). |
| 5 | Shift+Right | Shift+Right | Nothing happens (Shift is kept for selection, later). |
| 6 | Windows: press Alt+Right, let go of Alt, then press Right | | The caret moves; the menu bar doesn't take the keyboard after Alt. |
| 7 | Jump towards an edge | | The caret stops at the edge. After a jump, Backspace erases the cell the caret is on. |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Fixes from step 1.5

| # | Do this | Expected result |
|---|---|---|
| 1 | In the 500 × 500 sample, zoom to 300%+ over the bottom middle, then zoom out with the wheel and with Ctrl+- | The spot under the pointer (or the window centre for Ctrl+-) stays put as far as scrolling allows, and once the canvas fits it's centred. No drift to the bottom right. |
| 2 | Type `abcd`, click the `c`, press Backspace twice | The first Backspace erases `c`; the second erases `b` and moves onto it. |
| 3 | Same with the arrow keys: type `abcd`, press Left, Backspace | `d` is erased (the caret was on it). |
| 4 | Click the first column of a row with text, press Backspace | That cell is erased. |
| 5 | Click the second-to-last column, type `xyz`, then Backspace twice | The first erases the `z` in the last column, the second erases the `x` cell before it. |
| 6 | Type `test`, move away with the arrow keys only, type `test2`, press Enter | The caret goes under the start of `test2`, not under `test`. |
| 7 | Type `hellp`, press Left, type `o`, press Enter | The caret goes under the `h`: fixing a typo keeps the text's start. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Make a 4096 × 4096 document (the largest allowed): it should warn, open, scroll and zoom (slowly is fine).
- Resize with each of the nine anchors, growing and shrinking, and undo each one.
- Change fonts with text in different scripts on the canvas (box-drawing lines, accents, Chinese).
- New, then undo: there's nothing to undo; the old document is gone (no saving yet).

## Known limitations (not bugs)

- New doesn't ask about unsaved changes; that comes with saving in 1.7. The app still opens with the sample document.
- Fonts loaded from a file are forgotten when the app closes; a document using one falls back to JetBrains Mono after a restart (settings arrive in M2).
- The font picker has no preview, and only regular styles are used.
- The dialogs, toolbar and menus are placeholders; every choice is listed in `docs/TEMPORARY-UI.md`.
- The caret keys and jump sizes can't be changed yet (M8).

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 1.6, test T<n>, row <#>
OS: <Windows 11 / macOS version>, display scaling: <100% / 150% / Retina>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
