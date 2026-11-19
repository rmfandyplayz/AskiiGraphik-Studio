<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0047: Tool options and brush sizes

Status: Accepted

## Context

Step 3.2 gives the colour Brush and the hard Eraser a size and a shape, and adds the Tool Options toolbar: a toolbar built from the active tool's options as data, so the settings can be exported later (3.5). Tools are created afresh each time they're chosen, so their settings can't live in them. The owner's answers (2026-11-16): the colours move to a toolbar of their own, one size and shape per tool is enough for now, size 1 to 100 starting at 1, a circle that looks round, and `[` and `]` change the size. The owner also gave a new default layout, because the canvas edges were getting crowded.

## Decision

- **Options are data.** A tool's definition may carry a `ToolOptionGroup`: an id and a list of `ToolOption`s, each a number (min, max, default) or a choice (named values, default), with a resource key for its label. `ToolSettings` (a singleton) holds one `ToolOptionValues` per group, created on first use; values are clamped or checked when set, and `Changed` fires only on a real change. Exporting settings later means writing these values out by group and option id.
- **The Tool Options toolbar** reads the chosen tool's group and draws a slider for a number and toggle buttons for a choice. It knows nothing about any one tool. A tool without options shows "No options".
- **A brush tip** is a size, a shape (square or circle) and proportions. **Mathematical** counts cells: size 10 is 10 × 10 cells. **Perceptual** looks square or round on screen: it's `size` cells wide and `round(size × cell width ÷ cell height)` tall, at least 1. The cell shape comes from the document's font measured at 64 px.
- **A footprint** is the cells a tip covers. A circle takes the cells whose centres fall inside the ellipse filling the tip, shrunk by `1 ÷ (2 × the longer side)`, so small sizes look round (size 3 is a plus). It's centred on the pointer as nearly as whole cells allow: an odd size centres on the pointer's cell, and an even size leans to the half of the cell the pointer is in.
- **A stroke keeps the footprint it started with.** Between two pointer positions it covers every cell the footprint passes over, each once. A size changed mid-stroke (with `[` or `]`) takes effect on the next stroke.
- **The outline:** the tool's overlay carries its footprint, and the canvas draws its outline around the cells under the pointer, in the caret's colour and thickness, instead of the hover highlight. It follows the pointer off the canvas too, because a big brush can reach in from outside.
- **The Brush Options toolbar is now the Colors toolbar** (id `toolbar.colors`), since the Tool Options toolbar holds the brush's options.
- **The default layout is the owner's:** File top-left, Tool Options top middle, Colors top-right; View bottom-left, Tools bottom middle, Edit bottom-right; Layers above History in the widgets on the right (DOCKING.md §9).

## Alternatives

- **Settings as properties on each tool:** simpler, but lost each time a tool is chosen, and each tool would need its own panel and its own export code.
- **A hand-made options panel per tool:** looks can be tuned per tool, but the M8 UI Pass can restyle the generic one, and M9's plugin tools get a panel for free.
- **Always centring on the pointer's cell:** simpler, but an even size would then always lean the same way, whichever side of the cell the pointer is on.

## Consequences

- The Brush and the Eraser keep separate sizes and shapes. Tools that should share them could later name the same group.
- Settings aren't remembered between runs until 3.5 exports them.
- **Below a window about 1,750 px wide,** the default bottom row (View, Tools, Edit) doesn't fit beside the widgets, so View and Edit shrink and scroll. The smallest window is 1,280 px. The owner will decide whether to change the layout, the toolbars or the minimum.
- A saved layout keeps its old places; Window → Reset Layout gives the new default. The old Brush Options toolbar's place isn't carried over, so Colors starts at its default.

Updated 2026-11-16: a tool's definition has a list of option groups (`OptionGroups`), not one, and may give a warning (`WarningFor`). A choice can have a detail and can be unavailable, and a number can be a percentage. A choice with more than two values shows as a dropdown. The Brush, FG Brush and BG Brush share one tip group, and the Eraser keeps its own (ADR 0049).

Updated 2026-11-17: the Eraser's tip group is shared by the Eraser, the Soft Eraser and the BG Eraser (ADR 0050).

Updated 2026-11-18: tool settings are remembered between runs, saved when the window closes (ADR 0052). A choice option can take more choices from a source, which is how the Ramp option lists the user's ramps.
