<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.5b: Replace Dock, manual test

**Build:** the commit that marks this step `[~]`
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 15 min

## What changed

The Dock library is gone, replaced by our own layout system. Everything is now in your default layout: Layers and History as widgets on the right, and four small toolbars on their anchors. **Nothing can be dragged yet.** That comes back in 2.5c (toolbars) and 2.5e (windows). The Window menu still opens and closes everything.

Your saved layout resets once, because the old one was in Dock's format.

## Setup

1. Pull, then from the repo root run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Make the canvas bigger than the window, or type some art near its edges, so there's something to see behind the widgets. Document → Resize Canvas to about 200 × 80 works.

## Tests

### T1: The default layout

**Also on macOS:** Yes, because the menu bar is different there

| # | Do this | Expected result |
|---|---|---|
| 1 | Look at the window | File toolbar (New, Open, Save) top left. Edit toolbar (Undo, Redo, Resize Canvas, Font) top right, just left of the widgets. Tools toolbar (Text, Eraser) in the bottom middle. View toolbar (zoom buttons, Show Grid) bottom right. Layers above History on the right |
| 2 | Compare the top edge of the File and Edit toolbars with the top of Layers | All level. No toolbar sits above a window any more |
| 3 | Compare the bottom edge of the Tools and View toolbars with the bottom of History | All level |
| 4 | Pan some art under a widget and a toolbar | It shows faintly through them. They're somewhat translucent |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Clicks

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Move the pointer over art right beside a toolbar, or in the gap between Layers and History, while watching the status bar | The cell under the pointer updates, so the canvas is getting the pointer |
| 2 | With the Text tool, click art in the gap between two toolbars and type | The caret lands there and the text appears |
| 3 | Pan so art is under a toolbar, then click one of its buttons | The button works, and nothing happens on the canvas |
| 4 | Click a Layers or History row with art under it | The row responds, and nothing happens on the canvas |
| 5 | Click on a window's title | Nothing happens on the canvas |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Typing stays with the canvas

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click a place on the canvas, then click Show Grid on the View toolbar, then type | The text keeps going to the canvas |
| 2 | Click a History row, then type | The text goes to the canvas |
| 3 | Open and close a menu, then type | The text goes to the canvas |
| 4 | Double-click a layer's name, type a new one and press Enter, then type again | The name changes, and the later typing goes to the canvas |

- [ ] Pass  - [ ] Fail. Notes:

### T4: The canvas keeps clear

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Open a small canvas (say 40 × 20) and look at where it sits | Centred in the space the windows and toolbars leave free, not in the whole window |
| 2 | View → Actual Size, then scroll to each corner of a big canvas | Every cell can be brought clear of the windows and toolbars |
| 3 | Window → close Layers and History, with a small canvas | It re-centres, using the space they left |
| 4 | Window → hide File Toolbar | The canvas doesn't move, because Edit is still along the top |
| 5 | Window → hide Edit Toolbar too | The canvas moves up a little, using the free band |

- [ ] Pass  - [ ] Fail. Notes:

### T5: The Window menu

**Also on macOS:** Yes, because menus are native there

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the Window menu | Layers, History, then File, Tools, Edit and View Toolbar, all check-marked, then Reset Layout |
| 2 | Click Layers | Layers closes, History grows to fill the side, and the check mark goes |
| 3 | Click Layers again | It's back above History, at the same size as before |
| 4 | Close History instead, then reopen it | It's back below Layers |
| 5 | Close both, then reopen History, then Layers | Layers is back above History |
| 6 | Hide each toolbar, then show it | It goes and comes back in the same place, and the check mark follows |
| 7 | Hide a few things, then click Reset Layout | Everything is back as in T1 |

- [ ] Pass  - [ ] Fail. Notes:

### T6: The layout is saved

**Also on macOS:** Yes, because closing a window works differently there

| # | Do this | Expected result |
|---|---|---|
| 1 | Close Layers and hide the View toolbar, then quit and start the app again | Both are still gone, with the check marks off |
| 2 | Reopen them | Each is back in its place |
| 3 | On a Mac only: hide something, close the window (the document closes and the app keeps running), then click the Dock icon | The new window has the same layout |

- [ ] Pass  - [ ] Fail. Notes:

### T7: A damaged settings file

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Quit the app | |
| 2 | Open the settings folder: `%APPDATA%\AskiiGraphiks Studio` on Windows | It contains `settings.json` |
| 3 | Open `settings.json`, replace everything with `not settings`, and save | |
| 4 | Start the app | A message says the settings were reset, and the default layout shows |
| 5 | Look in the settings folder | The damaged file is there with `damaged` in its name |

- [ ] Pass  - [ ] Fail. Notes:

### T8: Resizing the app window

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make the window as small as it goes | Nothing overlaps. Each toolbar stays on its anchor, or moves aside for a corner toolbar |
| 2 | Make it wide | The Tools toolbar is in the middle of the space between the left edge and the widgets |
| 3 | Resize it slowly while watching the widgets | They keep filling the side's height, and the toolbars stay level with them |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- **Very wide or very narrow windows,** dragged quickly back and forth.
- **A hand-edited layout:** in `settings.json`, change something in `dockingLayout`, such as putting two toolbars on one anchor or giving a window a silly width. The app should start normally and put it right.

## Known limitations (not bugs)

- **Nothing can be dragged or resized yet,** and there are no grips, right-click menus or hover buttons. 2.5c and 2.5e bring them.
- **A toolbar that doesn't fit shows no fade yet.** It scrolls with the wheel, as before. Fades come in 2.5c.
- **Widgets aren't frosted,** only translucent, as we decided.
- **Toolbars still have text buttons,** so they're long. Your icons will make them much shorter.
- **The old settings entry stays in `settings.json`** under `dockLayoutV2`. Nothing reads it.
