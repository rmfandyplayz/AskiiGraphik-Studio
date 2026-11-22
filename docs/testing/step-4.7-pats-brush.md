<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.7: The PATS brush, manual test

**Voided 2026-11-22:** the owner skipped these tests after a first look. Faint, soft strokes kept backticks along their tops, with a gap below them. Step 4.10 revisits the brush.

**Build:** the commit that adds this file
**Test on:** Windows (Linux is covered by CI only; no test needs macOS, since glyphs are measured off-screen the same way on every platform)
**Time needed:** about 25 min

## What changed

The **Brush** and the **Soft Eraser** now pick each edge cell's character by where the tip covers the cell, not just by how much. Along the top of a round shape you get characters whose ink sits low in the cell, such as `_` and `.`; along the bottom, ones whose ink sits high, such as `'` and `"`; and down the sides, characters that lean in towards the middle. That's what you asked for after 4.5's testing, when the Fine ramp put `` ` `` and `.` on the wrong sides of a circle.

It's always on, for hard and soft tips, and it only ever uses the ramp's own characters. Only the edge cells change. A cell the tip covers fully still gets the same character as before.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**. Under **Characters**, choose **Unicode (any character)**. Set **Width** to **120** and **Height** to **50**, and click **OK**. Choose **Don't Save** if it asks.
3. Choose **View → Zoom In** two or three times, so you can read single characters.
4. Press **B** then **1** for the Brush. On Tool Options, choose the **Fine** ramp, and set **Intensity** to **100%**.

## Known limitations (not bugs)

- **Tiny slivers:** where only the very top or very bottom eighth of a cell is covered, no character has ink that high or low, so the cell gets the one nearest in darkness, whichever side its ink is on. Along the top of a hard round hole in T6, you may see a `,` or `.` among the `"` and `` ` ``.
- **Less predictable:** neighbouring edge cells can get quite different characters, such as `q` beside `@`, because each cell gets whichever character fits its own piece of the edge. You said that trade was fine.
- **Ramps whose characters can't lean:** Blocks' shades spread their ink evenly, so its edge cells just get a lighter shade where the tip covers part of the cell. Braille's light patterns all fill from the top left, and Standard has no light character with its ink at the top, so some of their edges keep a character whose ink is on the wrong side, as before.
- **Placeholders:** the choices listed in `docs/TEMPORARY-UI.md` under "The PATS brush". Those are the weighting, what happens when no character leans the right way, and how the smallest tips and a soft tip's middle are handled.

## Tests

### T1: A hard round tip on Fine

Checks the outline of a round shape with a hard edge.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | On Tool Options choose **Circle** and **Perceptual**, set **Size** to **21** and **Softness** to **0%**. Click once near the top left of the canvas | A round shape about 21 cells wide and 10 rows tall. Its inside is all the ramp's darkest character |
| 2 | Look along its top row | Where the shape covers only the bottom of a cell, the character has its ink low, such as `q` or `p`; where it covers nearly all of a cell, a full character such as `@`. None has its ink only at the top, such as `` ` `` or `'` |
| 3 | Look along its bottom row | The reverse: ink high in the cell, such as `^`, `f` or `Y`, or full characters such as `O`, `0` and `B`. None of `.`, `_` or `,` |
| 4 | Look down its left and right sides | On the left, characters with more ink on their right, such as `d`. On the right, the mirror of that, such as `b` or `p`. Each side leans in towards the middle |
| 5 | Step back from the screen, or zoom out once | The outline reads as round. Before this step it was a stepped ring of full characters |

### T2: A soft round tip on Fine

Checks that the soft fade's edge follows the shape too.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Set **Softness** to **50%**. Click once in an empty area | A round shape, dark in the middle, fading outwards |
| 2 | Look at its top edge | A row of `_` with a `.` at each end, or `.` all along. The ink sits low, on the shape's side |
| 3 | Look at its bottom edge | `"`, with `` ` `` at the ends: the ink sits high, on the shape's side |
| 4 | Look down its left and right sides, and at its top corners | Characters that lean in more often than not: `]` or `<` on the left and `}` on the right, `J` at the top left corner and `L` at the top right. The exact characters vary |
| 5 | Set **Softness** to **100%** and click in another empty area | A shape that fades from the very middle. Its outer rings follow the same rule: `.` and `_` along the top, `` ` ``, `'`, `"` and `^` along the bottom |

### T3: Square tips on Fine

Checks straight edges. A hard square covers whole cells, so it shouldn't change.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Square**, set **Size** to **15** and **Softness** to **0%**. Click in an empty area | A solid block, every cell the darkest character, with straight edges. Same as before this step |
| 2 | Set **Softness** to **50%** and click in another empty area | A block fading towards its edges. Its top row is mostly `_`, its bottom row mostly `"`, with `-` or `'` at the corners |
| 3 | Look down its left and right sides | The outer columns are `-`. Next in, each side leans in: `J` on the left, with its stem on the right, and `L` on the right, with its stem on the left |

### T4: Other ramps

Checks that shapes still look right on ramps with fewer or different characters.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Circle**, **Size 21**, **Softness 50%**, and the **Standard** ramp. Click in an empty area | Rings of `.`, `:`, `-`, `=`, `+`, `*`, `#`, `%` and `@`, much as before this step. Its top and bottom edges are both `.`, since Standard has no light character with its ink at the top |
| 2 | Choose **Standard Alphanumerical** and click in another empty area | A round shape. Its lightest letters, such as `v`, `c` and `x`, sit low in the cell, so they ring both its top and its bottom |
| 3 | Choose **Blocks** and click in another empty area | Rings of shades from `░` to `█`, much as before this step |
| 4 | Choose **Braille** and click in another empty area | A round shape with no gaps or stray dark dots, ringed by `⠁` and `⠃` |
| 5 | Repeat 1 to 4 with **Softness 0%** | Hard round shapes. Standard's top row is `+` at the ends and `@` between, and its bottom row mixes `*`, `#` and `%`. Standard Alphanumerical's top row has `u` and `d`, its bottom row `P` and `9`. Blocks is `█` with `▓` and `▒` round the edge. Braille's top row has `⣧` and `⣇`, with their dots low |

### T5: Small tips

Checks that the smallest tips paint as they did before. They're all edge, so they aren't shaped.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose the **Standard** ramp, **Circle**, **Perceptual** and **Softness 0%**. Set **Size** to **1** and click | One `@` |
| 2 | Set **Size** to **3** and click elsewhere, then **Size 5** and click again | A small patch of `@` each time, every cell the same |
| 3 | Set **Softness** to **100%** and **Size** to **3**. Click | An `@` in the middle and lighter characters either side |
| 4 | Choose **Mathematical** and **Square**, **Size 3**, **Softness 100%**. Click | A 3 × 3 block with `@` in the middle and lighter characters round it. The middle is still `@` |

### T6: The Soft Eraser's edges

Checks that the Soft Eraser leaves edge cells with their ink away from the hole.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Brush, choose the **Fine** ramp, **Square**, **Mathematical**, **Size 40**, **Softness 0%**. Click twice side by side in an empty area | A solid block of the darkest character, about 60 cells wide and 40 tall |
| 2 | Press **E** then **2** for the Soft Eraser. Choose **Circle**, **Perceptual**, **Size 21**, **Strength 100%** and **Softness 0%**. Click in the middle of the block | A round hole |
| 3 | Look at the row just above the hole | Mostly characters with their ink at the top, such as `"` or `` ` ``, away from the hole, and a few even ones such as `+` and `!`. A lone `,` or `.` is the sliver limit above |
| 4 | Look at the row just below the hole | Characters with their ink at the bottom, such as `_`, away from the hole |
| 5 | Set **Softness** to **50%** and click on another part of the block | A softer hole. Its bottom corners have `L` on the left and `J` on the right, and its bottom edge has `_` and `.` |

### T7: Strokes, undo and speed

Checks that strokes behave and feel as before.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **B** then **1**. Choose **Circle**, **Size 21**, **Softness 0%**, the **Fine** ramp. Drag a wavy line across an empty area | A thick line. Along its top the ink sits low and along its bottom it sits high, as in T1 |
| 2 | Open **Window → History** | The line is one **Paint** step |
| 3 | Start another drag and press **Esc** before letting go | That stroke disappears |
| 4 | Set **Size** to **100** and **Softness** to **100%**. Drag quickly back and forth across the canvas several times | The stroke keeps up with the pointer, about as smoothly as before this step |

### T8: Other fonts

Checks that the characters are picked for the document's font.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Document → Font…** and pick **IBM Plex Mono** | The art redraws in that font |
| 2 | With the Brush, **Fine**, **Circle**, **Size 21**, **Softness 50%**, click in an empty area | The edges follow the same rule as in T2: ink low along the top, high along the bottom, leaning in at the sides. The exact characters may differ from JetBrains Mono's |
| 3 | Repeat 1 and 2 with **Roboto Mono** and **Fira Code** | The same |

## Worth poking at (optional)

### P1: Half pressure

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Debug → Mouse Acts as a Pen at Half Pressure**. Paint a soft round shape on **Fine** | A lighter shape whose edges still lean the right way |
| 2 | Turn the Debug item off again | Back to full strength |

### P2: Painting over art

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Type a few lines of text with the Text tool (**T**). Then, with the Brush on **Fine**, **Softness 50%**, paint over part of it | The text blends towards the brush. Edge cells take characters that lean the brush's way, and letters the brush barely reaches can keep their own character |
| 2 | With the Soft Eraser, erase across the same text | The text lightens, and along the edges the ink stays on the side away from the tip |
