<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0042: The current colours

Status: Accepted

## Context

Step 2b.3a gives the user colours to paint and type with. The owner asked for an FG and a BG colour, like the two colours of an image editor, and a Brush Options window whose two buttons choose which of them the Color window (an RGB picker) and the Swatches window (the 16 ANSI colours) change. X swaps the colours, and D sets both to none, marked as temporary. Typing writes the FG colour as the text's colour and the BG colour as its background. When the BG colour is none, typing keeps the cell's own background.

## Decision

- **`PaintColors` (Core) holds the FG and BG colours** for the whole app, shared by every document and tool. They're kept as chosen (none, ANSI or RGB) and converted to the document's colour mode when used (`ForegroundIn`, `BackgroundIn`). A colour chosen in one document survives a visit to another, and a monochrome document gets no colours.
- **Tools are created from a `ToolContext`:** the editing session and the colours. Shared state for tools later (a ramp, a brush size) adds a property rather than a parameter to every tool's factory.
- **The text tool** writes the current colours. Backspace and Delete remove only the glyph and keep the cell's background, as removing a glyph does elsewhere (ADR 0036). The hard eraser still empties the whole cell.
- **X and D are canvas letters,** under the same rules as the tool letters (ADR 0033): they type while the caret is placed.
- **The three windows share one `PaintColorsViewModel`.** Which colour the windows change is kept there, not in Core, because only the windows use it. Their buttons never take keyboard focus, so the canvas keeps the keyboard. The swatches are hidden in a monochrome document, and the picker shows only in an RGB document; in an ANSI one, its dot would jump between the 16 colours as you dragged.
- **The picker is Avalonia.Controls.ColorPicker's `ColorView`,** with its alpha, palette and accent parts hidden. Its colour is set from code under a flag, and only the user moving it chooses a colour. While the colour is none, it shows the last colour picked, white at first.
- **The colour mode** is chosen in the New dialog (monochrome by default) and switched with three check-mark items in the Document menu, which ask first when `CountCellsThatLookDifferent` finds cells that would change.

## Alternatives

- **Colours per document:** switching documents would change the brush, unlike other editors.
- **Binding the picker two-way:** opening the window would write the colour shown for none back as a choice, without the user touching anything.
- **An Edit → Color submenu,** as the roadmap first said: the menus can't hold submenus yet, and the Document menu already has ASCII Only, which works the same way.

## Consequences

- The brush (2b.3b) reads the same colours from its `ToolContext`.
- An ANSI swatch chosen in an RGB document stays ANSI, is written as its VGA value, and shows as that hex code in Brush Options.
- If Avalonia's picker can't be styled enough, only `ColorPanelView` changes.
- D goes or moves when the owner designs the shortcuts.
