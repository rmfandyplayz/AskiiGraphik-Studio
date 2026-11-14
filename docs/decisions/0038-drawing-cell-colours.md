<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0038: Drawing cell colours

Status: Accepted

## Context

Step 2b.2 draws each cell's text colour and background (ADR 0036). The canvas draws its glyphs with one `DrawAtlas` call per alpha-only atlas page, tinted by the paint's colour (ADR 0015), and has to stay smooth on a busy 500 × 500 document (§21). A zoomed-out frame shows up to 250,000 cells, and in an RGB document each can have a colour of its own.

## Decision

- **Text colours** go in a colour array beside the sprites, pooled the same way, in the same `DrawAtlas` call. With a white paint and `SKBlendMode.Modulate`, each glyph is its own colour where the page covers it. The pages don't change, and it's still one call per page.
- **Backgrounds** are one image per frame, with a pixel for each visible cell, drawn stretched over those cells without smoothing. Cells are whole device pixels (§12), so each pixel fills its cell exactly, with no seams. The builder writes the pixels into a buffer it keeps between frames, and clears it only in frames that have a background; a frame without one has no image.
- **Unset colours** follow the theme: text in its ink, and no background, so the paper shows. Set colours are drawn as stored in every theme, ANSI ones through the VGA palette.
- **Draw order:** pasteboard, paper, backgrounds, the missing-glyph flags, glyphs, grid, hover, caret. The flags cover the background for now, so the error stays visible (a temporary marker, ADR 0013).

Scene building on the UI thread, Release, Windows, the whole 500 × 500 in view at 2 × 5 device pixels a cell: the monochrome sample took 2.5 ms before and 2.8 ms after; the colour sample, with every glyph coloured and half the cells on a background, takes 5.5 ms.

## Alternatives

- **A batch per page and colour:** an RGB picture with thousands of colours would need thousands of `DrawAtlas` calls. The colour array costs the same whatever the number of colours.
- **A rectangle per background,** even merged into runs along each row: up to 250,000 draw calls when neighbours differ, each one a call into native code.
- **Background tiles in `DrawAtlas`:** its transforms scale both ways equally, so each tile would have to be a cell-sized solid sprite, and the GPU would get four vertices per cell instead of one small texture.
- **RGBA atlas pages with the colour baked in:** a copy of a glyph for every colour it's drawn in.

## Consequences

- A frame with backgrounds copies and uploads an image of up to a pixel per visible cell (1 MB for all of a 500 × 500), which the GPU draws as one rectangle. If that ever shows up in a profile, the image can be kept until the composite or the visible cells change.
- The painter's tests check exact pixels on a CPU bitmap. The GPU backends (Direct3D through ANGLE on Windows, Metal on macOS) are checked by hand with the colour sample in the Debug menu.
