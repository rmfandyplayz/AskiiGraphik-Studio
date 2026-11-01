<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0018: Wheel gestures and scroll momentum

Status: Accepted

## Context

Touchpads, the Magic Mouse and free-spinning mouse wheels keep sending wheel events after the fingers lift, so a scroll glides to a stop. In step 1.4 testing on macOS, pressing Cmd during that glide (for Cmd+C, say) turned the rest of it into a zoom, because the canvas read the modifier keys afresh for every event. The owner asked for zoom to happen only while the fingers are on the touchpad. macOS marks glide events, but Avalonia 12 doesn't pass that on: a wheel event carries only a delta, the modifiers and a timestamp.

Each wheel event also zoomed one full step, so a touchpad's stream of small events raced through the zoom levels.

## Decision

- `WheelGestureTracker` groups wheel events into **gestures**: events less than 250 ms apart belong to the same one. The keys held at a gesture's **first** event decide it: zoom (Ctrl/Cmd), sideways scroll (Shift) or scroll.
- Pressing a key in the middle of a gesture changes nothing; the glide keeps scrolling. Letting go of Ctrl/Cmd in the middle of a zoom drops the rest of that gesture, rather than turning it into a sudden scroll.
- **Zoom steps follow distance.** A gesture's first event zooms one step, however small its delta, so one mouse-wheel notch is one step on every platform. After that, each further notch's worth of delta (1.0) is one step.
- Shift + wheel swaps a purely vertical delta to horizontal, and leaves a delta alone if the platform already made it sideways (macOS does).

## Alternatives

- **Reading macOS's momentum phase:** exact, but needs a change to Avalonia's native macOS code. Worth revisiting if Avalonia ever exposes it.
- **Ignoring all wheel events for a while after a modifier changes:** also blocks deliberate input, and needs the same timing guess.

## Consequences

- After scrolling, start a zoom with a short pause (a quarter of a second) or it continues the scroll. In practice the time it takes to press Ctrl/Cmd is longer.
- The 250 ms gap is a guess from typical event rates; adjust it if real hardware disagrees. Headless tests send wheel events back to back, so a test that mixes modifiers sends each gesture from a fresh control.
