<!-- Written by Claude (Anthropic). Claude Code updates step status; changes to scope or order need the owner's approval. -->

# Roadmap

## How this file works

- Milestones run in order. Only the current milestone is broken into steps; later milestones are listed as scopes. **Before starting a milestone, Claude Code proposes its step breakdown and the owner approves it.**
- **One step = one Claude Code session.** Each step ends with something the owner can test (or, for core-only steps, green CI plus a short check). Steps are sized to fit comfortably in one usage window.
- **Step status:** `[ ]` to do · `[~]` built and ready for the owner to test · `[x]` the owner confirmed it passes.
- Claude Code sets `[~]` and the owner's confirmation sets `[x]`. Keep the notes under each step to one or two lines. Decisions go in ADRs, not here.

---

## M0: Foundation

- [x] **0.1 Repo scaffold.** Create the solution and projects per ARCHITECTURE §4. Add `Directory.Build.props` (net10.0, nullable, warnings as errors, analyzers), `Directory.Packages.props`, `.editorconfig` (encoding the CLAUDE.md code style wherever analyzers can), `.gitattributes`, `LICENSE` (GPL-3.0), a stub `README.md`, `THIRD-PARTY-NOTICES.md`, `docs/decisions/0001-record-architecture-decisions.md` (which spells out the status and supersede rules), and the `docs/decisions/README.md` index. Add one trivial test per test project.
  *Owner checks:* `dotnet build` and `dotnet test` succeed on Windows and on Mac.
  *Note:* Built. Template mobile and browser projects were removed; xUnit and Verify are pinned to older versions so the headless tests work (ADR 0002).
- [x] **0.2 CI.** Add a GitHub Actions workflow that builds and tests on Windows, macOS and Ubuntu on every push and pull request. Put a status badge in the README.
  *Owner checks:* after a push, CI is green on all three.
  *Note:* Built (`.github/workflows/ci.yml`, ADR 0005). Also runs the format check on Linux, since the owner works in Rider. Not run yet; the first push verifies it.
- [x] **0.3 App skeleton.** The Avalonia app starts and shows an empty main window with a placeholder menu (File → Quit; Help → Open Log Folder, About). This step also adds:
  - the DI container and logging to a rolling file
  - `.resx` string resources, used by every visible string
  - the theme token dictionary
  - the action registry, with the three actions above
  - platform-aware shortcuts (Quit is Ctrl+Q on Windows and Cmd+Q on macOS)
  - an unhandled-exception handler

  *Owner checks:* the app launches on Windows and Mac, the menu and Quit shortcut work, and Open Log Folder shows a log file.
  *Note:* Built (ADRs 0006–0009). Native menus (macOS menu bar), Serilog file log in the local app-data folder, error dialog, and a Debug-build-only Debug menu.

## M1: MVP (type, undo, save)

Goal: a window with a grid you can type into, where you can pick a font, resize the canvas, undo and redo, and save and load. Layers exist in the model but have no UI yet.

- [x] **1.1 Document model** (Core). Document, Frame, Layer and Cell; `CellPoint` and `CellRect`; occupancy; compositing with the space-policy hook; change events with dirty regions; and canvas-resize logic with an anchor. Thorough unit tests, no UI.
  *Owner checks:* CI is green. (There's nothing to click yet.)
  *Note:* Built (ADRs 0010, 0011). Mutation is internal to Core, so only commands (1.2) can change a document; canvas hard limit is 4096 per side.
- [x] **1.2 Commands and undo** (Core). Covers:
  - the command interfaces, and a cell-edit command with a before/after diff
  - composite commands and the coalescing hook
  - history with a memory budget, and dirty-state tracking
  - a resize-canvas command, and the layer commands (add, remove, reorder, properties), even though layers have no UI yet

  Includes the property-based test: a random sequence of commands, then undo all, returns the original document.
  *Owner checks:* CI is green.
  *Note:* Built (ADR 0012). Coalescing uses caller-made `CoalescingGroup`s; the newest step survives the memory budget; CsCheck pinned at 4.9.1.
- [x] **1.3 Fonts and glyph atlas** (Imaging). Covers:
  - the bundled fonts, with their licenses
  - loading fonts from a file and from the system
  - deriving cell size from font metrics, and the monospace check
  - glyph rasterization and the atlas cache
  - missing-glyph handling (**ask the owner first**, ARCHITECTURE §23)

  A test generates a "font sheet" PNG for each bundled font so the owner can eyeball it.
  *Owner checks:* the font sheets look right; CI is green.
  *Note:* Built (ADRs 0013, 0014). Missing glyphs use an installed fallback font or a placeholder box and are reported per cell; sheets go to `artifacts/font-sheets/`, and CI uploads them per OS.
- [x] **1.4 Canvas control** (App). Renders a document (temporary sample content) with virtualization and the atlas. Covers:
  - zoom (Ctrl/Cmd + mouse wheel, plus keyboard shortcuts)
  - pan (scrollbars, middle-drag, space-drag)
  - a grid toggle and hover highlight
  - a status bar showing cell coordinates and zoom
  - correct HiDPI rendering

  *Owner checks:* zooming and panning are smooth on a 500×500 sample; text is crisp at 100% and 150% Windows scaling and on Retina.
  *Note:* Built (ADR 0015). Skia lease with one `DrawAtlas` per atlas page; zoom 25–800%; temporary sample document and orange/red missing-glyph markers. The macOS glide-zoom and CI fixes from its testing landed in 1.5.
- [x] **1.5 Text mode** (a Core tool, plus App wiring). Click to place the caret, and typing writes glyphs. Arrow keys, Home/End, Backspace, Delete and Enter work. Typing coalesces into undo steps. Undo and Redo actions use platform shortcuts. (**Ask about the charset policy first**, ARCHITECTURE §23.)
  *Owner checks:* typing, editing, and undo/redo behave as the test doc describes.
  *Note:* Built (ADRs 0016–0018). The text tool is always active and overwrites; per-document Unicode or ASCII only, with a temporary Edit → ASCII Only. Also fixes Cmd zooming during a touchpad glide and the macOS CI failure. The fixes from its testing (zoom-out drift, Backspace on a placed caret and at the edges, Enter following the current text) land in 1.6.
- [x] **1.6 Document actions.** Adds:
  - New (with a canvas-size dialog)
  - Resize Canvas (a size + anchor dialog, with the >500 warning)
  - a font picker (bundled fonts, system fonts, load from file)
  - placeholder menus and a toolbar, bound to actions

  *Owner checks:* each dialog works, resizing with different anchors and then undoing works, and changing the font resizes cells correctly.
  *Note:* Built (ADRs 0019, 0020). New defaults to 80 × 25 and ASCII only; the font is per document and undoable, and file fonts last until the app closes. Also adds caret jumps (per-platform keys) and the fixes from 1.5 testing. The fixes from its own testing (damaged font files, the 10-cell jump moved off Alt on Windows and Linux, size boxes that clamp) land in 1.7.
- [x] **1.7 Saving and opening.** Adds:
  - Save, Save As and Open, in a plain variant (`.asciiart`) and a compressed one (`.asciiartz`), with header sniffing, a format version, and atomic saves
  - round-trip tests, and the scaffolding for future format migrations
  - unsaved-changes prompts on close, New and Open, and a window title that shows the file name plus a dirty marker
  - the app starts with an empty document (the sample moves to the Debug menu), and opening a document whose font is missing says so

  *Owner checks:* both variants save and reopen, the plain file shows the art when opened in a text editor, and the unsaved-changes prompts behave correctly.
  *Note:* Built (ADR 0021). The fixes from 1.6 testing are in too (damaged fonts, Ctrl+Shift+arrow, size boxes that clamp). The fixes from its own testing (a damaged escape that failed CI on macOS, the open picker that didn't appear after saving on Windows) land in 1.8.
- [x] **1.8 Output and macOS windows.** Adds:
  - macOS window behavior: closing the window closes the document and the app keeps running, and clicking the Dock icon or choosing New or Open with no window opens one (requested in 0.3 testing; one window at a time)
  - Copy as Text (to the clipboard) and Export .txt

  *Owner checks:* copied art pastes correctly into a text editor, the exported file matches, and on a Mac, closing the window leaves the app running.
  *Note:* Built (ADRs 0022, 0023). On a Mac the File menu can't exist with no window open, so New and Open are in the Dock menu then; Mac windows also show the unsaved dot. The fixes from 1.7 testing are in too.

**The MVP is complete: the owner confirmed 1.8 on 2026-11-01.** (1.7 was split in two after 1.6 testing, with the owner's OK.)

## M2: Shell and layers

Docking is adopted here, before most panels exist, so later panels don't need to be rehoused.

- [~] **2.1 Dock shell.** Adds:
  - Dock, with the canvas painting the whole window and panels as widgets on top. Empty parts of the layout pass clicks to the canvas, and widgets always get clicks first
  - the safe area: centering, zoom and scroll limits count the docked widgets
  - the menu bar (except on a Mac) and the status bar as plain strips, and the toolbar and empty History and Layers panels as panels that can be docked, tabbed, floated inside the window and closed
  - no canvas scrollbars for now, and a minimum window size
  - the settings file, a saved layout, and a Window menu with panel toggles and Reset Layout

  *Owner checks:* art shows around the widgets, the layout survives a restart (and, on a Mac, closing and reopening the window), and a damaged settings file is reported and recovered from.
  *Note:* Built (ADRs 0025, 0026, 0029). Dock stays, as a see-through layer with floating kept inside the window; the owner chose this look over solid strips in M2 planning. Two rounds of testing led to fixes and design changes, awaiting a last retest of the changed parts. On 2026-11-02 the owner moved that retest into 2.5, so everything about docking and windows is checked together. This step stays open until then.
- [x] **2.2 History panel.** Named undo steps in a list, and clicking an entry jumps to that point using ordinary undo and redo.

  *Owner checks:* the list stays in sync with undo and redo, including while the panel floats.
  *Note:* Built (ADR 0028), at the owner's suggestion alongside the 2.1 fixes, so both were tested together.
- [x] **2.3 Layers panel.** Choosing the active layer; add, delete, duplicate, reorder, rename, visibility, lock and merge down.

  *Owner checks:* each operation, its undo, and saving and reopening.
  *Note:* Built (ADRs 0030, 0031), with a Layer menu whose actions the panel's buttons share. The owner confirmed it on both platforms on 2026-11-02, along with the fixes from its testing (the highlight after opening, saving the active layer).
- [x] **2.4 Space option and hard eraser.** The per-layer space-as-empty option, tool switching, and a 1-cell hard eraser (a fast drag leaves no gaps, and one stroke is one undo step).

  *Owner checks:* compositing and text export with the option on and off, erase strokes and their undo.
  *Note:* Built (ADR 0033). Also has the fixes from 2.3's testing (ADR 0031) and a fix for a crash on a Mac when a closed window reported its position. The owner confirmed it on 2026-11-02.
- [~] **2.4b Remembered font files.** Fonts loaded from a file are still there after a restart. Added at the owner's request on 2026-11-02, so documents using them stop falling back to the default font.

  *Owner checks:* a loaded font survives a restart, a document using it opens without a warning, and a moved or damaged file does no harm.
  *Note:* Built (ADR 0034). Fonts are remembered by file path in settings.
- [ ] **2.5 Docking pass.** The owner works through how docking and windowing feel, and Claude Code fixes or changes them to match. Added at the owner's request after 2.1's testing. Decisions waiting for it are listed in `docs/TEMPORARY-UI.md`. It also takes in 2.1's last retest.

  *Owner checks:* decided in the pass.

## M2b: Colour

Added after 2.3's testing, when the owner decided colour is in (ADR 0032). It comes before M3 so every tool from there on writes colour from the start. Later milestones keep their numbers.

A colour model for cells (foreground, background, and whichever attributes ANSI needs), the colours tools paint with, compositing, rendering and the file format, a colour panel, and ANSI export (`.ans` and the clipboard) as the real-world check of the model. The model itself is designed with the owner first (ARCHITECTURE §23). The owner's direction so far: a new document picks monochrome, RGB color or ANSI color, the way it picks its character set.

## M3: Ramps and brushes

Measure glyph density for each font. Add the ramp model, a ramp editor panel, and ramp presets that can be saved, loaded and shared as files. Add the brush tool (size, shape, intensity, ramp, and pen pressure where available), the soft eraser, a tool options panel and a character picker panel.

## M4: Selection

Add the selection mask and the rectangle, ellipse, lasso, polygonal lasso and magic wand tools. Support the replace, add, subtract and intersect combine modes, plus select all, select none, invert, grow and shrink. Drawing tools clip to the selection. Add cut, copy and paste with a floating selection, the move tool, and deleting the selection's contents.

## M5: Drawing tools

Line, curve, rectangle, rounded rectangle, ellipse, fill (flood fill, contiguous or global), pickup, table, and big text. Each tool's behavior is specified with the owner before it's built.

## M6: Image → ASCII

As described in ARCHITECTURE §17: an import dialog with a live preview, the structural matcher plus a fast ramp mode, dithering, and adjustable parameters. The result goes onto a new layer.

## M7: Export and import

PNG export (font, scale, padding, colors, and a transparent-background option), HTML export, text import, ANSI import, pasting text from the clipboard, and export options (line endings, trimming trailing spaces). PNG and HTML carry the document's colours (M2b).

## M8: Customization

Rearrangeable toolbars and tool palette, a shortcut-rebinding UI, a settings window, light/dark theme switching, and layout presets.

## M8b: Plugins

Added with M2b (ADR 0032). Loading plugins, a versioned public API for tools, importers, exporters, actions and panels, a way to manage installed plugins, and a Discord ANSI message plugin built in as the API's first user. How plugins are written, what they're allowed to do and how they're licensed get decided with the owner first (ARCHITECTURE §23).

## M9: Ship-readiness

Autosave and crash recovery, recent files, a notice when the open file is deleted or changed outside the app (ADR 0024), a file association for the document extension, the app icon, and an About / credits / licenses screen. Packaging for Windows (installer or zip), macOS (an unsigned `.app` bundle, with instructions in the README) and Linux (AppImage or tarball). A release workflow that runs when a tag is pushed.

---

## Parallel track: the owner's UI design

Whenever the owner delivers designs (layout, icons, text), Claude Code adds an integration step at the front of the queue.

**When the work turns mostly to UI design, remind the owner of the parked ideas in `docs/TEMPORARY-UI.md`,** the minimap in particular.

## Reserved, not planned

Per-cell alpha, layer opacity, blend modes, animation and wide characters. See ARCHITECTURE §18. The owner doesn't plan opacity for the base app; it and wide characters could come from plugins.
