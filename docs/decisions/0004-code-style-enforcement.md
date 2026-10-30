<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0004: How code style is enforced

Status: Accepted

## Context

CLAUDE.md sets a code style and requires zero-warning builds plus `dotnet format --verify-no-changes`. Step 0.1 asks for an `.editorconfig` that encodes that style "wherever analyzers can".

## Decision

- **Only the analyzers built into the .NET SDK** are used: `AnalysisLevel` `latest-recommended` plus `EnforceCodeStyleInBuild`, with no third-party analyzer packages. `TreatWarningsAsErrors` turns every warning into a build failure.
- **`.editorconfig` sets these to warning** (so they fail the build):
  - file-scoped namespaces (IDE0161)
  - a braceless body only for a single-line statement, on its own line (IDE0011, IDE2001)
  - formatting (IDE0055), using placement and unused usings (IDE0065, IDE0005)
  - explicit accessibility, readonly fields and modifier order (IDE0040, IDE0044, IDE0036)
  - naming: `I` prefix for interfaces, PascalCase for types, members and constants, and `_camelCase` for private fields (IDE1006)
  - missing XML doc comments, **in `src/AskiiArt.Core` only** (CS1591), since only public Core APIs need them
- **Test names** may use underscores (`Method_Scenario_Result`), so CA1707 is off under `tests/`.
- **Line endings:** every text file in the repo is LF (`.gitattributes` with `eol=lf`, plus `.editorconfig`). Verify snapshot files keep the encoding and line endings Verify writes.

## Alternatives

- **Third-party analyzers** (StyleCop.Analyzers, Roslynator, Meziantou). They cover more rules, but each one is a new package to approve and keep up to date. We can add one later if a real gap shows up.
- **CRLF on Windows** (`text=auto` alone). The same file would then differ between the owner's two machines, and `dotnet format` results would depend on the OS.

## Consequences

Some CLAUDE.md rules have no SDK analyzer and are checked in review only: classes `sealed` by default (CA1852 covers only internal types), short single-line ternaries, unabbreviated names, and no `async void`.
