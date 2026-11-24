<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.6: Cut, Copy and Paste, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 40 min

## What changed

The Edit menu has **Cut**, **Copy** and **Paste**, on Ctrl+X, Ctrl+C and Ctrl+V (Cmd on a Mac). Copy takes the active layer's selected cells. Other apps get them as plain text. This app gets them with their colours, their shape and the place they came from. Cut copies them and then empties them.

A paste lands centred on the pointer, floating and selected, with the **Move** tool chosen. You can drag it or nudge it with the arrow keys until Enter or any other edit ends it, and it's one **Paste** step in History. **Esc** takes the whole paste away. If the pointer isn't over the canvas, the paste goes back where it was copied from.

At the Text tool's caret, Ctrl+V types the clipboard's text in instead. **Copy as Text** and **Copy as ANSI** now copy just the selection while there is one. The Move tool's pointer shows the move cursor over the cells a drag would move. That was your request from 4.5's testing.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**. Under **Characters**, choose **Unicode (any character)**, keep the other defaults and click **OK**. Choose **Don't Save** if it asks.
3. Choose **Document → RGB Colors**, then **View → Zoom In** two or three times, so cells are easy to see.
4. Make some art to work on:
   - Press **T** and click a cell about 3 cells in from the top left. Type `abcdef`, press **Enter**, type `ghijkl`, press **Enter**, type `mnopqr`. Press **Esc**.
   - From here on, "the letters" means these three rows.
5. Open **Window → History**, so you can see each undo step.
6. Keep Notepad open beside the app (TextEdit on a Mac, set to plain text with **Format → Make Plain Text**).

## Known limitations (not bugs)

- **Placeholders:** the menu items' names and places, the step names, and where a paste lands when the pointer is elsewhere are listed in `docs/TEMPORARY-UI.md` under "Cut, Copy and Paste".
- **Edit → Paste from the menu** always uses the fallback place, since the pointer is on the menu, not the canvas. Ctrl+V pastes at the pointer.
- **Colours from other apps:** pasting text with ANSI colour codes drops the codes and keeps the characters. Reading the colours comes with ANSI import in M7.
- **Paste isn't greyed out** when the clipboard holds nothing it can use. It then does nothing.

## Tests

### T1: The Edit menu and its shortcuts

Checks the new menu items, their order, and when they're greyed out.

**Also on macOS:** Yes, because the Mac menu bar shows the shortcuts with Cmd.

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Select → Deselect** if it's available, then open the **Edit** menu | In order: **Undo**, **Redo**, a line, **Cut** (Ctrl+X), **Copy** (Ctrl+C), **Paste** (Ctrl+V), a line, **Copy as Text** (Ctrl+Shift+C), **Copy as ANSI**, a line, **Delete**. Cut and Copy are greyed out; Paste isn't |
| 2 | Close the menu. Press **M** and drag a box over `bcd` in the top row of the letters. Open **Edit** | Cut and Copy are available now |
| 3 | On Windows only: close the menu, press **Alt**, then **E** | The Edit menu opens, with the letters **t** in Cut, **C** in Copy, **P** in Paste and **x** in Copy as Text underlined |

### T2: Copy, then paste at the pointer

Checks that a paste lands centred on the pointer, floating and selected, and is one Paste step however much you move it.

**Also on macOS:** Yes, because the shortcuts use Cmd there.

| # | Do this | Expected result |
|---|---|---|
| 1 | With `bcd` still selected, press **Ctrl+C** | Nothing changes on the canvas, and History has no new step |
| 2 | Put the pointer over an empty spot a few rows under the letters, and press **Ctrl+V** | `bcd` appears with its middle letter `c` under the pointer, outlined as the selection. The **Move** tool is chosen. History's newest step is **Paste**. The original `bcd` is still there |
| 3 | Drag the pasted `bcd` across the letters and drop it beside them | It passes over the letters without harming them. History still has one **Paste** step |
| 4 | Press the **Right** arrow twice | The paste moves 2 cells right. Still one Paste step |
| 5 | Press **Enter**, then **Ctrl+Z** | Enter ends the paste. The undo takes the whole paste away, wherever it was moved to |

### T3: Cut, and the paste going back where it came from

Checks Cut, and that a paste goes back where it was copied from when the pointer isn't over the canvas.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, select `hij` in the middle row, and choose **Edit → Cut** | `hij` disappears, its outline stays, and History's newest step is **Cut** |
| 2 | Choose **Edit → Paste** from the menu | `hij` comes back exactly where it was, floating and selected, with the Move tool chosen. Press **Enter** |
| 3 | Press **Ctrl+V** with the pointer over the History window | Another copy floats exactly over the first `hij`, where it was copied from. Press **Esc** |
| 4 | Press **Ctrl+V** with the pointer outside the app's window | The same: it lands where it was copied from. Press **Esc** |
| 5 | Choose **View → Zoom Out** until there's grey space around the canvas. Put the pointer in the grey space far from the canvas's edge, about 20 cells away, and press **Ctrl+V** | It lands where it was copied from, since none of it would have landed on the canvas at the pointer. Press **Esc** |
| 6 | Put the pointer in the grey space just past the canvas's left edge, about 1 cell out, and press **Ctrl+V** | It lands at the pointer, partly off the canvas, with at least one letter on it. Press **Esc**, then zoom back in |

### T4: Esc takes a paste away

Checks that Esc removes the whole paste and leaves no step.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select `pqr` with **M**. Press **Ctrl+C**, point at an empty spot and press **Ctrl+V** | `pqr` floats at the pointer, selected |
| 2 | Drag it somewhere else, then press **Esc** | The paste is gone. The selection is back on the original `pqr`. History has no Paste step |
| 3 | Press **Esc** again | The Hand tool is chosen |

### T5: Colours and shape come along

Checks that a copy within the app keeps colours, and that the cells inside the selection's box that weren't selected aren't pasted.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | In the **Color** window (Window → Color), choose a red text colour and a blue background. Press **T**, click under the letters and type `XYZ`. Press **Esc** | A red `XYZ` on blue |
| 2 | Press **M** and drag a box over just the `a`. Then hold **Shift** and drag a box over just the `X` | Just those two cells are selected, far apart |
| 3 | Press **Ctrl+C**, point at an empty area to the right and press **Ctrl+V** | `a` and the red-on-blue `X` appear, the same distance apart as before, outlined as two separate cells. Nothing else from between them comes along |
| 4 | Drag the paste over some letters | The cells between the two pasted ones don't cover anything. Press **Enter** |

### T6: Pasting into a document that can't hold everything

Checks that colours change to the new document's colour mode, and characters it can't hold leave their cells empty. The clipboard survives File → New.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **T**, click an empty cell and type `é`, then select it and the red `XYZ` with **M**, and press **Ctrl+C** | |
| 2 | Choose **File → New**, keep the defaults (ASCII only, Monochrome) and click **OK**, choosing **Don't Save** | An empty document |
| 3 | Point at the middle of the canvas and press **Ctrl+V** | `XYZ` appears, in the default colours rather than red on blue, and where `é` was there's an empty cell. The outline still covers the whole copied box |
| 4 | Press **Esc**. Choose **Document → ANSI Colors**, then press **Ctrl+V** again | Now `XYZ` is red on blue again (the nearest of the 16 colours) |

### T7: To and from other apps

Checks plain text both ways: what Notepad gets from Copy, and what the canvas makes of Notepad's text.

**Also on macOS:** Yes, because the system clipboard works differently there (use TextEdit).

| # | Do this | Expected result |
|---|---|---|
| 1 | Make a new Unicode document as in Setup step 2 and type the letters again. Select the top two rows of letters with **M** and press **Ctrl+C**. Paste into Notepad | Two lines, `abcdef` and `ghijkl` |
| 2 | Back in the app, select a box that's partly empty, say the letters and two empty columns to their right, and copy it. Paste into Notepad | Each line ends with spaces for the empty cells |
| 3 | In Notepad type `x   y` (three spaces), press **Enter**, type `z`, select both lines and copy them. In the app, point at a spot over the letters and press **Ctrl+V** | `x` and `y` with `z` under the `x`, all floating at the pointer. The three spaces don't cover the letters under them: those show through |
| 4 | Press **Esc**. In Notepad, type `a`, press **Tab**, type `b`. Copy it and paste it on an empty part of the canvas | `b` lands 8 columns right of `a` |

### T8: Ctrl+V at the Text caret

Checks that pasting while the Text tool's caret shows types the text in, line under line, as one step.

**Also on macOS:** Yes, because Cmd+V there must reach the caret, not the menu's paste.

| # | Do this | Expected result |
|---|---|---|
| 1 | In Notepad, copy two lines: `one` and `two`. In the app press **T** and click an empty cell | The caret shows |
| 2 | Press **Ctrl+V** | `one` is typed from the caret and `two` starts right under the `o`. The caret ends after `two`. The Text tool stays chosen, nothing floats, and History's newest step is **Paste** |
| 3 | Type `!` | The `!` goes after `two`, as its own **Typing** step |
| 4 | Press **Esc**. Select two empty cells side by side with **M**, press **T** and click the empty cell just left of them, then press **Ctrl+V** | The two selected cells get `n` and `e`. The cell you clicked and the row under it stay empty, since they aren't selected |

### T9: Copy as Text and Copy as ANSI on a selection

Checks that the text copies take just the selection while there is one.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select `bc` and `hi` (a box 2 wide and 2 tall) with **M**. Choose **Edit → Copy as Text**, then paste into Notepad | Two lines: `bc` and `hi` |
| 2 | Choose **Select → Deselect**, then **Edit → Copy as Text**, and paste into Notepad | The whole canvas, every row its full width |
| 3 | Select the red `XYZ` from T5 (or type it again in colour), choose **Edit → Copy as ANSI**, and paste into Notepad | Just that row, with escape codes around the letters |

### T10: Locked and hidden layers

Checks what's greyed out on a layer you can't change.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select some letters with **M**. In the Layers window, tick the layer's **locked** box. Open **Edit** | **Copy** is available; **Cut** and **Paste** are greyed out |
| 2 | Choose **Copy**, then make another layer active (add one if needed) and press **Ctrl+V** | The paste floats on the new active layer |
| 3 | Press **Esc**, make the locked layer active again, untick **locked** and untick its **visible** box. Open **Edit** | Cut, Copy and Paste are all greyed out. Tick **visible** again |

### T11: The Move tool's cursor

Checks the move cursor from your 4.5 request: shown over the cells a drag would move, and only there.

**Also on macOS:** Yes, because the cursor is the open hand there and the four-way arrows on Windows.

| # | Do this | Expected result |
|---|---|---|
| 1 | Select a few letters with **M**, then press **V**. Hover over the selected cells | The pointer is the four-way arrows on Windows, the open hand on a Mac |
| 2 | Hover over a cell that isn't selected | The normal arrow |
| 3 | Choose **Select → Deselect** and hover anywhere on the canvas, then over the grey space around it | The move cursor anywhere on the canvas, and the normal arrow off it |
| 4 | Lock the layer and hover over the canvas | The normal arrow. Unlock it again |
| 5 | Copy some letters, paste them with **Ctrl+V** and hover over the paste while it floats | The move cursor over the pasted cells |

### T12: Text boxes keep their own Cut, Copy and Paste

Checks that the shortcuts edit a text box while it has the keyboard, rather than the canvas.

**Also on macOS:** Yes, because the Mac menu bar sees the shortcuts first.

| # | Do this | Expected result |
|---|---|---|
| 1 | Select some letters on the canvas. In the Layers window, double-click a layer to rename it. Press **Ctrl+A**, **Ctrl+C**, then **End** and **Ctrl+V** | The name appears twice in the text box. Nothing is pasted on the canvas |
| 2 | Press **Ctrl+A**, then **Ctrl+X** | The text box empties. The canvas letters are untouched. Type the old name back and press **Enter** |

## Worth poking at (optional)

### P1: A paste partly off the canvas

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Paste a few letters, then nudge them with the arrow keys until half of them go past the right edge | The outline is cut at the edge |
| 2 | Nudge them back | The letters that went off come back |
| 3 | Push them half off again and press **Enter**, then nudge the selection back with the Move tool | The letters that were off the canvas when the paste ended are gone |

### P2: Pasting during a paste

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Paste something, then point elsewhere and press **Ctrl+V** again | The first paste stays where it was, as its own Paste step, and a second one floats at the pointer |

### P3: Big text from another app

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Copy a whole page of text from a website or document, and press **Ctrl+V** over the canvas | It floats at the pointer, with lines longer than the canvas running off it. Press **Enter**: what's off the canvas is gone |
