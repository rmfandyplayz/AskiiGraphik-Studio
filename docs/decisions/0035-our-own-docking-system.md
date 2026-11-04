<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0035: Our own docking system replaces the Dock library

Status: Accepted

## Context

Testing 2.1 showed that docking felt like a programming IDE, not an art app. On 2026-11-04 the owner delivered a brief for what they want instead: Unity-style toolbars on 8 anchors, widget stacks that behave like iOS widgets, and Illustrator-style docks (`docs/DOCKING.md`).

Dock is built for tiled layouts. It has no idea of anchored toolbars that fight for space, stacks with gaps, or a side that converts as a whole. We already bend it to work as a see-through layer: selectors that remove its backgrounds, a flag to bring them back during drags, measuring what's around the canvas's dock, and correcting the sizes it gives new splits (ADRs 0025 and 0029). Going further would mean replacing its templates and fighting its drop logic, on top of following its updates.

Little depends on it: 6 files and 4 packages. Windows don't know their host (ARCHITECTURE §14.4), so the Layers and History views aren't affected.

## Decision

- **Build our own system,** and remove the 4 Dock packages. Step 2.5b did this.
- **New project:** `src/AskiiArt.Docking`, with no dependencies, tested by `tests/AskiiArt.Docking.Tests`. `App` references it. Views stay in `App/Shell/Docking`.
- **Layout as data → pure solver → thin view.** The layout is immutable data. A solver turns it, the app window's size and measured sizes into rectangles. Edits are pure functions from layout to layout, and a drag preview solves the edited layout, so a preview can't disagree with its drop.
- **A host panel with no background** (`DockingHost`), so empty pixels reach the canvas with no workaround.
- **Unchanged from ADR 0029:** the menu bar (except on macOS) and the status bar are plain strips outside the host, the canvas's scrollbars are off unless `CanvasView.ShowScrollBars` is set, and the window has a minimum size lowered to fit small screens.
- **Safe area:** the solver works it out, and `MainWindow` passes it to `CanvasView` after each layout pass. Sides count, and so does an anchored toolbar along the edge it lies on.
- **Windows and toolbars are registered by id,** ready for M8's customizer and M8b's plugins.
- **Floating stays inside the app window,** as in ADR 0025, for the same reasons. It also keeps frosted glass possible.
- **Words:** "dock" and "widget" replace "hard dock" and "soft dock". Code says *panel* for a window, because `Window` is an Avalonia class.
- **A new settings key,** `dockingLayout`, so everyone's layout resets once when this arrives. `IAppSettings` has no way to remove an entry, and adding one for this alone isn't worth it, so the old `dockLayoutV2` stays in the file, unread. A layout with the wrong shape falls back to the default. One with the right shape is repaired (`LayoutRepair`), so a window or toolbar added in a later version appears rather than counting as closed.
- **Built in steps 2.5b to 2.5g,** with dragging temporarily gone between 2.5b and the steps that bring it back.

## Alternatives

- **Keep Dock and restyle further:** toolbars and widget stacks would still have to be built beside it, leaving two systems and the existing workarounds.
- **Fork Dock:** we'd own a large codebase designed around tiling, for the parts we'd keep (drag and drop, tabs).
- **Another docking library:** the ones for Avalonia are also tiled, and none has anchored toolbars or widget stacks.
- **Views doing their own layout, with no solver:** quicker at first, but the rules would spread across controls, and only slow, flaky UI tests could check them.
- **No separate project, just a folder in `App`:** nothing would stop layout rules from using Avalonia types, which the solver's tests depend on not happening.

## Consequences

- We own every drag and edge-case bug that Dock handled for us. Property tests check the rules, but only the owner's testing can check how it feels, so expect tuning rounds.
- The dependency rule in ARCHITECTURE §4 gains `App → Docking`.
- Windows can't be dragged to a second monitor, as is already true today.
- Most of 2.1's retest list checks Dock behaviour that goes away. What still applies moves into the tests for 2.5b to 2.5g, and 2.5g closes 2.1.
- M8's "layout builder" idea becomes a toolbar customizer, since this system already places things anywhere.
- The same word, "Dock", is still the macOS icon bar in the app's code and in test docs.
