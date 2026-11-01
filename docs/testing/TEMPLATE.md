<!-- Written by Claude (Anthropic). Template for manual test docs. Copy to docs/testing/step-<id>-<slug>.md and fill in. -->
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

## Tests

### T1: <name>

**Also on macOS:** <Yes, because ... / No>

| # | Do this | Expected result |
|---|---|---|
| 1 | <one concrete action> | <what you should see> |
| 2 | ... | ... |

- [ ] Pass  - [ ] Fail. Notes:

### T2: <name>

**Also on macOS:** <Yes, because ... / No>

| # | Do this | Expected result |
|---|---|---|
| 1 | ... | ... |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- <Edge cases where bugs might hide, e.g. "resize to 1×1", "type at the last column">

## Known limitations (not bugs)

- <Things that are intentionally missing or temporary in this step, e.g. "the menu is placeholder UI">

## Reporting a problem

Paste this into Claude Code:

```
Bug in step <id>, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
