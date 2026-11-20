<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0056: Text-editing shortcuts go to a focused text box

Status: Accepted

## Context

Step 4.1 binds Select All to Cmd+A. Avalonia runs a window's key bindings before the focused control sees the key, so with the typing in a text box (a ramp's name, a character set), Cmd+A selected the whole canvas instead of the box's text. Undo and Redo already had the same problem: Cmd+Z in a text box undid the document. M4 also brings Copy, Cut and Paste (4.6), which every text box uses too.

## Decision

- When `MainWindowBuilder` binds an action's shortcut to the window, and the shortcut is one the platform's text boxes edit with (Avalonia's `PlatformHotkeyConfiguration`: Select All, Undo, Redo, Copy, Cut, Paste), it wraps the command in `YieldToTextBoxCommand`. That command can't run while a `TextBox` has the keyboard, so the key binding leaves the key alone and the text box gets it.
- Every other shortcut (New, Save, zoom, and so on) still works while typing in a text box.
- The actions themselves don't know about focus, and their menu items stay enabled.

## Alternatives

- **Checking focus inside each action:** every action with a text-editing shortcut would repeat it, and the menu item would do nothing when chosen while a text box has the keyboard.
- **Handling the shortcut keys in the canvas, like the tool letters (ADR 0033):** they'd stop working from the toolbars and windows, where the user expects Undo to work.
- **A fixed list of keys:** the platform's own list matches what its text boxes do, including Redo's different keys on each platform.

## Consequences

- Cmd+Z and Cmd+Y (Cmd+Shift+Z) in a text box now undo and redo the typing in that box, not the document. Clicking the canvas gives them back to the document.
- On macOS, a shortcut shown in the system menu bar is handled by the menu, not by a window key binding, so this doesn't cover it. Whether Cmd+A and Cmd+Z in a text box reach the box there is checked by hand.
