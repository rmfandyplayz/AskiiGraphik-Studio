<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0023: On macOS the app outlives its window

Status: Accepted

## Context

In 0.3 testing the owner asked for Mac behavior: closing the window keeps the app running, and the Dock icon or New or Open with no window opens one. Before 1.7 they chose one window at a time. In 1.7 testing they asked for the dot that Mac apps show in the close button while there are unsaved changes.

Avalonia 12 sets the limits:
- On macOS a window's `NativeMenu` is in the menu bar only while that window is focused. With no window, the bar falls back to the application menu alone (`showAppMenuOnly` in Avalonia.Native), so there's no File menu to choose New or Open from.
- A click on the Dock icon arrives as `IActivatableLifetime.Activated` with `ActivationKind.Reopen`.
- Avalonia has no API for `NSWindow.documentEdited`, which draws the dot.

## Decision

- **`MainWindowLifetime`** (App/Shell) opens the main window, one at a time; showing it again brings the open one forward. `desktop.MainWindow` follows it and is null while no window is open, so dialogs and the error handler see that there's no window.
- **On macOS** the app uses `ShutdownMode.OnExplicitShutdown`. When the window closes, the document is replaced by a new empty one (`NewDocumentSettings.Default`): closing the window closes the document. The next window is built from scratch by `MainWindowBuilder` and shows that new document. On Windows and Linux, closing the window still quits.
- **Dock:** Reopen opens a window when none is open. The Dock menu (`NativeDock`, from `MenuLayout.DockMenu`) has New and Open. Each item runs through `RunInWindow`, which opens a window first and runs the action once the window has opened.
- **Close** (Cmd+W, macOS only, in the File menu) closes the window through `IAppShell.CloseMainWindow`. The unsaved-changes question comes first, as for the close button.
- **Quitting:** the unsaved-changes guard cancels the window's close, which also cancels Avalonia's shutdown. So when the close was part of quitting (`ApplicationShutdown` or `OSShutdown`), Save or Don't Save asks for shutdown again rather than closing only the window, which on macOS would leave the app running.
- **The unsaved dot:** `MacOSDocumentEdited` calls `-[NSWindow setDocumentEdited:]` through the Objective-C runtime. It uses the native window from `TryGetPlatformHandle()` (descriptor `NSWindow`) and follows `WindowTitle.HasUnsavedChanges`. It does nothing elsewhere, and if the call fails only the dot is lost.
- Each window subscribes to the shared `WindowTitle` and unsubscribes when it closes.

## Alternatives

- **Hiding the window instead of closing it:** the menu bar still falls back to the application menu, and a hidden window confuses the Window menu and Mission Control.
- **New and Open in the application menu:** they'd work with no window, but Mac users don't look for them there. A Dock click and the Dock menu are the usual pair.
- **Keeping the File menu with no window** by changing Avalonia's menu holder: depends on its internals (the order of items inside it).
- **`IMacOSTopLevelPlatformHandle.NSWindow`:** marked unstable in Avalonia 12.1. The plain platform handle is the same pointer.

## Consequences

- With no window open on a Mac, Cmd+N and Cmd+O do nothing, because nothing in the menu bar has those shortcuts. The Dock icon and Dock menu open a window instead. Worth revisiting if Avalonia gains app-wide menus.
- Every new window builds new menu items, and the check-mark items (ASCII Only, Show Grid) keep a subscription each. That's a small leak per window opened: fine with one window at a time, worth fixing if windows ever multiply.
- The title keeps its `*` on macOS too, until the owner designs the title.
- Headless tests can't cover the shutdown mode, the Dock or the dot; the manual test does.

Updated 2026-11-01: each new window also gets the saved panel layout, and saves it when it closes (ADR 0025). The Window menu's check-mark items keep a subscription each, like the others.
