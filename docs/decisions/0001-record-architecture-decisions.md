<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0001: Record architecture decisions

Status: Accepted

## Context

The owner rarely reads the code, and Claude Code starts each step without memory of earlier ones. Decisions that live only in code or in a chat are lost, and a decision that quietly goes stale is worse than none, because it gets followed.

## Decision

Every non-obvious decision is recorded as an Architecture Decision Record (ADR) in `docs/decisions/`.

- **File name:** `NNNN-short-title.md`, numbered in order and never reused.
- **Shape:** one page at most, with a `Status:` line and the sections Context, Decision, Alternatives and Consequences.
- **Status** is exactly one of:
  - `Accepted`: describes how the code works now. Only Accepted ADRs are current practice.
  - `Superseded by NNNN`: replaced by a newer ADR.
  - `Deprecated`: no longer applies, and nothing replaces it.
- **Changing a decision:** write a new ADR, set the old one to `Superseded by NNNN`, and add a one-line pointer at the top of the old one. Never delete an ADR; the history of why things changed is useful.
- **Small corrections** that don't change the decision (a renamed type, an updated number) are edited in place, with a dated `Updated YYYY-MM-DD: ...` line at the bottom.
- **Same step:** the step that changes a decision also updates the affected ADRs, ARCHITECTURE.md (with the owner's OK) and CLAUDE.md.
- **Index:** `docs/decisions/README.md` lists every ADR with its title and status, and is kept current.
- **Conflicts:** if code contradicts an Accepted ADR or ARCHITECTURE.md, Claude Code points it out and asks the owner which is right, instead of silently following either.

## Alternatives

- **Only ARCHITECTURE.md.** It holds the agreed structure, but it gets unwieldy if every small decision and its reasons go in, and it keeps no history of why things changed.
- **Commit messages or PR descriptions.** Hard to find later, and they can't be marked as superseded.

## Consequences

- Reasons for decisions survive between sessions and can be reviewed by the owner.
- Each step has a small documentation cost. The step-end check in CLAUDE.md keeps ADRs from going stale.
