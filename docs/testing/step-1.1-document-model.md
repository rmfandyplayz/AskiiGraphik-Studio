<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 1.1: Document model, manual test

**Build:** the commit "claude: add document model (step 1.1)"
**Test on:** - [ ] GitHub (one push covers Windows, macOS and Linux)
**Time needed:** about 5 min, most of it waiting for the run

## What changed

Core now has the document model: a document holds frames (exactly one for now), a frame holds layers, and a layer holds a grid of cells. A cell is either empty (the layer below shows through) or holds a character, and a space is a deliberate blank. Core can flatten the layers into one picture, resize the canvas around any of nine anchors, and report exactly which cells changed. There's nothing to click yet; the app looks the same as after step 0.3.

## Setup

1. Push the commit to GitHub (Claude Code already pushed it if you asked it to).
2. Open https://github.com/rmfandyplayz/AskiiGraphik-Studio/actions in a browser.

## Tests

### T1: CI is green

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the CI run for the commit "claude: add document model (step 1.1)" and wait for it to finish | All three jobs (**windows-latest**, **macos-latest**, **ubuntu-latest**) have a green check mark. |
| 2 | Click the **ubuntu-latest** job and expand the **Test** step | The output ends with `Test run summary: Passed!` and `total: 172` or more. |
| 3 | In the same job, look at **Check formatting** | It has a check mark. (GitHub draws step check marks in grey, not green; a failed step shows a red ✗.) |

- [ ] Pass  - [ ] Fail. Notes:

### T2: The tests pass locally (optional)

| # | Do this | Expected result |
|---|---|---|
| 1 | In Rider, open the **Unit Tests** window and run all tests in **AskiiArt.Core.Tests** | All tests pass. The new ones are grouped under `Documents` (cells, rects, canvas sizes, layers, frames, change events, resize plans) and `Compositing`. |
| 2 | Run the app (`dotnet run --project src/AskiiArt.App`) | It starts and looks exactly as it did after step 0.3. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Nothing user-facing yet. If you read one test file, `tests/AskiiArt.Core.Tests/Compositing/CompositorTests.cs` shows the layering rules in small pictures, with `·` meaning an empty cell.

## Known limitations (not bugs)

- No UI uses the model yet; the canvas arrives in step 1.4.
- Changes can't be undone yet, and nothing outside Core can change a document. Commands and undo are step 1.2.
- The document doesn't record a font yet; that arrives with fonts and the font picker (steps 1.3 and 1.6).

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 1.1, test T<n>, row <#>
Run URL: <link to the failed run on GitHub>
Did: ...
Expected: ...
Actually: ...
Log: <expand the failed step and paste its last 30 or so lines>
```
