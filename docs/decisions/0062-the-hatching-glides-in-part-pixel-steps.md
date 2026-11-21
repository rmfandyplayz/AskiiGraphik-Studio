<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0062: The hatching glides in part-pixel steps

Status: Accepted

## Context

After testing step 4.2b, the owner saw the moving hatching (ADR 0061) flicker at the edge of their vision, but not when looking straight at it. It was clearest on Windows, fainter on a Mac, and showed in both themes. Each hatching was a tile of 1-pixel lines shifted along a row a whole device pixel at a time, which made 15 to 35 jumps a second at the speeds then used.

Each jump moved every line on the canvas at once by its full width. The jumps also fell unevenly on a 60 or 120 Hz screen: sometimes two frames apart, sometimes three. Peripheral vision sees fine detail poorly, but it reacts to sudden changes across a wide area more than central vision does. So the edge of the eye saw the jumps flicker, while the centre followed them as motion. A Retina pixel is half as big, so each jump was half as big there, which fits the fainter flicker on the Mac.

## Decision

- **Part-pixel steps:** the hatching moves in steps of a fraction of a device pixel, set by `AppCanvasSelectionHatchStepsPerPixel` (8 for now). It moves a little on nearly every frame instead of a whole pixel every few frames.
  - `SelectionMotion` gives the hatching's shift as a fraction of a pixel, rounded down to a whole step.
  - The painter samples the tile with linear smoothing, so a line part-way across a pixel spreads over the two pixels it lies across. A whole-pixel shift still lands exactly on the pixels.
- **Thicker lines:** the lines are twice as thick and twice as far apart, at 2 and 16 logical pixels.
  - A 1-pixel line moving by part of a pixel shimmers: it's sharp when it sits on a pixel and smeared over two between pixels.
  - A thicker line changes only at its edges.
- **The dashes still jump:** the outline's dashes still run a whole pixel at a time. They're thin and short, and smoothing them would blur them.

## Alternatives

- **Thicker lines that still jump a whole pixel:** this was the owner's first guess. Every line would still move at once on each jump, so it would help less. Setting the steps to 1 shows how this looks.
- **Slower hatching, or still hatching:** the owner asked for moving hatching, and then for it to move faster.
- **Redrawing the lines at each new position:** this looks the same as smoothing the tile, but redraws the tile every frame.
- **Moving the hatching with Avalonia's compositor instead of repainting:** the hatching is cut to the cells inside the canvas's own Skia drawing, so it would need a separate layer and clip that both have to stay in step with the canvas.

## Consequences

- **More repaints while something is selected:** the canvas repaints on nearly every display frame instead of 15 to 35 times a second.
  - Each repaint draws only the visible cells, but leaving a selection showing uses more power.
  - M10's profiling pass will check this. Fewer steps per pixel would reduce it.
- **Easy comparisons:** the step count, thickness and spacing are tokens. The owner can compare the old jumps (steps set to 1) and thin lines (thickness 1, spacing 8) by editing `Tokens.axaml`.

Updated 2026-11-20 (step 4.3): the owner confirmed the flicker is gone. The lines became a zebra of dark and light stripes, 12 logical pixels each, set by one token, `AppCanvasSelectionHatchBandWidth`, in place of the thickness and spacing (ADR 0064). The part-pixel steps are unchanged. An idle selection measured 1 to 1.5% CPU on the owner's i9-14900F, and 0.1% with nothing selected.
