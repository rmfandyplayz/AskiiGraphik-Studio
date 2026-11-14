<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0037: Colour in the file format (version 2)

Status: Accepted

## Context

The colours of ADR 0036 have to be saved. ADR 0021 says every format change bumps the version and adds a migration. The plain variant should stay readable in a text editor (ARCHITECTURE §13).

## Decision

- **Format version 2.** The top level always has `"colorMode"`: `"monochrome"`, `"ansi"` or `"rgb"`. `AddColorModeMigration` gives a version 1 file `"monochrome"`, since version 1 had no colour.
- **A layer may have `"foreground"` and `"background"` arrays,** each written only when the layer has a colour of that kind. Like the occupancy, each is on one line. It alternates counts and colours, covering the layer's cells row by row. A colour is `null` (none), an ANSI name in camelCase (`"brightRed"`), or `"#rrggbb"`. RGB is written in lowercase and read in either case.
- **The rows still show only glyphs.** A cell holding only a background is a space in its row, and the occupancy says it has no glyph.
- **These make a file damaged:**
  - colours in a monochrome document, or a colour the mode doesn't allow;
  - an array of odd length, or a count below 1;
  - runs that don't cover the layer exactly;
  - an unknown colour;
  - a text colour on a cell without a glyph.

## Alternatives

- **Only adding optional fields, without a version bump:** an older app would open a coloured file and silently drop its colours on the next save.
- **Leaving `colorMode` out at monochrome, like the reserved fields:** it isn't reserved, and writing it makes every file say which mode it's in.
- **Escape codes inside the rows:** a text editor would show a mess instead of the art.
- **Numbers for colours:** shorter, but the plain file wouldn't say which colour is meant.

## Consequences

- An app that only knows version 1 refuses version 2 files with its "made by a newer version" message.
- A busy RGB layer has long colour lines. The compressed variant keeps the file size down.
- ANSI import (M7) maps into these same colours.
