<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0030: The active layer and layer operations

Status: Accepted

## Context

Step 2.3 lets the user choose the active layer and add, delete, duplicate, reorder, rename, show, lock and merge layers. Until now the active layer was always the top one, and duplicate and merge down had no commands.

## Decision

- **The active layer belongs to the session, not the document.** `EditingSession.SetActiveLayer` chooses it and raises `ActiveLayerChanged`. It isn't undone. If the layer goes away, for example when its Add is undone, the top layer takes over.
- **Tools ask `EditingSession.CanEditActiveLayer`**, which is false for a locked layer and for a hidden one, since an edit there couldn't be seen.
- **`DuplicateLayerCommand`** copies its source each time it's applied, and holds no cells. Undo order means the source is just as it was the first time.
- **`MergeLayerDown.Create`** builds one undo step from what already exists: a `CellEditCommand` on the lower layer and a `RemoveLayerCommand`, in a `CompositeCommand`. A cell goes down exactly when the `CompositingPolicy` says it hides what's below, so the picture doesn't change. The caller supplies the policy, which the per-layer space option (2.4) will affect.
- **`SetLayerPropertiesCommand` takes a step name**, so History says "Rename Layer" or "Show or Hide Layer" rather than "Change Layer".
- **The Layer menu and the panel's buttons are one set of actions** (`LayerActions`, ARCHITECTURE §14.2). `LayersPanelViewModel` does the work and lives as long as the app, like the History panel's. Rows show the document's state rather than keeping their own, so undo and a checkbox can't disagree.
- **A panel hands typing back** by raising `PanelEvents.DoneTypingEvent`, which `MainWindow` listens for. The panel doesn't need to know where it's hosted.

## Alternatives

- **The active layer in the document, undoable:** choosing a layer would mark the document unsaved, and clicking between layers would fill History.
- **A dedicated merge command that stores both layers:** more code and more memory than the composite, for the same result.
- **Letting tools edit hidden layers:** matches the old behaviour, but changes nobody can see are easy to make by accident.

## Consequences

- Merge Down is only offered when both layers are visible and unlocked, because a hidden layer's cells would suddenly show or vanish. Delete is only offered for an unlocked layer when another remains. These rules live in the view model, so a future plugin calling the commands directly isn't bound by them.
- A merge reads the whole layer once, in time proportional to the canvas, which is fine at the sizes in ARCHITECTURE §21.
- Undoing a delete doesn't reselect the restored layer, since the active layer isn't undone.

Updated 2026-11-02: a saved file now remembers the active layer (ADR 0031).
