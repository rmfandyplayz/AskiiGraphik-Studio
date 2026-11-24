<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.10a: PATS comparison, manual test

**Build:** the commit that adds this file
**Test on:** Windows (Linux is covered by CI only)
**Time needed:** about 30 min

## What changed

Nothing changes for normal painting: the Brush and the Soft Eraser still paint as in 4.7. Instead, there are seven ways of picking edge glyphs to compare. They are 4.6's brush, 4.7's, the three ideas on their own (1: shape counts more when the edge is faint, 2: a glyph inked on the wrong side is never picked, 3: each edge cell is judged with the cells around it), ideas 1 and 3 together, and all three together. You can compare them in two ways: on picture sheets that show the same strokes painted each way, and by painting with each one, chosen from the Debug menu.

There's no right or wrong result here. The aim is for you to pick the variant that step 4.10b makes the brush's own, or to say what you'd change.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`). It has to be a Debug build for the Debug menu to show.
2. The sheets are already in `.local/pats-sheets` in the repo folder. To make them again, run this from the repo root: `dotnet test --project tests/AskiiArt.Imaging.Tests --filter-class "*PatsComparisonSheets" --explicit only`. It takes about a minute.

## Known limitations (not bugs)

- **Thin tips aren't shaped.** A tip under three cells tall picks whole cells, as in 4.7. So the sheets draw the diagonal and the curve with a size-6 tip.
- **The choice isn't remembered.** Brush Edges goes back to 4.7 each time the app starts.
- **Idea 3 can change edge glyphs near the tip as the stroke goes on.** As agreed, it re-picks them while their neighbours change.
- **Speed isn't judged yet.** Idea 3 does more work on every move. Step 4.10b measures whichever variant you pick.
- **Placeholders:** the menu item names and the sheets' layout. See `docs/TEMPORARY-UI.md`, "Debug → Brush Edges".

## Tests

### T1: The comparison sheets

**Also on macOS:** No

There are 12 sheets, one for each font (JetBrains Mono, Roboto Mono), ramp (Fine, Standard) and softness (0, 50, 100%). On each sheet, the columns are intensity 25, 50 and 100%, and the rows are the seven variants, labelled on the left. Every tile holds a soft circle, a square, a diagonal and a curve.

| # | Do this | Expected result |
|---|---|---|
| 1 | Open `.local/pats-sheets/JetBrainsMono-fine-soft100.png`. Look at the top edges of the circle and the square at 25% in the 4.7 row | Backticks along the top with a gap below them: the problem from 4.7's first look |
| 2 | Compare those top edges in rows 1, 2, 3, 1+3 and 1+2+3 | Each idea treats that edge differently. Note which reads best as the edge of a faint shape |
| 3 | In the same sheet, compare the diagonal and the curve at each intensity across the rows | Note which keeps a smooth, even line, and which has stray glyphs or gaps |
| 4 | Compare the insides of the soft circle at 50% and 100% across the rows | Inside a shape, every row should look about as even as 4.7's. Note any row that looks noisy |
| 5 | Look over the other 11 sheets the same way, paying most attention to the soft ones (50 and 100) and to Roboto Mono | Note any variant that looks good on one sheet but bad on another |
| 6 | Tell me which variant you'd keep, or what you'd change in one | Your pick, for 4.10b |

### T2: The Debug menu choice

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Debug** menu | Below **Mouse Acts as a Pen at Half Pressure**, between two separators, seven **Brush Edges** items, with **Brush Edges: 4.7, Shaped** ticked |
| 2 | Choose **Brush Edges: Idea 3, Neighboring Cells**, then open the Debug menu again | Only Idea 3 is ticked |
| 3 | Choose **Brush Edges: Idea 3, Neighboring Cells** again, then open the menu again | Idea 3 is still ticked: choosing the ticked item doesn't untick it |
| 4 | Close the app and run it again, then open the Debug menu | 4.7 is ticked again |

### T3: Painting with each variant

**Also on macOS:** No

This is where you see how the variants feel while painting, which the sheets can't show.

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **File → New** and click **OK**. Choose the **Brush**. In the Tool Options toolbar, set **Ramp** to Fine, **Intensity** to 25, **Softness** to 100 and **Size** to 18. Click once on the canvas | A faint soft circle, painted as in 4.7 |
| 2 | Choose each Brush Edges item in turn. With each one, click once on an empty part of the canvas, then drag a slow curve and a quick diagonal | Each paints its own way. Painted shapes stay as they were when you switch: only new strokes use the new choice |
| 3 | Repeat with Intensity at 50 and 100, Softness at 0 and 50, Size at 6, and Ramp set to Standard | Note which variant you prefer at each setting |
| 4 | With Idea 3 chosen, type a few lines of text with the Text tool, then paint a soft stroke that passes close to the text but over empty cells | The text isn't changed. Only cells the stroke paints change, but the edge glyphs next to the text may differ from the same stroke on empty canvas, because idea 3 takes the text into account |
| 5 | With Idea 3 chosen, select a rectangle with Rectangle Select, choose the Brush, and paint across the selection's edge | Nothing outside the selection changes |
| 6 | Choose the **Soft Eraser** and erase across a soft shape with each variant | The edges it leaves follow the same choice as the Brush |
| 7 | Choose **Document → Font**, pick Roboto Mono, and repeat rows 1 and 2 | Same as in JetBrains Mono, in that font's shapes |

### T4: Tests to revisit

| Step | Test | Where | Why |
|---|---|---|---|
| 4.9b | T4, row 1 | Windows | It was tried with a paste bigger than the window, which by the agreed rule leaves the view alone. Try it with a paste that fits in the window but lands partly out of view |

## Worth poking at (optional)

### P1: Big soft tips with idea 3

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Brush Edges: Ideas 1, 2 and 3**, set Size to 100 and Softness to 100, and drag quickly across the canvas | It may feel slower than with 4.7. Tell me if it stutters badly; 4.10b measures it properly |
