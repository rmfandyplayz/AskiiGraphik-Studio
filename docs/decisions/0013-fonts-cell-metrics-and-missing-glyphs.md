<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0013: Fonts, cell metrics and missing glyphs

Status: Accepted

## Context

ARCHITECTURE §11 lists the bundled fonts, asks for loading from files and the system, cell size from font metrics and a monospace check, and left missing glyphs open (§23). In step 1.3 the owner chose: only the Regular weight is bundled; a missing glyph is drawn with an installed fallback font, or a placeholder box if none has it, and the cell is flagged; a fallback glyph that's too big is shrunk to fit.

## Decision

- **Bundled fonts:** JetBrains Mono 2.304 (default), Roboto Mono 3.001, IBM Plex Mono 2.5.0 and Fira Code 6.2, Regular only, all OFL-1.1 (Roboto Mono moved from Apache-2.0 to OFL). The TTFs are embedded resources, so loading never depends on the install folder. Each license file is copied next to the app under `Licenses/Fonts/<Family>/`.
- **Loading** (`FontLoader`): bundled, from a file (first face of a TTF, OTF or collection; a non-font throws `FontLoadException`), or an installed family by name (its regular style). An unknown name returns null, including when the OS substitutes another family. Listing system families reads names only; the monospace check runs when a font is loaded.
- **Cell size** (`CellMetrics`): width = the unhinted advance of `0` (as CSS's `ch` unit), height = ascent + descent + line gap, each rounded to whole pixels. Half the line gap goes above the text. Ascent, descent and line gap are **read from the font's tables** (OS/2 "typo" values when the font asks for them, otherwise "hhea"), because Windows, macOS and Linux font engines each pick different tables. That keeps cell size identical on every OS.
- **Monospace check:** every printable ASCII glyph the font has is compared with `0`; a difference above 1% of the em is a mismatch. The result lists the mismatched glyphs; the font is still usable.
- **Missing glyphs** (`IFallbackFontProvider`): if the document's font lacks a glyph, `SystemFallbackFontProvider` asks the OS for an installed font that has it (and checks that it really does). If none does, the rasterizer draws a placeholder box. The `GlyphSource` result (primary, fallback or placeholder) lets the canvas flag the cell; how it's flagged is the owner's UI design (§23).

## Alternatives

- **Bundling a wide-coverage fallback font** (for example Noto Sans Mono): the same result on every machine, but no CJK or emoji and a bigger download. The owner chose system fallback.
- **The OS engine's own vertical metrics:** less code, but a document's cell height (and its PNG export size) would differ between Windows and macOS.
- **Rounding the cell width up:** avoids clipping a fraction of a pixel, but leaves gaps between box-drawing characters.

## Consequences

- A fallback glyph found on one computer may be a placeholder on another; the cell is flagged either way.
- The document's font reference (§5.10) and the font picker arrive in step 1.6.
