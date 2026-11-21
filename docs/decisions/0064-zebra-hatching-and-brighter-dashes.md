<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0064: Zebra hatching and brighter dashes

Status: Accepted

## Context

After step 4.2c the owner confirmed the flicker was gone (ADR 0062) and asked for the hatching they had parked: dark and light stripes side by side with no see-through gaps, "like a zebra", 12 pixels a stripe, both at 40% opacity. They also asked for brighter Subtract and Intersect dashes: a bright, desaturated red, and a somewhat desaturated light blue, so the lines stand out from the hatching under them.

## Decision

- **Zebra tile:** the tile is filled with the light colour, and the dark stripe is drawn over half of it, replacing what's there rather than blending, with smoothed edges. Every pixel ends up the same opacity, so the hatching has no gaps. The stripes are drawn a whole tile beyond each end, so wide stripes meet the next tile without a seam.
- **Size:** one token, `AppCanvasSelectionHatchBandWidth` (12 logical pixels), is the width of each stripe across itself. The tile is twice that times √2 wide, which makes the stripes 45°, replacing ADR 0062's line thickness and spacing.
- **Separate colours** for the hatching and the dashes. Subtract and Intersect each have their own hatching colours (`SubtractHatchLine`, `IntersectHatchLine` and their backdrops) and their own, lighter, dash colours, so either can change without the other.
- **Opacity:** the white and black stripes are 40%, and so is the coloured hatching over cells being deselected.

## Alternatives

- **Two lines with gaps between them:** what the hatching was before. The owner asked for no see-through parts.
- **Drawing the two stripes as separate lines:** their smoothed edges would overlap or leave a hairline gap, which would show as a faint seam inside each stripe.
- **One colour for a mode's dashes and hatching:** the dashes disappear over hatching of their own colour.

## Consequences

- Stripes at 40% hide more of the art than the thin lines did. All the values are tokens, listed in `docs/TEMPORARY-UI.md`.
- The motion is unchanged: the tile still glides in part-pixel steps (ADR 0062).

Updated 2026-11-21 (step 4.4): after the owner's 4.3 testing, every stripe is 8 logical pixels wide, and the black and white stripes are 15%. The coloured hatching stays at 40%.
