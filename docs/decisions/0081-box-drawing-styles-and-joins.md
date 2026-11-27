<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0081: Box-drawing styles and joins

Status: Accepted

> Partly replaced by [ADR 0082](0082-box-styles-on-the-rectangle-and-fills-over-joins.md): only the Rectangle has the box styles, in a Style of its own; Include joins is on the Line and the Rectangle only; and a rectangle's character fill covers the lines it joins. The rest still stands.

## Context

Step 5.4 adds box-drawing joins, opt-in as the owner asked in M5's planning: an "Include joins" option where lines and outlines join the lines already there (`┬ ┼ ╋`), with `+` for every join in an ASCII-only document. At the step's start no style drew box-drawing characters, since 5.1's styles (ADR 0078) are all plain text. The owner agreed to add three box styles, the rules below for what joins, a Tool Options warning now rather than an M8 notification, and one shared Include joins option.

## Decision

- **Box styles.** The Style gains **Box** (`─ │ ┌ ┐ └ ┘`), **Heavy box** (`━ ┃ ┏ ┓ ┗ ┛`) and **Double box** (`═ ║ ╔ ╗ ╚ ╝`), between Precise and Single character. Cells take their character by direction, as Standard's do. Diagonals are the light `╱ ╲` in all three, since Unicode has no heavy or double ones. A corner joins the cells on either side of it (`┌` where they're to its right and below). A corner whose neighbours are across corners from it, which rectangles never have, is the style's cross (`┼ ╋ ╬`), as Standard's are `+`. In Box, rounded rectangle corners are `╭ ╮ ╰ ╯`. Heavy and Double have no rounded characters, so their corners stay square.
- **ASCII-only documents.** `LineGlyphPicker.Drawn` draws a box style as Standard where the character set can't hold box-drawing characters, rounded corners included. The Tool Options toolbar's warning line says so, as it does for a character the document can't hold.
- **Arms.** A line character is `- | +` or a box-drawing character made of arms from its cell's middle to its sides, each light, heavy or double (`LineArms`). `LineJoins` holds every such character from U+2500 to U+257F, taken from their Unicode names. The ASCII characters count as light. Dashed and diagonal box-drawing characters aren't line characters.
- **Joining.** With **Include joins** on, a drawn line character over a line character joins it. The cell gets the drawn line's arms at their own weights, plus whatever other arms the old character had. An open line's ends have no arm pointing away from the line, so `─` ending on `│` makes `┤`, not `┼`. Joining that adds no arm keeps the drawn character, so a rounded corner stays rounded. The drawn character's kind decides the result. `- | +` give `+` wherever arms go both across and up and down. Box characters give the one with exactly those arms. Where none has them together (double with heavy, say), every arm takes the drawn line's weight, or failing that is light, which every set of arms has. Joining never makes a rounded corner. The cells joined are those on the active layer before the drag, since each redraw first takes back the last (ADR 0079). Single character never joins, nor do fill cells or anything else. Precise joins only where it picks `-` or `|`.
- **The option** is a `ToggleToolOption` in the Shapes group's shared group, after Style, off by default. It shows in Single character too, where it does nothing, since Tool Options can't yet hide an option by another's value.

## Alternatives

- **Arms for every cell in a box style** (`┐` wherever a path's neighbours turn): would put corners in ellipses' slopes. Direction picking keeps them as Standard draws them.
- **Box styles drawing nothing in an ASCII-only document,** as a character it can't hold does: the owner asked for `+` joins there, which needs lines.
- **Joining diagonals** (`╱` over `╲` as `╳`): left out. A crossing diagonal draws over.
- **Hiding box styles in ASCII-only documents:** choices are fixed data, and the warning already explains.

## Consequences

- Lines joining what's under them depend on what's on the active layer only, not on the other layers.
- The Curve, Table and Line brush steps can use the same joins through `ShapeCells.PutLine`.

Updated 2026-11-26: the box styles are the Rectangle's alone, Include joins is the Line's and the Rectangle's, and a character fill covers what it joins (ADR 0082).
