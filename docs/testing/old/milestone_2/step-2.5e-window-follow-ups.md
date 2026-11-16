<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.5e: Window follow-ups, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 15 min

## What changed

The changes you asked for after testing 2.5e:

- Esc closes a window's ≡ menu straight away, wherever the pointer is.
- The gaps and the inner edge show a faint blue line under the pointer, and a solid one while held.
- Double-clicking a window's name squeezes it to title-only, or opens it again to the height it had.
- A held window follows the pointer anywhere, even outside the app window. Where it can't land, a dashed outline with a very light hatch shows where it will go back to, as a toolbar does. This also fixes the window that turned into a widget under History.
- A window that's alone on its side comes off at the size it last had floating, or else at half its height.
- A window dropped on an empty side keeps its width.
- Floating windows stay 8 pixels inside the app window, and can't get wider than a side's windows can.
- Each window's menu offers the kinds it isn't.
- No window shows its ≡ and ✕ while any window is being dragged or resized.

You asked whether there was something better than halving the height. I did both: the window now follows the pointer past the app window's edge and snaps back inside on the drop, and a lone window also comes off at its remembered size or half its height.

## Setup

1. Pull, then from the repo root run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Choose Window → Reset Layout before each test. That leaves Layers above History on the right.
3. Resize the canvas to about 200 × 80 and type some art, so there's something to see behind things.

Words used below:

- **A window's name** is the text at the top left of its title row. Drag or double-click the name, not the buttons.
- **The gap** is the thin space between two windows in a stack.
- **The inner edge** is the thin space just beside a side's windows, on the canvas side.

## Tests

### T1: Esc closes the ≡ menu

This is your bug from 2.5e's T1, row 4.

**Also on macOS:** Yes, because keyboard focus works differently there

| # | Do this | Expected result |
|---|---|---|
| 1 | Move the pointer over Layers and click its ≡. Then move the pointer back over the canvas, away from the menu | The menu stays open |
| 2 | Press Esc | The menu closes at once |
| 3 | Type a letter | It appears on the canvas, so typing went back to the canvas |
| 4 | Click Layers' ≡ again, then press the Down arrow until Float is highlighted, and press Enter | Layers floats. Choose Window → Reset Layout afterwards |
| 5 | Right-click History's name, then press Esc | The menu closes |

- [ ] Pass  - [ ] Fail. Notes:

### T2: A line on the gap and the inner edge

This checks the feedback you sketched in your two screenshots.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Move the pointer onto the gap between Layers and History, without pressing | A faint blue line runs along the middle of the gap, and the pointer is drawn on top of it |
| 2 | Press and hold | The line turns solid blue |
| 3 | Drag up and down, still holding | The line stays solid and moves with the gap |
| 4 | Let go, then move the pointer onto the canvas | The line goes |
| 5 | Do rows 1 to 4 on the inner edge, beside Layers | The same, with an upright line |
| 6 | Drag History to the middle of the canvas and let go. Move the pointer onto its edges and corners | The pointer changes to a resize arrow, but no line shows. That's my call, since a floating window has no gap to fill |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Double-clicking a window's name

This checks your idea from 2.5e's T3. A squeezed window with no height to go back to opens to its minimum, but that's hard to set up by hand, so automated tests cover it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Double-click History's name | History squeezes to its title row, and Layers grows to fill the room |
| 2 | Double-click History's name again | History opens to exactly the height it had in row 1, and Layers shrinks back |
| 3 | Drag the gap down until History snaps to its title row. Then double-click History's name | History opens to the height it had when you started dragging the gap |
| 4 | Double-click History's name to squeeze it again. Then double-click Layers' name | Nothing happens, because one window in a stack always shows its contents |
| 5 | Window → Reset Layout. Drag History to the middle of the canvas and let go. Double-click its name, then double-click it again | It squeezes to its title row, then opens to the same height |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Dragging outside the app window

This is the change for your screenshot where the window didn't follow the pointer, and your T6 row 3, where a floating window turned into a widget under History.

**Also on macOS:** Yes, because a drag holds on to the pointer outside the window

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag History to the middle of the canvas and let go | It floats there at the same size |
| 2 | Drag History by its name past the app window's bottom right corner, out onto the desktop, and keep holding | History keeps following the pointer, with its name under it, even where it goes outside the app window. Inside the app window's corner, a dashed outline with running dashes and a very light hatch, with no "+", shows where it would land. It sits 8 pixels in from the edges |
| 3 | Let go | History moves into the outline |
| 4 | Drag History past the app window's right edge, level with Layers, and keep holding | No hatched "+" opens in the stack with Layers. The outline shows History landing inside the app window instead |
| 5 | Let go | History floats inside the app window. Layers is still on its own on the right |
| 6 | Drag History around in the middle of the canvas, keeping it well inside | No outline shows, because it would land where you're holding it |

- [ ] Pass  - [ ] Fail. Notes:

### T5: A lone window comes off smaller

This checks your rule for dragging the only window off a side.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Close History with its ✕. Layers is now alone on the right, the full height | |
| 2 | Drag Layers by its name toward the middle of the canvas and keep holding | Straight away Layers is half its height and the same width, with its name under the pointer |
| 3 | Let go in the middle | It floats at that size |
| 4 | Resize Layers by its bottom right corner until it's clearly smaller. Then drag it onto the right edge's blue strip and let go | Layers becomes a widget on the right again, the full height |
| 5 | Drag Layers off the right side again and keep holding | It's held at the size you gave it in row 4, not at half height |
| 6 | Press Esc to cancel, then click Layers' ≡ and choose Float | It floats at that same size, a little in from the right edge and down |

- [ ] Pass  - [ ] Fail. Notes:

### T6: An empty side keeps the window's width

This checks your change to dropping a window on an empty side.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag History to the middle of the canvas and let go. Drag its right edge until it's clearly wider than Layers | |
| 2 | Drag History by its name onto the left edge's blue strip and let go | History becomes a widget on the left, exactly as wide as it was floating |
| 3 | Window → Reset Layout. Close History with its ✕, then float Layers from its ≡ menu. Make it clearly narrower | Both sides are empty now |
| 4 | Click Layers' ≡ and choose Turn into Widget | Layers becomes a widget on one side, exactly as wide as it was floating |

- [ ] Pass  - [ ] Fail. Notes:

### T7: The menus offer the other kinds

This checks your change to the ≡ menu.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ | Float, Turn into Dock (greyed out until docks arrive in 2.5f) and Close. No Turn into Widget |
| 2 | Choose Float, then click Layers' ≡ again | Turn into Widget, Turn into Dock (greyed out) and Close. No Float |
| 3 | Press Esc, then right-click Layers' name | The same menu as row 2 |
| 4 | Choose Turn into Widget | Layers goes back to the right side |

- [ ] Pass  - [ ] Fail. Notes:

### T8: No buttons during a drag or a resize

This is the flicker you saw while dragging a floating window.

**Also on macOS:** Yes, because it depends on hover

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag History to the middle of the canvas and let go. Drag it slowly back and forth across Layers and around the canvas, and keep holding | No ≡ or ✕ shows on any window at any point, and nothing flickers |
| 2 | Let go in the middle | History's ≡ and ✕ show faintly again, since the pointer is over it |
| 3 | Resize History slowly by a corner, moving across Layers | No buttons show during the resize. They come back after you let go |
| 4 | Drag the inner edge beside Layers slowly | No buttons show on Layers during the drag |

- [ ] Pass  - [ ] Fail. Notes:

### T9: Floating windows keep clear of the app window's edges

This is your T6 row 6: resizing a floating window could clash with resizing the app window.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag History to the middle of the canvas and let go. Drag its right edge far past the app window's right edge | It stops 8 pixels short of the app window's edge, the same gap the widgets have |
| 2 | Move the pointer onto the app window's own right edge and drag it | The app window resizes, not History |
| 3 | Drag History's left edge far past the app window's left edge | History stops growing 320 pixels from the left edge. It's then as wide as a side's windows can be, which leaves the canvas its minimum width |
| 4 | Drag History's top and bottom edges past the app window's | They stop 8 pixels inside, below the menu bar and above the status bar |
| 5 | Drag History by its name down past the app window's bottom edge, below the middle of the canvas, and let go | It lands 8 pixels above the status bar, not touching it |

- [ ] Pass  - [ ] Fail. Notes:

### T10: Tests to revisit

Skip any you've already done.

| Step | Test | Where | Why |
|---|---|---|---|
| 2.5e | T1 row 4 and T6 rows 3 and 6 of `step-2.5e-widgets-and-floating-windows.md` | Windows and macOS | Covered by T1, T4 and T9 above, so there's nothing extra to do |

Nothing else from earlier steps is waiting for another platform.

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Drag a window out past an empty side's edge, then back into the app window, a few times before letting go.
- Drag a lone widget off its side and drop it straight back on its own place.
- Double-click the names of two different windows in a stack of three or more, then open them again in the other order.

## Known limitations (not bugs)

- Docks arrive in 2.5f. Until then Turn into Dock is greyed out.
- A window held outside the app window draws over the menu bar and status bar, as a held toolbar already does.
- Double-clicking a dock's tab comes with docks in 2.5f.
- The lines, outline and hatch are placeholders, for you to replace.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2.5e (window follow-ups), test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
