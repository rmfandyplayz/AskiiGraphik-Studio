<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.5d: Hidden toolbars during a drag, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 10 min

## What changed

The changes you asked for after testing 2.5d:

- Starting a drag hides every other toolbar at once. Only free strips and squares show, always at full length.
- After a drop or a cancel, the strips and squares fade away and the toolbars fade back in.
- A strip or square under the pushed-back copy of a toolbar now dims instead of disappearing.

Two problems you reported are also fixed. File no longer gets pushed down, and strips no longer overlap in a very narrow window.

## Setup

1. Pull, then from the repo root run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider). It has to be a Debug build for the Debug menu, which both of those are.
2. Choose Window → Reset Layout before each test. That puts File in the top left, Edit in the top right, Tools in the bottom middle and View in the bottom right.

To drag a toolbar, press on the small dots at either end of it. They appear when the pointer is over the toolbar.

## Tests

### T1: Other toolbars disappear, and strips are full length

This checks what shows while you hold a toolbar.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag Edit to the middle of the canvas and keep holding | File, Tools and View disappear the moment the drag starts, with no fade. Edit stays solid and follows the pointer |
| 2 | Look at the blue shapes, still holding | There are 5: the top strip, the top right square, the left strip, the right strip and the bottom left square. There's nothing in the top left, bottom middle or bottom right, because File, Tools and View are on those |
| 3 | Look at the top strip, still holding | It stops short of the top left corner by the same distance as it stops short of the top right corner, although there's no square showing in the top left |
| 4 | Press Esc and let go. Then drag File to the middle of the canvas and keep holding | Now the top left square shows, and the top right one doesn't. The top strip is in exactly the same place, at the same length |
| 5 | Press Esc and let go | Everything goes back to how it was |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Everything fades back when the drag ends

This checks the short animation at the end of a drag. It's quick, so you may want to repeat each row a few times.

**Also on macOS:** Yes, because the fades are animated

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag Tools onto the left strip and let go | Tools slides into place on the left edge. In the same moment the blue shapes fade away and File, Edit and View fade in. None of those three slide. They just appear where they were |
| 2 | Drag Tools to the middle of the canvas, then press Esc | The same fades, and Tools slides back to the left edge |
| 3 | Compare how long the fade and the slide take | They take the same time. It's 0.12 seconds now, not the 0.2 you guessed, and it's one setting if you'd like them slower |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Floating toolbars and sub-toolbars disappear too

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag View to the middle of the canvas and let go | View floats there |
| 2 | Drag File anywhere and keep holding | The floating View disappears too. Let go in the middle of the canvas, and View fades back |
| 3 | Window → Reset Layout. Then Debug → Sub-Toolbar on Tools. Three sample buttons appear above Tools | The sample sits just above Tools |
| 4 | Drag File to the middle of the canvas and keep holding | Tools and its sample both disappear. Press Esc and let go, and both fade back |
| 5 | Drag Tools to the middle of the canvas and keep holding | The sample stays with Tools, because it belongs to the toolbar you're holding. Press Esc and let go |
| 6 | Debug → Sub-Toolbar on Tools again | The sample goes away |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Zones dim under the pushed-back copy

This checks your change to a toolbar held past the canvas area, over the widgets.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag Tools over the Layers widget, about halfway down the window, and keep holding | A see-through copy of Tools in a running dashed outline shows just left of the widgets, on top of the right strip |
| 2 | Look at the right strip, still holding | It's clearly dimmer than the other blue shapes, but you can see it without staring |
| 3 | Move the pointer back to the middle of the canvas, still holding | The copy goes, and the right strip brightens to match the others |
| 4 | Press Esc and let go | Everything goes back |

- [ ] Pass  - [ ] Fail. Notes:

### T5: File stays where it is

This is your bug report with the second screenshot: File, vertical with a sample beside it, moved down when you dragged Edit.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Right-click File's dots and choose Vertical. Then Debug → Sub-Toolbar on File | File stands in the top left, with its top level with the top of Layers, and the sample is to its right |
| 2 | Drag Edit to the middle of the canvas and keep holding | File and the sample disappear, along with the other toolbars |
| 3 | Move over the right strip and let go | File and the sample fade back in the same place. File's top is still level with the top of Layers |
| 4 | Debug → Sub-Toolbar on File | The sample goes away |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Strips don't overlap in a very narrow window

This is the problem you found with your first screenshot. It can't happen at normal window sizes.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Debug → Allow Tiny Window, then drag the window's right edge left until the window is as narrow as your first screenshot | The window gets that narrow |
| 2 | Drag any toolbar to the middle of the canvas and keep holding | The left and right strips are thinner, with a space between them, and nothing blue overlaps anything else blue. The corner squares are smaller too |
| 3 | Press Esc and let go. Then Debug → Allow Tiny Window, and make the window normal size again | Everything goes back |

- [ ] Pass  - [ ] Fail. Notes:

### T7: Tests to revisit

Skip any you've already done.

| Step | Test | Where | Why |
|---|---|---|---|
| 2.5d | T1, T2 and T5 of `step-2.5d-sub-toolbars.md` | macOS | They were marked for a Mac, and your results didn't say which platform they ran on |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Start a second drag straight after a drop, before the fades have finished.
- Drag a toolbar and drop it in a corner that looks empty only because a toolbar is hidden there. It floats there, on top of the hidden toolbar, which then fades back underneath it.

## Known limitations (not bugs)

- Windows still can't be dragged or resized. That's the next step, 2.5e.
- All of this look is still placeholder, for you to replace.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2.5d (hidden toolbars), test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
