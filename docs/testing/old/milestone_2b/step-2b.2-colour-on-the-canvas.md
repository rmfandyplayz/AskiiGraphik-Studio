<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2b.2: Colour on the canvas, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 15 min

## What changed

The canvas now draws each cell's text colour and background. There's still no way to choose a colour (that's step 2b.3), so a new Debug menu item opens a busy colour sample to look at. Colours that are set look the same in light and dark mode; only cells without a colour use the canvas's own ink and paper, which follow the theme.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider). It has to be a Debug build, which both of those are, for the Debug menu to be there.
2. Put Windows in light mode: **Settings → Personalisation → Colours → Choose your mode → Light**. On a Mac: **System Settings → Appearance → Light**.

## Tests

### T1: The colour sample in light mode

This checks that text colours and backgrounds are drawn in the right places and colours.

**Also on macOS:** Yes, because the Mac draws through a different graphics system (Metal) and a Retina screen

| # | Do this | Expected result |
|---|---|---|
| 1 | **Debug → Open Color Sample** (choose **Don't Save** if it asks) | A big canvas fills with rings of shading. In the left half, the rings are coloured text on the white paper, changing colour from the top-left corner towards the bottom-right. In the right half, the rings are brighter text on dark backgrounds that change colour from top to bottom, and the gaps between the rings are filled with the background alone. |
| 2 | Look at the white box near the top-left | Its first two lines are black on white, the canvas's own ink and paper. |
| 3 | Look at the **ANSI colours as text** row and the **bright ones** row below it | Eight colour names on each row, each word in its own colour: Black, Red, Green, Yellow (brownish), Blue, Magenta, Cyan, White (light grey). The bright row is lighter; its White is pure white, so you can't see it on the paper. |
| 4 | Look at the **ANSI colours as backgrounds** row | 16 solid blocks, each 3 cells wide, under the numbers 0 to 15: black first, light grey at 7, dark grey at 8, and white at 15, which blends into the paper. |
| 5 | Look at the next three rows | **Ink on a background:** black words on orange. **Coloured text on paper:** "sky blue" in sky blue on white. **Only a background:** a sky blue bar, 8 cells long, with nothing in it. |
| 6 | Look at **No seams between cells** | Yellow box lines and shading blocks on a solid navy background covering both rows, with no thin lines or gaps between cells or between the two rows. |
| 7 | Look at **Missing glyphs (flagged)** | The two characters after the colon sit on orange cells, and the last one is a hollow box on a red cell. These flags cover the green background on purpose, for now. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Dark mode changes only the cells without a colour

This checks that the theme changes the canvas's own ink and paper and nothing else.

**Also on macOS:** Yes, because the theme comes from the system

| # | Do this | Expected result |
|---|---|---|
| 1 | With the colour sample still open, switch the system to dark mode (**Choose your mode → Dark**; on a Mac, **Appearance → Dark**) | The paper turns dark and the first two lines of the box turn light. Every coloured thing (the rings, the colour names, the blocks, the orange, the sky blue and the navy) keeps exactly the colour it had in light mode. |
| 2 | Look at the two rows of colour names again | Now **Black** is the one that's hard to see, and the bright **White** is easy. That's expected: set colours never change with the theme. |
| 3 | Look at **Ink on a background** | Its words are now light on orange, since unset text uses the ink, which follows the theme. |
| 4 | Switch back to light mode | Everything looks as it did in T1. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Smooth zooming and panning, and crisp text

This checks that colour hasn't made the canvas slow or blurry.

**Also on macOS:** Yes, because of the Retina screen and the different graphics system

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **Ctrl+-** (Cmd+- on a Mac) until it stops zooming out | The whole 500 × 500 canvas is in view, in colour. |
| 2 | Hold **Ctrl** (Cmd) and roll the mouse wheel over the right half, zooming in and out quickly a few times | The zoom follows the pointer smoothly, with no stutter or flicker. It should feel the same as doing this on **Debug → Open Sample Document**. |
| 3 | Pan around with the middle mouse button held down, and with **Space** + drag, at a few zoom levels | Smooth, with no stutter. |
| 4 | Zoom in to 400% or more over the **No seams between cells** rows | The background edges are sharp, with no blurry edges and no lines between cells. The letters are crisp and evenly coloured, with no dark or light fringe around them. |
| 5 | Press **Ctrl+'** to show the grid, then again to hide it | The grid lines are drawn over the backgrounds as well, and go away again. |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Saving keeps the colours

This checks the colours survive a save and reopen, from step 2b.1's file format through to the canvas.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the colour sample open, **File → Save As**, and save it anywhere as `colour sample` | It saves with no message. |
| 2 | **File → New** (any size), then **File → Open** and choose `colour sample.asciiart` | It opens looking exactly as in T1. |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Documents without colour look as before

This checks nothing changed for monochrome documents, which are all documents until step 2b.3.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | **Debug → Open Sample Document** (Don't Save) | The black-and-white sample, looking just as it did before this step, with its orange and red flags in the text block. |
| 2 | Click inside its text block and type a few letters | They appear in the ink colour, as before. |

- [ ] Pass  - [ ] Fail. Notes:

### T6: No seams between box and block characters (retest)

This rechecks T1 row 6 and T3 row 4, which showed thin lines between cells at many zoom levels. Box-drawing and block characters are now stretched to fill their cells exactly, and their lines are moved onto whole pixels (ADR 0039). It needs a build from the seam fix or later; the other tests don't need repeating.

**Also on macOS:** Yes, at the default display resolution, because the Mac showed seams at different zoom levels

| # | Do this | Expected result |
|---|---|---|
| 1 | **Debug → Open Color Sample** (choose **Don't Save** if it asks) and find the **No seams between cells** rows | Yellow box lines and shading blocks on navy, as in T1. |
| 2 | Press **Ctrl+-** (Cmd+- on a Mac) until it stops at 25%, then **Ctrl+=** (Cmd+=) one step at a time up to 800%, looking at those two rows at every step. On Windows, do this at 100% display scaling and again at 125% (**Settings → System → Display → Scale**). | At every step: the `██` blocks are one solid yellow area, with no thin lines between cells or between the two rows; the box lines are unbroken where cells meet, and as bright as the letters, not faded; and the half blocks `▀▄▌▐` meet cleanly. |
| 3 | **Document → Font…**, choose **IBM Plex Mono**, and repeat row 2 at 50%, 100%, 150% and 400% | The same: no seams, and both the single and the double box lines are unbroken. This font had the worst gaps. |
| 4 | Do row 3 again with **Fira Code** | The same. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Switch light and dark mode a few times while zoomed in on the colour sample.
- Turn on 125% or 150% display scaling (**Settings → System → Display → Scale**) and look at the colour sample's edges again.
- Resize the window quickly with the colour sample zoomed out.

## Known limitations (not bugs)

- There's no way to choose a colour yet; the colour sample is the only coloured document (step 2b.3).
- Typing in the colour sample writes plain ink-on-paper letters and removes the colours of the cells typed over. The text tool learns colours in step 2b.3.
- The missing-glyph flags cover backgrounds, and the caret keeps its theme colour on any background, so it can be hard to see on a blue one. Both are placeholders.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2b.2, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
