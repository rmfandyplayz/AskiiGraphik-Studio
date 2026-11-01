<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0020: The active document and the document actions

Status: Accepted

## Context

Until step 1.6, one `EditingSession` (a document and its history) lived for the whole run, and the canvas, Undo and Redo held on to it. New replaces the document, and with it the history. Step 1.6 also adds three dialogs (New, Resize Canvas, the font picker) and a placeholder toolbar bound to actions (ARCHITECTURE §14.2).

## Decision

- **`ActiveDocument`** holds the window's current `EditingSession` and raises `SessionChanged` when New replaces it. Everything that works on "the document" reads the current session when it needs it and follows that event: the canvas view model switches its composite, tool and font; Edit's Undo and Redo follow the new history; the document actions follow the new document.
- **`DocumentActions`** has New (Cmd+N), Resize Canvas, Font and ASCII Only (moved from Edit, its id unchanged). Resize Canvas and Font are commands, so they undo; New isn't undoable, and it asks about unsaved changes first (ADR 0021). Picking the same size or font adds no undo step.
- **`IDocumentDialogs`** is how the actions ask the user, so tests answer with a fake. Each dialog has a view model that builds the result (null when cancelled or invalid), and a thin window. The size boxes are shared by New and Resize Canvas: sides outside 1–4096 can't be accepted, and sides above 500 show a warning (§5.7).
- **The toolbar** is a `ToolbarLayout` of the same entries as a menu (an action or a separator), built into text buttons bound to the actions' commands. Buttons don't take keyboard focus, so typing still goes to the canvas after a click, and a toggle action gets a toggle button that follows its checked state.

## Alternatives

- **One `CanvasViewModel` per document, rebuilt by New:** the canvas actions and the window would need to rebind to the new one; following one event keeps them unchanged. Several documents at once (tabs or windows) come with Dock (M2), and will give each window its own `ActiveDocument`.
- **Dialog methods on `IAppShell`:** it would keep growing with every feature; the document dialogs are their own feature.

## Consequences

- The app opens with an empty document; the sample is in the Debug menu (ADR 0021).
- Anything new that keeps a reference to a session or history must follow `SessionChanged`.

Updated 2026-11-01: `ActiveDocument` also holds the file the document is saved in, New asks about unsaved changes, and Open, Save and Save As join the actions (step 1.7, ADR 0021).
Updated 2026-11-01: every modal dialog goes through `ModalDialog.ShowAsync`, which returns only once Avalonia has finished closing it (step 1.8). Avalonia finishes `ShowDialog` inside the dialog's `Closed` event, before it re-enables the owner, and going straight from there to a file picker (Save in the unsaved-changes question, then Open) stopped the next picker from appearing on Windows. Export as Text joins the actions (ADR 0022).
