<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0033: The space option, tool switching and the hard eraser

Status: Accepted

## Context

Step 2.4 adds the per-layer option that makes spaces see-through (chosen in M2 planning), a way to switch tools, and the first drawing tool. ARCHITECTURE §8.1 says a gesture emits exactly one command when it ends. ADR 0017 says tool shortcuts must not fire while the text tool is typing.

## Decision

- **`SpaceHandling` is a layer property.** `LayerProperties.SpaceHandling` defaults to spaces hiding what's below. The file writes `"spaces": "empty"` only when it's on, so format version 1 stays. `CompositingPolicy.For` gives the shared policy for each choice, and the compositor reads each layer's own, so its callers no longer pass a policy.
- **Merge Down keeps the picture whatever the options are.** Copying a hiding space onto a layer with see-through spaces would stop it hiding. In that one case the merged layer takes the upper layer's option, and the lower layer's own spaces become empty, which looks the same as they did.
- **Tools are chosen in `CanvasViewModel.SelectTool`,** and the tool stays chosen when New or Open replaces the document. `ToolActions` makes one checkable action per tool in the registry, so a tool registered from elsewhere gets one too (ADR 0032).
- **Shortcut letters live in the canvas, not the action system.** Single-letter action shortcuts would fire in every text box, and on a Mac the menu bar would take the key from all of them. `CanvasView` handles a letter only when the canvas has focus, no modifier is held, and `ITool.IsTakingText` is false. For the text tool that means no caret is placed, so Esc gets from typing to the shortcuts. T is Text and E is Eraser.
- **The eraser applies as you drag.** Each pointer move is a `CellEditCommand`, and a stroke's commands share a `CoalescingGroup`, like a burst of typing. A line between reported cells (`CellLine`, Bresenham) keeps a fast drag from leaving gaps. It works on the active layer and leaves locked or hidden layers alone. Space + drag pans.
- **Esc cancels a stroke** through `CommandHistory.CancelNewestStep`, which undoes the step and forgets it, so History shows nothing. The eraser checks that the newest step is still its own.
- **Losing pointer capture ends a gesture.** The canvas now tells the tool, because otherwise the next hover would keep erasing.

## Alternatives

- **A preview overlay, committed once when the stroke ends (§8.1's original wording):** the composite cache would need to pretend cells are empty during the stroke, which is a lot of machinery for no visible gain. §8.1 now allows both. Brushes in M3 may still want a preview.
- **Disabling Merge Down when the options differ:** simpler, but it blocks a harmless operation for a rare case.
- **Tool letters that work while typing, using a modifier:** would need a free modifier on every platform, and the letters are the convention in drawing apps.
- **A tool switch that keeps the text caret:** the caret would still be taking letters after switching back, and it was simpler to treat a switch as Esc.

## Consequences

- A long stroke merges its edits into one command as it goes, which costs time proportional to the stroke so far on each move. That's fine for a 1-cell eraser. Large brushes in M3 should measure it.
- The tool letters aren't shown in the menus, because menu shortcuts are what they must avoid.
- A coloured space is visible, so colour (M2b) has to decide whether the option still applies to one (ADR 0032).
