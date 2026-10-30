<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0009: UI strings and theme tokens

Status: Accepted

## Context

ARCHITECTURE §14.5 requires every user-visible string to live in `.resx` files with strongly typed access, and placeholder UI to use only design tokens (colors, spacing, sizes), so the owner's design can replace them in one place.

## Decision

- **Strings:** one file, `src/AskiiArt.App/Resources/Strings.resx`. MSBuild's own resource generator (`StronglyTypedLanguage` on the `EmbeddedResource`) generates the `Strings` class at build time. It doesn't depend on an IDE code generator, and no generated file is committed.
  - The class is **public** because AXAML's `{x:Static res:Strings.X}` only resolves public members.
  - A small target also runs the generator in **design-time builds**, which Rider and `dotnet format` use to analyze code. Without it, they report `Strings` as missing.
  - Code that only has a key (action labels, menu titles) uses `UiText.Get(key)`, which throws if the key is missing instead of showing a blank label.
  - An underscore in a menu label marks the Alt access key on Windows and Linux; macOS strips it.
- **Theme tokens:** `src/AskiiArt.App/Theme/Tokens.axaml`, merged into the app's resources on top of Avalonia's Fluent theme. Keys start with `App` (for example `AppSpacingMedium`, `AppSecondaryTextBrush`) so they never collide with Fluent's own keys. Colors have Light and Dark values in `ThemeDictionaries`; sizes and spacing are shared. Placeholder views use `StaticResource` for sizes and `DynamicResource` for colors, so colors follow a theme switch.

## Alternatives

- **Visual Studio's `ResXFileCodeGenerator` with a committed `Strings.Designer.cs`.** The CLI doesn't regenerate it, so it goes stale whenever the `.resx` file is edited outside an IDE.
- **A resx source generator package** (`Microsoft.CodeAnalysis.ResxSourceGenerator`). It would also work in IDEs, but it's another package, and MSBuild already does the job.

## Consequences

- Adding a string means adding one `<data>` entry. A test checks that every string has text and every action has a label.
- Translations later are extra `Strings.<culture>.resx` files with no code changes. `NeutralLanguage` is `en`.
