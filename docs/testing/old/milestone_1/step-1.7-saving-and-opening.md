<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 1.7: Saving and opening, manual test

**Build:** working tree (not pushed yet)
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 40 min

## What changed

You can save your art (**File → Save**, **Save As**) and open it again (**File → Open**). There are two kinds of file: `.asciiart`, which is readable in a text editor, and `.asciiartz`, which is compressed and smaller. Before New, Open or closing the window throws away unsaved changes, the app asks whether to save them. The window title shows the file's name, with a `*` while there are unsaved changes.

The app now starts with an empty 80 × 25 document; the old 500 × 500 sample is under **Debug → Open Sample Document**. The fixes from your 1.6 testing are in T7.

Shortcuts below say **Ctrl**. On a Mac, use **Cmd**.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Make an empty test folder, for example `Documents\AskiiTest`.

## Tests

### T1: Starting up and the title

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Start the app | An empty canvas. The status bar says `80 × 25 cells` and `ASCII only`. The title is `Untitled - AskiiGraphiks Studio`. |
| 2 | Click a cell and type `hi` | The title becomes `*Untitled - AskiiGraphiks Studio`. |
| 3 | Press Ctrl+Z until nothing is left to undo | The `*` goes away: the document is back to how it started. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Save and Save As

**Also on macOS:** Yes, because the save dialog and its file types are the operating system's own.

| # | Do this | Expected result |
|---|---|---|
| 1 | Ctrl+N, choose **Unicode**, 20 × 5, OK. Type `/\_/\ café` on the first row | The title is `*Untitled - …`. |
| 2 | Press **Ctrl+S** | The save dialog opens (there's no file yet), suggesting `Untitled`, with the type "AskiiGraphiks document". |
| 3 | Save it as `cat` in your test folder | The title becomes `cat.asciiart - AskiiGraphiks Studio`, with no `*`. |
| 4 | Open `cat.asciiart` in Notepad or TextEdit | The first line is `ASKIIART 1`. Further down, the rows of the canvas are visible as text, one per line, in quotes. Backslashes show doubled (`/\\_/\\`); see the limitations. |
| 5 | Back in the app, type `!` somewhere, then Ctrl+S | No dialog this time. The `*` appears after typing and goes away after saving. |
| 6 | **File → Save As…** (Ctrl+Shift+S), pick the type "AskiiGraphiks document, compressed", name `cat` | The file is `cat.asciiartz`, and the title says so. In a text editor it's unreadable binary. It's much smaller than `cat.asciiart`. |
| 7 | Save As again, keep the plain type selected, but type the name `cat2.asciiartz` | It's saved compressed: a name ending in one of the two extensions wins. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Open

**Also on macOS:** Yes, because the open dialog is the operating system's own.

| # | Do this | Expected result |
|---|---|---|
| 1 | Ctrl+N (Don't Save if asked), then **Ctrl+O** and pick `cat.asciiart` | The art comes back exactly, Unicode, 20 × 5. The title is `cat.asciiart - …` with no `*`, and Undo is greyed out. |
| 2 | Ctrl+O and pick `cat.asciiartz` | It opens the same way. |
| 3 | In the file manager, copy `cat.asciiartz` and rename the copy `copy.asciiart`. Open it | It still opens: the app reads what's inside the file, not its name. |
| 4 | Ctrl+O, choose "All files" and pick a `.txt` file or a picture | A message says it isn't a document this app can open. The document on screen doesn't change. |
| 5 | In a text editor, delete a few characters from one row of `cat.asciiart` (inside the quotes) and save. Open it in the app | A message says the file is damaged. Nothing else changes. |
| 6 | In a text editor, change the first line of a copy to `ASKIIART 2` and open it | A message says it was saved by a newer version (file format 2). |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Unsaved changes

**Also on macOS:** Yes, because closing the window and quitting work differently there.

| # | Do this | Expected result |
|---|---|---|
| 1 | Open `cat.asciiart`, type something, then Ctrl+N | A dialog asks "Do you want to save the changes to “cat.asciiart”?" with Save, Don't Save and Cancel. |
| 2 | Click **Cancel** (or press Esc) | Nothing happens, and the New dialog doesn't open. |
| 3 | Ctrl+N again, **Don't Save**, then OK in the New dialog | A new empty document. `cat.asciiart` keeps its old contents. |
| 4 | Type something, Ctrl+O, **Save** | The save dialog appears first (the document is untitled). After you save, the open dialog appears. |
| 5 | Type something, close the window with its close button, **Cancel** | The app stays open with your work. |
| 6 | Close it again, **Don't Save** | The app quits. |
| 7 | Start again, type, press **Ctrl+Q** (on a Mac, Cmd+Q or AskiiGraphiks Studio → Quit), **Save**, save the file | The file is saved, then the app quits. |
| 8 | Start again, and without typing, press Ctrl+N, then close the window | Nothing asks: there's nothing unsaved. |
| 9 | Type, Ctrl+N, and press **Enter** in the question | Enter means Save. |

- [ ] Pass  - [ ] Fail. Notes:

### T5: A document whose font is missing

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | In a text editor, change `"font": "JetBrains Mono"` in `cat.asciiart` to `"font": "No Such Font"` and save. Open it in the app | A message says the font "No Such Font" isn't available and the document is shown in JetBrains Mono. The status bar says `No Such Font (not available)`. |
| 2 | Type something, Ctrl+S, and look at the file in a text editor | It still says `"font": "No Such Font"`. |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Saving safely

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Type something and Save As into a folder you can't write to, such as `C:\Windows` (on a Mac, `/System`) | The dialog may refuse. If it doesn't, a message says the file couldn't be saved and why. The title keeps its `*`. |
| 2 | Save over `cat.asciiart` a few times, then look in the test folder | Only your own files are there: no `.tmp` files are left behind. |

- [ ] Pass  - [ ] Fail. Notes:

### T7: Fixes from step 1.6

**Also on macOS:** No. The Mac's jump keys didn't change.

| # | Do this | Expected result |
|---|---|---|
| 1 | Copy one of the bundled fonts from `src/AskiiArt.Imaging/Fonts/Bundled/` to your test folder. Open the copy in Notepad, delete a big chunk from the middle and save. In the app, use **Document → Font… → Load from File…** to load the copy | Either a message says it couldn't be loaded, or the font loads and draws. The app never crashes or shows the error dialog. |
| 2 | Click a cell, press **Ctrl+Shift+Right** | The caret jumps 10 cells. Ctrl+Shift with the other arrows works too. |
| 3 | Press **Alt+Right** | Nothing happens: Alt isn't used for moving the caret any more. |
| 4 | Ctrl+N, type `99999` in Width, press Tab | The box shows 4096. |
| 5 | Type `0` in Width, press Tab | The box shows 1. |

- [ ] Pass  - [ ] Fail. Notes:

### T8: The sample document (Debug builds only)

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Type something, then **Debug → Open Sample Document** | It asks about unsaved changes first. After Don't Save, the busy 500 × 500 sample appears. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Save and reopen a 4096 × 4096 document in both kinds. Saving may pause briefly; it should never fail.
- Type quickly while a large document is saving. Afterwards the title should still show `*`, because that typing isn't in the file.
- Box drawing, accents and Chinese characters in a Unicode document. They should come back exactly, and show as themselves (not `\u` codes) in the `.asciiart` file.
- Open a file that is read-only, then Save. A message should explain why it couldn't be saved.
- Save a document, undo past the save, then redo back to it. The `*` should go away again.

## Known limitations (not bugs)

- In the `.asciiart` file, backslashes and quotes are written as `\\` and `\"`, as JSON requires, so art that uses them looks shifted in a text editor. Export .txt (1.8) will give clean text.
- On a Mac, closing the window still quits the app (after asking about unsaved changes). Keeping the app running comes in 1.8, along with Copy as Text and Export .txt.
- The "save the changes?" buttons are in the same order on every OS.
- There are no recent files or autosave yet (M9), and double-clicking a file doesn't open the app (M9).
- Files don't remember the zoom or scroll position.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 1.7, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
