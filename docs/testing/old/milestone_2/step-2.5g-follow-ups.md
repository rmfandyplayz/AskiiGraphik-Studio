<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.5g: Follow-ups, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 15 min

## What changed

These are the fixes from your retest of `step-2.5g-opening-rules-and-polish.md`. Everything else in it passed, so this covers only the changed parts.

- **Squeezed dock windows:** a window squeezed to its tab strip now has drop zones above and below it. The bottom quarter of its strip and the top quarter of the next one's put the held window between them. This was T3 row 3.
- **No room to spare:** when the windows in a stack are all as short as they go, nothing moves, and the marker lies across the border, on top of the windows. That's your suggestion, but only for that case. Where the stack can give up the room, the windows still move apart, as everywhere else, so the two never look different for no reason.
- **Crowded tab strips:**
  - A tab that's dropped in now scrolls into view, instead of the strip jumping back to its start.
  - Clicking a tab scrolls it fully into view, clear of the fades.
  - The strip starts from where it was, so clicking a tab already in view doesn't move anything.
- **A widget side's edge strip:** the marker there is now 48 wide, as on an empty side, with the same gap of 8 on all four sides.

## Setup

1. Pull, then from the repo root run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Choose Window → Reset Layout before each test. That leaves Layers above History on the right, as widgets.
3. Make the app window wide, about 1400 pixels or more, except where a test says otherwise.

Words used below, as in the last test doc:

- **A dock** is a side whose windows are opaque and edge to edge.
- **A tab strip** is the row at the top of a dock window, holding its **tabs**.
- **Squeezed** means a window shows only its tab strip. Double-clicking a dock window's tab squeezes it, and double-clicking again opens it.
- **The marker** is the hatched strip with a "+" that shows where a held window would go.

## Tests

### T1: Between two squeezed dock windows

This redoes T3 row 3 of the last test doc. Before, the only thing you could do between two squeezed windows was make a tab.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock. Choose Debug → Sample Window 1, which opens on the left. Drag it by its name to the very bottom of History, and let go when the marker shows below History | One column, top to bottom: Layers, History, Sample Window 1 |
| 2 | Double-click Layers' tab, then History's tab | Both are squeezed to their tab strips, one right under the other. Sample Window 1 fills the rest of the column |
| 3 | Choose Debug → Sample Window 2, which opens on the left. Drag it by its name onto the line where Layers' and History's tab strips meet, and hold | The marker opens between the two strips. Both strips keep their height: History's slides down, and Sample Window 1 gets shorter to make the room |
| 4 | Still holding, move up to the middle of Layers' tab strip | The marker goes, the windows slide back, and a "+" tab shows in Layers' strip, with a dashed outline around Layers |
| 5 | Move up to the top few pixels of Layers' tab strip | The marker shows at the very top of the column, above Layers |
| 6 | Move back down onto the line where the two strips met in row 3 | The marker is between them again |
| 7 | Let go | Sample Window 2 sits between Layers and History, and shows its contents. Layers and History stay squeezed |

- [ ] Pass  - [ ] Fail. Notes:

### T2: A stack with no room to spare

This checks the overlay: when no window in the stack can get any shorter, the marker lies on top of them instead of pushing them around.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock | Layers above History, as a dock on the right |
| 2 | Choose Debug → Allow Tiny Window. Drag the app window's bottom edge up slowly until History snaps to its tab strip. Then drag back down just until History opens again, and let go | Layers and History are both as short as they go, with very little of their contents showing |
| 3 | Choose Debug → Sample Window 1, which opens on the left. Drag it by its name onto the border between Layers and History, and hold | Nothing moves. The marker lies across the border, over the bottom of Layers and the top of History's tab strip, with the usual small gap at its left and right ends |
| 4 | Let go | Sample Window 1 lands between Layers and History. All three don't fit, so the lower ones show only their tab strips |
| 5 | Drag the app window's bottom edge back down to where it was | All three open again |

- [ ] Pass  - [ ] Fail. Notes:

### T3: A crowded tab strip

This checks the tab bugs from your screenshots: a dropped tab that didn't scroll into view, a clicked tab left under the fade, and clicks that scrolled to the wrong tab or back to the start.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock. Drag History's tab into the middle of Layers and let go. Then open Sample Windows 1 and 2 from the Debug menu, and drag each into the middle of Layers and let go | Layers' tab strip has four tabs, more than fit. Each time one lands, the strip scrolls so that its tab is fully in view, clear of the fade, and that window shows |
| 2 | Choose Debug → Sample Window 3. Drag it by its name into the middle of Layers and let go | Sample Window 3's tab is the last, fully in view at the end of the strip, and it shows. The strip doesn't jump back to the start |
| 3 | Scroll the tab strip back to its start with the mouse wheel. Click the last tab you can see on the right, even if it's partly under the fade | The strip scrolls just far enough that the whole tab is in view, clear of the fade. The tabs move left by only that much |
| 4 | Click a tab that's fully in view, away from both fades | Its window shows, and the strip doesn't move |
| 5 | Click the tab at the right end again, partly under the fade, and keep doing that until you reach the last tab | Each time, the tab you clicked is the one that scrolls into view, never the one before it, and the strip never jumps back to the start |
| 6 | Click the tab at the left end, partly under the fade there | The strip scrolls back just far enough that the whole tab is in view |

- [ ] Pass  - [ ] Fail. Notes:

### T4: The marker on a widget side's edge strip

This checks your last note: on the edge strip of a side with widgets, the marker had much more space on the app window's side than on the widgets' side.

**Also on macOS:** Yes, because Retina screens draw small gaps at a different scale

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag Layers by its name into the middle of the canvas, and hold. Then move onto the blue edge strip at the far right | History moves in. The marker stands between History and the app window's edge, with the same gap on all four sides: from the app window's edge, from History, and at its top and bottom |
| 2 | Still holding, move onto the blue edge strip at the far left | The marker there is the same width as on the right |
| 3 | Move back over the upper half of History and let go | Layers is back above History |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Squeeze three windows in a row in one dock column, then hold a window on each border between them.
- In a crowded tab strip, drag a tab out and back in at different places, and click tabs at both ends.
- Repeat T2 with the app window made short enough that one window is squeezed, then hold a window on the border above it.

## Known limitations (not bugs)

- About your T2 note: with Debug → Allow Tiny Window, an app window narrower than its sides' windows at their narrowest lets the sides overlap, with the left drawn on top. Nothing else can give way by then: the canvas is already gone, and the windows are at their narrowest. Without Allow Tiny Window the app window can't get that narrow, so I've left it.
- Drop zones stay where they were when you picked the window up, so after the windows slide apart, the pointer may be over a different part of a window than the marker suggests.
- Your screenshots of the tab bugs and of the retest are saved in `.local/bugs/`, which git ignores.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2.5g follow-ups, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
