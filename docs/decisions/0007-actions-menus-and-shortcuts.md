<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0007: Actions, menus and shortcuts

Status: Accepted

## Context

ARCHITECTURE §14.2–14.3 requires every user-invocable operation to be an action with a stable id, and requires menus, toolbars and shortcuts to bind to action ids. Shortcuts must use Cmd on macOS and Ctrl elsewhere. macOS users expect menus in the system menu bar, with About and Quit in the application menu. The owner approved that layout.

## Decision

- **Actions live in the App project** (`Actions/`): `AppAction` holds an id, a label key, an optional icon key, default shortcuts, an `ICommand`, and for a toggle action (such as Show Grid) an `ActionCheckedState` that menus show as a check mark. Features contribute actions through `IActionProvider`s registered in DI, and `ActionRegistry` collects them and rejects duplicate ids. Label keys are written as `nameof(Strings.X)`, so a missing string is a compile error.
- **Shortcuts are platform-neutral:** `ActionShortcut(Key, ShortcutModifiers)`, where `Command` means "the platform's command key". It becomes a concrete `KeyGesture` using Avalonia's `HotkeyConfiguration.CommandModifiers`.
- **Menus are data:** `MenuLayout` lists menus and action ids (plus separators), and `MenuLayout.CreateDefault(isMacOS, includeDebugMenu)` builds the default layout. `NativeMenuBuilder` turns it into Avalonia `NativeMenu`s. On Windows and Linux, a `NativeMenuBar` shows them in the window; on macOS they go to the system menu bar.
- **macOS:** the application menu holds "About AskiiGraphiks Studio". Avalonia adds its standard items below it (Services, Hide, Hide Others, Show All, Quit ⌘Q), and its Quit calls the same `TryShutdown` as our Quit action. There's no File menu on macOS until File has more items.
  - `App.Initialize` sets the application menu. Avalonia's macOS exporter reads it only once, after `Initialize` and before `OnFrameworkInitializationCompleted`. If no menu is set by then, it puts its own "About Avalonia" menu there and ignores any menu set later. `AppTests` covers this.
- **Key bindings:** on Windows and Linux, `NativeMenuBar` only *displays* a gesture, so every action's shortcuts are added as window `KeyBinding`s. On macOS, a menu item's key equivalent triggers it natively, so actions shown in a menu get no window key binding (it would run them twice). Actions in no menu still get one.

## Alternatives

- **An in-window `Menu` control on every OS.** Simpler, but on a Mac it feels foreign, and Avalonia still shows its default "About Avalonia" application menu.
- **Turning off Avalonia's standard macOS items** (`DisableDefaultApplicationMenuItems`) and building them ourselves. Hide, Hide Others and Services need native calls that NativeMenu doesn't expose.
- **Defining actions in Core.** Core can't use Avalonia's `Key` or `ICommand`-based UI binding. Tool descriptors in Core (§8.1) will get their own neutral shortcut type when tools arrive.

## Consequences

- Avalonia's own macOS items ("Hide", "Quit", ...) are English-only literals. That matters only if the app is ever translated.
- On macOS, Cmd+Q is handled by Avalonia's Quit item. Our window binding for the Quit action should never fire there.
- Menu layout, labels and shortcuts can change without touching action logic, and that's the hook for user customization in M8.

Updated 2026-10-30: the application menu is now set in `App.Initialize`. It used to be set in `OnFrameworkInitializationCompleted`, which was too late, so macOS showed "About Avalonia".
Updated 2026-10-31: toggle actions have an `ActionCheckedState` (step 1.4), and there's a View menu on every OS.
