<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 0.3: App skeleton, manual test

**Build:** working tree
**Test on:** - [ ] Windows  - [ ] macOS  (Linux is covered by CI only)
**Time needed:** about 15 min per OS

## What changed

The app now opens a window titled "AskiiGraphiks Studio" with a placeholder menu: Quit, Open Log Folder and About. Quit has a shortcut (Ctrl+Q on Windows, Cmd+Q on Mac). The app writes a log file you can open from the Help menu. If something goes wrong unexpectedly, the app shows an error dialog instead of crashing. Debug builds have a Debug menu that triggers a test error.

## Setup

1. In Rider, open `AskiiArt.slnx`, pick the **AskiiArt.App** run configuration with the **Debug** configuration, and click Run. (From a terminal: `dotnet run --project src/AskiiArt.App`.)
2. Where the log folder is:
   - Windows: `%LOCALAPPDATA%\AskiiGraphiks Studio\Logs` (paste this into File Explorer's address bar)
   - macOS: `~/Library/Application Support/AskiiGraphiks Studio/Logs` (Finder → Go → Go to Folder…)

## Tests

### T1: The window and its menus (Windows)

| # | Do this | Expected result |
|---|---|---|
| 1 | Start the app | An empty window opens. Its title bar reads **AskiiGraphiks Studio**. A menu bar at the top of the window shows **File**, **Help** and **Debug**. |
| 2 | Click **File** | One item: **Quit**, with **Ctrl+Q** shown next to it. |
| 3 | Click **Help** | **Open Log Folder**, then a separator line, then **About**. |
| 4 | Click **Debug** | One item: **Throw Test Exception**. |
| 5 | Press **Alt**, then **F** | The File menu opens (the F in File is the access key). Press Esc to close it. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: The window and its menus (macOS)

| # | Do this | Expected result |
|---|---|---|
| 1 | Start the app | An empty window opens, titled **AskiiGraphiks Studio**. There's **no** menu bar inside the window. |
| 2 | Look at the menu bar at the top of the screen | Next to the Apple menu is the application menu, followed by **Help** and **Debug**. (When started from Rider, the application menu may be named "AskiiGraphiks Studio" or "AskiiArt.App"; both are fine for now.) |
| 3 | Open the application menu | **About AskiiGraphiks Studio** comes first, then the standard Mac items: Services, Hide…, Hide Others, Show All, and **Quit** with **⌘Q**. |
| 4 | Open **Help** | One item: **Open Log Folder**. |
| 5 | Open **Debug** | One item: **Throw Test Exception**. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Quit

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **Ctrl+Q** (Windows) or **⌘Q** (Mac) | The app closes right away. |
| 2 | Start it again. Windows: **File → Quit**. Mac: application menu → **Quit** | The app closes. |
| 3 | Start it again and close the window with its close button | The app closes. |

- [ ] Pass  - [ ] Fail. Notes:

### T4: About

| # | Do this | Expected result |
|---|---|---|
| 1 | Windows: **Help → About**. Mac: application menu → **About AskiiGraphiks Studio** | A small dialog appears over the main window. It shows **AskiiGraphiks Studio** in larger text, **Version 0.1.0 (xxxxxxx)** (seven letters/digits from the git commit), **Licensed under GPL-3.0-or-later** in grey, and an **OK** button. |
| 2 | Try clicking the main window behind it | Nothing happens; the dialog stays in front until it's closed. |
| 3 | Click **OK** | The dialog closes. |
| 4 | Open About again and press **Esc**; open it once more and press **Enter** | Each time, the dialog closes. |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Open Log Folder and the log file

| # | Do this | Expected result |
|---|---|---|
| 1 | **Help → Open Log Folder** | File Explorer (Windows) or Finder (Mac) opens the log folder from Setup step 2. It contains a file named like `log-20261030.txt` (today's date). |
| 2 | Open that file in a text editor | Each line starts with a date and time. One of the latest lines says `[INF] AskiiArt.App.Startup: Starting version 0.1.0+…` followed by your OS and .NET version. |
| 3 | Quit the app, then open the log file again | The last line now says `Exiting with code 0`. |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Unexpected-error dialog

| # | Do this | Expected result |
|---|---|---|
| 1 | **Debug → Throw Test Exception** | A dialog titled **Unexpected error** appears: "Something went wrong. The details were written to the log." It has three buttons: **Open Log Folder**, **Quit** and **Continue**. |
| 2 | Click **Open Log Folder** | The log folder opens, and the error dialog **stays open**. |
| 3 | In the log file, look at the latest lines | A line with `[ERR]` and `Unhandled exception on the UI thread`, followed by `System.InvalidOperationException: Test exception raised from the Debug menu.` and a stack trace. |
| 4 | Back in the app, click **Continue** | The dialog closes and the app keeps running normally (the menus still work). |
| 5 | Throw the test exception again and close the dialog with its title-bar close button | Same as Continue: the app keeps running. |
| 6 | Throw it once more and click **Quit** | The app closes. |

- [ ] Pass  - [ ] Fail. Notes:

### T7: Release builds have no Debug menu

| # | Do this | Expected result |
|---|---|---|
| 1 | In Rider, switch the build configuration from **Debug** to **Release** and run AskiiArt.App. (Terminal: `dotnet run -c Release --project src/AskiiArt.App`.) | The app opens as before, but there's **no Debug** menu. Everything else from T1/T2 is the same. |
| 2 | Switch Rider back to **Debug** afterwards | |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Switch the OS between light and dark mode while the app is open, then open About. The grey license line should be readable in both.
- Windows: try Ctrl+Q while a menu is open, or while the About dialog is open.
- Mac: check that ⌘Q quits exactly once and nothing odd happens (no second dialog, no error in the log).

## Known limitations (not bugs)

- All of the UI is placeholder: no app icon, an empty window, plain dialogs. The real About / credits screen comes in M9.
- On the Mac, the standard application-menu items (Hide, Show All, Quit…) come from Avalonia and are always in English.
- Avalonia's own window icon may appear in the taskbar or Dock. The app icon comes in M9.
- The window's starting size and position are whatever the OS picks.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 0.3, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <Help → Open Log Folder, then paste the last lines of today's log file>
```
