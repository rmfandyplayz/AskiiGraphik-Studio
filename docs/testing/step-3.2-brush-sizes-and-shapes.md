<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 3.2: Brush sizes and shapes, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 25 min

## What changed

The Brush and the Eraser have a size (1 to 100), a shape (Square or Circle) and proportions. **Perceptual** looks square or round on screen, so it's fewer cells tall than wide. **Mathematical** counts cells, so a size-10 square is 10 × 10 cells. A new **Tool Options** toolbar shows the chosen tool's options, and `[` and `]` make the brush smaller and bigger. An outline under the pointer shows which cells a stroke would cover. The Brush Options toolbar is now called **Colors**, and the toolbars start in your new default layout.

## Setup

1. Run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **Window → Reset Layout**. Your saved layout keeps the toolbars where they were, so this gives you the new default.
3. **File → New**, choose **ANSI (the 16 standard colors)** under **Colors**, and click **OK** (choose **Don't Save** if it asks).
4. Open **Window → Swatches**, so you can choose colours.

## Known limitations (not bugs)

- **The default bottom row doesn't fit windows narrower than about 1,750 px,** so View and Edit shrink and scroll. The app's smallest window is 1,280 px. Do you want a different layout, smaller toolbars, or a bigger minimum? (Recorded in TEMPORARY-UI.md.)
- The Tool Options toolbar is plain placeholder UI, for the UI Pass (M8) to redesign.
- Sizes and shapes aren't remembered after the app closes. Saving settings comes in step 3.5.
- The Brush and the Eraser always have one size and shape each, as agreed.
- A layout saved before this step keeps its old places until Reset Layout, and the Colors toolbar starts in its default corner, not where Brush Options was.

## Tests

### T1: The new default layout

Checks that the toolbars start where your screenshot put them, with the two new ones.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make the window as big as your screen allows, and look along the top edge of the canvas | Left to right: **File** (New, Open, Save) in the top-left corner, **Tool Options** in the middle, and **Colors** (FG and BG swatches) in the top-right corner, beside the widgets |
| 2 | Look along the bottom edge | Left to right: **View** (Zoom Out, Zoom In, Actual Size, Show Grid) in the bottom-left corner, **Tools** (Hand, Text, Brush, Eraser) in the middle, and **Edit** (Undo, Redo, Resize Canvas, Font) in the bottom-right corner, beside the widgets |
| 3 | Look at the widgets on the right | Layers above History, as before |
| 4 | Make the window narrower, a bit at a time | Below about 1,750 px wide, View and Edit shrink and fade at their ends, with the rest of their buttons a scroll away. This is expected (see Known limitations) |

### T2: The Window menu's new toolbars

Checks the two new Window menu items, which turn the Tool Options and Colors toolbars off and on.

**Also on macOS:** Yes, because the Window menu is the Mac's own menu bar there

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Window** menu | After the File, Tools, Edit and View toolbar items come **Tool Options Toolbar** and **Colors Toolbar**, both check-marked. There's no Brush Options item |
| 2 | Choose **Tool Options Toolbar** | The Tool Options toolbar goes, and the menu item loses its check mark. Choose it again: the toolbar comes back in the top middle |
| 3 | Choose **Colors Toolbar** twice | The Colors toolbar goes, then comes back in the top-right corner |

### T3: Tool Options follows the chosen tool

Checks that the toolbar shows the options of whichever tool is chosen, and that the Brush and the Eraser each have their own.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **V** for the Hand tool, and look at Tool Options | It says **No options** |
| 2 | Press **T** for the Text tool | Still **No options** |
| 3 | Press **B** for the Brush | Tool Options grows to show **Size** with a slider and the number **1**, **Shape** with **Square** and **Circle** (Circle looks pressed), and **Proportions** with **Perceptual** (pressed) and **Mathematical**. It stays in the middle of the top edge as it grows |
| 4 | Drag the Size slider to about 8, then click **Square** and **Mathematical** | The number follows the slider. Square and Mathematical look pressed, and Circle and Perceptual don't |
| 5 | Press **E** for the Eraser | Size is **1**, with Circle and Perceptual pressed: the Eraser has its own settings |
| 6 | Press **B** again | The Brush's settings are as you left them in row 4 |

### T4: Changing the size with [ and ]

Checks the two size keys, the limits of 1 and 100, and that the slider leaves the keyboard with the canvas.

**Also on macOS:** Yes, because `[` and `]` sit in different places on some keyboards

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Brush chosen, drag the Size slider to **5**, then press **]** three times | The size reads **8**, and the slider moves with it. Nothing is typed on the canvas |
| 2 | Press **[** once | It reads **7** |
| 3 | Drag the slider all the way left, then press **[** | It stays at **1** |
| 4 | Drag the slider all the way right, then press **]** | It reads **100**, and stays there |
| 5 | Press **E**, press **]** twice, then press **B** | The Eraser's size went from 1 to 3, and the Brush's is still 100 |
| 6 | Press **T**, click on an empty cell and type `[ab]` | `[ab]` is typed: the keys only change the size for the Brush and the Eraser. Press **Esc** |

### T5: The outline under the pointer

Checks the outline that shows which cells the brush would cover.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **B**, set Size to **1**, and move the pointer over the canvas | A thin outline goes around the single cell under the pointer, in the same colour as the text caret. The usual hover highlight doesn't show |
| 2 | Set Size to **5**, with **Square** and **Mathematical** | The outline is a 5 × 5 square of cells, centred on the cell under the pointer, and follows the pointer cell by cell |
| 3 | Click **Circle** | The outline is a rounded shape, 5 cells wide and tall, with its four corner cells cut off (see T6's pictures) |
| 4 | Move the pointer slowly off the left edge of the canvas | The whole outline keeps following, including the part outside the canvas, because a big brush still paints the cells it reaches. The hover highlight never shows |
| 5 | Set Size to **4**, and move the pointer slowly from the left half of a cell to its right half | The outline shifts one cell to the right as you cross the cell's middle. With an even size there's no middle cell, so it leans towards the side the pointer is on |
| 6 | Press **V** for the Hand tool, then **T** for the Text tool, and move the pointer over the canvas | No outline for either. The usual hover highlight shows instead |

### T6: The shapes the brush paints

Checks the cells each shape covers. The BG brush paints the background of every cell under it, even empty ones, which makes the shapes easy to see.

**Also on macOS:** No

In the pictures, `#` is a painted cell and `.` isn't.

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **B**, then **2** for the BG brush. Click **Red** in Swatches. Set Size **5**, **Square**, **Mathematical**, and click once on an empty part of the canvas | A red square, 5 cells wide and 5 tall, centred where you clicked |
| 2 | Set Size **3**, **Circle**, and click once somewhere else | A plus: `.#.` / `###` / `.#.` |
| 3 | Set Size **5**, and click | `.###.` / `#####` / `#####` / `#####` / `.###.` |
| 4 | Set Size **10**, and click | A circle 10 cells wide and 10 tall. Its rows are 4, 6, 8, 10, 10, 10, 10, 8, 6 and 4 cells wide. Because cells are taller than they're wide, it looks like a tall oval on screen |
| 5 | Click **Perceptual**, and click | 10 cells wide and 5 tall, rows of 6, 8, 10, 8 and 6 cells. On screen it looks about round |
| 6 | Click **Square**, and click | 10 cells wide and 5 tall. On screen it looks about square |
| 7 | Set Size **3**, and click | One row of 3 cells: at size 3 a perceptual brush is only one cell tall in this font |
| 8 | Press **Ctrl+Z** seven times | Each press takes away one of the shapes, newest first |

### T7: Painting and erasing with a big brush

Checks whole strokes: no gaps, one undo step each, Esc, and the Eraser.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the BG brush and Red, set Size **6**, **Circle**, **Perceptual**. Drag quickly in a long zigzag across the canvas | An unbroken red band 6 cells wide, with no gaps even where you moved fast |
| 2 | Look at the History widget | The stroke is one step, **Paint** |
| 3 | Press the mouse button, drag across the canvas, and press **Esc** before letting go | The colour from this drag vanishes, and no new History step appears |
| 4 | Press **T**, click near the top left and type a few rows of letters, such as `HELLO WORLD` three times on three rows. Press **Esc** | Three rows of text |
| 5 | Press **B**, then **1** for the FG brush. Click **Blue**, and drag a stroke across the text (the Brush is still size 6) | The letters under the stroke turn blue. Empty cells stay as they were |
| 6 | Press **E** for the Eraser, set Size **3**, **Square**, **Mathematical**, and drag across the middle of the text | A band 3 cells tall is erased: letters, their colour and the background. The outline is a 3 × 3 square |
| 7 | Press **Ctrl+Z** | The erasing comes back in one step |
| 8 | While dragging with the Eraser, press **]** before letting go | The stroke keeps its size to the end, and the outline grows when you let go |

### T8: Tool Options in a vertical toolbar

Checks that the toolbar still works when it's turned.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Brush chosen, right-click the Tool Options toolbar and choose **Vertical** | The options stack downwards: Size, then Shape, then Proportions. Each keeps its own row, with its name and controls side by side |
| 2 | Change the size with the slider and with **]**, and click **Square** | They work as before |
| 3 | Press **V** | It shrinks to **No options** |
| 4 | Choose **Window → Reset Layout** | Tool Options is horizontal again in the top middle |

## Worth poking at (optional)

### T9: The biggest brush on a small canvas

Checks that a brush much bigger than the canvas paints only the canvas, and stays smooth.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | **Edit → Resize Canvas** to 20 × 10. Press **B**, then **2** for the BG brush, click **Green**, and set Size **100**, **Circle**, **Perceptual** | The outline is far bigger than the canvas and goes past its edges |
| 2 | Click in the middle of the canvas | The whole canvas turns green, and nothing is painted outside it |
| 3 | Click **Mathematical**, and drag around for a few seconds | The outline follows the pointer without stutter |

### T10: Holding ] down

Checks the size keys with the key held, so it repeats.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Brush chosen and Size **1**, hold **]** down for a few seconds | The size climbs steadily, the outline grows with it, and it stops at **100** |
| 2 | Hold **[** down | It falls back to **1** and stops there |

### T11: Another font's cell shape

Checks that a perceptual brush follows the font: its height depends on how tall the font's cells are.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose the Brush with Size **10**, **Circle** and **Perceptual**, and look at the outline | It looks about round on screen |
| 2 | **Edit → Font**, and choose another monospace font installed on your computer | The canvas redraws in the new font. The outline still looks about round, though it may be a row taller or shorter than before |

### T12: Hidden and locked layers

Checks that a big brush leaves a layer you can't edit alone, as the one-cell brush did.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Hide the active layer in the Layers widget, then drag a size-5 stroke with the BG brush, then with the Eraser | Nothing changes and no History step appears. The outline still shows |
| 2 | Show the layer again, lock it, and repeat | The same: nothing changes |

### T13: Tool Options somewhere else

Checks that the toolbar grows and shrinks well away from its default place.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag Tool Options by its grip into empty space in the middle of the canvas, and drop it there | It floats where you dropped it |
| 2 | Switch between the Hand and the Brush a few times | It grows to show the Brush's options and shrinks to **No options**, without jumping away from where it is |
