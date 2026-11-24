<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 4.9b: Moving past the zone, scrolling round it, and arrows in the selection tools, manual test

**Build:** the commit that adds this file
**Test on:** Windows (Linux is covered by CI only)
**Time needed:** about 25 min

## What changed

A move can now take the selection anywhere, as long as its box still touches the zone round the canvas, even at a corner. So a selection, or a whole layer, can go right off the canvas. The view can scroll to see a selection out there, keeps still while you're moving things, then glides back once the extra space isn't needed. A paste that lands partly out of view brings the view to it. In every selection tool, the arrow keys now move the selected cells, as the Move tool's do. There's also a hidden "reduce motion" setting that stills the app's animations.

These tests are about how things look and feel, so each one says what to try rather than which cells to use. Judge by eye.

## Setup

1. Pull, then run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**, leave the size as it is, and click **OK**. Choose **Don't Save** if it asks.
3. With the Text tool, type a few lines of letters in the top-left part of the canvas, then press **Esc** twice.
4. Open **Window → History**, so you can see the undo steps.

## Known limitations (not bugs)

- **Cells past the canvas are lost** when a move ends, as in 4.9: only the outline stays out there.
- **Reduce motion has no menu or checkbox yet.** It's set by hand in the settings file (T7) and read when the app starts.
- **Placeholders:** the glide's length (0.15 s), how much room there is past a selection, and the other choices listed in `docs/TEMPORARY-UI.md` under "Moving past the zone, scrolling round it, and arrows in the selection tools".

## Tests

### T1: Moving everything off the canvas

Checks the fixed 4.9 T2: the zone is now a ring the selection has to keep touching, not a wall.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Select some letters, pick the Move tool (**V**), and drag them off the canvas to the left, as far as they'll go | They go right off the canvas. They stop once the selection's right side reaches the zone's outer edge, about 10 columns past the canvas edge |
| 2 | Keep dragging further left and up and down | The selection stays at that limit and slides up and down along it |
| 3 | Try the same off the right edge, the top and the bottom, and off a corner | Each time it can go fully off, and off a corner it stops only when just its corner still touches the zone's corner |
| 4 | With nothing selected (**Ctrl+D**), drag the whole layer off the canvas with the Move tool, then press **Ctrl+Z** | The whole layer goes off the canvas, and comes back with the undo |

### T2: Scrolling to a selection out past the zone

Checks that you can always see where the selection went, with some room round it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Zoom in until the canvas is bigger than the window. Select some letters and move them off one side as far as they go (**Shift+arrow** a few times is quickest) | The view doesn't chase them while you move them |
| 2 | Scroll towards them | You can scroll until you see the whole selection, with about 10 cells of room past it, and a little empty space past that |
| 3 | Zoom out until everything fits in the window | The canvas stays centred, unless the selection would be cut off, in which case the canvas sits off-centre just enough to show it |

### T3: The view keeps still, then tidies up

Checks that the view never moves under you while something is moving, and glides back afterwards.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Carry on from T2: scroll so you're looking at the selection past the zone, then drag it back onto the canvas, holding the button for a while before you let go | Nothing in the view moves while the button is down |
| 2 | Let go | After you let go, the view glides smoothly (and quickly) back, leaving no empty space it no longer needs |
| 3 | Move the selection out again with the arrow keys, pressing several in a row, then scroll to it and press **Ctrl+Z** a few times | While you're pressing arrows the view stays put, and it only moves a moment after you stop. Each undo that brings the selection back glides the view back too |
| 4 | Move it out again, scroll to it, and press **Ctrl+D** | The view glides back once the selection is gone |
| 5 | During a glide, scroll the wheel | The glide stops and the wheel takes over, with no jump |

### T4: A paste that lands partly out of view

Checks the new paste rule: a paste that would fit is brought into view; others leave the view alone.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Zoom in so the canvas is bigger than the window. Copy a few lines of letters. Put the pointer near an edge of the window, over the canvas, and press **Ctrl+V** | The paste lands partly out of view, and the view glides so it sits in the middle |
| 2 | Paste again with the pointer in the middle of the window | The view doesn't move |
| 3 | Copy something bigger than the window (zoom in a lot if you need to) and paste it | The view doesn't move |

### T5: Arrows in the selection tools

Checks that each selection tool now moves the selection with the arrows, as the Move tool does, without switching tools.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With Rectangle Select, select some letters and press the arrow keys, then **Shift** and an arrow | The letters and the selection move a cell per press, ten with Shift. Rectangle Select stays the tool. History shows Move steps, a run of presses as one step, as with the Move tool |
| 2 | Press **Ctrl+Z** and **Ctrl+Y** a few times, then an arrow again | The undos and redos step back and forth, and the arrow carries on from there, with the letters still floating over what's under them |
| 3 | Press **Enter** | The move ends and nothing is selected; the letters stay where they were left |
| 4 | Press **Enter** again with nothing moving, then press an arrow with nothing selected | Neither does anything |
| 5 | Do steps 1 and 3 with the Ellipse Select, the Lasso, the Polygonal Lasso and the Magic Wand | Each behaves the same |

### T6: When the arrows in a selection tool do nothing

Checks the cases where the arrows must keep out of a selection gesture's way. These are fiddly, so they're spelled out.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With something selected and Rectangle Select, start dragging a new box and, still holding the button, press an arrow | Nothing moves. Letting go makes the new selection as usual |
| 2 | With something selected and the Polygonal Lasso, click once to put a corner down, then press an arrow | Nothing moves. **Esc** cancels the shape as before |
| 3 | With something selected and the Magic Wand, hold the button down on a cell and press an arrow | Nothing moves |
| 4 | With Rectangle Select, move a selection over other letters with the arrows, then start a new drag anywhere and press **Esc** | The moved letters now cover the ones under them: moving the selection back with the arrows leaves blank cells where it was, rather than bringing the covered letters back |

### T7: Reduce motion

Checks the hidden setting that stills the app's motion. It's set by hand, so this one is precise.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Close the app. Open `%APPDATA%\AskiiGraphiks Studio\settings.json` in Notepad and add `"reduceMotion": "true",` on a new line just after the first `{` (the quotes round `true` matter). Save, then start the app | The app starts as normal |
| 2 | Select something | The outline's dashes and the hatching stand still |
| 3 | Drag a toolbar or a window to dock it somewhere else | The landing outline's dashes stand still, and things snap into place at once instead of sliding or fading |
| 4 | Repeat T3's steps 1 and 2 | The view jumps back at once instead of gliding |
| 5 | Close the app, delete the line you added, save, and start the app again | Everything moves as before |

### T8: Tests to revisit

| Step | Test | Where | Why |
|---|---|---|---|
| 4.8 | T4 | Windows and macOS | It failed and was fixed; you said you'd take a quick look rather than run it again |
| 4.9 | T2, P1 | Windows | Replaced by T1 and T2 here, so there's no need to run the old ones again |

## Worth poking at (optional)

### P1: A whole layer off a corner, with the window resized

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Move the whole layer off a corner as far as it goes, scroll to it, then make the window smaller and larger | The view stays sensible: you can still scroll to the layer, and nothing jumps while you resize |

### P2: Zooming during a glide

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Trigger a glide (as in T3 step 2) and zoom with **Ctrl** and the wheel while it runs | The glide stops and the zoom keeps the spot under the pointer still |
