<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 5.3: Rectangle and Ellipse, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 25 min

## What changed

The Shapes group (**U**) now has **Rectangle** and **Ellipse** after Line. Drag from one corner of a box to the other, and the shape is drawn in that box as you drag, ending as one History step. **Shift** makes the box look square on screen (so an ellipse looks round), and **Alt** (Option on a Mac) grows it outwards from where you pressed. Both use the same **Style** as the Line tool. A new **Fill** option fills the inside with the Character Brush's character or with the background colour. The rectangle has a **Rounded corners** option, which uses `.` and `'` at its corners.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Open **Window → History**, **Window → Characters** and **Window → Swatches** if they aren't open already.
3. If the document has no colours, choose **Document → ANSI Colors**.

## Known limitations (not bugs)

- **Number keys for now:** Rectangle is on 2 and Ellipse on 3 until Curve joins the group in 5.5, which will move them to 3 and 4. You plan to overhaul shortcuts in M8.
- **Rounded corners is a button, not a checkbox.** It stays down while it's on. A checkbox made Tool Options taller than the other toolbars.
- **The Tools toolbar may still scroll** on a 2048-pixel-wide window, as in 5.2.
- **Placeholders:** the option names, their order, and the Character box showing even when nothing uses it. See `docs/TEMPORARY-UI.md`, "Rectangle and Ellipse".

## Tests

### T1: Choosing the tools

This checks the new tools' keys, buttons and menu items, and the options each shows.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click the canvas once, press **U**, then **2** | The Rectangle tool is chosen. A sub-toolbar beside the Tools toolbar shows **Line**, **Rectangle** and **Ellipse** |
| 2 | Look at Tool Options | **Style** (Standard), **Fill** (a dropdown showing None), a **Rounded corners** button that isn't down, then **Character** |
| 3 | Press **3** | The Ellipse tool is chosen. Tool Options shows **Style**, **Fill** and **Character**, with no Rounded corners |
| 4 | Press **1** | The Line tool is chosen |
| 5 | Open the **Tools** menu | **Rectangle** and **Ellipse** come after **Line**, and choosing one chooses that tool |

### T2: Drawing a rectangle

This checks the basic drag: the box from corner to corner, what shows while dragging, and that it's one undo step.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Rectangle tool, press on an empty cell and drag down and to the right, slowly, without letting go | A rectangle of `-` and `\|` with `+` corners follows the pointer: the cell you pressed is one corner, the cell under the pointer the other |
| 2 | Still holding, move back up and to the left of where you pressed | The rectangle flips to that side, with no leftovers from earlier boxes |
| 3 | Let go | The rectangle stays. History has one new step, **Rectangle** |
| 4 | Press **Ctrl+Z** (Cmd+Z on a Mac) | The whole rectangle goes |
| 5 | Press, drag away, come back to the cell you pressed and let go | Nothing is drawn, and History has no new step |
| 6 | Drag straight across along one row | A straight line of `-`, as the Line tool would draw |

### T3: Rounded corners

This checks the rectangle's rounded corners in each style.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Click **Rounded corners** so it stays down, and draw a rectangle | The top corners are `.` and the bottom corners `'`, like `.----.` over `'----'` |
| 2 | Set **Style** to **Standard by step**, then **Precise**, drawing a rectangle each time | The same `.` and `'` corners each time |
| 3 | Set **Style** to **Single character** and draw a rectangle over some existing text | The edges are the Character box's character, and the four corner cells are left as they were |
| 4 | Click **Rounded corners** again so it's up, and draw | Corners are the character again |

### T4: Fill

This checks filling the inside with a character or a background colour.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Set **Style** to **Standard**, set **Fill** to **Character**, choose `#` in the Characters window, and draw a rectangle | The outline is `-`, `\|` and `+`, and every cell inside it is `#`. **Style** stayed Standard: choosing a character didn't change it, since the fill uses it |
| 2 | Choose a blue background in Swatches, set **Fill** to **Background**, type some text, and draw a rectangle around it | The inside turns blue, and the text inside keeps its characters |
| 3 | Choose **Document → Monochrome** | Tool Options shows "This document has no background colours". Drawing a rectangle draws only its outline. Choose **Document → ANSI Colors** again |
| 4 | Set **Fill** to **None** and draw a rectangle across the text | Only the outline is drawn, and the text inside stays as it was |

### T5: Ellipse

This checks the ellipse and its fill.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **U** then **3**, set **Fill** to **None**, and drag a box about 20 cells across and 10 rows down | An ellipse that touches the middle of each side of the box. History says **Ellipse** |
| 2 | Draw it again in **Standard by step** and **Precise** | Each style's characters, as they look on the Line tool's lines |
| 3 | Set **Fill** to **Character** and draw an ellipse | The inside is filled right up to the outline, with nothing outside it |

### T6: Shift and Alt

This checks the selection tools' keys on the shapes: Shift for square or round, Alt for growing from the middle, also when pressed or let go during a drag.

**Also on macOS:** Yes, because Alt is Option there.

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Rectangle tool, hold **Shift** and drag in any direction | The rectangle looks square on screen: about twice as many columns as rows |
| 2 | With the Ellipse tool, hold **Shift** and drag | The ellipse looks like a circle |
| 3 | Hold **Alt** (Option) and drag | The shape grows outwards from where you pressed, which stays its middle |
| 4 | Drag without keys, then press **Shift**, then **Alt**, then let both go, without moving the mouse | The shape changes at once each time, and goes back when they're let go |
| 5 | Let go of the mouse after an Alt drag (on Windows) | The menu bar doesn't open |

### T7: Esc, selection and locked layers

This checks the same rules the Line tool follows.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Drag a rectangle and press **Esc** before letting go | It disappears, History has no new step, and the Rectangle tool stays chosen |
| 2 | Press **M**, select a small area, press **U** then **2**, and draw a rectangle across it with **Fill** set to **Character** | Only the parts inside the selection are drawn. Press **Ctrl+D** (Cmd+D) to deselect |
| 3 | Lock the layer in the Layers window and try to draw | Nothing is drawn. Unlock it again |

### T8: The shared character

This checks that the shapes use the Character Brush's character, and how choosing one in the Characters window changes the style.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | With the Rectangle tool, **Style** on **Standard** and **Fill** on **None**, click `@` in the Characters window | **Style** changes to **Single character**, the box shows `@`, and the Rectangle tool stays chosen |
| 2 | Set **Fill** to **Character** and **Style** to **Precise**, then click `%` in the Characters window | **Style** stays **Precise**, since the fill uses the character. A rectangle draws `%` inside |
| 3 | Choose the full block `█` (Blocks and Shades), then **Document → ASCII Only** (choose **Switch to ASCII Only** if it asks) | Tool Options shows "This character needs a Unicode document", and drawing draws nothing. Set **Fill** to **None** and **Style** to **Standard**: the warning goes, and shapes draw again |

## Worth poking at (optional)

### P1: Big and small

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Draw a filled Precise ellipse almost as big as the canvas, dragging around for a while | It keeps up with the pointer |
| 2 | Draw rectangles and ellipses only 2 or 3 cells each way | Small but whole shapes, with nothing drawn outside their box |
| 3 | Draw a shape that runs off the edge of the canvas | Only the part on the canvas is drawn |
