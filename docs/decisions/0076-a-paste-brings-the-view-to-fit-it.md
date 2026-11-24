<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0076: A paste brings the view to fit it

Status: Accepted

## Context

Since 4.9b (ADR 0072), a paste partly out of view glided to the middle of the view only if it fitted at the current zoom. After 4.10a's testing the owner asked that any paste not fully in view bring the view to it and zoom to fit it with room round it, in to 150% at most and out to 25% at least, snapping to the usual zoom steps, gliding there zoom and all, with nothing else changing the zoom until it's done. Cells are whole device pixels at each zoom step (§12), so the zoom can't take the in-between sizes a smooth glide passes through.

## Decision

- **The fit.** `PasteFit.ZoomFor` takes the largest of `ZoomLevels.Factors` up to 150% at which the paste, plus the canvas margin (64 logical pixels) on every side, fits the usable area, or 25% if none does. Each step's cell size is measured as its atlas would measure it (`CellMetrics.Measure`), without building the atlas. A paste fully inside the usable area changes nothing, at any zoom.
- **The view.** `CanvasControl.BringIntoView` (which replaces `CentreOnIfPartlyHidden`) works out the view at that zoom with the paste in the middle, as far as the reach allows, sets the zoom factor and that view at once, so the pointer, the scroll bars and the status bar act as they will at the end.
- **The glide draws, the view doesn't move.** `ZoomGlide` gives, for each frame, a `ViewTransform` that scales and shifts the end's view to show how far the glide has got: the cell size changes by the same ratio every moment (geometrically), and the canvas spot in the middle of the view moves steadily from where it was to where it ends, eased out over `AppCanvasZoomGlideMilliseconds` (250 ms). Each frame builds its scene from a view at the end's zoom that covers what the screen shows, and `CanvasDrawOperation` applies the transform. Glyphs come from the end zoom's atlas, smoothed while scaled. A glide that keeps the zoom shifts by whole pixels only.
- **The lock.** While the glide runs, `CanvasControl.IsZoomLocked` is true, reported to `CanvasViewModel.IsZoomLocked`: the wheel doesn't zoom, and Zoom In, Zoom Out and Actual Size are greyed out and do nothing.
- **Ending early.** Scrolling, panning, a resize or anything else that rebuilds the view, and a gesture or an arrow held, end the glide at once at its end, which is where the view already is.
- **Reduced motion.** The new token is one of `ReducedMotion`'s, so with reduce motion on the view jumps.

## Alternatives

- **Gliding through real zooms:** every frame would need an atlas at its own pixel size, rasterizing every glyph again, and cells of fractional size leave seams.
- **Stepping through the zoom steps:** a handful of jerky frames, not a glide.
- **Moving the view itself frame by frame:** the view can only sit at whole cell sizes, so it could pan but not zoom smoothly.
- **Zooming only when the paste wouldn't fit at the current zoom:** the owner wanted the view to focus on the paste.

## Consequences

- During a glide the drawing and the pointer disagree: a click lands where the end's view has it. The glide is short, and a gesture ends it.
- Zooming out, the start of the glide shows the smaller atlas's glyphs scaled up, slightly blurred, for a few frames.
- A paste out of view at a zoom above 150% zooms out to 150% or less, even if it would fit as it is.
