<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Temporary UI and ideas to revisit

Everything the user sees is the owner's to design (CLAUDE.md). Until the owner's designs arrive, Claude Code builds plain placeholder UI and makes the small UX calls itself (ARCHITECTURE §14.6). This file lists every one of those calls, so the owner can review and replace them, plus ideas the owner parked for later.

**Rule:** a step that adds or changes placeholder UI, or a UX behaviour the owner hasn't specified, adds a row here in the same step. Strings live in `src/AskiiArt.App/Resources/Strings.resx`, and colours and sizes in `src/AskiiArt.App/Theme/Tokens.axaml`.

## Temporary choices

### Window and menus

| Choice | Where | Step |
|---|---|---|
| Menus: File (New, Open, Save, Save As, Export as Text, Quit), Edit (Undo, Redo, Copy as Text), Document (Resize Canvas, Font, ASCII Only), Layer (see the Layers panel), View (Zoom In, Zoom Out, Actual Size, Show Grid), Help (Open Log Folder, About). On macOS: About and Quit in the app menu, so File has no Quit, and File has Close between Open and Save | `Shell/Menus/MenuLayout.cs` | 0.3, 1.4, 1.5, 1.6, 1.7, 1.8 |
| Four toolbars of plain text buttons: File (New, Open, Save), Tools (Text, Eraser, toggles), Edit (Undo, Redo; Resize Canvas, Font) and View (Zoom Out, Zoom In, Actual Size, Show Grid, a toggle). The owner asked for the first two, and Edit and View hold the rest until the toolbar customizer (M8). The buttons never take keyboard focus | `Shell/Toolbars/ToolbarLayout.cs`, `ShellToolbars.cs` | 1.6, 1.7, 2.1, 2.4, 2.5b |
| A Tools menu between Layer and View: Text and Eraser, with a check mark on the active one | `MenuLayout.cs`, `Canvas/ToolActions.cs` | 2.4 |
| A Window menu between View and Help: Layers, History, then the four toolbars (all check-marked), then Reset Layout. It shows on a Mac too | `MenuLayout.cs`, `Shell/Docking/WindowMenuActions.cs` | 2.1, 2.5b |
| Default layout (`docs/DOCKING.md` §9): the menu bar as a strip along the top (Windows and Linux) and the status bar along the bottom. Layers above History as widgets on the right, 240 wide. File toolbar top left and Tools in the bottom middle, as the owner asked. Edit top right and View bottom right, as placeholders | `Shell/Docking/ShellDefaultLayout.cs`, `Theme/Tokens.axaml` | 2.1, 2.5b |
| A toolbar that doesn't fit scrolls along its length with no scrollbar, 50 pixels per wheel notch on a horizontal one. Its buttons fade over the last 20 pixels on a side with more to scroll to | `Shell/Docking/ToolbarFrame.axaml`, `ToolbarFades.cs`, `AppToolbarWheelStep`, `AppToolbarFadeLength` | 2.1, 2.5c |
| The window can't go below 1280 × 720, or below what its screen has room for when that's less | `Shell/WindowMinimumSize.cs`, `AppWindowMinWidth`, `AppWindowMinHeight` | 2.1 |
| The menu bar and status bar can't be moved or closed, and nothing can be docked above the menu bar or below the status bar | `Shell/MainWindow.axaml` | 2.1 |
| Widgets and toolbars look like plain square boxes, 85% opaque, with margins and gaps of 8. A window has a plain title 28 tall. Frosted glass waits for M8 | `Shell/Docking/WindowFrame.axaml`, `Shell/Docking/ToolbarFrame.axaml`, `AppWidgetBrush` and the tokens beside it | 2.1, 2.5b |
| Starting numbers for the layout rules: a window goes title-only below 140, sides shrink rather than leave the canvas region under 320, toolbars sharing an edge have a gap of 8, a toolbar shorter than 80 hides until there's room, and a middle toolbar gets 25% more of a shared edge | `Theme/Tokens.axaml`, `Shell/Docking/ShellLayoutMetrics.cs` | 2.5b |
| A window's title row: ≡ and ✕ drawn as thin lines in 20-pixel buttons, faint at 50%, with a light square behind the one under the pointer. Tooltips say Window Options and Close. Turn into Dock stays greyed out until docks arrive in 2.5f, and until then dragging a window to the edge of a side with widgets does nothing special | `Shell/Docking/WindowFrame.axaml`, `Shell/Docking/WindowMenu.cs`, `AppWindowButton*`, `AppWindowIcon*` | 2.5e |
| While a window is dragged: an empty side shows its edge strip in blue, 24 wide, which is my addition so that it can be found. Over it, that side opens to a placeholder 48 wide, taken from the proportions of the owner's picture. Elsewhere the hatched placeholder is the size the window would land at, where the owner's sketches sometimes draw it smaller. The window held over a zone is 35% opaque, as a toolbar is. Toolbars stay showing during a window drag, because the owner's picture has them move out of the way, unlike a toolbar drag | `Shell/Docking/WindowPlaceholderView.cs`, `AskiiArt.Docking/Dragging/WindowDrag.cs`, `AppWindowEdge*`, `AppWindowPlaceholder*` | 2.5e |
| Where a window lands: a window dropped on an empty side gets the default 240, not its own width. A window floated by dragging keeps its size. Float in the menu moves it 24 in and down, so that it visibly comes off. A title-only widget that floats opens to 280 tall. A floating title-only window that's only moved stays title-only, though the owner's rule says a title-only window that lands gets a normal size, because collapsing a window and then having it open whenever it's moved seemed wrong | `AskiiArt.Docking/Dragging/WindowDrag.cs`, `Shell/Docking/ShellDocking.cs`, `AppFloatingWindowHeight`, `AppWindowFloatOffset` | 2.5e |
| Resizing windows: the narrowest is 160. A window at its minimum height sticks there until the drag has gone halfway to its title, then snaps, and the same in reverse, so the gap never strays far from the pointer. The gaps' and inner edge's handles are invisible and fill the 8-pixel spaces. A floating window's grips are invisible too, 5 thick along its edges and 10 square at its corners | `AskiiArt.Docking/Dragging/WindowResizes.cs`, `Shell/Docking/StackResizeHandleView.cs`, `AppWindowMinWidth`, `AppWindowResizeGripSize`, `AppWindowResizeCornerSize` | 2.5e |
| Debug → Sample Window 1 to 3 open empty windows, because stacks can't be tried with only Layers and History. A restart forgets them. They go when there are enough real windows | `Shell/Docking/WindowSampleActions.cs` | 2.5e |
| Toolbar grips: 14 long, with dots of 2 and gaps of 2 in the secondary text colour at 60%, and a move cursor. A press has to move 4 pixels before it counts as a drag | `Shell/Docking/ToolbarGrip.cs`, `AppToolbarGrip*`, `AppDockingDragThreshold` | 2.5c |
| While a toolbar is dragged, the drop zones are see-through blue. The owner set their rules (`docs/DOCKING.md` §5.1). The 30% that a strip always gets, which only matters in a very small region, is taken from the owner's picture and scales with the edge. A strip's thickness comes from the dragged toolbar, so strips can differ between drags until toolbars are all one shape. The owner asked for a corner square that's a strip wrapped around a strip, and never thinner than the strips. Adding the two thicknesses is my reading of that | `Shell/Docking/DropZoneView.cs`, `AskiiArt.Docking/Dragging/ToolbarDrag.cs`, `AppToolbarZoneMinimumShare`, `AppDropZoneBrush` | 2.5c |
| The owner asked for all existing toolbars to disappear during a drag. I took that to include floating toolbars and other toolbars' sub-toolbars, though neither holds a strip or a square. A zone overlapped by a pushed-back copy dims to 40%. After a drag the zones, the outline and the pushed-back copy fade out, and the other toolbars fade in, over the 120 ms a toolbar takes to slide. A cancel does the same | `Shell/Docking/DockingHost.cs`, `AppDropZoneDimmedOpacity`, `AppDockingMotionMilliseconds` | 2.5d |
| The preview's look: a pushed-back copy of the toolbar is 60% opaque. The outline is 1.5 thick, in the accent blue, with dashes of about 6 and gaps of about 4.5, stretched slightly so a whole number fit around it. Its dashes run clockwise at 24 a second. Over a zone, the zone's blue fades away altogether, and the dragged toolbar and its sub-toolbar are 35% opaque, fading over 120 ms | `Shell/Docking/ToolbarLandingPreview.cs`, `Shell/Docking/MarchingAnts.cs`, `AppLanding*`, `AppToolbarAnchoringOpacity` | 2.5c |
| Sub-toolbars: the same look as a toolbar, with no grips and no right-click menu. One appears at once, with no animation of its own, while whatever it pushes slides. A floating toolbar's doesn't push anchored toolbars. During a drag it stays centred on the held toolbar, even if it would land aligned to a corner. Over a zone there's no outline for where it would land, only the zone's | `AskiiArt.Docking/Solving/ToolbarSolver.SubToolbar.cs`, `Shell/Docking/DockingHost.cs` | 2.5d |
| Debug → Sub-Toolbar on File, Tools, Edit and View put a sample on that toolbar, with 3 buttons that do nothing. Long Sub-Toolbar makes it 16. They go when tool groups arrive in M3 | `Shell/Docking/SubToolbarSampleActions.cs` | 2.5d |
| A dragged toolbar turns as soon as it's over an edge middle's zone, to show what a drop would do, and turns back when it leaves. The pointer keeps its place along the toolbar | `Shell/Docking/DockingHost.cs` | 2.5c |
| Anything the user does slides toolbars to their new places over 120 ms, slowing at the end. Resizing the app window snaps them | `Shell/Docking/LayoutMotions.cs`, `AppDockingMotionMilliseconds` | 2.5c |
| A toolbar's right-click menu works anywhere on it, buttons included: Horizontal and Vertical with a dot on the current one (greyed out on an edge middle), a line, then Hide | `Shell/Docking/ToolbarMenu.cs` | 2.5c |
| Floating toolbars draw over anchored ones, and the one dropped last draws over other floating ones. Dropping one on another just overlaps | `Shell/Docking/DockingHost.cs`, `LayoutEdits.FloatToolbar` | 2.5c |
| A hidden toolbar shown again when its anchor has been taken floats exactly where it would sit with that anchor to itself | `LayoutSolver.WhereAloneOnItsAnchor` | 2.5c |
| Debug → Allow Tiny Window lifts the minimum size until the window closes, to test the fights along an edge | `MainWindow.AllowAnySize` | 2.5c |
| A window with no remembered place joins the bottom of the right side, then the left, and stays closed with no room on either. The opening rules replace this in 2.5g, and floating does in 2.5e | `AskiiArt.Docking/Layouts/LayoutEdits.cs` | 2.5b |
| Clicking a window or toolbar, or closing a menu, hands keyboard focus back to the canvas, unless a text box has it | `MainWindow.GiveTypingBackToCanvas` | 2.1 |
| A damaged settings file: a message says so when the window opens, and the old file is kept with "damaged" in its name. An unreadable saved layout alone resets silently, and one that's readable but wrong is put right | `Settings/AppSettingsFile.cs`, `Shell/Docking/ShellDocking.cs`, `AskiiArt.Docking/Layouts/LayoutRepair.cs` | 2.1, 2.5b |
| Layers panel: a plain list, top layer first, with the active one highlighted. Each row has a visible checkbox, a locked checkbox, a "spaces are see-through" checkbox and the name, which turns grey when hidden. Below it, text buttons that wrap: New, Duplicate, Delete, Rename, Move Up, Move Down, Merge Down | `Layers/LayersPanelView.axaml`, `ToolbarLayout.CreateLayersPanel` | 2.3 |
| A Layer menu between Document and View, with the same items as the buttons. No shortcuts yet | `MenuLayout.cs`, `Layers/LayerActions.cs` | 2.3 |
| A new layer goes above the active one and is called "Layer N", with the first number not in use, counting from how many layers there'll be. A copy goes just above its original, called "… copy", with the same visible and locked settings. Each becomes active | `LayersPanelViewModel` | 2.3 |
| Rename: double-click a row, or Rename. Enter or clicking away keeps the new name, and Esc cancels. An empty name changes nothing. With the panel closed, Rename does nothing | `LayersPanelView`, `LayersPanelViewModel.FinishRename` | 2.3 |
| Deleting needs another layer to remain and the layer unlocked, and the one below it takes over (or above, for the bottom one). No confirmation, since it can be undone | `LayersPanelViewModel` | 2.3 |
| Merge Down needs both layers visible and unlocked. The result keeps the lower layer's name and settings, and the picture doesn't change | `LayersPanelViewModel`, `MergeLayerDown` | 2.3 |
| Typing does nothing on a hidden layer, as on a locked one, and gives no message | `EditingSession.CanEditActiveLayer` | 2.3 |
| Clicking a row or a checkbox doesn't take the keyboard from the canvas. Hiding or locking a row that isn't active leaves the active layer alone | `LayersPanelView`, `MainWindow` | 2.3 |
| The active layer isn't undone, and choosing one doesn't mark the document unsaved, so changing only that and closing asks nothing and loses the choice. The file remembers it from the last save (the owner asked for that part). When the active layer goes away because of an undo, the top one takes over | `EditingSession`, `DocumentFiles` | 2.3 |
| History panel: a plain list, oldest step first and newest at the bottom, below a "Start" row. The current row is highlighted and undone rows are dimmed. A single click jumps, and focus then returns to the canvas | `Editing/HistoryPanelView.axaml` | 2.2 |
| Step names: Typing, Edit Cells, Resize Canvas, Change Font, Change Character Set, Add Layer, Duplicate Layer, Delete Layer, Move Layer, Merge Down, Rename Layer, Show or Hide Layer, Lock or Unlock Layer, Change Space Option, Erase, Change Layer. Backspace and Delete count as Typing, because they merge into the same burst | `Strings.resx`, `TextTool` | 2.2 |
| "Start" stays at the top even after the oldest steps are dropped to save memory, when it means the oldest point still reachable | `HistoryPanelViewModel` | 2.2 |
| A Debug menu (Open Sample Document, Throw Test Exception) in Debug builds only | `MenuLayout.cs`, `ShellActions.cs`, `DocumentActions.cs` | 0.3, 1.7 |
| Shortcuts (Cmd on macOS): New Ctrl+N; Open Ctrl+O; Save Ctrl+S; Save As Ctrl+Shift+S; Quit Ctrl+Q; Undo Ctrl+Z; Redo Ctrl+Shift+Z or Ctrl+Y; Zoom In Ctrl+= (also Ctrl+Shift+= and keypad +); Zoom Out Ctrl+- (keypad -); Actual Size Ctrl+0; Show Grid Ctrl+'; Copy as Text Ctrl+Shift+C; Close Cmd+W (macOS only). Export as Text has none | each feature's `*Actions.cs` | 0.3–1.8 |
| The menu shows only an action's first shortcut, so Redo shows Ctrl+Shift+Z on Windows too | `NativeMenuBuilder.cs` | 1.5 |
| About window and unexpected-error dialog (Open Log Folder, Quit, Continue) | `Shell/Dialogs/` | 0.3 |
| A plain yes-or-cancel dialog, with Cancel as the default button | `Shell/Dialogs/ConfirmWindow.axaml` | 1.5 |
| The menu bar on Windows and Linux: Avalonia's built-in menu in a plain strip along the top, not a widget. Its look isn't customized yet | `Shell/MainWindow.axaml` | 2.3 |
| Status bar, a plain strip along the bottom, not a widget: zoom, canvas size, character set, font, cell under the pointer (counted from 0) | `Shell/StatusBarView.axaml` | 1.4, 1.5, 1.6, 2.1 |
| A plain message dialog with OK, for files that can't be opened or saved and for a missing font | `Shell/Dialogs/MessageWindow.axaml` | 1.7 |

### Documents and fonts

| Choice | Where | Step |
|---|---|---|
| New asks for the width, height (80 × 25 to begin with) and characters (ASCII only to begin with, or Unicode) | `Documents/NewDocumentWindow.axaml`, `NewDocumentViewModel` | 1.6 |
| The size boxes take whole numbers from 1 to 4096, and a number outside that is clamped to the nearest limit (shown once you leave the box); above 500 a warning appears under them, and OK still works | `Documents/CanvasSizeFieldsView.axaml` | 1.6, 1.7 |
| Resize Canvas starts at the current size, anchored in the centre. The anchor is a 3 × 3 grid of arrows, and shrinking shows "cells outside the new size will be removed" | `Documents/ResizeCanvasWindow.axaml` | 1.6 |
| The font picker: a search box, one list (the bundled fonts, marked "comes with the app", then fonts loaded from files, then installed fonts), Load from File, and a warning when the selected font isn't monospaced. No preview | `Fonts/FontPickerWindow.axaml` | 1.6 |
| A font file whose family is already available isn't added; the picker says the one already there is used (ADR 0019) | `FontCatalog.AddFile` | 1.6 |
| Fonts loaded from files are remembered by path. If the file is moved, deleted or damaged, its font quietly leaves the picker (with a line in the log) and comes back if the file does. There's no way to remove one from the picker | `FontCatalog`, `RememberedFontFiles` | 1.6, 2.4b |
| Warnings in dialogs are orange text | `AppWarningTextBrush` | 1.6 |

### Files (ADR 0021)

| Choice | Where | Step |
|---|---|---|
| The app starts with an empty, untitled 80 × 25 ASCII-only document | `Hosting/AppServices.cs`, `NewDocumentSettings.Default` | 1.7 |
| The window title is "art.asciiart - AskiiGraphiks Studio", or "Untitled - …" before the first save, with a `*` in front while there are unsaved changes | `Shell/WindowTitle.cs`, `WindowTitle*Format` strings | 1.7 |
| With unsaved changes, New, Open, Open Sample Document and closing the window (also Quit) first ask "Do you want to save the changes to “name”?": Save (the default, Enter), Don't Save, Cancel (Esc). Same button order on every OS. New and Open ask before their own dialogs | `Documents/SaveChangesWindow.axaml`, `DocumentFiles` | 1.7 |
| Save As offers two file types, "AskiiGraphiks document" (.asciiart) and "AskiiGraphiks document, compressed" (.asciiartz), with the document's own first. It suggests the current name (or "Untitled") and starts in the file's folder. A typed .asciiart or .asciiartz decides the variant; a name with no extension gets the chosen type's. On a Mac the dialog itself adds the chosen type's extension to any name, so there the type has to be chosen in the dialog | `AvaloniaDocumentDialogs`, `DocumentFile.ForSavePath` | 1.7, 1.8 |
| Save keeps the variant the file was opened in, even if its name says otherwise | `DocumentFiles` | 1.7 |
| Open shows one file type for both extensions, plus All files | `AvaloniaDocumentDialogs` | 1.7 |
| A file that can't be opened says why in one sentence: not a document, saved by a newer version (with its format number), damaged, or couldn't be read (with the system's reason). A failed save says the system's reason, and the changes stay unsaved | `OpenFailed*`, `SaveFailed*` strings | 1.7 |
| A document whose font isn't available opens in JetBrains Mono, says so once, and the status bar shows "Font Name (not available)" | `DocumentFiles`, `CanvasViewModel.FontText` | 1.7 |

### Output and macOS windows (ADRs 0022, 0023)

| Choice | Where | Step |
|---|---|---|
| Copy as Text copies the visible layers as plain text: every row the canvas's full width, empty cells as spaces, this OS's line breaks between rows and none after the last | `Editing/EditActions.cs`, `TextExporter` | 1.8 |
| Export as Text writes UTF-8 text with `\n` after every row, the last included. It suggests the document's name, starts in its folder, and adds `.txt` to a name without an extension. The document keeps its file, title and unsaved changes. No options yet (line breaks, trimming) | `DocumentFiles.ExportAsTextAsync` | 1.8 |
| On a Mac, closing the window (its close button, or File → Close) closes the document, after asking about unsaved changes, and the app keeps running. A click on the Dock icon then opens a window with a new empty 80 × 25 ASCII-only document | `Shell/MainWindowLifetime.cs` | 1.8 |
| On a Mac, the Dock icon's menu has New and Open. With no window open they open one first; cancelling their dialog leaves that window open with an empty document. With no window, Cmd+N and Cmd+O do nothing | `MenuLayout.DockMenu`, `MainWindowLifetime.RunInWindow` | 1.8 |
| On a Mac, the close button shows a dot while there are unsaved changes, and the title keeps its `*` as well | `Shell/MacOSDocumentEdited.cs` | 1.8 |

### Canvas

| Choice | Where | Step |
|---|---|---|
| 100% zoom means a 16-pixel font; zoom steps from 25% to 800% | `CanvasViewModel.DefaultFontSize`, `ZoomLevels.cs` | 1.4 |
| Colours: grey pasteboard, white or dark paper, black or light ink, faint grid lines, translucent blue hover, following light and dark mode | `AppCanvas*Brush` tokens | 1.4 |
| 32-pixel margin around the canvas | `AppCanvasMargin` token | 1.4 |
| Grid off by default, and not drawn when cells are under 4 device pixels wide (so it depends on display scaling) | `CanvasViewModel`, `CanvasScenePainter.MinimumCellWidthForGrid` | 1.4 |
| Missing glyphs: an orange cell for "drawn with another font", a red cell and a hollow box for "no font has it" | `AppCanvasFallbackGlyphBrush`, `AppCanvasPlaceholderGlyphBrush`, `GlyphRasterizer` | 1.3, 1.4 |
| A wide glyph from the document's font is squeezed sideways to fit its cell; box-drawing and block characters are clipped instead | `GlyphRasterizer` | 1.4 |
| No canvas scrollbars for now (the owner may revise this; see the minimap idea below). Setting `ShowScrollBars` to true on the `CanvasView` in `MainWindow.axaml` brings them back, just inside the docked widgets. The wheel scrolls 50 pixels a notch | `CanvasView.axaml`, `CanvasControl` | 1.4, 2.1 |
| Hand pointer while panning with Space or the middle button | `CanvasControl` | 1.4 |
| A wheel gesture keeps the keys it started with: pressing Cmd during a touchpad glide doesn't zoom (ADR 0018) | `WheelGestureTracker` | 1.5 |
| The busy 500 × 500 sample document, opened from the Debug menu (it was the startup document until 1.7) | `Canvas/SampleDocument.cs` | 1.4, 1.7 |

### Typing (the text tool, ADR 0017)

| Choice | Where | Step |
|---|---|---|
| The Text tool is active at first. With it, Space types a space and Space + drag doesn't pan; with the Eraser, Space + drag pans | `CanvasViewModel`, each tool's `AllowsSpacePanning` | 1.5, 2.4 |
| T picks Text and E picks Eraser, only while the canvas has the keyboard and no caret is placed. Esc removes the caret, so it's the way from typing to these keys. They aren't shown in the menus | `CanvasView`, `ToolDescriptor.ShortcutLetter` | 2.4 |
| The chosen tool stays chosen when New or Open replaces the document, and switching tools removes the caret | `CanvasViewModel.SelectTool` | 2.4 |
| The Eraser: one cell wide, erases as you drag, one stroke is one History step named Erase. Esc during a stroke takes it back and leaves no step. It leaves locked and hidden layers alone, with no message. No special pointer | `Core/Tools/Eraser/EraserTool.cs` | 2.4 |
| The space option is off for new layers. It can be changed on a locked or hidden layer, like visibility. Merge Down unticks it on the merged layer when that's what keeps the picture the same | `LayerProperties`, `MergeLayerDown` | 2.4 |
| The caret is a blue outline around its cell, 2 pixels thick, not blinking | `AppCanvasCaretBrush`, `AppCanvasCaretThickness` | 1.5 |
| Typing overwrites; nothing shifts sideways. At the right edge the caret stays in the last column | `TextTool` | 1.5 |
| Backspace after typing empties the cell before the caret and moves into it. After a click, an arrow key, Home or Enter, at the left edge, or after typing into the last column, it empties the caret's own cell first. Delete empties the caret's cell. Both leave empty cells, not spaces | `TextTool` | 1.5, 1.6 |
| Enter goes to the next row, under the start of the text you're typing. A click starts new text; moving with the keys and typing on the same rows (from its start to just past its end) continues it; typing anywhere else starts new text there | `TextTool.TextBlock` | 1.5, 1.6 |
| Home goes to the first column; End to just past the row's last character; Esc hides the caret | `TextTool` | 1.5 |
| Jumps: 3 cells with Ctrl+arrow (Option+arrow on a Mac), 10 cells with Ctrl+Shift+arrow (Cmd+Option+arrow). Ctrl+Home and Ctrl+End (Cmd+Up and Cmd+Down) go to the top and bottom of the column; on a Mac, Cmd+Left and Cmd+Right are Home and End. Shift+arrow and Alt+arrow do nothing (Shift is kept for selection, and a lone Alt opens the menu bar on Windows and Linux) | `Canvas/ToolKeymap.cs`, `TextTool.ShortJump`, `LongJump` | 1.6, 1.7 |
| The bottom of the column is the canvas's last row, even when the column is empty (End, in contrast, goes just past the row's text) | `TextTool` | 1.6 |
| One undo step per burst of typing; a 2-second pause, or moving the caret, starts a new one | `TextTool.TypingPause` | 1.5 |
| The canvas scrolls to keep the caret on screen | `CanvasControl` | 1.5 |
| Characters the document's character set doesn't allow are silently skipped | `TextTool` | 1.5 |
| ASCII Only is a check-mark item in the Document menu (the Edit menu in 1.5), and switching to it asks first when it would remove characters | `DocumentActions`, `AsciiOnlyConfirm*` strings | 1.5, 1.6 |

## Ideas to revisit

The owner's ideas, parked until the stage they belong to.

- **Canvas-first layout, like Sketchbook and Excalidraw** (raised in M2 planning): the canvas stays the main character. Controls are docks that look like small floating widgets, anchored to part of the screen with an offset, like a Unity UI element anchored to the right with a position offset. The canvas paints the whole window, so art shows around and between the widgets and slides under them when panned. Widgets take up space all the same: centering, zoom-to-fit and scroll limits count them, and they always get clicks and other input before the canvas. On Windows and Linux the menu bar (File, Edit, View, Window and so on) is a small built-in strip along the top, whose fonts and colours can be customized later. On a Mac it stays in the system menu bar. The status bar is a small strip along the bottom, which a settings entry might turn off or edit. Those two are the only fixed things. The exact behavior gets designed with the owner in a later milestone.
- **Toolbar customizer** (raised after 2.2's testing as a "layout builder", and reshaped on 2026-11-04): a window for searching tools and dragging them anywhere, adding to toolbars or making new ones. Planned for M8.
- **Frosted glass and progressive blur for widgets** (from the docking brief): planned for M8, after a trial, with plain translucency until then.
- **Windows dragged out to a second monitor, Illustrator's collapse to icons, Unity's Panel and Collapse modes** (from the docking brief): parked (`docs/DOCKING.md` §10).
- **Minimap** (raised after 2.2's testing; **remind the owner when the work turns mostly to UI design**): an optional panel showing where the view is on a big canvas, as Aseprite has, for people using 4000 × 4000 canvases. It answers the one thing lost by having no scrollbars.
- **Decisions for the docking pass** (step 2.5): decided on 2026-11-04, in `docs/DOCKING.md`. Windows dock at the sides only, toolbars have a small grip, and toolbars and windows never share a tab or pass each other's height.
- **Eraser ideas** (2.4): a cursor that shows it's the eraser, and brush sizes (M3).
- **Layers panel ideas** (raised in 2.3): dragging rows to reorder, and icons instead of checkboxes, as part of the owner's design.
- **Debug menu in release builds** (raised in 1.5): hidden by default in release builds, but a setting, probably on an "Advanced" tab of the settings window (M8), turns it on for curious users and contributors hunting bugs. Today the Debug menu exists only in Debug builds.
- **Draw mode and text mode** (raised in 1.5): two primary ways of working. Draw mode works like an image editor, painting with brushes; text mode is a tool for typing text. Space + drag pans in draw mode only.
- **Document options** (raised in 1.5): the New dialog (1.6) chooses Unicode or ASCII only; document options should switch it later, with a warning that switching to ASCII removes every other character. Until then it's Document → ASCII Only.
- **Blinking caret** (raised in 1.5): blink at the system's caret rate where the OS has one (on Windows, the Control Panel keyboard setting), else every 0.5 s. The rate becomes a setting later (M8).
- **Rebinding the caret keys and jump sizes** (raised in 1.5; the jumps were built in 1.6): part of the shortcut settings (M8).
- **Typing pause as a setting** (raised in 1.5): the 2-second pause that ends an undo step could be an advanced setting (M8).
- **How missing glyphs are flagged** (1.3): the orange and red cells are placeholders.
- **Color and plugins** (decided after 2.3's testing, ADR 0032): colour and ANSI are in, as milestone M2b. Plugins are in, as M8b, with a built-in Discord ANSI message plugin. Opacity isn't planned; wide (CJK) characters could be a plugin.
- **Notice when a file changes outside the app** (raised in 1.7): a message when the open document's file is deleted or changed elsewhere, instead of locking the file (ADR 0024). It's on the roadmap for M9.
