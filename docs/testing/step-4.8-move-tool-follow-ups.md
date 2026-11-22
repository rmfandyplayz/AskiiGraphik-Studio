<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.8: Move tool follow-ups, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the test marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 15 min

## What changed

With the **Move tool**, clicking a cell outside the selection now deselects, as Rectangle Select's click does. While you drag, Shift keeps the move straight across or straight up and down. Ctrl (Cmd on a Mac) also allows diagonals that look like 45°, and Ctrl+Shift diagonals of one column per row. Moving the whole layer now selects it. In the **Layers** window, arrow keys no longer end a rename.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, leave the size as it is, and click **OK**. Choose **Don't Save** if it asks.
3. Press **T** for the Text tool, click near the top left of the canvas and type three lines of letters, such as `ABCDEFGH`, pressing **Enter** after each. Press **Esc** twice to leave the Text tool.
4. Open **Window → History**, so you can see the undo steps.

## Known limitations (not bugs)

- **The canvas edge:** a selection still can't go past the canvas edge, and its outline is cut where it meets the edge. That's step 4.9.
- **Placeholders:** the choices listed in `docs/TEMPORARY-UI.md` under "Move tool follow-ups", such as how the direction is judged and what Alt+click does.

## Tests

### T1: Clicking outside the selection deselects

Checks that a Move-tool click away from the selection clears it, and that drags still move it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, then drag a box round the middle few letters of the top two lines. Press **V** for the Move tool | Those letters are selected: a moving dashed outline round them, hatching everywhere else |
| 2 | Press the mouse button on a letter outside the selection and keep it held, without moving | Nothing changes yet |
| 3 | Let go | The outline and hatching go: nothing is selected. History's newest step is **Deselect**. The letters haven't moved |
| 4 | Press **Ctrl+Z** | The selection is back |
| 5 | Click once inside the selection | Nothing changes, and History gets no new step |
| 6 | Press on a cell outside the selection and drag a few cells to the right before letting go | The selected letters move with the pointer, and stay selected |
| 7 | Press **Ctrl+Z**. Then press on a cell outside the selection, move the pointer one cell over and back again, and let go | This counts as a drag, not a click: the selection stays, and the letters are where they were |

### T2: Clicking away ends a move

Checks that a click outside the selection also finishes a move under way.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With some letters selected and the Move tool, drag them a few cells to the right | They move. History's newest step is **Move** |
| 2 | Click once on a cell outside the selection | The letters stay where you moved them, now with nothing selected. History shows **Move** then **Deselect** |
| 3 | Press **Ctrl+Z** | The selection comes back where you moved it |
| 4 | Press **Ctrl+Z** again | The letters go back to where they started |

### T3: Shift keeps a drag straight

Checks dragging with Shift, and Shift pressed or let go mid-drag.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select a few letters. Hold **Shift** and drag to the right and a little down | The letters move only sideways, at their own height |
| 2 | Still holding Shift and the button, move the pointer well down, so it has gone further down than across as it looks on screen | The letters switch to moving straight down, in their own columns |
| 3 | Let go. Without Shift, drag the letters diagonally, and while still dragging press **Shift**, without moving the mouse | The letters jump at once onto the straight line nearer to where the pointer is |
| 4 | Let go of **Shift**, still without moving the mouse | The letters jump back under the pointer. Then let go of the button |
| 5 | Press **Shift+Right** a few times, without dragging | Each press moves the letters 10 cells right, as before |

### T4: Ctrl and Ctrl+Shift allow diagonals

Checks the two kinds of diagonal. On a Mac, use **Cmd** instead of Ctrl.

**Also on macOS:** Yes, because Ctrl becomes Cmd there

| # | Do this | Expected result |
|---|---|---|
| 1 | Select a few letters. Hold **Ctrl** and drag down and to the right at about 45° on screen | The letters move along a line that looks like 45°: about two columns for every row |
| 2 | Still holding Ctrl and the button, move the pointer to go mostly sideways, then mostly up or down | The letters move straight sideways, then straight up or down |
| 3 | Let go. Hold **Ctrl+Shift** and drag down and to the right at about 45° | The letters move one column for every row, so their line looks steeper than in step 1 |
| 4 | Drag diagonally without keys, and while still dragging press **Ctrl**, then let go of it, without moving the mouse | The letters jump onto the nearest allowed direction at once, then back under the pointer |
| 5 | Let go of the button. Then Ctrl+drag again, starting somewhere else | The direction is judged from where this new drag started |

### T5: Moving the whole layer selects it

Checks that a drag with nothing selected selects what it moves.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Select → Deselect**. With the Move tool, drag the art a few cells down and to the right | All of the art moves, and an outline appears round where the whole layer now is, with hatching over the strips it left along the top and left |
| 2 | Press **Esc** | The art goes back, and nothing is selected |
| 3 | Drag the art again, then press **Ctrl+Z** | The art goes back and nothing is selected. That was one **Move** step |
| 4 | Drag the art again and press **Enter**. Then drag from anywhere inside the outline | The selected area moves, as any selection does |

### T6: Arrow keys while renaming a layer

Checks that the rename box keeps arrow keys it can't use.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | In the **Layers** window, click **New** twice, then click the middle layer. Double-click its name | A box opens with the name selected |
| 2 | Press **Up**, then **Down** | Nothing happens: the box stays open and the same layer stays highlighted |
| 3 | Press **Home** then **Left**, then **End** then **Right** | The cursor goes to each end, and the arrows past the ends do nothing. The box stays open |
| 4 | Press **Left** twice | The cursor moves two characters left, as usual |
| 5 | Type a new name and press **Enter** | The layer has the new name |

## Worth poking at (optional)

### P1: Alt+click and locked layers

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select some letters. With the Move tool, hold **Alt** and click a cell outside the selection | Nothing is selected afterwards, as with a plain click |
| 2 | Select some letters, lock the layer in the Layers window, and click a cell outside the selection | Nothing happens: on a locked layer the Move tool does nothing |
