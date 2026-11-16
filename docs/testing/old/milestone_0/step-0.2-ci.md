<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 0.2: CI, manual test

**Build:** working tree
**Test on:** - [ ] GitHub (one push covers Windows, macOS and Linux)
**Time needed:** about 10 min, most of it waiting for the run

## What changed

Every push and pull request now starts a GitHub Actions run that builds the solution and runs all tests on Windows, macOS and Linux. On Linux it also checks code formatting, which is the one check Rider doesn't run for you. The README shows a badge with the latest result.

## Setup

1. Commit the changes from this step and push them to GitHub (in Rider: **Git → Commit**, then **Git → Push**).
2. Open https://github.com/rmfandyplayz/AskiiGraphik-Studio/actions in a browser.

## Tests

### T1: The push starts a run

| # | Do this | Expected result |
|---|---|---|
| 1 | Look at the Actions page right after pushing | A run named **CI** appears at the top, labelled with your commit message, branch `master` and the trigger `push`. |
| 2 | Click the run | Three jobs are listed: **Build and test (windows-latest)**, **Build and test (macos-latest)** and **Build and test (ubuntu-latest)**. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: All three platforms go green

| # | Do this | Expected result |
|---|---|---|
| 1 | Wait for the run to finish (a few minutes; the first run is slower because nothing is cached yet) | All three jobs have a green check mark, and the run as a whole shows **Success**. |
| 2 | Click the **ubuntu-latest** job and expand the **Test** step | The output ends with `Test run summary: Passed!` and `total: 5`. |
| 3 | In the same job, look at the step list | **Check formatting** ran and is green. In the Windows and macOS jobs, that step is shown as skipped (greyed out), which is expected. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: The README badge

| # | Do this | Expected result |
|---|---|---|
| 1 | Open https://github.com/rmfandyplayz/AskiiGraphik-Studio | Near the top of the README, a badge reads **CI \| passing** in green. (It can take a minute to update after the run finishes.) |
| 2 | Click the badge | It opens the CI workflow's page on the Actions tab. |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Manual run button

| # | Do this | Expected result |
|---|---|---|
| 1 | On the Actions page, click **CI** in the left sidebar | A **Run workflow** button appears on the right. |
| 2 | Click **Run workflow**, keep branch `master`, and confirm | A new run starts, with the trigger `workflow_dispatch`, and finishes green like the first one. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- On a throwaway branch, break a test on purpose (for example, change `SKColors.Black` to `SKColors.White` in the `Assert.Equal` line of `tests/AskiiArt.Imaging.Tests/SkiaNativeLibraryTests.cs`) and push it. All three jobs should fail at the **Test** step, and the other OSes keep running when the first one fails. Delete the branch afterwards.
- Push twice quickly to the same branch: the first run should be cancelled in favour of the second.

## Known limitations (not bugs)

- The badge only shows the status of `master`, not of other branches.
- CI only builds and tests. Producing downloadable app packages comes in M9.
- If a pull request comes from a branch in this same repo, both the push and the pull request start a run, so you see two runs. That's harmless.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 0.2, test T<n>, row <#>
Run URL: <link to the failed run on GitHub>
Did: ...
Expected: ...
Actually: ...
Log: <expand the failed step and paste its last 30 or so lines>
```
