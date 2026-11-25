<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0079: The Line tool and the Shapes group

Status: Accepted

Partly replaces ADR 0078: both Standards are kept, and the comparison sheets are gone.

## Context

Step 5.2 puts 5.1's line characters (ADR 0078) into a tool. The owner chose U for a Shapes group holding Line, Curve, Rectangle, Ellipse and Table on 1 to 5, the Move tool's constraints for its drags, and the Style option from 5.1. After judging 5.1's sheets, the owner kept both Standard and Standard by step, since each looks better in some places, and asked that Single character share one character with the Character Brush, both ways.

## Decision

- **The group.** `BuiltInToolGroups.Shapes` is on U and holds `LineTool` on 1; the later shape tools join it.
- **Style.** `LineStyleOptions.Group` (`askiiart.shapes`) holds only Style: Standard (the default), Standard by step, Precise and Single character. Every shape tool lists it, so a style chosen for lines carries to the others. The Line tool also lists the Character Brush's own group, so Tool Options shows its Character, and Single character draws that character: changing it on either tool changes it on both.
- **The Characters window.** `ToolDefinition.CharacterChosen` says what choosing a character there does while a tool is active. The Line tool's makes Style Single character and keeps the Line tool. A tool without it, as before, switches to the Character Brush. The Character Brush's does nothing more.
- **Drawing.** A press starts the line at the middle of its cell, and each move draws it to the middle of the cell under the pointer, kept to a direction by the modifiers through `MoveConstraint` (ADR 0069). The line is real cells in the document as it's dragged: each new end takes back the last end's step (`CancelNewestStep`) and executes a new one, so the preview is exactly what's drawn and the drag ends as one step. A line back in the cell it started in draws nothing. Esc takes the line back. If something else changes the history meanwhile, such as an undo, the drag ends without taking back a step not its own.
- **Cells.** The line clips to the selection, skips cells off the canvas, and draws in the current colours, keeping each cell's background when no background is chosen. It doesn't start on a locked or hidden layer, or in Single character while the document can't hold the character; the Character Brush's warning shows then.
- **Cost.** Measured on JetBrains Mono, a pick takes about 0.3 ms for 1000 cells in Standard, and in Precise about 1.4 ms for 40 cells, 6.7 ms for 200 and 32 ms for 1000. A pick happens only when the end changes cell.

## Alternatives

- **A preview overlay, drawn into the document on release:** two renderings of the same line to keep alike, and an overlay can't show how the line composites with layers above.
- **Choosing a character always switching to the Character Brush:** it would undo the sharing the owner asked for while drawing lines.
- **Keeping one Standard:** the owner kept both.

## Consequences

- Precise lines across very large canvases may lag while dragged; if so, finding each cell's nearby pieces can be made cheaper, as it allocates now.
- The later shape tools reuse the drawing pattern and the style group.
