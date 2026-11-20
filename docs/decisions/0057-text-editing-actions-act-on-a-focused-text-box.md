<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0057: Text-editing actions act on a focused text box

Status: Accepted

Supersedes [0056](0056-text-editing-shortcuts-go-to-a-focused-text-box.md).

## Context

ADR 0056 made the window's key bindings for Select All, Undo and Redo step aside while a text box had the keyboard, so the key reached the box. Testing step 4.1 on macOS showed that isn't enough there: the system menu bar handles a menu item's shortcut before the window sees the key, so Cmd+Z in a text box undid the document's whole history and Cmd+A selected the canvas. Choosing Edit → Undo with a text box focused did the same on every platform.

## Decision

- `TextBoxShortcuts` knows the platform's text-editing shortcuts (Avalonia's `PlatformHotkeyConfiguration`: Select All, Undo, Redo, Copy, Cut, Paste) and which text box in the main window has the keyboard, if any.
- An action whose shortcut is one of those has its command wrapped in `TextBoxFirstCommand`, both in the window's key binding and in its menu item (in the window and in the macOS menu bar). While a text box has the keyboard, the wrapped command makes the box's own edit instead, and it can always run then. Otherwise it runs the action.
- Clicking a menu in the window's own menu bar (Windows and Linux) takes the keyboard until the menu closes, so while a menu has it, `TextBoxShortcuts` keeps the text box that had it before.
- It raises `CanExecuteChanged` whenever the keyboard moves, so the macOS menu item is enabled while typing even when the document has nothing to undo, and goes back to the document's state afterwards.
- Undo and Redo in a text box use its typing history (ADR 0058).

## Alternatives

- **Keep 0056 and leave these shortcuts out of the macOS menu bar:** the menu would stop showing them, which Mac users expect to see.
- **Check the focus inside each action:** every such action would repeat it, and Core-facing actions would have to know about text boxes.
- **Leave the menu items acting on the document:** Edit → Undo while typing would change art the user isn't looking at, which is what the owner reported.

## Consequences

- The keys and the menu items behave alike on every platform. Clicking the canvas gives them back to the document.
- Copy, Cut and Paste (4.6) get this by having those shortcuts, with nothing more to do.
- Only the main window's text boxes are covered. A dialog is its own window, and its text boxes handle their own keys.
