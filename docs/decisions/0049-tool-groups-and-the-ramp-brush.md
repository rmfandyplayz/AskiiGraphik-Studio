<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0049: Tool groups and the ramp Brush

Status: Accepted

## Context

Step 3.3 makes the Brush paint ramps (ADR 0045). It also replaces the one-off Brush sub-toolbar (ADR 0043) and the Debug sub-toolbar samples with tool groups. The owner's answers (2026-11-16):

- The Brush group is Brush, FG Brush and BG Brush, on keys 1–3. B brings back the brush used last.
- **Intensity** (default 100%) is how dark the middle paints. **Softness** (default 50%) is how much of the radius fades, and the fade follows the shape.
- The Brush blends towards the intensity, and writes the FG and BG colours as typing does. Alt+click with it picks both colours.
- Blend mode is a dropdown, with Replace and Darken greyed out as a proof of concept.

## Decision

- **Tool groups are data in Core.**
  - A `ToolGroup` has an id, a name, an icon, the shortcut letter and its tools in order. The letter belongs to the group; tools no longer have one.
  - `ToolRegistry` takes the tools and the groups, and rejects a tool that's in no group or in two.
  - Groups register through DI, as tools do, so a plugin can add one.
- **In the App:**
  - There's an action per tool, for the Tools menu and the sub-toolbars, and one per group, for the Tools toolbar.
  - `CanvasViewModel` remembers each group's last tool, so choosing the group, or pressing its letter, brings that tool back.
  - **Number keys** choose a tool of the active group. `CanvasView` takes them as typed text the tool didn't use, so the Text tool still types digits, and keyboards that need Shift for a digit work.
  - `ToolGroupSubToolbar` shows the active group beside the Tools toolbar while it has more than one tool.
- **Three brushes:**
  - `BrushTool` (`askiiart.brush`) paints ramps.
  - `ColorBrushTool` is the old colour brush, split into two tools: `askiiart.brush.foreground` and `askiiart.brush.background`.
  - All three share the tip options, so they have one size and shape.
  - `ColorBrushTargetSync` keeps `PaintColors.Target` in step. Choosing the FG or BG Brush chooses its colour. Choosing the other colour while one of them is active switches to the other brush. With any other tool, choosing a colour only chooses it.
- **Blend:**
  - **A cell's level:**
    - A glyph in the ramp counts as its own level.
    - Any other glyph counts as the level with the nearest measured density (`Ramp.NearestLevel`), with ties going to the lighter level.
    - An empty cell or a space counts as 0.
  - **The weight** comes from `BrushFalloff`. It's 1 inside `1 − softness` of the radius, then follows a smoothstep down to 0 at the edge. Distance is measured on the footprint's own axes: Euclidean for a circle, Chebyshev for a square.
  - **The new level** is `LevelAt(d + (intensity − d) × weight)`.
  - In one stroke, each cell blends once, from what it was before the stroke, with the greatest weight it got. A second stroke pushes further.
  - A cell whose level doesn't change is left alone, so a glyph not in the ramp survives unless it moves a whole level.
- **Colours:** the Brush writes FG, and BG unless BG is None, but only on cells whose glyph changes. Lightening a cell to nothing removes its glyph and keeps its background.
- **Options and warnings are data:**
  - A `ToolDefinition` has a list of option groups, and may have a `WarningFor`, which gives a resource key when the tool can't work.
  - The Brush has the tip group, then Ramp, Intensity, Softness and Blend mode.
  - A choice can have a detail, such as a ramp's characters. A choice can also be unavailable: it shows greyed out, and can't be set or be the default.
  - A number can be a percentage.
  - A ramp that the document's character set can't hold stays listed, paints nothing, and shows the warning.
- **Glyph densities** reach a tool through `ToolContext`, as a function that returns the document font's meter from the shared cache, as the cell shape does.

## Alternatives

- **A group property on each tool:** the order and the letter would be spread across tools, and the registry would have to infer the groups.
- **Number keys inside each brush, as in 2b.3b:** every grouped tool would need the same code.
- **Blending on every pass of a stroke:** scrubbing would darken without limit, and the result would depend on the pointer's speed.
- **Hiding ramps the document can't hold:** the choice would change behind the user's back when the character set changes.

## Consequences

- ADR 0043 is superseded: the brushes' keys and sub-toolbar now belong to the group.
- Replace and Darken can't be chosen until they're built.
- A plugin's tool has to name a group, even when it's the group's only tool.
- The soft Eraser (§8.3) can reuse the falloff and the blend.
