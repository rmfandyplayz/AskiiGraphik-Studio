<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0048: Floating toolbars keep their nearest corner

Status: Accepted

## Context

A floating toolbar saved where its top left went, so it always grew right and down. Step 3.2 made toolbars change size as the active tool changes (Tool Options), and the owner found it odd that a toolbar floated near the bottom right still grew from its top left (2026-11-16 testing notes). They suggested pinning it by its middle, or by the corner of the quarter of the canvas region it's in. They chose the corner.

## Decision

- `ToolbarPlacement` gains `FloatingPivot`, a `ToolbarCorner`: which of its corners `FloatingAt` holds.
- A drop picks the corner from the quarter of the canvas region (inside the margin) that the toolbar's middle is in, where it landed. A middle exactly on a centre line counts as top or left. `ToolbarDrag.LayoutAt` does this, so `LayoutEdits` stays free of solver types.
- The solver puts that corner at `FloatingAt`, then clamps into the canvas region as before.
- The pivot stays the same until the toolbar is dropped again. Docking a toolbar resets it to `TopLeft`, and so does a nonsense saved value.
- Floating a toolbar any other way (showing a hidden one whose anchor is taken) pins its top left, as before.

## Alternatives

- **Pin the middle.** It grows both ways, so a toolbar near an edge pushes into it and gets clamped back, which moves its other side too.
- **Pick the corner afresh on every solve.** The toolbar would jump between corners as it crossed a centre line while growing.

## Consequences

- Old saved layouts have no pivot, read as `TopLeft` and behave as before.
- A toolbar pinned by its bottom right grows up and left, so it stays clear of the edges it's nearest.
