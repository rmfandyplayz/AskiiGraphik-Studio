<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0077: Fitting without zooming in, and after arrow runs

Status: Accepted

## Context

After 4.11's testing (ADR 0076) the owner retracted zooming in to 150%: the view should do only what it must to bring a paste into view, so one letter just out of view at 800% stays at 800%. The owner also asked that a run of arrow presses bring the selection into view the same way, while mouse moves stay as they are. And a paste made while the view was settling first snapped to where the settling would end, then glided the rest of the way, or didn't glide at all.

## Decision

- **The fit.** `ViewFit.ZoomFor` (was `PasteFit`) keeps the current zoom when the cells fit the usable area there with the canvas margin round them; otherwise it takes the largest usual step below the current one at which they fit, or 25%. It never zooms in. The cells are still centred, as before.
- **After arrow runs.** The canvas control notes when the selection changes while an arrow is held or in the 0.4 s pause after (`_hasArrowRunChangedSelection`). When the pause ends, instead of only settling, it calls `BringIntoView` with the selection's bounds, which does nothing if they're fully in view, then settles. Drags, and arrows that change nothing (such as the Text tool's), only settle, as before.
- **No snap.** `BringIntoView` drops a settling glide under way without releasing the view, so the new glide starts from what's on screen. `CanvasControl.CanvasOriginOnScreen` reports where the canvas is drawn, gliding or not, for the test.

## Alternatives

- **Scrolling arrows' selection just into view, rather than centring it:** less movement, but the owner asked for the paste's behaviour.
- **Bringing the selection into view after each press:** the view would move while a run goes on, which ADR 0072 rules out.

## Consequences

- Arrows that move a selection bigger than the view, such as the whole layer the Move tool selects when nothing was, zoom out to fit it once the run ends.
- No test doc: the owner looks it over (agreed 2026-11-24).
