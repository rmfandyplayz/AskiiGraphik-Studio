<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 5.5: Curve, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 30 min

## What changed

The Shapes group has a **Curve** tool on **2**, which works like a pen tool. Each click places an anchor, and the curve runs smoothly through them. Dragging as you place an anchor pulls out its handles, and **Alt+click** places a sharp corner. Until the curve is finished, you can drag its anchors and handles to reshape it. **Ctrl+Z** (Cmd+Z on a Mac) takes the last anchor away, as Backspace does, and now does the same in the Polygonal Lasso. Rectangle and Ellipse have moved to **3** and **4**.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Start a new document, and zoom in until the cells are big enough to click a chosen cell easily.
3. Open **Window → History** if it isn't open already.
4. In these tests:
   - an **anchor** is a point the curve passes through, shown by a small dot, like the Polygonal Lasso's corners;
   - a **handle** is a line out of an anchor ending in a small square, which pulls the curve towards it;
   - "click a cell" means click once without moving the mouse.

## Known limitations (not bugs)

- **A finished curve can't be edited.** Its anchors and handles are gone once it's finished, and it's ordinary cells from then on.
- **Shift does nothing while dragging an anchor.** It keeps a new anchor in line, and a handle to 45°.
- **The newest anchor's handles move as the pointer moves.** The piece of curve to the pointer counts as its next neighbour, so they show what a click would make.
- **Include joins shows in Single character too,** where it does nothing, as for the Line tool.
- **Placeholders:** the handles' look, the tool's name, and how near a click must be to grab an anchor or a handle. See `docs/TEMPORARY-UI.md`, "The Curve tool".

## Tests

### T1: The Curve tool and the shape keys

This checks where the Curve tool sits and which options it has.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click the canvas once, then press **U** and **2** | The Shapes sub-toolbar shows **Line**, **Curve**, **Rectangle**, **Ellipse**, and **Curve** is chosen |
| 2 | Look at Tool Options | **Style**, then the **Include joins** button, then **Character** |
| 3 | Open the **Style** dropdown | **Standard**, **Standard by step**, **Precise**, **Single character**: the same four as the Line tool |
| 4 | Press **3**, then **4** | **3** chooses Rectangle and **4** chooses Ellipse |
| 5 | Open the **Tools** menu | **Curve** is listed between **Line** and **Rectangle** |

### T2: Placing anchors with clicks

This checks that clicks build a smooth curve, drawn as you go, that finishes as one step.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose the Curve tool (**U**, **2**) and click a cell near the bottom left | A dot appears in the middle of that cell. Nothing is drawn yet |
| 2 | Move the mouse away without clicking | A line of characters follows the pointer from the dot to the pointer's cell |
| 3 | Click a cell near the top middle | A second dot. The line now ends there, and the first dot becomes the bigger finishing marker |
| 4 | Click a cell near the bottom right | A third dot. The characters make a smooth arch through all three dots, with no `+` at the top |
| 5 | Look at the History window | One **Curve** step, however many times you clicked or moved |
| 6 | Press **Enter** | The dots go away and the arch stays, still one **Curve** step |
| 7 | Press **Ctrl+Z** (Cmd+Z) | The whole curve goes away at once |

### T3: Dragging out handles, and Alt

This checks pulling out handles as you place an anchor, and the Alt modifier for corners and unlinking.

**Also on macOS:** Yes, because Alt is Option there

| # | Do this | Expected result |
|---|---|---|
| 1 | Click a cell on the left. Then press on a cell a few cells to the right and, still holding, drag up and to the right before letting go | Two handles come out of the second anchor, in a straight line through it, the second pointing the opposite way to the first. The curve bends to meet the anchor along that line |
| 2 | Press **Esc** to throw the curve away. Click a cell, then hold **Alt** and click a cell to the right, then (without Alt) click a cell below that one, and press **Enter** | A sharp corner at the middle anchor, drawn as `+`, with straight lines to the other two |
| 3 | Do the same without Alt | The curve rounds that anchor: no `+` there |
| 4 | Click a cell, then press on another cell, drag out its handles, and while still dragging hold **Alt** and move further | Only the handle under the pointer moves. The other one stays where it was |
| 5 | Press **Esc**. Click a cell, then hold **Alt**, press on another cell and drag | Only one handle comes out, on the side you drag towards |

### T4: Shift

This checks Shift for keeping a new anchor in line, and a handle to 45° steps.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click a cell, then hold **Shift** and move the pointer a little above or below the cell's row, far to the right | The piece of curve to the pointer stays flat, in the first cell's row |
| 2 | Still holding **Shift**, click | The new anchor goes in that row, not where the pointer is |
| 3 | Press on another cell and drag out handles while holding **Shift**, moving the pointer round in a circle | The handles snap to across, upright and the four diagonals that look like 45° on screen |

### T5: Editing before it's finished

This checks dragging anchors and handles, and which handles show strongly.

**Also on macOS:** Yes, because Alt is Option there

| # | Do this | Expected result |
|---|---|---|
| 1 | Click four cells in a zigzag, without finishing | Four dots. The last anchor's handles show strongly, and the others' faintly |
| 2 | Move the pointer onto the second anchor's dot | Its handles show strongly while the pointer is on it, and the piece of curve to the pointer goes away |
| 3 | Press on the second anchor and drag it a few cells | The anchor follows the pointer from cell to cell, and the curve reshapes round it |
| 4 | Let go and move the pointer away | The second anchor's handles still show strongly: it's now the selected anchor |
| 5 | Press on the end of one of its handles and drag it | That handle follows the pointer and the other turns with it, always pointing the opposite way. The curve on both sides changes |
| 6 | Hold **Alt** and drag the same handle again | Only that handle moves. Let go of Alt and drag it again: the two stay apart from now on |

### T6: Finishing, closing and throwing away

This checks every way a curve ends.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click two cells, then double-click a third | The curve finishes through all three: the dots go away |
| 2 | Click three cells, then move the pointer onto the first dot | The first dot lights up |
| 3 | Click it | The curve closes into a loop and finishes. The History window has one **Curve** step for it |
| 4 | Click two cells, then press **B** for the Brush | The Brush is chosen and the curve stays as it was, without the piece that followed the pointer |
| 5 | Press **U**, click two cells, then press **Esc** | The whole curve goes away, and the History window has no new step. The Curve tool is still chosen |
| 6 | Press **Esc** again | The Hand tool is chosen, as Esc does when nothing is being drawn |
| 7 | Press **U**, click one cell and press **Enter** | Nothing is drawn, and the History window has no new step |

### T7: Backspace and Ctrl+Z while drawing

This checks that Ctrl+Z takes the last anchor or corner away while a shape is being drawn, in the Curve and the Polygonal Lasso.

**Also on macOS:** Yes, because the shortcut is Cmd+Z there

| # | Do this | Expected result |
|---|---|---|
| 1 | Draw something with the Brush, so the History window has a step. Then choose the Curve tool and click four cells | Four dots |
| 2 | Press **Backspace** | The last dot goes away, and the piece of curve to the pointer now starts at the third |
| 3 | Press **Ctrl+Z** (Cmd+Z) | The third dot goes away the same way. The Brush stroke is still there |
| 4 | Open the **Edit** menu | **Undo** is available and **Redo** is greyed out |
| 5 | Press **Ctrl+Z** twice more | The last two dots go away. The curve is gone, and the Brush stroke is still there |
| 6 | Press **Ctrl+Z** once more | Now the Brush stroke is undone, as usual |
| 7 | Choose the Polygonal Lasso (**M**, then **4**), click four points, and press **Ctrl+Z** | The last corner goes away, as Backspace does. Nothing else is undone |
| 8 | Press **Ctrl+Z** until the last corner is gone, then once more | The shape is gone, then the next Ctrl+Z undoes the last step in the History window as usual |

### T8: Style, joins and the character

This checks that the curve draws as the Line tool does.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Set **Style** to **Precise** and draw a curve through four cells | The curve is drawn in Precise's characters |
| 2 | Click two cells without finishing, then change **Style** to **Standard by step** and move the pointer back over the canvas | The curve redraws at once in the new style |
| 3 | Press **Enter**. With the Line tool, draw an upright line. Turn **Include joins** on, then with the Curve tool draw a flat curve across that line and finish it | Where the curve crosses the line it makes `+` |
| 4 | Pick a character in the **Characters** window while the Curve tool is chosen | The Curve tool stays chosen and its **Style** becomes **Single character** |
| 5 | Draw a curve | It's drawn in that character |

## Worth poking at (optional)

### P1: Zoom

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click a few anchors with handles, then zoom out a long way and back in | The dots and the handles' squares stay the same size on screen |
| 2 | Zoomed far out, try to grab a short handle | Its anchor may be grabbed instead, as an anchor comes first. Zoom in to grab the handle |

### P2: The History window during a curve

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click three cells, then click an older step in the History window, and move the pointer back over the canvas | The curve ends where it was: its dots go away, and the History window's undo leaves it as it says |

### P3: A selection, and panning

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select a rectangle, then draw a curve that runs in and out of it | Only the cells inside the selection are drawn |
| 2 | During a curve, hold **Space** and drag to pan, then let go and click another cell | The view pans, and the click places the next anchor as usual |
