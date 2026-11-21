<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.2c: Smooth hatching, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the checks marked for it (Linux is covered by CI only)
**Time needed:** about 15 min

## What changed

The hatching used to jump a whole screen pixel at a time. Now it glides, an eighth of a pixel at a time, and its lines are twice as thick and twice as far apart. White, red and blue hatching all move at the outline's dash speed, which is how fast the red hatching moved before.

An undo or redo while you're using a selection tool now shows straight away, without moving the mouse.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, keep the defaults and click **OK**. Choose **Don't Save** if it asks.
3. Press **T**, click near the top-left of the canvas and type a few lines of text, so there's art under the hatching. Press **Esc**.
4. Choose **View → Zoom In** two or three times.
5. Open **Window → History**, so you can see each undo step.

## Known limitations (not bugs)

- **Placeholders:** all the colours, sizes and speeds are placeholders, listed in `docs/TEMPORARY-UI.md` under "Selection overlay".
- **Alternating lines:** the alternating dark and light lines you asked for wait until you've confirmed the flicker is gone.
- **Repaint rate:** while anything is selected, the canvas now repaints on nearly every screen refresh, which is what lets the hatching glide. M10's profiling pass will check what that costs.

## Visual tests (poke around and try these)

- **Flicker.** Select something, then look a little away from the canvas, so the hatching is at the edge of your vision, where you saw the flicker before. It should glide without flickering.
  - Check both dark and light mode, on Windows and on the Mac.
  - Check the red hatching (drag with **Alt**) and the blue (drag with **Shift+Alt**) too.
- **Speed:** white, red and blue hatching all move at the same speed, as fast as the red moved before.
- **Thickness:** the lines are about twice as thick and twice as far apart as before, and they stay that size at every zoom.
- **If it still flickers (optional comparisons).** For each one: close the app, change the value in `src/AskiiArt.App/Theme/Tokens.axaml`, run the app again, then put the value back afterwards.
  - **Thick lines that jump:** set `AppCanvasSelectionHatchStepsPerPixel` to `1`. The lines jump a whole pixel at a time, as before. If this flickers and the glide doesn't, the jumps were the cause.
  - **Thin lines that glide:** set `AppCanvasSelectionHatchThickness` to `1` and `AppCanvasSelectionHatchPeriod` to `8`. These are the old thin lines, gliding. If this flickers too, thin lines play a part.

## Tests

### T1: An undo during a Lasso drag shows at once

Checks the bug you found: an undo in the middle of a lasso drag now shows straight away.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, then **1**. Drag a rectangle over part of the art, then choose **Select → Deselect** | Nothing is selected, and History ends with **Deselect** |
| 2 | Press **3** (Lasso). Hold **Shift** and drag a loop somewhere else. Keep holding the mouse button | Your loop's preview shows |
| 3 | Still holding the button, and keeping the mouse still, press **Ctrl+Z** | Straight away, the rectangle is back, previewed together with your loop, with hatching outside both |
| 4 | Let go | The rectangle and the loop are both selected. History adds **Lasso** |

### T2: An undo during a Polygonal Lasso shape shows at once

Checks the same thing with the Polygonal Lasso.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **1**. Drag a rectangle, then choose **Select → Deselect** | Nothing is selected |
| 2 | Press **4** (Polygonal Lasso). Hold **Shift** and click a corner away from the rectangle, then click two more corners. Move the pointer a little | The shape's preview shows |
| 3 | Without moving the mouse, press **Ctrl+Z** | Straight away, the rectangle is back, previewed together with the shape |
| 4 | Click the first corner's marker | The rectangle and the shape are both selected. History adds **Polygonal Lasso** |

### T3: An undo during a Rectangle Select drag shows at once

Checks the same thing with Rectangle Select.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **1**. Drag a rectangle, then choose **Select → Deselect** | Nothing is selected |
| 2 | Hold **Shift** and drag another rectangle elsewhere. Keep holding the mouse button and keep the mouse still, then press **Ctrl+Z** | Straight away, the first rectangle is back, previewed together with the one you're dragging |
| 3 | Let go | Both rectangles are selected. History adds **Rectangle Select** |

### T4: An undo after a cancelled drag

Checks that a cancelled gesture doesn't come back with an undo.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **1**. Drag a rectangle, then choose **Select → Deselect** | Nothing is selected |
| 2 | Press **3** (Lasso). Drag a loop, and before letting go press **Esc**. Then let go | The loop disappears, and nothing is selected |
| 3 | Press **Ctrl+Z** | The rectangle is selected again. No loop or preview appears |

### T5: Tests to revisit

| Step | Test | Where | Why |
|---|---|---|---|
| 4.2b | P1 | Windows | The canvas now repaints on nearly every screen refresh while something is selected, so check that a big selection still moves smoothly |

## Worth poking at (optional)

### P1: What an idle selection costs

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select something, leave the app alone, and open Task Manager | AskiiGraphiks Studio uses a little CPU while the hatching moves. Note the number: it gives M10's profiling pass a starting point |
| 2 | Choose **Select → Deselect** and wait a few seconds | Its CPU use drops to about nothing, because nothing moves |
