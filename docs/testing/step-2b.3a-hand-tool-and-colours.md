<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2b.3a: Hand tool and colours, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 25 min

## What changed

There's a Hand tool (V) that pans the canvas with a plain left-drag, and Esc switches to it from any tool, so a tool is always chosen. Documents can now have colours: the New dialog picks monochrome, ANSI or RGB, and the Document menu switches between them. There's an FG colour and a BG colour, chosen in three new windows (Brush Options, Color and Swatches), and the text tool types in them. X swaps the two colours and D sets both to None; D is a temporary key.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).

## Tests

### T1: The Hand tool pans and changes nothing

This checks the new Hand tool, its V key and its cursor.

**Also on macOS:** Yes, because the cursor comes from the system and a touchpad drag is different

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Tools** menu | It lists **Hand**, **Text** and **Eraser**, with a check mark on **Text**. The Tools toolbar has a **Hand** button too. |
| 2 | Click an empty part of the canvas, so that it has the keyboard, then press **Esc** | The caret goes, and **Tools** has the check mark on **Hand**. |
| 3 | Press **T**, then **V** | **Text** is chosen, then **Hand** again. Over the canvas the mouse pointer is a hand, and it stays a hand while you just move it around. |
| 4 | Drag across the canvas with the left mouse button (on a Mac, with the touchpad pressed) | The art moves with the pointer, as Space + drag does. Nothing is typed or erased, and the History window gets no new step. |
| 5 | Hold **Space** and drag | It pans in the same way. |
| 6 | Press **T** | **Text** is chosen again and the pointer over the canvas is back to normal. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Esc always leaves a tool chosen

This checks what Esc does with each tool.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the **Text** tool, click a cell and type `ab` | The letters appear with the caret after them. |
| 2 | Press **Esc** once | The caret is gone and **Tools** has the check mark on **Hand**. |
| 3 | Press **Esc** again | Nothing changes; **Hand** stays chosen. |
| 4 | Press **E** for the Eraser. Press the left mouse button on the **a** and, still holding it, drag over the **b** | Both letters are erased as you drag. |
| 5 | Still holding the button, press **Esc** | Both letters come back, History shows no erase step, and **Eraser** is still chosen. |
| 6 | Let go of the button, then press **Esc** | **Hand** is chosen. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: A new document's colours

This checks the New dialog's new colour choice and the three new windows.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | **File → New** (choose **Don't Save** if it asks) | Under **Colors** there are three choices: **Monochrome (no colors)**, which is chosen, **ANSI (the 16 standard colors)** and **RGB (any color)**. |
| 2 | Choose **RGB (any color)** and click **OK** | A new empty document. The **Document** menu ends with **Monochrome**, **ANSI Colors** and **RGB Colors**, with the check mark on **RGB Colors**. |
| 3 | Open **Window → Brush Options**, **Window → Color** and **Window → Swatches** | Each window opens, docked beside the Layers window or floating over the canvas. Each name in the Window menu now has a check mark. |
| 4 | Look at **Brush Options** | Two choices, **FG: None** (chosen) and **BG: None**, each with an empty square, and a line saying what X and D do, with D marked as temporary. |
| 5 | Look at **Swatches** and **Color** | Both say **Changes the FG color**. Swatches has 16 small coloured squares and a **None** button; Color has a colour picker. |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Choosing the FG and BG colours

This checks that the three windows work together.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | In **Swatches**, rest the pointer on the second square | A tooltip says **Red**. |
| 2 | Click it | Brush Options shows **FG: #AA0000** with a red square, and the Color picker moves to that red. In an RGB document an ANSI colour shows as its hex code. |
| 3 | In **Brush Options**, click **BG: None** | Swatches and Color now say **Changes the BG color**. |
| 4 | In **Color**, drag around the big colour area | The BG square in Brush Options follows the colour you drag to, with its hex code. The FG colour stays red. |
| 5 | In **Swatches**, click **None** | Brush Options shows **BG: None** again. |
| 6 | Choose a BG colour again with any swatch. Click an empty part of the canvas, press **Esc** for the Hand tool, then press **X** | The two colours swap places in Brush Options. Press **X** again and they swap back. |
| 7 | Click a swatch, then press **X** straight away, without clicking the canvas | The colours still swap: clicking the windows doesn't take the keyboard from the canvas. |
| 8 | Press **D** | Both colours are **None**. |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Typing in colour, across layers

This checks that the text tool writes the colours, and keeps a background when the BG colour is None.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Red** for the FG colour and **Blue** (the fifth square) for the BG colour. Press **T**, click a cell and type `Hello` | Red letters on blue cells. |
| 2 | Set the BG colour to **None** (click **BG** in Brush Options, then **None** in Swatches). Click the **H** and type `yo` | The **H** and **e** become red **y** and **o**, still on blue: with the BG colour None, typing keeps a cell's background. |
| 3 | Press **Backspace** | The **o** is erased, but its cell stays blue. |
| 4 | With the caret still placed, press **X** and then **D** | They're typed as letters, `x` and `d`. The colours in Brush Options don't change. |
| 5 | Press **Esc**. **Layer → New Layer**. Press **T**, click a cell on the blue row and type `zz` | Red letters, and the blue of the layer below still shows behind them. |
| 6 | Hide the new layer in the Layers window | The `zz` disappears and the row below shows as before. Show the layer again. |
| 7 | **Edit → Undo** a few times | The typing comes off in steps, newest first. **Redo** puts it back. |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Switching colour modes, and undoing

This checks the Document menu's colour items and their warnings.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the T5 document still open, pick an orange in the **Color** window for the FG colour and type a few letters | Orange letters. |
| 2 | **Document → ANSI Colors** | A **Change Colors** box: "Switching to ANSI colors changes N cells to the nearest of the 16 colors. You can undo this.", where N counts the orange cells and any others that would change. |
| 3 | Click **Cancel** | Nothing changes, and the check mark stays on **RGB Colors**. |
| 4 | **Document → ANSI Colors** again, then **Convert Colors** | The orange letters change to the nearest of the 16 colours. The check mark moves to **ANSI Colors**. The Color window shows a note instead of the picker, Swatches still works, and Brush Options names the FG colour by the ANSI colour nearest the orange. |
| 5 | **Edit → Undo** | The orange letters are back, and **RGB Colors** is ticked again. |
| 6 | **Document → Monochrome**, then **Remove Colors** | The box says it removes the colors from N cells. All colours go: the letters are plain ink on paper. Brush Options and Swatches show a note saying the document has no colours. |
| 7 | Type a few letters | They have no colour. |
| 8 | **Edit → Undo** until **RGB Colors** is ticked again. Choose **Document → ANSI Colors** and **Convert Colors**, then **Document → RGB Colors** | It switches straight back without asking, because going from ANSI to RGB changes no cell's look. |

- [ ] Pass  - [ ] Fail. Notes:

### T7: Saving and reopening keeps the colours

This checks that colours and the colour mode are saved.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With colour on the canvas, **File → Save As** and save it | Saved. |
| 2 | **File → New** (any settings), then **File → Open** the saved file | The colours are back exactly, and the **Document** menu ticks the mode the file was saved in. |
| 3 | Look at **Brush Options** | The FG and BG colours are the same as before **New**: they belong to the app, not to a document. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Close and reopen the three windows, dock them in other places, and switch documents while they're open.
- Choose colours in an ANSI document, then open an RGB one, and the other way round.
- Type with colours in an ASCII-only document, and switch ASCII Only on and off.

## Known limitations (not bugs)

- The three windows and the colour items in the Document menu are placeholder UI. The windows aren't in the default layout yet.
- **D is a temporary key.**
- The colour picker is only in RGB documents; ANSI documents use Swatches.
- Right-click does nothing yet, and there's no brush or eyedropper (that's step 2b.3b).
- There's no picking a colour from outside the app.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2b.3a, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
