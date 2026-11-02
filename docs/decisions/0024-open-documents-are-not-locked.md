<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0024: Open documents are not locked

Status: Accepted

## Context

In 1.7 testing the owner found that a saved document can be deleted in the OS while it's open in the app, and asked whether the app should refuse that. The app reads a file once when opening it, then keeps the whole document in memory, so a deleted file loses nothing: Save writes it again.

## Decision

The app holds no file open between reads and writes, so it doesn't stop other programs from deleting or changing a document. The owner agreed after hearing the costs below.

A notice comes with M9 instead: when the document's file is deleted or changed outside the app, the app says so (for example, that Save will create the file again).

## Alternatives

- **Holding the file open with sharing denied, on Windows:** the only way to make the OS refuse the delete. Save replaces the file by renaming a finished copy over it (ADR 0021), and a held file blocks that, so every save would release and reopen the file, leaving a gap with no protection. It would also stop other programs from using the file: editing it in a text editor, Git, backup and sync tools. It does nothing on macOS or Linux, where an open file can be deleted anyway. Editors that load the whole file into memory generally don't lock it either.

## Consequences

- Until the notice is built, a deleted or changed file goes unnoticed: Save writes over whatever is there, or creates the file again.
- Other programs can read and edit an open document freely.
