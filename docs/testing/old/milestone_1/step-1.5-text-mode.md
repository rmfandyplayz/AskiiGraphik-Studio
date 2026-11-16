<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 1.5: Text mode, manual test

**Build:** working tree (not pushed yet)
**Test on:** - [ ] Windows  - [ ] macOS  (Linux is covered by CI only)
**Time needed:** about 25 min

## What changed

You can type on the canvas. The text tool is the only tool for now, so it's always on: click a cell to put the caret there, then type. A new **Edit** menu has Undo, Redo and a temporary **ASCII Only** switch, and the status bar shows whether the document is `Unicode` or `ASCII only`.

It also fixes three things from your 1.4 testing: Cmd during a touchpad glide no longer zooms; on macOS, the extra zoom shortcuts (Cmd+Shift+= and the keypad) now work; and the macOS CI failure.

Shortcuts below say **Ctrl**. On a Mac, use **Cmd** instead.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Zoom in to 200% or so (Ctrl+=), so cells are easy to see. The clear box around the text at the top left is a good place to type.

## Tests

### T1: Place the caret and type

| # | Do this | Expected result |
|---|---|---|
| 1 | Click an empty cell in the clear box | A blue outline appears around that cell: the caret. |
| 2 | Type `Hello world` | The letters appear one per cell, left to right, and the caret moves along. Space types a space. |
| 3 | Type some accented letters (on Windows with your keyboard layout, on a Mac with Option+E then E for é) | They appear like any other letter. |
| 4 | Click a cell in the shading rings and type `XYZ` | The letters replace the ring characters. Nothing to the right moves. |
| 5 | Hold **Space** and drag with the left mouse button | Spaces are typed (Space repeats while held), the caret jumps to where you pressed, and the canvas does **not** pan. (Middle-drag still pans.) |
| 6 | Scroll to the right edge of the canvas, click the last column and type `abc` | Only `c` is left in the last cell; the caret stays in the last column. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Editing keys

| # | Do this | Expected result |
|---|---|---|
| 1 | Click a cell and press the arrow keys | The caret moves one cell at a time and stops at the canvas edges. |
| 2 | Type `abcd`, press **Left** twice, then **Backspace** | The `b` disappears and the caret moves onto its cell. `c` and `d` stay where they were. |
| 3 | Press **Delete** (Fn+Delete on a Mac laptop) | The character under the caret disappears; the caret stays. |
| 4 | Click a cell, type `one`, press **Enter**, type `two` | `two` starts on the next row, right under `one`. |
| 5 | Press **Home**, then **End** (Fn+Left and Fn+Right on a Mac laptop) | Home jumps to the first column of the row. End jumps to just after the row's last character (in the rings, that's the border). |
| 6 | Hold an arrow key until the caret goes past the edge of the window | The canvas scrolls to keep the caret in view. |
| 7 | Press **Esc**, then type | The caret disappears, and typing does nothing until you click again. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Undo and redo

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Edit** menu before typing anything | Undo and Redo are greyed out. |
| 2 | Click a cell and type `hello world` without stopping, then press **Ctrl+Z** | All of `hello world` disappears in one go. |
| 3 | Type `abc`, wait 3 seconds, type `def`, then press Ctrl+Z twice | The first Ctrl+Z removes `def`, the second removes `abc`. |
| 4 | Press **Ctrl+Y** (or **Ctrl+Shift+Z**; Cmd+Shift+Z on a Mac) twice | `abc` comes back, then `def`. |
| 5 | Type `abc`, press **Left**, type `x`, then Ctrl+Z | Only the `x` is undone: moving the caret started a new undo step. |

- [ ] Pass  - [ ] Fail. Notes:

### T4: ASCII only

| # | Do this | Expected result |
|---|---|---|
| 1 | Look at the status bar | It says `Unicode`. |
| 2 | Choose **Edit → ASCII Only** | A dialog says switching removes every character that isn't ASCII, and how many there are. |
| 3 | Click **Cancel** | Nothing changes. ASCII Only has no check mark, and the status bar still says `Unicode`. |
| 4 | Choose **Edit → ASCII Only** again and click **Remove and Switch** | The accented letters, lines, arrows, Chinese and Japanese characters and the border disappear. The ring characters stay. The status bar says `ASCII only`, and the menu item has a check mark. |
| 5 | Type `é`, then `a` | `é` isn't typed; `a` is. |
| 6 | Press Ctrl+Z | Everything removed comes back, and the status bar says `Unicode` again. |
| 7 | Press Ctrl+Y, then choose **Edit → ASCII Only** once more | It switches back to `Unicode` straight away, with no dialog. The removed characters don't come back (only undo does that). |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Fixes from step 1.4

| # | Do this | Expected result |
|---|---|---|
| 1 | macOS: at 800%, swipe to scroll, lift your fingers and press **Cmd** while it's still gliding | It keeps scrolling to a stop and never zooms. Try it with the Magic Mouse too. |
| 2 | macOS: wait for the glide to stop, then hold Cmd and swipe | It zooms a few steps per swipe, not all the way at once. Let go of Cmd mid-glide: the zoom stops, and nothing scrolls. |
| 3 | Windows: Ctrl + mouse wheel | Still one zoom step per notch. Scroll, pause briefly, then Ctrl + wheel zooms. |
| 4 | macOS: press **Cmd+Shift+=**, then the keypad **+** with Cmd if you have one | Both zoom in. |
| 5 | After pushing, check CI | All three jobs are green; the macOS job no longer fails on the `←` glyph. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Type quickly over a large area, then undo and redo many times.
- Type at 25% and at 800% zoom, and with 150% display scaling.
- Switch to ASCII Only, type, undo and redo in different orders.
- On Windows, try an emoji from the Win+. picker; on a Mac, from Ctrl+Cmd+Space.

## Known limitations (not bugs)

- The text tool is always on, so Space + drag can't pan in this step. Middle-drag still does. Space + drag returns with the drawing tools (M3), as you described for draw mode.
- Chinese, Japanese and Korean input methods don't work yet (no IME support). Characters typed directly, including accents and emoji, do.
- Wide characters such as 漢 fit in one cell, drawn small on an orange cell.
- ASCII Only sits in the Edit menu until you design the New dialog and document options. Characters that aren't allowed are skipped without any message.
- The caret doesn't blink, there's no selection yet, and undo doesn't move the caret back.
- All these placeholder choices are listed in `docs/TEMPORARY-UI.md`.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 1.5, test T<n>, row <#>
OS: <Windows 11 / macOS version>, display scaling: <100% / 150% / Retina>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
