<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2b.4: ANSI export, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 25 min

## What changed

There's **File → Export as ANSI…** and **Edit → Copy as ANSI**. They work like Export as Text and Copy as Text, but they keep the colours, as the escape codes that terminals read. An ANSI document gets the 16-colour codes, and an RGB document gets 24-bit ones. There are two Debug samples sized for a terminal: **Debug → Open ANSI Test Sample** and **Open RGB Test Sample**.

## How a terminal shows the art

A terminal shows the colours when the file's contents are printed in it. Opening the `.ans` file by double-clicking it doesn't work: that opens a text editor, which shows the codes as odd characters such as `[31m`.

**On Windows**, use Windows Terminal, which comes with Windows 11:

1. Open the **Start** menu, type **Terminal**, and open it. It starts in PowerShell.
2. Make the window wider than the art if it isn't already (the samples are 70 columns wide).
3. To show a file in Downloads, paste this and press **Enter** (change the name if yours is different):
   ```
   Get-Content -Raw -Encoding UTF8 "$HOME\Downloads\ansi-sample.ans"
   ```
4. To show what Copy as ANSI put on the clipboard, paste this and press **Enter**:
   ```
   Get-Clipboard -Raw
   ```

**On macOS**, use Terminal:

1. Open **Terminal** (in Applications → Utilities, or search for it with Spotlight).
2. Make the window wider than the art if needed.
3. To show a file in Downloads, paste this and press **Return**:
   ```
   cat ~/Downloads/ansi-sample.ans
   ```
4. To show the clipboard, paste this and press **Return**:
   ```
   pbpaste
   ```

To clear the terminal between tries, type `clear` and press Enter. Pressing the up arrow brings back the last command.

**What to expect from the colours:**
- The 16 ANSI colours look as the terminal's own palette shows them, which isn't quite what the canvas shows. For example, Windows Terminal's blue is lighter. That's how ANSI colours work, not a bug.
- 24-bit colours (from an RGB document) should match the canvas. macOS Terminal shows them from macOS 26 (Tahoe); older versions show the nearest colours they have.
- Cells with no colour in the app show the terminal's own text colour and background, so they follow the terminal's theme, light or dark.
- The **Black** name in the sample's text rows is hard to see on a dark terminal. That's expected.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).

## Tests

### T1: The menu items

This checks the two new commands are where they should be.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **File** menu | **Export as ANSI…** is just below **Export as Text…**. |
| 2 | Open the **Edit** menu | **Copy as ANSI** is just below **Copy as Text**, with no shortcut shown beside it. |
| 3 | Open the **Debug** menu | **Open ANSI Test Sample** and **Open RGB Test Sample** are below **Open Color Sample**. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Exporting a 16-colour document

This checks the 16-colour codes in a terminal.

**Also on macOS:** Yes, because it's a different terminal

| # | Do this | Expected result |
|---|---|---|
| 1 | **Debug → Open ANSI Test Sample** (choose **Don't Save** if it asks) | A small document with colour names, two rows of coloured blocks, and a few test lines. The title bar says Untitled. |
| 2 | **File → Export as ANSI…** | A save dialog titled **Export as ANSI**, with the file type **ANSI art**, suggesting the name **Untitled**. |
| 3 | Go to your **Downloads** folder, name the file `ansi-sample`, and save | It's saved. The window title doesn't change. |
| 4 | Show the file in the terminal, as described above | The art appears in colour. Each colour name is in its own colour, with the bright ones brighter. The two rows of blocks show 16 different colours, the second row with numbers 0 to 15 on them. |
| 5 | Look at the test lines | "the terminal's own text colour" is in the terminal's normal text colour on blue. "bright cyan" is cyan on the terminal's own background. "bright yellow on red" is that. "Only a background" has a magenta block. The box characters join up, in yellow on blue. |
| 6 | Look to the right of the art, and at the next prompt | No colour runs on past the art, and the prompt below it is in the terminal's normal colours. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Exporting a 24-bit document

This checks the 24-bit codes.

**Also on macOS:** Yes

| # | Do this | Expected result |
|---|---|---|
| 1 | **Debug → Open RGB Test Sample** | The same sample, with two more rows at the bottom: text and backgrounds going round the colour wheel. |
| 2 | **File → Export as ANSI…**, and save it in Downloads as `rgb-sample` | Saved. |
| 3 | Show it in the terminal (use `rgb-sample.ans` in the command) | The same art. The colours match the canvas's this time, and the two bottom rows are smooth rainbows. (On a Mac older than macOS 26, they may look banded.) |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Copy as ANSI

This checks the clipboard.

**Also on macOS:** Yes, because the clipboard command is different

| # | Do this | Expected result |
|---|---|---|
| 1 | With the RGB sample still open, **Edit → Copy as ANSI** | Nothing visible happens. |
| 2 | In the terminal, show the clipboard as described above | The same art in colour as in T3. |
| 3 | In the app, **Edit → Copy as Text**, and show the clipboard again | The same art with no colours at all. |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Your own art, layers and monochrome

This checks that the export follows what the canvas shows.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | **File → New**, choose **ANSI (the 16 standard colors)**, and click **OK**. Press **T** if the Text tool isn't chosen, and type a word. Press **Esc** and **B**, and paint the word's text and its background with the Brush, choosing two colours in **Window → Swatches** | The word in colour on the canvas. |
| 2 | Add a layer, and paint a background on it across part of the word | That part's background changes. |
| 3 | Export as ANSI to Downloads as `my-art`, and show it in the terminal | It looks like the canvas: the word, its colours, and the top layer's background over part of it. |
| 4 | Hide the top layer, export again over the same file (say yes to replacing it), and show it | The top layer's background has gone, as on the canvas. |
| 5 | **Document → Monochrome**, and confirm. Export as ANSI again over the same file, and show it | The word with no colours, like Export as Text. |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Tests to revisit

| Step | Test | Where | Why |
|---|---|---|---|
| 2b.3b follow-ups | T1 (menu bar after an Alt+click) | Windows | Not reported yet |
| 2b.3b follow-ups | T2 (Esc while picking) | Windows | Not reported yet |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Export the big Debug → Open Color Sample (500 × 500): it should be written quickly, even though it's far too wide for a terminal.
- Paste what Copy as ANSI copied into a text editor such as Notepad: the codes show as odd characters, which is expected.

## Known limitations (not bugs)

- No options yet, such as line breaks or trimming the spaces at the ends of rows.
- These `.ans` files are UTF-8, for terminals. Old ANSI art viewers expect another format, which M7's classic export adds.
- Pasting Copy as ANSI at a terminal's prompt doesn't show colours: the shell takes it as typing. Use the clipboard commands above.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2b.4, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
