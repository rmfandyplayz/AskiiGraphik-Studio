<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Temporary UI and ideas to revisit

Everything the user sees is the owner's to design (CLAUDE.md). Until the owner's designs arrive, Claude Code builds plain placeholder UI and makes the small UX calls itself (ARCHITECTURE §14.6). This file lists every one of those calls, so the owner can review and replace them, plus ideas the owner parked for later.

**Rule:** a step that adds or changes placeholder UI, or a UX behaviour the owner hasn't specified, adds a row here in the same step. Strings live in `src/AskiiArt.App/Resources/Strings.resx`, and colours and sizes in `src/AskiiArt.App/Theme/Tokens.axaml`.

## Temporary choices

### Window and menus

| Choice | Where | Step |
|---|---|---|
| Menus: File (Quit), Edit (Undo, Redo, ASCII Only), View (Zoom In, Zoom Out, Actual Size, Show Grid), Help (Open Log Folder, About). On macOS: About in the app menu, no File menu | `Shell/Menus/MenuLayout.cs` | 0.3, 1.4, 1.5 |
| A Debug menu (Throw Test Exception) in Debug builds only | `MenuLayout.cs`, `ShellActions.cs` | 0.3 |
| Shortcuts (Cmd on macOS): Quit Ctrl+Q; Undo Ctrl+Z; Redo Ctrl+Shift+Z or Ctrl+Y; Zoom In Ctrl+= (also Ctrl+Shift+= and keypad +); Zoom Out Ctrl+- (keypad -); Actual Size Ctrl+0; Show Grid Ctrl+' | each feature's `*Actions.cs` | 0.3–1.5 |
| The menu shows only an action's first shortcut, so Redo shows Ctrl+Shift+Z on Windows too | `NativeMenuBuilder.cs` | 1.5 |
| About window and unexpected-error dialog (Open Log Folder, Quit, Continue) | `Shell/Dialogs/` | 0.3 |
| A plain yes-or-cancel dialog, with Cancel as the default button | `Shell/Dialogs/ConfirmWindow.axaml` | 1.5 |
| Status bar along the bottom: zoom, canvas size, character set, cell under the pointer (counted from 0) | `Shell/MainWindow.axaml` | 1.4, 1.5 |

### Canvas

| Choice | Where | Step |
|---|---|---|
| 100% zoom means a 16-pixel font; zoom steps from 25% to 800% | `CanvasViewModel.DefaultFontSize`, `ZoomLevels.cs` | 1.4 |
| Colours: grey pasteboard, white or dark paper, black or light ink, faint grid lines, translucent blue hover, following light and dark mode | `AppCanvas*Brush` tokens | 1.4 |
| 32-pixel margin around the canvas | `AppCanvasMargin` token | 1.4 |
| Grid off by default, and not drawn when cells are under 4 device pixels wide (so it depends on display scaling) | `CanvasViewModel`, `CanvasScenePainter.MinimumCellWidthForGrid` | 1.4 |
| Missing glyphs: an orange cell for "drawn with another font", a red cell and a hollow box for "no font has it" | `AppCanvasFallbackGlyphBrush`, `AppCanvasPlaceholderGlyphBrush`, `GlyphRasterizer` | 1.3, 1.4 |
| A wide glyph from the document's font is squeezed sideways to fit its cell; box-drawing and block characters are clipped instead | `GlyphRasterizer` | 1.4 |
| Scrollbars always visible; the wheel scrolls 50 pixels a notch | `CanvasView.axaml`, `CanvasControl` | 1.4 |
| Hand pointer while panning with Space or the middle button | `CanvasControl` | 1.4 |
| A wheel gesture keeps the keys it started with: pressing Cmd during a touchpad glide doesn't zoom (ADR 0018) | `WheelGestureTracker` | 1.5 |
| The sample document shown at startup, until New and Open exist | `Canvas/SampleDocument.cs` | 1.4 |

### Typing (the text tool, ADR 0017)

| Choice | Where | Step |
|---|---|---|
| The text tool is always active (it's the only tool), so Space types a space and Space + drag doesn't pan | `CanvasViewModel` | 1.5 |
| The caret is a blue outline around its cell, 2 pixels thick, not blinking | `AppCanvasCaretBrush`, `AppCanvasCaretThickness` | 1.5 |
| Typing overwrites; nothing shifts sideways. At the right edge the caret stays in the last column | `TextTool` | 1.5 |
| Backspace after typing empties the cell before the caret and moves into it. After a click, an arrow key, Home or Enter, at the left edge, or after typing into the last column, it empties the caret's own cell first. Delete empties the caret's cell. Both leave empty cells, not spaces | `TextTool` | 1.5, 1.6 |
| Enter goes to the next row, under the start of the text you're typing. A click starts new text; moving with the keys and typing on the same rows (from its start to just past its end) continues it; typing anywhere else starts new text there | `TextTool.TextBlock` | 1.5, 1.6 |
| Home goes to the first column; End to just past the row's last character; Esc hides the caret | `TextTool` | 1.5 |
| One undo step per burst of typing; a 2-second pause, or moving the caret, starts a new one | `TextTool.TypingPause` | 1.5 |
| The canvas scrolls to keep the caret on screen | `CanvasControl` | 1.5 |
| Characters the document's character set doesn't allow are silently skipped | `TextTool` | 1.5 |
| ASCII Only is a check-mark item in the Edit menu, and switching to it asks first when it would remove characters | `EditActions`, `AsciiOnlyConfirm*` strings | 1.5 |

## Ideas to revisit

The owner's ideas, parked until the stage they belong to.

- **Debug menu in release builds** (raised in 1.5): hidden by default in release builds, but a setting, probably on an "Advanced" tab of the settings window (M8), turns it on for curious users and contributors hunting bugs. Today the Debug menu exists only in Debug builds.
- **Draw mode and text mode** (raised in 1.5): two primary ways of working. Draw mode works like an image editor, painting with brushes; text mode is a tool for typing text. Space + drag pans in draw mode only.
- **Character set UI** (raised in 1.5): the New dialog (1.6) lets you choose Unicode or ASCII only, and document options let you switch later, with a warning that switching to ASCII removes every other character.
- **Blinking caret** (raised in 1.5): blink at the system's caret rate where the OS has one (on Windows, the Control Panel keyboard setting), else every 0.5 s. The rate becomes a setting later (M8).
- **Moving the caret faster** (raised in 1.5): jumps of a few cells, of many cells, and to the edge of the row or column, with the keys and jump sizes rebindable (M8). The default keys are being agreed for 1.6.
- **Typing pause as a setting** (raised in 1.5): the 2-second pause that ends an undo step could be an advanced setting (M8).
- **How missing glyphs are flagged** (1.3): the orange and red cells are placeholders.
- **Color and plugins**: possible colour for Discord ANSI art; opacity as a plugin only; wide (CJK) characters as a plugin.
