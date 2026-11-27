<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0082: Box styles on the rectangle, and fills over joins

Status: Accepted

## Context

After testing 5.4 (ADR 0081), the owner said the box-drawing styles were meant for the Rectangle only. The Line and the Ellipse should stay as they were, except that the Line keeps Include joins. The owner also found that a rectangle filled with a character still joined the lines it covered, making `┼` where a line went on into the fill. The newer rectangle should cover what's behind it, as it does inside.

## Decision

- **The Rectangle's own Style.** `ShapeOptions.RectangleStyle`, in its own group (`askiiart.shapes.rectangle.style`), offers every style, the box-drawing ones included. `LineStyleOptions.Style`, shared by the Line, the Ellipse and the coming Curve, offers Standard, Standard by step, Precise and Single character. The two are separate values, so a style chosen for lines no longer carries to rectangles. Both are made by `LineStyleOptions.CreateStyle`, with the id `style`, so `ReadStyle` reads either. Choosing a character in the Characters window makes the active tool's own Style Single character.
- **Include joins** moves to a group of its own (`askiiart.shapes.joins`), which the Line and the Rectangle list after their Style. The Ellipse doesn't list it and never joins. The Line's joins are `+`, since it draws plain characters. The box-drawing warning is the Rectangle's alone.
- **Fills over joins.** When a rectangle is filled with a character, its outline's joins drop the arms the old character had toward the cells the fill covers (`LineSides`, `ShapeCells.PutLine`'s covered cells). An old line running into the rectangle stops at its edge, as `┴` or `┤` rather than `┼`. A fill of None or Background leaves the old lines inside, so they still cross.

## Alternatives

- **One shared Style, with the Line and Ellipse falling back to Standard for a box style:** the dropdown would show a choice the tool can't draw.
- **Covering for every fill:** a background fill keeps the glyphs inside, so their lines still meet the outline.

## Consequences

- Saved settings with a box style in the shared Style read back as Standard, since that choice no longer exists there.
- The Line tool joins box-drawing lines with `+`. A Curve or Table that should join in box characters would need the box styles too.
