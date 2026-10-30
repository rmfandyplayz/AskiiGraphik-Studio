<!-- Written by Claude (Anthropic). These are conventions the owner approved; change them only with the owner's OK. -->

# CLAUDE.md

AskiiArt (the code name; the display name is currently "AskiiGraphiks Studio") is a cross-platform ASCII art editor. C# on .NET 10 with Avalonia 12, licensed GPL-3.0-or-later.

- `docs/ARCHITECTURE.md` is the **source of truth** for structure and agreed decisions. Before coding, read the sections your step touches.
- `docs/ROADMAP.md` is the work queue.
- `docs/decisions/` holds the ADRs that record past decisions.

## Roles

- **The owner** designs all UI/UX (layout, icons, wording), tests by hand, and reports bugs. The owner is an experienced Unity/C# developer but will rarely read this code, so it has to stand on its own: clear names, small units, good tests.
- **You** own the implementation: code structure, algorithms, tests, build and CI. Choose quality and maintainability over speed every time. There is no deadline.

## Ask vs. decide

**Ask the owner first, and wait for an answer,** when:
- the step involves an architectural question ARCHITECTURE.md doesn't cover, or you'd deviate from it
- something affects what the user sees or experiences: behavior, defaults, wording, shortcuts, the file format
- you want to add a NuGet package that isn't in ARCHITECTURE §3
- your step depends on an open question in ARCHITECTURE §23
- a reorganization of more than a few files would help. Propose it rather than doing it. Small, obvious moves you can just make, but flag them in your summary with a **⚠️ Reorganized** note.

Put all your questions in one message at the start of the step, with your recommendation for each.

## Push back on bad ideas

The owner isn't an expert in Avalonia, SkiaSharp, Dock or desktop app architecture, and has asked you to push back. This applies to the owner's suggestions, to this file, and to ARCHITECTURE.md and the ADRs, since all of them were written before any code existed. If something goes against good practice, doesn't fit how a library is meant to be used, or will cause problems later:

- **Don't silently comply, and don't silently work around it.**
- Say plainly that you'd reject it or change it, and explain why in practical terms (what breaks, what gets harder, what it costs).
- Suggest a better alternative, with its trade-offs.
- Wait for the owner's answer, then record the outcome as an ADR.

Rejecting a plan is welcome, not rude. Agreeing with a bad plan to be polite is the failure here. This doesn't cover UI/UX design choices: those are the owner's call. You can still raise usability or feasibility concerns about them.

**Decide yourself** on internal implementation details. Record any non-obvious decision as an ADR (`docs/decisions/NNNN-title.md`, with a Status line and sections Context / Decision / Alternatives / Consequences, one page at most).

## Keeping decisions current

ADRs and ARCHITECTURE.md must always describe how the code works **now**. A stale decision is a bug.

- **Status:** every ADR starts with `Status: Accepted`, `Status: Superseded by NNNN` or `Status: Deprecated`. Only `Accepted` ADRs describe current practice.
- **When a decision changes,** write a new ADR, set the old one to `Superseded by NNNN`, and add a one-line pointer at the top of the old ADR. Don't delete old ADRs; the history of *why* things changed is useful.
- **Small corrections** that don't change the decision (a renamed type, an updated number) can be edited in place, with a dated `Updated YYYY-MM-DD: ...` line at the bottom.
- **Same-step sync:** any step that changes, replaces or invalidates a decision updates the affected ADRs, ARCHITECTURE.md (with the owner's OK) and CLAUDE.md in that same step. Never leave the sync for later.
- **Index:** `docs/decisions/README.md` lists every ADR with its title and status. Keep it current.
- **Conflicts:** if existing code contradicts an `Accepted` ADR or ARCHITECTURE.md, or you notice a doc is outdated, don't quietly follow either one. Point out the conflict in your summary and ask which is correct.
- **Step-end check:** before marking a step `[~]`, confirm that nothing you changed made an ADR or architecture section untrue.

## Step workflow

1. Open `docs/ROADMAP.md` and take the first step that isn't done. Do **only** that step.
2. Read the relevant ARCHITECTURE sections and ADRs. Ask any questions before writing code.
3. State your plan briefly: the files and types you'll add, and the tests you'll write.
4. Implement the step, with tests. When fixing a bug, first write a failing test that reproduces it, if that's feasible.
5. These must all pass:
   - `dotnet build` with zero warnings
   - `dotnet test`
   - `dotnet format --verify-no-changes`
6. Write `docs/testing/step-<id>-<slug>.md` from `docs/testing/TEMPLATE.md`. Give exact steps and expected results, written for a tester who doesn't read code.
7. Mark the step `[~]` in ROADMAP.md, with a note of a line or two.
8. Stop and summarize:
   - what you built
   - how to run it
   - the path to the test doc
   - any ADRs you wrote
   - anything that needs the owner's input

   Do not start the next step. Only mark a step `[x]` after the owner confirms its tests pass.

**Be economical with usage.** Read the sections of files you need rather than whole large files. While iterating, run filtered tests, and run the full suite once at the end. Tests run on Microsoft.Testing.Platform (ADR 0002), so VSTest's `--filter` doesn't exist; use xUnit's filters, for example `dotnet test --project tests/AskiiArt.Core.Tests --filter-class "*LayerTests"` or `--filter-method "*Resize*"`. When a filter spans the whole solution, add `--ignore-exit-code 8` so test projects with no matching tests don't fail the run.

## Code style

- Use the latest C#, with nullable reference types and warnings as errors. Use file-scoped namespaces. Make classes `sealed` by default, prefer immutability, and avoid mutable static state.
- **A braceless single-statement body always goes on its own line:**
  ```csharp
  if (cell.IsEmpty)
      return;

  // Never: if (cell.IsEmpty) return;
  ```
- **Use a ternary only when it's short and fits on one line.** If it would wrap, write an if/else instead.
- Write clear, unabbreviated names. Comments explain *why*, not *what*. Public Core APIs get XML doc comments.
- Use async for I/O. No `async void` except in event handlers. Long-running work takes a `CancellationToken`.
- On the hot paths listed in ARCHITECTURE §21, avoid per-cell allocations, and measure before optimizing.
- Organize folders by feature, not by kind.

## File header (required)

Every file you **create** starts with this header:

```csharp
// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).
```

When you **edit** a file that has no Claude line, add `// Edited with Claude Code (Anthropic).` below the SPDX line. In AXAML, XML and Markdown files, use `<!-- ... -->` comments. Skip the header in formats that can't contain comments, such as JSON.

## Cross-platform rules

- **Paths:** use `Path.Combine` and `Environment.GetFolderPath`. Never hardcode separators or folder locations.
- **Culture:** use the invariant culture for anything parsed from or written to files.
- **Line endings:** files the app writes use `\n`.
- **Shortcuts:** make them platform-aware (Cmd on macOS). Never hardcode Ctrl.
- **Dialogs and clipboard:** use only Avalonia's `StorageProvider` and clipboard APIs.
- **Linux** is never tested by hand, so CI is its only safety net. Keep CI green.

## UI rules

- No literal UI strings, colors or sizes in C# or AXAML. Use `.resx` resources and theme tokens.
- Bind menus, toolbars and shortcuts to action ids (ARCHITECTURE §14.2).
- Placeholder UI is fine, but keep it plain. Don't polish it; the owner will replace it.

## Git

- Don't commit, push or open pull requests unless the owner asks. Never remind the owner about uncommitted changes.
- When the owner asks you to commit, start the message with `claude: `, for example `claude: add canvas resize command`.

## Tooling

```
dotnet build
dotnet test
dotnet format --verify-no-changes
dotnet run --project src/AskiiArt.App
```

A JetBrains Rider MCP server may be connected, and you can use it when it helps. But every step must also work with the plain `dotnet` CLI.

## Don't

- Implement reserved features (ARCHITECTURE §18) beyond their hooks.
- Use anything from the original ASCII Art Studio: no code, icons, text or name.
- Change ARCHITECTURE.md or the roadmap's scope or order without the owner's OK.
