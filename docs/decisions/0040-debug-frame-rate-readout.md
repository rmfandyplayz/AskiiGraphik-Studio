<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0040: The Debug frame-rate readout

Status: Accepted

## Context

After step 2b.2 the owner asked for a frame-rate readout in the status bar, with the average and the 1% low, to watch smoothness while testing. The owner's machines are fast, so a hitch that would show on a slower one is easy to miss. But Avalonia redraws only when something changes. A plain count of frames a second then measures how often things change (how fast the mouse moves, the key repeat rate), and a wait while nothing changes would look like a very slow frame.

## Decision

- **What counts as a frame:** each time Avalonia's render thread paints the canvas (`CanvasDrawOperation.Render`). That includes repaints when something drawn over the canvas changes, since those reach the screen too. The time between two paints is a frame's time. A wait longer than 100 ms means nothing changed, so it isn't counted.
- **What it shows,** from the last 2 seconds and refreshed twice a second: the average frames a second, the 1% low (the average rate of the slowest 1% of frames, or the slowest frame when there are fewer than 200), and the slowest draw, the most time the canvas itself spent building and painting one frame. The slowest draw doesn't depend on how fast things change, so it shows a slow frame even when the rates can't. With no two frames close enough to make a rate, it says the canvas is idle.
- **Cost:** recording happens only while the readout shows. It writes into a fixed ring of 4,096 frames under a lock, so it allocates nothing per frame. The readout exists only in Debug builds, under Debug → Show Frame Rate, and starts off.

## Alternatives

- **Avalonia's own overlay** (`RendererDiagnostics.DebugOverlays`): it counts every frame the window draws and shows a frame-time graph, but it sits at the window's top left, over the canvas, and has no 1% low.
- **Animating the canvas continuously while measuring:** gives a true rate, but measures a canvas that never happens in use and costs power.
- **Measuring the time from a change to the screen:** a good measure of responsiveness, but it isn't a frame rate, which is what the owner asked for.

## Consequences

- The rates mean the most while something changes all the time, such as panning with a steady drag. Pauses shorter than 100 ms in the movement count as slow frames and lower the 1% low.
- The slowest draw is CPU time. Skia's work on the graphics card happens after the canvas has painted, so it isn't included.
