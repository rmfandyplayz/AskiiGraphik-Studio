<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0041: The Hand tool and Esc

Status: Accepted

## Context

Until step 2b.3a, Esc removed the text caret or cancelled an eraser stroke, and otherwise did nothing. The owner wants one tool always chosen, and Esc to switch to a "Hand" tool, which a Select tool will probably replace later. The owner chose V for it, as Select is in Illustrator, and asked that it not be over-built. A "none" tool was the owner's fallback if a Hand tool cost too much.

## Decision

- **The Hand tool** (`askiiart.hand`, V) is a Core tool that never changes the document. A new `ITool.PansWithPrimaryButton` says whether a plain left-drag pans the canvas. For the Hand tool it does, and the canvas shows the hand cursor for as long as the tool is chosen. Space + drag pans too.
- **Esc goes to the tool first.** A tool uses it only to cancel what it's doing: during an eraser stroke, Esc cancels the stroke and nothing else. Any other Esc on the canvas switches to the Hand tool. Esc with the Hand tool chosen does nothing.
- **The text tool no longer uses Esc.** Switching away removes the caret, so from typing one Esc goes straight to the Hand tool, and the tool letters work again.
- Esc is handled by the canvas only while it has focus, like the tool letters (ADR 0033), so dialogs and text boxes keep their own Esc.
- The app still starts on the Text tool. The Hand tool comes first in the Tools menu and on the Tools toolbar.

## Alternatives

- **A "none" tool:** costs about as much as the Hand tool, but a canvas where clicks do nothing looks broken, and it has nothing to show in the menus.
- **A first Esc removing the caret, and a second one switching:** two presses for the most common case.
- **Esc as an action shortcut:** it would fire in every text box and dialog, the reason the tool letters live in the canvas (ADR 0033).

## Consequences

- A future tool that cancels something with Esc (a shape being dragged out) returns true from `KeyPressed`. Everything else falls through to the Hand tool.
- When the Select tool arrives, it will probably take V and become where Esc goes, and the Hand tool may get another letter or go.

Updated 2026-11-14: Avalonia's Hand cursor is the pointing finger of links. The pan cursor is now `DragMove` on a Mac, which is the system's open hand, and `SizeAll` (the four-way move arrows) on Windows and Linux, where `DragMove` is a drag-and-drop arrow (`PanCursor`).
Updated 2026-11-19: the selection tools don't replace the Hand tool. The owner chose M for the shape selection tools and W for the Magic Wand; the Move tool takes V in step 4.5, the Hand tool moves to H, and Esc keeps switching to the Hand tool (ADR 0055).

Updated 2026-11-21: in step 4.5 the Hand tool lost its letter instead of moving to H, so Esc is the key that picks it, and it's the tool at launch. The Move tool has V, and while it's moving, Esc puts the cells back before the next Esc picks the Hand tool (ADR 0066).
