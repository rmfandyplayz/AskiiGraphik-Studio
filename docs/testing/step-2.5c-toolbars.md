<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.5c: Toolbars, manual test

**Build:** the commit that marks this step `[~]`
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 25 min

## What changed

Toolbars can now be moved. Each has a 6-dot grip at both ends. Dragging one shows the places it can go, and the other toolbars show live what a drop would do. Dropping anywhere else floats it. Right-clicking a toolbar lets you turn it or hide it, and a toolbar that doesn't fit now fades on the sides with more to scroll to.

Windows still can't be dragged. That's 2.5e. Your saved layout carries over.

## Setup

1. Pull, then from the repo root run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider). T7 needs a Debug build, which both of those are.
2. Window → Reset Layout, so you start from the default.
3. Resize the canvas to about 200 × 80 and type some art, so there's something to see behind things.

## Tests

### T1: Grips

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Keep the pointer away from a toolbar and look at its ends | No grips. There's a little empty space at each end |
| 2 | Move the pointer over the toolbar | A faint 6-dot grip shows at both ends. The buttons don't shift |
| 3 | Move the pointer away | The grips go |
| 4 | Hover over a grip | The cursor shows it can be moved |
| 5 | Press the middle of a toolbar, on a gap or a button, and drag | Nothing moves. Only the grips start a drag |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Starting a drag

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press the File toolbar's grip and release without moving | Nothing happens |
| 2 | Press it and drag into the middle of the canvas, without letting go | The toolbar follows the pointer, held by its grip. Blue zones show: a square for each free corner and a strip along each edge with a free middle |
| 3 | Look at where the zones are | Bottom left has a square, and the top, left and right edges have strips. Top right and bottom right have none, because Edit and View are there. The bottom edge has none, because Tools is there. File's own corner at top left has one |
| 4 | Move the pointer around | The zones stay exactly where they are |
| 5 | Watch the art while you drag | The canvas doesn't move at all during a drag |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Every anchor

**Also on macOS:** No

Drag the File toolbar to each of these in turn. Between drops, Window → Reset Layout if something's in the way.

| # | Do this | Expected result |
|---|---|---|
| 1 | Hover over the left edge's strip, without letting go | That strip gets stronger, the toolbar turns vertical, and a stronger rectangle shows where it would land |
| 2 | Move back to the middle of the canvas | It turns back to horizontal, and the rectangle goes |
| 3 | Drop on the left strip | It slides into place, centred on the left edge and vertical. Its top and bottom grips are at its ends |
| 4 | Drag it to the right edge's strip and drop | Centred on the right edge, against the widgets, vertical |
| 5 | Drag it to the top edge's strip and drop | Centred along the top, horizontal |
| 6 | Reset Layout, then hide View and drag File to the bottom right square | In the corner, still horizontal: a corner keeps its direction |
| 7 | Reset Layout, then right-click File and choose Vertical, then drag it to the bottom left square | In the corner, still vertical |
| 8 | After any of these, compare the top and bottom of the toolbars with the widgets | No toolbar goes above the top of Layers or below the bottom of History |
| 9 | Reset Layout, hide Tools, and drag File to the bottom edge's strip | Centred along the bottom, because hiding Tools left that anchor free |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Floating

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Reset Layout, then drag File to the middle of the canvas and drop | It stays exactly where you left it |
| 2 | Drag Edit so it covers the right half of File, and drop | They overlap, with Edit on top. Nothing moves out of the way |
| 3 | Drag File a little way by its left grip, still overlapping Edit, and drop | File is now on top |
| 4 | Drag a floating toolbar over the widgets and hold | The toolbar follows the pointer, and a rectangle shows it would land just left of the widgets |
| 5 | Drop it there, then close Layers and History from the Window menu | The toolbar stays where it landed, and doesn't jump under where the pointer was |
| 6 | Drag a floating toolbar up over the menu bar and drop | It sits just below the menu bar, level with the top of the widgets, not above it |
| 7 | Drag a floating toolbar to a corner of the region that's free | Its zone lights up, and dropping puts it back on that anchor |
| 8 | Reset Layout, resize the canvas to 40 × 20, then float Tools, then View | The canvas holds still during both drags. After View's drop it re-centres, because nothing lies along the bottom any more |
| 9 | Right-click a floating toolbar, choose Vertical, then Horizontal | It turns, and its top left stays put |
| 10 | Make the app window big, float a toolbar near its bottom right, shrink the window, then grow it back | It's pushed in to stay inside, and returns to its place |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Cancelling

**Also on macOS:** Yes, because switching apps works differently there

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag File over the left strip, then press Esc while still holding the button | It slides back to where it was, the zones go, and nothing else has changed |
| 2 | Keep holding, then release over the canvas | Nothing happens |
| 3 | Start another drag, then press Alt+Tab (Cmd+Tab on a Mac) | The drag cancels the same way when the app loses focus |
| 4 | Click in the canvas and type | The text goes to the canvas |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Right-click and hiding

**Also on macOS:** Yes, because right-clicking differs there (two fingers, or Ctrl+click)

| # | Do this | Expected result |
|---|---|---|
| 1 | Right-click the File toolbar, on a button | A menu: Horizontal (with a dot), Vertical, a line, Hide. The button doesn't fire |
| 2 | Choose Vertical, then right-click it again | It turned and stayed in its corner, and the dot is on Vertical now |
| 3 | Right-click Tools, on the bottom middle | Horizontal and Vertical are greyed out, because a middle follows its edge. Hide works |
| 4 | Choose Hide on Tools | It goes, and its check mark in the Window menu goes too |
| 5 | Show it from the Window menu | It's back on its anchor |
| 6 | Close the menu by clicking away, then type | The text goes to the canvas |
| 7 | Hide Tools, drag File onto the bottom edge's strip, then show Tools from the Window menu | Tools floats where it would have sat, over File. File doesn't move |

- [ ] Pass  - [ ] Fail. Notes:

### T7: Sharing an edge

**Also on macOS:** No

Debug → Allow Tiny Window first, then make the app window narrow enough for this to happen. It lasts until the window closes.

| # | Do this | Expected result |
|---|---|---|
| 1 | Close Layers and History. Put File top left, View top middle and Edit top right, and make the window wide | Corners in corners, the middle centred, all at full size |
| 2 | Slowly narrow the window | The middle stays centred until a corner toolbar reaches it, then moves aside. They never overlap |
| 3 | Keep narrowing | All three shrink, with the middle a bit longer than the corners, and they scroll with fades |
| 4 | Keep narrowing | Corners hide first, then the middle. Widen again and they come back |
| 5 | Put Edit top right, horizontal, and File on the right edge's middle. Make the window short | File starts below Edit. Corner toolbars win |
| 6 | Right-click Edit and choose Vertical | Edit and File share the right edge, one above the other, and never overlap |
| 7 | Put View along the top and narrow the window | View stops before Edit's column rather than running under it |

- [ ] Pass  - [ ] Fail. Notes:

### T8: Too little room

**Also on macOS:** Yes, because trackpad scrolling differs there

Debug → Allow Tiny Window, then shrink the window until a toolbar doesn't fit.

| # | Do this | Expected result |
|---|---|---|
| 1 | Look at a horizontal toolbar that doesn't fit | Its buttons fade out towards the right, where there's more. The left has no fade. The background and grips stay put |
| 2 | Scroll it with the wheel, part of the way | Fades on both sides |
| 3 | Scroll to the end | Only the left fades |
| 4 | Do the same with a vertical toolbar | The same, top and bottom |
| 5 | Grow the window until it fits | No fades |
| 6 | Click a faded button | It still works |

- [ ] Pass  - [ ] Fail. Notes:

### T9: The layout is saved

**Also on macOS:** Yes, because closing a window works differently there

| # | Do this | Expected result |
|---|---|---|
| 1 | Float File, put Edit on the left edge, and turn View vertical. Quit and start the app | Everything's where you left it |
| 2 | On a Mac only: close the window (the app keeps running), then click the Dock icon | The new window has the same layout, and its toolbars can still be dragged |
| 3 | Window → Reset Layout | Everything's back in the default layout |

- [ ] Pass  - [ ] Fail. Notes:

### T10: Clicks and typing around toolbars

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Text tool, click art right beside a floating toolbar and type | The caret lands there and the text appears |
| 2 | Click a button on a floating toolbar with art under it | The button works, and nothing happens on the canvas |
| 3 | Drag a toolbar by its grip, drop it, then type | The text keeps going to the canvas |
| 4 | Move the pointer across a vertical toolbar while watching the status bar | The cell doesn't update while the pointer is on the toolbar, and does again beside it |

- [ ] Pass  - [ ] Fail. Notes:

## How it feels

Not pass or fail, but this is the part only you can judge, so tell me about anything here:

- The size of the zones, and whether you can float a toolbar near an edge without hitting one.
- A toolbar turning as soon as it reaches a middle's zone: helpful, or too jumpy?
- The slide speed (120 ms).
- How faint the grips are.

## Worth poking at (optional, unscripted)

- **Dragging fast,** in and out of zones and across the widgets.
- **Dragging out of the app window** and releasing outside it.
- **A hand-edited layout:** in `settings.json`, set a toolbar's `floatingAt` to something silly, such as `{ "x": -9000, "y": 9000 }`. It should be pushed inside.

## Known limitations (not bugs)

- **Windows can't be dragged or resized yet,** and have no hover buttons. That's 2.5e.
- **No sub-toolbars yet.** That's 2.5d.
- **Toolbars still have text buttons,** so they're long and vertical ones are wide. Your icons will fix that.
- **The canvas jumps rather than slides** when a drop changes the space it has.
