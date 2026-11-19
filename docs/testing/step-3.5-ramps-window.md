<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 3.5: The Ramps window and remembered settings, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 40 min

## What changed

There's a new **Ramps** window (**Window → Ramps**). It lists the five built-in ramps and any you make, and lets you:

- **make** ramps: **New**, **Duplicate** (the only way to change a built-in ramp is to edit a copy), **Generate** (the app picks evenly spread characters from a set you give it), and **Import…** from `.asciiramp` files
- **edit** your own ramps: the name, the characters (lightest first) and whether the lightest level is an empty cell or a space. Changes apply as you type, as soon as they're valid
- **Sort by Darkness**, which reorders a ramp's characters by how dark they look in the document's font
- **Delete** and **Export…** ramps

Your ramps show in the Brush's and Soft Eraser's **Ramp** dropdown, after the built-in ones.

The app now also **remembers between runs** your ramps, every Tool Options value, the tool you last used in each group, and the FG and BG colours. It still starts on the Text tool.

## Setup

1. Run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`).
2. Choose **File → New**. Under **Characters** choose **Unicode (any character)**, and under **Colors** choose **ANSI (the 16 standard colors)**. Click **OK**, and choose **Don't Save** if it asks.
3. Choose **View → Zoom In** two or three times, so each character is easy to read.
4. The document's font should be **JetBrains Mono**, the default. Some expected results below depend on it.
5. If you've run this build before, open **Window → Ramps** and delete the ramps you made (choose each one and click **Delete**), so the names below match.

## Known limitations (not bugs)

- The window's layout, its text buttons and the wording are placeholders.
- Ramp edits aren't on Undo, and Delete can't be undone (it asks first).
- Ramps are saved as soon as they change, but tool options, last tools and colours are saved only when the app closes normally. **Rider's Stop button skips that**, so close the app with its own close button (or **File → Quit**, or Cmd+Q on a Mac) when a test checks remembering.
- The Ramps window shows characters in a common monospaced font, not the document's, so they can look a little different from the canvas.

## Tests

### T1: The Ramps window

Checks that the window opens from the Window menu, lists the built-in ramps, and shows a built-in ramp without letting you change it.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Window** menu | **Ramps** is listed after **Swatches**, with no check mark |
| 2 | Choose **Ramps** | The Ramps window opens. **Window → Ramps** now has a check mark |
| 3 | Look at the list at the top of the window | Five ramps: **Standard**, **Standard Alphanumerical**, **Fine**, **Blocks**, **Braille**, each with its characters on a line below its name. **Standard** is chosen |
| 4 | Look below the list | Six buttons: **New**, **Duplicate**, **Delete**, **Sort by Darkness**, **Import…**, **Export…**. **Delete** and **Sort by Darkness** are greyed out. Below them, a grey note: "Built-in ramps can't be changed. Duplicate one to edit a copy." |
| 5 | Look at the editor below the note | **Name** says `Standard`, **Characters, lightest first** says `.:-=+*#%@`, **Lightest level** has **Empty cell** chosen, and **Preview** shows three identical rows going from blank on the left through `.` `:` `-` … to `@` on the right |
| 6 | Click in the **Name** box and type | Nothing changes: the box is read-only. The **Empty cell** and **Space** buttons are greyed out |
| 7 | Click **Blocks** in the list | The editor shows Blocks' name and its block characters, and the preview changes to them |

### T2: A new ramp, edited as you type

Checks that New adds an editable ramp and that edits show at once in the list and the preview.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click **New** | **New ramp** is added at the bottom of the list and chosen, with Standard's characters `.:-=+*#%@`. The grey note goes, and **Delete** and **Sort by Darkness** are no longer greyed out |
| 2 | Select all the text in **Name** and type `Dots` | The list's row changes to **Dots** letter by letter as you type |
| 3 | Select all the text in **Characters** and type `.oO@` | The row's characters and the preview follow as you type. When you've finished, the preview goes from blank through `.`, `o` and `O` to `@` |
| 4 | Click **Space** under **Lightest level** | Space is chosen. The preview looks the same: a space and an empty cell both show as blank |
| 5 | Click **Empty cell** | Empty cell is chosen again |

### T3: What can't be used

Checks that a name or characters that can't be used say why in orange, and don't change the ramp. Edits apply as you type, so typing a letter at a time applies each step that can be used, such as `a` on the way to `a b`; pasting the whole text shows the results below exactly.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With **Dots** chosen, select all of **Characters** and press **Delete** | The box is empty, and below it in orange: "Type at least one character." The list still shows `.oO@` for Dots |
| 2 | Paste `a b` | Orange: "A space can't be a step. To paint spaces where there's no ink, choose Space as the lightest level." The list still shows `.oO@` |
| 3 | Select all and paste `abca` | Orange: "a is in the ramp more than once." The list still shows `.oO@` |
| 4 | Select all and type `.oO@` | The orange message goes |
| 5 | Select all of **Name** and paste `standard` | Orange: "Another ramp already has this name." The list still says **Dots** |
| 6 | Select all of **Name** and press **Delete** | Orange: "Give the ramp a name." The list still says **Dots** |
| 7 | Type `Dots` | The orange message goes |
| 8 | Type `abca` into **Characters** again, then click **Standard** in the list, then **Dots** | The editor shows Dots' good characters `.oO@`, with no orange message: what couldn't be used is dropped |

### T4: The Brush paints with your ramp

Checks that your ramps are in Tool Options' Ramp dropdown and that the Brush follows edits, renames and deletions.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **B** then **1** for the Brush, and open the **Ramp** dropdown on Tool Options | The five built-in ramps, then **Dots** with `.oO@` beside it |
| 2 | Choose **Dots**. Set **Size 9**, **Circle**, **Intensity 100%**, **Softness 100%**. Click once on the left of the canvas | A round blob made only of `.`, `o`, `O` and `@`, darkest in the middle |
| 3 | In the Ramps window, change Dots' **Characters** to `.xX#` | The Ramp dropdown's Dots entry now shows `.xX#` |
| 4 | Click once on the right of the canvas, away from the first blob | The new blob is made only of `.`, `x`, `X` and `#`. The first blob hasn't changed |
| 5 | Rename Dots to `Crosses` | The Ramp dropdown shows **Crosses**, still chosen |
| 6 | Press **E** then **2** for the Soft Eraser, and open its **Ramp** dropdown | **Crosses** is chosen there too, since the two tools share the setting |

### T5: Duplicate

Checks that Duplicate makes an editable copy, of a built-in ramp or your own.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Blocks** in the list and click **Duplicate** | **Blocks copy** is added at the bottom and chosen, with Blocks' characters, and it can be edited |
| 2 | Click **Duplicate** again | **Blocks copy 2** is added and chosen |
| 3 | Choose **Crosses** and click **Duplicate** | **Crosses copy** is added, with `.xX#` |

### T6: Sort by Darkness

Checks that Sort by Darkness reorders the characters by how dark they look in the document's font.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Standard** and click **Duplicate** | **Standard copy** is added, with `.:-=+*#%@` |
| 2 | Click **Sort by Darkness** | The characters become `.-:=+*#%@`: in JetBrains Mono, `-` is a little lighter than `:` |
| 3 | Type `@#.+:` into **Characters**, then click **Sort by Darkness** | The characters become `.:+#@` |

### T7: Generate

Checks that Generate picks evenly spread characters in the document's font and adds them as a new ramp. Darkness is measured as each system draws the font, so the exact picks can differ a little between Windows and macOS (changed after testing: the picks below are from Windows).

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Scroll down the Ramps window to **Generate a ramp** | **Pick from these characters** holds `!"#$%&'()*+,-./0123456789:;<=>?@`… up to `~` (every printable ASCII character but the space). **How many to pick** says 10 |
| 2 | Click **Generate** | **Generated ramp** is added and chosen: 10 characters from light to dark, ending with `@`. On Windows, `._!rcCE8$@`; on macOS, `.:^|*oE0$@` |
| 3 | Replace **Pick from these characters** with `abcdefghijklmnopqrstuvwxyz`, set **How many to pick** to 5 and click **Generate** | **Generated ramp 2** is added, ending with `g`. On Windows, `rvleg`; on macOS, `rlveg` |
| 4 | Click the down arrow on **How many to pick** until it stops, then the up arrow a few times | It stops at 1. Typing `500` and clicking elsewhere gives 100, the most |
| 5 | Replace **Pick from these characters** with a few spaces and click **Generate** | Orange: "None of these characters shows any ink in this font." No ramp is added |

### T8: Delete

Checks that Delete asks first, and that a tool using the deleted ramp goes back to Standard.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Crosses** and click **Delete** | A dialog titled **Delete Ramp**: "Delete the ramp “Crosses”? This can't be undone. Tools using it will switch to Standard.", with a **Delete** button |
| 2 | Cancel it | Crosses is still there |
| 3 | Click **Delete** again and confirm | Crosses is gone, and the ramp below it in the list is chosen |
| 4 | Look at Tool Options (the Soft Eraser is still chosen) and open the **Ramp** dropdown | **Standard** is chosen, and Crosses isn't listed. Press **B**: the Brush's Ramp is Standard too |
| 5 | Choose the last ramp in the list and delete it | The ramp above it is chosen |

### T9: Export and import

Checks that a ramp can be saved as a file and read back, and that files that can't be read are reported.

**Also on macOS:** Yes, because the file pickers are the system's own

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Crosses copy**, change **Lightest level** to **Space**, and click **Export…** | A save dialog titled **Export Ramp**, with the name `Crosses copy` and the type **Ramp files (.asciiramp)** |
| 2 | Save it on the Desktop | No message. `Crosses copy.asciiramp` is on the Desktop |
| 3 | Open the file in a text editor | Readable text with `"format": "askiiart.ramp"`, `"version": 1`, `"name": "Crosses copy"`, `"lightest": "space"` and `"characters": ".xX#"` |
| 4 | Change `"version": 1` to `"version": 99` and save it as `future.asciiramp` on the Desktop, leaving the first file as it was | Two ramp files on the Desktop |
| 5 | Click **Import…** | An open dialog titled **Import Ramps**, showing ramp files. The other file type to choose is all files |
| 6 | Pick both files at once and open them | **Crosses copy 2** is added and chosen, with `.xX#` and **Space** chosen. A message says these files couldn't be imported: "future.asciiramp: it's from a newer version of the app" |
| 7 | Click **Import…**, then cancel | Nothing changes |

### T10: Remembered between runs

Checks that your ramps, Tool Options values, each group's last tool and the colours come back when the app is opened again, and that it starts on the Text tool.

**Also on macOS:** Yes, because settings live in a different folder there and quitting works differently

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **B** then **1**. Set **Size 5**, **Ramp: Generated ramp**, **Intensity 40%** | The Brush's options show those values |
| 2 | Press **E** then **2** for the Soft Eraser. Open **Window → Swatches** and choose a red text colour and a blue background | Tool Options shows the Soft Eraser. The colour squares show red and blue |
| 3 | Close the app with its own close button (on a Mac, **Cmd+Q**). Choose **Don't Save** if it asks | The app closes |
| 4 | Run the app again | The **Text** tool is chosen. The colour squares are empty, because the app starts with a monochrome document (changed after testing: this row expected red and blue) |
| 4a | Choose **File → New**, choose **ANSI (the 16 standard colors)** under **Colors**, and click **OK** | The colour squares show red and blue: they were remembered |
| 5 | Open **Window → Ramps** | Your ramps are there, in the same order, with their names and characters |
| 6 | Press **B** | The **Brush** comes back (not the FG or BG Brush), with **Size 5**, **Generated ramp** and **Intensity 40%** |
| 7 | Press **E** | The **Soft Eraser** comes back, the eraser you used last |

### T11: Tests to revisit

The Soft Eraser changed after step 3.4's testing: every stroke lightens at least one level. These two rows check that.

| Step | Test | Where | Why |
|---|---|---|---|
| 3.4 | T7, row 4 | Windows | changed after testing: 40% now goes `:`, `.`, then empty, instead of stopping at `.` |
| 3.4 | T10, row 3 | Windows | changed after testing: a letter always turns into a lighter ramp character at 30% |

## Worth poking at (optional)

### P1: A name that only changes case

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make a ramp called `Dots`, then change its name to `dots` | It's renamed to `dots`, with no orange message: a ramp's own name doesn't count as taken |

### P2: Another font

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Document → Font** and pick another monospaced font, then generate a ramp and sort one | The results can differ from T6 and T7, since darkness is measured in the document's font |

### P3: A ramp an ASCII-only document can't hold

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Document → ASCII Only**. Choose **Blocks copy** for the Brush's Ramp | Tool Options warns "This ramp needs a Unicode document", as the built-in Blocks does |
| 2 | Generate a ramp from `.░▒▓█` | It's added. Choosing it for the Brush shows the same warning |

### P4: Damaged and wrong files

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Copy a ramp file, delete its last `}` in a text editor, and import it | "it's damaged" |
| 2 | Import a `.txt` file, choosing all files in the dialog to see it | "it isn't a ramp file" |

### P5: Long ramps and reopening the window

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Generate a ramp of 100 characters | Its row in the list ends in `…` where it doesn't fit. The preview shows 48 of its characters |
| 2 | Close the Ramps window and open it again | The same ramp is chosen, and the Generate settings are as you left them |
