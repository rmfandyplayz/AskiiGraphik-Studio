<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0043: The colour brush

Status: Accepted

## Context

Step 2b.3b adds a Brush tool (B) that paints the FG colour as text colour or the BG colour as background, one cell at a time, with an FG brush and a BG brush on its sub-toolbar (keys 1 and 2), and Alt+click (Option+click) to pick a colour up. The owner asked for the FG/BG brush choice and the Brush Options choice to be one choice for now, made so it can be split later. Picking takes what you see (all visible layers combined), and only the colour the brush paints.

## Decision

- **One choice, in Core.** `PaintColors.Target` says which colour is chosen. The Color and Swatches windows change it, the Brush paints it, and the Brush Options toolbar, the Brush's sub-toolbar and keys 1 and 2 all set it. It moved out of `PaintColorsViewModel` because a Core tool needs it. To split the two later, the Brush gets a choice of its own beside it in `PaintColors`, not in the tool, because a tool is made afresh each time it's chosen.
- **`BrushTool` (Core) paints like the eraser:** one cell wide along a Bresenham line, each move its own command, the moves coalesced into one step named Paint. Esc during a stroke takes it back. A stroke keeps the colour and brush it started with. The FG brush skips cells without a glyph, since there's no text to colour. None takes the colour away: the text goes back to the canvas's ink, or the background goes. Locked and hidden layers, and monochrome documents, are left alone.
- **Picking** samples `Compositor.CompositeCell`, so it ignores hidden layers and works whatever the active layer's state. A cell with no glyph, or no background, gives None. Dragging with Alt held keeps picking, Esc gives back the colour from before (on Windows, Alt+Esc is the system's, so Alt is let go first), and a monochrome document picks nothing, so the chosen colour isn't lost. Picking isn't an edit, so it leaves no undo step.
- **Keys 1 and 2 are the Brush's.** It takes them in `TextEntered`, so they work only while it's chosen, and with any keyboard that types the digits. They aren't action shortcuts, for the same reason as the tool letters (ADR 0033).
- **The sub-toolbar** is two action buttons, FG Brush and BG Brush, built like any toolbar. `BrushSubToolbar`, one per main window, shows it beside the Tools toolbar while the Brush is chosen and hides it for other tools.
- **Alt+click doesn't open the menu bar.** Avalonia (12.1.4) opens the menu when Alt is let go, unless another key went down in between; a click doesn't count. So an Alt+click on the canvas raises a key-down for `Key.None`, which the access key handler counts as that other key and nothing else responds to (`AltClickMenuGuard`). The handler hides the menu bar's underlined letters on a click, but Windows repeats a held key and each repeat of Alt underlines them again, so the guard hides them after each repeat until Alt goes up. Tests catch it if Avalonia changes.

## Alternatives

- **Two choices from the start:** the owner chose one, and keeping one in Core makes splitting it a small change.
- **Keys 1 and 2 in `CanvasViewModel`, like X and D:** the canvas would have to know which tool owns which digit. A tool's own keys belong to the tool.
- **Swallowing the Alt release:** the access key handler would still think Alt was held, and the next Alt tap after a keypress wouldn't open the menu. Replacing the handler needs Avalonia's private service locator.
- **Picking from the active layer only:** the owner chose what you see.

## Consequences

- Right-click does nothing yet. Brush sizes and shapes come with M3's brushes.
- There's no eyedropper pointer while Alt is held; Avalonia has none built in.
- The guard can go when Avalonia counts a click as using Alt (its newer code is still the same here).

Updated 2026-11-15: the guard also keeps the menu bar's letters from staying underlined after an Alt+click, which the owner's testing found, and Esc while picking needs Alt let go first on Windows.

Updated 2026-11-16: the Brush Options toolbar is now the Colors toolbar (ADR 0047).
