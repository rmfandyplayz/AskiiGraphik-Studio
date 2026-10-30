<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0002: Test stack and version pins

Status: Accepted

## Context

ARCHITECTURE §3 picks xUnit, Verify, a property-based library and Avalonia.Headless, and asks us to confirm that they work together. When this was checked (2026-10-29), the latest stable versions did not:

- **Avalonia.Headless.XUnit 12.1.4** is built against xUnit v3 **3.2.2**. On xunit.v3 4.0.2, every `[AvaloniaFact]` test fails during discovery with a `MissingMethodException`.
- **Verify.XunitV3 33.x** requires xUnit v3 4.x. Verify 33 also adds a "maintenance fee" check that fails the build unless the project declares a sponsorship or an exemption. The exemption that applies to us (non-revenue open source) expires after at most 12 months, so CI would break each time it lapses.
- **Verify.XunitV3 32.0.1** is the last release on xUnit v3 3.x. It's MIT-licensed and has no build check.

## Decision

- **xunit.v3 3.2.2** and **Verify.XunitV3 32.0.1**, pinned in `Directory.Packages.props`.
- **CsCheck** for property-based tests (approved by the owner; pinned when first used, in step 1.2).
- **Avalonia.Headless.XUnit** at the same version as every other Avalonia package.
- **Tests run on Microsoft.Testing.Platform (MTP),** set in `global.json`. xUnit v3 test projects are self-hosting executables (`OutputType` Exe, set in `tests/Directory.Build.props`), and `Microsoft.NET.Test.Sdk` isn't needed.
- **`global.json` pins the SDK to 10.0.403** with `rollForward: latestFeature`, so any newer .NET 10 SDK also works.

## Alternatives

- **xUnit v3 4.x and Verify 33.x without headless UI tests.** That drops the only automated coverage of action wiring and canvas input (ARCHITECTURE §19).
- **Different xUnit versions per test project** (via `VersionOverride`). Two xUnit majors in one repo means two sets of rules for anyone writing tests.
- **The VSTest runner** (`Microsoft.NET.Test.Sdk` plus `xunit.runner.visualstudio`). It works, but MTP is xUnit v3's native mode and the direction .NET 10 is going.
- **FsCheck** for property tests. It's capable, but its API is designed for F#; CsCheck is designed for C#.

## Consequences

- `dotnet test --filter` (a VSTest option) doesn't exist under MTP. Use xUnit's `--filter-class`, `--filter-method`, `--filter-trait` or `--filter-query`, and add `--ignore-exit-code 8` when filtering across the whole solution (exit code 8 means "zero tests ran"). CLAUDE.md says this.
- **Revisit** when an Avalonia.Headless.XUnit release supports xUnit v3 4.x. Moving to Verify 33+ then needs the owner to choose between a sponsorship, a renewable exemption, or staying on Verify 32.
