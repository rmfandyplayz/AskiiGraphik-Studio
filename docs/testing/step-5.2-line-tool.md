<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 5.2: The Shapes group and the Line tool, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 20 min

## What changed

There's a new **Shapes** group on **U**, with its first tool, **Line**, on 1. Press, drag and let go to draw a straight line. The line shows in the drawing as you drag and follows the pointer, and when you let go it's one step in History. Tool Options shows a **Style** dropdown with all four ways from 5.1: **Standard**, **Standard by step**, **Precise** and **Single character**. Single character draws the same character as the Character Brush: changing it for one changes it for the other.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Open **Window → History** and **Window → Characters** if they aren't open already.

## Known limitations (not bugs)

- **Only the Line tool so far.** Rectangle, Ellipse, Curve and Table join the Shapes group in later steps, so it has no sub-toolbar yet.
- **The Tools toolbar may scroll.** With the new **Shapes** button, the bottom row of toolbars no longer fits a window 2048 pixels wide at 100% (your 2560 screen at 125%), so the Tools toolbar scrolls, with a fade at its ends. Scroll it with the wheel over it. It should fit again once its text buttons become icons.
- **No preview outline.** The line is drawn into the drawing itself as you drag, so you see exactly what you'll get.
- **Precise can pick characters that aren't mirror images** on the two sides of a shape, such as `'` on one side and `` ` `` on the other. It's parked for tuning once you've tried the tool (`docs/TEMPORARY-UI.md`, "Precise lines' two halves alike").
- **Placeholders:** the "Shapes" button text, the style names, and the Character box showing in every style. See `docs/TEMPORARY-UI.md`, "The Line tool".

## Tests

### T1: Choosing the Line tool

This checks the new group's key, button and menu item, and the options it shows.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click the canvas once, then press **U** | The Line tool is chosen: the **Shapes** button on the Tools toolbar is highlighted |
| 2 | Look at Tool Options | **Style**, showing **Standard**, then **Character** with a small box showing a character |
| 3 | Open the **Style** dropdown | Four choices: **Standard**, **Standard by step**, **Precise**, **Single character**. Close it without changing anything |
| 4 | Press **B**, then **1** | The Brush is chosen. **1** didn't pick the Line tool, since the Brush group is active |
| 5 | Press **U**, then **1** | The Line tool is chosen again |
| 6 | Press **B**, then open the **Tools** menu and choose **Line** | The Line tool is chosen. In the menu, **Line** comes after the erasers |

### T2: Drawing a line

This checks the basic drag: what you see while dragging, what's left after, and that it's one undo step.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | In **Window → Swatches**, choose a red text colour. With the Line tool, press on an empty cell near the top left and drag slowly to the right without letting go | A red line of `-` grows from the cell you pressed to the cell under the pointer |
| 2 | Still holding, move the pointer down and to the right, then back up a little | The line follows, each time replacing the last one: no leftover cells from earlier positions |
| 3 | Let go | The line stays. History has one new step, **Line** |
| 4 | Press **Ctrl+Z** (Cmd+Z on a Mac) | The whole line goes, in one undo |
| 5 | Click once on an empty cell without dragging | Nothing is drawn, and History has no new step |
| 6 | Press, drag a few cells away, then come back to the cell you pressed and let go | Nothing is left, and History has no new step |
| 7 | Draw a line across some existing text | The line's cells replace the text where it crosses, and only there |

### T3: The four styles

This checks that each style draws its own characters. Use a shallow line: about 20 cells across and 4 rows down.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Set **Style** to **Standard** and draw the shallow line | All `-`, stepping down a row every five cells or so: at this slope the line is nearer flat than diagonal on screen |
| 2 | Set **Style** to **Standard by step** and draw the same line under it | Runs of `-`, with a `\` at each place the line steps down a row |
| 3 | Set **Style** to **Precise** and draw the same line under that | A smoother mix of characters such as `_`, `.`, `-` and `'` that follows where the line really runs through each cell |
| 4 | Draw a line straight down and one at about 45° on screen in **Precise** | Mostly `\|` for the straight one, and `\` (or `/` going up) for the diagonal |
| 5 | Set **Style** to **Single character**. Click the Character box in Tool Options, choose `*` in the Characters window, and draw a line | A line of `*` |
| 6 | Change **Style** back to **Standard**, press **B** then **U** | Style still says **Standard**: the tool remembers it |

### T4: Keeping the line straight

This checks the same modifier keys as the Move tool's drag: Shift for straight across or down, Ctrl for the 45°-looking diagonals, and Ctrl+Shift for one column per row.

**Also on macOS:** Yes, because Ctrl is Cmd there.

| # | Do this | Expected result |
|---|---|---|
| 1 | Hold **Shift** and drag at a slight angle, mostly to the right | The line stays straight across, on the row you started on |
| 2 | Still holding Shift, move the pointer mostly down | The line jumps to straight down, in the column you started on |
| 3 | Without Shift, drag at a slight angle, then press **Shift** without moving the mouse | The line straightens at once. Let go of Shift (still not moving) and it goes back to the angle |
| 4 | Hold **Ctrl** (Cmd on a Mac) and drag at different angles | The line snaps to the nearest diagonal that looks like 45° on screen, in whichever direction you drag |
| 5 | Hold **Ctrl+Shift** (Cmd+Shift) and drag | The line snaps to a diagonal of one column per row, which looks steeper than 45° |

### T5: Esc and undo

This checks that Esc throws away a line you're dragging, and that the tool stays chosen.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press and drag a line, and press **Esc** while still holding the button | The line disappears. Moving the pointer draws nothing more |
| 2 | Let go | Nothing is drawn, History has no new step, and the Line tool is still chosen |
| 3 | Press **Esc** again | The Hand tool is chosen, as Esc does whenever a tool isn't using it |
| 4 | Draw three lines, then press **Ctrl+Z** (Cmd+Z) three times | They go one by one, newest first |

### T6: Selection and locked layers

This checks that the line keeps to the selection, as every drawing tool does, and leaves a locked layer alone.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M** and select a rectangle in the middle of the canvas. Press **U** and draw a line from well outside the rectangle, across it, to the other side | Only the part inside the rectangle is drawn |
| 2 | Press **Ctrl+D** (Cmd+D) to deselect. In the Layers window, lock the layer, and try to draw a line | Nothing is drawn. Unlock it again |

### T7: The shared character

This checks that Single character and the Character Brush share one character, and that the Characters window keeps the Line tool when it's chosen.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Line tool and **Style** set to **Standard**, click `#` in the Characters window | **Style** changes to **Single character**, the box shows `#`, and the Line tool is still chosen. A line now draws `#` |
| 2 | Press **B** then **4** for the Character Brush | Its box shows `#` too |
| 3 | Click `@` in the Characters window, then press **U** | The Line tool's box shows `@`, and a line draws `@` |
| 4 | Press **B** then **1** for the Brush, and click `%` in the Characters window | The Character Brush is chosen, as before this step, with `%` |

### T8: A character the document can't hold

This checks the Character Brush's warning in Single character, and that the other styles don't need it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Line tool in **Single character**, choose **Blocks and Shades** in the Characters window and click the full block `█` | The box shows `█` |
| 2 | Choose **File → New**, choose **ASCII only** under **Characters**, and click **OK** (choose **Don't Save** if it asks) | Tool Options shows the warning "This character needs a Unicode document". Drawing a line draws nothing |
| 3 | Set **Style** to **Standard** | The warning goes, and a line draws in `-`, `\|`, `/` and `\` |

## Worth poking at (optional)

### P1: Off the canvas

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Draw a line that runs off the edge of the canvas | Only the cells on the canvas are drawn, as if the line went on past the edge |
| 2 | Press outside the canvas and drag onto it | The part of the line on the canvas is drawn |

### P2: Undo while dragging

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Draw one line. Then start a second, and while still holding the button press **Ctrl+Z** (Cmd+Z), then keep dragging | The second line goes and stops following the pointer. The first line stays. Letting go draws nothing |

### P3: Precise in other fonts

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Change the font with **Document → Font…** to each bundled font in turn, and draw the same shallow line in **Precise** each time | Each font may pick a little differently, for example `,` where another picks `.`, but each line follows its path |
