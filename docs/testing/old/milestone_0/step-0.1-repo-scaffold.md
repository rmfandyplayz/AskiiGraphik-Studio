<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 0.1: Repo scaffold, manual test

**Build:** working tree
**Test on:** - [ ] Windows  - [ ] macOS  (Linux is covered by CI only)
**Time needed:** about 10 min per OS

## What changed

The repo now has its real structure: one solution (`AskiiArt.slnx`) with three app projects (Core, Imaging, App) and a test project for each. Shared build settings, pinned package versions, the code-style rules, the license and the decision records are in place. There's nothing to use yet; this step only proves that the project builds and its tests pass on your machines.

## Setup

1. Install the .NET 10 SDK, version **10.0.403 or any newer 10.0.x** (https://dotnet.microsoft.com/download/dotnet/10.0).
2. Open a terminal in the repo root (the folder that contains `AskiiArt.slnx`).
3. **Rider users:** close the old solution first. Then open `AskiiArt.slnx`. Rider makes a fresh `.idea` folder for it. The old `.idea/.idea.AskiiGraphikStudio` folder is no longer used, and you can delete it.

## Tests

### T1: The right SDK is picked up

| # | Do this | Expected result |
|---|---|---|
| 1 | Run `dotnet --version` | Prints `10.0.403` or a newer `10.0.x` version. If it prints an error saying a compatible SDK wasn't found, install the SDK from Setup step 1. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: The solution builds cleanly

| # | Do this | Expected result |
|---|---|---|
| 1 | Run `dotnet build` | Ends with `Build succeeded`, followed by `0 Warning(s)` and `0 Error(s)`. The first run downloads packages and takes longer. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: All tests pass

| # | Do this | Expected result |
|---|---|---|
| 1 | Run `dotnet test` | Three lines that each end in `passed`, one per test project (Core, Imaging, App). |
| 2 | Look at the summary at the end | `Test run summary: Passed!`, with `total: 5`, `failed: 0`, `succeeded: 5`. |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Code formatting check passes

| # | Do this | Expected result |
|---|---|---|
| 1 | Run `dotnet format --verify-no-changes` | Finishes without printing any errors. |

- [ ] Pass  - [ ] Fail. Notes:

### T5: The app starts

| # | Do this | Expected result |
|---|---|---|
| 1 | Run `dotnet run --project src/AskiiArt.App` | An empty window opens. It has no title, no menu and no content; that's expected for now. |
| 2 | Close the window | The command in the terminal ends, and you get your prompt back. |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Rider sees the solution (optional, if you use Rider)

| # | Do this | Expected result |
|---|---|---|
| 1 | Open `AskiiArt.slnx` in Rider | The Solution view shows `src` (AskiiArt.App, AskiiArt.Core, AskiiArt.Imaging) and `tests` (three test projects). |
| 2 | Build the solution in Rider | Build succeeds with no warnings. |
| 3 | Run all tests from Rider's Unit Tests window | 5 tests, all passing. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- On the Mac, run T2 and T3 in a fresh clone, to make sure nothing depends on files that only exist on your Windows machine.

## Known limitations (not bugs)

- The window is empty and untitled. The menu, title, strings and theme come in step 0.3.
- There's no CI yet. That's step 0.2.
- `dotnet test --filter` isn't supported. The tests run on a newer test runner with different filter options (see ADR 0002). It doesn't affect anything you do by hand.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 0.1, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <paste the full terminal output of the failing command>
```
