<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0080: Rectangle and Ellipse

Status: Accepted

## Context

Step 5.3 adds the Rectangle and Ellipse tools to the Shapes group (ADR 0079). Agreed in M5's planning: they keep the box-shaped selection tools' Shift for looking square or round and Alt for drawing from the middle (ADR 0060). At the step's start the owner chose a Fill option, rounded corners drawn with `.` and `'`, and left the number keys to work as they simplest can, since M8 overhauls shortcuts.

## Decision

- **One tool class.** `BoxShapeTool` draws a `BoxShape`, Rectangle or Ellipse, as `EraserTool` does its targets. The Shapes group holds Line, Rectangle and Ellipse in that order, so keys 1 to 3 choose them; Curve, coming in 5.5 as the group's second, will move Rectangle and Ellipse to 3 and 4.
- **The box.** `DragBox`, taken out of `BoxSelectGesture` and shared with it, makes the box from the first cell to the pointer's, both in. Shift makes it look square on screen and Alt grows it from the first cell. With no combine mode to choose, they shape the box whenever they're held, from the press on. The pointer back in the first cell draws nothing, Shift or not, and a box one cell thin draws as a straight line.
- **The outline** is `LinePath.Rectangle` or `LinePath.Ellipse` through the box's outermost cells' middles, given characters by `LineGlyphPicker` in the shared Style (ADR 0078). **Rounded corners** (rectangle only) replace the four corner cells' characters with `.` at the top and `'` at the bottom in Standard, Standard by step and Precise. In Single character they leave the corner cells alone.
- **Fill** is None (the default), Character or Background, shared by both tools. The inside is every cell of the box the outline closes in: `ShapeInterior` floods from outside the box, stepping across, up and down only, so an outline that steps diagonally still closes. Character puts the Character Brush's character there, in the current colours. Background gives the cells the background colour and keeps their glyphs, as the BG Brush does. In a monochrome document it fills nothing, and Tool Options shows the BG Eraser's warning.
- **The character.** A shape uses the Character Brush's character when the style is Single character or the fill is Character. Then it joins Recent, and a document that can't hold it draws nothing and shows the Character Brush's warning. Choosing a character in the Characters window keeps the tool, and makes the style Single character only when neither the style nor the fill already uses the character.
- **Options.** `ShapeOptions.FillGroup` holds Fill, and `ShapeOptions.RectangleGroup` holds Rounded corners, a new kind of option, `ToggleToolOption`, saved as true or false. Tool Options shows a toggle as a button that stays down while it's on: the Fluent checkbox's template is 32 pixels tall and made Tool Options taller than its neighbours.
- **Drawing** is the Line tool's (ADR 0079), now shared: `LiveShape` keeps the step drawn so far, and `ShapeCells` writes cells, skipping those off the canvas, kept to the selection.

## Alternatives

- **Filling by shrinking the box:** right for rectangles only. One flood serves both, and later closed shapes.
- **An Empty fill that clears the inside,** for a box over existing art: offered, not chosen.
- **A checkbox for toggles:** too tall, as above. A checkbox made shorter by a fixed height or negative margins would hide part of its template.
- **Fixed number keys with gaps** (Rectangle on 3 before Curve exists): more machinery for keys M8 replaces.

## Consequences

- Each redraw floods the box when filling, which costs what the box covers, as drawing the fill does anyway.
- Rectangle and Ellipse change number keys when Curve joins the group.
