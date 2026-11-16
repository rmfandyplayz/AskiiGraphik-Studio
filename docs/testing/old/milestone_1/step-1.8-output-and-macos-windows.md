<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 1.8: Output and macOS windows, manual test

**Build:** working tree (not pushed yet)
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 30 min

## What changed

You can copy your art to the clipboard as plain text (**Edit → Copy as Text**) and save it as a `.txt` file (**File → Export as Text…**). On a Mac, closing the window no longer quits the app: the Dock icon opens a new window, and the close button shows a dot while there are unsaved changes.

Also fixed from your 1.7 testing: on Windows, the open dialog now appears after you save from the "save the changes?" question (T5), and a kind of damaged file that crashed a test on the Mac CI now opens with the usual "damaged" message.

Shortcuts below say **Ctrl**. On a Mac, use **Cmd**.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Use the test folder from 1.7, for example `Documents\AskiiTest`.

## Tests

### T1: Copy as Text

**Also on macOS:** Yes, because the clipboard's line breaks differ there.

| # | Do this | Expected result |
|---|---|---|
| 1 | Ctrl+N, choose **Unicode**, 12 × 3, OK. Click the top-left cell and type ` /\_/\` (starting with a space), Enter, `( o.o )`, Enter, ` > é <` | A cat face over three rows. |
| 2 | **Edit → Copy as Text**. Open Notepad (on a Mac, TextEdit with **Format → Make Plain Text**) and paste | The same three rows, lined up as on the canvas. Backslashes appear once each, not doubled. |
| 3 | In Notepad, press End on each pasted line | Every line is 12 characters wide: the empty cells came across as spaces. |
| 4 | Look below the last pasted line | There's no extra empty line after ` > é <`. |
| 5 | Click the canvas, press **Ctrl+Shift+C**, and paste again | The same text again: the shortcut works. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Export as Text

**Also on macOS:** Yes, because the save dialog is the operating system's own.

| # | Do this | Expected result |
|---|---|---|
| 1 | With the cat from T1 (not saved yet), **File → Export as Text…** | A save dialog suggests `Untitled`, with the type "Text file". |
| 2 | Save it as `cat` in your test folder | `cat.txt` appears. The title still says `*Untitled - …`: exporting isn't saving. |
| 3 | Open `cat.txt` in Notepad or TextEdit | The same three rows as in T1, each 12 characters wide, backslashes not doubled. After the last row there's a line break, so the cursor can go to an empty fourth line. |
| 4 | Press **Ctrl+S** | The Save As dialog opens, suggesting `Untitled`: the export didn't become the document's file. Save it as `cat` (`cat.asciiart`). |
| 5 | **File → Export as Text…** again | It suggests `cat`, in the test folder. |
| 6 | Save over `cat.txt` | The dialog asks whether to replace it. After yes, the file is replaced. |
| 7 | Export as Text into a folder you can't write to (`C:\Windows`; on a Mac, `/System`) | The dialog may refuse. If it doesn't, a message says the file couldn't be written and why. Nothing else changes. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Windows on a Mac

**Also on macOS:** Yes. This test is for the Mac only.

| # | Do this | Expected result |
|---|---|---|
| 1 | Start the app and type something | A dot appears in the red close button, and the title has its `*`. |
| 2 | Press **Cmd+S** and save the file anywhere | The dot goes away. |
| 3 | Type again, then click the red close button, **Cancel** | The window stays, with your work. |
| 4 | Click the close button again, **Don't Save** | The window closes, but the app keeps running: its name is still in the menu bar, and its Dock icon still has the dot underneath. |
| 5 | Click the app's Dock icon | A window opens with an empty, untitled 80 × 25 document. The title has no `*`. |
| 6 | **File → Close** (or Cmd+W) | The window closes without asking (nothing was typed). |
| 7 | Right-click the Dock icon and choose **New…** | A window opens, then the New dialog. Cancel it: the window stays, with an empty document. |
| 8 | Close the window. Right-click the Dock icon, choose **Open…**, and pick the file from row 2 | A window opens, then the open dialog, then the file. |
| 9 | With a window open, click the Dock icon | Nothing new opens; the same window comes forward. |
| 10 | Type something, press **Cmd+Q**, **Don't Save** | The app quits completely: its Dock icon loses the dot. |
| 11 | Start again, type, **Cmd+Q**, **Save**, and save the file | The file is saved, then the app quits. |
| 12 | Start again, close the window, then press Cmd+Q | The app quits. |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Closing the window on Windows

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Start the app, and without typing, close the window | The app quits, as before. |
| 2 | Start again and press **Ctrl+W** | Nothing happens: Close is a Mac-only item. |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Fixes from step 1.7

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Start the app, type something, press **Ctrl+O**, choose **Save**, and save the file | After the save dialog closes, the open dialog appears (this is the 1.7 T4 row 4 that failed). |
| 2 | Type something, press **Ctrl+N**, choose **Save** | The file saves without a dialog (it has a name now), then the New dialog appears. |
| 3 | **File → New…**, Don't Save if asked, OK. Type something, Ctrl+O, **Save**, save as `other` | The open dialog appears after the save dialog, as in row 1. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Copy as Text and Export as Text on a 4096 × 4096 document. Either may pause briefly; neither should fail.
- Paste copied art into a Discord code block (between three backticks), where it should keep its shape.
- Box drawing and accents in a Unicode document: they should copy and export as themselves.
- On a Mac, open several dialogs from the Dock menu in a row, with and without a window open.

## Known limitations (not bugs)

- Export as Text has no options yet (Windows line breaks, trimming trailing spaces). They come with the other export formats (M7).
- On a Mac with no window open, Cmd+N and Cmd+O do nothing: the File menu belongs to the window. Use the Dock icon or its menu.
- On a Mac, the title keeps its `*` as well as the dot, until you design the title.
- On a Mac, the save dialog adds the chosen type's extension itself, so typing `cat.asciiartz` with the plain type chosen saves `cat.asciiartz.asciiart`. Choose the type in the dialog instead (1.7 T2 row 7).

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 1.8, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
