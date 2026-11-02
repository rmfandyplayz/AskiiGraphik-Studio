<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.1: Dock shell, manual test

**Build:** working tree
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 25 min

## What changed

The canvas now paints the whole window, and everything else sits on top of it as a widget. Art shows around the widgets and slides under them, but the canvas centers and scrolls within the space they leave free. The History and Layers panels are empty for now, but you can dock, tab, float, close and reopen them. Your layout is remembered, and the new Window menu can reset it.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. This step needs a Debug build, for Debug → Open Sample Document.

## Tests

### T1: The canvas fills the window

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Start the app | The menu bar is at the top left and the toolbar is centered in the row below it. Layers is above History at the right. The status bar is at the bottom left. The canvas is centered in the space between them |
| 2 | Choose Debug → Open Sample Document, then zoom in with View → Zoom In until the art is wider and taller than the window | The art shows to the right of the menu bar, to the left and right of the toolbar, and to the right of the status bar. It also shows in the thin gaps around each widget |
| 3 | Scroll around with the scrollbars | The art slides under the widgets. No widget ever shows art on top of it |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Widgets get input first

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the art still large, click on art beside the toolbar, then type a letter | The caret appears where you clicked, and the letter lands there |
| 2 | Click in the middle of the Layers panel | Nothing changes on the canvas, and no caret moves there |
| 3 | Click the toolbar's Show Grid button twice | The grid turns on, then off. The canvas behind the button doesn't get the click |
| 4 | Hover over a widget, then scroll the mouse wheel | The canvas doesn't scroll or zoom |
| 5 | Hover over the canvas and watch the status bar, then hover over a widget | The cell number follows the pointer over the canvas, and stops updating over the widget |
| 6 | Click the Layers panel's title, then type a letter | The letter lands at the caret on the canvas |

- [ ] Pass  - [ ] Fail. Notes:

### T3: The canvas keeps clear of widgets

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose File → New and accept the defaults, then View → Actual Size | The canvas is centered between the widgets, with all of it visible if the window is big enough |
| 2 | Choose Window → Layers, then Window → History | Each one closes and loses its check mark. Once both are closed, the canvas centers in the wider space and the vertical scrollbar moves to the window's right edge |
| 3 | Choose Window → Reset Layout, then zoom in until the canvas is much bigger than the window | Both panels are back. Scrollbars appear beside them, not under them |
| 4 | Scroll to each corner in turn | Every corner of the canvas can be brought fully clear of all widgets, with a small margin around it |
| 5 | Click near the canvas's right edge and type until the caret reaches the edge of the free space | The view scrolls so the caret stays clear of the Layers panel |
| 6 | Press Ctrl+0, then Ctrl+= several times | Zooming stays centered on the middle of the free space, not the middle of the window |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Docking and tabs

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag History's title bar over the Layers panel | Drop targets appear over Layers |
| 2 | Drop it on the center target | History and Layers become tabs in one panel. Clicking a tab switches between them |
| 3 | Drag the History tab back out and drop it on an edge target of Layers | The two panels sit side by side or stacked, depending on the edge |
| 4 | Drag a panel to the far left edge of the window and drop it on the edge target that appears | It docks along the left side at the same width as before. The canvas re-centers in the new free space |
| 5 | Try dragging a panel onto the middle of the canvas and look for drop targets on the canvas | No target offers to drop onto the canvas itself. Releasing there floats the panel (T5) |
| 6 | Try to drag the menu bar, toolbar or status bar | They don't move and have no title bar |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Floating panels stay inside the window

**Also on macOS:** Yes, because Mac menu bars belong to the focused window

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag History over the canvas and release away from any target | It floats over the canvas, inside the main window. No new window appears in the taskbar |
| 2 | Drag the floating panel around, and try to drag it past the window's edge | It moves with the pointer and stays inside the main window |
| 3 | Click on the art around the floating panel | The caret lands on the canvas |
| 4 | Type a letter, click inside the floating panel, then press Ctrl+Z (Cmd+Z on a Mac) | The typing is undone |
| 5 | On a Mac: click inside the floating panel and look at the menu bar | The File menu and the others are still there |
| 6 | Choose File → New while it floats, and try clicking the floating panel while the dialog is open | The panel doesn't respond until you close the dialog |
| 7 | Drag the floating panel back onto Layers and drop it on a target | It docks again |
| 8 | Float it again and click its close button | It closes, and Window → History has no check mark |

- [ ] Pass  - [ ] Fail. Notes:

### T6: The layout is remembered

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Close Layers, float History, then quit the app with File → Quit | The app quits normally |
| 2 | Start the app again | Layers is still closed, with no check mark, and History is still floating where you left it |
| 3 | Choose Window → Reset Layout | Everything is back in the default layout, and nothing floats |
| 4 | Dock History on the left, close the app with the window's X, and start it again | History is still on the left |
| 5 | Choose Window → Reset Layout | The default layout is back |

- [ ] Pass  - [ ] Fail. Notes:

### T7: The menu bar

**Also on macOS:** Yes, because only Windows and Linux have this widget

| # | Do this | Expected result |
|---|---|---|
| 1 | On Windows: open each menu with the mouse, and try one item from each | They open over the art and work like before |
| 2 | On Windows: press Alt, then F | The File menu opens, as it did before this step |
| 3 | On Windows: press Esc until the menu closes, then type a letter | The letter lands at the caret on the canvas |
| 4 | On a Mac: look at the window | There's no menu bar widget. The toolbar is the top row |
| 5 | On a Mac: look at the system menu bar | It has File, Edit, Document, View, Window and Help, and their items work |

- [ ] Pass  - [ ] Fail. Notes:

### T8: Reopening the window on a Mac

**Also on macOS:** Yes, this test is Mac only

| # | Do this | Expected result |
|---|---|---|
| 1 | Close Layers and float History, then close the window with its red button | The app keeps running |
| 2 | Click the Dock icon | A new window opens with the same layout |
| 3 | Choose Window → Layers | Layers comes back, and its check mark returns |
| 4 | Close the window and reopen it twice more, choosing Window → History each time | Each time the layout comes back, the check marks match it, and the toggle works |
| 5 | Close the window, then use the Dock menu's New | A window opens with the saved layout, and the New dialog appears |

- [ ] Pass  - [ ] Fail. Notes:

### T9: A damaged settings file

**Also on macOS:** Yes, because the file's location differs

| # | Do this | Expected result |
|---|---|---|
| 1 | Move a panel so the layout isn't the default, then quit the app | |
| 2 | Open the settings folder: `%APPDATA%\AskiiGraphiks Studio` on Windows, `~/Library/Application Support/AskiiGraphiks Studio` on a Mac | It contains `settings.json` |
| 3 | Open `settings.json` in a text editor, replace everything with `not settings`, and save | |
| 4 | Start the app | A "Settings were reset" message appears over the window, and the layout is the default |
| 5 | Look in the settings folder | The damaged file is there with `damaged` in its name |
| 6 | Click OK, quit the app and start it again | No message this time |

- [ ] Pass  - [ ] Fail. Notes:

### T10: Everything else still works

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Type some text, then undo and redo with the shortcuts | Works as before |
| 2 | Use the wheel to scroll, and Ctrl + wheel to zoom, over the canvas | Works as before, zooming around the pointer |
| 3 | Save a document, reopen it, and open the Resize Canvas and Font dialogs | Works as before |
| 4 | Resize the window, small and large | Widgets keep their positions along their edges and the side panels keep their width. The canvas re-centers |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Tabbing both panels together, then closing the tab group and reopening each from the Window menu.
- Dragging a panel very quickly, or pressing Esc in the middle of a drag.
- Making the window very small, so the toolbar no longer fits.
- Docking a panel at the very top or bottom edge of the window. It can end up above the menu bar's row, which is allowed for now.

## Known limitations (not bugs)

- The widget look is placeholder: square boxes, and Dock's own header on panels. Your design step replaces it.
- Panels fill their dock's height rather than shrinking to their contents, so art only shows in the gaps around them. The menu bar, toolbar and status bar do shrink to fit.
- The History and Layers panels are empty until steps 2.2 and 2.3.
- The toolbar and status bar can't be rearranged yet (M8).
- Widgets sit in rows and columns rather than anchoring anywhere with any offset. That waits for your design.
- Floating panels can't leave the main window, so there are no panels on a second monitor (ADR 0025).

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2.1, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
