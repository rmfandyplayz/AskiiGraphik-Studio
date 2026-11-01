<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0011: Canvas size limits and resize anchoring

Status: Accepted

## Context

ARCHITECTURE §5.7 sets a soft limit of 500 × 500 (warn, but allow), asks for a hard limit against typos to be recorded in an ADR, and says resizing uses one of nine anchors.

## Decision

- **Hard limit: 1 to 4096 cells per side** (`CanvasSize.MinimumSide`, `CanvasSize.MaximumSide`). `CanvasSize` can't be constructed outside it; `CanvasSize.IsValid` checks input without exceptions.
- **Soft limit: 500 per side** (`CanvasSize.RecommendedMaximumSide`). `ExceedsRecommendedSize` is true when *either* side is above 500. The warning itself is UI, in step 1.6.
- **Anchors:** `ResizeAnchor` has nine values. `CanvasResizePlan.Create(old, new, anchor)` returns the `ContentOffset` (how far every cell moves), the `KeptRegion` and up to four `CroppedRegions`, all in old-canvas coordinates.
- **Odd centered changes:** when a centered side changes by an odd number, the extra cell goes to the right or bottom (growing 10 → 13 adds 1 column at the left and 2 at the right). The offset is `(new - old) / 2` rounded toward zero, so resizing back with the same anchor moves every cell exactly back.

## Alternatives

- **A lower hard limit such as 2048.** It would cap memory lower, but ARCHITECTURE suggests 4096 and the soft-limit warning already covers memory.
- **Limiting the area (cells) rather than each side.** It lets very wide, short canvases through, but it's harder to explain in a dialog.
- **The extra cell at the left or top.** Equally valid and also reversible; one rule had to be picked, and the owner can flip it if it feels wrong in the resize dialog (step 1.6).

## Consequences

- Memory: a cell is 8 bytes today (16 bytes at most later), so one 4096 × 4096 layer takes about 134 MB (268 MB at 16 bytes). Large canvases with many layers can use gigabytes, which is why the soft-limit warning exists. Tiled storage (§5.8) is the fix if it becomes a problem.
- The resize command (step 1.2) stores only the cropped cells for undo, using `CroppedRegions`.

Updated 2026-11-01: the Resize Canvas dialog (step 1.6) starts anchored in the centre, and the New and Resize Canvas size boxes accept only sides within the hard limit and warn above the soft limit (ADR 0020).
Updated 2026-11-01: from the owner's 1.6 testing, a number typed outside the hard limit is clamped to it (99999 becomes 4096, 0 becomes 1) instead of being dropped.
