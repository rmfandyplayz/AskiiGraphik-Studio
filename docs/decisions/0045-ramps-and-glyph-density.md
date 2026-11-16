<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0045: Ramps and glyph density

Status: Accepted

## Context

Step 3.1 starts M3 with the two things its brushes, soft eraser and ramp editor share (ARCHITECTURE §7): a ramp that turns an intensity into a glyph, and a measure of how dark each glyph looks in a font. The owner agreed the ramps to ship in M3's planning (2026-11-16): Standard, a letters-and-digits ramp of about 20, Fine, Blocks and Braille. Since 2b.2 the canvas draws box and block characters fitted to the cell (ADR 0039) and missing glyphs from a fallback font, so a font file's own outlines don't say how dark a glyph looks on screen.

## Decision

- **`Ramp` (Core) is an immutable value:** glyphs from lightest to darkest, plus what intensity 0 paints (an empty cell or a space). A ramp of n glyphs has n + 1 levels at even steps, k / n, and an intensity paints the nearest level, halfway going to the darker one. So 0 is always no ink, 1 is always the darkest glyph, and a level's intensity maps back to that level. A ramp can't hold a space (that's the zero level's), the same glyph twice, or a character that can't fill a cell. Equality is by glyphs and zero level.
- **Names live where ramps are kept,** not in `Ramp`. Built-in ramps are `BuiltInRamp` records with a stable id and the App's resource key for the name, as tools' descriptors do. User ramps get their names and ids with the ramp editor (3.5).
- **The order is the user's.** Measured darkness sorts a ramp on request (`SortedByDensity`), and later matches glyphs that aren't in the ramp, but it never moves the levels.
- **`IGlyphDensityProvider` (Core) gives one font's densities;** `GlyphDensityMeter` (Imaging) measures them by drawing each glyph alone through the one `GlyphRasterizer` into a cell-sized alpha bitmap at 64 px per em, and taking the share of the cell its ink covers. So block characters measure exactly as fitted (▒ is 50%), and a missing glyph measures as the fallback font or placeholder box the canvas shows. Each glyph is measured once per font; `GlyphDensityCache` keeps a meter for each of the 4 most recently used fonts. Both are UI-thread only.
- **The built-in ramps** paint an empty cell at 0. Standard is the classic `.:-=+*#%@`. Standard Alphanumerical (20 letters and digits) and Fine (the 69 characters of the well-known 70-level ramp) were ordered by measuring the four bundled fonts and averaging, which cut the out-of-order pairs about fivefold; Standard Alphanumerical is in exact order in JetBrains Mono. Blocks is `░▒▓█`, and Braille goes from one dot to all eight.

## Alternatives

- **Levels placed at each glyph's measured density:** truer to the eye for image conversion, but a ramp would behave differently in every font, and a brush's intensity slider would jump unevenly. M6 can still map by density.
- **Equal-width bands, 1 / (n + 1) each:** the zero level would take a whole band, and a level's intensity wouldn't come out as a round number.
- **Measuring from the font's outlines:** misses the fitting of block characters and fallback fonts, so `░▒▓█` and missing glyphs would measure wrong.
- **Measuring at the canvas's current size:** results would change with zoom. At 64 px, rounding barely matters.

## Consequences

- No bundled font has braille, so the Braille ramp is drawn from a fallback font, and its cells are flagged as missing glyphs.
- Text characters top out at about a quarter of the cell (`@` is 25% in JetBrains Mono), while blocks reach 100%, so mixing the two in one ramp needs care.
- In every bundled font `-` measures lighter than `:`, so Standard's classic order is slightly off. It's kept as agreed.
