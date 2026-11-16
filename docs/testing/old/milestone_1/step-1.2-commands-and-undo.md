<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 1.2: Commands and undo, manual test

**Build:** working tree (not pushed yet)
**Test on:** - [ ] Windows  - [ ] macOS  (Linux is covered by CI once pushed)
**Time needed:** about 5 min, plus waiting for CI after the push

## What changed

Every change to a document now goes through an undoable command: editing cells, resizing the canvas, and adding, removing, reordering or changing layers. Several commands can be grouped into one undo step, and consecutive typing can merge into one step. The undo history stays within a memory budget (256 MB by default) and knows whether the document has unsaved changes. There's still nothing to click; undo and redo get their menu items and shortcuts in step 1.5.

## Setup

1. Open the solution in Rider.
2. When you're ready, commit and push the changes so CI runs on all three operating systems.

## Tests

### T1: The tests pass locally

| # | Do this | Expected result |
|---|---|---|
| 1 | In Rider, open the **Unit Tests** window and run all tests in the solution | All tests pass (217 or more). The new ones are under **AskiiArt.Core.Tests → Commands**. |
| 2 | Find **UndoPropertyTests** and run it on its own | It passes. It tries 300 random sequences of up to 40 commands each, and finishes within a few seconds. |
| 3 | Run the app (`dotnet run --project src/AskiiArt.App`) | It starts and looks exactly as it did after step 0.3. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: CI is green after the push

| # | Do this | Expected result |
|---|---|---|
| 1 | Push, then open https://github.com/rmfandyplayz/AskiiGraphik-Studio/actions and wait for the run | All three jobs (**windows-latest**, **macos-latest**, **ubuntu-latest**) have a green check mark. |
| 2 | Click the **ubuntu-latest** job and expand the **Test** step | The output ends with `Test run summary: Passed!` and `total: 217` or more. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- If the property test ever fails, its message includes a line like `Set seed: "6KvUiT7QdDy5"`. Paste that whole message into a bug report: the seed replays the exact failing sequence.

## Known limitations (not bugs)

- No Undo or Redo menu items or shortcuts yet (step 1.5), and no history panel (M2).
- The memory budget can't be changed from the UI yet; that's for the settings window (M8).
- A frame's last layer can't be removed. That's intended (confirmed by the owner).

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 1.2, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <the failing test's full message, or the last 30 lines of the failed CI step>
```
