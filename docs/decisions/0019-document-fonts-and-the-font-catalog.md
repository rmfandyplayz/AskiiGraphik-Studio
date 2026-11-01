<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0019: Document fonts and the font catalog

Status: Accepted

## Context

Step 1.6 adds a font picker: the bundled fonts, installed fonts, and fonts loaded from a file (ARCHITECTURE §11). §5.10 says a document stores a font reference ("family name, style and size"), not a file path, and falls back to the default font when the font is missing. In 1.6 the owner chose: the picker chooses only the font, 100% zoom stays at 16 px for every document (PNG export will have its own scale in M7), and fonts loaded from a file are remembered only until the app closes, because the settings file that can store a user font list arrives in M2.

## Decision

- **`FontReference`** (Core) holds just the family name, compared without case. `Document.Font` defaults to `FontReference.Default` (JetBrains Mono, kept in step with the bundled default by a test) and changes only through `SetFontCommand`, which raises a settings change and is undoable. Style is always Regular and the size isn't stored; either can join the reference if a later step needs it.
- **`FontCatalog`** (App) lists the choices (bundled first, then fonts loaded from files, then installed families not already listed) and turns a reference into a `LoadedFont`, falling back to the default. It loads each font once and keeps it until the app closes, because glyph atlases are cached per loaded font.
- **Same family name:** documents store names, so two fonts with the same name can't be told apart. When a loaded file's family is already available (bundled, installed or loaded earlier), the font already there is used and the picker says so.
- **Monospace check:** run when a font is selected in the picker, cached per font; a font that fails is allowed, with a warning (§11).
- The canvas view model re-resolves the font on every settings change, so picking a font or undoing it redraws with the new cell size.

## Alternatives

- **Store the font file's path:** works for file fonts across restarts, but breaks on another computer and goes against §5.10.
- **A file font replaces a bundled or installed one with the same name:** lets a user try a newer version, but the document couldn't say which one it meant when reopened.
- **Persist loaded fonts now:** needs the settings store, which M2 brings for the dock layout anyway.

## Consequences

- A document whose font came from a file falls back to the default font once the app restarts, until the user font list is stored in settings (M2) and Open (1.7) tells the user about the fallback.
- Hundreds of installed fonts are listed by name only; nothing is loaded until one is selected.
