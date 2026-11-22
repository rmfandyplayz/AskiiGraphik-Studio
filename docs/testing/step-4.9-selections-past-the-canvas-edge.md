<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.9: Selections past the canvas edge, manual test

**Build:** the commit that adds this file
**Test on:** Windows (Linux is covered by CI only)
**Time needed:** about 20 min

## What changed

The **Move tool** can now take the selection past the canvas edge, up to 10 columns past the left and right edges and about as far on screen above and below. Past the edge only the selection's outline shows, paler than on the canvas, and it no longer closes along the edge. The outline stays there when the move ends. Selection tools and commands only work on the part on the canvas, and drop the rest. A selection that's all past the canvas still counts, so drawing paints nothing until you deselect.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, leave the size as it is, and click **OK**. Choose **Don't Save** if it asks.
3. Press **T** for the Text tool, click in the top-left cell of the canvas and type three lines of letters, such as `ABCDEFGHIJ`, pressing **Enter** after each. Press **Esc** twice to leave the Text tool.
4. If the canvas fills the window, hold **Ctrl** and scroll the wheel down a step or two, so there's empty space all round the canvas.
5. Open **Window → History**, so you can see the undo steps.

## Known limitations (not bugs)

- **The space round the canvas:** with a canvas bigger than the window, you can't scroll far enough to see the whole zone past its edges. Zoom out to see it. Whether that space should grow is a question for you.
- **Cells past the edge are lost** when a move ends, as before this step: only the outline stays there.
- **Placeholders:** how pale the outline is past the edge, and the other choices listed in `docs/TEMPORARY-UI.md` under "Selections past the canvas edge".

## Tests

### T1: Moving past each edge

Checks that the outline follows the selection past the canvas edge, paler there, and that the letters come back if moved back on before you let go.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M** for Rectangle Select and drag a box round the first four letters of the top two lines. Press **V** for the Move tool | The letters are selected: a moving dashed outline round them, hatching over the rest of the canvas |
| 2 | Press on a selected letter and drag two cells to the left, keeping the button held | The outline goes two columns past the canvas's left edge. Past the edge it's paler, and its dashes still move. There's no line along the canvas edge. The two left columns of letters are hidden |
| 3 | Without letting go, drag back to where you started | All four columns of letters are back |
| 4 | Drag two cells left again and let go | The letters past the edge are gone. The outline stays past the edge, paler there. History's newest step is **Move** |
| 5 | Press **Ctrl+Z**, then do the same with the arrow keys: press **Up** once | The outline reaches a row above the canvas, paler there. The top line of selected letters is hidden, and the second now sits in the top row |
| 6 | Press **Ctrl+Z**. Drag the selection to the canvas's right edge and past it, then to the bottom edge and past it | The same happens at the right and bottom edges: a paler outline past the edge, no line along it |

### T2: The zone's limit

Checks that a move stops 10 columns past the left and right edges, and about as far on screen above and below, and that a drag then slides along that limit.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With some letters selected and the Move tool, press on a selected letter and drag far to the left of the canvas, keeping the button held | The outline stops when its left side is 10 columns past the canvas's left edge, however far you drag |
| 2 | Without letting go, move the pointer up and down | The outline slides up and down along that limit, keeping its place across |
| 3 | Let go, then press **Shift+Up** a few times | The outline stops when its top is a few rows above the canvas, about as far on screen as 10 columns |
| 4 | Press **Shift+Right** a few times, until it stops | The outline stops when its right side is 10 columns past the canvas's right edge |

### T3: The outline stays when the move ends

Checks that a selection past the edge is still there after you change tool, and moves back on as you'd expect.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select the first four letters of the top line and drag them two cells left with the Move tool. Let go | Two letters show on the canvas, two columns of outline past its edge |
| 2 | Press **Esc** for the Hand tool, then **V** for the Move tool again | The outline is still where you left it, paler past the edge |
| 3 | Drag the selection three cells right | The outline is now all on the canvas. Only the two letters that stayed on the canvas move; the other two cells of the selection are empty |

### T4: Selection tools on a selection partly past the edge

Checks that every selection tool, in every mode, drops the part past the canvas, and shows that part very faintly while you use it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Set up a selection partly past the left edge, as in T3 step 1. Press **M** for Rectangle Select | The outline still shows past the edge, paler there, with its dashes moving |
| 2 | Hold **Shift** (add) and start dragging a box over a few letters in the middle of the canvas, keeping the button held | The part past the edge turns very faint and stops moving. The outline that would stay closes along the canvas edge |
| 3 | Let go | The new box is added. The part past the canvas is gone, and the outline closes along the edge |
| 4 | Press **Ctrl+Z**, so the selection is back partly past the edge. Hold **Alt** (subtract) and drag a box over one letter on the canvas | While dragging, the part past the edge is very faint and still. After letting go, that letter is deselected and the part past the canvas is gone |
| 5 | Undo again. Hold **Shift+Alt** (intersect) and drag a box over the selection's part on the canvas | After letting go, only what's both in the box and on the canvas stays selected |
| 6 | Undo again. Hold **Shift** and click once on a letter, without dragging | Nothing is added, but the part past the canvas is dropped |
| 7 | Undo again. Without any keys, drag a new box | The old selection is replaced by the new box, as usual |
| 8 | Repeat steps 2 and 3 with **Ellipse Select** (**M**, then **2**), **Lasso** (**3**), **Polygonal Lasso** (**4**) and the **Magic Wand** (**W**, Shift+click a letter) | Each drops the part past the canvas the same way |

### T5: Commands on a selection partly past the edge

Checks that the Edit and Select menu commands only use the selected letters on the canvas.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Set up a selection partly past the left edge, as in T3 step 1. Choose **Edit → Copy**, click a blank part of the canvas with the Move tool, and press **Ctrl+V** | The paste holds only the two letters that were on the canvas |
| 2 | Press **Ctrl+Z** until the selection is back partly past the edge. Choose **Edit → Copy as Text**, and paste into Notepad | Only the two letters on the canvas are copied |
| 3 | Choose **Select → Invert** | Every cell on the canvas is selected except the two letters. Nothing past the edge is selected |
| 4 | Undo. Choose **Select → Grow by 1 Cell** | The two letters on the canvas grow by a cell, within the canvas. The part past the edge is gone |
| 5 | Undo. Press **Delete** | The two letters on the canvas are emptied. The outline stays as it was, past the edge too |

### T6: A selection all past the canvas

Checks that a selection with nothing on the canvas still counts: nothing can be drawn, and the commands that need cells are greyed out.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select two letters at the left of the top line and drag them five cells left with the Move tool, so the whole outline is past the canvas. Let go | The whole canvas is hatched. The outline shows only past the edge, paler |
| 2 | Open the **Edit** menu | **Cut**, **Copy**, **Copy as Text**, **Copy as ANSI** and **Delete** are greyed out. **Paste** isn't |
| 3 | Open the **Select** menu | **Grow by 1 Cell** and **Shrink by 1 Cell** are greyed out. **Select All**, **Deselect** and **Invert** aren't |
| 4 | Press **B** for the Brush and paint across the canvas | Nothing is painted |
| 5 | Press **V** for the Move tool and point at the outline past the edge | The move cursor shows |
| 6 | Drag the selection back onto the canvas, over some letters | The outline moves onto the canvas and the letters under it are now selected. Nothing moves with it: the two letters you moved off were lost when you picked the Brush |
| 7 | Move it all past the edge again, and choose **Select → Invert** | The whole canvas is selected |

### T7: Pastes past the edge

Checks that a paste can go past the edge like any selection.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select a few letters and press **Ctrl+C**. Point near the canvas's right edge and press **Ctrl+V** | The paste lands centred on the pointer, selected, with any part past the edge shown only as a paler outline |
| 2 | Press **Right** a few times | The paste moves further past the edge, and stops at the zone's limit |
| 3 | Press **Left** until it's all back on the canvas | All its letters show again |
| 4 | Press **Enter** | The move ends and nothing is selected |

### T8: Tests to revisit

| Step | Test | Where | Why |
|---|---|---|---|
| 4.8 | T4 | Windows and macOS | It failed and was fixed; you said you'd take a quick look rather than run it again |

## Worth poking at (optional)

### P1: A paste wider than the zone

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | In Notepad, type a line of about 120 letters and copy it. Paste it onto the canvas with **Ctrl+V** | The paste runs past both edges. Its outline shows past the edges only as far as the zone reaches |
| 2 | Press **Shift+Left** and **Shift+Right** a few times each | It moves only a little either way, as far as it can while still covering the whole zone |

### P2: Resizing the canvas

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Move a selection so it's partly past the left edge. Choose **Image → Canvas Size**, make the canvas 5 columns wider, anchored at the right, and click **OK** | The canvas grows to the left. The cells of the selection that are now on the canvas stay selected, and they're empty |
| 2 | Press **Ctrl+Z** | The canvas and the selection go back to how they were, past the edge too |
