<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 3.4: Soft eraser, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 30 min

## What changed

The Eraser is now a **tool group**, like the Brush group. It holds three erasers, on its own small toolbar and on keys 1–3:

- the **Eraser**, which empties whole cells, backgrounds included, as before
- the new **Soft Eraser**, which lightens characters along the ramp, one step at a time, so `@` becomes `#`, then `+`, and so on, until the cell is empty
- the new **BG Eraser**, which removes background colours and leaves the characters

E brings back whichever eraser you used last. The three erasers share one size and shape, separate from the brushes'.

On Tool Options, the Soft Eraser has the same **Ramp** dropdown as the Brush. It's one setting, so changing it with either tool changes it for both. It also has:

- **Strength**: how far one stroke lightens the middle of the outline. At 100% the middle empties.
- **Softness**: how much of the outline's edge fades, as for the Brush, but set separately.

## Setup

1. Run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**. Under **Characters** choose **Unicode (any character)**, and under **Colors** choose **ANSI (the 16 standard colors)**. Click **OK**, and choose **Don't Save** if it asks.
3. Choose **View → Zoom In** two or three times, so each character is easy to read.
4. Open **Window → Swatches** and **Window → History**.
5. **Paint a block to erase:** press **B** then **1** for the Brush. On Tool Options choose **Square** and **Mathematical**, set **Size** to **9**, **Intensity** to **100%** and **Softness** to **0%**. Choose a red text colour and a blue background on the Swatches window. Click three or four times side by side near the middle of the canvas. You get a block of red `@` on blue, about 30 cells wide and 9 tall.

Repaint the block this way whenever a test runs out of `@`.

## Known limitations (not bugs)

- **At a low Strength, a light character can stop lightening.** Each stroke rounds to the nearest step of the ramp, so a stroke that moves a character less than half a step leaves it as it is. At 40%, for example, a `.` never clears however many times you go over it. Use a higher Strength, or the Eraser, to clear it. Tell me if you'd rather it always moved at least one step.
- The Eraser group's button and the first eraser are both called "Eraser", as with the Brush group. The text buttons are placeholders until they become icon squares.
- Each group's last-used tool, and every option, is forgotten when the app closes. Saving settings comes in step 3.5.
- There's no soft BG Eraser. What one would do is decided in M5.

## Tests

### T1: The Eraser group's sub-toolbar

Checks that the Eraser button on the Tools toolbar shows the three erasers on a small toolbar beside it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click **Eraser** on the Tools toolbar (bottom middle) | Eraser is pressed. A small toolbar appears just above the Tools toolbar with **Eraser**, **Soft Eraser** and **BG Eraser**. The small toolbar's Eraser is pressed |
| 2 | Click **Soft Eraser** on the small toolbar | Soft Eraser is pressed and Eraser lets go. On the Tools toolbar, Eraser stays pressed |
| 3 | Click **Brush** on the Tools toolbar | The small toolbar changes to the three brushes |
| 4 | Click **Eraser** on the Tools toolbar | The erasers' small toolbar comes back, with **Soft Eraser** pressed: the eraser you used last |

### T2: The letter and number keys

Checks that E brings back the last eraser and that 1, 2 and 3 choose the erasers.

**Also on macOS:** Yes, because the number keys arrive as typed text, which macOS keyboards deliver their own way

| # | Do this | Expected result |
|---|---|---|
| 1 | Click an empty spot on the canvas, then press **Esc** | The Hand tool is chosen |
| 2 | Press **E** | The Eraser group is chosen, with **Soft Eraser** pressed (still the last one used, from T1) |
| 3 | Press **3** | **BG Eraser** is chosen |
| 4 | Press **1** | **Eraser** is chosen |
| 5 | Press **2** | **Soft Eraser** is chosen |
| 6 | Press **4** | Nothing changes: the group has only three erasers |
| 7 | Press **B**, then **E** | A brush is chosen, then the **Soft Eraser** again |

### T3: The Tools menu

Checks that the Tools menu lists the two new erasers after the Eraser.

**Also on macOS:** Yes, because the menu is in the Mac's own menu bar there

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Tools** menu | It lists **Hand**, **Text**, **Brush**, **FG Brush**, **BG Brush**, **Eraser**, **Soft Eraser** and **BG Eraser**. The chosen one has a check mark |
| 2 | Choose **BG Eraser** | The BG Eraser is chosen, and the small toolbar shows it pressed |

### T4: Each eraser's options, and the shared size

Checks what Tool Options shows for each eraser, and that the three share one size and shape that the brushes don't.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **E** then **1** (Eraser) | Tool Options shows **Size**, **Shape** and **Proportions** |
| 2 | Press **2** (Soft Eraser) | Tool Options shows **Size**, **Shape**, **Proportions**, **Ramp**, **Strength 100%** and **Softness 50%** |
| 3 | Press **3** (BG Eraser) | Tool Options shows **Size**, **Shape** and **Proportions** |
| 4 | Set **Size** to **5** and choose **Square** | The outline under the pointer is 5 cells wide and square |
| 5 | Press **1**, then **2** | Both show **Size 5** and **Square** too |
| 6 | Press **B** then **1** (Brush) | The Brush still has its own size, **9** from Setup |

### T5: The ramp is shared with the Brush

Checks that the Soft Eraser and the Brush use one Ramp setting.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Brush, choose **Fine** for **Ramp** | The dropdown shows **Fine** |
| 2 | Press **E** then **2** (Soft Eraser) | Its **Ramp** dropdown shows **Fine** too |
| 3 | Choose **Standard** for **Ramp** | The dropdown shows **Standard** |
| 4 | Press **B** then **1** | The Brush's **Ramp** shows **Standard** |

### T6: The Soft Eraser at full strength

Checks that at Strength 100% with a hard edge, the Soft Eraser empties every cell it covers, and leaves the background.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **E** then **2** (Soft Eraser). Set **Size 3**, **Square**, **Mathematical**, **Strength 100%** and **Softness 0%** | The outline under the pointer is a 3 × 3 square |
| 2 | Click in the middle of the `@` block | A 3 × 3 hole of empty cells appears. The blue background is still there in the hole |
| 3 | Look at the History window | It has one new step, **Erase** |

### T7: Strength

Checks that a lower Strength lightens a step at a time, and that a second stroke lightens further.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Soft Eraser, set **Size 1** and **Strength 40%** | The outline is one cell |
| 2 | Click one `@` | It becomes `+`, still red on blue |
| 3 | Click the same cell again | It becomes `-` |
| 4 | Click it three more times | It becomes `:`, then `.`, then stays `.` (see Known limitations) |
| 5 | Drag back and forth across a row of `@` several times without letting go, then let go | Every cell you crossed is `+`. Going over a cell again in the same stroke doesn't lighten it again |
| 6 | Drag across the same row again | They become `-` |

### T8: Softness

Checks that a soft edge lightens less towards the outline's edge.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Soft Eraser, set **Size 3**, **Square**, **Mathematical**, **Strength 100%** and **Softness 100%** | The outline is a 3 × 3 square |
| 2 | Click on a part of the block that's still all `@` | The middle cell empties, and the eight around it become `#` |
| 3 | Set **Size 9** and click on another untouched part | An empty middle, with lighter characters around it that get darker towards the edge of the outline, back to `@` or `%` at the very edge |

### T9: Colours stay

Checks that the Soft Eraser keeps each character's own colours, whatever colours are chosen.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose a green text colour and a yellow background on the Swatches window | The Colors toolbar shows green and yellow |
| 2 | With the Soft Eraser at **Size 1**, **Strength 40%**, click a red `@` on blue | It becomes a **red** `+` on **blue**. The chosen green and yellow aren't used |
| 3 | Set **Strength 100%** and click it again | The character goes, and the blue background stays |
| 4 | Drag across a few empty cells with no background | Nothing changes, and the History window shows no new step |

### T10: Letters that aren't in the ramp

Checks that the Soft Eraser treats letters as the ramp step whose darkness they're nearest, and doesn't change them until they move a whole step.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **T**, click an empty cell and type **HELLO WORLD**, then press **Esc** | The text is there, and the caret goes |
| 2 | Press **E** then **2**. With **Size 1**, **Softness 0%** and **Strength 100%**, drag across **HELLO** | Each letter goes, leaving empty cells |
| 3 | Set **Strength 30%** and drag once across **WORLD** | Each letter either stays as it is, or turns into a ramp character such as `+`, `=` or `-`. None becomes darker. Any that stayed are close to a step that 30% doesn't reach |

### T11: The BG Eraser

Checks that the BG Eraser removes backgrounds and leaves characters and their text colour.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **B** then **3** (BG Brush) and paint across a few empty cells beside the block | Those cells get the chosen background, with no characters |
| 2 | Press **E** then **3** (BG Eraser), set **Size 3**, and drag across the edge of the block and those cells | Under the outline, the `@`s are still there and still red, but have no background. The cells that had only a background are empty |
| 3 | Look at the History window | It has one new step, **Erase** |
| 4 | Drag across the same place again | Nothing changes, and there's no new step |

### T12: The BG Eraser in a monochrome document

Checks that the BG Eraser says why it does nothing in a document without colours.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the BG Eraser chosen, choose **Document → Monochrome**, confirming if it asks | Orange text at the end of Tool Options says **This document has no background colours** |
| 2 | Drag across some characters | Nothing changes, and the History window shows no new step |
| 3 | Press **1** (Eraser), then **2** (Soft Eraser) | Neither shows the warning |
| 4 | Press **3**, then choose **Document → ANSI Colors** | The warning goes |

### T13: A ramp that needs Unicode

Checks that the Soft Eraser, like the Brush, does nothing with a ramp an ASCII-only document can't hold, and says why.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Document → ASCII Only**, confirming if it asks | The status bar says ASCII only |
| 2 | Press **E** then **2** (Soft Eraser) and choose **Blocks** for **Ramp** | Orange text at the end of Tool Options says **This ramp needs a Unicode document** |
| 3 | Drag across some `@`s | Nothing changes, and the History window shows no new step |
| 4 | Choose **Standard** again | The warning goes, and the Soft Eraser works again |
| 5 | Choose **Document → ASCII Only** again to turn it off | The status bar no longer says ASCII only |

### T14: Undo, Esc and locked layers

Checks that each stroke is one undo step, that Esc takes a stroke back, and that a locked layer is left alone, for the new erasers.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Soft Eraser, drag one long stroke across the block | The History window shows one new step, **Erase**. **Edit → Undo** brings back the whole stroke in one go |
| 2 | Start another stroke, and press **Esc** while still holding the mouse button | The stroke is taken back. Let go: nothing is erased, and the Soft Eraser is still chosen |
| 3 | Press **Esc** again | The Hand tool is chosen |
| 4 | Press **E** then **3** (BG Eraser), start a stroke and press **Esc** while holding the button | The backgrounds come back |
| 5 | Lock the layer in the Layers window, then drag with each of the three erasers | Nothing is erased |
| 6 | Unlock the layer, and drag with the Soft Eraser | It erases again |

## Worth poking at (optional)

### P1: Strength 0%

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Soft Eraser, set **Strength 0%** and drag across the block | Nothing changes, and there's no new History step |

### P2: A big soft circle over a painted gradient

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Paint a large area with the Brush at **Size 20**, **Softness 100%** | A soft round blob, darkest in the middle |
| 2 | With the Soft Eraser at **Size 20**, **Circle**, **Strength 50%**, **Softness 100%**, click on the blob's edge | It lightens most where you clicked, fading away from there, with no sharp line |

### P3: Changing the ramp before erasing

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Paint with the **Standard** ramp, then choose **Fine** and soft-erase at **Strength 40%** | The characters become lighter characters from Fine, not from Standard. Standard's characters count as the Fine step whose darkness is nearest |

### P4: Switching eraser in the middle of a stroke

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Start a Soft Eraser stroke, and while holding the mouse button press **3** | Whatever the stroke erased so far stays, and the BG Eraser is chosen |
