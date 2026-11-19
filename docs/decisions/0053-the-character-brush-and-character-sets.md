<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0053: The Character Brush and character sets

Status: Accepted

## Context

Step 3.6 adds a Character Brush that paints one chosen character, and a Characters window that chooses it. The owner's answers (2026-11-19): the brush stamps the character into every cell under the tip, hard-edged, in the FG and BG colours as typing gives them, with the Brush group's shared tip, one undo step per stroke and Esc to cancel, on key 4 of the Brush group. Alt+click picks the character and both colours; an empty cell picks nothing. Clicking a character in the window chooses it and switches to the brush. The window lists Unicode groups and a Recent list of 16, remembered between runs; search by name waits. In an ASCII document the characters it can't hold are greyed out and Tool Options warns. Tool Options shows the character in a box that opens the window. The default is `#`. The owner also asked for user character sets that work like ramps: built-in ones are read-only, the user's can be added from typed text, duplicated, imported and exported as `.asciicharset` files, and they're listed between Recent and the built-in ones.

## Decision

- **Naming.** The user sees "character set"; the code says **glyph set** (`AskiiArt.Core.GlyphSets`), because `CharacterSet` already means a document's Unicode-or-ASCII choice.
- **`GlyphSet`** is an immutable, ordered list of distinct glyphs that each fill a cell and show ink, so a space can't be one. Typed text is split on spaces and line breaks, and repeats are skipped. `GlyphSet.Check` says why text can't make one: empty, or a character that can't be painted.
- **Built-in sets** are Unicode blocks of single-width characters (Basic Latin, Latin-1, Greek, Box Drawing, Blocks, Braille, Legacy Computing and so on). The window hides the characters no installed font can draw, measured by `GlyphDensityMeter.GetSource` in the canvas's font, so a set never shows a page of placeholder boxes.
- **A shared library layer** (`AskiiArt.Core.Libraries`). Ramps and character sets keep the user's items the same way, so the rules live once: `UserItemList<T>` (stable `user.<guid>` ids, names unique ignoring case and surrounding spaces among the user's and the reserved names, `Add` numbering a taken name), `LibraryJson` (the file header, version check and shared keys), and the `LibraryNameProblem` and `LibraryFileProblem` enums, formerly the ramp-only ones. The App shares `LibraryFilePickers` for the import and export dialogs.
- **`GlyphSetLibrary`** holds the user's sets as `UserGlyphSet(Id, Name, Glyphs)`, oldest first. Recent's name and the built-in names are reserved. It raises `Changed`, and the App saves it to settings (`characterSets`) on every change, as ramps are.
- **Character set files** are UTF-8 JSON with `\n` endings: `format: "askiiart.charset"`, `version: 1`, `name`, `characters` (one string, as typed). A newer version is refused.
- **Recent** (`RecentGlyphs`) is the 16 characters the Character Brush painted with most recently, newest first. It's shared by every document, lives in `ToolSettings`, and is saved (`recentCharacters`) on every change. Choosing a character without painting doesn't add it.
- **The Character Brush** is a `GlyphToolOption` (the character, saved with the other tool options as a one-character string) plus a `FootprintStroke`, the stroke code it now shares with the FG and BG Brushes: the tip moves in straight lines between pointer reports and every covered cell changes the same way. A character the document can't hold paints nothing, and `WarningFor` gives Tool Options its warning.
- **The Characters window** updates its set list in place, so the dropdown keeps its selection while sets change, and edits a user set live as the Ramps window does, with no Undo. Duplicate and Export copy what the set shows, so a built-in set's copy has only the drawable characters.

## Alternatives

- **Naming the type `CharacterSet` in another namespace:** two meanings of one name in the same files.
- **Copying the ramp library code:** two copies of the id, name and file rules would drift apart.
- **Showing every character of a block:** many blocks are mostly boxes on a machine without the right fonts.
- **Adding to Recent on every choice:** clicking through the grid would push out the characters actually used.

## Consequences

- A third library (brush presets, M5 or later) reuses `UserItemList` and `LibraryJson`.
- What a built-in set shows depends on the fonts installed, so its Duplicate or Export can differ between machines.
- Search by name and the window's final look are left for later; the owner plans a dedicated pop-up for both this window and the Ramps window.
