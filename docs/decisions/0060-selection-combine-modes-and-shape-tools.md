<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0060: Selection combine modes and the shape selection tools

Status: Accepted

## Context

Step 4.2 completes the M group with Ellipse Select, the Lasso and the Polygonal Lasso, and adds the combine modes ARCHITECTURE §10 asks for. The roadmap left open how the modifier keys fit with Alt+click picking colour (ADR 0043). The owner (2026-11-20) chose the six recommendations put to them, recorded below.

## Decision

- **Modes:** `SelectionCombineMode` is Replace, Add, Subtract or Intersect. `SelectionMask.CombinedWith` does the set operation over the two masks' bounds only, so it costs what the shapes cover, not the canvas.
- **Keys at the press pick the mode** for that gesture: Shift adds, Alt (Option) subtracts, both intersect. Otherwise the mode is the **Mode** tool option, four buttons shared by every selection tool (`SelectionToolOptions.Group`) and remembered like other options. Alt+click picking stays with the brushes, where Alt has no selection meaning.
- **Keys pressed during a drag shape the box** of Rectangle and Ellipse Select: Shift makes it look square on screen, using the cell's width-to-height ratio as the Brush's perceptual proportions do (ADR 0047), and Alt grows it from the first cell outwards, with odd sides so it stays centred. A key held since the press only picks the mode until it's let go and pressed again. The canvas tells the tool when Shift or Alt goes down or up (`ITool.ModifiersChanged`), so the shape changes without the pointer moving.
- **A cell is in a shape when its centre is**, on the edge included. A lasso's path counts by the nonzero winding rule, so every loop it goes round is inside, also where it crosses itself.
- **A click** (a press that never reaches another cell, or a lasso with no area) deselects in Replace mode and changes nothing in the others.
- **The Lasso** takes a point whenever the pointer has moved a fifth of a cell, and joins its end to its start on release. **The Polygonal Lasso** takes a corner per click, with a line from the last one to the pointer, and finishes on a click on the first corner's marker (once there are three corners; ADR 0061), a double-click (`ToolPointerInput.ClickCount`) or Enter. Backspace removes the last corner, and Esc or switching tools cancels. Both show their line through `ToolOverlay.Path`.
- Every gesture is one undo step, named after its tool, made through `SelectionEdits.Select` (ADR 0055).
- Alt pressed during a drag on the canvas counts as used, as an Alt+click does, so letting it go doesn't open the menu bar (ADR 0043).

## Alternatives

- **A cell is in when the shape touches it:** small ellipses come out as boxes, and a lasso grabs cells it only grazes.
- **Even-odd filling:** a lasso that loops over itself would leave holes where the loops overlap, which surprises more than it helps.
- **Keys keep shaping the box from the press** (one Shift both adds and squares, as some editors do): the owner chose to keep the two apart.
- **A Polygonal Lasso that finishes only by returning to the first corner:** slower, and hard to hit on small cells.

## Consequences

- Each tool builds a mask and hands it to `SelectionCombiner`, so the Magic Wand (4.3) only has to build its mask.
- On Windows, Alt+Shift can be the system's shortcut for switching keyboard layouts, which may get in the way of Intersect for people with several layouts.
- `ChoiceToolOption.IsShownAsButtons` lets an option with more than two choices show as buttons.

Updated 2026-11-20: the Polygonal Lasso finishes on a click on its first corner's marker, not in its cell, as decided with the selection overlay (ADR 0061).
