<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.10b: Build the chosen PATS, manual test

**Build:** the commit that adds this file
**Test on:** Windows (Linux is covered by CI only)
**Time needed:** about 20 min

## What changed

The Brush and the Soft Eraser now pick edge glyphs the way you chose in 4.10a: each edge cell is judged with the cells around it (idea 3), and a cell whose glyph wouldn't clearly show its edge is painted as a whole, as 4.6 did (the fix). This is called **Standard**, and it's the default. **Strict** (ideas 1, 2 and 3 with the fix) stays beside it in the Debug menu, so you can paint with both before deciding. The other variants are gone. Painting with a big soft tip is also faster, with exactly the same result.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`). It has to be a Debug build for the Debug menu to show.
2. 4.10a's sheets are still in `.local/pats-sheets`, if you want to compare with them. Their **3+fix** row is Standard and their **1+2+3+fix** row is Strict.

## Known limitations (not bugs)

- **Thin tips aren't shaped.** A tip under three cells tall paints whole cells, as since 4.7.
- **Stray marks can remain.** Where no glyph clearly shows a cell's edge, the cell takes the ramp's glyph for its darkness, whichever side that glyph's ink is on. So soft squares on the Fine ramp can still get a few backticks round them.
- **Edge glyphs near the tip may change as the stroke goes on**, as their neighbours change.
- **The choice isn't remembered.** Brush Edges goes back to Standard each time the app starts.
- **A Debug build is slower than what ships.** Rider's run configuration is a Debug build. T5 row 3 tries a Release build.
- **Placeholders:** the menu item names. See `docs/TEMPORARY-UI.md`, "Debug → Brush Edges".

## Tests

### T1: The Debug menu choice

**Also on macOS:** No

This checks that the nine 4.10a items have become two, and that they tick like radio buttons.

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Debug** menu | Below **Mouse Acts as a Pen at Half Pressure**, between two separators, two items: **Brush Edges: Standard (3 with the Fix)**, which is ticked, and **Brush Edges: Strict (1, 2 and 3 with the Fix)** |
| 2 | Choose **Brush Edges: Strict (1, 2 and 3 with the Fix)**, then open the Debug menu again | Only Strict is ticked |
| 3 | Choose **Brush Edges: Strict (1, 2 and 3 with the Fix)** again, then open the menu again | Strict is still ticked: choosing the ticked item doesn't untick it |
| 4 | Close the app and run it again, then open the Debug menu | Standard is ticked again |

### T2: Painting with Standard

**Also on macOS:** No

This checks that the default brush paints the way 4.10a's **3+fix** variant did.

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **File → New** and click **OK**. Choose the **Brush**. In the Tool Options toolbar, set **Ramp** to Fine, **Shape** to circle, **Intensity** to 25, **Softness** to 100 and **Size** to 18. Click once on the canvas | A faint soft circle whose edge glyphs look like the 3+fix row's circle on `JetBrainsMono-fine-soft100.png` at 25% |
| 2 | Set **Shape** to square and click on an empty part of the canvas | A soft square. Its straight sides look as 4.6's brush drew them, rather than shaped |
| 3 | Set the shape back to circle. Drag a slow curve and a quick diagonal | Smooth lines whose edges follow the stroke's shape |
| 4 | Repeat rows 1 to 3 with Intensity at 50 and 100, Softness at 0 and 50, and Ramp set to Standard | Each looks like the 3+fix row on the matching sheet |
| 5 | Set **Ramp** to Fine, **Intensity** to 100, **Softness** to 100 and **Size** to 7, then click and drag a short line | A small soft dot and line. In 4.10a you preferred 4.6 here; tell me whether this reads well enough now |

### T3: Painting with Strict

**Also on macOS:** No

This checks that Strict paints the way 4.10a's **1+2+3+fix** variant did, so you can compare the two.

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Debug → Brush Edges: Strict (1, 2 and 3 with the Fix)**. Repeat T2's rows 1 to 4 on an empty part of the canvas | Each looks like the 1+2+3+fix row on the matching sheet. Faint edges keep more of their shape than with Standard |
| 2 | Switch between Standard and Strict while painting on, and note which you prefer where | Shapes already painted don't change when you switch: only new strokes use the new choice |

### T4: Existing art, the selection and the Soft Eraser

**Also on macOS:** No

This checks that judging edge cells with their neighbours only ever changes the cells a stroke paints.

| # | Do this | Expected result |
|---|---|---|
| 1 | With Standard chosen, type a few lines of text with the **Text** tool. Choose the Brush with Softness at 50 and Size at 10, and paint a stroke that passes close to the text but only over empty cells | The text isn't changed. The edge glyphs next to the text may differ from the same stroke on empty canvas, since the text is taken into account |
| 2 | Select a rectangle with **Rectangle Select**, choose the Brush, and paint across the selection's edge | Nothing outside the selection changes |
| 3 | Choose the **Soft Eraser**, set its Softness to 50, and erase across a soft shape, once with Standard and once with Strict | The edges it leaves follow the choice, as the Brush's do. With Strict, a little more of a faint edge is kept |

### T5: Speed with a big soft tip

**Also on macOS:** No

In 4.10a, idea 3 stuttered with a big soft tip. This checks that it's smooth now.

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Debug → Show Frame Rate**. Choose the Brush, set **Ramp** to Fine, **Size** to 100 and **Softness** to 100, and drag quickly back and forth across the canvas with Standard | Smooth painting, with no hitches worse than with 4.7's brush |
| 2 | Choose Strict and do the same | The same |
| 3 | Optional, to see what ships: close the app, run `dotnet run -c Release --project src/AskiiArt.App` from the repo root, and repeat row 1 (a Release build has no Debug menu, so it paints with Standard) | Smoother still |

## Worth poking at (optional)

### P1: Strokes that pass over their own edges

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With Softness at 50 and Size at 12, drag a slow spiral that crosses itself several times | Edges stay tidy where the stroke overlaps itself, and the middle stays full |
| 2 | Undo, then redo | The stroke goes and comes back exactly as it was |
