<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0005: CI workflow

Status: Accepted

## Context

Step 0.2 adds GitHub Actions CI that builds and tests on Windows, macOS and Ubuntu on every push and pull request. Linux is never tested by hand, so CI is its only safety net. The owner works mostly in Rider, so CI is also where the CLI-only check (`dotnet format --verify-no-changes`) runs.

## Decision

One workflow, `.github/workflows/ci.yml`, with one job run on a matrix of `windows-latest`, `macos-latest` and `ubuntu-latest`:

- **Triggers:** every push to any branch, every pull request, and a manual "Run workflow" button.
- **Steps:** restore, build, test, and (Linux only) the format check. The format check runs once because formatting doesn't depend on the OS: every file is LF (ADR 0004).
- **Release configuration.** That's what ships; the owner's local runs cover Debug.
- **`fail-fast: false`,** so one run shows every OS that's broken.
- **The SDK version comes from `global.json`,** so CI and local builds use the same SDK.
- **The NuGet package folder is cached,** keyed on the files that decide which packages are restored.
- **Failed runs upload Verify's `*.received.*` files** as an artifact, so a snapshot difference can be inspected without rerunning locally.
- **Every run that gets as far as testing uploads the font sheets** (`artifacts/font-sheets/`, one artifact per OS, kept 14 days), so the owner can check each OS's glyph rendering by eye (ADR 0014).
- **Least privilege:** the workflow token can only read the repo, and a newer push cancels an older run on the same branch.
- **Action versions:** GitHub's own `actions/*` are pinned to a major version tag (`@v7`). Any third-party action added later is pinned to a full commit SHA.

## Alternatives

- **Pin every action to a commit SHA.** This is the strictest supply-chain practice, but without Dependabot updating them, the SHAs go stale silently and are harder to read. GitHub's own actions are a small risk.
- **Dependabot for actions and NuGet.** It would open pull requests automatically, which is a workflow change for the owner. It wasn't part of this step, and can be proposed separately.
- **Format check on all three OSes.** It costs runner minutes and catches nothing extra.
- **A `packages.lock.json` per project** (for `setup-dotnet`'s built-in cache). It adds files to every project for little gain over keying the cache on the project files.

## Consequences

- A broken build, failing test or formatting change fails CI on the affected OS. The README badge shows the status of the default branch.
- The badge URL contains the GitHub repo name, so it needs updating if the repo is renamed.

Updated 2026-10-31: added the font-sheet upload (step 1.3).
