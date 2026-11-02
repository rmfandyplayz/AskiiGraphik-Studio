<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.4b: Remembered font files, manual test

**Build:** latest `master`
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 5 min

## What changed

A font you load from a file is still in the font picker after you restart the app, and documents using it open in that font. Before, it was forgotten when the app closed.

## Setup

1. Get a `.ttf` or `.otf` font file that isn't installed on your computer and isn't one of the fonts that comes with the app (JetBrains Mono, Roboto Mono, IBM Plex Mono, Fira Code). Put it in a folder where you can easily move it.
2. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).

## Tests

### T1: Remembered after a restart

**Also on macOS:** Yes, because file dialogs and settings folders differ there

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose Document → Font, click Load from File, pick your font and click OK. Type something | The text is drawn in your font |
| 2 | Save the document, then quit the app completely and start it again | The app starts normally |
| 3 | Choose Document → Font | Your font is listed, marked "(from a file)" |
| 4 | Cancel, then open the document you saved | It's drawn in your font, and there's no message about a missing font |
| 5 | Choose Document → Font and load the same file again | The picker says the font is already available. It's listed once |

- [ ] Pass  - [ ] Fail. Notes:

### T2: When the file isn't there

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Quit the app, move your font file to another folder, and start the app | The app starts normally |
| 2 | Choose Document → Font | Your font isn't listed |
| 3 | Cancel, then open the document you saved | You're told its font is missing, and it's drawn in JetBrains Mono |
| 4 | Quit, move the font file back, and start the app again. Open the document | It's drawn in your font again, with no message |
| 5 | Quit. Move your font file away again, and in its place put a text file renamed to the same name. Start the app | The app starts normally, and your font isn't listed. Help → Open Log Folder has a warning about that file in the newest log |
| 6 | Quit, delete the renamed text file and move your font file back | Tidied up. Your font is back at the next start |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Tests to revisit

| Step | Test | Where | Why |
|---|---|---|---|
| 2.4 | T6 row 3 | Either | Only if you come across a file saved before 2.4: it should open with the top layer highlighted. Automated tests cover files with no saved layer, so don't go out of your way |
| 2.1 | The retest list at the top of [its doc](step-2.1-dock-shell.md) | Nothing to do now | Moved into 2.5 at your request, and 2.1 stays open until then |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Loading several font files, then restarting.
- A font file on a USB drive or network folder that isn't connected when the app starts.
- Installing a font you'd loaded from a file, then restarting: the installed one takes over.

## Known limitations (not bugs)

- You can't remove a font from the picker, other than by moving or deleting its file.
- If you move a font file and load it from its new place, the old location stays in the settings and adds one harmless line to the log at each start.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2.4b, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
