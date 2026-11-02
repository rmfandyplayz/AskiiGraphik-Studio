<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0026: The settings file

Status: Accepted

## Context

The saved panel layout (ADR 0025) is the first setting. ARCHITECTURE §15 asks for versioned JSON in the platform's app-data folder, and says bad settings must never stop the app. ADR 0006 left one thing open: on macOS, .NET's roaming folder is `~/.config`, not the Application Support folder §15 names.

## Decision

- **File:** `settings.json`, in a folder named after the app: `%APPDATA%` on Windows, `~/Library/Application Support` on macOS and `$XDG_CONFIG_HOME` (`~/.config`) on Linux. On macOS that means .NET's `LocalApplicationData` rather than `ApplicationData`. Logs stay in the per-machine folder (ADR 0006).
- **Format:** a JSON object with a `version` number, written atomically with `\n` line endings. `AtomicFile` gained a synchronous `Write`, because the layout is saved while the app is shutting down, and an unfinished asynchronous write would be lost.
- **API:** `IAppSettings` stores text by key and saves on every change; failing to save is only logged. Tests use an in-memory fake, so they never touch the real file.
- **A damaged file**, or one with a newer version, is renamed to `settings.json.damaged-<time>`, the defaults apply, and the user is told once when the window opens. A file that merely can't be read is left alone, because it may be fine.
- **The layout is one text value**, Dock's own JSON. A layout that can't be read or used (for example after a Dock update, or one lacking a panel the shell needs) falls back to the default and is logged without bothering the user, because losing it costs little. It's text rather than nested JSON so Dock's depth and `$id` references can't affect reading the rest of the file.

## Alternatives

- **A separate layout file:** isolates the layout, but every later setting would then need its own place too, and §15 puts the layout in settings.
- **Nested JSON for the layout:** nicer to read, but nobody needs to read it, and it needs raised depth limits to parse.

## Consequences

- Windows and Linux settings can roam or be backed up apart from logs.
- There's no migration code yet, since version 1 is the only version. A file from a newer version counts as damaged, which also keeps a copy of it.
