<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0003: Use the .slnx solution format

Status: Accepted

## Context

ARCHITECTURE §4 originally named the solution `AskiiArt.sln`. The project template the repo started from created both a `.sln` and a `.slnx`. With two solution files in one folder, a bare `dotnet build` stops with error MSB1011 ("more than one project or solution file"), so one has to go.

## Decision

The solution is `AskiiArt.slnx`, the XML solution format. It's the default for `dotnet new sln` in the .NET 10 SDK, and Rider, Visual Studio and the `dotnet` CLI all support it. Projects are grouped into `src` and `tests` solution folders that mirror the directories. Approved by the owner.

## Alternatives

- **The classic `.sln` format.** It works everywhere, but it's verbose, full of GUIDs, and produces noisy merge conflicts. Nothing in this project needs it.

## Consequences

- `dotnet build`, `dotnet test` and `dotnet format` at the repo root find the solution without arguments.
- Old tools that only understand `.sln` can't open the solution. None of the tools this project uses has that limitation.
