<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0046: The UI Pass, with plugins and ship-readiness renumbered

Status: Accepted

## Context

M8 was "Customization": a toolbar customizer, our own colour picker, a settings window, shortcut rebinding, themes and more. ADR 0032 put plugins in as M8b, between M8 and M9 (Ship-readiness), so that later milestones kept their numbers. After 3.1's testing (2026-11-16), the owner reshaped M8 into a UI Pass: building the real UI together and polishing the app until it looks professional. Plugins aren't part of that pass, so the owner gave them a milestone number of their own.

## Decision

- **M8 is the UI Pass.** It opens with fixed steps for what has to exist, such as all the menus the app needs, then turns open-ended: the owner brings the UI and UX they've envisioned, one step at a time. M8's old Customization scope is part of it.
- **Plugins become M9, and Ship-readiness M10.**
- **Every current reference changed in the same step:** ROADMAP, ARCHITECTURE, DOCKING.md, TEMPORARY-UI, code comments, and the Accepted ADRs 0024, 0032 and 0035, with Updated lines. This supersedes ADR 0032's rule that later milestones keep their numbers.
- **Records of the past keep the old numbers:** the test docs under `docs/testing/old/`, superseded ADRs and the git history. In anything written before 2026-11-16, M8b means plugins and M9 means ship-readiness.

## Alternatives

- **Keep M8b for plugins:** no renumbering, but the "b" makes plugins read as part of the UI Pass, which they aren't.
- **Milestones with names and no numbers:** nothing would ever need renumbering, but step ids such as 3.1 and 2b.4 are built on the numbers.

## Consequences

- Plugins still come after the UI has settled, which suits an API that exposes panels and toolbars, and still before packaging, so ADR 0032's reasoning stands.
- A reference to M9 that was missed would now point at the wrong milestone. This step searched the repository for them.
- Checking pen pressure on a real tablet (3.7) moves with Ship-readiness to M10.
