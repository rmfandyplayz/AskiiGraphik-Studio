<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0015: Canvas rendering

Status: Accepted

## Context

ARCHITECTURE §12 left the drawing approach to step 1.4, to be chosen by measurement: atlas bitmaps through Avalonia's `DrawingContext`, or a custom draw operation with a direct Skia lease. The canvas must stay smooth on a 500 × 500 document (§21), crisp at any display scaling, and redraw only what changed (§5.9).

## Measurements

A headless Avalonia window, rendering with Skia on the CPU (Release build, Windows; milliseconds per frame, lower is better):

| Viewport, cell size (visible cells) | `DrawingContext.DrawImage` per cell | Skia lease, one `DrawAtlas` per page |
|---|---|---|
| 1920 × 1080, 10 × 21 (9,800) | 32 | 22 |
| 1920 × 1080, 3 × 5 (108,000) | 138 | 33 |
| 3840 × 2160, 10 × 21 (39,000) | 99 | 83 |
| 3840 × 2160, 3 × 5 (216,000) | 266 | 72 |

Per-cell `DrawImage` also spends 13–38 ms just recording its operations on the UI thread, against 1–2 ms for the batch. On the GPU both are faster, and the gap stays.

## Decision

- **Skia lease and `DrawAtlas`.** `CanvasControl.Render` builds a `CanvasScene` on the UI thread, and a `CanvasDrawOperation` paints it on Avalonia's render thread through `ISkiaSharpApiLeaseFeature`. Glyphs from one atlas page go out in a single `DrawAtlas` call. The pages stay alpha-only, so the paint's colour tints them (verified by test).
- **Device pixels throughout.** The font is rasterized at `font size × zoom × display scaling`. The draw operation drops Avalonia's scale and snaps its offset to a whole pixel, so every cell edge lands on the device's pixel grid.
- **Zoom** has fixed steps from 25% to 800%. Each step scales the font size, and the font metrics round the cell to whole pixels. The wheel zooms around the pointer; shortcuts zoom around the centre.
- **Only visible cells** are read. A `CompositeCache` holds the composited frame and re-composites only the regions in each change event. Avalonia repaints the control as a whole, so each frame redraws the visible cells from the cache and the atlas.
- **Threading.** The scene owns everything it draws: pooled sprite arrays, and reference-counted copies of the atlas pages, refreshed when a page's version changes. The UI thread can keep editing the document and atlas while a frame is being painted.
- **Scrolling** is two plain `ScrollBar`s bound to the control's offsets, in device pixels, rather than a `ScrollViewer`, because zooming around a point needs the control to own the offset. Content is the canvas plus a margin; a canvas smaller than the window is centred.
- **Missing glyphs** are flagged with a tinted cell background: orange for a fallback font, red for a placeholder. It's a temporary marker until the owner designs one (ADR 0013).

## Alternatives

- **`DrawingContext.DrawImage` per cell:** slower in every case above, needs RGBA pages, and can't tint them per colour.
- **A `ScrollViewer` with `ILogicalScrollable`:** gives native scrolling, but zoom anchoring would fight the viewer for the offset.
- **A tile cache of pre-drawn regions:** would make panning cheaper still. It's not needed at the measured cost, so it can wait for a measurement that asks for it (§12).

## Consequences

- The canvas depends on Avalonia's Skia backend, which every desktop platform uses. Headless tests have no Skia, so they test input and maths, and the painter is tested on a plain Skia bitmap.
- Space + drag pans only when the active tool allows it; the text tool doesn't (ADR 0017).
- How wheel events become scrolling or zooming is in ADR 0018.

Updated 2026-10-31: Space + drag depends on the tool, and the wheel works in gestures (step 1.5).
