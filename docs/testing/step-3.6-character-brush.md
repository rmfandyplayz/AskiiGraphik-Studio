<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 3.6: The Character Brush and the Characters window, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 40 min

## What changed

There's a fourth brush, the **Character Brush** (**B** then **4**). It paints one character you choose into every cell under the tip, in the current colours. **Alt+click** (Option+click on a Mac) picks a character and its colours up from the canvas.

You choose the character in the new **Characters** window (**Window → Characters**, or click the character box in Tool Options). Its dropdown lists:

- **Recent**: the characters you painted with last, newest first
- **your own character sets**, which you make by typing characters and clicking **Add**, then rename and edit as you type. You can also **Duplicate**, **Delete**, **Import…** and **Export…** them as `.asciicharset` files
- **built-in sets** such as Basic Latin, Box Drawing and Braille, which can't be changed, only duplicated

Clicking a character chooses it and switches to the Character Brush. Your sets, Recent and the chosen character are remembered between runs.

## Setup

1. Run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**. Under **Characters** choose **Unicode (any character)**, and under **Colors** choose **ANSI (the 16 standard colors)**. Click **OK**, and choose **Don't Save** if it asks.
3. Choose **View → Zoom In** two or three times, so each character is easy to read.
4. Open **Window → History**, so you can see each undo step.
5. If you've run this build before, open **Window → Characters** and delete the sets you made (choose each one and click **Delete**), so the names below match. Recent can't be emptied, so it may already list characters; that's fine.

## Known limitations (not bugs)

- The Characters window's layout, its text buttons and the wording are placeholders. You plan to turn it and the Ramps window into pop-ups later.
- There's no search by name yet.
- The window shows characters in a common monospaced font, not the document's, so they can look a little different from the canvas.
- A built-in set only shows the characters a font on this computer can draw, so it can show fewer characters on another computer.
- Set edits aren't on Undo, and Delete can't be undone (it asks first).
- Your sets and Recent are saved as soon as they change, but the chosen character is saved only when the app closes normally. **Rider's Stop button skips that**, so close the app with its own close button (or **File → Quit**, or Cmd+Q on a Mac) in T10.

## Tests

### T1: The Characters window

Checks that the window opens from the Window menu, lists Recent and the built-in sets, and shows a built-in set without letting you change it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Window** menu | **Characters** is listed after **Ramps**, with no check mark |
| 2 | Choose **Characters** | The Characters window opens. **Window → Characters** now has a check mark |
| 3 | Look at the top of the window | Four buttons: **Duplicate**, **Delete** (greyed out), **Import…**, **Export…**. Below them, a dropdown showing **Basic Latin** (or **Recent**, if you painted with the Character Brush in an earlier run) |
| 4 | Open the dropdown | **Recent** first, then the built-in sets: **Basic Latin**, **Latin-1 Supplement**, **Latin Extended-A**, **Greek**, **Cyrillic**, … down to **Braille** and **Legacy Computing**. Close it again and choose **Basic Latin** |
| 5 | Look below the dropdown | A grey note: "Built-in sets can't be changed. Duplicate one to edit a copy." Then a grid of square buttons with every printable ASCII character from `!` to `~` (94 of them), wrapping to the window's width. `#` is highlighted blue, because it's the Character Brush's character |
| 6 | Hover over the `A` button | A tooltip says `U+0041` |
| 7 | Look at the bottom of the window | A line, then "New set from these characters", a text box and an **Add** button (greyed out). There's no Name or Characters box, since a built-in set can't be edited |
| 8 | Choose **Box Drawing** in the dropdown | The grid shows the box-drawing lines (`─`, `│`, `┌`, `┼`, …). No character is highlighted |

### T2: Choosing a character and painting

Checks that clicking a character switches to the Character Brush, and that the brush paints hard-edged strokes that undo in one step.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Basic Latin** and click `A` | `A` is highlighted blue instead of `#`. On the Tools toolbar the **Brush** group is chosen, and its sub-toolbar shows four buttons, **Brush**, **FG Brush**, **BG Brush** and **Character Brush**, with **Character Brush** pressed |
| 2 | Look at Tool Options | **Size**, **Shape** and **Proportions**, then **Character** with a small button showing `A` |
| 3 | Set **Size** to 3 and click once on the canvas | A small round patch of `A`s, all the same: no lighter edge. One new History step, **Paint** |
| 4 | Drag quickly across the canvas in a long curve | A gap-free band of `A`s along the path. One more **Paint** step, however long the drag |
| 5 | Press **Ctrl+Z** (Cmd+Z on a Mac) | The whole curve disappears at once. The patch from row 3 stays |
| 6 | Start another drag, and press **Esc** before letting go | The stroke disappears as soon as you press Esc, and no History step is added |
| 7 | Press **]** twice, then **[** once | **Size** goes 3 → 5 → 4 |

### T3: The character box and key 4

Checks that the Character Brush has its own key, and that the box in Tool Options opens the Characters window.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **B** then **1** | The ramp **Brush** is chosen, and Tool Options shows its options |
| 2 | Press **4** | The **Character Brush** is chosen again, with `A` in its box |
| 3 | Close the Characters window (its **×** or **Window → Characters**) | It closes |
| 4 | Click the `A` box in Tool Options | The Characters window opens again |
| 5 | Click the box again while the window is open | The window stays open (it doesn't close) |
| 6 | Drag the Characters window into a dock beside another window so they share a tab strip, and click the other window's tab. Then click the `A` box | The Characters tab is brought to the front |

### T4: Alt+click picks a character and its colours

Checks that Alt+click (Option+click on a Mac) takes the character and both colours from a cell, and picks nothing from an empty cell.

**Also on macOS:** Yes, because the modifier is Option there

| # | Do this | Expected result |
|---|---|---|
| 1 | Open **Window → Swatches**. Choose a red text colour and a blue background. In the Characters window click `#` (Basic Latin) and paint a short line | Red `#`s on blue |
| 2 | Choose a green text colour and a yellow background, and click `@` in the Characters window | The box shows `@`. The colour squares show green and yellow |
| 3 | Hold **Alt** (Option) and click one of the red `#`s | The box shows `#`, the colour squares show red and blue, and `#` is highlighted in the Characters window. Nothing is painted and no History step is added |
| 4 | Alt+click (Option+click) an empty cell | Nothing changes: still `#`, red and blue |
| 5 | Press **Esc** in the middle of an Alt+drag across several cells | The character and colours go back to what they were before the Alt+click |

### T5: Recent

Checks that Recent lists the characters you painted with, newest first, and only those.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Recent** in the dropdown | A grey note: "Recent can't be changed. Duplicate it to keep these characters as a set." The grid shows `#` then `A` (and anything painted in earlier runs after them). `#` is highlighted |
| 2 | Choose **Basic Latin**, click `Q`, then choose **Recent** again | `Q` isn't listed: choosing a character doesn't make it recent until you paint with it |
| 3 | Paint once with `Q`, keeping the window on **Recent** | `Q` appears at the front straight away |
| 4 | Click `A` in Recent and paint with it | `A` moves to the front, and isn't listed twice |

### T6: Your own sets

Checks that typed characters make a new set, that it's edited as you type, and that names and characters that can't be used are explained.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | In the box at the bottom, type `/ | - _ /` (with the spaces) | **Add** turns on |
| 2 | Click **Add** | The box empties. The dropdown shows **New set**, chosen, with the grid `/`, `|`, `-`, `_`: the spaces only separate them and the repeated `/` counts once. Below the grid are a **Name** box (`New set`) and a **Characters** box (`/|-_`). **Delete** turns on, and there's no grey note |
| 3 | Open the dropdown | **Recent**, then **New set**, then **Basic Latin** and the other built-in sets |
| 4 | Change **Name** to `Lines` | The dropdown's entry becomes **Lines** as you type |
| 5 | Type `+` at the end of **Characters** | `+` appears at the end of the grid straight away |
| 6 | Change **Name** to `recent` | Orange text below the box: "Another set already has this name." The dropdown still says **Lines** |
| 7 | Change **Name** to `Box drawing`, then empty it | First the same message (names ignore case), then "Give the set a name." Put the name back to `Lines`: the message goes |
| 8 | Empty **Characters** | "Type at least one character." The grid still shows `/|-_+`. Type `/|-_+` back in |
| 9 | Type `x` in the bottom box and click **Add**, then type `y` and click **Add** | Two more sets, **New set** and **New set 2**, each chosen as it's added |

### T7: Duplicate and Delete

Checks that Duplicate gives an editable copy of what a set shows, and that Delete asks first.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Box Drawing** and click **Duplicate** | **Box Drawing copy** is added and chosen, with the same characters, and its Name and Characters boxes can be edited |
| 2 | Choose **Recent** and click **Duplicate** | **Recent copy** is added and chosen, with Recent's characters in the same order |
| 3 | Choose **New set** and click **Delete** | A dialog titled **Delete Character Set**: "Delete the set “New set”? This can't be undone.", with a **Delete** button |
| 4 | Cancel it | **New set** is still there |
| 5 | Click **Delete** again and confirm | **New set** is gone, and **New set 2**, which took its place, is chosen |
| 6 | Open the dropdown | **Recent**, **Lines**, **New set 2**, **Box Drawing copy**, **Recent copy**, then the built-in sets |

### T8: An ASCII-only document

Checks that characters an ASCII-only document can't hold are greyed out, warned about and never painted.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Blocks and Shades** and click the full block `█` | The box in Tool Options shows `█` |
| 2 | Choose **File → New**, choose **ASCII only** under **Characters**, and click **OK** (choose **Don't Save** if it asks) | Tool Options shows a warning: "This character needs a Unicode document". Every character in the grid is greyed out, with the note "Greyed-out characters can't go in this document, which is ASCII only." |
| 3 | Click a greyed-out character | Nothing happens |
| 4 | Click and drag on the canvas | Nothing is painted and no History step is added |
| 5 | Choose **Lines** | Nothing is greyed out and the note is gone |
| 6 | Click `+` and paint | The warning goes, and `+`s are painted |

### T9: Export and import

Checks that a set can be saved as a file and read back, and that files that can't be read are reported.

**Also on macOS:** Yes, because the file pickers are the system's own

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Lines** and click **Export…** | A save dialog titled **Export Character Set**, with the name `Lines` and the type **Character set files (.asciicharset)** |
| 2 | Save it on the Desktop | No message. `Lines.asciicharset` is on the Desktop |
| 3 | Open the file in a text editor | Readable text with `"format": "askiiart.charset"`, `"version": 1`, `"name": "Lines"` and `"characters": "/|-_+"` |
| 4 | Change `"version": 1` to `"version": 99` and save it as `future.asciicharset` on the Desktop, leaving the first file as it was | Two character set files on the Desktop |
| 5 | Click **Import…** | An open dialog titled **Import Character Sets**, showing character set files. The other file type to choose is all files |
| 6 | Pick both files at once and open them | **Lines 2** is added and chosen, with `/|-_+`. A message titled **Import Character Sets** says these files couldn't be imported: "future.asciicharset: it's from a newer version of the app" |
| 7 | Click **Import…**, then cancel | Nothing changes |

### T10: Remembered between runs

Checks that your sets, Recent and the chosen character come back when the app is opened again.

**Also on macOS:** Yes, because settings live in a different folder there and quitting works differently

| # | Do this | Expected result |
|---|---|---|
| 1 | Note the order of the dropdown and of Recent. Choose **Basic Latin** and click `@` | The box shows `@` |
| 2 | Close the app with its own close button (on a Mac, **Cmd+Q**). Choose **Don't Save** if it asks | The app closes |
| 3 | Run the app again and press **B** | The **Character Brush** comes back, the brush you used last, with `@` in its box |
| 4 | Open **Window → Characters** if it isn't open, and open the dropdown | Your sets are there, in the same order |
| 5 | Choose **Recent** | The same characters, in the same order, as before closing |

### T11: Tests to revisit

The Ramp dropdown was fixed after step 3.5's testing, and one row there was changed. These rows check them. Follow step 3.5's test doc for each, starting from its setup.

| Step | Test | Where | Why |
|---|---|---|---|
| 3.5 | T4, row 5 | Windows | fixed after testing: the Ramp dropdown went blank when the chosen ramp was renamed |
| 3.5 | T8, row 4 | Windows | fixed after testing: the Ramp dropdown went blank when the chosen ramp was deleted |
| 3.5 | T10, row 4a | Windows | changed after testing: the colours only show once a document has colours |

## Worth poking at (optional)

### P1: A set no font can draw

**Also on macOS:** Yes, because each system has different fonts

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Legacy Computing**, then **Braille** | Each shows only the characters some font on this computer can draw. If none can, the grid is empty with the note "No font on this computer can draw these characters.", and **Duplicate** and **Export…** are greyed out |

### P2: Recent keeps 16

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Paint once each with 17 different characters, then choose **Recent** | 16 characters, newest first. The oldest is gone |

### P3: Files that aren't character sets

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click **Import…**, switch the type to all files, and pick a `.asciiramp` file (for example `Crosses copy.asciiramp` from step 3.5) and a text file | Nothing is added. The message lists both: the ramp file "it isn't a character set file", and the text file "it isn't a character set file" too |

### P4: Pasting and odd characters

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Paste `←↑→↓ ░▒▓█` into the bottom box and click **Add** | A set of those eight characters |
| 2 | Rename a set to the same name in different case, such as `lines` for **Lines** | Accepted: a set's own name never counts as taken |
