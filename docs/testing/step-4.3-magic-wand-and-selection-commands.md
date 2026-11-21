<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.3: Magic Wand and selection commands, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 30 min

## What changed

There's a Magic Wand. Click a cell and it selects the cells that match it: the same character, about as dark a character, or a close text or background colour. It can take only the matching cells joined to the one you clicked, or every match on the canvas, and look at the active layer or at every visible layer together. Dragging sideways during the click changes the tolerance, and you see the result before letting go. It's on **W**, and it's the fifth tool of the **M** group.

The Select menu adds Invert, Grow by 1 Cell, Shrink by 1 Cell, Select Cells with Glyphs and Select Cells with Background Color.

From your 4.2c testing: the hatching is now black and white stripes side by side, like a zebra, and Subtract's and Intersect's dashes are lighter so they show over their hatching.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, choose **RGB (any color)**, keep the other defaults and click **OK**. Choose **Don't Save** if it asks.
3. Press **T** and click the canvas's top-left cell. Type `aaaa..aa`, press **Enter**, then type `a..a..aa`. Press **Esc**.
   - From here on, "the art" means these two lines. The `a`s on the left make one connected shape, and the two `aa` blocks on the right are joined to each other but not to it.
4. Choose **View → Zoom In** two or three times, so cells are easy to click.
5. Open **Window → History**, so you can see each undo step.

## Known limitations (not bugs)

- **Placeholders:** the Magic Wand's options, their names and defaults, the menu names and the Invert shortcut are placeholders, listed in `docs/TEMPORARY-UI.md` under "Magic Wand and selection commands".
- **No dialogs:** Grow and Shrink always go one cell. Dialogs to choose how far come later.
- **No icons:** the tool buttons are still text.

## Visual tests (poke around and try these)

- **Zebra hatching.** Select something with **M** then **1**. Over everything that isn't selected, the hatching is black and white stripes side by side, each about 12 pixels wide, with nothing see-through between them. Both colours are faint (40%), so the art still shows through.
  - Check it in dark and light mode.
  - It still glides to the top left without flickering.
- **Subtract's dashes.** Drag a rectangle with **Alt** held, over part of the selection. The running dashes round the shape are a bright, pale red that stands out from the red hatching.
- **Intersect's dashes.** Drag with **Shift+Alt** held. The dashes are a pale light blue that stands out from the blue hatching.
- **Ragged selections.** Select every `.` with the wand (Global), then zoom and pan around. The outline and hatching keep up smoothly.

## Tests

### T1: Choosing the Magic Wand

Checks that W picks the wand, that it's the M group's fifth tool, and that Tool Options shows its options.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **V**, then **W** | The **Select** button looks pressed, and its sub-toolbar shows **Magic Wand** pressed, after Polygonal Lasso |
| 2 | Look at the Tool Options toolbar | After the four **Mode** buttons: **Contiguous** (pressed) and **Global**, **Active Layer** (pressed) and **Visible Layers**, a dropdown showing **Same Glyph**, and a **Tolerance** slider at 10% |
| 3 | Press **1**, then **5** | 1 picks Rectangle Select, and 5 picks the Magic Wand again |
| 4 | Press **V**, then **M** | The Magic Wand comes back, since it was the group's last tool |
| 5 | Open the **Tools** menu | **Magic Wand** is listed after Polygonal Lasso, with the check mark |

### T2: Same Glyph, contiguous and global

Checks the default matching: the same character, only the joined cells, then every match.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Magic Wand, click the first `a` of the art | All the `a`s of the left shape are selected (the first four on line 1, and the first and fourth on line 2). The `aa` blocks on the right aren't. History adds **Magic Wand** |
| 2 | Click **Global** on Tool Options, then click the first `a` again | Every `a` is selected, the right-hand blocks too |
| 3 | Click one of the `.`s | Every `.` is selected, and nothing else |
| 4 | Click **Contiguous**, then click an empty cell below the art | Every empty cell joined to it is selected: the whole empty canvas around the art, but not the `.`s, which aren't empty |
| 5 | Click a cell just right of the art on line 1, past the last `a` | The same: empty cells join up round the art's right end |

### T3: Spaces count as empty

Checks that a space and an empty cell match each other.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **T**, click the cell just below the art's first `a` (line 3), type two spaces, and press **Esc**. Press **W** | Nothing looks different: spaces look like empty cells |
| 2 | Click one of the two cells where you typed the spaces | They're selected along with every other empty cell joined to them |

### T4: Lightness and the tolerance drag

Checks matching by how dark a character looks, and that dragging sideways changes the tolerance and the preview.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **T**, click the first cell of line 4, type `.:-=+*#%@`, press **Esc**, then press **W** | A row of characters from light to dark |
| 2 | In the **Match** dropdown, choose **Lightness**. Set **Area** to **Global**. Drag the **Tolerance** slider to **0%**, then click the `+` | Only characters exactly as dark as `+` are selected (the `+` itself) |
| 3 | Press the mouse button on the `+` and hold it. Without letting go, drag slowly to the right | The slider moves up as you drag, about 1% every 2 screen pixels. The preview grows to take in characters either side of `+` that are close in darkness |
| 4 | Still holding, drag back to the left past where you started | The slider goes down to 0% and the preview shrinks back |
| 5 | Drag right again, then let go | The selection is what the preview showed, and the slider stays where it was |
| 6 | Press and hold on the `+`, drag right a long way, then press **Esc** and let go | The preview disappears, the selection is as it was before, and the slider is back where it was when you pressed |
| 7 | Press and hold, drag up and down without moving sideways | The slider doesn't move |

### T5: Text and background colours

Checks matching by colour, within the tolerance.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **B**, then **2** for the BG Brush. Click **Red** in Swatches, and paint a short streak below the art. Then pick a slightly different red with the BG colour picker (change one of its numbers by about 10) and paint a second streak that touches the first. Then paint a blue streak somewhere else | Three streaks: two reds touching, and a blue |
| 2 | Press **W**. Choose **Background Color** in Match, set **Area** to **Contiguous**, Tolerance **10%**, and click the first red streak | Both red streaks are selected, since their reds are close. The blue isn't |
| 3 | Set Tolerance to **0%** and click the first red streak again | Only the first red streak is selected |
| 4 | Click an empty cell away from the streaks | Every cell with no background joined to it is selected, the art's cells included, since they have no background either |
| 5 | Press **B**, then **1** for the FG Brush, pick a colour, and paint over the left `a` shape. Press **W**, choose **Text Color**, set **Area** to **Global** and click one of the painted `a`s | The `a`s you painted are selected. Characters left in the default colour aren't, and neither are empty cells |

### T6: Visible layers

Checks that the wand can look at every visible layer together, and leaves hidden layers out.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Layer → New**. Press **T**, click the art's second `a` on line 1, type `b`, press **Esc**, then press **W** | The new layer is active, and the canvas shows `abaa..aa` on line 1 |
| 2 | Set Match to **Same Glyph**, **Area** to **Contiguous**, **Sample** to **Active Layer**, and click the first `a` | The new layer holds only the `b`, so the cell you clicked is empty there: every cell except the `b` is selected |
| 3 | Set **Sample** to **Visible Layers** and click the first `a` | The wand sees the canvas as it looks: the `b` splits line 1, so the first `a` and the `a` below it are selected, and the `a`s after the `b` aren't |
| 4 | In **Window → Layers**, hide the new layer, then click the first `a` again | The hidden `b` is left out, so the whole left `a` shape is selected again |
| 5 | Show the new layer again | It's visible again |

### T7: Modes with the Magic Wand

Checks that the wand combines with the selection like the other selection tools.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Set Match to **Same Glyph**, Area **Global**, Sample **Active Layer**. Press **M**, then **1**, and select the bottom-left area of the canvas, away from the art. Press **W** | The rectangle is selected |
| 2 | Hold **Shift**, press on a `.` and hold the button | The preview shows the rectangle and every `.` together |
| 3 | Let go | Both are selected. History adds **Magic Wand** |
| 4 | Hold **Alt** and click a `.` | The `.`s leave the selection, and the preview showed them hatched in red before you let go |
| 5 | Click outside the canvas, with no keys held | Everything is deselected. History adds **Deselect** |

### T8: Invert

Checks Select → Invert and its shortcut.

**Also on macOS:** Yes, because the shortcut is Cmd+Shift+I there

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **M**, then **1**, and drag a rectangle | The rectangle is selected |
| 2 | Choose **Select → Invert** | Everything except the rectangle is selected. History adds **Invert Selection** |
| 3 | Press **Ctrl+Shift+I** (**Cmd+Shift+I** on a Mac) | The rectangle alone is selected again |
| 4 | Choose **Select → Deselect**, then **Select → Invert** | The whole canvas is selected |

### T9: Grow and Shrink

Checks that each goes one cell, corners included, and that Shrink keeps cells along the canvas's edges.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Select → Deselect**, then open the **Select** menu | **Grow by 1 Cell** and **Shrink by 1 Cell** are greyed out |
| 2 | Press **M**, then **1**. Press on a cell in the middle of the canvas, drag to the next cell and back, and let go, so that one cell is selected. Choose **Select → Grow by 1 Cell** | A 3 by 3 block is selected, centred on that cell, corners included. History adds **Grow Selection** |
| 3 | Choose **Select → Shrink by 1 Cell** | Only the middle cell is left. History adds **Shrink Selection** |
| 4 | Choose **Select → Shrink by 1 Cell** again | Nothing is left selected |
| 5 | Choose **Select → Select All**, then **Select → Shrink by 1 Cell** | Nothing changes, and History adds nothing: cells along the canvas's edges stay |
| 6 | Select a rectangle touching the canvas's left edge, then **Shrink by 1 Cell** | It loses a cell on its top, right and bottom sides, but not along the canvas's edge |

### T10: Select Cells with Glyphs and with Background Color

Checks the two commands that select by what the active layer holds.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | In **Window → Layers**, click the bottom layer (the one with the art and the streaks) to make it active | It's highlighted |
| 2 | Choose **Select → Select Cells with Glyphs** | Every character you typed on this layer is selected, the `.`s too. The spaces you typed in T3 and the empty cells aren't. History adds **Select Cells with Glyphs** |
| 3 | Choose **Select → Select Cells with Background Color** | Only the streaks you painted are selected. History adds **Select Cells with Background Color** |
| 4 | Make the top layer active, then choose **Select → Select Cells with Glyphs** | Only the `b` is selected, since the top layer holds nothing else |

## Worth poking at (optional)

### P1: An undo during a Magic Wand click

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select something with **M** then **1**. Press **W**, hold **Shift**, press on a `.` and keep holding. Press **Ctrl+Z** | The preview at once shows the selection from before your rectangle, combined with the `.`s |
| 2 | Let go | That's what's selected |

### P2: A big canvas

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make a new document 500 by 500, type a few lines, and click an empty cell with the wand (Contiguous) | The preview shows straight away, with no noticeable wait |
| 2 | Press and drag sideways | The preview keeps up with the drag |
