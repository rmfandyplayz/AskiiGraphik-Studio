<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.2: Ellipse Select, the lassos and combine modes, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 35 min

## What changed

The Select group (**M**) now has four tools: **Rectangle Select**, **Ellipse Select**, the **Lasso** (drag around what you want) and the **Polygonal Lasso** (click corner after corner). Keys **1** to **4** choose them while one of them is chosen.

Each one can now add to the selection, take away from it, or keep only the overlap:
- **Shift** held when you start adds.
- **Alt** (Option on a Mac) subtracts.
- **Both** keep only where the two overlap (intersect).

With no key held, the new **Mode** buttons on Tool Options decide. They're Replace, Add, Subtract and Intersect.

Pressed *during* a Rectangle or Ellipse drag, the same keys do something else: **Shift** makes the box look square (or the ellipse round), and **Alt** draws it from the middle outwards.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, keep the defaults and click **OK**. Choose **Don't Save** if it asks.
3. Press **T**, click near the top-left of the canvas and type a few lines of text, so there's art to see the outline over. Press **Esc**.
4. Choose **View → Zoom In** two or three times, so cells are easy to aim at.
5. Open **Window → History**, so you can see each undo step.

## Known limitations (not bugs)

- The tool buttons and the Mode buttons are plain text; the owner's icons come later.
- The lassos' line and the selection outline are placeholder looks.
- Drawing tools still paint outside the selection. Clipping comes in 4.4.
- On Windows, **Alt+Shift** may switch your keyboard layout if you have more than one installed. That's Windows, not the app.

## Tests

### T1: The four tools

Checks that M and the number keys reach each selection tool, and that the menus and toolbars list them.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M** | The **Select** button on the Tools toolbar is checked, and a small toolbar beside it shows **Rectangle Select**, **Ellipse Select**, **Lasso** and **Polygonal Lasso**, with Rectangle Select checked |
| 2 | Press **2**, then **3**, then **4**, then **1** | Each press checks the next tool on that small toolbar: Ellipse Select, Lasso, Polygonal Lasso, then Rectangle Select again |
| 3 | Open the **Tools** menu | It lists Rectangle Select, Ellipse Select, Lasso and Polygonal Lasso after Hand, with a check mark on Rectangle Select |
| 4 | Press **3** (Lasso), then **B**, then **M** | The Select button brings back the Lasso, the last selection tool used |

### T2: Ellipse Select

Checks the new ellipse tool, including Shift for a circle and Alt for drawing from the middle.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, then **2**. Drag from one cell to another about 10 cells right and 5 down, and hold still | A dashed outline shows the ellipse that fits that box. History has no new step yet |
| 2 | Release | The ellipse is selected. History has one new step, **Ellipse Select**. The window title has no `*` |
| 3 | Start another drag, and while dragging press and hold **Shift** | The ellipse turns into a circle that looks round on screen, at once, without moving the mouse. It's more cells wide than tall, since cells are taller than they're wide |
| 4 | Still dragging, let go of **Shift** | It goes back to the ellipse that fits the box |
| 5 | Still dragging, press and hold **Alt** (Option on a Mac) | The ellipse is now centred on the cell you started on, and grows both ways as you move |
| 6 | Release, then let go of Alt | The centred ellipse is selected. On Windows the menu bar doesn't open |

### T3: Rectangle Select's square and middle

Checks that Shift and Alt pressed during a drag shape the rectangle, and that a key held from the start doesn't.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **1** (Rectangle Select). Drag from a cell, and while dragging press **Shift** | The rectangle becomes one that looks square on screen |
| 2 | Release, and let go of Shift | The square is selected |
| 3 | Start a drag with **Alt** pressed during it, then release | The rectangle is centred on the cell you started on |
| 4 | Click an empty spot (no keys) to deselect. Hold **Shift**, then start a drag and move around | The rectangle follows the mouse as a normal rectangle, not a square: Shift held from the start means Add (see T4) |
| 5 | Still dragging, let go of Shift and press it again | Now it becomes a square |

### T4: Adding, subtracting and intersecting with keys

Checks the keys held when a selection starts.

**Also on macOS:** Yes, because Option takes Alt's place there

| # | Do this | Expected result |
|---|---|---|
| 1 | With Rectangle Select, drag a rectangle on the left of your text | It's selected |
| 2 | Hold **Shift**, drag a second rectangle that doesn't touch the first, release, and let go of Shift | Both rectangles are selected at once, with their own outlines. History has another **Rectangle Select** |
| 3 | Hold **Alt** (Option), drag a rectangle across the middle of the first one, and release | That part is taken out of the selection, leaving a gap |
| 4 | Hold **Shift and Alt** (Shift and Option), drag a rectangle that covers part of the selection and part of empty space, and release | Only the cells that were selected and inside the new rectangle stay selected |
| 5 | Hold **Shift** and click once without dragging | Nothing changes, and History has no new step |
| 6 | Click once without dragging and without keys | Everything is deselected, as before, with a **Deselect** step |
| 7 | Press **Ctrl+Z** (Cmd+Z) a few times | Each undo brings back the selection before that step |

### T5: The Mode buttons

Checks that the Mode buttons on Tool Options pick the mode when no key is held, for all four tools, and are remembered.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With a selection tool chosen, look at the **Tool Options** toolbar | It shows **Mode** with four buttons: Replace, Add, Subtract, Intersect. Replace is down |
| 2 | Click **Add**, then drag two separate rectangles with no keys held | Both stay selected |
| 3 | Press **2** (Ellipse Select) | Tool Options still shows Add down. Dragging an ellipse adds it too |
| 4 | Hold **Alt** and drag an ellipse over part of the selection | That part is taken away, though Add is down. The buttons still show Add |
| 5 | Close the app (**Don't Save**), run it again, and press **M** | The Mode buttons still show Add |
| 6 | Click **Replace** | Back to normal: each drag replaces the selection |

### T6: The Lasso

Checks drawing a freehand selection, including a path that crosses itself.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, then **3**. Press the mouse and draw a rough circle around part of your text, without closing it, and hold still | A black-and-white line follows your path. History has no new step yet |
| 2 | Release | The line goes, and the cells whose middles were inside the shape are selected. The end of the path joined straight back to the start. History has a new **Lasso** step |
| 3 | Draw a figure 8 and release | Both loops of the 8 are selected |
| 4 | Start another path, then press **Esc** before releasing, and release | The line goes and the selection stays as it was. No new step |
| 5 | Click once without dragging | Everything is deselected |

### T7: The Polygonal Lasso

Checks the click-by-click lasso and each way to finish or undo a corner.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, then **4**. Click once on the canvas, then move the mouse away | A line runs from where you clicked to the pointer, and follows it |
| 2 | Click three more places to make four corners | Each click fixes a corner, and the line now runs through them to the pointer |
| 3 | Click the cell of the first corner | The shape closes and its cells are selected. History has a new **Polygonal Lasso** step |
| 4 | Make three corners, and double-click a fourth spot | The double-click adds that corner and finishes. The shape is selected |
| 5 | Make three corners, then press **Enter** | It finishes with those three corners |
| 6 | Make four corners, press **Backspace** once, then press **Enter** | The fourth corner was removed, so the triangle through the first three is selected |
| 7 | Click once, then press **Backspace** | The line goes: removing the only corner cancels. The selection stays as it was |
| 8 | Make three corners, press **Esc** | The line goes, and the selection stays as it was |
| 9 | Make two corners, then press **B** | The Brush is chosen, and the line goes. Nothing was selected |
| 10 | Make two corners. Hold **Space**, drag to pan, let go of Space, then click a third corner and press Enter | Panning works between clicks, and the triangle is selected |

### T8: Alt during a drag doesn't open the menu bar

Checks that Alt pressed in the middle of a drag doesn't trigger the menu bar on its release, as an Alt-click already didn't.

**Also on macOS:** No, Option doesn't open menus on a Mac

| # | Do this | Expected result |
|---|---|---|
| 1 | With Rectangle Select, start a drag, press **Alt** during it, release the mouse, then let go of Alt | The menu bar doesn't open, and its letters aren't underlined |
| 2 | Press and let go of **Alt** on its own | The menu bar opens, as usual. Press **Esc** to close it |

### T9: Tests to revisit

| Step | Test | Where | Why |
|---|---|---|---|
| 4.1 | `step-4.1-follow-ups-2.md`, all of it | Windows, and macOS where marked | not tested yet |
| 4.1 | `step-4.1-selection-mask.md` P3, row 2 | Windows | changed after testing: panning and zooming a big canvas while its outline runs should still be smooth. Optional, as P tests are |

## Worth poking at (optional)

### P1: Big and odd shapes

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **File → New** with a 1000 × 1000 canvas. Zoom out and drag an ellipse over most of it | It previews and selects without a noticeable pause |
| 2 | Draw a Lasso path that goes off the canvas and back | Only the part on the canvas is selected |
| 3 | With Rectangle Select, hold **Shift** and **Alt** during a drag | A square centred on the first cell, with an odd number of cells across and down |
