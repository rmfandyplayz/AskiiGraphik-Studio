<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2b.3a follow-ups, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 10 min

## What changed

From your 2b.3a testing: Brush Options is now a toolbar laid out as "FG: [swatch] • BG: [swatch]", instead of a window. A colour of None shows as an empty square with a red slash. The Hand tool's pointer is no longer the link pointer: on a Mac it's the open hand, and on Windows it's the four-way move arrows, because Windows has no open hand built in. Space + drag uses the same pointer.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).

## Tests

### T1: The Brush Options toolbar

This checks the new toolbar and the None slash.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Look at the middle of the top edge | A toolbar reading **FG:**, a square button, a small dot, **BG:** and another square button. Both squares are empty with a red slash from top-left to bottom-right (None), and the FG button looks pressed. It appears for you even though you have a saved layout, because it's new. |
| 2 | Rest the pointer on each square | The FG tooltip starts **FG: None** and the BG one **BG: None**. Both say what clicking, X and D do, with D marked as temporary. |
| 3 | Open **Window** | The menu lists Layers, History, Color, Swatches, then the five toolbars, with **Brush Options Toolbar** last and ticked. There's no Brush Options window any more. |
| 4 | Open **Window → Swatches**, and click the red square | The FG square on the toolbar turns red, and its tooltip says **FG: Red** in an ANSI document, or **FG: #AA0000** in an RGB one. |
| 5 | Click the toolbar's **BG** square | It looks pressed and FG doesn't. The Swatches window says **Changes the BG color**. |
| 6 | Click **None** in Swatches | The BG square shows the red slash again. The None button in Swatches has the same slashed square before its name. |
| 7 | Click an empty part of the canvas, press **Esc**, then **X** | The two squares swap. Press **D**: both show the slash. |
| 8 | Right-click the toolbar's grip, choose **Vertical**, then put it back to **Horizontal** | Vertical: FG, its square, the dot, BG and its square run top to bottom. Horizontal: back as it was. |
| 9 | Drag the toolbar by its grip to another free spot, then drop it back | It moves like the other toolbars. While dragging, the FG/BG choice doesn't change. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: The pan pointer

This checks the pointer for the Hand tool and for Space + drag.

**Also on macOS:** Yes, because each system has its own pointers

| # | Do this | Expected result |
|---|---|---|
| 1 | Click an empty part of the canvas and press **Esc** to choose the Hand tool. Move the pointer over the canvas | On Windows: the four-way move arrows. On a Mac: an open hand. Not the pointing finger. |
| 2 | Drag across the canvas | It pans, with the same pointer. |
| 3 | Press **T**, then hold **Space** over the canvas and drag | The same pointer shows while Space is held, and it pans. Letting go of Space gives the normal pointer back. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Hide the Brush Options toolbar from the Window menu and show it again.
- Switch the document to monochrome with colours chosen: both squares show the slash, because a monochrome document holds no colours. Switching back shows them again.

## Known limitations (not bugs)

- The toolbar is placeholder UI, and it's a pixel shorter than the text-button toolbars beside it.
- On Windows the move arrows stand in for a hand. A real open hand would need a cursor image of our own, which belongs with your icon design.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2b.3a follow-ups, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
