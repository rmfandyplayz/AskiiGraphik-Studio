<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0016: A character set per document

Status: Accepted

## Context

ARCHITECTURE §23 asked whether input is free Unicode or can be limited to printable ASCII. The owner decided at step 1.5: each document chooses **Unicode** or **ASCII only** when it's created, and can switch later from document options. Switching from Unicode to ASCII warns that every non-ASCII character will be removed. The New dialog and document options are the owner's UI to design (step 1.6 and later).

## Decision

- `Document.CharacterSet` is `Unicode` (the default) or `Ascii`. It's set by `Document.Create` and changed only by `SetCharacterSetCommand`, so switching is one undo step. Change events report it as `SettingsChanged`.
- **ASCII** means U+0020 to U+007E: the space and the printable characters.
- **Unicode** means any character that fills a cell on its own. Control characters are never stored (§6), and neither are format characters, combining marks or line and paragraph separators (Unicode categories Cc, Cf, Mn, Mc, Me, Zl, Zp): alone in a cell they'd be invisible or drawn on a dotted circle. Private-use characters (icon fonts) are allowed.
- **The document never holds a glyph its set doesn't allow.** `CellEditBuilder.Set` throws for one, so every edit path is covered, and the text tool skips such characters as they're typed.
- **Switching to ASCII** empties (not blanks) every cell holding a non-ASCII glyph, in every layer, and undo puts them back. `CountCellsToRemove` lets the UI say how many first; with none, there's nothing to warn about.
- Until the owner designs the UI, **Edit → ASCII Only** is a temporary check-mark item, and the status bar shows the current set.

## Alternatives

- **One app-wide setting:** simpler, but an ASCII-only piece and a Unicode piece couldn't be open together, and the file wouldn't say which it is.
- **Replacing removed characters with spaces, or a look-alike (é → e):** keeps more of the drawing, but spaces hide the layers below, and look-alikes are a guess. The owner asked for removal.
- **Checking only at input:** pasting, importing and loading would each need their own check. Checking in the builder covers them all.

## Consequences

- The file format (step 1.7) stores the character set, and loading an ASCII document that holds other characters is a corrupt file.
- Paste, import and image conversion (M4, M6, M7) must filter or map characters to the document's set before building cells.
- Wide characters (CJK) can be typed in a Unicode document. They fill one cell, drawn small with a fallback font and the orange marker; how to flag them is still open (§23).
