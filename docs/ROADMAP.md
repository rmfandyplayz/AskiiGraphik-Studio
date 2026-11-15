<!-- Written by Claude (Anthropic). Claude Code updates step status; changes to scope or order need the owner's approval. -->

# Roadmap

## How this file works

- Milestones run in order. Only the current milestone is broken into steps; later milestones are listed as scopes. **Before starting a milestone, Claude Code proposes its step breakdown and the owner approves it.**
- **One step = one Claude Code session.** Each step ends with something the owner can test (or, for core-only steps, green CI plus a short check). Steps are sized to fit comfortably in one usage window.
- **Step status:** `[ ]` to do · `[~]` built and ready for the owner to test · `[x]` the owner confirmed it passes. Claude Code sets `[~]` and the owner's confirmation sets `[x]`.
- **An open step** has its scope, what the owner checks, and a note of a line or two on where it stands.
- **A done step shrinks to one line:** what it added, its ADRs and its test docs. Nothing is kept here twice: decisions are in the ADRs and ARCHITECTURE.md, placeholder choices in `docs/TEMPORARY-UI.md`, and each round of testing in the step's test doc and the git history.

---

## M0: Foundation (done 2026-10-30)

- [x] **0.1 Repo scaffold.** The solution and projects, shared build settings, code style, license and the ADR process. ADRs 0001–0004 · [test doc](testing/step-0.1-repo-scaffold.md)
- [x] **0.2 CI.** Build, test and format check on Windows, macOS and Ubuntu. ADR 0005 · [test doc](testing/step-0.2-ci.md)
- [x] **0.3 App skeleton.** DI, file logging, string resources, theme tokens, the action registry, platform shortcuts and the error handler. ADRs 0006–0009 · [test doc](testing/step-0.3-app-skeleton.md)

## M1: MVP: type, undo, save (done 2026-11-01)

- [x] **1.1 Document model.** Documents, frames, layers and cells, compositing, change events, and resizing the canvas with an anchor. ADRs 0010, 0011 · [test doc](testing/step-1.1-document-model.md)
- [x] **1.2 Commands and undo.** Cell edits, composite commands, coalescing, history with a memory budget, dirty state and the layer commands. ADR 0012 · [test doc](testing/step-1.2-commands-and-undo.md)
- [x] **1.3 Fonts and glyph atlas.** Bundled fonts, cell size from font metrics, the monospace check, the rasterizer and atlas, and missing glyphs. ADRs 0013, 0014 · [test doc](testing/step-1.3-fonts-and-glyph-atlas.md)
- [x] **1.4 Canvas control.** Virtualized drawing, zoom, pan, the grid, hover, the status bar and HiDPI. ADR 0015 · [test doc](testing/step-1.4-canvas-control.md)
- [x] **1.5 Text mode.** The text tool, caret keys, typing coalesced into undo steps, and a character set per document. ADRs 0016–0018 · [test doc](testing/step-1.5-text-mode.md)
- [x] **1.6 Document actions.** New, Resize Canvas, the font picker, and placeholder menus and toolbar. ADRs 0019, 0020 · [test doc](testing/step-1.6-document-actions.md)
- [x] **1.7 Saving and opening.** `.asciiart` and `.asciiartz`, atomic saves, format migrations and unsaved-changes prompts. ADR 0021 · [test doc](testing/step-1.7-saving-and-opening.md)
- [x] **1.8 Output and macOS windows.** Copy as Text, Export .txt, and the app outliving its window on a Mac. ADRs 0022–0024 · [test doc](testing/step-1.8-output-and-macos-windows.md)

## M2: Shell and layers (done 2026-11-13)

- [x] **2.1 Dock shell.** The canvas under see-through windows, the safe area, the settings file, a saved layout and the Window menu. Its last checks moved into 2.5. ADRs 0025–0027, 0029 (all but 0026 since superseded) · [test doc](testing/step-2.1-dock-shell.md)
- [x] **2.2 History panel.** Named undo steps; clicking one jumps there. ADR 0028 · [test doc](testing/step-2.2-history-panel.md)
- [x] **2.3 Layers panel.** The active layer and every layer operation, with a Layer menu. ADRs 0030, 0031 · [test doc](testing/step-2.3-layers-panel.md)
- [x] **2.4 Space option and hard eraser.** Spaces as empty per layer, tool switching, and a 1-cell eraser. ADR 0033 · [test doc](testing/step-2.4-space-option-and-eraser.md)
- [x] **2.4b Remembered font files.** Fonts loaded from a file survive a restart. ADR 0034 · [test doc](testing/step-2.4b-remembered-fonts.md)
- **2.5 Docking pass.** Our own docking system replaced the Dock library. Spec: `docs/DOCKING.md`, ADR 0035.
  - [x] **2.5a Spec.** `docs/DOCKING.md` and this split. No code.
  - [x] **2.5b Replace Dock.** The `AskiiArt.Docking` project, the default layout, the Window menu, a saved layout, the safe area and click-through. [test doc](testing/step-2.5b-replace-dock.md)
  - [x] **2.5c Toolbars.** Floating, dragging with zones and a live preview, Horizontal and Vertical, fades and grips. [test doc](testing/step-2.5c-toolbars.md)
  - [x] **2.5d Sub-toolbars.** One at a time, with a Debug sample. [test doc](testing/step-2.5d-sub-toolbars.md), [follow-ups](testing/step-2.5d-hidden-toolbars.md)
  - [x] **2.5e Widgets and floating windows.** Stacks, title-only windows, hover buttons and menus, dragging, resizing, floating and the edge strip. [test doc](testing/step-2.5e-widgets-and-floating-windows.md), [follow-ups](testing/step-2.5e-window-follow-ups.md)
  - [x] **2.5f Docks.** Columns, tabs, and converting between docks and widgets. [test doc](testing/step-2.5f-docks.md)
  - [x] **2.5g Opening rules and polish.** Where new windows go, the drop marker, too-small windows, and 2.1's last checks. [test doc](testing/step-2.5g-opening-rules-and-polish.md), [follow-ups](testing/step-2.5g-follow-ups.md)

## M2b: Colour

Added after 2.3's testing (ADR 0032), before M3 so every tool from then on writes colour. Later milestones keep their numbers. Decided with the owner on 2026-11-14: a document is monochrome, ANSI (the standard 16 colours, shown with the VGA palette) or RGB. Each cell has a text colour and a background, and either can be none. Backgrounds composite as their own layer of colour. The base app has no text attributes such as bold, but the hook stays for plugins. Discord's 8 + 8 colours belong to its plugin (M8b).

- [x] **2b.1 Colour model.** Text and background colours per cell, the colour mode and its conversion, backgrounds composited on their own, and file format version 2. ADRs 0036, 0037 · [test doc](testing/step-2b.1-colour-model.md)
- [x] **2b.2 Colour on the canvas.** Each cell's colours drawn in the same batched glyph draw, a Debug colour sample, and box and block characters fitted to the cell so they join without seams. ADRs 0038, 0039 · [test doc](testing/step-2b.2-colour-on-the-canvas.md)
- [x] **2b.2b Frame-rate readout.** Debug → Show Frame Rate: the average, the 1% low and the slowest draw at the right of the status bar. ADR 0040 · [test doc](testing/step-2b.2b-frame-rate-readout.md)
- [x] **2b.3a Hand tool and colours.** A Hand tool (V) that pans with a left-drag, with Esc switching to it so a tool is always chosen. The New dialog's colour choice (monochrome by default), temporary Document menu items that warn before converting or removing colours, an FG colour and a BG colour that X swaps, the text tool writing them, and three windows: Brush Options (the two colours), Color (an RGB picker from Avalonia.Controls.ColorPicker) and Swatches (the 16 ANSI colours).
  *Owner checks:* the Hand tool and Esc, typing in colour across layers, switching modes and undoing, saving and reopening.
  *Note:* reshaped with the owner on 2026-11-14 (two brushes under one tool, and the windows). Avalonia's picker may later give way to our own if it can't be styled enough.
  *Note:* also decided 2026-11-14: the FG colour is what the FG brush paints and typing writes as text; the BG colour is what the BG brush paints and typing writes as background, where none keeps the cell's own. Brush Options' two buttons choose which one the Color and Swatches windows change. X swaps them; D resets both to none, marked as temporary. Esc switches to the Hand tool from any tool, except during a stroke, where it only cancels the stroke; the app still starts on the Text tool. The Hand tool is likely to give way to a Select tool later, which is why it's V. The first plan's 2b.3a (Hand) and 2b.3b (Colours) were merged into this step at the owner's request.
  *Note:* built 2026-11-14 (ADRs 0041 and 0042). The colour mode became three check-mark items in the Document menu, next to ASCII Only, instead of an Edit → Color menu. [test doc](testing/step-2b.3a-hand-tool-and-colours.md), [follow-ups](testing/step-2b.3a-follow-ups.md)
  *Note:* follow-ups after the owner's testing on 2026-11-14, ready for testing: Brush Options became a toolbar, None shows as a slashed square, and the pan cursor is an open hand on a Mac and the move arrows elsewhere.
- [x] **2b.3b Brush.** A Brush tool (B) with an FG brush and a BG brush on its sub-toolbar (1 and 2) that paint one cell's text colour or background, and Alt+click (Option+click) with the brush to pick up a cell's colour.
  *Owner checks:* painting in colour across layers, undoing, saving and reopening.
  *Note:* right-click doesn't paint yet. The eyedropper picks only from the canvas for now; a later image layer for reference images can be picked from the same way.
  *Note:* built 2026-11-14 (ADR 0043), ready for testing. The FG/BG brush and the Brush Options choice are one choice, as the owner asked; Alt+click picks from what you see, only the colour the brush paints. [test doc](testing/step-2b.3b-brush.md)
  *Note:* follow-ups after the owner's testing on 2026-11-15, ready for testing: the menu bar's letters no longer stay underlined after an Alt+click, and Esc while picking is tested with Alt let go first, since Alt+Esc is Windows'. [follow-ups](testing/step-2b.3b-follow-ups.md)
- [~] **2b.4 ANSI export.** Export .ans and Copy as ANSI, in UTF-8 with escape codes: 16-colour codes for an ANSI document, 24-bit ones for RGB.
  *Owner checks:* the exported art in a terminal on Windows and macOS.
  *Note:* built 2026-11-15 (ADR 0044), ready for testing. Both sit beside their Text versions and still work in a monochrome document, as the owner chose; Debug → Open ANSI Test Sample and Open RGB Test Sample give art sized for a terminal. [test doc](testing/step-2b.4-ansi-export.md)

## M3: Ramps and brushes

Measure glyph density for each font. Add the ramp model, a ramp editor panel, and ramp presets that can be saved, loaded and shared as files. Add the brush tool (size, shape, intensity, ramp, and pen pressure where available), the soft eraser, a tool options panel and a character picker panel.

## M4: Selection

Add the selection mask and the rectangle, ellipse, lasso, polygonal lasso and magic wand tools. Support the replace, add, subtract and intersect combine modes, plus select all, select none, invert, grow and shrink. Drawing tools clip to the selection. Add cut, copy and paste with a floating selection, the move tool, and deleting the selection's contents.

## M5: Drawing tools

Line, curve, rectangle, rounded rectangle, ellipse, fill (flood fill, contiguous or global), pickup, table, and big text. Each tool's behavior is specified with the owner before it's built.

## M6: Image → ASCII

As described in ARCHITECTURE §17: an import dialog with a live preview, the structural matcher plus a fast ramp mode, dithering, and adjustable parameters. The result goes onto a new layer.

## M7: Export and import

PNG export (font, scale, padding, colors, and a transparent-background option), HTML export, text import, ANSI import, a classic `.ans` export (CP437 with a SAUCE record) for old-school ANSI art viewers, pasting text from the clipboard, and export options (line endings, trimming trailing spaces). PNG and HTML carry the document's colours (M2b).

## M8: Customization

A toolbar customizer: a window for searching tools and dragging them anywhere, adding to toolbars or making new ones. It replaces the earlier "layout builder" idea, since the docking pass already lets everything be placed anywhere. Also our own colour picker in place of Avalonia's, laid out to grow with its window and with control over each part (decided 2026-11-15), a tool palette, a shortcut-rebinding UI, a settings window, light/dark theme switching, layout presets, and frosted glass with a progressive blur for widgets, after a trial to see what they cost (`docs/DOCKING.md` §10).

## M8b: Plugins

Added with M2b (ADR 0032). Loading plugins, a versioned public API for tools, importers, exporters, actions and panels, a way to manage installed plugins, and a Discord ANSI message plugin built in as the API's first user. How plugins are written, what they're allowed to do and how they're licensed get decided with the owner first (ARCHITECTURE §23).

## M9: Ship-readiness

Autosave and crash recovery, recent files, a notice when the open file is deleted or changed outside the app (ADR 0024), a file association for the document extension, the app icon, and an About / credits / licenses screen. Packaging for Windows (installer or zip), macOS (an unsigned `.app` bundle, with instructions in the README) and Linux (AppImage or tarball). A release workflow that runs when a tag is pushed.

---

## Parallel track: the owner's UI design

Whenever the owner delivers designs (layout, icons, text), Claude Code adds an integration step at the front of the queue.

**When the work turns mostly to UI design, remind the owner of the parked ideas in `docs/TEMPORARY-UI.md`,** the minimap in particular.

## Reserved, not planned

Per-cell alpha, text attributes (bold, underline and so on), layer opacity, blend modes, animation and wide characters. See ARCHITECTURE §18. The owner doesn't plan opacity or attributes for the base app; they and wide characters could come from plugins.
