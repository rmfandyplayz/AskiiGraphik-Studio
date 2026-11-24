<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.1: Follow-ups 2, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 15 min

## What changed

These are the fixes and requests from your testing of `step-4.1-follow-ups.md`. Everything else in it passed.

- **Typing over a selection:** typing that replaces highlighted text, and the typing straight after it, are now one undo step. One Undo brings back what was highlighted, highlighted again. You guessed right: the replacement was a step of its own, which made sense in the code but not in use.
- **The highlight after Undo:** text that comes back is highlighted again (T2 row 7). Placing the caret after the text came back was clearing the highlight.
- **The canvas Text tool** now starts a new undo step after a 1-second pause, like the text boxes. It was 2 seconds.
- **The main window** now opens where you left it: same size, same position, on the same monitor, and maximized if it was. If that monitor isn't plugged in any more, it opens in the middle of the main screen.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, keep the defaults and click **OK**. Choose **Don't Save** if it asks.

## Known limitations (not bugs)

- Full screen isn't remembered: a window closed full screen comes back as it was before.

## Tests

### T1: Typing over a selection is one step with what follows

This is your reproduction from the last round. Before, Undo left `x` behind.

**Also on macOS:** Yes, because the keys go through the menu bar there

| # | Do this | Expected result |
|---|---|---|
| 1 | Open **Window → Ramps**, click **New**, and click in the **Characters, lightest first** box. Type `asdf3897c01[]{}` | The box shows it |
| 2 | Wait two seconds, press **Ctrl+A** (Cmd+A on a Mac), and type `xyz` quickly | The box shows `xyz` |
| 3 | Press **Ctrl+Z** (Cmd+Z) | `asdf3897c01[]{}` comes back, all highlighted |
| 4 | Press **Ctrl+Y** (Cmd+Shift+Z) | The box shows `xyz` again |

### T2: What comes back is highlighted

This redoes T2 rows 4 to 7 of the last test doc, where row 7 failed.

**Also on macOS:** Yes, because it failed there too

| # | Do this | Expected result |
|---|---|---|
| 1 | Empty the box (**Ctrl+A**, then **Backspace**). Type `abc` quickly | The box shows `abc` |
| 2 | Wait two seconds. Highlight `bc` by dragging over it, and type `z` | The box shows `az` |
| 3 | Press **Ctrl+Z** | The box shows `abc`, with `bc` highlighted |

### T3: The Text tool's pause

Checks that typing on the canvas starts a new undo step after a 1-second pause. It used to be 2 seconds.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Open **Window → History**. Press **T**, click the canvas, type `abc` quickly, wait about a second and a half, then type `def` quickly | History has two new typing steps |
| 2 | Press **Esc**, then **Ctrl+Z** | `def` goes; `abc` stays |

### T4: The window's size and place

Checks that the main window opens where it was last closed.

**Also on macOS:** Yes, because each system places windows its own way

| # | Do this | Expected result |
|---|---|---|
| 1 | Make the app window smaller than it is now, and drag it somewhere else on the screen. Close it (choose **Don't Save** if it asks), and run it again | It opens at the same size and in the same place |
| 2 | If you have a second monitor: drag the window onto it, resize it, close it and run it again | It opens on the second monitor, at the size you left it |
| 3 | Maximize the window (on a Mac, double-click its title bar or Option-click the green button, rather than going full screen). Close it and run it again | It opens maximized, on the monitor it was on |
| 4 | Un-maximize it | It goes back to the size and place it had before you maximized it in row 3 |
| 5 | If you have a second monitor: put the window on it, close the app, unplug or turn off that monitor (or set the system to show only one), and run the app | It opens in the middle of the main screen, no bigger than the screen |

### T5: Tests to revisit

| Step | Test | Where | Why |
|---|---|---|---|
| 4.1 | `step-4.1-selection-mask.md` P3, row 2 | Windows | changed after testing: panning and zooming a big canvas while its outline runs should still be smooth. Optional, as P tests are |
