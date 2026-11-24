<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.4: Clipping and Delete, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 20 min

## What changed

While anything is selected, every tool that changes cells changes only the selected ones: the Brush, FG Brush, BG Brush, Character Brush, Eraser, Soft Eraser, BG Eraser and Text tool. The Text tool's caret still moves over cells outside the selection; it just doesn't write there.

**Edit → Delete** empties the selected cells of the active layer, taking away their characters, colours and backgrounds, and leaves the selection in place. The **Delete** and **Backspace** keys do the same on the canvas, except while the Text tool's caret is showing, where they edit text as before.

From your 4.3 testing: the black and white hatching is thinner (8 pixels) and fainter (15%), and the highlight under the pointer no longer has stripes through it. Your slow idle frame on a ragged selection is noted for M10's profiling pass.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, choose **RGB (any color)**, keep the other defaults and click **OK**. Choose **Don't Save** if it asks.
3. Choose **View → Zoom In** two or three times, so cells are easy to see.
4. Make some art to work on:
   - Press **B**, then **3** for the BG Brush. Click **Red** in Swatches, and paint a solid patch about 12 cells wide and 6 tall near the top left.
   - Press **B**, then **1** for the Brush, and paint over the same patch and a bit past it until most of its cells hold characters.
   - From here on, "the art" means this patch: characters on red, with a few characters on no background round its edge.
5. Open **Window → History**, so you can see each undo step.

## Known limitations (not bugs)

- **Placeholders:** the menu item's name and place, and the keys, are placeholders, listed in `docs/TEMPORARY-UI.md` under "Clipping and Delete".
- **No shortcut in the menu:** Edit → Delete shows no shortcut, on purpose. A window-wide Delete shortcut would also fire while typing in a box such as Tool Options' Size; the keys work from the canvas instead.
- **Moving, cutting and pasting the selection** come in steps 4.5 and 4.6.

## Visual tests (poke around and try these)

- **Thinner, fainter hatching.** Press **M**, then **1**, and drag a rectangle over part of the art. Over everything that isn't selected, the black and white stripes are each about 8 pixels wide and faint (15%), so the art underneath reads clearly.
  - Check it in dark and light mode.
  - Hold **Alt** and drag over part of the selection: the red hatching over the cells it would deselect has the same 8-pixel stripes, and is as strong as before (40%).
- **The pointer's highlight sits on top.** With part of the canvas hatched, press **Esc** for the Hand tool and move the pointer over hatched cells. The light blue square under the pointer is clean, with no stripes running through it. Press **T** and do the same before clicking: same result.

## Tests

### T1: Brushes keep to the selection

Checks that the four brushes only change selected cells, and that a stroke is still one undo step.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, then **1**. Drag a rectangle about 4 cells wide over the middle of the art, from its top to below its bottom edge | That rectangle is selected; everything else is hatched |
| 2 | Press **B**, then **1** for the Brush. Pick a different text colour in the Color window, and drag a stroke from well left of the art, straight through the selection, to well right of it | Only the cells inside the selection change. Cells the stroke crossed outside it, empty or not, are untouched. History adds one **Paint** step |
| 3 | Press **Ctrl+Z** | The stroke is undone |
| 4 | Press **B**, then **2** for the FG Brush, and drag the same way | Only the characters inside the selection change colour |
| 5 | Press **B**, then **3** for the BG Brush. Click **Blue** in Swatches and drag the same way | Only the selected cells turn blue; the red round them stays |
| 6 | Press **B**, then **4** for the Character Brush and drag the same way | The character appears only inside the selection |
| 7 | Drag a Brush stroke that stays wholly outside the selection | Nothing changes, and History adds nothing |

### T2: Erasers keep to the selection

Checks the Eraser, Soft Eraser and BG Eraser.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Keep the selection from T1 (or make one the same way). Press **E**, then **1** for the Eraser, and drag through the selection from one side of the art to the other | Only the selected cells are emptied, red included. The art on both sides is untouched |
| 2 | Press **Ctrl+Z**, then **E**, then **2** for the Soft Eraser, and drag the same way | Only the selected cells get lighter |
| 3 | Press **Ctrl+Z**, then **E**, then **3** for the BG Eraser, and drag the same way | Only the selected cells lose their red; their characters stay |

### T3: Typing keeps to the selection

Checks that typed characters land only in selected cells while the caret carries on as normal.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, then **1**, and select a box 3 cells wide and 1 tall in an empty part of the canvas | It's selected |
| 2 | Press **T** and click the cell two to the left of the box. Type `abcdefg` | Only `cde` appear, in the three selected cells. The caret keeps moving as you type and ends just after where the `g` would be |
| 3 | Press **Backspace** seven times | The caret walks back to where you clicked, and the three characters are erased as it passes them |
| 4 | Click two cells to the left of the box again and type `ab` | Nothing appears, the caret moves two cells, and History adds nothing |

### T4: Edit → Delete

Checks that Delete empties the selected cells completely and keeps the selection.

**Also on macOS:** Yes, because the Edit menu is the Mac's own menu bar there

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, then **1**, and drag a rectangle over part of the art, crossing its edge so it takes in some red cells and some empty ones | It's selected |
| 2 | Choose **Edit → Delete** | Every selected cell is empty: no characters, no red. The outline and hatching are unchanged, so the same cells are still selected. History adds **Delete** |
| 3 | Press **Ctrl+Z** (**Cmd+Z** on macOS) | The art comes back, red included |
| 4 | Press **Ctrl+D** (**Cmd+D**) to deselect, then open the **Edit** menu | **Delete** is greyed out |

### T5: The Delete and Backspace keys

Checks the keys on the canvas, and that the Text tool's caret and text boxes keep them for text.

**Also on macOS:** Yes, because the keys differ: the Mac's **delete** key is Backspace, and **fn+delete** is Delete

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, then **1**, and select part of the art. Press **Delete** (**fn+delete** on macOS) | The selected cells are emptied, as Edit → Delete does. History adds **Delete** |
| 2 | Press **Ctrl+Z**, then **Backspace** (**delete** on macOS) | The same happens |
| 3 | Press **Ctrl+Z**. Press **T**, then click a cell inside the selection that holds a character, and press **Delete** | Only that one cell's character goes, as the Text tool always did; the rest of the selection is untouched. History adds **Typing** |
| 4 | Press **Backspace** | The Text tool takes it: one more character goes at most, not the whole selection |
| 5 | Press **Esc** (the caret goes and the Hand tool is chosen), then press **Delete** | Now the whole selection is emptied |
| 6 | Press **Ctrl+Z**. Click in Tool Options' **Size** box (choose the Brush first with **B**, **1** if it isn't showing), and press **Backspace** | The box loses a digit; the canvas doesn't change |

### T6: Locked and hidden layers

Checks that Delete and the keys do nothing to a layer that can't be changed.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select part of the art. In the Layers panel, tick **Locked** for the layer, then open the **Edit** menu | **Delete** is greyed out |
| 2 | Close the menu, click the canvas with the Hand tool (**Esc**), and press **Delete** | Nothing changes, and History adds nothing new |
| 3 | Untick **Locked** and untick **Visible** instead, then open the **Edit** menu | **Delete** is still greyed out |
| 4 | Tick **Visible** again | **Delete** is available again |

### T7: Tests to revisit

| Step | Test | Where | Why |
|---|---|---|---|
| 4.3 | T10 (Select Cells with Glyphs and with Background Color) | Windows | It wasn't in your list of 4.3 passes. If you ran it, ignore this row. Also, 4.3's T5 had the wrong numbers: the BG Brush is **B** then **3**, and the FG Brush **B** then **2** |

## Worth poking at (optional)

### P1: Delete after the Magic Wand

Checks Delete on a ragged selection.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **W**. In Tool Options, choose **Same Glyph** in Match and **Global** in Area, then click one character of the art | Every cell with that character is selected, scattered over the art |
| 2 | Press **Delete** | Exactly those cells are emptied; the cells between them keep their characters and red |

### P2: A big brush half over the selection

Checks that a brush bigger than the selection still only paints inside it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select a box 3 cells wide and 3 tall over the art. Press **B**, then **1**, and set Tool Options' **Size** to **9** | The brush outline is much bigger than the box |
| 2 | Click once with the outline centred on the box's corner | Only the box's cells change, though the outline covers far more |
