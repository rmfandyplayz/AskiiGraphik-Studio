<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0036: The colour model

Status: Accepted

## Context

M2b adds colour (ADR 0032). The owner decided on 2026-11-14 that a document is monochrome, ANSI (the standard 16 colours) or RGB. Each cell has a text colour and a background, and either can be none. Backgrounds composite on their own. The base app has no text attributes. The mode can be changed later, after a warning, and new documents start monochrome. The owner may also remove the per-layer "spaces are empty" option (ADR 0033) one day.

## Decision

- **`CellColor` is a 4-byte value** holding a kind (unset, ANSI or RGB) and the colour. Unset means the canvas's own ink or paper, and exports with no colour code. `AnsiColor` lists the 16 in escape-code order, and `RgbColor` is three bytes. A `Cell` still fits in 16 bytes.
- **`Document.ColorMode` is Monochrome, Ansi or Rgb.** A document never holds a colour its mode doesn't allow. `CellEditBuilder` refuses one, and a file that holds one is damaged.
- **A cell holds a glyph, a background, both or neither.** `HasGlyph` replaces `IsOccupied`, and `IsEmpty` now means neither. Only a cell with a glyph can have a text colour. A cell holding only a background colours the cell without hiding the glyph below it.
- **Compositing works in two planes.** The glyph and its text colour come from the topmost visible cell that hides the glyphs below it (the space policy). The background comes from the topmost visible cell that has one. So a coloured space on a "spaces are empty" layer still shows its background, which settles ADR 0033's open point. Merge Down still keeps the picture for every combination of options.
- **Removing a glyph keeps its background.** For now that happens when a document switches to ASCII (ADR 0016) and in Merge Down. The hard eraser still empties the whole cell.
- **ANSI colours are shown with the VGA palette,** which is also used to convert between modes. RGB to ANSI picks the nearest of the 16 by the "redmean" distance, with a tie going to the lower number. ANSI to RGB uses the palette's value. Switching to monochrome removes every colour. `SetColorModeCommand` converts every cell in every layer as one undo step. `CountCellsThatLookDifferent` lets the UI warn first (2b.3).
- **There are no text attributes.** `CellAttributes` stays as a hook for plugins and is always None.
- **Nothing depends on the space option except the glyph plane,** so removing it later stays a small change.
- **The RGB picker will be Avalonia.Controls.ColorPicker** (2b.3). If it can't be customized enough, a picker of our own replaces it.

## Alternatives

- **Painting a background as a coloured space:** Merge Down could no longer keep the picture when the lower layer treats spaces as empty.
- **Compositing colour with its glyph, as one plane:** a glyph above would hide every colour below it, so a background couldn't be painted under text on another layer.
- **256 colours or a per-document palette:** not asked for. The 16 plus RGB cover ANSI art, and Discord's colours belong to its plugin.
- **Keeping RGB colours and converting them only at export:** the document would hold colours its mode can't show.

## Consequences

- Every tool from M3 on writes the current colours, within the document's mode.
- The canvas (2b.2) draws backgrounds as their own pass, under the glyphs.
- RGB to ANSI loses detail. The warning count lets the user back out, and undo restores the colours.
- Code that asks "does this cell have a glyph?" uses `HasGlyph`. `IsEmpty` is only for "does this cell hold anything?".

Updated 2026-11-14: the text tool writes the current colours from step 2b.3a, ahead of M3's tools. The picker is Avalonia's `ColorView`, in the Color window, shown only for RGB documents (ADR 0042).
