<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 1.4: Canvas control, manual test

**Build:** working tree (not pushed yet)
**Test on:** - [ ] Windows at 100%  - [ ] Windows at 150%  - [ ] macOS (Retina)  (Linux is covered by CI only)
**Time needed:** about 20 min

## What changed

The window now shows a canvas. It opens a temporary 500 × 500 sample document (rings of `.:-=+*#%@` shading, a border, and a block of text at the top-left), because New and Open don't exist yet. You can zoom, pan, show a cell grid, and see which cell is under the mouse in the status bar. Nothing can be edited yet; typing arrives in step 1.5.

Shortcuts below say **Ctrl**. On a Mac, use **Cmd** instead.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. For the 150% test, set Windows **Settings → System → Display → Scale** to 150%, then restart the app.

## Tests

### T1: The sample document appears, crisp

| # | Do this | Expected result |
|---|---|---|
| 1 | Start the app | The window shows the sample: text in a clear box at the top-left, shading rings around it, and a line border along the canvas edge. The status bar at the bottom says `Zoom 100%` and `500 × 500 cells`. |
| 2 | Look closely at the text block | Every character is sharp, not blurry or colour-fringed. The box-drawing shapes on the "Lines" row join up with no gaps. |
| 3 | Look at the "Arrows" row | `↔` is whole: both tips show, slightly narrower than the other arrows (this is the fix from your 1.3 report). |
| 4 | Look at the last text row | The Chinese, Japanese, smiley and note characters sit on **orange** cells. The last character is a hollow box on a **red** cell. These colours are temporary markers for "drawn with another font" and "no font has this". |
| 5 | If your system is in dark mode, switch it to light (or the other way round) | The canvas colours follow: dark paper with light text in dark mode, white paper with black text in light mode. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Zoom

| # | Do this | Expected result |
|---|---|---|
| 1 | Point at a letter in the text block, hold **Ctrl** and scroll the wheel up a few notches | The canvas zooms in step by step: 125%, 150%, 200%… The letter stays under the mouse pointer. The status bar shows the new zoom. |
| 2 | Keep zooming in | It stops at 800%. Text stays sharp at every step, never blurry or blocky. |
| 3 | Hold **Ctrl** and scroll down until it stops | It stops at 25%. The rings are still visible as a pattern. (A 500 × 500 canvas is still bigger than the window there.) |
| 4 | Press **Ctrl+0** | Back to 100%. |
| 5 | Press **Ctrl+=** (or Ctrl and the keypad +) twice, then **Ctrl+-** once | 125%, 150%, then 125%. The zoom centres on the middle of the window. |
| 6 | Open the **View** menu | It lists Zoom In, Zoom Out, Actual Size and Show Grid, with their shortcuts. At 800%, Zoom In is greyed out; at 25%, Zoom Out is. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Pan

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag the scrollbars on the right and at the bottom | The canvas scrolls. At each edge you see a grey margin around the canvas. |
| 2 | Scroll the wheel without Ctrl; then hold **Shift** and scroll | The wheel scrolls up and down; Shift + wheel scrolls sideways. (A touchpad scrolls both ways on its own.) |
| 3 | Hold the **middle mouse button** and drag | The canvas follows the mouse, like grabbing paper. |
| 4 | Click the canvas once, then hold **Space** | The pointer turns into a hand. |
| 5 | Still holding Space, drag with the left button. Then release Space | The canvas follows the mouse. After you release Space, the pointer goes back to normal, and a left drag no longer pans. |
| 6 | At 25%, 100% and 800%, pan around quickly with each method | It feels smooth and keeps up with the mouse, with no stutter. |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Grid and hover

| # | Do this | Expected result |
|---|---|---|
| 1 | Move the mouse over the canvas | The cell under the pointer is highlighted, and the status bar shows `Cell x, y` (counted from 0 at the top-left). |
| 2 | Move the mouse onto the grey margin, or out of the window | The highlight and the `Cell` text disappear. |
| 3 | Press **Ctrl+'** (the apostrophe key) | Faint lines appear between all cells. **View → Show Grid** now has a check mark. |
| 4 | Zoom out to 25% | The grid isn't drawn, because the cells are under 4 screen pixels wide; the check mark stays on. It comes back when you zoom in. With display scaling above 100%, cells are bigger, so it may stay visible. |
| 5 | Press **Ctrl+'** again | The grid disappears and **View → Show Grid** loses its check mark. |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Display scaling

| # | Do this | Expected result |
|---|---|---|
| 1 | Windows at 150% scaling: repeat T1 rows 1–3 and T2 rows 1–2 | Same results. Text is as sharp as at 100%, not blurry, and box-drawing lines still join. |
| 2 | Move the window between two monitors with different scaling, if you have them | The canvas redraws sharp on each monitor. |
| 3 | macOS (Retina): repeat T1 to T4 with Cmd instead of Ctrl | Same results. Text is Retina-sharp. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Resize the window very small, then very large, at different zoom levels.
- Zoom with Ctrl + wheel while the pointer is on the grey margin, or near a corner.
- Hold Space, start a drag, then release Space before releasing the mouse.

## Known limitations (not bugs)

- The sample document, its text and the canvas colours are temporary. You can't edit, save or open anything yet.
- The orange and red cell markers are placeholders until you design how missing glyphs are flagged.
- Touchpad pinch-to-zoom isn't supported yet; use Ctrl/Cmd + scroll.
- Space + drag will need a rule once typing arrives (step 1.5), since Space also types a space. Claude Code will ask then.
- The View menu, status bar and scrollbars are plain placeholder UI.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 1.4, test T<n>, row <#>
OS: <Windows 11 / macOS version>, display scaling: <100% / 150% / Retina>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
