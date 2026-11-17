<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0050: The erasers

Status: Accepted

## Context

Step 3.4 adds a soft eraser for text and a hard one for backgrounds, as separate tools (ARCHITECTURE §8.3). The owner's answers (2026-11-17):

- The Eraser group is **Eraser**, which still empties whole cells, **Soft Eraser** and **BG Eraser**, on keys 1–3. E brings back the eraser used last.
- The three erasers share one size and shape, separate from the brushes'. The Soft Eraser's ramp is the Brush's, so changing it on either changes both.
- **Strength** (default 100%) is how far one stroke lightens the middle; at 100% the middle clears. **Softness** defaults to 50%.
- A lightened glyph keeps its own colours. In a monochrome document the BG Eraser stays listed, erases nothing, and says why.

## Decision

- **The hard erasers are one class.** `EraserTool` takes an `EraserTarget`:
  - `Everything` (`askiiart.eraser`) empties the cell.
  - `Background` (`askiiart.eraser.background`) unsets the background and keeps the glyph and its colour. A cell with no background isn't changed, so it leaves no step.
  - The BG Eraser's `WarningFor` gives `ToolWarningNoBackgrounds` in a monochrome document.
- **The Soft Eraser** is `SoftEraserTool` (`askiiart.eraser.soft`).
- **Shared options are shared groups.** `ToolSettings` keeps one set of values per group, so tools that list the same group share its values.
  - All three erasers list `EraserTool.OptionGroup`, the tip.
  - The ramp moves out of the Brush's own group into `RampOptions.Group`, which the Brush and the Soft Eraser both list. `RampOptions.WarningFor` is the shared warning for a ramp the document can't hold.
  - The Soft Eraser's own group is Strength, then Softness. Its Softness isn't the Brush's.
- **One stroke model.** The Brush's stroke from 3.3 becomes `RampStroke` (`Core/Tools/RampStrokes`). It moves each cell from its darkness `d` towards a target, as `LevelAt(d + (target − d) × weight × strength)`, once per stroke, at the greatest weight that reached it. A delegate gives each moved cell.
  - The Brush: the target is its intensity, the strength is 1, and the cell takes the FG and BG colours.
  - The Soft Eraser: the target is 0, and the strength is its Strength. A glyph whose level changes keeps its FG and BG, and one taken to level 0 leaves its background. An empty cell stays as it is.
  - A glyph not in the ramp counts as the level that measures nearest (ADR 0049), and stays until it moves a whole level.
- **Undo:** each eraser's stroke is one step named Erase, and Esc takes it back.

## Alternatives

- **One Eraser with a mode option:** the owner asked for separate tools. Each mode has different options, and Tool Options can't show options that depend on another option.
- **A ramp per tool:** erasing along another ramp than the one painted with would jump between characters that don't match.
- **Hiding the BG Eraser in a monochrome document:** the keys and menus would shift with the document. Ramps that can't be used stay listed with a warning, and so does this.
- **Always moving at least one level:** a 1% stroke would then strip a whole level, so low strengths would all look alike. Rounding to the nearest level keeps them distinct, but a light glyph stops lightening once one stroke moves it less than half a level.

## Consequences

- ADR 0049's ramp option and stroke now live in `RampOptions` and `RampStroke`, and the Brush's own group holds Intensity, Softness and Blend mode.
- At low Strength, repeated strokes stop at a light glyph rather than clearing it. At 40%, `.` never clears.
- What a soft background eraser does stays open (ARCHITECTURE §23).
- The ramp choice that 3.5 saves is one setting for both tools.
