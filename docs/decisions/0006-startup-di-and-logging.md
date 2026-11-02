<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0006: Startup, dependency injection and logging

Status: Accepted

## Context

Step 0.3 adds the DI container and a rolling log file that the owner attaches to bug reports. ARCHITECTURE §3 names Microsoft.Extensions.Logging and "a rolling file sink" without choosing one. §20 puts logs in "the app-data folder" without saying which one.

## Decision

- **Composition root:** `Hosting/AppServices.Create` builds the one `ServiceProvider`, with `ValidateOnBuild` on, so a missing registration fails at startup instead of on first use. `Program.Main` builds it **before** Avalonia starts, so a failure during Avalonia's startup is still logged. Disposing the provider at exit flushes and closes the log.
- **Logging:** app code only uses Microsoft's `ILogger`. Serilog (`Serilog.Extensions.Logging` plus `Serilog.Sinks.File`, Apache-2.0, approved by the owner) only writes the file. Messages are `[LoggerMessage]` source-generated methods, which is what CA1848 asks for.
- **File:** `log-YYYYMMDD.txt`, one per day, a new file at 10 MB, and the 7 newest files kept. Writes are unbuffered, so the last messages before a crash are on disk. The minimum level is Debug in Debug builds and Information in Release.
- **Line endings:** a small formatter rewrites `\r\n` to `\n` (Serilog and stack traces use the OS line ending), because files the app writes use `\n`.
- **Location:** `Environment.SpecialFolder.LocalApplicationData` + `AskiiGraphiks Studio/Logs`, which gives:
  - Windows: `%LOCALAPPDATA%\AskiiGraphiks Studio\Logs`
  - macOS: `~/Library/Application Support/AskiiGraphiks Studio/Logs`
  - Linux: `~/.local/share/AskiiGraphiks Studio/Logs`

  The folder name is its own constant (`AppDataLocations.FolderName`). Once the app is released, it must not change even if the display name does.
- **Version:** the About dialog shows `0.1.0 (fc89da6)`, the version plus the short git commit the SDK stamps in. The log's first line has the full version, OS and .NET version.

## Alternatives

- **The roaming folder (`%APPDATA%`).** Windows roaming profiles copy it between machines, and logs shouldn't travel.
- **NReco.Logging.File** (one package) or **our own file provider.** Serilog is the most widely used and handles rolling, retention and crash flushing well. Writing our own would mean getting thread safety and flushing right ourselves.

## Consequences

- Serilog can be swapped out by changing `FileLogging` alone.
- ARCHITECTURE §15 expects settings in `~/Library/Application Support` on macOS, but `SpecialFolder.ApplicationData` on macOS is `~/.config`. That has to be resolved when settings are built.

Updated 2026-11-01: resolved in ADR 0026. Settings on macOS use .NET's `LocalApplicationData`, which is `~/Library/Application Support`.
