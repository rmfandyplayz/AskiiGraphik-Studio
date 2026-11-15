<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2b.3b: Brush, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 20 min

## What changed

There's a Brush tool (B) that paints colour onto what's already on the canvas: the FG brush colours text, and the BG brush colours backgrounds. A small sub-toolbar with **FG Brush** and **BG Brush** appears beside the Tools toolbar while the Brush is chosen, and keys 1 and 2 switch between them. This is the same choice as the FG and BG squares on the Brush Options toolbar. Alt+click (Option+click on a Mac) with the Brush picks a colour up from the canvas.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. **File → New**, choose **ANSI (the 16 standard colors)** under **Colors**, and click **OK** (choose **Don't Save** if it asks).
3. Open **Window → Swatches**, so you can choose colours.
4. With the Text tool (press **T** if it isn't chosen), click near the top left of the canvas and type `HELLO WORLD`. Press **Enter** and type `HELLO AGAIN` on the next row.

## Tests

### T1: Choosing the Brush and its two brushes

This checks the new tool, its sub-toolbar and keys 1 and 2.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Look at the Tools toolbar | Its buttons read **Hand**, **Text**, **Brush**, **Eraser**. |
| 2 | Click **Brush** | It looks pressed. A small toolbar appears beside the Tools toolbar with **FG Brush**, which looks pressed, and **BG Brush**. |
| 3 | Click **BG Brush** | It looks pressed and FG Brush doesn't. On the Brush Options toolbar, the **BG** square looks pressed too, and Swatches says **Changes the BG color**. |
| 4 | Click the **FG** square on the Brush Options toolbar | **FG Brush** looks pressed again. |
| 5 | Click an empty part of the canvas, then press **2**, then **1** | 2 presses **BG Brush**, and 1 presses **FG Brush**. Nothing is typed. |
| 6 | Press **T** | The Text tool is chosen and the sub-toolbar goes. Press **B**: the Brush is chosen and the sub-toolbar comes back. |
| 7 | Open the **Tools** menu | Hand, Text, Brush, Eraser, with the check mark on **Brush**. |
| 8 | Press **T**, click on an empty cell, and type **1** | A **1** is typed, and the FG/BG choice doesn't change. Press **Esc**, then **B**. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Painting text colours and backgrounds

This checks painting, None, undo and Esc.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With **FG Brush** chosen, click the red square in Swatches, then drag across the whole of `HELLO WORLD` and a few cells past it | The letters turn red. The space between the words and the cells past the end stay empty: no background appears. |
| 2 | Press **2**, click the blue square in Swatches, then drag along the same row, past the end of the text | Blue appears behind the letters, the space and the empty cells you dragged over. The letters stay red. |
| 3 | Open **Window → History** | Two new steps named **Paint**. |
| 4 | Press **Ctrl+Z** twice | First the blue goes, then the red. Press **Ctrl+Y** twice: both come back. |
| 5 | Click **None** in Swatches (BG is still chosen), and drag over part of the blue | The blue goes from those cells. Letters keep their red. |
| 6 | Press **1**, click **None**, and drag over a few red letters | They go back to the normal text colour. |
| 7 | Choose a colour again, press the mouse button on `HELLO AGAIN`, drag across it, and press **Esc** before letting go | The colour from this drag vanishes, no new History step appears, and the Brush is still chosen. |
| 8 | Right-click and drag across `HELLO AGAIN` | Nothing is painted. |
| 9 | Hold **Space** and drag | The canvas pans, and nothing is painted. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Painting across layers

This checks that the Brush paints the active layer only, and leaves locked and hidden ones alone.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | In the **Layers** window, add a new layer. It's now the active one | A second layer above the first. |
| 2 | With **FG Brush** and a green colour, drag across `HELLO AGAIN` | Nothing changes: the letters are on the other layer, and the new layer has no letters to colour. |
| 3 | Press **2**, choose yellow, and drag across `HELLO AGAIN` | Yellow appears behind the letters, which still show. |
| 4 | Hide the new layer | The yellow goes and the letters stay. Show it again: the yellow is back. |
| 5 | Lock the new layer and drag across another row | Nothing is painted. Unlock it. |
| 6 | Make the first layer active, press **1**, choose green, and drag across `HELLO AGAIN` | The letters turn green, with the yellow still behind them. |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Picking a colour with Alt+click

This checks the eyedropper, and that letting go of Alt doesn't open the menu bar.

**Also on macOS:** Yes, because it's Option+click there, and the menu bar works differently

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Brush and **FG Brush** chosen, choose white in Swatches. Hold **Alt** (Option on a Mac) and click a red letter of `HELLO WORLD` | The FG square on the Brush Options toolbar turns red. Nothing is painted, and no History step appears. |
| 2 | Let go of **Alt** | On Windows, the menu bar doesn't open and no letters are underlined in it. Tap **Alt** on its own: now the menu bar opens. Press **Esc** to close it. |
| 3 | Alt+click an empty cell | The FG square shows the red slash (None). |
| 4 | Press **2** and Alt+click a cell with a blue background | The BG square turns blue, and the FG one doesn't change. |
| 5 | Hold **Alt**, press the mouse button, and drag over cells with different backgrounds | The BG square follows the cell under the pointer. Let go. |
| 6 | Hold **Alt**, press the mouse button on a cell with another colour, and press **Esc** before letting go | The BG square goes back to what it was before you pressed. |
| 7 | Hide the top layer, then Alt+click a cell where it had yellow | It picks what you see without that layer: the colour below, or None. Show the layer again. |
| 8 | **Document → Monochrome**, and confirm. Alt+click a cell, then drag on the canvas | Both squares show the slash, as they do in any monochrome document, and nothing is painted. Press **Ctrl+Z** (Cmd+Z on a Mac): the canvas gets its colours back, and the squares show the colours they had before, so the Alt+click didn't change them. |

- [ ] Pass  - [ ] Fail. Notes:

### T5: RGB colours

This checks painting and picking with any colour.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | **File → New**, choose **RGB (any color)**, and type a word with the Text tool | A new document with the word. |
| 2 | Press **Esc**, then **B**. Open **Window → Color**, and pick an orange | The FG square turns orange. |
| 3 | Drag across the word | The letters turn that orange. |
| 4 | Choose another colour, then Alt+click one of the orange letters | The FG square is orange again, and its tooltip shows the same hex code as before. |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Saving and reopening

This checks that painted colours are saved.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Go back to the ANSI document from T1 to T4 (or paint a new one with both brushes on two layers), and **File → Save As** | Saved. |
| 2 | **File → New**, then **File → Open** the saved file | The text colours and backgrounds are back as you painted them, on both layers. |

- [ ] Pass  - [ ] Fail. Notes:

### T7: Tests to revisit

| Step | Test | Where | Why |
|---|---|---|---|
| 2b.3a follow-ups | T1 (Brush Options toolbar) | Windows | Not reported yet |
| 2b.3a follow-ups | T2 (pan pointer) | Windows and macOS | Not reported yet |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Undo with Ctrl+Z while still holding the mouse button in a stroke, then keep dragging.
- Switch the document to ANSI while an RGB colour is chosen, and paint: it should use the nearest of the 16.
- On a Mac, close the window while the Brush is chosen and reopen it from the Dock: the sub-toolbar should be there.

## Known limitations (not bugs)

- The Brush is one cell wide. Sizes come with M3's brushes.
- Right-click doesn't paint yet.
- There's no eyedropper pointer while Alt is held.
- The sub-toolbar is placeholder text buttons.
- The app still starts on the Text tool, so the Brush isn't remembered between runs.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2b.3b, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
