<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0044: ANSI export

Status: Accepted

## Context

Step 2b.4 adds File → Export as ANSI… and Edit → Copy as ANSI, so art in colour can be shown in a terminal: UTF-8 text with escape codes, 16-colour codes for an ANSI document and 24-bit ones for RGB (ARCHITECTURE §16). The owner chose to put both beside their Text versions, with no shortcut for Copy as ANSI, and to let them work on a monochrome document too, where they write the art with no codes.

## Decision

- **`AnsiExporter` (Core)** is an `IExporter`, registered like `TextExporter`. It flattens the visible layers the same way: every row the canvas's full width, empty cells as spaces, UTF-8 without a byte-order mark, `\n` after every row in a file, and this OS's line breaks with none after the last on the clipboard.
- **Codes follow the colour, not the mode.** A document only holds colours its mode allows (ADR 0036), so an ANSI colour gets its 16-colour code (text 30–37 and 90–97, backgrounds 10 more) and an RGB colour its 24-bit one (`38;2;r;g;b`, `48;2;r;g;b`). The bright colours use 90–97 rather than bold, which today's terminals read as bright and some draw as bold too.
- **Unset colours get no code**, so they're the terminal's own text colour and background (§5.11). When a colour goes back to unset, the code is 39 or 49.
- **A code only where a colour changes**, with the text colour and background in one code when both do. A cell without a glyph shows no text colour, so it leaves the text colour as it was.
- **Each row that set a colour ends with a reset (`ESC[0m`)** before its line break. Otherwise a background runs on to the terminal's edge, and a row's colours carry into the next.
- **Two Debug samples**, Open ANSI Test Sample and Open RGB Test Sample, are small enough for an 80 × 24 terminal, so the owner can check the export without drawing a test.

## Alternatives

- **Codes from the document's mode,** converting each colour: the same output, with a conversion that can't change anything.
- **Bold (`1;31`) for the bright colours:** what old terminals needed, but it makes the text bold where a terminal draws it, and a background can't be bright that way.
- **A reset after every cell, or a full reset (`0`) for each change:** simpler, but several times longer and harder to read in an editor.
- **Trimming each row after its last coloured cell:** shorter files, but the art wouldn't keep its shape where it's pasted, unlike text export (ADR 0022).

## Consequences

- The 16 colours look as each terminal's own palette shows them, not as the canvas's VGA palette does. 24-bit colours look the same everywhere that supports them; macOS Terminal does from macOS 26.
- Pasting Copy as ANSI at a shell prompt doesn't show colours: the shell takes it as typing. It's for apps and files that read the codes.
- These `.ans` files are UTF-8, for terminals. Old ANSI art viewers expect CP437 and a SAUCE record, which M7's classic export adds, so both exports will use `.ans` and the menu names will have to tell them apart.

Updated 2026-11-21: while anything is selected, Copy as ANSI copies only the selection's bounding rectangle, with unselected cells as plain spaces (ADR 0067).
