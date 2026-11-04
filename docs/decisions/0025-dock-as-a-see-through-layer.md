<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0025: Dock as a see-through layer over the canvas

Status: Accepted

## Context

M2 adopts Dock for panels. In M2 planning the owner set the direction, with Sketchbook and Excalidraw as examples: the canvas is the main character, and controls are docks that look like small widgets. Art shows around widgets and slides under them, yet widgets take up space in anything that works out the canvas's usable size, and they always get input first.

Dock is built for tiled layouts, where panels take space from the content. A throwaway test confirmed that it can work as a layer over the canvas.

## Decision

- **Layers:** `MainWindow` stacks the `CanvasView`, filling the space between the menu bar and status bar strips, under a `DockControl` (ADR 0029). The layout's middle is an empty `ToolDock` (`ShellDockIds.CanvasDock`).
- **Nothing lands on the canvas's dock:** it accepts drops, because with `CanDrop` off Dock shows no targets at all over it, not even the window-edge ones. Its own docking group, which no panel has, makes Dock offer no target of its own there, and releasing a panel over it floats the panel.
- **Clicks pass through empty areas:** Dock's templates give empty areas an invisible background, which catches clicks. `DockStyles.axaml` removes those backgrounds, using selectors on `DockControl.IsDraggingDock`. A selector that depends on a property has a higher priority than a template's values. While a panel is being dragged the backgrounds return, because Dock finds its drop areas by hit testing them. `ClickRoutingTests` fails if a Dock update breaks this.
- **Safe area:** `MainWindow` measures what's around the canvas dock and passes it to `CanvasView`, which adds any visible scrollbars and sets `CanvasControl.SafeArea`. `CanvasViewport` centres the canvas in what's left and sets its scroll limits so every cell can reach that area. Cells still draw under widgets. Floating panels don't count, because they can be dragged away.
- **Fixed strips:** the menu bar (not on macOS, where the system menu bar stays) and status bar are plain strips outside Dock (ADR 0029). The toolbar is an ordinary panel, which hugs its content in its default row, so art shows beside it.
- **Movable panels** (History and Layers so far) can be docked, tabbed, floated and closed. A new split gets the fixed side width, because Dock's default would give a panel half of the canvas's space.
- **Floating stays inside the window:** `IRootDock.FloatingWindowHostMode` is `Managed`, so one window still holds everything. Shortcuts keep working, dialogs block panels too, and macOS keeps its menu bar. Dock doesn't show a restored layout's floating windows by itself, so `ShellDock` presents them once the host has loaded, moved back inside if the window is now smaller. `ShellDockFactory` ignores Float on a panel that already floats alone.
- **Panels are plain Dock `Tool`s, found by id:** `ShellPanelTemplates` maps an id to a view. The saved layout holds no type of ours, so renaming our classes can't break it, and titles are refreshed on load.
- **One layout per window:** `ShellDock` attaches a saved layout to each window and saves when the window closes, so macOS rebuilds (ADR 0023) restore it. The Window menu's actions act on whichever window is attached.
- **Packages:** `Dock.Avalonia`, `Dock.Avalonia.Themes.Fluent`, `Dock.Model.Mvvm` and `Dock.Serializer.SystemTextJson` (MIT), at one version built for our exact Avalonia version.

## Alternatives

- **Tiled docks, restyled:** works out of the box, but a canvas between panels can't show art beside a small toolbar. A toolbar would claim its whole row.
- **A widget system of our own:** no dependency and true anchoring, but we'd have to build drag-to-dock, tabs and resizing. Rejected once the owner confirmed they want docks.
- **Native floating windows:** would allow panels on a second monitor, but bring back shortcut forwarding, dialogs that don't block panels, and an empty macOS menu bar when a panel has focus.
- **Replacing Dock's control templates:** avoids the selector trick, but copies Dock's internals into this repo, to be kept up with every update.

## Consequences

- Panels can't leave the main window.
- Each Dock update may need a look at `DockStyles.axaml`. The tests catch click-through breaking, but not a change in how something looks.
- Docks aren't anchored the way Unity's UI is: a widget sits in a row or column, with a margin as its offset. A floating panel keeps its position when the window resizes. True anchoring waits for the owner's design.
- The widget look is a placeholder. Rounded corners and Dock's panel header are left for the owner's design step.

Updated 2026-11-01: from 2.1 testing, the top bar and status bar moved out of Dock (ADR 0027), the canvas's dock now accepts drops so window-edge targets show over it, and restored floating panels are shown.
Updated 2026-11-02: the menu bar became a plain strip and the toolbar a Dock panel (ADR 0029). Moving a floating panel now also makes the see-through areas catch hits, so targets show over the canvas.
Updated 2026-11-04: ADR 0035 replaces Dock with our own system. This ADR still describes the code until step 2.5b, which marks it superseded.
