<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.5e: Widgets and floating windows, manual test

**Build:** the commit that marks this step `[~]`
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 30 min

## What changed

Windows can finally be moved and resized. A window's title drags it into a stack, onto an empty side, or out to float on its own. The gaps between widgets and the side's inner edge resize them. A window squeezed too small snaps to just its title. Each window has a menu button and a close button that only show when the pointer is over it.

Only Layers and History exist so far, which isn't enough for your stack examples, so the Debug menu now opens up to 3 empty sample windows.

## Setup

1. Pull, then from the repo root run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider). It has to be a Debug build for the Debug menu, which both of those are.
2. Choose Window → Reset Layout before each test. That leaves Layers above History on the right, and closes any sample windows.
3. Resize the canvas to about 200 × 80 and type some art, so there's something to see behind things.

Words used below:

- **The title row** is the strip at the top of a window with its name in it. Drag the name, not the buttons.
- **The gap** is the thin space between two windows in a stack.
- **The inner edge** is the thin space just beside a side's windows, on the canvas side.

## Tests

### T1: The menu and close buttons

This checks that the buttons stay out of the way until they're wanted.

**Also on macOS:** Yes, because it depends on hover

| # | Do this | Expected result |
|---|---|---|
| 1 | Move the pointer over the canvas | No window shows any buttons |
| 2 | Move the pointer over the middle of Layers | ≡ and ✕ appear faintly at the right of Layers's title row. History still shows none |
| 3 | Move the pointer onto Layers's ✕ | The ✕ is fully bright, with a lighter square behind it. The ≡ stays faint |
| 4 | Click Layers's ≡ | A menu opens with Float, Turn into Dock (greyed out) and Close. Press Esc |
| 5 | Right-click the word "Layers" | The same menu opens. Press Esc |
| 6 | Click Layers's ✕ | Layers closes, and History fills the side |
| 7 | Choose Window → Layers | Layers comes back above History, at the same height as before |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Resizing widgets

This checks the gap and the inner edge. Art on the canvas shouldn't move until you let go.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Point at the gap between Layers and History | The pointer becomes an up-and-down arrow |
| 2 | Drag the gap down about 100 pixels and let go | The gap follows the pointer. Layers gets taller, and History shorter |
| 3 | Point at the inner edge, left of Layers | The pointer becomes a left-and-right arrow |
| 4 | Drag the inner edge left about 100 pixels and hold | Both windows get wider together, and the toolbars on the right move left. The art doesn't move |
| 5 | Let go | The art re-centres in the space that's left |
| 6 | Drag the inner edge right as far as it goes, and let go | It stops at 160 wide |
| 7 | Drag the inner edge left as far as it goes, and hold | It stops, leaving at least 320 for the canvas area |
| 8 | Press Esc, still holding, then let go | The windows go back to how they were before this drag |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Title-only

This checks that a window is either its title or at least its minimum height, with nothing in between.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag the gap between Layers and History down slowly, and keep holding | History shrinks until it reaches its minimum height, then stops, and the gap stops following the pointer |
| 2 | Keep dragging down about 60 pixels more | History snaps to just its title, and Layers jumps to take the rest |
| 3 | Let go | History stays as a title |
| 4 | Drag the gap up slowly, and keep holding | History stays as a title for about 60 pixels, then snaps to its minimum height |
| 5 | Let go, then drag the gap down again until History is a title, and let go | History is a title again |
| 6 | Debug → Sample Window 1, which opens on the left. Drag its title to the very bottom of the right side, and let go | The stack is Layers, History as a title, then Sample Window 1 |
| 7 | Drag the gap between Layers and History down, and hold | Layers grows. History stays a title and moves down with the gap. Sample Window 1 is the one that shrinks, because the next window gives way |
| 8 | Keep dragging down as far as you can, and let go | Sample Window 1 snaps to a title too. Layers still shows its contents, because one window always does |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Your examples of dropping a window

This follows your four examples from `02_ex_04`. A window you're holding goes see-through over a place it could land, like a toolbar does. A hatched rectangle with a "+" shows where it would land.

**Also on macOS:** Yes, because a drag holds on to the pointer

| # | Do this | Expected result |
|---|---|---|
| 1 | **Example 1.** Close History with its ✕. Debug → Sample Window 1, which opens on the left side | Layers is alone on the right, and the sample is alone on the left |
| 2 | Drag the sample's title over the lower half of Layers, and hold | Layers shrinks, sliding briefly, and the hatched "+" shows below it. The sample you're holding is see-through. The left side has emptied, and File moves left |
| 3 | Let go | The sample lands exactly where the hatched "+" was, and is solid |
| 4 | Drag the sample over the upper half of Layers, and let go | The sample lands above Layers |
| 5 | **Example 2.** Window → Reset Layout. Debug → Sample Window 1, which opens on the left. Drag its title over the gap between Layers and History, and hold | Layers and History move apart for the hatched "+" |
| 6 | Let go | The stack is Layers, the sample, History, all about the same height |
| 7 | **Example 3.** Window → Reset Layout. Debug → Sample Window 1. Drag the gap between Layers and History up as far as it goes, so that Layers is just its title. Then drag the sample's title over the upper half of History, and let go | Layers stays just its title. History gives up the room for the sample, which lands between them |
| 8 | **Example 4.** Window → Reset Layout. Drag History's title to the middle of the canvas, and hold | A thin blue strip shows along the far left edge of the app window. It's there because that side is empty |
| 9 | Move the pointer onto that strip, or past the left edge of the app window, and hold | The blue goes, a thin hatched "+" opens at the edge, and File moves right to make room |
| 10 | Let go | History becomes a widget on the left, 240 wide, and File moves right again to clear it |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Other places to drop a window

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag Layers's title a little way, staying over Layers, and hold | The hatched "+" shows where Layers already is. Let go, and nothing has changed |
| 2 | Drag History to the middle of the canvas and press Esc, then let go | History goes back to where it was |
| 3 | Open all 3 sample windows from the Debug menu. Drag windows from the left onto the right-hand stack until it holds 4 | The right side holds 4 windows, sharing the height, and 1 is left on the left side |
| 4 | Drag that last window over the right-hand stack, and hold | There's no hatched "+", and the window stays solid, because the side is full |
| 5 | Let go | It floats on top of the stack |
| 6 | Window → Reset Layout. Close History. Drag Layers to the middle of the canvas, and hold | The right side has emptied, and Edit and View move right, sliding briefly |
| 7 | Press Esc, then let go | Layers goes back, and Edit and View move left again |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Floating windows

**Also on macOS:** Yes, because of the resize cursors and the drag

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag Layers's title to the middle of the canvas and let go | Layers floats there, at the same size. History fills the right side |
| 2 | Drag it over a toolbar and let go | Layers draws over the toolbar, which doesn't move |
| 3 | Drag it off the bottom right of the app window and let go | It stops at the edge, and stays entirely inside |
| 4 | Move it back to the middle. Point at each edge and corner in turn | The pointer changes to the matching arrow |
| 5 | Drag the bottom right corner out, and let go | It gets bigger. Its top left doesn't move |
| 6 | Drag the left edge as far right as it goes, and let go | It stops at 160 wide, with its right edge still in place |
| 7 | Drag the bottom edge up until it's near the title, and let go | It snaps to just its title, as in T3 |
| 8 | Drag its title somewhere else and let go | It moves and stays a title |
| 9 | Drag the bottom edge down about 60 pixels and let go | It snaps open to its minimum height |
| 10 | Drag History out to float too, overlapping Layers. Then click Layers | Layers comes to the front. Click History, and History comes to the front |
| 11 | Drag Layers's title over History's and let go | Layers floats on top of History |
| 12 | Open Layers's ≡ | The menu has Turn into Widget and Close |
| 13 | Choose Close, then Window → Layers | Layers comes back where it was floating, at the same size |
| 14 | Choose Turn into Widget in Layers's menu | Layers becomes a widget. With both sides empty, it goes on the right |
| 15 | Choose Float in Layers's menu | It comes off the side, a little in and down from where it was, at the same size |

- [ ] Pass  - [ ] Fail. Notes:

### T7: The app window changing size, and restarting

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Float Layers near the bottom right of the canvas. Note where it is | Layers floats there |
| 2 | Make the app window smaller, then make it bigger again | Layers stays inside as the window shrinks, and goes back to where it was as it grows |
| 3 | Close the app and start it again | Layers is floating where you left it |
| 4 | Window → Reset Layout. Squeeze History to a title and widen the side. Close the app and start it again | History is still a title, and the side is still that wide |
| 5 | Open a sample window, close the app and start it again | The sample window is gone, as samples aren't kept |

- [ ] Pass  - [ ] Fail. Notes:

### T8: Tests to revisit

Skip any you've already done.

| Step | Test | Where | Why |
|---|---|---|---|
| 2.5d | T1 to T7 of `step-2.5d-hidden-toolbars.md` | Windows | Your last changes to toolbar drags, if you haven't run them yet |
| 2.5d | T2 of `step-2.5d-hidden-toolbars.md` | macOS | It's marked for a Mac |
| 2.5d | T1, T2 and T5 of `step-2.5d-sub-toolbars.md` | macOS | They were marked for a Mac, and your results didn't say which platform they ran on |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Drag a toolbar while a window floats over the canvas.
- Start another drag straight after a drop, before anything has finished sliding.
- Move a window while Debug → Allow Tiny Window has made the app window very small.
- Switch to another app in the middle of a drag or a resize. It should cancel.

## Known limitations (not bugs)

- Docks arrive in 2.5f. Until then Turn into Dock is greyed out, and dragging a window to the edge of a side with widgets on it does nothing special.
- The hatched "+", the buttons' icons and the strips are placeholders, for you to replace.
- Frosted glass is still waiting for M8.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2.5e, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
