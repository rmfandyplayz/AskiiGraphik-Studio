<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.1: Selection mask, Rectangle Select, Select All and Deselect, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 25 min

> The fixes from your first round of testing are in [step-4.1-follow-ups.md](step-4.1-follow-ups.md). Its tests replace T7 row 3 and T8 here.

## What changed

You can now select cells. **Rectangle Select** (press **M**, or the new **Select** button on the Tools toolbar) selects a rectangle by dragging, and a new **Select** menu has **Select All** (Ctrl+A) and **Deselect** (Ctrl+D). The selection shows as a dashed black-and-white outline.

Selecting is an undo step you can see in History, but it never makes the document unsaved (no `*` in the window title), since selections aren't saved. Resizing the canvas moves the selection along with the art.

Nothing uses the selection yet: drawing tools still paint outside it. That comes in later M4 steps.

## Setup

1. Run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, keep the defaults and click **OK**. Choose **Don't Save** if it asks.
3. Press **T**, click near the top-left of the canvas and type a few lines of text, so there's art to see the outline over. Press **Esc**.
4. Choose **View → Zoom In** two or three times, so cells are easy to aim at.
5. Open **Window → History**, so you can see each undo step.

## Known limitations (not bugs)

- The outline is a placeholder: still dashes, no marching ants, the same in light and dark mode.
- Drawing tools don't keep to the selection yet (step 4.4), and there's no way to move, copy or delete the selected cells yet (4.4–4.6).
- Shift and Alt do nothing with Rectangle Select yet. Adding to and taking away from a selection comes in 4.2.
- The Select group has only Rectangle Select, so it has no sub-toolbar yet. Ellipse and the lassos join it in 4.2.
- The pointer stays the normal arrow with Rectangle Select.

## Tests

### T1: Dragging out a rectangle

Checks that M chooses Rectangle Select, that a drag selects every cell from the first to the last, and that it's one undo step.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M** | The **Select** button on the Tools toolbar is checked, and **Tools** menu → **Rectangle Select** has a check mark |
| 2 | Press the mouse on a cell inside your text, drag two or three cells right and down, and hold still | A dashed outline follows the pointer, around a rectangle from the first cell to the one under the pointer. History has no new step yet |
| 3 | Release | The outline stays, around every cell from the first to the last, both included. History has one new step, **Rectangle Select**. The window title has no `*` |
| 4 | Drag a new rectangle somewhere else | The old outline goes, the new one shows, and History has a second **Rectangle Select** |

### T2: Dragging in other directions and off the edge

Checks that the rectangle is the same whichever way you drag, and stops at the canvas edge.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag from a cell up and to the left | The rectangle covers both end cells, just as dragging the other way would |
| 2 | Drag from a cell near the right edge out onto the grey around the canvas, and release there | The outline stops at the canvas's right edge; nothing outside the canvas is outlined |
| 3 | Hold **Space** and drag | The canvas pans, and the selection stays where it was on the art |

### T3: Clicking and Esc

Checks that a click deselects, that Esc cancels a drag, and that Esc otherwise goes to the Hand tool as before.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With a selection showing, click a cell without dragging | The outline goes. History adds **Deselect** |
| 2 | Click again | Nothing changes, and History adds nothing |
| 3 | Drag out a rectangle, and before releasing press **Esc**, then release | The outline you were dragging goes, and the selection is as it was before the drag. History adds nothing |
| 4 | Press **Esc** again | The **Hand** button is checked |

### T4: Select All and Deselect

Checks the new Select menu and its shortcuts.

**Also on macOS:** Yes, because the Select menu is in the system menu bar and the keys are Cmd+A and Cmd+D

| # | Do this | Expected result |
|---|---|---|
| 1 | With nothing selected, open the **Select** menu (between Edit and Document) | It has **Select All** and **Deselect**. Deselect is greyed out |
| 2 | Choose **Select All** | The outline runs round the whole canvas. History adds **Select All** |
| 3 | Open the **Select** menu again | Deselect is available now |
| 4 | Click the canvas with the **Hand** tool, then press **Ctrl+D** (Cmd+D on a Mac) | The outline goes. History adds **Deselect** |
| 5 | Press **Ctrl+A** (Cmd+A) | The whole canvas is selected again |
| 6 | Press **Ctrl+A** again | Nothing changes, and History adds nothing |

### T5: Undo, and staying saved

Checks that selection steps undo and redo, but never make the document unsaved.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **File → Save** and save the document anywhere | The window title has no `*` |
| 2 | Drag out a rectangle, then press **Ctrl+A** | Two new steps in History. Still no `*` |
| 3 | Press **Ctrl+Z** twice | The selection goes back to your rectangle, then to what it was before. Still no `*` |
| 4 | Press **Ctrl+Y** (Ctrl+Shift+Z) twice | Both selections come back. Still no `*` |
| 5 | Press **Ctrl+Z** once, then drag out a different rectangle | The new rectangle replaces the undone step in History. Still no `*` |
| 6 | Press **T**, click a cell and type a letter, then press **Esc** | The title gets a `*` |
| 7 | Press **Ctrl+Z** until the letter is gone, but not past the save | The `*` goes once the letter is undone, even with selection steps still to undo |
| 8 | Drag out a rectangle with **M**, then choose **File → New** | It creates a new document without asking to save |

### T6: Resizing the canvas with a selection

Checks that Resize Canvas moves the selection with the art, drops the cells it crops, and undoes as one step.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Type some text near the top-left again, press **Esc**, then **M**, and drag a rectangle round a word | The word is outlined |
| 2 | Choose **Document → Resize Canvas…**, add 10 to the Width and the Height, click the bottom-right square under **Anchor**, and click **OK** | The canvas grows at the top and left. The word moves down and right, and the outline moves with it, still round the same word. History adds one **Resize Canvas** |
| 3 | Press **Ctrl+Z** | The canvas, the word and the outline are back where they were, in one step |
| 4 | Resize again, taking the Width down so the right half of the selected word is cut off, with the top-left square under **Anchor** | Only the part of the selection still on the canvas is outlined |
| 5 | Press **Ctrl+Z** | The whole selection is back |

### T7: How the outline looks

Checks that the outline reads on any art, stays on the art while it scrolls, and is crisp at every zoom.

**Also on macOS:** Yes, because a Retina display draws it at a different pixel size

| # | Do this | Expected result |
|---|---|---|
| 1 | Select a rectangle that runs over text, empty cells and, if your document has colours, a coloured background | The black and white dashes show clearly over all of them |
| 2 | Turn on **View → Show Grid** | The outline is drawn over the grid lines |
| 3 | Scroll and zoom in and out | The outline stays on the same cells, the dashes stay put on the art rather than crawling, and the line is thin and sharp at every zoom |
| 4 | Switch the system to dark mode (or light, if it's dark) | The outline looks the same |

### T8: Shortcuts in a text box

Checks that Ctrl+A and Ctrl+Z in a text box edit the box, not the canvas. Before this step, Ctrl+Z in a text box undid the document.

**Also on macOS:** Yes, because there the Select menu's shortcuts go through the system menu bar

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **Ctrl+D** to deselect. Open **Window → Ramps**, click **New**, click in the **Characters, lightest first** box, and type a few characters if it's empty | The typing goes into that box |
| 2 | Press **Ctrl+A** (Cmd+A) | All the characters in the box are highlighted. The canvas shows no outline, and History adds nothing |
| 3 | Type a few characters, then press **Ctrl+Z** (Cmd+Z) | The typing in the box is undone. History doesn't move |
| 4 | Click the canvas, then press **Ctrl+A** | The whole canvas is selected |

## Worth poking at (optional)

### P1: Odd drags

Checks drags that start or end in unusual places.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Start a drag on the grey outside the canvas and end it on the canvas | The part on the canvas is selected |
| 2 | Drag only over the grey, never touching the canvas | Nothing is selected afterwards (History adds **Deselect** if something was selected) |
| 3 | Drag from a cell to its neighbour and back to the first cell, then release | Just that one cell is selected |
| 4 | Start a drag, and while still holding the button press **B** | The Brush is chosen, the half-dragged outline goes, and the selection is as it was |

### P2: Locked and hidden layers

Checks that selecting works whatever the active layer's state, since it doesn't change any cells.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | In **Window → Layers**, lock the active layer, then drag a rectangle with **M** | It selects as usual |
| 2 | Hide the layer instead, and drag again | It selects as usual |

### P3: A big canvas

Checks that a selection on a big canvas doesn't slow the canvas down.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Debug → Open Sample Document** (500 × 500), then press **Ctrl+A** | The whole canvas is outlined straight away |
| 2 | Pan and zoom around | As smooth as without a selection |

### P4: Jumping in History

Checks that clicking a step in History brings back the selection it had.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make three different rectangle selections, then click the first **Rectangle Select** in History | The first rectangle is outlined |
| 2 | Click the last one | The last rectangle is outlined |
