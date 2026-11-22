<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0054: Pen pressure

Status: Accepted

## Context

Step 3.7 makes a drawing tablet's pen pressure count. The roadmap said pressure "scales the Brush's intensity", which can be read two ways. Scaling the brush's target darkness makes a light touch paint towards a light character, so over dark art it lightens `@` to `.`: a light touch erases. Scaling how strongly the brush covers a cell, as opacity does in painting apps, makes a light touch nudge cells towards the Intensity setting. The two look the same on an empty cell. The owner (2026-11-19) chose coverage, for the Brush and the Soft Eraser. The owner has no tablet yet, so checking a real one waits for M10.

## Decision

- **Pressure is coverage.** `RampStroke`, which the Brush and the Soft Eraser share, multiplies the tip's weight at each cell by the pressure. A cell still moves once per stroke, by the greatest weight that has reached it, so pressing harder takes it further and going over it lighter adds nothing. The Soft Eraser still lightens at least one level wherever it reaches at all; no pressure reaches nothing.
- **Each pointer report brings its pressure.** The straight line from the last report takes the new report's pressure. A pen pressed harder without moving stamps again where it is.
- **Only pens press.** Avalonia reports 0.5 for a mouse, so `PointerPressure` gives a mouse or touch 1 and only a pen its own value, kept within 0 to 1, with an unknown one (NaN) counting as 1. The hard-edged tools (Eraser, BG Eraser, FG and BG Brushes, Character Brush) ignore pressure.
- **No settings yet.** Pressure is linear, always on with a pen, and has no toggle or curve. The pen's eraser end and side buttons do nothing special: the eraser end paints like the tip.
- **Debug → Mouse Acts as a Pen at Half Pressure** makes the mouse press at 0.5, so the effect can be tried without a tablet. It's off at every start.

## Alternatives

- **Scaling the target intensity:** a light touch would erase existing art, and a cell reached at different pressures would need a rule for which target wins.
- **Interpolating pressure along the line between reports:** reports come often enough that the difference doesn't show in cells.
- **A Tool Options switch for pressure:** what pressure controls is the user's choice later (M10), so one switch now would be replaced.

## Consequences

- Nothing changes for a mouse: every existing stroke paints as before.
- A pen whose driver reports no pressure gets Avalonia's 0.5 and paints at half strength. Checking real tablets on each platform is part of M10.
- Choosing what pressure controls (size, strength, a curve) and the eraser end are parked in `docs/TEMPORARY-UI.md`.

Updated 2026-11-22: on a cell the tip weighs unevenly, pressure scales each part's weight, and each part moves once by the greatest that has reached it (ADR 0068).
