<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0051: Built with the .NET 11 SDK

Status: Accepted

## Context

In step 3.4, an IDE update replaced the owner's .NET 10 SDK with the .NET 11 SDK, and the owner moved `global.json` to 11.0.100. ARCHITECTURE §3 keeps .NET 10 as the runtime: it's the LTS release, supported to November 2028, and Avalonia 12 targets it.

A newer SDK builds older targets, and the build was clean. Two things changed underneath, though:

- The .NET 11 SDK doesn't include the .NET 10 runtime that the app and the tests run on.
- `LangVersion latest` means the newest C# the **SDK** knows (C# 15), not the newest the target supports. C# 15 isn't supported on .NET 10, and some of its features need runtime support that .NET 10 lacks.

## Decision

- **Build with the .NET 11 SDK** (`global.json`, ADR 0002) and keep **targeting .NET 10**.
- **CI installs .NET 10 beside the SDK** (ADR 0005), rather than relying on the runner image to have it.
- **Pin `LangVersion` to 14** in `Directory.Build.props`, the newest C# that .NET 10 supports. Move it when the target framework moves.

## Alternatives

- **Roll back to the .NET 10 SDK.** It works, but the owner's IDE keeps pushing the newer SDK, so it would keep breaking locally.
- **Target .NET 11.** It isn't an LTS release, so it would leave support well before .NET 10 does.
- **Keep `latest`.** C# 15 features would compile but could fail on the .NET 10 runtime, or behave in ways the docs for .NET 10 don't describe.

## Consequences

- Contributors need the .NET 11 SDK and the .NET 10 runtime (README).
- Language upgrades are a deliberate change to `Directory.Build.props`, not a side effect of updating the SDK.
