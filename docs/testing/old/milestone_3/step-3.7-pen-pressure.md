<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 3.7: Pen pressure, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 25 min

## What changed

A drawing tablet's pen now presses: the harder you press, the more strongly the **Brush** and the **Soft Eraser** cover each cell. A light touch moves cells only part of the way towards the Brush's Intensity (or towards empty, for the Soft Eraser), so it never wipes out what's there. A mouse always presses fully, so mouse painting hasn't changed.

Without a tablet, **Debug → Mouse Acts as a Pen at Half Pressure** makes the mouse press like a pen at half pressure. Every test below uses it.

The Standard ramp goes from light to dark: `.` `:` `-` `=` `+` `*` `#` `%` `@`. Half of full darkness is `+`.

## Setup

1. Run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**. Under **Characters** choose **Unicode (any character)**, and under **Colors** choose **ANSI (the 16 standard colors)**. Click **OK**, and choose **Don't Save** if it asks.
3. Choose **View → Zoom In** two or three times, so each character is easy to read.
4. Open **Window → History**, so you can see each undo step.
5. Press **B** then **1** for the Brush. On Tool Options set **Size** to **1**, **Ramp** to **Standard**, **Intensity** to **100%** and **Softness** to **0%**.

## Known limitations (not bugs)

- A real tablet isn't checked until M10. The Debug toggle stands in for a pen at one fixed pressure, so you can't vary the pressure during a stroke.
- The Debug toggle is off at every start and isn't remembered.
- There's no setting for pressure yet: no switch, no curve, no choice of what it controls.
- Only the Brush and the Soft Eraser follow pressure. A pen's eraser end paints like its tip.

## Tests

### T1: The Debug toggle

Checks that the Debug menu has the toggle, and that it ticks and unticks.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Debug** menu | **Mouse Acts as a Pen at Half Pressure** is listed just after **Show Frame Rate**, with no check mark |
| 2 | Choose it, then open the **Debug** menu again | It has a check mark |
| 3 | Choose it again, then open the **Debug** menu | The check mark is gone. Close the menu, and leave the toggle unticked |

### T2: Half pressure on an empty canvas

Checks that half pressure paints half as dark, and that the mouse still paints at full strength when the toggle is off.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the toggle **unticked**, click an empty cell near the top left of the canvas | `@` |
| 2 | Tick **Debug → Mouse Acts as a Pen at Half Pressure**, and click an empty cell two to the right | `+`, the middle of the ramp. An `@` here would mean pressure isn't reaching the Brush |
| 3 | Set **Intensity** to **50%**, and click another empty cell two to the right | `:`, half of 50%: about a quarter of the way to `@` |
| 4 | Untick the toggle, and click another empty cell two to the right (Intensity is still 50%) | `+`, the same as row 2: on an empty cell, half pressure looks like half the intensity |
| 5 | Set **Intensity** back to **100%** | |

### T3: A light touch doesn't erase

Checks the choice you made for this step: pressure changes how strongly the Brush covers a cell, not how dark it paints, so pressing lightly over dark characters doesn't lighten them.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the toggle **unticked** and **Intensity 100%**, drag along an empty row for about 10 cells | A row of `@` |
| 2 | Tick the toggle, and drag along the same row again | The row stays `@`. History gets no new step, since nothing changed. If the row turned `+`, a light touch would be erasing, which is the behaviour you turned down |
| 3 | Set **Intensity** to **0%**. With the toggle still ticked, drag along the left half of the row | Those cells turn `+`: half pressure moves them only halfway towards empty |
| 4 | Untick the toggle, and drag along the right half of the row (Intensity is still 0%) | Those cells empty completely |
| 5 | Set **Intensity** back to **100%** | |

### T4: Pressing again

Checks that going over a cell again in the same stroke adds nothing, and that a new stroke takes it further.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Tick the toggle. With **Intensity 100%**, click an empty cell | `+` |
| 2 | Click the same cell again | `#`: the second stroke goes half of the rest of the way |
| 3 | On an empty row, press and drag right across 5 cells, back left over them, and right again, then let go | All 5 cells are `+`, none darker: going over a cell again in one stroke adds nothing |
| 4 | Look at History | Row 3 added a single **Paint** step |

### T5: The Soft Eraser at half pressure

Checks that the Soft Eraser follows pressure too, and that even a light, weak stroke lightens at least one level, as you chose after 3.4.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Untick the toggle. With the Brush at **Intensity 100%**, drag along an empty row for about 10 cells | A row of `@` |
| 2 | Press **E** then **2** for the Soft Eraser. On Tool Options set **Size** to **1**, **Strength** to **100%** and **Softness** to **0%** | The outline under the pointer is one cell |
| 3 | Tick the toggle, and click the first `@` of the row | It turns `+`: halfway to empty |
| 4 | Untick the toggle, and click the second `@` | It empties |
| 5 | Tick the toggle, set **Strength** to **10%**, and click the third `@` | It turns `%`, one level lighter. Half of 10% would round back to `@`, so this is the one level every stroke takes |

### T6: Tools that ignore pressure

Checks that the hard-edged tools work as before while the mouse acts as a pen.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Keep the toggle ticked. Press **E** then **1** for the Eraser, set **Size** to **1**, and click one of the remaining `@` in T5's row | It empties completely |
| 2 | Press **B** then **4** for the Character Brush, set **Size** to **1**, and click an empty cell | The Character Brush's character appears, the same as with the toggle off |
| 3 | Untick the toggle | |

### T7: A confirmation keeps the keyboard

In 3.6's T7, pressing Esc while Delete's confirmation was open chose the Hand tool, because the keys went to the canvas behind the dialog. This checks the fix: while a confirmation is open, keys go to it, and afterwards they go back to the canvas without clicking it.

**Also on macOS:** Yes, because dialogs and keyboard focus work differently there

| # | Do this | Expected result |
|---|---|---|
| 1 | Open **Window → Characters**. Type `abc` in the box at the bottom and click **Add** | A set named **New set** (or **New set 2** and so on) is added and chosen |
| 2 | Click `a` in the grid | The Tools toolbar has the **Brush** group chosen, with **Character Brush** pressed on its sub-toolbar |
| 3 | Click **Delete** | A dialog titled **Delete Character Set** opens |
| 4 | Press **E** | Nothing changes behind the dialog: the Tools toolbar still has the Brush group chosen, not the Eraser group |
| 5 | Press **Esc** | The dialog closes and the set is still there. The Tools toolbar still has the **Brush** group chosen, with **Character Brush** pressed. The **Hand** tool being chosen here would be the old bug |
| 6 | Without clicking anything, press **E** | The **Eraser** group is chosen: typing went back to the canvas |
| 7 | Open **Window → Ramps**, choose **Standard** and click **Duplicate**, then click **Delete** | A dialog titled **Delete Ramp** opens |
| 8 | Press **Esc** | The dialog closes and **Standard copy** (or **Standard copy 2**, if a copy was left from earlier) is still there. The Tools toolbar still has the **Eraser** group chosen, not the Hand |
| 9 | Delete that ramp and the character set from row 1, confirming each with the dialog's **Delete** button | Both are gone |

## Worth poking at (optional)

### P1: A soft edge at half pressure

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose the Brush with **Size 7**, **Softness 100%** and **Intensity 100%**. Untick the toggle and click an empty area | A round patch, `@` in the middle, fading to light characters at the edge |
| 2 | Tick the toggle and click another empty area | The same shape, but lighter all over: `+` in the middle, and the faintest edge cells may stay empty |

### P2: Everything else with the toggle on

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the toggle ticked, type with the Text tool, pan with the Hand tool, and paint colour with the FG and BG Brushes | All of them work as they do with the toggle off |
