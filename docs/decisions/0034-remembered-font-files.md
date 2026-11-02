<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0034: Fonts loaded from a file are remembered by path

Status: Accepted

## Context

Since step 1.6, a font loaded from a file lasted only until the app closed (ADR 0019), so a document using it fell back to the default font after a restart. The settings file arrived in step 2.1, and the owner asked for this to be fixed within M2.

## Decision

- **The settings file stores the path of each font file** the user loads, as a JSON list under `fontFiles`. `RememberedFontFiles` reads and writes it. Documents still store only the family name (§5.10).
- **`FontCatalog` loads the remembered files when it's created.** There are few of them, and the catalog needs each one's family name to list it.
- **Only a file whose font was actually added is remembered.** A file whose family is already available, or that won't load, isn't.
- **A file that can't be loaded on a later run is skipped** with a warning in the log, and stays remembered. It may be on a drive that's unplugged for now, and the font comes back along with the file.
- **If a remembered font's family has since become available** (installed, say), the one already there is used, as in ADR 0019.

## Alternatives

- **Copy each font file into the app's own folder:** survives the original being moved or deleted, but duplicates the file, may go against the licence of a commercial font, and hides where the font lives. This could be added later without changing documents.
- **Store the family name with the path, and load only when needed:** starts slightly faster and never touches a font that isn't used, but the list could go stale when a file changes. It isn't worth it for a handful of fonts.
- **Forget a file as soon as it can't be found:** keeps the list tidy, but loses fonts on an unplugged drive.

## Consequences

- Moving a font file makes its font leave the picker. Loading it from the new place works, and the old path stays in the list, adding one line to the log at each start.
- There's no way to remove a font from the picker except moving or deleting its file. Removal is more than a list edit, because a loaded font must stay loaded until the app closes (ADR 0019).
- Settings roam on Windows, so another computer may have paths that don't exist. Those fonts are skipped there.
