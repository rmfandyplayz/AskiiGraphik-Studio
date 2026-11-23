<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0072: Moving past the zone, and scrolling round it

Status: Accepted

## Context

In ADR 0071 the zone around the canvas was a wall: a move stopped when the selection's side reached its outer edge. Testing 4.9, the owner meant a ring the selection must still *touch*, so everything can go off the canvas but not drift away for ever. The owner also asked that the view can scroll to see all of it, never moves while something is moving, then tidies up, and that all of the app's motion can be turned off for accessibility.

## Decision

- **Touching rule.** `FloatingMove.Limit` clamps each axis to `[zone.Left - shape.Right, zone.Right - shape.Left]` (and the same for rows): the selection's bounding box still shares at least an edge or a corner with the zone. A drag that reaches the limit stops and slides along it. A gesture can always go back to where it began, even when it began outside these limits.
- **Reach.** The view can scroll over the *reach*: the canvas and its zone, and, while the selection lies past the zone, the selection's bounds with as much room again around them (`SelectionZone.Around(cells, ratio)`, `CanvasReach.Of`). Past the reach is the canvas margin, now 64 logical pixels (was 32). While all of it fits, the canvas stays centred, shifted only as far as it must be to show the reach.
- **Hold, settle, glide.** While something may be moving the view keeps still: during a tool gesture, while an arrow is held, and for `MoveTool.ArrowRunPause` (0.4 s) after the last arrow is let go, so a run of presses isn't chased. `CanvasViewport.HeldAt(origin)` adds the current view to the content so the canvas stays put whatever the reach does. Once nothing is moving the view *settles*: it glides (`ViewGlide`, an ease-out over `AppCanvasViewGlideMilliseconds`, 150 ms) to where the content without the hold would put it, which is after each drag or arrow run, undo, redo and deselect. Scrolling, panning or zooming stops a glide where it is.
- **Paste.** A paste that is partly out of view but would fit in it glides to the middle of the view (`CentreOnIfPartlyHidden`). Otherwise the view stays put.
- **Reduced motion.** One app-wide setting, `"reduceMotion": "true"` in the settings file, off by default and with no UI until there's a Preferences window. At start-up `ReducedMotion.Apply` sets every motion token to zero in the App's own resources, which come before the themes they merge: the selection's dashes and hatching, the landing outline's dashes, the docking animations and fades, and the view's glide. Speeds of zero stand still and durations of zero jump, and no frames are asked for to move nothing.

## Alternatives

- **Scroll the view to follow a move as it happens:** the canvas would slide under the pointer during a drag, and the owner asked for it to keep still.
- **Settle with a timer after arrows:** Avalonia's headless tests never run timers, and the canvas already runs on animation frames, so frames watch the time.
- **Follow the operating system's reduce-motion setting:** each platform keeps it differently, and whether Avalonia reads it everywhere is still to be checked; parked in TEMPORARY-UI for the Preferences window.
- **A reduce-motion flag read by each animation:** every new animation would have to remember it, where tokens already set every speed and duration.

## Consequences

- A whole layer can now go entirely off the canvas; the cells there are still lost when the move ends (ADR 0071).
- The 150 ms glide, the centring and the paste rule are guesses for the owner to judge (TEMPORARY-UI).
- Changing the reduce-motion setting takes effect at the next start.
