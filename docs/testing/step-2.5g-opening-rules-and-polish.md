<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.5g: Opening rules and polish, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 25 min

## What changed

- **New windows:** a new window with nowhere to go as a widget becomes a tab in the largest dock window.
- **Small app window:** when the app window gets too small, dock columns stop shrinking at the narrowest a window can be, and the canvas gives way instead.
- **Dragging a window:** this is the version you tuned with your mockups and notes.
  - The windows around a drop spot move apart for a small hatched strip.
  - Making a tab shows a "+" tab and an outline instead.
  - A window alone on its side goes back only from that side's edge strip.
- **Dock window menus:** Turn into Widget is now hidden, not greyed out, when the dock couldn't become widgets.

You've seen most of this while we tuned it, so as you asked, these tests only cover edge cases. T7 is the new set of tests that closes step 2.1, whose old retest list checked Dock.

## Setup

1. Pull, then from the repo root run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Choose Window → Reset Layout before each test. That leaves Layers above History on the right, as widgets, and forgets where the sample windows were.
3. Make the app window wide, about 1400 pixels or more, except where a test says otherwise.

Words used below, as in 2.5f's test doc:

- **A dock** is a side whose windows are opaque and edge to edge, in one or more **columns**. Column 1 is at the app window's edge, and column 2 is beside it on the canvas side.
- **A tab strip** is the row at the top of a dock window, holding its **tabs**.
- **An edge strip** is the blue strip along a side's outer edge that shows while a window is held.
- **The marker** is the hatched strip with a "+" that shows where a held window would go.

## Tests

### T1: A new window when no side can take a widget

This checks opening rule 4: with no room for a widget on either side, a new window becomes a tab in the dock window with the most area, not just the first one.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock | Layers and History become a dock on the right, one above the other |
| 2 | Choose Debug → Sample Window 1 | It opens as a widget on the left, since a dock can't take widgets |
| 3 | Click Sample Window 1's ≡ and choose Turn into Dock | The left becomes a dock too |
| 4 | Choose Debug → Sample Window 2 | It becomes a tab beside Sample Window 1, and shows. That window is the full height of the app window, so it has more area than Layers or History, which are half height each |
| 5 | Drag the right dock's inner edge (its edge on the canvas side) towards the middle, until the right dock is well over twice as wide as the left. Then drag the border between Layers and History down a little, so Layers is the taller | Layers is now the largest dock window |
| 6 | Choose Debug → Sample Window 3 | It becomes a tab in Layers' strip, not the left's, and shows |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Docks in a small app window

This checks what gives way when the app window is too small for all the dock columns: they shrink together down to the narrowest a window can be, and then the canvas shrinks instead.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock. Drag History's tab onto the dock's inner edge and let go. Then choose Debug → Sample Window 1, and turn it into a dock with its ≡ | Three columns: Layers and History on the right, Sample Window 1 on the left |
| 2 | Choose Debug → Allow Tiny Window. Then drag the app window's right edge slowly to the left, as far as it goes | First the canvas narrows, and toolbars may scroll or hide. Then all three columns narrow together. When they're about two thirds of their usual width, they stop, and the canvas narrows again until there's almost nothing left of it |
| 3 | Drag the app window's edge back out to where it was | Each column comes back to exactly the width it had in row 1 |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Windows that can't give the strip room

This checks where the room for the marker comes from when the windows beside it are already as small as they go. In a stack, the others give it up. Between columns at their narrowest, the dock widens instead.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock. Choose Debug → Sample Window 1, which opens on the left. Drag it by its name to the very bottom of History, and let go when the marker shows below History | One column, top to bottom: Layers, History, Sample Window 1 |
| 2 | Drag the border under Layers up as far as it goes before Layers snaps to just its tab strip. Do the same with the border under History | Layers and History are as short as they go, and Sample Window 1 is tall |
| 3 | Choose Debug → Sample Window 2, which opens on the left. Drag it by its name onto the bottom edge of Layers, just above History's tab strip, and hold | The marker opens between them. Layers and History keep their heights: History slides down, and Sample Window 1, at the bottom, gets shorter to make the room |
| 4 | Let go | Sample Window 2 sits between Layers and History |
| 5 | Window → Reset Layout. Click Layers' ≡ and choose Turn into Dock, then drag History's tab onto the dock's inner edge and let go | Two columns: Layers at the edge, History beside it |
| 6 | Drag the border between the two columns towards the app window's edge as far as it goes, then the dock's inner edge the same way | Both columns are as narrow as they go |
| 7 | Choose Debug → Sample Window 1. Drag it onto the border between the two columns, and hold | Neither column gets narrower. History slides towards the canvas to make the room, so the dock widens |
| 8 | Let go | Sample Window 1 is a new column between Layers and History |

- [ ] Pass  - [ ] Fail. Notes:

### T4: The marker's spacing in a dock

This checks your last note: in a dock, the marker is centred in its room, with the same small gap on all four sides.

**Also on macOS:** Yes, because Retina screens draw small gaps at a different scale

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock. Choose Debug → Sample Window 1, and drag it by its name onto the border between Layers and History, and hold | The windows part, and the marker sits between them. The gap above it, below it, and at its left and right ends all look the same, about 4 pixels |
| 2 | Still holding, move onto the dock's inner edge | The marker stands at the dock's inner edge, with the same gap from the dock, from the canvas side of its room, and from the top and bottom |
| 3 | Move onto the right's edge strip | The dock moves in. The marker is between it and the app window's edge, with the same gap on every side |
| 4 | Move to just below Layers' tab strip, at the top of the column | Layers slides down, and the marker shows at the very top of the column, with the same gap on every side. It's as deep as the bands, so it's much thicker than the marker in row 1 |
| 5 | Still holding, look at where the pointer is now | It may now be over Layers' moved tab strip, but the marker stays. That's expected: drop zones stay where they were when you picked the window up, so that they don't jump around as things move |

- [ ] Pass  - [ ] Fail. Notes:

### T5: A window alone on its side

This checks the bug you found, where the place a lone window left behind took drops all across its old width, and the dock version, which showed two strips.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Close History with its ✕. Drag Layers by its name onto the left's edge strip, and let go | Layers is a widget on the left, and the right is empty |
| 2 | Drag Layers by its name into the middle of the canvas, and hold. Then move it slowly back to the left, without letting go | Nothing opens and no marker shows while the pointer is over where Layers was. That's what's fixed: before, the whole old width counted |
| 3 | Keep moving until the pointer is on the blue edge strip at the far left | The marker shows, and toolbars on the left move in |
| 4 | Let go | Layers is back where it was, at the same width |
| 5 | Click Layers' ≡ and choose Turn into Dock | Layers is a dock on the left, alone |
| 6 | Drag Layers by its tab into the middle of the canvas, and hold | There's one blue strip on the left, at the app window's edge, and one on the right. Before, a second strip showed just inside where the dock's inner edge was |
| 7 | Still holding, move to just inside where the dock's inner edge was | No marker: letting go here would float Layers |
| 8 | Move the pointer out past the app window's left edge, beyond the app altogether, and let go | Layers goes back to being a dock on the left, at the same width. Past the app window's edge, the edge strip still counts |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Making a tab in a crowded tab strip

This checks the "+" tab when a tab strip has more tabs than fit, and the menu change for a dock with tabs.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock. Drag History's tab into the middle of Layers and let go. Then open Sample Windows 1 and 2 from the Debug menu, and drag each into the middle of Layers and let go | Layers' tab strip has four tabs, more than fit, and fades on the side with more |
| 2 | Choose Debug → Sample Window 3. Drag it by its name into the middle of Layers, and hold | The tab strip scrolls to its end, where a hatched "+" tab shows, as long as Sample Window 3's tab will be. The window keeps showing whichever tab was selected, with a running dashed outline around it |
| 3 | Let go | Sample Window 3 is the last tab, the same length as the "+" was, and it shows |
| 4 | Right-click any tab | The menu has Float and Close only. Turn into Widget is hidden, because a dock with tabs can't become widgets |

- [ ] Pass  - [ ] Fail. Notes:

### T7: What's left of 2.1

This closes step 2.1, whose last retest checked the old docking. Most of it is already covered by 2.5b to 2.5f, so these are the leftovers: the menu bar and status bar, and shortcuts and dialogs while a window floats.

**Also on macOS:** Yes, rows 3 and 4, because shortcuts and dialogs work differently there. Rows 5 and 6 are Windows only, since the Mac has no menu bar strip

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag Layers by its name up over the menu bar, and let go there. Then do the same over the status bar | Each time it floats, moved back inside, just below the menu bar or just above the status bar. Nothing docks above the menu bar or below the status bar |
| 2 | Try to drag the menu bar and the status bar | They don't move |
| 3 | Leave Layers floating. Click the canvas and type a letter. Then click Layers' name, and press Ctrl+Z (Cmd+Z on a Mac) | The letter is undone |
| 4 | Choose File → New, and while the dialog is open, try to click Layers' ≡ | Layers doesn't respond until you close the dialog |
| 5 | Press Alt, then F | The File menu opens |
| 6 | Press Esc until the menu closes, then type a letter without clicking anything | The letter lands at the caret on the canvas |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Sweep a held window slowly across a dock with several columns and stacked windows, watching for a marker that's off-centre or a window that jumps rather than slides.
- Squeeze a dock window to its tab strip, then hold a window on the borders above and below it.
- Drag a window over a dock while the app window is very small (Debug → Allow Tiny Window).
- With docks on both sides, close every window, then reopen them in a different order from the Window menu.

## Known limitations (not bugs)

- Opening rule 5, a new window floating because both sides are full of widgets, needs 8 windows, and the app has 5. The automated tests cover it.
- The marker's sizes are set in pixels at 100% scaling, so they grow with Windows' display scaling.
- The look of the marker, the "+" tab and docks is a placeholder, for you to replace.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2.5g, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
