<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.5d: Sub-toolbars, manual test

**Build:** the commit that marks this step `[~]`
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 25 min

## What changed

**Your changes to how a drag looks (T1 to T4).** Over a zone, you now see only a dashed outline around the strip or square, and the toolbar you're holding goes much more see-through. Corner squares are bigger. The dashes now run around the outline like ants, instead of the outline zooming. A toolbar pushed back from over the widgets looks as it did, apart from the dashes.

**Sub-toolbars (T5 to T11).** A toolbar can now have a smaller toolbar beside it, showing the rest of a tool group. Real tool groups arrive in M3, so for now the Debug menu puts a sample on any toolbar. Only one shows at a time. It sits on the canvas side of its toolbar, pushes the toolbars of neighbouring edges out of the way, and goes with its toolbar through a drag.

## Setup

1. Pull, then from the repo root run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider). It has to be a Debug build for the Debug menu, which both of those are.
2. Window → Reset Layout, so you start from the default.
3. Resize the canvas to about 200 × 80 and type some art, so there's something to see behind things.

## Tests

### T1: Anchoring shows only a dashed strip

**Also on macOS:** Yes, because it's drawn every frame

Hold each drag until the row says to let go.

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag Tools's grip into the middle of the canvas | Blue zones show. The toolbar is solid, and no outline shows |
| 2 | Move over the left strip | The strip's blue fades, leaving a dashed outline right around the strip. There's no copy of the toolbar. The toolbar you're holding turns vertical and goes much more see-through |
| 3 | Look along the rest of the edges | Nothing else moved or changed |
| 4 | Move over the top strip, then the bottom left square | Each time the outline goes around that strip or square, and the blue comes back on the one you left |
| 5 | Move back to the middle of the canvas | The outline goes, and the toolbar is solid again |
| 6 | Move over the left strip and let go | The toolbar lands on the left edge, centred and solid |
| 7 | Drag it to the middle of the canvas and press Esc | It goes back to the left edge, solid and in its own shape |

- [ ] Pass  - [ ] Fail. Notes:

### T2: The dashes run

**Also on macOS:** Yes, because it's drawn every frame

| # | Do this | Expected result |
|---|---|---|
| 1 | Reset Layout. Hold a toolbar over the left strip and watch the outline | The dashes run steadily around the rectangle, hugging its edges. The outline itself doesn't grow or shrink |
| 2 | Watch the corners for a few seconds | No jump or seam anywhere, as dashes pass each corner |
| 3 | Hold it over a corner square, then a top or bottom strip | The same, at about the same speed and dash size |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Corner squares

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Reset Layout. Drag Tools into the middle and look at the bottom left square | About as big as the top strip's height plus the left strip's width: much bigger than it was |
| 2 | Compare it with the strips beside it | It's wider than the left strip and taller than the bottom strip. Each strip stops a gap short of it |
| 3 | Press Esc. Hide File and Edit from the Window menu, drag Tools into the middle again, and look at the top corners | Squares there too, with the strips running between them |
| 4 | Press Esc, then Reset Layout. Debug → Allow Tiny Window, and make the window as small as it goes. Drag a toolbar | Squares never overlap each other. Some zones may be gone for lack of room |

- [ ] Pass  - [ ] Fail. Notes:

### T4: A pushed-back toolbar is as it was

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Reset Layout. Drag Tools over the widgets, about halfway down, and hold | The toolbar stays solid and follows the pointer. A see-through copy shows just left of the widgets, in a dashed outline, and the dashes run |
| 2 | Look at the right strip | It fades away where the copy overlaps it |
| 3 | Let go | The toolbar lands exactly on the copy |

- [ ] Pass  - [ ] Fail. Notes:

### T5: A sub-toolbar on each kind of toolbar

**Also on macOS:** Yes, because of the check marks in the system menu bar

| # | Do this | Expected result |
|---|---|---|
| 1 | Reset Layout. Debug → Sub-Toolbar on File | A bar with Sample 1 to 3 shows below File, with a gap, its left edge in line with File's. It has no grips. The Debug item has a check mark |
| 2 | Debug → Sub-Toolbar on Edit | The sample moves below Edit, its right edge in line with Edit's. The check mark moves. Only ever one sample |
| 3 | Debug → Sub-Toolbar on View | It shows above View, its right edge in line with View's |
| 4 | Debug → Sub-Toolbar on Tools | It shows above Tools, centred on it |
| 5 | Debug → Sub-Toolbar on Tools again | It goes, and the check mark clears |
| 6 | Drag File onto the left strip and drop it, then Debug → Sub-Toolbar on File | It shows right of File, standing vertical and centred on it |
| 7 | Drag File onto the right strip and drop it | The sample goes with it, and shows left of File |
| 8 | Reset Layout. Right-click File and choose Vertical | The sample stands vertical to the right of File, its top in line with File's |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Its length

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Reset Layout. Debug → Sub-Toolbar on File, then Debug → Long Sub-Toolbar | The sample has grown to about one and a half times File's length, and stops there. It fades on its far end |
| 2 | Scroll it with the wheel over it | The buttons scroll, and the fades follow, as on a toolbar |
| 3 | Debug → Long Sub-Toolbar again | Back to 3 buttons, shorter than the limit |
| 4 | Debug → Long Sub-Toolbar, then Debug → Allow Tiny Window. Narrow the window until File and Edit are shrinking and fading | The sample is now no longer than File |
| 5 | Widen the window again | It goes back to one and a half times File's length |

- [ ] Pass  - [ ] Fail. Notes:

### T7: What it pushes

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Reset Layout. Drag Tools onto the right strip and drop it | Tools stands in the middle of the right edge |
| 2 | Debug → Sub-Toolbar on Edit, with Long Sub-Toolbar off | Tools hasn't moved, because the sample doesn't reach it |
| 3 | Debug → Allow Tiny Window, then drag the bottom of the window up until Tools would reach the sample | Tools stops a gap below the sample and moves down with it, instead of overlapping it. This is your mockup |
| 4 | Take the sample away and put it back, using Debug → Sub-Toolbar on Edit, while the window is that short | Tools slides out of the sample's way, briefly, and back |
| 5 | Keep making the window shorter | Tools shortens and fades only once it no longer fits. Below its smallest size, it hides |
| 6 | Debug → Sub-Toolbar on Edit, to take the sample away | Tools slides back to where it wants to be |
| 7 | Reset Layout. Right-click File, choose Vertical, then Debug → Sub-Toolbar on File | File stands in the corner with the sample to its right. Nothing on the top edge overlaps it, and anything long enough to reach moves right to make room |

- [ ] Pass  - [ ] Fail. Notes:

### T8: The canvas doesn't move

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Reset Layout. Note exactly where the art is. Debug → Sub-Toolbar on File | The art doesn't move. The sample covers it, which is fine |
| 2 | Debug → Sub-Toolbar on Tools, then View, then Edit | Still no movement, each time |
| 3 | Take the sample away | Still no movement |

- [ ] Pass  - [ ] Fail. Notes:

### T9: Floating toolbars remember the side

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Reset Layout. Debug → Sub-Toolbar on File. Drag File into the middle of the canvas and drop it | The sample stays below, centred on File |
| 2 | Debug → Sub-Toolbar on Tools. Drag Tools into the middle of the canvas and drop it | The sample stays above, because Tools came from the bottom edge |
| 3 | Drag Tools up until it's just below the top strip, with the pointer off the strip. Watch the sample, and drop it there | The sample flips below as you get there, because there's no room above |
| 4 | Drag Tools back to the middle and drop it | The sample goes back above |
| 5 | Right-click Tools and choose Vertical | The sample stands vertical on Tools's left. Above goes with left, and below with right |
| 6 | Drag Tools until it's just right of the left strip, with the pointer off the strip | The sample flips to the right, and goes back to the left when you move away. Drop it back in the middle |
| 7 | Close the app and run it again, then Debug → Sub-Toolbar on Tools | The sample's on Tools's left again, so the side was saved. The sample itself wasn't, so it wasn't showing until you asked |

- [ ] Pass  - [ ] Fail. Notes:

### T10: Dragging a toolbar that has one

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Reset Layout. Debug → Sub-Toolbar on File. Drag File into the middle of the canvas, without letting go | The sample follows, centred below File, at the same speed with no lag |
| 2 | Look along the left edge and the top | The zones ignore where the sample was. The left strip starts a gap below File's corner square |
| 3 | Move over the left strip | The sample turns vertical too, on the right of File, and both are see-through. The dashed outline is only around the strip |
| 4 | Move over the widgets and hold | Both are solid. See-through copies of both show where they'd land, each in its own dashed outline |
| 5 | Press Esc | Both slide back to where they started, the right shape |
| 6 | Drag File to the left strip and drop it | Both land: File on the left edge, and the sample to its right |

- [ ] Pass  - [ ] Fail. Notes:

### T11: Dragging another toolbar

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Reset Layout. Debug → Sub-Toolbar on File. Drag Tools into the middle of the canvas | The sample stays with File. The left strip starts a gap below the sample, not below File |
| 2 | Press Esc. Debug → Long Sub-Toolbar, and drag Tools into the middle again | No zone overlaps the sample |
| 3 | Drop Tools on the left strip | Tools lands on the left edge, clear of the sample |
| 4 | Click a Sample button, then type on the canvas | Nothing happens from the button, and the text goes to the canvas |
| 5 | Right-click the sample | No menu |
| 6 | Press its bar and drag | Nothing moves. It can't be dragged |
| 7 | Hide File from the Window menu | The sample goes with it |
| 8 | Show File again | It comes back |
| 9 | Window → Reset Layout | The sample stays on File, which goes back to its corner |

- [ ] Pass  - [ ] Fail. Notes:

## How it feels

Not pass or fail, but this is the part only you can judge, so tell me about anything here:

- **How see-through the held toolbar is** over a zone (35%).
- **The ants:** their speed (24 a second), and their dash size.
- **The corner squares.** They're the two strip thicknesses added together, so about 110 today, and double a strip once toolbars are all one shape. Whether you can still float a toolbar near a corner.
- **The strip's fill.** I read "just the dashed line" as no blue at all over the zone you're on. If you'd like a faint fill left in it, that's easy.
- **A corner's outline is the square,** not the toolbar. A long toolbar lands longer than the square it was dropped on. That follows from having no preview.
- **Whether the sub-toolbar should show where it would land while anchoring.** Right now only the strip's outline shows, and the sample shows the side by going with the toolbar. An outline for it is easy to add, but it would be a second box under a long strip, which might be the sort of overlap you dislike.
- **A sub-toolbar appearing.** It shows at once, while what it pushes slides. It could slide out of its toolbar instead.
- **A floating toolbar's sub-toolbar overlaps anchored toolbars,** rather than pushing them. You said floating toolbars don't push anything, so I kept it the same.

## Known limitations (not bugs)

- The sample's buttons do nothing. Tool groups arrive in M3.
- The Debug items only exist in Debug builds.
- Vertical toolbars are still oddly wide or long, as you expected, until toolbars are all one shape.
- Windows still can't be dragged. That's 2.5e.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2.5d, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
