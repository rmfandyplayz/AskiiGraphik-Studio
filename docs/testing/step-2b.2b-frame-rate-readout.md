<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2b.2b: Frame-rate readout, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 5 min

## What changed

The Debug menu has a new item, **Show Frame Rate**, which shows how smoothly the canvas is drawing at the right end of the status bar. It gives three numbers from the last 2 seconds:
- the average frames per second;
- the **1% low**, the rate of the slowest 1% of frames, which shows stutter that an average hides;
- the **slowest draw**, the most time the canvas itself spent drawing one frame.

The canvas only redraws when something changes, so the frame rate follows how fast things change, up to your screen's refresh rate. When nothing has changed for 2 seconds, it says **Canvas idle**.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider). It has to be a Debug build, which both of those are, for the Debug menu to be there.

## Tests

### T1: Turning the readout on and off

This checks that the menu item shows and hides the readout in the right place.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Debug** menu | **Show Frame Rate** is there, without a check mark. |
| 2 | Click **Show Frame Rate**, then keep the mouse still for 2 seconds | At the far right of the status bar, after a gap, it says **Canvas idle**. The rest of the status bar hasn't moved. The menu item now has a check mark. |
| 3 | Click **Debug → Show Frame Rate** again | The readout disappears, and the check mark goes. |
| 4 | Turn it on again, close the app, and start it again | The readout is off: it's never remembered between runs. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Numbers while the canvas moves

This checks that the readout measures real drawing, and gives you a way to watch for stutter from now on.

**Also on macOS:** Yes, because the Mac draws through a different graphics system (Metal), and its screen may run at 120 Hz

| # | Do this | Expected result |
|---|---|---|
| 1 | **Debug → Open Color Sample** (choose **Don't Save** if it asks), zoom to 200% with **Ctrl+=** (Cmd+= on a Mac), and turn on **Debug → Show Frame Rate** | **Canvas idle** at the right of the status bar. |
| 2 | Hold the middle mouse button on the canvas and drag around in steady circles for a few seconds | The readout changes to something like `144 fps · 1% low 120 · slowest draw 1.2 ms`. The first number is close to your screen's refresh rate (for example 60, 120, 144 or 165), or your mouse's rate if that's lower. The slowest draw is a few milliseconds at most. |
| 3 | Stop dragging and keep the mouse still | The numbers stay for up to 2 seconds, then it says **Canvas idle** again. |
| 4 | Click in the canvas and hold down a letter key for a few seconds | It shows roughly your keyboard's repeat rate, often about 30 fps, because the canvas only redraws when a new letter appears. That's expected, not a slowdown. |
| 5 | Zoom in and out one step at a time with **Ctrl+=** and **Ctrl+-** (Cmd on a Mac) | The slowest draw can jump up for a moment the first time you reach a zoom level, because the letters are drawn at the new size then. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Pan around the colour sample at 25% zoom, where the most cells are on screen, and compare the slowest draw with 800%.
- Drag a toolbar or a window over the canvas while the readout is on. Those repaint the canvas under them, so they count as frames too.
- If the numbers ever look bad, note what you were doing and the readout at the time; that's worth a bug report.

## Known limitations (not bugs)

- Short pauses while you move (under a tenth of a second) count as slow frames, so jerky movement lowers the 1% low. Steady movement gives the truest numbers.
- The slowest draw is the time on the processor. The graphics card's own work comes after and isn't included.
- The readout's look is a placeholder, and it's only in Debug builds.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2b.2b, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
