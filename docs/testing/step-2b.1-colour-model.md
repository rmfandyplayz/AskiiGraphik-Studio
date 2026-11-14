<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2b.1: Colour model, manual test

**Build:** the commit that adds this file
**Test on:** GitHub for T1, then Windows (Linux is covered by CI only)
**Time needed:** about 10 min, most of it waiting for the run

## What changed

Behind the scenes, a document can now hold colour. It has a colour mode (monochrome, ANSI or RGB), and each cell can have a text colour and a background. Nothing in the app shows or sets colour yet; that's steps 2b.2 and 2b.3. Files are now saved as format version 2, and files saved before this step still open.

## Setup

1. Open https://github.com/rmfandyplayz/AskiiGraphik-Studio/actions in a browser.
2. For T2, the sample file is `.local/samples/version-1-file.asciiart` in the repo folder. It's a version 1 file (the format before this step), and it isn't in git.

## Tests

### T1: CI passes on every platform

This checks the new colour code on all three systems. The automated tests cover the colour rules, the mode switch and its undo, layering colours, Merge Down with colours, and saving and opening coloured files.

**Also on macOS:** No, CI covers it

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the run at the top of the Actions page, the one for this step's commit | All three jobs (windows, macos, ubuntu) have a green check mark, and the run shows **Success**. |
| 2 | Open the **ubuntu-latest** job | **Check formatting** has a check mark (a failure would be a red ✗). |

- [ ] Pass  - [ ] Fail. Notes:

### T2: An old file still opens and saves in the new format

This checks the upgrade from version 1 files, which is the one part of this step you can see in the app.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Run the app, then **File → Open** and choose `.local/samples/version-1-file.asciiart` | A 9 × 3 box opens with "old v1" inside it, and no error. The Layers window shows two layers, **Words** above **Box**. |
| 2 | Type a letter somewhere in the box, then **File → Save** | It saves with no message. |
| 3 | Open the file in Notepad | The first line now reads `ASKIIART 2`, and near the top there's a line `"colorMode": "monochrome",`. There's no `"foreground"` or `"background"` anywhere. |
| 4 | Close Notepad, and in the app use **File → Open** on the same file again | It opens with your letter in it. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Open a few of your own files saved before this step. Each should open looking exactly as it did.
- Typing, erasing, Merge Down and switching to ASCII only should all behave as before.

## Known limitations (not bugs)

- No colour shows anywhere yet: there's no way to set one, and the canvas doesn't draw them (2b.2, 2b.3).
- A file saved by this build won't open in an older build, which says the file is from a newer version. That's on purpose (ADR 0037).

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2b.1, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
