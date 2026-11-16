<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.5f: Docks, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 30 min

## What changed

Windows can now be docks, as in Illustrator: opaque columns along a side, edge to edge, where windows stack and share places as tabs. You can make one by dropping a window on a side's edge strip or with Turn into Dock, rearrange it by dragging tabs, resize its columns and windows, and turn it back into widgets.

Also in this build:

- A held toolbar follows the pointer past the app window's edge, as a window does, and goes back inside on the drop.
- A fix: a window's ≡ button opened its menu with items that don't apply, such as Turn into Widget on a widget. Right-clicking was already right.

## Setup

1. Pull, then from the repo root run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Choose Window → Reset Layout before each test. That leaves Layers above History on the right, as widgets.
3. Make the app window wide, about 1400 pixels or more, so that a few columns fit.

Words used below:

- **A dock** is a side whose windows are opaque and edge to edge, in one or more **columns**. Column 1 is the one at the app window's edge, column 2 is beside it on the canvas side, and so on.
- **A tab strip** is the row at the top of a dock window, in place of its name. Each **tab** is a window's name. Windows that share a place are tabs in one strip, and only the selected tab's window shows.
- **An edge strip** is the blue strip along a side's outer edge that shows while a window is held.
- **A band** is a strip along the inside of a dock window's top, bottom, left or right edge, where a drop puts the window beside it. The middle is everything inside the bands and below the tab strip.
- **The inner edge** of a dock is its edge on the canvas side.

## Tests

### T1: Turn a side of widgets into a dock

This checks the ≡ menu fix and Turn into Dock on a widget, which converts the whole side.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ | The menu has Float, Turn into Dock and Close, and nothing else |
| 2 | Choose Turn into Dock | Layers and History become a dock: opaque, touching the app window's edge, the top and each other, with no gaps. Each has a tab strip with its own name, and they keep their heights |
| 3 | Move the pointer over either window | No ≡ or ✕ appears |
| 4 | Right-click Layers' tab | The menu has Float, Turn into Widget and Close. Turn into Widget is enabled |
| 5 | Choose Turn into Widget | Both are widgets again, with their margins and gap, and the same heights |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Make a dock from an edge strip

This checks the edge strip of a side that already has windows, which now makes a dock.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag Layers by its name over the canvas and hold it there | Blue edge strips show on both sides, the left's because it's empty and the right's because History is there |
| 2 | Move the pointer onto the right's edge strip, then let go | The right becomes a dock. Layers is column 1, at the app window's edge, and History is column 2, beside it |
| 3 | Drag Layers' tab onto the left's edge strip, and let go | Layers becomes a widget on the left, keeping its width. History stays a dock on the right |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Tabs

This checks putting windows together as tabs, switching between them, and moving tabs along a strip.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock | Layers above History, in one column |
| 2 | Drag History's tab into the middle of Layers, and hold it | History looks like a floating window under the pointer, and a placeholder covers the column |
| 3 | Let go | Layers' tab strip shows Layers then History, with History's tab highlighted and History's contents showing. The pair fills the column |
| 4 | Click the Layers tab | Layers' contents show and its tab is highlighted. Nothing changes until the button comes up |
| 5 | Drag the History tab along the strip to the left of the Layers tab, and let go | The strip shows History then Layers, with History showing |
| 6 | Press the History tab and drag it out over the canvas, then let go | Only History comes off and floats. Layers stays in the dock |
| 7 | Choose Debug → Sample Window 1, 2 and 3, and drag each into the middle of Layers | Five tabs in one strip. The ones that don't fit fade at the strip's end |
| 8 | Turn the mouse wheel over the strip | The tabs scroll sideways, with a fade on whichever side has more |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Where a drop on a dock goes

This checks the bands and the tab strip, which decide where a dropped window goes in a dock.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock. Then choose Debug → Sample Window 1 | Sample Window 1 opens as a widget on the left |
| 2 | Drag Sample Window 1 over the top band of Layers, just under its tab strip, and let go | It's stacked above Layers, in the same column |
| 3 | Drag it over the bottom band of History, and let go | It's stacked below History |
| 4 | Drag it over the left band of Layers, the side towards the canvas, and let go | It becomes column 2, to the left of Layers and History |
| 5 | Drag it over the right band of Layers, the side towards the app window's edge, and let go | It becomes column 1, at the edge, and Layers and History are column 2 |
| 6 | Drag it onto Layers' tab strip, just before the Layers tab, and let go | It becomes a tab before Layers |
| 7 | Drag it over the right's edge strip, and let go | It becomes a new column 1 at the very edge |

- [ ] Pass  - [ ] Fail. Notes:

### T5: As many columns as fit

This checks that a new column is only offered while it would fit, with the canvas keeping its minimum width.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock. Open Sample Windows 1 to 3 | They open on the left |
| 2 | Drag each Sample Window onto the right's edge strip in turn | Each becomes a new column at the edge, as long as there's room |
| 3 | Keep going, or narrow the app window and drag one more | Once another column wouldn't fit, the right's edge strip no longer shows, and the dock windows have no left and right bands. A drop near the app window's edge makes a tab or stacks instead, depending on where in the window it lands |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Resizing a dock

This checks your rule that a border resizes only the column outside it, with the canvas giving way.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag Layers by its name onto the right's edge strip, and let go | Two columns: Layers at the edge and History beside it, each with one window |
| 2 | Move the pointer over the border between the two columns | A faint blue line, and a left-right resize pointer |
| 3 | Drag the border left by about 100 pixels, and let go | Layers, the outer column, gets 100 wider. History keeps its width and moves left, and the canvas gives way |
| 4 | Drag the inner edge, on History's canvas side, to the right as far as it goes | History stops at its narrowest, about 160 |
| 5 | Drag the inner edge left as far as it goes | History stops when the canvas region is at its minimum |
| 6 | Choose Debug → Sample Window 1 and drag it to the bottom band of History. Then drag the border between History and Sample Window 1 up and down | It works like the gap between widgets: only those two change height |

- [ ] Pass  - [ ] Fail. Notes:

### T7: Double-clicking a tab

This checks your rule: double-clicking a tab squeezes its window, unless it's alone in its column.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock. Then double-click the Layers tab | Layers squeezes to its tab strip, and History takes the room |
| 2 | Double-click the Layers tab again | Layers opens to the height it had |
| 3 | Drag History's tab into the middle of Layers, then double-click either tab | Nothing changes, because the pair is alone in its column |
| 4 | Choose Debug → Sample Window 1 and drag it to the bottom band of the pair. Click the Layers tab, then double-click the History tab | History shows, and the pair squeezes to its tab strip |

- [ ] Pass  - [ ] Fail. Notes:

### T8: The menus of dock windows

This checks right-clicking tabs, and Turn into Widget, which stays greyed out for a dock that widgets couldn't hold, as you chose for now.

**Also on macOS:** Yes, because right-clicking (or Control-clicking) works differently there

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock. Drag History's tab into the middle of Layers, then click the Layers tab | Layers and History are tabs, with Layers showing |
| 2 | Right-click the History tab | A menu opens at the tab, with Float, Turn into Widget and Close. Turn into Widget is greyed out, because widgets can't have tabs |
| 3 | Choose Float | History floats. Layers is still docked |
| 4 | Right-click History's name, and choose Turn into Dock | History joins the dock as a new column 1, at the edge, keeping its width as far as it fits |
| 5 | Right-click the History tab | Turn into Widget is greyed out, because the dock has two columns |
| 6 | Right-click the empty part of a tab strip, to the right of the tabs | The menu of the window showing opens |

- [ ] Pass  - [ ] Fail. Notes:

### T9: Turn into Dock on a floating window, with no dock

This checks your answer: with no dock, the side the widget rules would choose becomes the dock.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag Layers by its name over the canvas, and let go | Layers floats. History is alone on the right |
| 2 | Right-click Layers' name, and choose Turn into Dock | Layers becomes a dock on the left, because the left had fewer windows. History stays a widget on the right |
| 3 | Drag History over the canvas, then right-click its name and choose Turn into Dock | History joins the left's dock as column 1, at the edge, and Layers is column 2 |

- [ ] Pass  - [ ] Fail. Notes:

### T10: Closing and reopening a tab

This checks that a dock window goes back where it was when it's reopened.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click Layers' ≡ and choose Turn into Dock. Drag History's tab into the middle of Layers | Layers and History are tabs, with History showing |
| 2 | Right-click the History tab and choose Close | History goes, and Layers shows |
| 3 | Choose Window → History | History comes back as a tab after Layers, and shows |

- [ ] Pass  - [ ] Fail. Notes:

### T11: A held toolbar past the app window's edge

This checks the change you asked for at the end of 2.5e: toolbars go off-screen while held and come back on the drop, as windows do.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag the File toolbar by its grip past the app window's left edge, and hold it | The toolbar follows the pointer outside the app window. A preview inside shows where it will go back to |
| 2 | Let go | The toolbar lands where the preview showed |
| 3 | Drag the Tools toolbar past the bottom edge, then back over a zone inside, and let go | It lands in that zone, as usual |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Docks on both sides at once, and every combination of widgets, docks and empty on the two sides.
- Drag a tab around its own strip and over its own window before letting go.
- Squeeze a dock window, then drag the border under it.
- Resize the app window smaller and larger with several columns.
- Save and restart with docks and tabs in place.

## Known limitations (not bugs)

- The look of docks and tabs is a placeholder, for you to replace.
- Turn into Widget is greyed out rather than hidden when it can't work, as you chose for now. It's noted in `docs/TEMPORARY-UI.md` to become hidden once everything is in place.
- While a tab is held over a dock window, that window's contents give way to the placeholder, because the held tab would be the one showing.
- New windows don't become tabs yet: where they go is 2.5g's opening rules.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2.5f, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
