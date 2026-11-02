<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0027: The top bar and status bar sit outside Dock

Status: Accepted

## Context

ADR 0025 put the menu bar, toolbar and status bar in fixed Dock rows. In 2.1 testing the owner changed the design:
- The status bar isn't a widget. It's its own strip, which a future setting might toggle or edit.
- The toolbar shares the menu bar's row, anchored right. If it doesn't fit, it scrolls with no scrollbar. The menu bar never scrolls, and might later be movable.
- The canvas has no scrollbars for now, in a way that can be undone.
- The window has a minimum size of 1280 × 720, and the owner was open to other solutions.

## Decision

- **Top bar:** a plain `Grid` above the `DockControl`, on top of the canvas. It holds the menu bar's widget (none on macOS) and, at the right, a `ScrollViewer` with hidden scrollbars around the toolbar's widget. A vertical wheel scrolls it sideways, since a mouse has no other wheel. Clicks in the gap reach the canvas, and the safe area counts the row's height.
- **Status bar:** a strip below the canvas, in a `DockPanel`. The canvas no longer runs under it.
- **Dock holds only movable panels and the canvas's dock.** Layouts saved before this hold the old fixed panels, so `ShellDockFactory.IsUsable` rejects them and the default layout is used.
- **Scrollbars:** `CanvasView.ShowScrollBars`, off by default. The bars, their bindings and their tests all stay; setting it to true in `MainWindow.axaml` brings them back. Navigation is the wheel, middle-drag, Space-drag where the tool allows it, the keyboard and zoom.
- **Minimum size:** 1280 × 720 from theme tokens, lowered to what the current screen has room for (`WindowMinimumSize`), and rechecked when the window moves or its scaling changes.

## Alternatives

- **Keeping both widgets in one Dock row:** Dock sizes a row's children by proportion, so they can't each hug their content with a gap between them. Dock's edge targets could also put panels above that row.
- **A hard 1280 × 720 minimum:** a 1080p laptop at 150% display scaling, a common setup, has about 1280 × 670 logical pixels, so the bottom of the window, with the status bar, would be off the screen.
- **Removing the scrollbars:** the owner may bring them back, so nothing should have to be rebuilt.
- **A settings entry for scrollbars:** there's no settings window until M8.

## Consequences

- Window-edge drop targets cover only the Dock layer, so a panel can't land above the top bar or below the status bar. While the pointer is over either one no targets show, and releasing there floats the panel.
- Floating panels stay within the Dock layer too, so they can't cover the top bar.
- With today's wide text buttons, the toolbar scrolls in windows narrower than about 1620 pixels. The owner's toolbar design replaces those buttons.
- Without scrollbars, nothing on screen shows where the view is within a big canvas. Scroll limits keep the canvas from being lost entirely.
- Moving the menu bar later means changing this `Grid`, not Dock.
