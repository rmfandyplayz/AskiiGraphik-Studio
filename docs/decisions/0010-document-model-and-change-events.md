<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0010: Document model, mutation and change events

Status: Accepted

## Context

ARCHITECTURE §5 describes the document (frames, layers, cells, occupancy, compositing, change events) but leaves the code shape open. Principle 3 says every document change goes through a command, and §5.9 says change events carry the affected layer and dirty rects.

## Decision

- **Namespaces:** the model lives in `AskiiArt.Core.Documents` (plural, because a `Document` type inside a `Document` namespace breaks name lookup in other namespaces). Compositing lives in `AskiiArt.Core.Compositing`.
- **Reading is public, changing is internal.** `Document`, `Frame` and `Layer` expose read-only APIs. Their mutators (`Layer.SetCell`, `Layer.SetProperties`, `Frame.InsertLayer/RemoveLayer/MoveLayer`, `Document.ResizeCanvas`) are `internal`, so only Core's commands can change a document. The App can't bypass undo by accident. Core's tests see internals through `InternalsVisibleTo`.
- **Cell:** `default(Cell)` is the empty cell, so new arrays start empty. A cell stores only its `Rune` and an occupied flag. The reserved `Foreground`, `Background`, `Attributes` and `Alpha` properties exist but return fixed defaults without storage; they gain fields when their feature is built. Control characters are rejected by `Cell.FromGlyph`.
- **Layer properties** are an immutable record (`LayerProperties`: name, visible, locked, and the reserved opacity and blend mode). `SetLayerPropertiesCommand` swaps whole records.
- **A frame always has at least one layer.** `Frame.RemoveLayer` refuses to remove the last one. The owner confirmed this rule; it first becomes visible in M2's layers panel.
- **Change events:** `Document.Changed` carries `DocumentChangedEventArgs`: per-layer dirty rects (`CellChanges`), `LayerListChanged`, `LayersWithChangedProperties` and `CanvasResize`. A change outside a group raises at once. `Document.BeginChanges()` (internal) starts a group that raises one combined event when its outermost scope ends; `CommandHistory` runs every command inside one (ADR 0012). Setting a cell to the value it already holds raises nothing.
- **Dirty rects:** per layer, rects merge only when their bounding rect adds no extra cells (typing along a row becomes one rect). Above 16 rects they collapse into one bounding rect. After a canvas resize in a group, cell rects are dropped, because the whole canvas changed.
- **Compositing:** one static `Compositor` with a per-cell and a region API. The region API paints visible layers bottom-up into a caller's span (no allocations); the per-cell API walks top-down. One `CompositingPolicy` object decides which cells hide the layers below; only U+0020 counts as a space.
- **Not yet in the model:** the font reference, format version and document settings (§5.10). They arrive with the steps that use them (1.3/1.6 and 1.7).

## Alternatives

- **Public mutators, with commands by convention.** Simpler, but nothing would stop the App from editing cells outside undo.
- **One event per cell.** Simple, but a 500 × 500 fill would raise 250,000 events.
- **An `ICellStorage` interface for §5.8.** One implementation doesn't need an interface; the internal `CellGrid` class is the boundary, and tiles can replace its insides.

## Consequences

- Code outside Core (sample content in 1.4, for example) changes documents only through the commands of ADR 0012.
- Views treat dirty rects as "at least these cells changed" and may redraw a little more.

Updated 2026-10-30: the commands now exist (step 1.2, ADR 0012); wording updated to match.
Updated 2026-10-31: the owner confirmed the "at least one layer" rule; ARCHITECTURE §4's folder example now says `Documents/`.
Updated 2026-11-01: the format version lives in the file's header, not in the model; loading builds documents with `Document.CreateWithLayers` (step 1.7, ADR 0021).
