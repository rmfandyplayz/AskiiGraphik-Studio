<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.2b: Selection overlay, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 30 min

## What changed

Everything that isn't selected is now covered with faint diagonal lines ("hatching") that drift slowly. While you use any selection tool, the canvas shows exactly what letting go would leave. **Subtract** shows in red and **Intersect** in blue. In both, the cells about to be deselected get hatching of their own, slanted the other way.

The lassos show a faint line where they would close. The Polygonal Lasso has markers on its corners, and a click on the first corner's marker finishes it. On a Mac, a window closed full screen opens full screen again.

This doc covers the changes from your visual check too: the moving hatching, the grid fading under it, the outline strengths, and Intersect off the canvas.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, keep the defaults and click **OK**. Choose **Don't Save** if it asks.
3. Press **T**, click near the top-left of the canvas and type a few lines of text, so there's art under the hatching. Press **Esc**.
4. Choose **View → Zoom In** two or three times, so cells are easy to aim at.
5. Open **Window → History**, so you can see each undo step.

## Known limitations (not bugs)

- All the colours, sizes, opacities and speeds are placeholders, listed in `docs/TEMPORARY-UI.md` under "Selection overlay".
- The lassos' own line stays black on white in every mode. Only the outlines and hatching take Subtract's and Intersect's colours.
- Windows has no full-screen command yet, so T7 is for macOS only.

## Visual tests (poke around and try these)

Quick checks of the changes from your visual test. Each is what to try and what you should see.

- **Fainter closing line:** draw with either lasso. The dashed line from the pointer back to the start is half as strong as before.
- **Three outline strengths.** Make a selection, then drag a shape over part of it in each mode:
  - Lines that will be the selection's outline once you let go are bright and running.
  - The shape's other lines are faint.
  - The old selection's lines that will go are fainter still, at about 40% of the faint ones.
  - In **Replace** and **Intersect**, the old outline now shows very faintly. Before, it disappeared.
- **Moving hatching:**
  - The white hatching (over what isn't selected) drifts slowly towards the top left, at a bit over half the speed of the outline's dashes.
  - **Subtract (Alt):** only the red hatching moves, towards the bottom left, as fast as the dashes. The white hatching stops, then carries on from where it stopped when you let go.
  - **Intersect (Shift+Alt):** only the blue hatching moves, towards the bottom left, at the white hatching's speed. The white hatching stops.
- **Grid under hatching:** turn on **View → Show Grid**. Under white hatching the grid shows at about 40% of its usual strength, and under red or blue hatching at about 70%. Inside the selection it's as strong as usual.
- **White hatching stays during Intersect:** start an Intersect drag over cells outside the selection only. The white hatching stays on every cell except the blue ones; before, it disappeared.
- **Intersect off the canvas:** an Intersect drag that starts and ends on the grey area outside the canvas looks the same as one over unselected cells: the old selection is hatched blue and the white hatching covers the rest.

## Tests

### T1: The hatching follows the art and keeps its size

Checks that the hatching appears over what isn't selected, scrolls with the art, and looks the same at every zoom.

**Also on macOS:** Yes, because a Retina screen has twice the pixels. The lines should be just as thin and as far apart as on Windows.

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M** and drag a rectangle over the middle of the art | Every cell outside the rectangle has thin diagonal lines rising to the right, a dark one beside a light one. Cells inside have none |
| 2 | Watch the hatching for a few seconds | It drifts slowly towards the top left |
| 3 | Scroll the canvas with the mouse wheel | The hatching scrolls with the art. Apart from its own slow drift, it doesn't slide across the art |
| 4 | Choose **View → Zoom In** twice, then **View → Zoom Out** four times | The lines stay just as thin and as far apart on screen at every zoom. They don't line up with the cells |
| 5 | Choose **Select → Deselect** | The hatching and the outline go |

### T2: The preview is what you get

Checks that, for each mode, what the canvas shows during a drag is exactly what's selected when you let go.

**Also on macOS:** Yes, because the mode keys are Shift and Option there

| # | Do this | Expected result |
|---|---|---|
| 1 | With Rectangle Select, drag a rectangle. Then hold **Shift** and drag another that overlaps it. Before letting go, note the bright outline | While dragging, the bright outline runs round both rectangles together. Lines inside them are faint. After letting go, the outline is exactly where the bright one was |
| 2 | Hold **Alt** (Option on a Mac) and drag over part of the selection. Before letting go, note the red hatching | The cells being removed are hatched red, slanted the other way, and the red outline runs round your rectangle. After letting go, exactly those cells are deselected |
| 3 | Hold **Shift+Alt** and drag over part of the selection | What stays has a running blue outline, and the selected cells outside your rectangle are hatched blue. After letting go, only the blue-outlined part is selected |
| 4 | Press **M**, then **3** (Lasso), hold **Shift** and drag a loop | While dragging, the result is previewed the same way. Letting go selects what the preview showed |

### T3: Intersect over unselected cells and off the canvas

Checks that Intersect leaves nothing when your shape misses the selection, wherever you draw it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, then **1**. Drag a rectangle over part of the art | It's selected |
| 2 | Hold **Shift+Alt** and drag a rectangle over cells outside the selection only, then let go | While dragging, the whole selection is hatched blue and white hatching covers everything else. After letting go nothing is selected, and History adds **Deselect** |
| 3 | Choose **Edit → Undo**, then hold **Shift+Alt** and drag between two spots on the grey area to the right of the canvas, and let go | The same as row 2: blue hatching while dragging, nothing selected after, and a **Deselect** step |
| 4 | Undo again. Hold **Shift** and drag between two spots to the right of the canvas, then let go | The selection doesn't change, and History adds no step |
| 5 | Hold **Alt** and drag between two spots to the right of the canvas, then let go | The selection doesn't change, and History adds no step |

### T4: A click still leaves the selection alone

Checks that a click, which makes no shape, still changes nothing outside Replace.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With a selection showing and Rectangle Select chosen, hold **Shift+Alt** and click a cell without dragging | Nothing changes, and History adds no step |
| 2 | Press **3** (Lasso), hold **Shift+Alt** and click without dragging | Nothing changes |
| 3 | Press **4** (Polygonal Lasso). Hold **Shift+Alt** and click once, click a second spot, then press **Enter** | A shape of two corners counts as a click: the selection doesn't change |
| 4 | With no key held, click once with the Lasso | Everything is deselected, as a click in Replace always did |

### T5: The Polygonal Lasso's markers

Checks the corner markers and finishing on the first one.

**Also on macOS:** Yes, because a Retina screen has twice the pixels. The markers should be the same size on screen as on Windows.

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **View → Zoom In** until a cell is several times wider than the first corner's disc will be (about the size of a pea). Press **M**, then **4**. Click the first corner just inside the right edge of a cell, then two more corners elsewhere | The first corner has a white disc with a black ring and dot, poking into the next cell to the right. The others have small white dots ringed in black, slightly faded |
| 2 | Move the pointer onto the first corner's disc | The disc turns black with a white ring and dot, and the line snaps to its middle |
| 3 | Click the part of the disc that pokes into the next cell | The shape finishes and is selected. History adds **Polygonal Lasso** |
| 4 | Start a new shape, with its first corner in the middle of a cell, and add two more corners. Click inside the first corner's cell but well away from its disc | A fourth corner is added. The shape doesn't finish |
| 5 | Press **Esc**. Zoom out two steps, then make three corners | The markers are the same size on screen as before |

### T6: The lassos' closing line

Checks the faint line showing where each lasso would close.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **3** (Lasso) and drag a curve without letting go | A faint dashed line runs from the pointer straight back to where you started |
| 2 | Let go | The line goes, and what the loop went round is selected |
| 3 | Press **4** (Polygonal Lasso), click three corners, then move the pointer away | A faint dashed line runs from the pointer back to the first corner |
| 4 | Move the pointer onto the first corner's disc | The faint line goes: the shape's own line now ends at the first corner |

### T7: A window closed full screen opens full screen

Checks that full screen is remembered with the window's place.

**Also on macOS:** Yes, and only there: Windows has no full-screen command yet

| # | Do this | Expected result |
|---|---|---|
| 1 | Click the window's green button to go full screen, then quit with **Cmd+Q** (**Don't Save** if it asks) | The app closes |
| 2 | Run the app again | It opens full screen |
| 3 | Leave full screen, quit, and run it again | It opens in a normal window, where it was before going full screen |

### T8: Tests to revisit

| Step | Test | Where | Why |
|---|---|---|---|
| 4.2 | T7, row 3 | Windows | changed after testing: the shape now finishes on a click on the first corner's marker, not anywhere in its cell. T5 here covers it, so there's no need to redo 4.2's row |

## Worth poking at (optional)

### P1: A big selection

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make a big canvas (**File → New**, 1000 × 1000), choose **Select → Select All**, then drag a rectangle with **Alt** over most of it | The red hatching and the outline move smoothly, with no stutter |
| 2 | Zoom all the way out and pan around while something is selected | Panning stays smooth while the hatching moves |

### P2: Hatching on dark art

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Paint some cells with a dark background colour, then select a rectangle beside them | The hatching shows on the dark cells as well as the light ones, because each line has a light line beside it |
