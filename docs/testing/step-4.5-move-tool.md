<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.5: The Move tool, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 30 min

## What changed

The **Move** tool, on **V**, moves the selected cells of the active layer, or the whole layer when nothing is selected. Drag from anywhere on the canvas, or nudge with the arrow keys: 1 cell, or 10 with Shift. Alt+drag (Option+drag on a Mac) copies instead.

A move floats until something else happens. Across several drags and nudges, the cells pass over other art without harming it, and the whole move is one **Move** step in History. Enter, another tool, another layer, an undo or any other edit ends it. Esc puts the cells back where they started.

The **Hand** tool is now the tool at launch. It has no letter any more: **Esc** picks it, and **H** does nothing.

A fix from your 4.4 report: a window that's maximized and then moved to your second monitor now reopens on that monitor.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, keep the defaults and click **OK**. Choose **Don't Save** if it asks.
3. Choose **View → Zoom In** two or three times, so cells are easy to see.
4. Make some art to work on:
   - Press **T** and click a cell about 3 cells in from the top left. Type `abcdef`, press **Enter**, type `ghijkl`, press **Enter**, type `mnopqr`. Press **Esc**.
   - Press **B**, then **1** for the Brush, and paint a solid patch about 6 cells wide and 3 tall, a few cells to the right of the letters, on the same rows.
   - From here on, "the letters" means the three rows you typed, and "the patch" means the Brush patch.
5. Open **Window → History**, so you can see each undo step.

## Known limitations (not bugs)

- **Placeholders:** the toolbar button and menu item's name and place, the normal arrow pointer and the step's name are placeholders, listed in `docs/TEMPORARY-UI.md` under "Move tool".
- **Narrow windows:** the Tools toolbar has one more text button, so in a window 1920 pixels wide (at Windows' 100% scaling) or narrower it doesn't quite fit and scrolls. That goes when the icon buttons arrive.
- **Not built:** moving only the outline without the cells, and holding Shift during a drag to keep it straight.
- **Cut, Copy and Paste** come in step 4.6.

## Tests

### T1: The Hand tool at launch, and the new keys

Checks which tool is chosen at launch, and that V picks Move while H picks nothing.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Close the app and start it again. Look at the Tools toolbar | **Hand** is the chosen button, and **Move** is the next one after it |
| 2 | Open the **Tools** menu | **Move** is right after **Hand**. Hand has the check mark |
| 3 | Close the menu, click the canvas once, and press **V** | The Move button is chosen |
| 4 | Press **H** | Nothing changes: Move stays chosen |
| 5 | Press **Esc** | Hand is chosen |

### T2: Dragging the selected cells

Checks a plain move: the cells and their outline go together, and it's one undo step.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make the setup's art. Press **M**, then **1**, and drag a rectangle over `abc` only | `abc` is selected; the rest is hatched |
| 2 | Press **V**. Press on an empty cell well away from the selection, drag 2 cells down and 1 cell left, and let go | `abc` moves 2 cells down and 1 left, onto `mno`'s row, covering `mn` and the empty cell before them. Where `abc` was is empty. The outline moves with it. History adds one **Move** step |
| 3 | Press **Ctrl+Z** | `abc` and the outline are back where they started, and the letters they covered are back too |

### T3: Floating over art across several drags

Checks that moved cells don't harm what they pass over, and that several drags are one step.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With `abc` selected and the Move tool chosen, drag the selection onto the patch and let go | `abc` covers part of the patch. History adds **Move** |
| 2 | Drag it again, off the far side of the patch, and let go | The patch is whole again where `abc` was: nothing was lost under it. History still has just one **Move** step, not two |
| 3 | Press the arrow keys a few times | The cells step one cell per press, still over nothing they harm. Still one **Move** step |
| 4 | Drag the selection back onto the patch, let go, and press **Enter** | `abc` covers part of the patch. Enter ended the move, so those cells of the patch are now really replaced. Still one **Move** step |
| 5 | Drag it off the patch again | This is a new move: History adds a second **Move** step. This time the patch has a hole where `abc` sat when you pressed Enter |
| 6 | Press **Ctrl+Z** twice | Everything is back as it was before T3 |

### T4: Empty cells don't cover anything

Checks that empty cells in a selection let the art below show through.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, then **1**, and drag a rectangle from 2 rows above `a` down to `c`, so it takes in `abc` and the empty cells above it | It's selected: 3 cells wide and 3 tall |
| 2 | Press **V** and drag it onto the middle of the patch | Only the letters cover the patch. Where the empty selected cells land, the patch still shows through, though the outline is round them |
| 3 | Press **Ctrl+Z** | The letters are back |

### T5: Esc puts the cells back

Checks that Esc cancels a move with no undo step, and the next Esc picks the Hand tool.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select `abc` and press **V**. Drag it a few cells, let go, and press an arrow key | `abc` has moved. History has a **Move** step |
| 2 | Press **Esc** | `abc` and the outline are back where they started, and History has no **Move** step. The Move tool is still chosen |
| 3 | Press **Esc** again | The Hand tool is chosen |
| 4 | Press **V**, start dragging the selection, and press **Esc** while still holding the button | The cells jump back where they started. Letting go doesn't move them again |

### T6: Arrow keys

Checks the nudges: 1 cell, or 10 with Shift.

**Also on macOS:** Yes, because the keys are read differently there

| # | Do this | Expected result |
|---|---|---|
| 1 | Select `abc` and press **V**. Press **Right** three times | `abc` moves 3 cells right. History has one **Move** step |
| 2 | Press **Shift+Down** | `abc` moves 10 cells down |
| 3 | Press **Shift+Up**, then **Left** three times | `abc` is back where it started, and History has no **Move** step |

### T7: Alt+drag copies

Checks that Alt at the press leaves the cells where they were as well.

**Also on macOS:** Yes, because it's **Option** there

| # | Do this | Expected result |
|---|---|---|
| 1 | Select `abc` and press **V**. Hold **Alt** (**Option** on macOS), drag the selection 4 cells down, and let go | A copy of `abc` is 4 cells down, and the original is still there. The outline is on the copy. History adds **Move** |
| 2 | Hold **Alt** again and drag the copy 2 cells right | A second copy lands, and the first copy stays where it was. History has a second **Move** step |
| 3 | Without Alt, drag it once more | The second copy moves on, and no third copy appears. History still has two **Move** steps |
| 4 | Press **Ctrl+Z** twice | Both copies are gone; only the original `abc` is left |

### T8: Moving the whole layer

Checks a move with nothing selected, and what happens to cells pushed off the canvas.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **Ctrl+D** to deselect, then **V**. Press **Shift+Right** until part of the patch is past the canvas's right edge (use **Right** for the last few cells) | All the art moves: the letters and the patch together. Nothing becomes selected. The part past the edge is out of sight. History has one **Move** step |
| 2 | Press **Shift+Left** a few times | The part that was past the edge comes back, whole |
| 3 | Press **Shift+Right** again until part of the patch is past the edge. Press **Enter**, then **Shift+Left** a few times | The part that was past the edge is gone now: the patch is cut where the edge was |
| 4 | Press **Ctrl+Z** twice | The art is whole again, where it began |

### T9: What ends a move, and what doesn't

Checks that an undo or a layer change ends a move, while saving doesn't.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select `abc` and press **V**. Drag it 2 cells right, then press **Ctrl+Z** | `abc` is back where it started, and History has no **Move** step |
| 2 | Drag it 2 cells right again. Press **Ctrl+S** and save the file anywhere you like | The file saves. History still shows one **Move** step |
| 3 | Drag it 2 more cells right | It moves on, and History still shows one **Move** step: the save didn't end the move |
| 4 | Press **Enter**. In the Layers panel, add a layer, then click back on the first layer's row | The first layer is the active one again |
| 5 | Drag the selection 2 cells right. Click the new layer's row in the Layers panel, click the first layer's row again, and drag the selection 2 more cells right | History adds two **Move** steps, not one: choosing another layer ended the first of them |

### T10: Locked and hidden layers

Checks that the Move tool leaves a layer alone when it can't be changed.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select `abc`. In the Layers panel, tick **Locked** for its layer. Press **V** and drag the selection | Nothing moves, and History adds nothing |
| 2 | Press **Right** | Nothing moves |
| 3 | Untick **Locked**, untick **Visible**, and drag again | Nothing moves |
| 4 | Tick **Visible** again and drag | `abc` moves |

### T11: The window reopens on your second monitor

Checks the fix for your 4.4 report, where a window moved to your second monitor sometimes reopened on the first. Needs two monitors.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the app on your main monitor and not maximized, drag it by its title bar onto your second monitor. Close the app and start it again | It opens on the second monitor, where you left it, at the same size |
| 2 | Drag it back onto your main monitor and maximize it. Press **Win+Shift+Right** (or **Left**, whichever way your second monitor is) to send it to the second monitor maximized. Close the app and start it again | It opens maximized on the second monitor |
| 3 | Click the title bar's restore button (the one between minimize and close) | It becomes a normal window on the second monitor, not on the main one |
| 4 | Maximize it on the main monitor. Drag it by its title bar to the top edge of the second monitor, so Windows maximizes it there. Close the app and start it again | It opens maximized on the second monitor |
| 5 | Repeat 1 to 4 a few times each | It opens on the right monitor every time. If any try fails, please note which row, and whether your two monitors use different scaling in Windows' Display settings |

## Worth poking at (optional)

### P1: A ragged selection

Checks a move of a selection that isn't a rectangle.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **W**. In Tool Options, choose **Global** in Area, then click the patch | The patch's cells are selected |
| 2 | Press **V**, then **Shift+Right** a few times | The patch slides 10 cells at a time with its outline, and the letters stay put |

### P2: A big, full canvas

Checks that moving a whole big layer keeps up with the pointer.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Document → Resize Canvas** and make it 500 × 500. Press **B**, then **1**, set Tool Options' **Size** to **100**, and paint over a large part of it | Lots of art |
| 2 | Press **Ctrl+D**, then **V**, and drag the whole layer around for a while | The art follows the pointer smoothly |
