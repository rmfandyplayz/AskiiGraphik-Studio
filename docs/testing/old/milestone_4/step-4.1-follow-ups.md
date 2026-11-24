<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.1: Follow-ups, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 20 min

## What changed

These are the fixes from your testing of `step-4.1-selection-mask.md`. Everything else in it passed, so this covers only the changed parts.

- **Undo in a text box** takes away a burst of typing at a time, not one character. A pause of about a second starts a new step, and so does moving the caret, switching between typing and deleting, pasting, or typing over selected text. There's no longer a limit of nine.
- **Text boxes on a Mac:** Cmd+A, Cmd+Z and Cmd+Shift+Z in a text box now act on the box, as they already did on Windows. Choosing **Edit → Undo** or **Select → Select All** from the menu while typing in a text box does the same, on both systems.
- **The selection outline moves:** its dashes run clockwise round the selection, so it's easy to spot on a big canvas.
- **Windows are remembered:** Ramps, Characters, Color and Swatches now come back after a restart, where you left them. Before, only Layers and History did.
- **Tools menu:** it now lists the **Character Brush**, after BG Brush.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, keep the defaults and click **OK**. Choose **Don't Save** if it asks.
3. Open **Window → History**, so you can see each undo step.

## Known limitations (not bugs)

- The canvas's own Text tool still waits 2 seconds before starting a new undo step; only text boxes use about a second.
- While something is selected, **Debug → Show Frame Rate** shows a frame rate instead of "Canvas idle", since the outline is moving. It goes back to idle when nothing is selected.

## Tests

### T1: Undo in a text box, a burst at a time

Checks that Undo in a text box takes away whole bursts of typing, and remembers more than nine of them. Before, it took one character at a time and stopped after nine.

**Also on macOS:** Yes, because the keys go through the menu bar there

| # | Do this | Expected result |
|---|---|---|
| 1 | Open **Window → Ramps**, click **New**, and click in the **Characters, lightest first** box. Type a few characters if it's empty, wait two seconds, then press **Ctrl+A** (Cmd+A on a Mac) and **Backspace** | The box is empty |
| 2 | Type `abc` quickly, wait two seconds, then type `def` quickly | The box shows `abcdef` |
| 3 | Press **Ctrl+Z** (Cmd+Z) | The box shows `abc`. History on the right doesn't move |
| 4 | Press **Ctrl+Z** again | The box is empty |
| 5 | Press **Ctrl+Z** again | The characters you deleted in row 1 come back, highlighted |
| 6 | Press **Ctrl+Y** (Cmd+Shift+Z) three times | The box is empty, then shows `abc`, then `abcdef` |
| 7 | Click at the end of the box. Type one character, wait a second or so, and repeat until you've typed 12 | 12 new characters |
| 8 | Press **Ctrl+Z** 12 times | Each press takes away one of them, the last one first, until all 12 are gone |

### T2: What starts a new step

Checks the other things that end a burst, besides a pause.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Empty the box as in T1 row 1. Type `world` quickly, then press **Backspace** twice quickly | The box shows `wor` |
| 2 | Press **Ctrl+Z** | `ld` comes back, highlighted: the deleting was its own step |
| 3 | Press **Ctrl+Z** | The box is empty again |
| 4 | Type `abc` quickly, click between `a` and `b`, and quickly type `x` | The box shows `axbc` |
| 5 | Press **Ctrl+Z** | The box shows `abc`: moving the caret started a new step |
| 6 | Highlight `bc` by dragging over it, and type `z` | The box shows `az` |
| 7 | Press **Ctrl+Z** | The box shows `abc` again, with `bc` highlighted |

### T3: Shortcuts and menus in a text box

This replaces T8 of the first test doc, which failed on the Mac: Cmd+Z undid the whole document history before the box, and Cmd+A selected the canvas.

**Also on macOS:** Yes, because there the Select and Edit menus' shortcuts go through the system menu bar

| # | Do this | Expected result |
|---|---|---|
| 1 | Close the Ramps window if it's open. Press **T**, click the canvas, type a few letters and press **Esc**. Then press **M** and drag out a rectangle | History has a typing step and a **Rectangle Select** |
| 2 | Press **Ctrl+D** (Cmd+D) to deselect. Open **Window → Ramps**, click **New**, click in the **Characters, lightest first** box and type a few characters | The typing goes into that box |
| 3 | Press **Ctrl+A** (Cmd+A) | All the characters in the box are highlighted. The canvas shows no outline, and History adds nothing |
| 4 | Type `xyz`, then press **Ctrl+Z** (Cmd+Z) | `xyz` goes from the box. History doesn't move, and the canvas still has its letters |
| 5 | Press **Ctrl+Y** (Cmd+Shift+Z) | `xyz` comes back. History doesn't move |
| 6 | With the box still showing the caret, choose **Edit → Undo** from the menu | `xyz` goes again. History doesn't move |
| 7 | Choose **Select → Select All** from the menu | The box's characters are highlighted, and the canvas shows no outline |
| 8 | Click the canvas, then press **Ctrl+A** | The whole canvas is selected, and History adds **Select All** |
| 9 | Press **Ctrl+Z** | The selection goes, and History steps back: Undo works on the document again |

### T4: The running outline

Checks that the selection outline's dashes move, in the right directions, and stop when nothing's selected.

**Also on macOS:** Yes, because a Retina display draws it at a different pixel size

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **View → Zoom In** two or three times, press **M** and drag out a rectangle a few cells across | The dashes move steadily round the rectangle, like a line of ants |
| 2 | Watch each edge | They run right along the top, down the right side, left along the bottom and up the left side: clockwise all the way round |
| 3 | Scroll and zoom while they run | The outline stays on the same cells, thin and sharp, and the dashes keep running |
| 4 | Choose **Debug → Open Sample Document** (500 × 500), press **Ctrl+A**, then zoom out as far as it goes | The moving outline round the whole canvas is easy to spot |
| 5 | Turn on **Debug → Show Frame Rate**, and watch the status bar | It shows a frame rate while the outline runs |
| 6 | Press **Ctrl+D** | The outline goes, and within a couple of seconds the status bar says "Canvas idle" |

### T5: Windows remembered after a restart

Checks that every window comes back where you left it. Before, only Layers and History did.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Window → Reset Layout**. Then open **Window → Ramps**, **Window → Characters**, **Window → Color** and **Window → Swatches** | All four open |
| 2 | Drag Ramps to the left side, and drag Swatches onto Color's tab strip so they share a window | They move there |
| 3 | Close the app with its own close button. Choose **Don't Save** if it asks. Run it again | Ramps, Characters, Color and Swatches are all back, where you left them, with Swatches still beside Color |
| 4 | Close **Characters** with its close button, then close and run the app again | Characters stays closed. The others are back |
| 5 | Choose **Window → Reset Layout**, then close and run the app again | Only the windows that open on a fresh start show: Layers and History |
| 6 | Choose **Debug → Sample Window 1**, then close and run the app again | The sample window doesn't come back. That's on purpose: sample windows are only for trying out stacks |

### T6: The Character Brush in the Tools menu

Checks that the Tools menu lists every tool.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Tools** menu | **Character Brush** is listed after **BG Brush** |
| 2 | Choose **Character Brush** | It has a check mark in the Tools menu, and Tool Options shows its character box |

### T7: Tests to revisit

The outline now repaints while it runs, so this optional test is worth one more look.

| Step | Test | Where | Why |
|---|---|---|---|
| 4.1 | P3, row 2 | Windows | changed after testing: panning and zooming a big canvas while its outline runs should still be smooth |

## Worth poking at (optional)

### P1: A long burst

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | In a text box, type a sentence without stopping for a second, including spaces | One **Ctrl+Z** takes the whole sentence away |
| 2 | Type a word, press **Backspace** to fix a letter, and carry on typing | **Ctrl+Z** takes away the typing after the fix first, then the fix, then the typing before it |
