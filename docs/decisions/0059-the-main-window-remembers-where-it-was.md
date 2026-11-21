<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0059: The main window remembers where it was

Status: Accepted

## Context

After testing step 4.1 the owner (2026-11-20) asked for the main window to come back at its last size, and on the monitor it was on, on every platform. It always opened at the system's default size on the main screen.

## Decision

- `RememberedWindowPlacement` saves a `WindowPlacement` under the `mainWindow` setting when the window closes, as the layout is saved: its top-left corner in device pixels and its size in logical pixels while it was last neither maximized nor minimized, and whether it was maximized or full screen.
- It notes the window's place just after it moves, resizes or changes state, not during, because a window being maximized can report its new position before it reports being maximized. Minimized or full screen, it keeps the position and size it noted before.
- On opening, if the saved title bar is on one of today's screens (its top middle, 16 logical pixels down, in that screen's room for windows), the window goes back there, made no bigger than that screen. A maximized window is maximized on that screen, and a window closed full screen opens full screen there. Otherwise, as when a monitor has been unplugged, it opens in the middle of the main screen, at the saved size made to fit.
- The rule is a pure function of the saved placement and the screens, so it's tested without a display.

## Alternatives

- **Each platform's own API** (Windows' `GetWindowPlacement`, macOS's frame autosave): exact, but three versions of the same thing, and Avalonia doesn't expose them.
- **Saving the whole frame in device pixels:** a window moved between screens with different scaling would come back the wrong size.
- **Keeping the window wholly on screen:** moving a window the user left partly off screen would surprise; it only has to be reachable by its title bar.

## Consequences

- Linux under Wayland doesn't let apps place their windows, so there the size comes back but not the position. That isn't tested by hand.
- On macOS a window opened from the Dock after the last one closed also comes back where that one was.

Updated 2026-11-20: at the owner's request a window closed full screen opens full screen again. Placements saved before then read as not full screen.

Updated 2026-11-21 (step 4.5): a maximized or full-screen window moved to another screen now notes that screen, so it reopens there. Its remembered normal size and place move onto that screen, keeping their distance from its corner where they fit, else centred, and shrinking to fit a smaller screen (`WindowPlacement.MovedOnto`). Before, they stayed on the screen it was maximized on.
