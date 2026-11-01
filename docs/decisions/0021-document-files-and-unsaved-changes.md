<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0021: Document files and unsaved changes

Status: Accepted

## Context

Step 1.7 adds Save, Save As and Open. ARCHITECTURE §13 sketches the format: a plain variant (a text header, then UTF-8 JSON) and a compressed one (magic bytes, a version, then Brotli-compressed JSON), sniffed on open, versioned, with atomic saves and a round-trip guarantee. The owner chose two extensions, `.asciiart` and `.asciiartz`, so the save dialog's file type picks the variant. The owner also chose that the app starts with an empty document, and that a document whose font is missing keeps asking for it and says so once. Saving writes in the background, so the user can type while a save is in progress.

## Decision

- **Headers** (`Core/Formats/DocumentFileFormat`):
  - The plain variant starts `ASKIIART <version>\n`.
  - The compressed one starts with 8 bytes `89 41 53 4B 5A 0D 0A 1A`, then the version as a little-endian `int32`.
  - Open looks at the header, not the name.
  - A byte-order mark and a `\r\n` after the plain header are accepted, for files a text editor has saved.
  - The extensions and `CurrentVersion` (1) are constants.
- **The JSON** (`DocumentJson`) is identical in both variants:
  - The top level holds `canvas` (width, height), `characterSet` (`unicode` or `ascii`), `font` (the family name), and `frames`. Version 1 has exactly one frame.
  - Each frame holds `layers`, bottom first. A layer has `id`, `name`, `visible`, `locked`, `rows` and `occupancy`. `opacity` and `blendMode` are written only when they're not the default.
  - `rows` holds one string per row, with empty cells written as spaces.
  - `occupancy` lists run lengths of cells, row by row, alternately empty and occupied, starting with empty. It's on one line so it doesn't bury the art.
  - Non-ASCII characters are written as they are, not as `\u` escapes.
- **Reading is strict.** A missing or wrong value, a row of the wrong length, runs that don't cover the canvas, duplicate layer ids, a control character, or non-ASCII in an ASCII-only document makes the file `Damaged`. Version 1 never opens part of a document.
  - The bytes must be valid UTF-8 before parsing.
  - A file above 1 GiB, or one that would decompress past it, is refused.
  - `DocumentFileException.Problem` tells `Damaged`, `NotADocument` and `NewerVersion` apart.
- **Migrations:** each `IFormatMigration` changes the JSON tree from one version to the next before it's read. A test fails if `CurrentVersion` goes up without a migration from the version before it.
- **Atomic save:** the file is written to `.<name>.<guid>.tmp` in the same folder and flushed to disk. It then replaces the target with `File.Move(overwrite)`. The temporary file is deleted if anything fails.
- **Saving while editing:** the document is turned into JSON on the UI thread. Compressing and writing happen in the background. `CommandHistory.BeginSave` returns a `SavePoint` and stops merging into the step being saved. `MarkSaved(SavePoint)` then marks that state as saved, so anything typed during the write stays unsaved.
- **App:** `DocumentFiles` handles Save, Save As, Open, `ConfirmDiscardingChangesAsync` and `ReplaceWithNewAsync`.
  - New, Open, the Debug sample and closing the window all ask first when there are unsaved changes. Save is the default answer; Cancel and Esc stop the action.
  - `ActiveDocument.File` holds the file's path and variant.
  - `WindowTitle` shows the file name (or "Untitled"), with `*` while there are unsaved changes.
  - To ask before closing, the window's `Closing` is cancelled, the question is asked, and if the user goes ahead the window is closed again after the event.
  - Save As: a name ending in one of the two extensions chooses that variant. Otherwise the chosen file type decides, and that type's extension is added if the name has none.
  - Open keeps the document's font reference. It shows a message, and the status bar shows "*name* (not available)".
  - The app starts with an empty untitled 80 × 25 ASCII-only document. The 500 × 500 sample is under Debug.

## Alternatives

- **Rows as raw text after the JSON, outside it:** the art would read exactly as drawn, with no escaped `\` or `"`. But it means two parsers, a format that invites hand edits, and a change to §13. Not done unless the owner wants it.
- **Occupancy implied by non-space characters, with a mask for spaces only:** friendlier to hand edits, but adds rules. The full mask is simpler and exact.
- **`MarkSaved()` after the write:** wrong whenever the user types during the write.
- **`File.Replace`:** behaves differently on Windows and needs the target to exist already.

## Consequences

- In the plain file, JSON escapes backslashes and quotes (`\\`, `\"`), so art that uses them looks shifted in a text editor. Export .txt (1.8) gives clean text.
- Every format change bumps `CurrentVersion` and adds a migration. Files from a newer app are refused with a message, never guessed at.
- A 4096 × 4096 canvas pauses briefly while it's turned into JSON on the UI thread.
- macOS window behaviour is in ADR 0023; Copy as Text and Export as Text are in ADR 0022.
- On macOS the save dialog adds the chosen type's extension itself, so a name ending in the other extension gets both (`cat.asciiartz.asciiart`, saved plain). Choosing the type in the dialog works on every OS.

Updated 2026-11-01: `DocumentJson.Parse` also checks every escaped string and property name before parsing (step 1.8). An escape that makes no sense, such as half of a surrogate pair, is `Damaged`; it used to surface as an unexpected exception, which the corruption test found on macOS. The atomic write is now `AtomicFile`, shared with Export as Text (ADR 0022). After a Quit, Save or Don't Save asks for shutdown again rather than closing only the window (ADR 0023).
