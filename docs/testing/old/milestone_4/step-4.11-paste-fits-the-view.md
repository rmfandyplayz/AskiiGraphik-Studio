<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.11: A paste brings the view to fit it, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 15 min

## What changed

A paste that isn't fully in view now brings the view to it and zooms so the whole paste fits with some room round it: in as far as 150%, and out as far as 25%, always on one of the usual zoom steps. Before, only a paste that already fitted was brought to the middle, and the zoom never changed. The view glides there, zoom and all, in a quarter of a second, and nothing else can change the zoom until it's done. A paste that's fully in view still changes nothing.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, set the size to **200 × 100**, and click **OK**. Type a few short lines of letters with the **Text** tool near the top-left corner.

## Known limitations (not bugs)

- **Zooming out, glyphs blur slightly** for the first few frames of the glide, as the smaller zoom's glyphs are drawn bigger for a moment.
- **A click during the glide** lands where the view ends up, not where the moving picture shows it. The glide is short, and a click ends it at once.
- **Placeholders:** the room round the paste (the canvas margin, 64 pixels), the glide's length (0.25 s) and the rule that a paste out of view at over 150% zooms out to 150%. See `docs/TEMPORARY-UI.md`, "A paste brings the view to fit it".

## Tests

### T1: A paste fully in view

**Also on macOS:** No

This checks that a paste you can already see changes nothing.

| # | Do this | Expected result |
|---|---|---|
| 1 | With **Rectangle Select**, select two or three of your lines and press **Ctrl+C**. Put the pointer in the middle of the window, over the canvas, and press **Ctrl+V** | The paste lands under the pointer. The view and the zoom in the status bar don't change |

### T2: A small paste out of view

**Also on macOS:** Yes (the glide's look on a Retina screen)

This checks that a paste that lands partly out of view brings the view to it, zoomed in to 150%.

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **Ctrl+0** for 100%. Put the pointer near the right edge of the window, over the canvas, so a paste there would stick out past it, and press **Ctrl+V** | The view glides, zooming in, until the paste sits in the middle of the view. The status bar shows **150%** |
| 2 | Press **Ctrl+Z**. Press **Ctrl+=** until the status bar shows **300%**, then paste near the bottom edge of the window | The view glides, zooming out, to the paste, and the status bar shows **150%** |
| 3 | Watch the glide closely a few times (undo and paste again) | It's smooth: fast at first and slowing to a stop, with no jump at the start or the end |

### T3: A paste too big to fit

**Also on macOS:** No

This checks that a big paste zooms out to the largest step at which it fits, or to 25%.

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Rectangle Select** and press **Ctrl+-** until the status bar shows **50%**. Select a rectangle across the canvas's whole width and about 20 rows tall, and press **Ctrl+C**. Press **Ctrl+0**, put the pointer in the middle of the window, and press **Ctrl+V** | The paste is wider than the window, so the view glides, zooming out, until the whole paste fits with some room round it. The zoom in the status bar is one of the usual steps |
| 2 | Press **Ctrl+=** once | The paste no longer fits in the window |
| 3 | Press **Ctrl+0**, then **Ctrl+A** and **Ctrl+C**, and paste with the pointer near a corner of the window | The view zooms out until the whole pasted canvas fits with room round it, going no further out than **25%** |

### T4: Nothing else changes the zoom during the glide

**Also on macOS:** No

A quarter of a second is too short to try things during it, so this makes the glide longer for a while.

| # | Do this | Expected result |
|---|---|---|
| 1 | Close the app. In `src/AskiiArt.App/Theme/Tokens.axaml`, change `AppCanvasZoomGlideMilliseconds` from `250` to `4000`, and run the app again. Set up as in Setup | The app starts as normal |
| 2 | Copy a few lines with **Rectangle Select**, press **Ctrl+0**, and paste near the right edge of the window. While it glides, open the **View** menu | **Zoom In**, **Zoom Out** and **Actual Size** are greyed out |
| 3 | Undo, paste again near the edge, and during the glide press **Ctrl+=**, **Ctrl+-**, **Ctrl+0**, and turn the wheel with **Ctrl** held | The zoom doesn't change, and the glide carries on |
| 4 | Once it's done, open the View menu and press **Ctrl+=** | The items are available again and the zoom changes |
| 5 | Undo, paste near the edge again, and during the glide drag with the middle mouse button | The view jumps to the end of the glide at once, then pans with the drag |
| 6 | Undo, paste near the edge again, and during the glide turn the wheel without Ctrl | The view jumps to the end of the glide at once, then scrolls |
| 7 | Close the app and put `AppCanvasZoomGlideMilliseconds` back to `250` | |

### T5: Reduce motion

**Also on macOS:** No

This checks that with reduce motion on, the view jumps rather than glides.

| # | Do this | Expected result |
|---|---|---|
| 1 | Close the app. Open `%APPDATA%\AskiiGraphiks Studio\settings.json` in Notepad and add `"reduceMotion": "true",` on a new line just after the first `{` (the quotes round `true` matter). Save, then start the app and set up as in Setup | The app starts as normal |
| 2 | Repeat T2's row 1 | The view jumps straight to the paste at 150%, with no glide |
| 3 | Close the app, delete the line you added, save, and start the app again | The view glides again |

## Worth poking at (optional)

### P1: Pastes near the canvas's edges

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Paste a few lines where they land partly past the canvas's right or bottom edge, with the pointer near the window's edge | The view brings the whole paste into view, even the part past the canvas |
| 2 | Paste with the pointer off the canvas view (over a toolbar), after scrolling the copied spot out of view | The paste goes where it was copied from, and the view glides there |
