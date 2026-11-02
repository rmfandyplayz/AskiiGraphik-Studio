<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0031: A saved file remembers its active layer

Status: Accepted

## Context

ADR 0030 kept the active layer out of the file, so a reopened document always started on its top layer. After testing 2.3, the owner asked for the file to remember it, so reopening picks up where you left off.

## Decision

- **The file has an optional top-level `activeLayer`,** the layer's id. Saving always writes it. It stays out of `DocumentJson`'s document reading: `DocumentFileContents.ActiveLayerId` carries it, and `DocumentFiles` applies it to the new session.
- **It still belongs to the session, not the document.** Choosing a layer doesn't mark the document unsaved and isn't undone, so changing only the active layer and closing asks nothing, and that choice is lost. The next real save records it.
- **Reading it is lenient, unlike the rest of the file (ADR 0021).** A value that isn't a known layer's id is ignored and the top layer is used. It only saves a click, so it isn't worth refusing a file over.
- **The format version stays 1.** The field is optional and readers ignore fields they don't know, so older builds still open these files, and new builds open older files.

## Alternatives

- **Moving the active layer into `Document`:** `load(save(doc)) == doc` would cover it, but every view of the document would then see a value that isn't content, and it would have to change outside commands, which ADR 0010 rules out.
- **Marking the document unsaved when the active layer changes:** the choice would never be lost, but clicking a row would ask to save on close, which feels wrong for something that changes nothing in the art.
- **Bumping the format version:** nothing needs migrating, and older builds would refuse files for no gain.

## Consequences

- A hidden or locked layer can be the one reopened on, just as it could be active before saving.
- `DocumentFileFormat.SaveAsync` gained an `activeLayerId` parameter. It's optional, so code saving a document with no session behind it can leave it out.
