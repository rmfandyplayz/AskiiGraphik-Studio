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

- [~] **1.1 Document model** (Core). Document, Frame, Layer and Cell; `CellPoint` and `CellRect`; occupancy; compositing with the space-policy hook; change events with dirty regions; and canvas-resize logic with an anchor. Thorough unit tests, no UI.
  *Owner checks:* CI is green. (There's nothing to click yet.)
  *Note:* Built (ADRs 0010, 0011). Mutation is internal to Core, so only commands (1.2) can change a document; canvas hard limit is 4096 per side.
- [ ] **1.2 Commands and undo** (Core). Covers:
  - the command interfaces, and a cell-edit command with a before/after diff
  - composite commands and the coalescing hook
  - history with a memory budget, and dirty-state tracking
  - a resize-canvas command, and the layer commands (add, remove, reorder, properties), even though layers have no UI yet

  Includes the property-based test: a random sequence of commands, then undo all, returns the original document.
  *Owner checks:* CI is green.
- [ ] **1.3 Fonts and glyph atlas** (Imaging). Covers:
  - the bundled fonts, with their licenses
  - loading fonts from a file and from the system
  - deriving cell size from font metrics, and the monospace check
  - glyph rasterization and the atlas cache
  - missing-glyph handling (**ask the owner first**, ARCHITECTURE §23)

  A test generates a "font sheet" PNG for each bundled font so the owner can eyeball it.
  *Owner checks:* the font sheets look right; CI is green.
- [ ] **1.4 Canvas control** (App). Renders a document (temporary sample content) with virtualization and the atlas. Covers:
  - zoom (Ctrl/Cmd + mouse wheel, plus keyboard shortcuts)
  - pan (scrollbars, middle-drag, space-drag)
  - a grid toggle and hover highlight
  - a status bar showing cell coordinates and zoom
  - correct HiDPI rendering

  *Owner checks:* zooming and panning are smooth on a 500×500 sample; text is crisp at 100% and 150% Windows scaling and on Retina.
- [ ] **1.5 Text mode** (a Core tool, plus App wiring). Click to place the caret, and typing writes glyphs. Arrow keys, Home/End, Backspace, Delete and Enter work. Typing coalesces into undo steps. Undo and Redo actions use platform shortcuts. (**Ask about the charset policy first**, ARCHITECTURE §23.)
  *Owner checks:* typing, editing, and undo/redo behave as the test doc describes.
- [ ] **1.6 Document actions.** Adds:
  - New (with a canvas-size dialog)
  - Resize Canvas (a size + anchor dialog, with the >500 warning)
  - a font picker (bundled fonts, system fonts, load from file)
  - placeholder menus and a toolbar, bound to actions

  *Owner checks:* each dialog works, resizing with different anchors and then undoing works, and changing the font resizes cells correctly.
- [ ] **1.7 File format and output.** Adds:
  - `.asciiart` Save, Save As and Open, in both plain and compressed variants, with header sniffing, a format version, and atomic saves
  - round-trip tests, and the scaffolding for future format migrations
  - unsaved-changes prompts on close, New and Open, and a window title that shows the file name plus a dirty marker
  - macOS window behavior: closing the window closes the document and the app keeps running, and clicking the Dock icon or choosing New opens a new window (requested in 0.3 testing)
  - Copy as Text (to the clipboard) and Export .txt

  *Owner checks:* both variants save and reopen, the plain file shows the art when opened in a text editor, the unsaved-changes prompts behave correctly, copied art pastes correctly into a text editor, and on a Mac, closing the window leaves the app running.

**The MVP is complete after 1.7.**

## M2: Shell and layers

Adopt Dock: dockable, floating and tabbed panels, a saved layout, and a Reset Layout action. Add a placeholder layers panel (add, delete, duplicate, reorder, rename, visibility, lock, merge down) and the concept of an active layer. Add the space-policy option (with its scope decided with the owner), the hard eraser, and a history panel (also useful for testing undo).

Docking is adopted here, before most panels exist, so later panels don't need to be rehoused.

## M3: Ramps and brushes

Measure glyph density for each font. Add the ramp model, a ramp editor panel, and ramp presets that can be saved, loaded and shared as files. Add the brush tool (size, shape, intensity, ramp, and pen pressure where available), the soft eraser, a tool options panel and a character picker panel.

## M4: Selection

Add the selection mask and the rectangle, ellipse, lasso, polygonal lasso and magic wand tools. Support the replace, add, subtract and intersect combine modes, plus select all, select none, invert, grow and shrink. Drawing tools clip to the selection. Add cut, copy and paste with a floating selection, the move tool, and deleting the selection's contents.

## M5: Drawing tools

Line, curve, rectangle, rounded rectangle, ellipse, fill (flood fill, contiguous or global), pickup, table, and big text. Each tool's behavior is specified with the owner before it's built.

## M6: Image → ASCII

As described in ARCHITECTURE §17: an import dialog with a live preview, the structural matcher plus a fast ramp mode, dithering, and adjustable parameters. The result goes onto a new layer.

## M7: Export and import

PNG export (font, scale, padding, colors, and a transparent-background option), HTML export, text import, pasting text from the clipboard, and export options (line endings, trimming trailing spaces).

## M8: Customization

Rearrangeable toolbars and tool palette, a shortcut-rebinding UI, a settings window, light/dark theme switching, and layout presets.

## M9: Ship-readiness

Autosave and crash recovery, recent files, a file association for the document extension, the app icon, and an About / credits / licenses screen. Packaging for Windows (installer or zip), macOS (an unsigned `.app` bundle, with instructions in the README) and Linux (AppImage or tarball). A release workflow that runs when a tag is pushed.

---

## Parallel track: the owner's UI design

Whenever the owner delivers designs (layout, icons, text), Claude Code adds an integration step at the front of the queue.

## Reserved, not planned

Color and ANSI, per-cell alpha, blend modes, animation, plugins and wide characters. See ARCHITECTURE §18.
