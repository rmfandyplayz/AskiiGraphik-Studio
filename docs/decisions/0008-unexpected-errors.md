<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0008: Handling unexpected errors

Status: Accepted

## Context

ARCHITECTURE §20 says unhandled exceptions are logged with context, shown to the user as a clear message, and never swallowed silently. .NET and Avalonia raise uncaught exceptions in three different places, and the app can survive only one of them. The owner approved the behavior below.

## Decision

`Errors/UnexpectedErrorHandler` handles all three:

| Where | Hooked through | What happens |
|---|---|---|
| UI thread | `Dispatcher.UIThread.UnhandledException` | Logged as Error, with the stack trace. A dialog says the details were logged and offers **Open Log Folder** (keeps the dialog open), **Quit** and **Continue** (the default, also used when the dialog is closed). The exception is marked handled, so the app keeps running. |
| Background thread | `AppDomain.UnhandledException` | Logged as Critical. The process ends regardless, so there's no dialog. |
| A failed task nobody awaited | `TaskScheduler.UnobservedTaskException` | Logged as Error and marked observed. No dialog. |

- **Before the main window exists,** a UI-thread exception is logged but *not* marked handled, so the app crashes rather than run on half-started with no window.
- **While the error dialog is open,** further exceptions are logged only, which prevents a flood of dialogs when an error repeats (for example every frame).
- **If the dialog itself fails,** that failure is logged too and nothing is rethrown.
- **Testing by hand:** Debug builds have a Debug → Throw Test Exception menu item. It throws from a dispatcher job, the same path a typical UI-thread exception takes.

## Alternatives

- **Quit after any UI-thread exception.** Safer in theory, but once documents exist it would lose the user's unsaved work. Continuing is usually fine, and the user can still choose Quit.
- **Show the exception message in the dialog.** It's developer text and often meaningless to users. It's in the log.

## Consequences

- The recovery-copy offer (§20) is added once there's a document to save (M1).
- `IAppShell` keeps the handler testable without real windows. The tests cover every row above.
