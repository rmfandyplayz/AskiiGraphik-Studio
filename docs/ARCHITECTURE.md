<!-- Written by Claude (Anthropic). Maintained by Claude Code. Changes need the owner's approval and an ADR in docs/decisions/. -->

# AskiiArt: Architecture

> **Status:** v0.1, the agreed baseline before any code exists.
> **Name:** "AskiiArt" is the code name, used for the namespaces and the solution. The product's display name is currently **AskiiGraphiks Studio** (tentative); it lives only in `Strings.resx` (`AppDisplayName`). Keep the display name and file extension in single constants so renaming is cheap.

---

## 1. What we're building

A modern, cross-platform (Windows, macOS, Linux) editor for text art. It's inspired by the old Windows freeware *ASCII Art Studio*, but it should feel like a modern image editor: layers, selections, customizable brushes, real undo history, dockable panels.

- **Primary use:** plain monochrome, density-based ASCII art.
- **Must stay cheap to add later:** Unicode beyond ASCII, ANSI-style color, per-cell alpha, layer blend modes, animation frames, plugins. These are **reserved**. The model has room for them, but nothing designs, builds or exposes them (§18).
- **Roles:** the owner designs all UI/UX (layout, icons, wording) and tests by hand. Claude Code owns implementation. Placeholder UI is allowed and expected (§14.6).
- **Pace:** long-term, with quality first. Work proceeds in small steps, each covered by automated tests and then tested by the owner by hand.
- **License:** GPL-3.0-or-later. The repo is public, but the project isn't looking for contributors.

## 2. Principles

1. **The core knows nothing about UI.** `AskiiArt.Core` references neither Avalonia nor SkiaSharp. Everything that matters (the document, tools, commands, selection, ramps and the file format) is testable headlessly.
2. **Reserve, don't build.** For a reserved feature, add the minimal structural hook (a field, a list of one, an interface) and stop there. No speculative logic and no UI.
3. **Every document change is a command.** If it changes the document, it goes through the command system and can be undone. There are no exceptions.
4. **One glyph rasterizer.** Screen rendering, PNG export and glyph-density analysis all use the same rasterization path, so what you see is what you export.
5. **UI is replaceable.** Views stay thin, and behavior lives in view models, actions and the core. Bringing in the owner's final design should change views and styles, not logic.
6. **Correctness before speed, except on documented hot paths** (§21). On those, allocation and complexity are designed in from the start.
7. **Decisions are written down and kept current.** Anything this document doesn't cover gets asked, then recorded as an ADR. When a decision changes, its ADR is superseded and this document is updated in the same step. The docs must always describe how the code works now (see "Keeping decisions current" in CLAUDE.md).

## 3. Tech stack

| Area | Choice | Notes |
|---|---|---|
| Runtime | .NET 10 (LTS, supported to Nov 2028) | Latest C#, nullable enabled |
| UI | Avalonia 12.x, latest stable | Avalonia has no LTS label; 12.x is the current stable line |
| MVVM | CommunityToolkit.Mvvm | Source-generated and minimal |
| DI | Microsoft.Extensions.DependencyInjection | Holds the registries for tools, importers, exporters and actions |
| Graphics | SkiaSharp | **Use the exact version Avalonia depends on.** Never pin a different one. |
| Docking | Dock (wieslawsoltes/Dock), Avalonia 12 package line: Dock.Avalonia, Dock.Avalonia.Themes.Fluent, Dock.Model.Mvvm, Dock.Serializer.SystemTextJson | A see-through layer over the canvas (§14.4, ADR 0025) |
| Tests | xUnit v3, Verify (snapshot tests), CsCheck (property-based tests), Avalonia.Headless | Run on Microsoft.Testing.Platform. xUnit v3 stays on 3.x and Verify on 32.x until Avalonia.Headless.XUnit supports xUnit 4 (ADR 0002) |
| Logging | Microsoft.Extensions.Logging, writing to a rolling file through Serilog (Serilog.Extensions.Logging, Serilog.Sinks.File) | The owner attaches the log to bug reports (ADR 0006) |
| CI | GitHub Actions, matrix of Windows, macOS and Ubuntu | Build and run all tests on every push and pull request, and check formatting (ADR 0005) |

All package versions are pinned in `Directory.Packages.props` (Central Package Management). Before adding a package that isn't listed here, ask first (see CLAUDE.md), and check that its license is compatible with GPL-3.0.

## 4. Solution layout

```
AskiiArt.slnx                XML solution format (ADR 0003)
global.json                  pinned .NET SDK and test runner (ADR 0002)
Directory.Build.props        shared settings: TFM, nullable, warnings-as-errors, analyzers
Directory.Packages.props     pinned package versions
.editorconfig  .gitattributes  .gitignore  LICENSE  README.md  THIRD-PARTY-NOTICES.md  CLAUDE.md
.github/workflows/ci.yml     build and test on Windows, macOS and Ubuntu (ADR 0005)
docs/
  ARCHITECTURE.md  ROADMAP.md
  decisions/                 ADRs, named NNNN-short-title.md
  testing/                   TEMPLATE.md plus one manual test doc per step
src/
  AskiiArt.Core/             model, commands, selection, ramps, tool logic, file format. BCL only.
  AskiiArt.Imaging/          fonts, glyph rasterization and atlas, glyph density, PNG export, image→ASCII. Core + SkiaSharp.
  AskiiArt.App/              Avalonia app: shell, views, view models, canvas control, actions, settings.
tests/
  Directory.Build.props      settings shared by the test projects
  AskiiArt.Core.Tests/
  AskiiArt.Imaging.Tests/
  AskiiArt.App.Tests/        Avalonia.Headless interaction tests (few and focused, see §19)
```

Dependencies point strictly in the order `App → Imaging → Core`. Core never references the other two, and Imaging never references Avalonia. Inside each project, folders are organized by feature (`Documents/`, `Commands/`, `Selection/`, `Tools/`, `Ramps/`, `Formats/`, …), not by kind (`Interfaces/`, `Models/`).

## 5. Document model

### 5.1 Hierarchy

```
Document
 ├─ metadata: format version, canvas size, font reference, document settings
 └─ Frames: list<Frame>                   (exactly one in v1; see §18)
      └─ Layers: ordered list<Layer>      (index 0 = bottom)
           └─ Cells: Width × Height grid of Cell
```

Selection is not part of the document; it lives in the editing session (§10).

Every tool, command, renderer and exporter works on **the active frame** and never reads `Frames[0]` directly. That one indirection is the whole animation reservation.

### 5.2 Coordinates

Cell coordinates are `(x, y)`, meaning (column, row), 0-based, with the origin at the top left. Use dedicated `CellPoint` and `CellRect` value types rather than raw tuples, and never mix cell units and pixel units in one signature.

### 5.3 Cell

A cell is a small value type, stored densely, with a target size of 16 bytes or less.

| Field | Behavior in v1 | Reserved for |
|---|---|---|
| `Glyph` (`System.Text.Rune`) | the character | any Unicode scalar value |
| `Occupied` (flag) | **live**: empty vs. has a glyph | n/a |
| `Foreground`, `Background` | always "unset" | color update |
| `Attributes` (flags) | always none | bold, underline, etc. |
| `Alpha` | always fully opaque | alpha update |

Use `Rune`, never `char`. A `char` is a UTF-16 code unit and can't hold characters outside the Basic Multilingual Plane.

### 5.4 Occupancy: empty vs. space

**Occupancy is structural and live in v1.** Layers can't work without it.

- An **empty** cell holds nothing, so the layer below shows through. Erasing makes a cell empty.
- An **occupied** cell holding a **space** is a deliberate blank. By default it **hides** the layer below.
- The space rule is a **policy, not hardcoded behavior**. An option lets spaces be treated as empty during compositing; whether it applies per layer or per document is an open question (§23). Implement it as one compositing policy object, not as `if (glyph == ' ')` checks scattered through the code.

### 5.5 Layers

Each layer has a stable `Id` (a GUID; commands reference layers by id, never by index), plus `Name`, `Visible` and `Locked`. It also has the reserved fields `Opacity` and `BlendMode` (§18). Every layer operation is a command: add, delete, duplicate, reorder, rename, merge down and property changes.

### 5.6 Compositing

Layers are flattened top to bottom, one cell at a time. The first visible layer whose cell is occupied, after applying the space policy, wins. If every layer is empty at that cell, the result is empty. An empty cell exports as a space in text output and as background in image output.

There is one compositing implementation, in Core. The renderer, exporters, clipboard and the magic wand's "sample all layers" option all use it. Cached composite tiles can be added later if profiling shows a need.

### 5.7 Canvas size

The canvas has a fixed size that the user can resize. Resizing is a command with an **anchor**: nine points that set which edge or corner stays put. Shrinking crops the canvas, and the command stores the cropped cells so undo restores them.

- **Soft limit: 500 × 500.** Above this, warn the user (framed around memory and usability), but allow it.
- **Hard limit:** a sane technical maximum to guard against typos (for example 4096 per side). Record the chosen value in an ADR.

### 5.8 Storage

Each layer's cells sit behind an internal storage abstraction. The v1 implementation is one dense array per layer. If memory or undo cost becomes a problem, this can be swapped for lazily allocated fixed-size tiles without changing the public API: empty tiles would cost nothing, and dirty tracking and undo snapshots would work per tile. Don't build tiles now.

### 5.9 Change notification

The document raises change events that carry the **affected region**: the layer id plus the dirty cell rect or rects. The renderer and other views invalidate only those regions. View models never wrap individual cells.

### 5.10 Document metadata

- **Font reference:** the family name the document was authored with, changed with a command. Store this, not a file path. If the font isn't available when the document opens, fall back to the default font and tell the user. The style is always Regular and the size at 100% zoom is the same for every document, so neither is stored yet (ADR 0019).
- **Document-level settings** (the character set, §6, and the space policy if it ends up per-document) live in the metadata and change only through commands.

## 6. Characters and width

- In v1 the grid is **always one column per cell**.
- A `CharWidth` service can compute each glyph's expected display width (1 or 2 columns, following East Asian Width rules). In v1 it's used only to flag wide characters; the owner decides how they're flagged visually when this comes up. It's the hook for real wide-character support later.
- Control characters are never stored. Input filters them out. Neither are characters that only modify or join their neighbours, such as combining accents.
- **Character set** (decided in step 1.5, ADR 0016): each document is either **Unicode** or **ASCII only** (printable ASCII), chosen when it's created and switchable later from document options. Switching to ASCII only warns the user, then removes every other character (undoably). A document never holds a character its set doesn't allow.

## 7. Intensity, ramps and glyph density

One mechanism powers brushes, soft erase and image conversion.

- **Intensity** is a value in [0, 1] that a tool produces for a cell. 0 means nothing, and 1 means the most ink.
- A **ramp** is an ordered, user-editable mapping from intensity bands to glyphs, for example `" .:-=+*#%@"`. Ramps are first-class objects: they're named, stored in user settings, and can be exported and imported as small files so users can share them. Whether intensity 0 maps to an empty cell or to a space is part of each ramp's definition.
- **Glyph density** is how much ink a glyph actually puts on screen in a given font, measured by rasterizing it (Imaging, §11). It's used to auto-generate a ramp from a font, to sort a user's characters by darkness, for soft erase (glyph → intensity → lighter glyph), and for image conversion.

Core defines `Ramp` and the `IGlyphDensityProvider` interface, and Imaging implements the provider. Core tests use a fake provider.

## 8. Tools

### 8.1 Shape of a tool

Tools are **Core** classes with no UI dependencies. Each tool has:

- **A descriptor:** a stable string id (`"askiiart.brush"`), a resource key for its localized name, an icon key, a default shortcut (added with the second tool) and a category.
- **Options:** a plain model object (size, ramp id, mode, …) that the UI binds to. The tool doesn't know what draws its options panel.
- **Input handling:** the tool receives Core input events. These are pointer down, move and up events in **cell coordinates plus sub-cell position**, with modifiers and **pressure** (reserved for drawing tablets), and key commands (move left, jump right, row end, …) for text mode. The App translates Avalonia events into these; a per-platform keymap turns keys into commands, following each platform's conventions (ADR 0017).
- **Preview, then commit:** during a gesture, a tool can draw a preview overlay that isn't part of the document. When the gesture ends, it emits **exactly one** command. Pressing Esc cancels the gesture and discards the preview.
- **Constraints:** every tool respects the active selection (§10) and the active layer's lock.
- **Space:** each tool says whether Space + drag pans the canvas while it's active. Text mode doesn't, so Space types a space (ADR 0017).

Tools edit through an **editing session**: the document, its undo history and the active layer. New replaces the window's session, and everything that edits the document follows the active one (ADR 0020). Tools are registered through DI in a `ToolRegistry` and created per session. Built-in tools register exactly the way a future plugin would (§18).

### 8.2 Planned tools

Planned tools, built over several milestones: text, brush, eraser, fill, pickup (eyedropper), line, curve, rectangle, rounded rectangle, ellipse, big text, table, the selection tools (§10), and move. Each tool's detailed behavior is specified with the owner when its milestone starts.

### 8.3 Eraser

The eraser has two modes. Hard mode arrives with layers (M2). Soft mode needs glyph density, so it arrives with ramps (M3).

- **Hard:** everything inside the brush footprint becomes empty, and everything outside it is untouched.
- **Soft:** lowers intensity. Each affected glyph is mapped to a lighter one using glyph density and the active ramp. When intensity reaches zero, the cell becomes empty.

If alpha ever becomes real, a third mode could lower alpha instead. That mode is reserved; don't build it.

## 9. Commands and undo

- Undo uses the command pattern. Each command can apply and revert itself, and it stores **only what it touched**. For cell edits, that's a compact list of (layer id, cell index, before, after).
- **One gesture = one command:** a brush stroke, a fill, a shape.
- **Coalescing:** consecutive typing in text mode merges into one command. The merge ends when the caret jumps, the tool changes, a pause threshold passes, or the user does anything else. Repeated nudges coalesce the same way.
- **Composite commands** group several commands into one undo step (for example, paste = add layer + write cells).
- History has a **memory budget** that's configurable, with a default such as 256 MB. When the budget is exceeded, the oldest entries are dropped. There is no fixed limit on the number of steps.
- The document's **dirty state** comes from comparing the history position with the last-saved position. Undoing back to the saved state clears "unsaved changes".
- Whether selection changes count as undo steps is an open question (§23).

## 10. Selection

- A selection is a **mask**: an arbitrary set of cells (a bitset the size of the canvas) plus a cached bounding rect. It isn't limited to rectangles.
- The selection lives in the **editing session**, not in the document, and it isn't saved to files.
- Creation tools: rectangle, ellipse, lasso (freehand), polygonal lasso, and magic wand. The magic wand can work contiguously or globally, matches by glyph (later also by color), and samples either the active layer or all visible layers. Other selection operations: select all, select none, invert, grow, shrink, and select occupied cells.
- Combine modes: replace, add, subtract and intersect, chosen with modifier keys or a tool option.
- When a selection exists, every drawing tool clips to it.
- Selected content can be moved, copied, cut and pasted. Paste creates a **floating** selection that commits as a command. Details get settled at that milestone.
- How the selection looks (marching ants or similar) is the owner's design call.

## 11. Fonts and glyph rasterization (Imaging)

- **Bundled fonts:** JetBrains Mono (the default), Roboto Mono, IBM Plex Mono and Fira Code, Regular weight only, all OFL-1.1. Each one ships with its license file and is listed in THIRD-PARTY-NOTICES.md (ADR 0013).
- **User fonts:** any TTF or OTF file the user picks, or any installed system font. Fonts loaded from a file are remembered until the app closes, until the user font list is stored in settings (§15, ADR 0019).
  - **Cell size comes from font metrics:** the advance width of a reference glyph (`0`) × the line height (ascent + descent + line gap), read from the font file so it's the same on every OS (ADR 0013). It is never hardcoded.
  - **Monospace check:** measure the advance of a sample of printable ASCII glyphs. If they differ, warn the user but still allow the font; the grid uses the reference advance.
  - **Damaged files:** a file that isn't a readable font, or whose name, glyphs or cell size are broken, is rejected with a message and never crashes the app (ADR 0019).
  - **Missing glyphs:** drawn with an installed font that has them, or as a placeholder box if none does; a fallback glyph too big for the cell is shrunk to fit. Either way the cell is flagged as an error (ADR 0013); how the flag looks is still open (§23). It must never crash.
- **Too-wide glyphs:** a glyph from the document's font that reaches well past its cell (like `↔` in some fonts) is squeezed sideways to fit; box-drawing and block characters are clipped instead, so they still join their neighbours (ADR 0014).
- **Ligatures can't happen,** because each cell is rasterized as a single glyph and never shaped as a run of text.
- **Glyph atlas:** rasterized glyphs are cached per (font, pixel size, render options) and reused. Glyphs are rasterized at the actual device-pixel size for the current zoom and DPI so text stays crisp. A glyph bitmap is never scaled.
- **One rasterizer** (Principle 4): the atlas, PNG export and glyph-density measurement all share the same Skia code path and settings.

## 12. Rendering (the App's canvas control)

- **One custom control** draws the whole canvas. There are no per-cell controls and no `TextBlock`s.
- **Virtualized:** it draws only the cells that intersect the viewport, so cost scales with the number of visible cells, not with document size.
- **Dirty regions:** only the regions that changed are re-composited from their layers (§5.9). Avalonia always repaints a control as a whole, so each frame redraws the visible cells from the cached composite and the glyph atlas, which is cheap. A cache of pre-drawn tiles gets added only if measurements show it's needed.
- **Draw order:** composite → grid lines (toggleable) → tool preview overlay → selection overlay → caret and hover highlight. Overlays never touch the document.
- **Zoom:** discrete steps (25% to 800%) where the cell size lands on whole device pixels, so no seams appear between cells. Zoom centers on the cursor. Pan with the scrollbars, the mouse wheel, middle-drag or Space-drag (when the tool allows it, §8.1). Wheel events are grouped into gestures, so a touchpad's glide never turns into a zoom (ADR 0018).
- **HiDPI:** rendering must be crisp under Windows display scaling (125%, 150%, …) and on Retina displays. Test both.
- **Target:** smooth zoom, pan and brush strokes on a 500 × 500 canvas (§21).
- **Drawing approach:** a custom draw operation paints through a direct Skia lease, one batched `DrawAtlas` call per atlas page. It measured 1.2–4× faster than drawing atlas bitmaps through Avalonia's `DrawingContext` (ADR 0015).

## 13. File format (`.asciiart`, tentative)

There are two file extensions: `.asciiart` for the plain variant and `.asciiartz` for the compressed one (like `.svg` and `.svgz`). They may change to match the final product name, so keep them in constants.

```
plain:       "ASKIIART <format-version>\n" followed by UTF-8 JSON
compressed:  binary magic bytes + format version + Brotli-compressed UTF-8 JSON (the same JSON)
```

- The loader **sniffs** the header, so both variants open the same way, whatever the file is called. The save dialog's file types choose the variant (decided in 1.7).
- **The plain variant should be readable in a text editor.** Store each layer's glyphs as an array of row strings so the art is visible in the file, and store occupancy separately (for example as a run-length-encoded mask). Reserved fields are **left out when they hold default values**.
- **Every file has a format version,** and every version bump comes with a migration so old files keep opening. If a file's version is newer than the app understands, refuse to open it with a clear message rather than guessing.
- **Saves are atomic:** write to a temp file in the same directory, then replace the original. A half-written file is never left behind.
- **Line endings** inside the JSON are always `\n`, on every OS.
- **Round-trip guarantee:** load(save(doc)) == doc for both variants. This is tested (§19).

## 14. App shell and UI decoupling

### 14.1 MVVM

Views are thin: AXAML plus minimal code-behind, where code-behind is needed for things like the canvas. View models hold state and call into Core. View models never reference Avalonia controls.

### 14.2 Actions

Every operation the user can invoke (Undo, Save, New Layer, Select Brush, Zoom In, …) is an **action**. An action has a stable id, a localized label key, an icon key, default shortcut(s), an enabled/checked state, and an execute method. Menus, toolbars, context menus, shortcuts and (later) a command palette all bind to **action ids**. That's what lets toolbars be rearranged and shortcuts be rebound without touching any logic.

Actions are different from document *commands* (§9): a command is an undoable change to the document, and an action may create one.

### 14.3 Shortcuts

Shortcuts are defined per action and are platform-aware: `Ctrl` on Windows and Linux corresponds to `Cmd` on macOS. Use Avalonia's platform hotkey configuration and never hardcode `Ctrl`. User rebinding comes later and is stored in settings.

Menus are Avalonia native menus built from a menu layout of action ids: the system menu bar on macOS (with About in the application menu), and on Windows and Linux a menu bar in a widget that can't be removed (ADR 0007, ADR 0025).

### 14.4 Panels and docking

Each panel (layers, tool options, ramps, character picker, history, …) is a self-contained view and view model that doesn't know where it's hosted. The shell uses **Dock**, so panels can be docked, tabbed, floated and rearranged, and the layout is saved in settings.

The canvas is the main character (ADR 0025). It paints the whole window, and Dock lies on top as a see-through layer: panels are widgets over the canvas, art shows around them and slides under them, and they always get input first. Docked widgets still take up space: centering, zoom and scroll limits work within the **safe area** they leave free. Floating panels stay inside the main window.

### 14.5 Theming, icons and text

- **Design tokens:** colors, spacing, corner radii and font sizes are defined once, in a theme resource dictionary. Placeholder UI uses only these tokens, never literal colors or sizes, so the owner's design system can replace them. Light and dark themes come from swapping token sets.
- **Icons:** referenced by icon key through a single lookup. Placeholder icons can be simple shapes or text glyphs. The owner's icon set replaces the files, not the code.
- **User-visible strings:** every one lives in resource files (`.resx`) from day one and is accessed through strongly typed keys. There are no literal UI strings in AXAML or C#.

### 14.6 Placeholder UI policy

Claude Code may build any temporary UI it needs to exercise a feature. Keep it plain and functional, build it only from tokens, resource strings and actions, and make it obviously temporary. Don't spend effort polishing UI the owner will replace. Every placeholder choice is listed in `docs/TEMPORARY-UI.md`, the owner's checklist when designing. When the owner delivers a design, integrating it is its own roadmap step.

## 15. Settings and user data

- Settings are stored as versioned JSON in the platform's app-data folder (`%APPDATA%` on Windows, `~/Library/Application Support` on macOS, `$XDG_CONFIG_HOME` or `~/.config` on Linux). Resolve the folder with `Environment.GetFolderPath`; never hardcode paths (ADR 0026).
- Settings hold preferences, ramps and brush presets, the dock layout, shortcut bindings, recent files and the user font list.
- Corrupt settings never stop the app from starting. Back up the bad file, load defaults, and tell the user.

## 16. Import and export

Importers and exporters implement the `IImporter` and `IExporter` interfaces, which are defined in Core; implementations live in Core or Imaging as needed. They register through DI, the same way tools do.

| Format | Direction | Milestone |
|---|---|---|
| `.asciiart` | both | M1 |
| Clipboard (plain text, flattened) | both | copy in M1, paste later |
| `.txt` | both | export in M1, import later |
| PNG (rendered with the document font; scale, padding and color options) | export | M7 |
| HTML (`<pre>`) | export | M7 |
| Image → ASCII | import | M6 |
| ANSI (`.ans`) | both | reserved |

Text export always flattens the visible layers and writes `\n` line endings by default, with an option for `\r\n` and an option to trim trailing spaces.

## 17. Image → ASCII (advanced)

Here quality matters more than speed. This gets its own milestone, and the details are designed with the owner then. The expected shape:

- Resample the image to the canvas grid using the **current font's cell aspect ratio**, since cells aren't square.
- Preprocess: grayscale, levels and contrast, optional edge enhancement, invert.
- **Structural matching:** for each cell-sized tile, pick the glyph from the allowed set (a ramp or a charset) whose rasterized shape best matches the tile. Compare against glyph bitmaps from the atlas, using an SSIM-like metric or gradient and edge-orientation features, rather than only average brightness. Brightness-only ramp mapping is kept as a fast mode.
- Optional dithering for the tone-mapped mode.
- A live preview with adjustable parameters. Conversion runs off the UI thread, and the result is applied as one command (on a new layer by default).
- Snapshot tests against a small set of fixed reference images.

## 18. Reserved features (hooks only, no design)

| Feature | Hook that exists now | Not built |
|---|---|---|
| Animation | The document holds a list of frames (exactly one); everything works on the active frame | Frame UI, timeline, playback, export |
| Color / ANSI | `Foreground`, `Background` and `Attributes` on Cell; optional in the file format | Color tools, palettes, ANSI import/export |
| Per-cell alpha | `Alpha` on Cell | Alpha compositing, alpha eraser |
| Layer opacity / blend modes | Fields on Layer; compositing goes through one policy object | Any mode other than normal |
| Plugins | Tools, importers, exporters and actions use interfaces and DI registries | Loading external assemblies, sandboxing, plugin API versioning |
| Wide characters | `CharWidth` service; one-column grid | Double-width cells |

If implementing something would require making a reserved feature real, stop and ask.

## 19. Testing strategy

**Automated tests** (Claude Code writes and runs these):

- **Core unit tests** cover the document model, compositing, resizing, commands and undo, selection, ramps, tool logic and the file format. This is where coverage is highest and tests are fastest.
- **Property-based tests** check invariants. The two most important: applying any random sequence of commands and then undoing all of them restores the original document exactly, and save → load round-trips random documents unchanged.
- **Snapshot tests** with Verify cover tool output on a small canvas (shapes, fills), flattened text, image→ASCII output and PNG export. Approved snapshots are committed. A changed snapshot is a reviewed change and is never accepted automatically.
- **Imaging tests** cover font metrics, monospace detection, glyph-density ordering and atlas correctness.
- **Headless UI tests** (Avalonia.Headless) cover only things that survive a UI redesign: canvas input producing document changes, action wiring and shortcuts, and dirty-state and save prompts. They make no pixel or layout assertions about placeholder UI. If a headless test turns out to be flaky, delete it and cover the case in the manual test doc instead.
- **CI** runs every test on Windows, macOS and Linux. Linux gets no manual testing, so CI is its only safety net.

**Manual tests** (the owner runs these): every roadmap step produces `docs/testing/step-<id>-<slug>.md`, based on `docs/testing/TEMPLATE.md`.

## 20. Threading, errors and logging

- The document is changed **only on the UI thread**. Heavy work (image conversion, global fill on large canvases, export, compression) runs as a background task. It reports progress, supports cancellation, and returns a result that the UI thread applies as a command.
- **Saving and text export are the exception so far:** reading the document has to happen on the UI thread, so turning it into JSON or text happens there, and only compressing and writing run in the background, with no progress or cancel (ADRs 0021, 0022). The owner tried this at 4096 × 4096 and text export felt instant. Revisit it with PNG export (M7), where rendering takes real time.
- No `async void` except in event handlers, and no fire-and-forget work without error handling.
- Unhandled exceptions are logged with context and shown to the user as a clear message, and the app tries to keep the document safe by offering to save a recovery copy. Exceptions are never swallowed silently. How each kind of exception is handled is in ADR 0008.
- Logs go to a rolling file in the per-machine (local, non-roaming) app-data folder, under `Logs` (ADR 0006). A Help → "Open log folder" menu item makes bug reports easy.

## 21. Performance targets

| Scenario | Target |
|---|---|
| Canvas up to 500 × 500 with 10 layers | Zoom, pan and brush strokes feel instant (aim for 60 fps on mid-range hardware) |
| Typing | No perceptible latency |
| Global fill or magic wand on 500 × 500 | Under 100 ms, or run in the background with feedback |
| Opening or saving a typical document | Under 1 s |
| Canvas larger than 500 × 500 | Allowed with a warning; must stay usable but may be slower |

Hot paths (cell storage, compositing, rendering, flood fill and image conversion) avoid per-cell allocations. Measure them before and after optimizing, using BenchmarkDotNet where useful. All other code optimizes for clarity.

## 22. Licensing and third-party code

- The project is licensed GPL-3.0-or-later, and every source file carries the SPDX identifier in its header (see CLAUDE.md).
- Every dependency and bundled asset must be GPL-3.0-compatible. MIT, BSD and Apache-2.0 are fine, and so is OFL for fonts. Each one is recorded in THIRD-PARTY-NOTICES.md with its license.
- Bundled fonts ship with their license texts.
- Nothing from the original ASCII Art Studio is used: no code, icons, text, name or file format.

## 23. Open questions

Ask the owner about each of these at the milestone that needs it.

| Question | Needed by |
|---|---|
| How the canvas marks a cell whose glyph is missing from the document's font. *Decided in 1.3:* draw it with a fallback font and flag the cell as an error. *Still open:* what the flag looks like and how the user is warned | The owner's UI design (bring it up by step 1.4) |
| Charset policy. *Decided in 1.5:* per document, Unicode or ASCII only (§6, ADR 0016). *Decided in 1.6:* new documents default to ASCII only, chosen in the New dialog. *Still open:* the document options that switch it later | The owner's UI design |
| Should the space-as-empty option be per layer, per document or app-wide? *Decided in M2 planning:* per layer, built in step 2.4 | M2 |
| Should selection changes count as undo steps? | M4 |
| Default ramp(s) to ship | M3 |
| Exact behavior of each tool | That tool's milestone |
| Big-text font format (FIGlet?), and which fonts can legally be bundled | M5 |
| How to flag wide characters visually | When Unicode input is allowed |
| When the owner's UI design will arrive, and in what form (Figma, AXAML, screenshots) | Any time |

## 24. Glossary

- **ADR (Architecture Decision Record):** a short file in `docs/decisions/` that records one decision, why it was made, and the alternatives considered.
- **Action:** an operation the user can invoke, bound to menus, toolbars and shortcuts (§14.2).
- **Command:** an undoable change to the document (§9).
- **Compositing / flattening:** combining the visible layers into one grid (§5.6).
- **Glyph atlas:** a cache of pre-rendered character images (§11).
- **Intensity:** a 0–1 value for how much ink a cell should have (§7).
- **Occupancy:** whether a cell holds anything at all (§5.4).
- **Ramp:** a mapping from intensity to glyphs, for example `" .:-=+*#%@"` (§7).
- **Round-trip:** saving and then loading a document; the result must equal the original.
- **Snapshot test (golden file):** a test that compares output against a saved, human-approved expected result.
