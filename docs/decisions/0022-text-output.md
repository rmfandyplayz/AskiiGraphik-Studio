<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0022: Text output: Copy as Text and Export as Text

Status: Accepted

## Context

Step 1.8 adds Copy as Text and Export .txt. ARCHITECTURE §16 says text output flattens the visible layers, writes `\n` by default with options for `\r\n` and for trimming trailing spaces, and that exporters implement an `IExporter` interface registered through DI. The owner chose to keep the full rectangle (no trimming), `\n` in files, the platform's own line breaks on the clipboard, Ctrl+Shift+C (Cmd on macOS) for Copy as Text, and no shortcut for Export.

## Decision

- **`IExporter`** (`Core/Exporting`): `Id`, `FileExtension`, and `byte[] Export(Document)`, which reads the document's active frame and so runs on the UI thread. It's kept minimal until a second exporter (PNG and HTML, M7) shows what options look like. DI registers `TextExporter` as itself and as an `IExporter`.
- **`TextExporter`** flattens the active frame row by row with the `Compositor`, using the canvas's policy (`CompositingPolicy.Default`). Empty cells become spaces and every line is the canvas's full width, so the art keeps its shape when pasted.
  - `Export`: UTF-8 without a byte-order mark, with `\n` after every row, the last included.
  - `ToText(document, lineBreak)`: the same rows with no line break after the last, for the clipboard.
- **Copy as Text** (Edit menu): `ToText` with `Environment.NewLine`, then `IAppShell.CopyTextAsync`, which uses Avalonia's clipboard.
- **Export as Text…** (File menu), in `DocumentFiles.ExportAsTextAsync`:
  - The save picker offers "Text file", suggests the document's own name, and starts in its folder.
  - `.txt` is added when the name has no extension.
  - The text is built on the UI thread, then written with `AtomicFile`. That's the atomic write that Save used privately, moved out of `DocumentFileFormat` so both share it.
  - The document keeps its file, title and unsaved state. A failure shows a message and is logged.

## Alternatives

- **Trimming trailing spaces or empty edges:** shorter output, but art whose left side is empty would shift. The owner chose the full rectangle. §16's trim option comes with export options.
- **`\n` on the clipboard everywhere:** simpler, but some Windows apps would paste it as one line.
- **An exporter that writes to a stream:** it could stream large exports, but the document has to be read on the UI thread anyway. Bytes are simpler until PNG needs otherwise.
- **No interface until there's a second exporter:** §16 asks for one, and this one is small.

## Consequences

- The `\r\n` and trimming options in §16 aren't built yet. They need an export options UI, which comes with PNG and HTML export (M7).
- Exporting a 4096 × 4096 canvas pauses briefly while it's flattened, as Save does.
- Wide characters, once allowed, will need a rule here, because one glyph will cover two cells.
