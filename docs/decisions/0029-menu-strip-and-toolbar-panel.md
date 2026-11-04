<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0029: The menu bar is a plain strip, and the toolbar is a Dock panel

Status: Superseded by 0035

> Step 2.5b replaced the Dock library with our own system, and the toolbar is no longer a panel. The menu bar and status bar stay plain strips. See [ADR 0035](0035-our-own-docking-system.md).

## Context

ADR 0027 put the menu bar and toolbar in a fixed row outside Dock, so they could share it. The owner plans a "layout builder" mode: any number of toolbars, holding anything, placed anywhere. A fixed toolbar row would stand in the way, so the owner decided that only two things stay fixed: a small menu bar at the top and a small status bar at the bottom, which can be turned off later.

## Decision

- **Menu bar:** Avalonia's built-in menu in a plain strip along the top of the window (`MainWindow.MenuBarStrip`), with no strip on macOS, which keeps the system menu bar. The canvas stops below it. Fonts and colours can be customized later through the menu's theme resources.
- **Status bar:** a plain strip along the bottom, as in ADR 0027. The canvas stops above it.
- **Toolbar:** an ordinary Dock panel (`ShellDockIds.ToolbarPanel`) that can be docked, floated, tabbed and closed like any other, with a check-marked Window → Toolbar. Its default place is a row along the top of the layout, at a fixed height (`AppDockToolbarHeight`). There it hugs its buttons at the right (`PanelAlignment`), so art shows in the rest of the row. A bar that doesn't fit scrolls sideways with no scrollbar (`ToolbarPanelView`). Reopened, it goes back along the top.
- **Saved layouts:** the setting's name changed to `dockLayoutV2`, because in an older layout a missing toolbar would look as if the user had closed it.
- **Moving a floating panel** now gets the same drop targets as dragging a docked one. Dock only sets `DockControl.IsDraggingDock` for a docked panel's drag, and the see-through styles need that flag for the canvas's dock to be hit, so `ShellDock` sets it during a floating panel's move too. A floating panel's title also had a handle that started a different drag, one that could dock Dock's internal wrapper instead of the panel, leaving a broken layout. `DockStyles.axaml` makes that handle part of the plain header.
- **Unchanged from ADR 0027:** `CanvasView.ShowScrollBars` is off by default, with the bars, bindings and tests kept, and navigation is the wheel, middle-drag, Space-drag where the tool allows it, the keyboard and zoom. The window has a minimum size of 1280 × 720 from theme tokens, lowered to what the current screen has room for (`WindowMinimumSize`), rechecked when the window moves or its scaling changes.

## Alternatives

- **Keeping the toolbar fixed until M8:** less work now, but later panels and the safe area would keep assuming a row that's going away.
- **The toolbar on the menu bar's row as a Dock panel:** Dock's layer sits below the strip. Running the layer behind the menu bar would make panels slide under it, and the row would only suit one toolbar.
- **A custom toolbar host beside Dock:** it would have to reimplement docking, floating and saving, which is what the layout builder needs from Dock.
- **A hard 1280 × 720 minimum:** a 1080p laptop at 150% display scaling has about 1280 × 670 logical pixels, so the bottom of the window would be off the screen.

## Consequences

- The toolbar needs Dock's title bar to be dragged, so its row is taller than the buttons. The docking pass (step 2.5) can slim that down.
- A toolbar can be tabbed with a panel, and panels can land above the toolbar's row. Neither does harm, and the docking pass can decide whether to allow them.
- More toolbars (M8) mean more panels with the same view and their own `ToolbarLayout`, and nothing in the shell has to change.
- Window-edge targets cover only the Dock layer, so nothing lands above the menu bar or below the status bar, and floating panels can't cover either.
- Without scrollbars, nothing on screen shows where the view is within a big canvas. The owner has parked a minimap for that (`docs/TEMPORARY-UI.md`).
