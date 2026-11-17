<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 3.3: The Brush paints ramps, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 35 min

## What changed

The Tools toolbar now has one button per **tool group**. The Brush group holds three brushes, on its own small toolbar and on keys 1–3:

- the new **Brush**, which paints a ramp's characters
- the **FG Brush** and the **BG Brush**, which paint colour as the old Brush did

B brings back whichever brush you used last.

The new Brush is darkest in the middle, at the **Intensity** you choose, and its edge fades by the **Softness** you choose. It blends with what's already there, rather than stamping over it. Tool Options gains:

- **Ramp**, a dropdown that lists each ramp with its characters
- **Intensity** and **Softness**, shown as percentages
- **Blend mode**, a dropdown where Replace and Darken are greyed out, as the proof of concept you asked for

The Debug menu's sample sub-toolbars are gone.

## Setup

1. Run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**. Under **Characters** choose **Unicode (any character)**, and under **Colors** choose **ANSI (the 16 standard colors)**. Click **OK**, and choose **Don't Save** if it asks.
3. Choose **View → Zoom In** two or three times, so each character is easy to read.
4. Open **Window → Swatches**, so you can choose colours.

## Known limitations (not bugs)

- **Replace and Darken can't be chosen.** They're greyed out on purpose, until they're built.
- **The arrow keys can't move through an open dropdown.** Nothing on Tool Options takes the keyboard, so that `[`, `]` and the number keys keep working. Choose with the mouse.
- The Brush group's button and the new Brush are both called "Brush". The text buttons are placeholders until they become icon squares.
- Each group's last-used tool, and every option, is forgotten when the app closes. Saving settings comes in step 3.5.
- Painting with the same intensity over a cell that's already at that level changes nothing, not even its colours. That's how blending works (see T9).

## Tests

### T1: The Tools toolbar and the Brush group's sub-toolbar

Checks that the Tools toolbar has one button per group, and that the Brush group shows its brushes on a small toolbar beside it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Look at the Tools toolbar (bottom middle) | It has **Hand**, **Text**, **Brush** and **Eraser**, as before. Text is pressed |
| 2 | Click **Brush** on the Tools toolbar | Brush is pressed. A small toolbar appears just above the Tools toolbar with **Brush**, **FG Brush** and **BG Brush**. The small toolbar's Brush is pressed |
| 3 | Click **FG Brush** on the small toolbar | FG Brush is pressed and Brush lets go. On the Tools toolbar, Brush stays pressed |
| 4 | Click **Eraser** on the Tools toolbar | The small toolbar disappears |
| 5 | Click **Brush** on the Tools toolbar | The small toolbar comes back, with **FG Brush** pressed: the brush you used last |
| 6 | Click **Hand**, then **Text** | No small toolbar appears for either |

### T2: The letter and number keys

Checks that B brings back the last brush, that 1, 2 and 3 choose the brushes, and that digits still type with the Text tool.

**Also on macOS:** Yes, because the number keys arrive as typed text, which macOS keyboards deliver their own way

| # | Do this | Expected result |
|---|---|---|
| 1 | Click an empty spot on the canvas, then press **Esc** | The Hand tool is chosen |
| 2 | Press **B** | The Brush group is chosen, with **FG Brush** pressed on the small toolbar (still the last one used, from T1) |
| 3 | Press **1** | **Brush** is chosen |
| 4 | Press **3** | **BG Brush** is chosen, and the Colors toolbar's BG swatch looks pressed |
| 5 | Press **2** | **FG Brush** is chosen, and the FG swatch looks pressed |
| 6 | Press **E**, then **B** | The Eraser is chosen, then the FG Brush again |
| 7 | Press **E**, then **2** | Nothing changes: the Eraser stays chosen, because its group has only one tool |
| 8 | Press **T**, click an empty cell and type **123** | 1, 2 and 3 are typed on the canvas, and the Text tool stays chosen |

### T3: The Tools menu

Checks that the Tools menu lists every tool, with the three brushes together.

**Also on macOS:** Yes, because the menu is in the Mac's own menu bar there

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Tools** menu | It lists **Hand**, **Text**, **Brush**, **FG Brush**, **BG Brush** and **Eraser**, with a check mark beside the active tool |
| 2 | Choose **BG Brush** | The BG Brush is chosen: the small toolbar shows it pressed, and the Tools toolbar's Brush is pressed |

### T4: Tool Options for each brush

Checks which options each brush shows, and that the three brushes share one size and shape.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **B**, then **1**, and look at Tool Options (top middle) | In order: **Size** (a slider and 1), **Shape** (Square, Circle), **Proportions** (Perceptual, Mathematical), **Ramp** (a dropdown showing Standard), **Intensity** (a slider and 100%), **Softness** (a slider and 50%) and **Blend mode** (a dropdown showing Blend) |
| 2 | Compare the height of Tool Options with the File toolbar beside it | Tool Options is no taller |
| 3 | Press **2** (FG Brush) | Only Size, Shape and Proportions |
| 4 | Drag the **Size** slider to **5**, then press **1** and **3** | Brush and BG Brush also show Size 5 |
| 5 | Press **E** | The Eraser shows its own size, which isn't 5 unless you set it to 5 before |

### T5: The dropdowns

Checks the Ramp and Blend mode dropdowns, including the greyed-out blend modes, and that they leave the keyboard with the canvas.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **B** and **1** for the Brush, then click the **Ramp** dropdown | It opens and lists **Standard**, **Standard Alphanumerical**, **Fine**, **Blocks** and **Braille**. Each name is followed by its characters in grey, such as `.:-=+*#%@` for Standard |
| 2 | Choose **Blocks** | The dropdown closes and shows only **Blocks**, without its characters |
| 3 | Without clicking the canvas, press **]** | Size goes up by 1: the dropdown didn't take the keyboard |
| 4 | Click the **Blend mode** dropdown | It lists **Blend**, **Replace** and **Darken**. Replace and Darken are greyed out |
| 5 | Click **Replace** | Nothing happens. Click outside the list to close it. It still shows **Blend** |
| 6 | Choose **Standard** for **Ramp** again, and set **Size** back to **1** | Ready for the next test |

### T6: A hard tip at different intensities

Checks which character each intensity paints, with no fading.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Brush at Size 1, drag **Softness** to **0%**, and click an empty cell | An **@** appears |
| 2 | Drag across a row of empty cells | A line of **@**. **Edit → Undo** takes the whole line away in one go |
| 3 | Drag **Intensity** until it reads **50%**, and click an empty cell | A **+** appears |
| 4 | Set Intensity to **20%** and click an empty cell | A **:** appears |
| 5 | Set Intensity to **5%** and click an empty cell | Nothing appears, because 5% is closer to empty than to the lightest character |

### T7: A soft tip

Checks that softness fades the edge and follows the shape.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Set **Intensity 100%**, **Softness 50%**, **Size 15**, **Circle**, **Perceptual**. Click once in an empty area | A round blob: **@** across the middle, then lighter and lighter characters (such as `%`, `#`, `*`, `+`, `=`, `-`, `:` and `.`) towards the edge. It looks round on screen |
| 2 | Set **Softness 100%** and click once in another empty area | Only the very middle is **@**, and it fades all the way out from there |
| 3 | Set **Softness 0%** and click once elsewhere | A solid round blob of **@** |
| 4 | Set **Shape** to **Square** and **Softness 100%**, and click once elsewhere | A square that fades evenly towards all four sides, so each ring around the middle is one character |

### T8: Blending

Checks that the Brush blends with what's there: one stroke counts once, a second stroke pushes further, and a low intensity lightens.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Set **Circle**, **Size 9**, **Softness 100%**, **Intensity 100%**. Press the mouse button in an empty area and scrub back and forth over a short stretch for a few seconds, then let go | The edges don't keep getting darker while you scrub. It looks like one pass |
| 2 | Paint over the same stretch once more | Its edges get darker |
| 3 | Set **Size 3**, **Softness 0%**, **Intensity 30%**, and paint across the **@**s in the middle | They lighten to **-** |
| 4 | Set **Intensity 0%** and paint across them again | They're emptied |

### T9: Colours

Checks that the Brush writes the FG and BG colours on the cells it changes, and that Alt+click picks both colours up.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Red** as FG and **Blue** as BG in Swatches. With the Brush at **Size 3**, **Softness 0%** and **Intensity 100%**, click an empty area | A 3 × 3 patch of red **@** on blue |
| 2 | Choose **Green** as FG and **None** as BG. Click the middle of the patch again | Nothing changes, because those cells are already **@**. The Brush only colours cells whose character it changes |
| 3 | Set **Intensity 50%** and click the middle of the patch again | The patch becomes green **+**, still on blue, because a BG of None keeps the background |
| 4 | Set **Intensity 0%** and click it again | The characters go, and the blue background stays |
| 5 | Set **Intensity 100%** and paint a green **@** somewhere on that blue patch. Choose **Yellow** as FG and **Red** as BG, then **Alt+click** the green **@** | Both swatches change: FG to Green and BG to Blue. Nothing is painted |

### T10: The Colors toolbar and the FG and BG Brushes

Checks that clicking a swatch switches between the FG and BG Brushes only while one of them is chosen.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **2** (FG Brush) and click the **BG** swatch on the Colors toolbar | The BG Brush is chosen |
| 2 | Click the **FG** swatch | The FG Brush is chosen |
| 3 | Press **1** (Brush) and click the **BG** swatch | The BG swatch looks pressed, and the Brush stays chosen |
| 4 | Press **2** | The FG Brush is chosen, and the FG swatch looks pressed |

### T11: A ramp the document can't hold

Checks that a ramp with characters outside ASCII paints nothing in an ASCII-only document, and says why.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Document → ASCII Only**, confirming if it asks | The status bar says ASCII only |
| 2 | Press **1** (Brush) and choose **Blocks** for **Ramp** | Orange text at the end of Tool Options says **This ramp needs a Unicode document** |
| 3 | Set **Intensity 100%** and drag across an empty area | Nothing is painted, and the History window shows no new Paint step |
| 4 | Choose **Standard** for **Ramp** | The warning goes, and painting works again |
| 5 | Choose **Blocks** again, then **Document → ASCII Only** again to turn it off | The warning goes, and the Brush paints block characters such as `█` |
| 6 | Choose **Document → ASCII Only** again, then press **2** | The FG Brush shows no warning |

### T12: Undo, Esc and locked layers

Checks that each stroke is one undo step, that Esc takes a stroke back, and that a locked layer is left alone.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Standard** with the Brush, and paint one long stroke | The History window shows one new step, **Paint** |
| 2 | Start another stroke, and press **Esc** while still holding the mouse button | The stroke disappears. Let go: nothing is painted, and the Brush is still chosen |
| 3 | Press **Esc** again | The Hand tool is chosen |
| 4 | Lock the layer in the Layers window, choose the Brush and paint | Nothing is painted |

### T13: The Debug menu

Checks that the sample sub-toolbars are gone.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Debug** menu | There are no **Sub-Toolbar on …** or **Long Sub-Toolbar** items. The sample windows are still there |

## Worth poking at (optional)

### P1: Painting over text

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Type a few words with the Text tool. With the Brush at **Softness 0%** and **Intensity 100%**, paint across them | The letters become **@** |
| 2 | Type a few more words, and paint across them at **Intensity 40%** | Letters darker than **=** get lighter and letters lighter than it get darker, judged by how they look in the document's font. A letter that's already close to **=** stays as it is |

### P2: Other places and fonts

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag the Tools toolbar to the left edge, then choose a brush | The small toolbar follows the Tools toolbar, beside it on the canvas side |
| 2 | Choose **File → New** while a brush is chosen | The same brush stays chosen, and the small toolbar stays |
| 3 | Change **Document → Font** to another font, then paint with a soft brush | It still paints and fades. Foreign letters may count as slightly different levels in the new font |
| 4 | Try the **Braille** and **Fine** ramps in a Unicode document | Each paints its own characters, lightest at the edge |
