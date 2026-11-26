<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 5.4: Box-drawing styles and joins, manual test

**Build:** the commit that adds this file
**Test on:** Windows (Linux and macOS are covered by CI only for this step)
**Time needed:** about 25 min

## What changed

The shape tools' **Style** has three new box-drawing styles: **Box** (`─ │ ┌`), **Heavy box** (`━ ┃ ┏`) and **Double box** (`═ ║ ╔`). A new **Include joins** button, off at first, makes lines and outlines join the lines already there instead of drawing over them. A line across `│` makes `┼`, one ending on it makes `┤`, and plain-text lines join as `+`. In an ASCII-only document the box styles draw as Standard, and Tool Options says so.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Start a new document. If **Document → ASCII Only** has a check mark, click it so the document is Unicode.
3. Open **Window → History** if it isn't open already.
4. In these tests, "draw an upright line" means: with the Line tool, hold **Shift** and drag straight down.

## Known limitations (not bugs)

- **Include joins shows in Single character too,** where it does nothing. Tool Options can't yet hide an option by another option's value.
- **Only the active layer joins.** Lines on other layers are drawn over as if they weren't there.
- **Diagonals don't join.** `╱` drawn over `╲` replaces it, rather than making `╳`.
- **Heavy box and Double box have no rounded corners,** so Rounded corners leaves theirs square.
- **Placeholders:** the style names, where they sit in the list, the button and the warning's wording. See `docs/TEMPORARY-UI.md`, "Box-drawing styles and joins".

## Tests

### T1: The new options

This checks that the box styles and Include joins are offered by every shape tool.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click the canvas once, press **U**, then **1** for the Line tool, and open the **Style** dropdown | **Standard**, **Standard by step**, **Precise**, **Box**, **Heavy box**, **Double box**, **Single character** |
| 2 | Look at Tool Options | **Style**, then an **Include joins** button that isn't down, then **Character** |
| 3 | Press **2** (Rectangle), then **3** (Ellipse) | Both show **Include joins** right after **Style**, still up |
| 4 | Click **Include joins**, then press **1** | It's down on the Line tool too: the three tools share it. Click it again so it's up |

### T2: Drawing in the box styles

This checks the characters each box style draws, with Include joins up.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Set **Style** to **Box**. Draw a line across, an upright line, and a line going down and to the right at about 45° | `─────`, a column of `│`, and `╲╲╲` |
| 2 | Draw a line going up and to the right | `╱╱╱` |
| 3 | Press **2** and draw a rectangle | `┌───┐` at the top, `└───┘` at the bottom, and `│` down the sides |
| 4 | Set **Style** to **Heavy box**, then **Double box**, drawing a rectangle each time | `┏━━━┓` over `┗━━━┛`, then `╔═══╗` over `╚═══╝` |
| 5 | Press **3** and draw an ellipse in each box style | Its top and bottom are the style's across line, its sides the upright line, and its slopes `╱` and `╲` |

### T3: Rounded corners in the box styles

This checks what Rounded corners does in each box style.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **2**, click **Rounded corners** so it's down, set **Style** to **Box** and draw a rectangle | `╭───╮` over `╰───╯` |
| 2 | Set **Style** to **Heavy box**, then **Double box**, drawing a rectangle each time | Square corners, as in T2 row 4 |
| 3 | Click **Rounded corners** so it's up again | |

### T4: Lines joining lines

This checks joins where a line crosses another or ends on it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **1**, set **Style** to **Box**, and draw an upright line about 10 rows long | A column of `│` |
| 2 | Click **Include joins** so it's down. Draw a line across that passes right through the upright line | `┼` where they cross |
| 3 | Draw a line across that starts left of the upright line and ends on it | `┤` where it ends: the join reaches only the side the line came from |
| 4 | Draw one that starts on the upright line and goes right | `├` |
| 5 | Draw an upright line that ends on one of your lines across, coming from above | `┴` where it ends |
| 6 | Press **Ctrl+Z** (Cmd+Z on a Mac) once | Only the last line goes, and the `┴` becomes `─` again |
| 7 | Set **Style** to **Heavy box** and draw across the first upright line | `┿`: the heavy line keeps its weight across, and the light one up and down |
| 8 | Set **Style** to **Double box** and draw across a heavy upright line (draw one first in Heavy box) | `╬`. No character mixes double and heavy lines, so the whole join is double |

### T5: Shapes joining

This checks rectangles that share edges, and rounded corners that join.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Clear the canvas (select all with **Ctrl+A** or Cmd+A, then **Delete**). Press **2**, set **Style** to **Box**, keep **Include joins** down, and draw a rectangle | `┌───┐` and so on |
| 2 | Draw a second rectangle whose left side lies on the first one's right side, the same height | Where they share a side, the corners are `┬` at the top and `┴` at the bottom, with `│` between |
| 3 | Draw a rectangle across the middle of the first one, so their sides cross | `┼` wherever a side crosses a side |
| 4 | Click **Rounded corners** so it's down, and draw a rectangle whose left corners lie on an upright line | Its left corners become `├`, while its right corners stay `╮` and `╯`. Click **Rounded corners** again so it's up |

### T6: Plain-text styles join as `+`

This checks the styles that aren't box-drawing.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **1**, set **Style** to **Standard**, and draw a line across the `│` lines from earlier | `+` wherever it crosses or ends on one |
| 2 | Draw an upright line in **Standard**, then a line across it in **Box** | `┼`: a box line joins a plain one too |
| 3 | Set **Style** to **Precise** and draw a flat line across an upright line | `+` where it crosses, if Precise drew `-` there |

### T7: What doesn't join

This checks the cases that draw over, as before.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click **Include joins** so it's up, and draw a **Box** line across an upright line | `─` all the way: the line draws over, as in 5.3 |
| 2 | Click **Include joins** so it's down. Set **Style** to **Single character**, choose `─` in the Characters window, and draw across an upright line | `─` all the way: Single character never joins |
| 3 | Set **Style** to **Box** and draw a line across some typed letters and a `╱` line | `─` all the way: other characters and diagonals are drawn over |
| 4 | Press **2**, set **Fill** to **Character**, then choose `#` in the Characters window, and draw a rectangle around part of an upright line | The outline joins where it crosses the line, and the inside is all `#`: the fill doesn't join. Set **Fill** back to **None** |

### T8: An ASCII-only document

This checks the box styles where box-drawing characters aren't allowed.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Start a new document and choose **Document → ASCII Only**. Press **U**, then **1**, and set **Style** to **Box** | Tool Options says "Box-drawing lines need a Unicode document, so they're drawn in Standard" |
| 2 | With **Include joins** down, draw an upright line, then a line across it | `|` and `-`, with `+` where they cross |
| 3 | Press **2**, click **Rounded corners** so it's down, and draw a rectangle | `.----.` over `'----'`, as Standard draws it. Click **Rounded corners** so it's up |
| 4 | Set **Style** to **Standard** | The warning goes |
| 5 | Set **Style** to **Box** again, then click **Document → ASCII Only** so it's off | The warning goes, and new lines are box-drawing ones |

### T9: Joins while dragging

This checks that a join follows the line while it's dragged and is undone with it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | In a Unicode document, with the Line tool, **Box** and **Include joins** down, draw an upright line | A column of `│` |
| 2 | Press left of it and drag right across it, slowly, without letting go | `┼` appears when the line reaches across the upright line |
| 3 | Still holding, drag back left until the line no longer reaches it, then let go | The upright line is whole `│` again, with no leftover join. History has one **Line** step for this drag |
| 4 | Drag a line across it again, let go, then press **Ctrl+Z** (Cmd+Z) | The line goes, and the `┼` is `│` again |

## Worth poking at (optional)

### P1: Fonts

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Draw a few joined boxes in each box style, then change the document's font to each bundled font in turn | The lines meet without gaps in every font |

### P2: Layers

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Draw a Box rectangle, add a new layer, and with **Include joins** down draw a line across the rectangle on the new layer | The new line draws straight across, as `─`: it doesn't join the other layer's lines |
