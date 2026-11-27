<!-- Written by Claude (Anthropic). Maintained by Claude Code. Each change comes with an ADR in docs/decisions/ and is listed in its step's summary (CLAUDE.md). -->

# AskiiArt: Architecture

> **Status:** v0.1, the agreed baseline before any code exists.
> **Name:** "AskiiArt" is the code name, used for the namespaces and the solution. The product's display name is currently **AskiiGraphiks Studio** (tentative); it lives only in `Strings.resx` (`AppDisplayName`). Keep the display name and file extension in single constants so renaming is cheap.

---

## 1. What we're building

A modern, cross-platform (Windows, macOS, Linux) editor for text art. It's inspired by the old Windows freeware *ASCII Art Studio*, but it should feel like a modern image editor: layers, selections, customizable brushes, real undo history, dockable panels.

- **Primary use:** density-based ASCII art, in plain monochrome or with ANSI-style color.
- **Colour** (milestone M2b, ADR 0036): a document is monochrome, ANSI (the standard 16 colours) or RGB. **Planned:** plugins (M9, ADRs 0032 and 0046).
- **Must stay cheap to add later:** Unicode beyond ASCII, per-cell alpha, layer blend modes, animation frames. These are **reserved**. The model has room for them, but nothing designs, builds or exposes them (§18).
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
| Runtime | .NET 10 (LTS, supported to Nov 2028), built with the .NET 11 SDK | C# 14, the newest that .NET 10 supports, nullable enabled (ADR 0051) |
| UI | Avalonia 12.x, latest stable, with Avalonia.Controls.ColorPicker for the RGB picker | Avalonia has no LTS label; 12.x is the current stable line. The picker may give way to our own (ADR 0042) |
| MVVM | CommunityToolkit.Mvvm | Source-generated and minimal |
| DI | Microsoft.Extensions.DependencyInjection | Holds the registries for tools, importers, exporters and actions |
| Graphics | SkiaSharp | **Use the exact version Avalonia depends on.** Never pin a different one. |
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
  DOCKING.md                 how windows and toolbars behave
  decisions/                 ADRs, named NNNN-short-title.md
  testing/                   TEMPLATE.md plus the current milestone's manual test docs, one per step
    old/                     earlier test docs, a folder per milestone (milestone_2b) or release (version_<name>)
src/
  AskiiArt.Core/             model, commands, selection, ramps, tool logic, file format. BCL only.
  AskiiArt.Imaging/          fonts, glyph rasterization and atlas, glyph density, PNG export, image→ASCII. Core + SkiaSharp.
  AskiiArt.Docking/          where windows and toolbars go: layout data, solver, edits. BCL only (ADR 0035).
  AskiiArt.App/              Avalonia app: shell, views, view models, canvas control, actions, settings.
tests/
  Directory.Build.props      settings shared by the test projects
  AskiiArt.Core.Tests/
  AskiiArt.Imaging.Tests/
  AskiiArt.Docking.Tests/
  AskiiArt.App.Tests/        Avalonia.Headless interaction tests (few and focused, see §19)
```

Dependencies point strictly in the order `App → Imaging → Core`, and `App → Docking`. Core never references the others, Imaging never references Avalonia, and Docking references nothing. Inside each project, folders are organized by feature (`Documents/`, `Commands/`, `Selection/`, `Tools/`, `Ramps/`, `Formats/`, …), not by kind (`Interfaces/`, `Models/`).

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
| `HasGlyph` (flag) | **live**: whether the cell has a glyph | n/a |
| `Foreground`, `Background` | **live** (M2b): each unset, one of the 16 ANSI colours, or RGB (§5.11). Only a cell with a glyph has a text colour | n/a |
| `Attributes` (flags) | always none | bold, underline and so on, for plugins only (ADR 0036) |
| `Alpha` | always fully opaque | alpha update |

A cell holds a glyph, a background, both or neither; `IsEmpty` means neither. Use `Rune`, never `char`. A `char` is a UTF-16 code unit and can't hold characters outside the Basic Multilingual Plane.

### 5.4 Occupancy: empty vs. space

**Occupancy is structural and live in v1.** Layers can't work without it.

- An **empty** cell holds nothing, so the layer below shows through. Erasing makes a cell empty.
- A cell holding **only a background** has no glyph, so the glyph below shows through, on its colour (§5.6).
- A cell whose glyph is a **space** is a deliberate blank. By default it **hides** the glyphs below, though not their background.
- The space rule is a **policy, not hardcoded behavior**. A per-layer option lets that layer's spaces be treated as empty during compositing (ADR 0033). It's one compositing policy object, not `if (glyph == ' ')` checks scattered through the code. The owner may remove the option one day, so nothing but the glyph plane depends on it (ADR 0036).

### 5.5 Layers

Each layer has a stable `Id` (a GUID; commands reference layers by id, never by index), plus `Name`, `Visible`, `Locked` and the space option. It also has the reserved fields `Opacity` and `BlendMode` (§18). Every layer operation is a command: add, delete, duplicate, reorder, rename, merge down and property changes.

### 5.6 Compositing

Layers are flattened top to bottom, one cell at a time, in two planes (ADR 0036). The glyph and its text colour come from the first visible layer whose cell has a glyph that hides what's below, after applying the space policy. The background comes from the first visible layer whose cell has one. If every layer is empty at that cell, the result is empty. A cell without a glyph exports as a space in text output, and a cell without a background shows the paper colour in image output.

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
- **Document-level settings** (the character set, §6, and the colour mode, §5.11) live in the metadata and change only through commands.

### 5.11 Colour

- A document's **colour mode** is monochrome, ANSI or RGB, and the document never holds a colour its mode doesn't allow. New documents are monochrome. Changing the mode is one command that converts every colour, or removes them all for monochrome. The UI warns first with the number of cells that will look different (ADR 0036).
- **ANSI** means the standard 16 colours, shown with the VGA palette. RGB converts to the nearest of them by the "redmean" distance.
- An **unset** colour is the canvas's own ink or paper, and exports with no colour code.
- The base app has **no text attributes**. `CellAttributes` is a hook for plugins.

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
- **Glyph shape** is where in its cell that ink lies, measured in the same drawing. The Brush and the Soft Eraser use it to pick edge glyphs whose ink lies where the tip paints (§8.3, §8.4), lines' Precise style to pick the glyph that follows a line (§8.5), and image conversion can too (§17).

Core defines `Ramp` and the `IGlyphDensityProvider` interface, and Imaging implements the provider. Core tests use a fake provider. *Decided in 3.1 (ADR 0045):* a ramp of n glyphs has n + 1 evenly spaced levels, the lowest being its empty cell or space; measured density sorts a ramp only when asked, never moving its levels; a ramp's name and id live with whatever keeps it; and density is measured as the canvas draws each glyph, fallback fonts and fitted block characters included. *Decided in 3.3 (ADR 0049):* a glyph that isn't in the ramp counts as the level whose measured density is nearest, so the Brush can blend over any text. *Decided in 3.5 (ADR 0052):* the user's ramps live in a `RampLibrary` with stable ids and unique names; built-in ramps are read-only and are copied to be edited. The Ramps window edits a ramp live, sorts it and generates one in the active document's font, and imports and exports `.asciiramp` files of one ramp each. *Decided in 4.7 (ADR 0068):* `IGlyphDensityProvider.GetShape` gives a `GlyphShape`, the cell cut into 4 × 8 equal parts and how much of each the ink covers. `RampShapeMatcher` picks the ramp level whose glyph best shows a target of 32 part intensities: near the target's average in tone, and with its ink's unevenness, scaled down as far as fits, matching the target's, both blurred a little as the eye blurs thin strokes. An even target gets the level nearest its intensity. *Decided in 4.10b (ADR 0075):* `RampShapeMatcher` can also judge a cell with the eight around it, comparing only where each cell's ink lies across the cell and the half of each neighbour nearest it, and says whether its glyph clearly shows the target's edge.

## 8. Tools

### 8.1 Shape of a tool

Tools are **Core** classes with no UI dependencies. Each tool has:

- **A descriptor:** a stable string id (`"askiiart.brush"`), a resource key for its localized name, an icon key and a category.
- **A group:** every tool belongs to one `ToolGroup`, which has the shortcut letter and its tools in order. The Tools toolbar has a button per group, which brings back the group's last tool, and a group with several tools shows them on a sub-toolbar, where keys 1–9 choose them. The letter and number keys work while the canvas has focus and the active tool isn't taking text (ADRs 0033, 0049). *Decided in 4.1 (ADR 0055):* the shape selection tools are one group on M and the Magic Wand is W. *Decided in 4.5 (ADR 0066):* the Move tool is on V, and the Hand tool has no letter: Esc picks it, and it's the tool at launch. *Decided in 4.3 (ADR 0063):* the Magic Wand is the Select group's fifth tool, and a group can give a tool a letter of its own, so W picks it directly. *Decided in 5.2 (ADR 0079):* the shape tools are one group on U, with Line on 1. *Decided in 5.3 (ADR 0080):* Rectangle and Ellipse follow it, on 2 and 3 until Curve comes between them and Line.
- **Options:** data, not controls. A tool's definition may carry `ToolOptionGroup`s (numbers, choices, characters and toggles, each with an id and a label), and `ToolSettings` holds the values for each group, so they outlive the tool, tools can share them, and they can be exported. The Tool Options toolbar is drawn from the chosen tool's groups, so the tool doesn't know what draws its options. A definition can also give a warning for the toolbar to show when the tool can't work in the document, and say what choosing a character in the Characters window does while the tool is active (ADR 0079). A choice can take more choices from a source that `ToolSettings` registers, which is how the Ramp option lists the user's ramps (ADRs 0047, 0049, 0052).
- **Input handling:** the tool receives Core input events. These are pointer down, move and up events in **cell coordinates plus sub-cell position**, with modifiers, **pressure** (a pen's, from 0 to 1; a mouse or touch presses fully), the click count, how far the tool's markers reach and how wide a cell is on screen at the current zoom, and key commands (move left, jump right, row end, …) for text mode. The App translates Avalonia events into these; a per-platform keymap turns keys into commands, following each platform's conventions (ADR 0017). *Decided in 3.7 (ADR 0054):* pressure scales how strongly the Brush and the Soft Eraser cover a cell, not their intensity, so a light touch never erases; the other tools ignore it.
- **One gesture, one undo step:** during a gesture, a tool can draw a preview overlay that isn't part of the document and emit one command when it ends. Or, where the result has to show as it happens, it can apply small commands that coalesce into one step, as the eraser does (ADR 0033). Or it can draw the result into the document as the gesture goes, each change taking back its own last step and executing a new one, so the preview is exactly the result and the gesture ends as one step, as the Line tool does (ADR 0079). Pressing Esc cancels the gesture and leaves no trace.
- **Constraints:** every tool respects the active selection (§10) and the active layer's lock.
- **Space:** each tool says whether Space + drag pans the canvas while it's active. Text mode doesn't, so Space types a space (ADR 0017). The Hand tool also pans with a plain left-drag.
- **Esc:** a tool uses Esc only to cancel what it's doing. Any other Esc on the canvas switches to the Hand tool, so a tool is always chosen (ADR 0041).

Tools edit through an **editing session**: the document, its undo history and the active layer. New replaces the window's session, and everything that edits the document follows the active one (ADR 0020). Tools and their groups are registered through DI in a `ToolRegistry`, and tools are created per session from a `ToolContext`: the session, the FG and BG colours, which every document shares (ADR 0042), the tool settings, the shape of the document's cells, which a brush that looks round needs (ADR 0047), and the glyph densities in the document's font, which a ramp needs (ADR 0049). Built-in tools register exactly the way a future plugin would (§18).

### 8.2 Planned tools

Planned tools, built over several milestones: text, brush, eraser, fill, eyedropper, line, curve, rectangle (rounded or not), ellipse, table, gradient, line brush, the selection tools (§10), and move. Each tool's detailed behavior is specified with the owner when its milestone starts.

### 8.3 Erasers

The Eraser group (ADR 0050) has three erasers, which share one size and shape (ADR 0047), separate from the brushes'.

- **Eraser** (step 2.4): everything inside the footprint becomes empty, and everything outside it is untouched.
- **Soft Eraser** (step 3.4): lightens text along the ramp it shares with the Brush (§8.4). Each glyph moves lighter by the strength times the tip's weight there, and at least one level, once per stroke. Where the tip covers only part of a cell, the cell keeps the glyph whose ink lies away from the tip (step 4.7, ADR 0068), judged with the cells around it as the Brush's are (step 4.10b, ADR 0075). One lightened to nothing goes. Glyphs keep their colours, and backgrounds stay. A glyph that isn't in the ramp counts as the level that measures nearest.
- **BG Eraser** (step 3.4): takes backgrounds away, and leaves glyphs and their colours. In a monochrome document it erases nothing, and Tool Options says so.

A soft background eraser was parked on 2026-11-24, with M8's idea for erasing text and backgrounds separately (§23). If alpha ever becomes real, an eraser could lower alpha instead. That one is reserved; don't build it.

### 8.4 Brushes

The Brush group (ADR 0049) has four brushes, which share one size and shape (ADR 0047). Alt+click (Option+click) picks colour up from what the canvas shows, so there's no separate eyedropper tool for colour.

- **Brush** paints a ramp's characters (§7, ADR 0045), along the ramp it shares with the Soft Eraser (§8.3). Its middle paints at the chosen intensity, and its edge fades by the chosen softness. It blends: each cell moves towards the intensity by how strongly the brush covers it, once per stroke. Where the tip covers only part of a cell, hard edges included, the cell gets the ramp glyph whose ink lies where the tip painted (PATS, step 4.7, ADR 0068): each part of the cell moves by its own weight, and `RampShapeMatcher` (§7) picks the level. Edge cells near the tip are picked again with the cells around it, art already on the layer included, so they may change as the stroke grows, though only cells the stroke paints change. A cell whose glyph wouldn't clearly show its edge moves as a whole by the tip's weight at its middle (step 4.10b, ADR 0075). It writes the FG and BG colours on every glyph it reaches, and Alt+click picks both. A ramp the document's character set can't hold paints nothing.
- **FG Brush and BG Brush** (step 2b.3b) paint colour onto cells that are already there: text colour or background. Choosing one chooses the colour the colour windows change, and choosing the other colour while one of them is active switches to the other brush (ADRs 0042, 0043).
- **Character Brush** (step 3.6, ADR 0053) puts one chosen character in every cell under the tip, hard-edged, in the FG and BG colours as typing gives them. Alt+click picks the character and both colours. The character is chosen in the Characters window, which lists Recent (the 16 characters painted with last), the user's character sets and built-in Unicode blocks. User sets are made from typed characters, edited live, and imported and exported as `.asciicharset` files, as ramps are (§7). A character the document's character set can't hold paints nothing. *Decided in 5.2 (ADR 0079):* the character is shared with the Line tool's Single character style (§8.5), and choosing one in the Characters window keeps the Line tool rather than switching to the Character Brush.

### 8.5 Lines and shapes

M5's lines and shapes follow a `LinePath`, straight pieces in canvas coordinates; rectangles and ellipses run through the middles of their outermost cells. *Decided in 5.1 (ADR 0078):* a path takes one cell to a column where it runs more across than down and one to a row elsewhere, unbroken, each once, and a turn sharper than 45° on screen makes a corner. A Style chooses each cell's character (`LineGlyphPicker`): **Standard** by the path's direction on screen (`- | \ /`, with `+` at corners), **Precise** by the candidate glyph whose measured ink (§7) lies most like a thin stroke along the path through the cell, centred across where the path is steep, and **Single character**. Every style takes the same cells. *Decided in 5.2 (ADR 0079):* **Standard by step**, which goes by each cell's step from the cell before to the cell after, stays beside Standard as a style of its own. The Style is one option that the Line, Ellipse and Curve tools share. The **Line tool** draws from the middle of the cell it starts in to the middle of the cell under the pointer, as real cells while it's dragged, kept to a direction by the Move tool's constraints, and in Single character it draws the Character Brush's character. *Decided in 5.3 (ADR 0080):* the **Rectangle** and **Ellipse** tools draw in the box a drag makes, Shift making it look square and Alt growing it from the first cell, as the box-shaped selection tools do (`DragBox`). A rectangle's corners can be rounded with `.` and `'`. A **Fill** of the Character Brush's character or the background colour goes in every cell the outline closes in (`ShapeInterior`). The three tools share how they draw live (`LiveShape`, `ShapeCells`). *Decided in 5.4 (ADR 0081):* **Box**, **Heavy box** and **Double box** styles draw in box-drawing characters by direction, with corners such as `┌`, and in an ASCII-only document they draw as Standard. **Include joins**, a shared option off by default, makes lines join the line characters already on the active layer (`LineJoins`): a cell keeps the old character's arms and takes the new line's at its weight, so a line across `│` makes `┼` and one ending on it `┤`. Lines in `- | +` join as `+`. *Decided in 5.4b (ADR 0082):* only the Rectangle has the box styles, in a Style of its own, and only the Line and the Rectangle have Include joins. A rectangle filled with a character covers the old lines inside it, so they stop at its outline rather than crossing it.

## 9. Commands and undo

- Undo uses the command pattern. Each command can apply and revert itself, and it stores **only what it touched**. For cell edits, that's a compact list of (layer id, cell index, before, after).
- **One gesture = one command:** a brush stroke, a fill, a shape.
- **Coalescing:** consecutive typing in text mode merges into one command. The merge ends when the caret jumps, the tool changes, a pause threshold passes, or the user does anything else. Repeated nudges coalesce the same way.
- **Composite commands** group several commands into one undo step (for example, a paste = write cells + select them).
- History has a **memory budget** that's configurable, with a default such as 256 MB. When the budget is exceeded, the oldest entries are dropped. There is no fixed limit on the number of steps.
- The document's **dirty state** comes from the steps between the history position and the last-saved position: it's unsaved when one of them changes the document. Undoing back to the saved state clears "unsaved changes".
- Each command names its step with a key the app turns into text, and the History panel can jump to any step using ordinary undo and redo (ADR 0028). Which layer is active belongs to the editing session, and isn't saved or undone (ADR 0030).
- *Decided in 4.1 (ADR 0055):* selection changes are undo steps, named in History, but a step that changes only the selection doesn't make the document unsaved.

## 10. Selection

- A selection is a **mask**: an arbitrary set of cells plus a cached bounding rect. It isn't limited to rectangles. *Decided in 4.1 (ADR 0055):* the mask is immutable and stores bits only for its bounding rect, or none when it's a rectangle, so a selection costs little on a huge canvas and undo steps can share masks.
- The selection lives in the **editing session**, not in the document, and it isn't saved to files. Resizing the canvas moves it with the art and drops the cells that are cropped, in the same undo step (ADR 0055).
- Creation tools: rectangle, ellipse, lasso (freehand), polygonal lasso, and magic wand. The magic wand can work contiguously or globally, matches by glyph, lightness, text colour or background colour, and samples either the active layer or all visible layers. Other selection operations: select all, select none, invert, grow, shrink, select the cells with glyphs, and select the cells with a background colour.
- *Decided in 4.3 (ADR 0063):* the wand joins cells along their sides only, and compares lightness and colours within a tolerance, which dragging sideways during a click also changes. Grow and Shrink go one cell, corners included, and Shrink keeps cells along the canvas's edges.
- Combine modes: replace, add, subtract and intersect, chosen with modifier keys or a tool option. *Decided in 4.2 (ADR 0060):* Shift, Alt or both held at the press pick add, subtract or intersect for that gesture; otherwise a Mode option shared by the selection tools does. Pressed during a drag, Shift makes the box look square and Alt draws it from the middle. A cell is in a shape when its centre is, and a lasso fills every loop it goes round.
- When a selection exists, every drawing tool clips to it. *Decided in 4.4 (ADR 0065):* tools get their cell edits from the session, which leaves out cells outside the selection, so no tool checks it itself. The Text tool's caret still moves over unselected cells without writing them. Delete, from the Edit menu or the Delete and Backspace keys on the canvas while no Text caret shows, empties the active layer's selected cells, characters, colours and backgrounds alike, and keeps the selection.
- Selected content can be moved, copied, cut and pasted. Paste creates a **floating** selection that commits as a command. *Decided in 4.5 (ADR 0066):* the Move tool moves the active layer's selected cells, or the whole layer when nothing is selected, by dragging from anywhere or with the arrow keys (Shift for 10 cells), and Alt at the press copies them. The document always shows the move: each new place takes back the last one's undo step and makes a new one from the cells as they were, so they pass over other art without harming it. Empty cells don't cover anything, cells off the canvas are lost when the move ends (since 4.9 the selection stays there), and the move ends at Enter or anything else that changes the history, the active layer or the tool. The Move tool is the one tool that writes outside the selection. *Decided in 4.6 (ADR 0067):* Copy takes the active layer's selected cells, putting them on the clipboard as plain text and in the app's own format, which keeps their colours, shape and place; Cut also empties them, as one step. Paste floats the clipboard's cells as a Move-tool move, selected, centred on the pointer, or where they were copied from when the pointer isn't over the canvas or too little would land, as a Paste step. Characters the document can't hold leave their cells empty. At the Text tool's caret, Paste types the clipboard's text in instead. *Decided in 4.8 (ADR 0069):* a Move-tool click that stays in an unselected cell ends the move and deselects, as Rectangle Select's does. During a drag, Shift keeps the move to an axis, Ctrl (Cmd on macOS) to the diagonals that look like 45°, and Ctrl+Shift to the diagonals of one column per row, chosen as the drag looks on screen and following the keys at once. Every move sets the selection, so moving the whole layer selects it. *Decided with 4.8's follow-ups (ADR 0070):* each drag is its own Move step, and so is each run of arrow presses. A run ends when a press comes 0.4 s or more after the last arrow was let go, or turns after three or more presses one way, so pressing two arrows in turn moves diagonally in one step. Each Shift+arrow is a step of its own. Undos and redos of the move's own steps keep its cells floating; any other change ends the move. Enter ends the move and deselects. Esc puts back only a drag under way; otherwise it picks the Hand tool and the cells stay where they are. *Decided in 4.9 (ADR 0071):* a move can take the selection into a zone past the canvas, 10 columns wide left and right and as far on screen above and below, and arrows and drags stop at the zone's outer edge, a drag sliding along it. Only moves make such a selection (`SelectionMask.MovedWithin`); every other way of making one still crops to the canvas. Past the canvas only the outline shows, at half strength, and it stays where a move leaves it. Selection tools and commands work on the canvas part only (`CutToCanvas`), so in any mode they drop the rest, which shows at 15% while a selection tool is in use. A selection all past the canvas still counts: the whole canvas is hatched, drawing tools paint nothing, Move moves it, and Copy, Cut, Delete, Grow, Shrink and Copy as Text or ANSI are greyed out. *Decided in 4.9b (ADRs 0072, 0073):* the zone is a ring the selection must still touch rather than a wall: a move goes as far as the selection's bounding box still shares an edge or a corner with it, so everything, a whole layer too, can go off the canvas. The arrows move the selected cells in every selection tool too, as the Move tool's do, through the same `CellMover`, while nothing is being drawn; with nothing selected they do nothing, and Enter ends a move they made and deselects.
- How the selection looks (marching ants or similar) is the owner's design call. Until then it's a dashed black-and-white outline whose dashes run clockwise round the selection, with diagonal stripes of dark and light side by side over every cell that isn't selected, moving to the top left (ADR 0064, `docs/TEMPORARY-UI.md`).
- *Decided in 4.2b (ADR 0061):* while a selection tool is used, the canvas previews what letting go would leave. Lines that would stay run brightly, the shape's other lines show faintly, and the old selection's fainter still. Subtract runs a red outline round the shape and hatches the cells it would deselect in red, the other way; Intersect runs a blue outline round what stays and hatches what it would deselect in blue. During those two, only their coloured hatching moves. The hatching over what isn't selected stays while a gesture would leave nothing, and the grid shows less under hatching. A shape with no cells, such as one drawn off the canvas, combines like any other; only a click leaves the selection alone outside Replace. The lassos show faintly how they'd close, and the Polygonal Lasso's corners have markers that keep their size on screen; a click on the first corner's marker finishes the shape.

## 11. Fonts and glyph rasterization (Imaging)

- **Bundled fonts:** JetBrains Mono (the default), Roboto Mono, IBM Plex Mono and Fira Code, Regular weight only, all OFL-1.1. Each one ships with its license file and is listed in THIRD-PARTY-NOTICES.md (ADR 0013).
- **User fonts:** any TTF or OTF file the user picks, or any installed system font. Fonts loaded from a file are remembered by path between runs, in the user font list in settings (§15, ADR 0034).
  - **Cell size comes from font metrics:** the advance width of a reference glyph (`0`) × the line height (ascent + descent + line gap), read from the font file so it's the same on every OS (ADR 0013). It is never hardcoded.
  - **Monospace check:** measure the advance of a sample of printable ASCII glyphs. If they differ, warn the user but still allow the font; the grid uses the reference advance.
  - **Damaged files:** a file that isn't a readable font, or whose name, glyphs or cell size are broken, is rejected with a message and never crashes the app (ADR 0019).
  - **Missing glyphs:** drawn with an installed font that has them, or as a placeholder box if none does; a fallback glyph too big for the cell is shrunk to fit. Either way the cell is flagged as an error (ADR 0013); how the flag looks is still open (§23). It must never crash.
- **Too-wide glyphs:** a glyph from the document's font that reaches well past its cell (like `↔` in some fonts) is squeezed sideways to fit; box-drawing and block characters are instead fitted to fill the cell exactly, with their lines on whole pixels, so they join their neighbours without seams (ADR 0039).
- **Ligatures can't happen,** because each cell is rasterized as a single glyph and never shaped as a run of text.
- **Glyph atlas:** rasterized glyphs are cached per (font, pixel size, render options) and reused. Glyphs are rasterized at the actual device-pixel size for the current zoom and DPI so text stays crisp. A glyph bitmap is never scaled, except for the moment the view glides to a paste (ADR 0076).
- **One rasterizer** (Principle 4): the atlas, PNG export and glyph-density measurement all share the same Skia code path and settings.

## 12. Rendering (the App's canvas control)

- **One custom control** draws the whole canvas. There are no per-cell controls and no `TextBlock`s.
- **Virtualized:** it draws only the cells that intersect the viewport, so cost scales with the number of visible cells, not with document size.
- **Dirty regions:** only the regions that changed are re-composited from their layers (§5.9). Avalonia always repaints a control as a whole, so each frame redraws the visible cells from the cached composite and the glyph atlas, which is cheap. A cache of pre-drawn tiles gets added only if measurements show it's needed.
- **Draw order:** composite (paper, cell backgrounds, glyphs in their colours) → grid lines (toggleable, fainter under selection hatching) → selection hatching (left off the hovered cell) and outlines (or what a selection tool's gesture would leave) → hover highlight, caret and brush outline → the tool's line, its closing line and its markers. Overlays never touch the document.
- **Zoom:** discrete steps (25% to 800%) where the cell size lands on whole device pixels, so no seams appear between cells. Zoom centers on the cursor. Pan with the mouse wheel, middle-drag or Space-drag (when the tool allows it, §8.1). Scrollbars exist but are off for now (ADR 0035). Wheel events are grouped into gestures, so a touchpad's glide never turns into a zoom (ADR 0018). *Since 4.9b (ADR 0072):* the view scrolls over the canvas, the zone around it and any selection past that zone with as much room around it, plus a 64-pixel margin, and keeps the canvas centred while that fits. It keeps still while something may be moving (a gesture, an arrow held, and 0.4 s after), then glides back out of space nothing needs. *Since 4.11 and 4.11b (ADRs 0076, 0077):* a paste not fully in view, and the selection after a run of arrow presses that moved it, bring the view to their centre, at the current zoom if they fit there with the margin round them, else zoomed out to the largest step at which they do, or to 25%; it never zooms in. The view and the zoom go there at once and only the drawing glides, scaled and shifted by a `ViewTransform` each frame; meanwhile the zoom is locked (`IsZoomLocked`), and scrolling, panning or a gesture end the glide.
- **HiDPI:** rendering must be crisp under Windows display scaling (125%, 150%, …) and on Retina displays. Test both.
- **Target:** smooth zoom, pan and brush strokes on a 500 × 500 canvas (§21).
- **Drawing approach:** a custom draw operation paints through a direct Skia lease, one batched `DrawAtlas` call per atlas page. It measured 1.2–4× faster than drawing atlas bitmaps through Avalonia's `DrawingContext` (ADR 0015). Each glyph carries its colour in that call, and the visible cells' backgrounds are one image with a pixel per cell, stretched over them without smoothing (ADR 0038). Selection hatching is drawn the same way: a small tile of stripes, shifted by parts of a pixel as it moves (ADR 0062), shown through an image with a pixel per visible cell, in one draw, and the grid is drawn through another such image that dims it under the hatching (ADR 0061).

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
- **Colour** (format version 2, ADR 0037): the top level names the colour mode, and a layer with colour has one-line `foreground` and `background` runs. Version 1 files open as monochrome.
- **The active layer** is saved as an optional id, so a document reopens where it was left. It isn't content: choosing a layer doesn't mark the document unsaved, and an id that's no use is ignored (ADR 0031).
- **Round-trip guarantee:** load(save(doc)) == doc for both variants. This is tested (§19).

## 14. App shell and UI decoupling

### 14.1 MVVM

Views are thin: AXAML plus minimal code-behind, where code-behind is needed for things like the canvas. View models hold state and call into Core. View models never reference Avalonia controls.

### 14.2 Actions

Every operation the user can invoke (Undo, Save, New Layer, Select Brush, Zoom In, …) is an **action**. An action has a stable id, a localized label key, an icon key, default shortcut(s), an enabled/checked state, and an execute method. Menus, toolbars, context menus, shortcuts and (later) a command palette all bind to **action ids**. That's what lets toolbars be rearranged and shortcuts be rebound without touching any logic.

Actions are different from document *commands* (§9): a command is an undoable change to the document, and an action may create one.

### 14.3 Shortcuts

Shortcuts are defined per action and are platform-aware: `Ctrl` on Windows and Linux corresponds to `Cmd` on macOS. Use Avalonia's platform hotkey configuration and never hardcode `Ctrl`. User rebinding comes later and is stored in settings. A shortcut that text boxes also edit with (Select All, Undo, Redo, Copy, Cut, Paste) goes to a text box that has the keyboard instead, from the key and from the menu (ADR 0057).

Menus are Avalonia native menus built from a menu layout of action ids: the system menu bar on macOS (with About in the application menu), and on Windows and Linux a menu bar in a plain strip along the top of the window (ADR 0007, ADR 0035).

### 14.4 Panels and docking

Each panel (layers, tool options, ramps, character picker, history, …) is a self-contained view and view model that doesn't know where it's hosted. The user calls these **windows**, and the code calls them panels, because `Window` is an Avalonia class. The shell has its own docking system (ADR 0035), and `docs/DOCKING.md` is its spec. Steps 2.5c to 2.5f built toolbars, widgets, floating windows and docks, with dragging and resizing, and 2.5g finishes the spec with the opening rules and polish.

The canvas is the main character. It paints everything between the menu bar and the status bar, which are plain strips, and `DockingHost` lies on top with no background: art shows around windows and toolbars and through them, and they always get input first. They still take up space: centering, zoom and scroll limits work within the **safe area** they leave free. The window has a minimum size, lowered to fit small screens.

The layout is plain data (`DockingLayout`), saved in settings. `LayoutSolver` turns it and the current sizes into rectangles, and `LayoutEdits` turns one layout into the next. All of that is in `AskiiArt.Docking`, with no UI, so its rules are tested without one. Windows and toolbars are registered by id (`ShellCatalog`).

### 14.5 Theming, icons and text

- **Design tokens:** colors, spacing, corner radii and font sizes are defined once, in a theme resource dictionary. Placeholder UI uses only these tokens, never literal colors or sizes, so the owner's design system can replace them. Light and dark themes come from swapping token sets.
- **Icons:** referenced by icon key through a single lookup. Placeholder icons can be simple shapes or text glyphs. The owner's icon set replaces the files, not the code.
- **User-visible strings:** every one lives in resource files (`.resx`) from day one and is accessed through strongly typed keys. There are no literal UI strings in AXAML or C#.

### 14.6 Placeholder UI policy

Claude Code may build any temporary UI it needs to exercise a feature. Keep it plain and functional, build it only from tokens, resource strings and actions, and make it obviously temporary. Don't spend effort polishing UI the owner will replace. Every placeholder choice is listed in `docs/TEMPORARY-UI.md`, the owner's checklist when designing. When the owner delivers a design, integrating it is its own roadmap step.

## 15. Settings and user data

- Settings are stored as versioned JSON in the platform's app-data folder (`%APPDATA%` on Windows, `~/Library/Application Support` on macOS, `$XDG_CONFIG_HOME` or `~/.config` on Linux). Resolve the folder with `Environment.GetFolderPath`; never hardcode paths (ADR 0026).
- Settings hold preferences, ramps and brush presets, the dock layout, shortcut bindings, recent files and the user font list. *Since 3.5 (ADR 0052):* the user's ramps are saved whenever they change, as are the user's character sets and the recent characters since 3.6 (ADR 0053). The tool options, each tool group's last tool and the FG and BG colours are saved when the window closes, as the layout is, and anything saved that no longer applies is ignored. *Since 4.9b (ADR 0072):* `"reduceMotion": "true"` stills all of the app's motion from the next start, by setting every motion token to zero; it has no UI until there's a Preferences window.
- Corrupt settings never stop the app from starting. Back up the bad file, load defaults, and tell the user.

## 16. Import and export

Importers and exporters implement the `IImporter` and `IExporter` interfaces, which are defined in Core; implementations live in Core or Imaging as needed. They register through DI, the same way tools do.

| Format | Direction | Milestone |
|---|---|---|
| `.asciiart` | both | M1 |
| Clipboard (plain text, flattened) | both | copy in M1, paste in 4.6 |
| Clipboard (the app's own cells, ADR 0067) | both | 4.6 |
| `.txt` | both | export in M1, import later |
| PNG (rendered with the document font; scale, padding and color options) | export | M7 |
| HTML (`<pre>`) | export | M7 |
| Image → ASCII | import | M6 |
| ANSI (`.ans`, clipboard) | both | export in M2b, import in M7 |

Text export always flattens the visible layers and writes `\n` line endings by default (Copy as Text and Copy as ANSI copy only the selection's bounding rectangle while anything is selected), with an option for `\r\n` and an option to trim trailing spaces.

Ramps are imported and exported from the Ramps window as `.asciiramp` files, versioned JSON holding one ramp each (ADR 0052). Character sets are imported and exported from the Characters window as `.asciicharset` files, holding one set each in the same way (ADR 0053).

## 17. Image → ASCII (advanced)

Here quality matters more than speed. This gets its own milestone, and the details are designed with the owner then. The expected shape:

- Resample the image to the canvas grid using the **current font's cell aspect ratio**, since cells aren't square.
- Preprocess: grayscale, levels and contrast, optional edge enhancement, invert.
- **Structural matching:** for each cell-sized tile, pick the glyph from the allowed set (a ramp or a charset) whose rasterized shape best matches the tile. Compare against glyph bitmaps from the atlas, using an SSIM-like metric or gradient and edge-orientation features, rather than only average brightness. Brightness-only ramp mapping is kept as a fast mode. *Decided in 4.7 (ADR 0068):* the Brush already matches cells to a ramp's glyph shapes with `GlyphShape` and `RampShapeMatcher` (§7), which conversion can reuse with targets taken from the image, or replace with a finer metric.
- Optional dithering for the tone-mapped mode.
- A live preview with adjustable parameters. Conversion runs off the UI thread, and the result is applied as one command (on a new layer by default).
- Snapshot tests against a small set of fixed reference images.

## 18. Reserved features (hooks only, no design)

Colour is built in M2b (ADR 0036). Plugins have a milestone (M9, ADRs 0032 and 0046) and stay a hook until it starts. The owner doesn't plan opacity, blend modes or text attributes for the base app.

| Feature | Hook that exists now | Not built |
|---|---|---|
| Animation | The document holds a list of frames (exactly one); everything works on the active frame | Frame UI, timeline, playback, export |
| Text attributes | `Attributes` on Cell, always none; for plugins only (ADR 0036) | Bold, underline and the rest, in the model, the file format and the renderer |
| Per-cell alpha | `Alpha` on Cell | Alpha compositing, alpha eraser |
| Layer opacity / blend modes | Fields on Layer; compositing goes through one policy object | Any mode other than normal |
| Plugins (M9) | Tools, importers, exporters and actions use interfaces and DI registries | Loading external assemblies, sandboxing, plugin API versioning |
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

**Manual tests** (the owner runs these): every roadmap step produces `docs/testing/step-<id>-<slug>.md`, based on `docs/testing/TEMPLATE.md`. When a milestone is done, its test docs move to `docs/testing/old/milestone_<id>/` (after releases begin, `old/version_<name>/`).

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
| How the canvas marks a cell whose glyph is missing from the document's font. *Decided in 1.3:* draw it with a fallback font and flag the cell as an error. *Still open:* what the flag looks like and how the user is warned. *The owner's lean (after 3.1):* a character borrowed from another font, such as braille, which no bundled font has, isn't flagged on the canvas but is warned about some other way, since it may look different on another computer or after export | M8 (UI Pass) |
| Charset policy. *Decided in 1.5:* per document, Unicode or ASCII only (§6, ADR 0016). *Decided in 1.6:* new documents default to ASCII only, chosen in the New dialog. *Still open:* the document options that switch it later | The owner's UI design |
| Should selection changes count as undo steps? *Decided in 4.1:* yes, but they don't make the document unsaved (ADR 0055) | M4 |
| Default ramp(s) to ship. *Decided in M3's planning:* Standard, Standard Alphanumerical, Fine, Blocks and Braille, each painting an empty cell at intensity 0 (ADR 0045) | M3 |
| What a soft background eraser does, since a background colour has no intensity to lower. 3.4 built soft and hard text erasers and a hard background eraser (ADR 0050). *Parked on 2026-11-24:* not built in M5; looked at again with M8's idea for erasing text and backgrounds separately | M8 |
| Exact behavior of each tool | That tool's milestone |
| Big-text font format (FIGlet?), and which fonts can legally be bundled. *Left out of M5 on 2026-11-24:* text from online generators already pastes in, and too few FIGlet fonts are clearly licensed to bundle; parked as a plugin idea | M9 |
| How plugins are written (.NET assemblies, which run with the user's full permissions, or a scripting language), what they can add, and how they're licensed (the GPL likely covers an in-process plugin unless the project grants an exception, which is easiest before accepting outside code) | M9; the license before accepting outside code |
| How to flag wide characters visually | When Unicode input is allowed |
| When the owner's UI design will arrive, and in what form (Figma, AXAML, screenshots) | Any time |

## 24. Glossary

- **ADR (Architecture Decision Record):** a short file in `docs/decisions/` that records one decision, why it was made, and the alternatives considered.
- **Action:** an operation the user can invoke, bound to menus, toolbars and shortcuts (§14.2).
- **Command:** an undoable change to the document (§9).
- **Compositing / flattening:** combining the visible layers into one grid (§5.6).
- **Dock:** opaque windows that touch the edge of the app window and each other, with tabs (§14.4, `docs/DOCKING.md`). Not the macOS Dock, or the library this project used to use.
- **Glyph atlas:** a cache of pre-rendered character images (§11).
- **Intensity:** a 0–1 value for how much ink a cell should have (§7).
- **Occupancy:** whether a cell has a glyph (§5.4). A cell without one can still hold a background; a cell holding neither is **empty**.
- **Ramp:** a mapping from intensity to glyphs, for example `" .:-=+*#%@"` (§7).
- **Round-trip:** saving and then loading a document; the result must equal the original.
- **Safe area:** what windows and toolbars leave free for centring the canvas (§14.4).
- **Snapshot test (golden file):** a test that compares output against a saved, human-approved expected result.
- **Widget:** a translucent window in a stack, with gaps around it (§14.4, `docs/DOCKING.md`).
- **Window:** a content panel such as Layers or History, as the user sees it (§14.4). The whole program is the *app window*.
