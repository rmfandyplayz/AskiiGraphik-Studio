<!-- Written by Claude (Anthropic). Template for manual test docs. Copy to docs/testing/step-<id>-<slug>.md and fill in.
     Only the current milestone's test docs sit beside this file; finished milestones' move to old/milestone_<id>/. -->
<!-- Edited with Claude Code (Anthropic). -->
<!-- The owner tests on Windows by default. Mark a test "Also on macOS: Yes" only where the
     platforms can differ: shortcuts and modifier keys, native menus, file and font dialogs,
     installed fonts, Retina rendering, touchpad gestures, window and Dock behaviour. Shared logic
     (documents, undo, file contents) is covered by Windows plus CI on macOS. -->

# Step <id>: <title>, manual test

**Build:** <commit hash, or "working tree">
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about <N> min

## What changed

Two to four plain-language sentences: what you can do now that you couldn't before.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. <Any sample files needed, with their paths.>

## Known limitations (not bugs)

- <Things that are intentionally missing or temporary in this step, e.g. "the menu is placeholder UI">

## Tests

### T1: <name>

**Also on macOS:** <Yes, because ... / No>

| # | Do this | Expected result |
|---|---|---|
| 1 | (one concrete action) | (what should be observed) |
| 2 | ... | ... |

### T2: <name>

**Also on macOS:** <Yes, because ... / No>

| # | Do this | Expected result |
|---|---|---|
| 1 | ... | ... |

### T<last>: Tests to revisit

The last test is always this table, when anything is left over: tests from earlier steps that still need another platform or a retest. Leave it out when nothing is.

| Step | Test | Where | Why |
|---|---|---|---|
| <id> | <T#> | <Windows / macOS> | <e.g. "passed on a Mac, not tried on Windows yet"> |

## Worth poking at (optional)

Follow the same conventions as the `## Tests` section (except for the re-tests), but this section is for specifically edge/weird cases. Instead of doing "T#" as the test header numbers, the headers should be "P#" (i.e. P1, P2, P3). Not doing these should not pose a problem.
