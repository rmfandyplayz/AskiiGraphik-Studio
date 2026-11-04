<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0035: Our own docking system replaces the Dock library

Status: Accepted

> The decision is made, and the code follows in step 2.5b. Until then ADRs 0025 and 0029 still describe how the app works, and 2.5b marks them superseded.

## Context

Testing 2.1 showed that docking felt like a programming IDE, not an art app. On 2026-11-04 the owner delivered a brief for what they want instead: Unity-style toolbars on 8 anchors, widget stacks that behave like iOS widgets, and Illustrator-style docks (`docs/DOCKING.md`).

Dock is built for tiled layouts. It has no idea of anchored toolbars that fight for space, stacks with gaps, or a side that converts as a whole. We already bend it to work as a see-through layer: selectors that remove its backgrounds, a flag to bring them back during drags, measuring what's around the canvas's dock, and correcting the sizes it gives new splits (ADRs 0025 and 0029). Going further would mean replacing its templates and fighting its drop logic, on top of following its updates.

Little depends on it: 6 files and 4 packages. Windows don't know their host (ARCHITECTURE §14.4), so the Layers and History views aren't affected.

## Decision

- **Build our own system** and remove the 4 Dock packages in step 2.5b.
- **New project:** `src/AskiiArt.Docking`, with no dependencies, tested by `tests/AskiiArt.Docking.Tests`. `App` references it. Views stay in `App/Shell/Docking`.
- **Layout as data → pure solver → thin view.** The layout is immutable data. A solver turns it, the app window's size and measured sizes into rectangles. Edits are pure functions from layout to layout, and a drag preview solves the edited layout, so a preview can't disagree with its drop.
- **A host panel with no background,** so empty pixels reach the canvas with no workaround.
- **Windows and toolbars are registered by id,** ready for M8's customizer and M8b's plugins.
- **Floating stays inside the app window,** as in ADR 0025, for the same reasons. It also keeps frosted glass possible.
- **Words:** "dock" and "widget" replace "hard dock" and "soft dock". Code says *panel* for a window, because `Window` is an Avalonia class.
- **A new settings key.** The old layout is ignored once, so everyone's layout resets when this arrives.
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
- CLAUDE.md, ARCHITECTURE §3, §4, §14.4 and the glossary change in 2.5b, with the code.
