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

- [x] **0.1 Repo scaffold.** The solution and projects, shared build settings, code style, license and the ADR process. ADRs 0001–0004 · [test doc](testing/old/milestone_0/step-0.1-repo-scaffold.md)
- [x] **0.2 CI.** Build, test and format check on Windows, macOS and Ubuntu. ADR 0005 · [test doc](testing/old/milestone_0/step-0.2-ci.md)
- [x] **0.3 App skeleton.** DI, file logging, string resources, theme tokens, the action registry, platform shortcuts and the error handler. ADRs 0006–0009 · [test doc](testing/old/milestone_0/step-0.3-app-skeleton.md)

## M1: MVP: type, undo, save (done 2026-11-01)

- [x] **1.1 Document model.** Documents, frames, layers and cells, compositing, change events, and resizing the canvas with an anchor. ADRs 0010, 0011 · [test doc](testing/old/milestone_1/step-1.1-document-model.md)
- [x] **1.2 Commands and undo.** Cell edits, composite commands, coalescing, history with a memory budget, dirty state and the layer commands. ADR 0012 · [test doc](testing/old/milestone_1/step-1.2-commands-and-undo.md)
- [x] **1.3 Fonts and glyph atlas.** Bundled fonts, cell size from font metrics, the monospace check, the rasterizer and atlas, and missing glyphs. ADRs 0013, 0014 · [test doc](testing/old/milestone_1/step-1.3-fonts-and-glyph-atlas.md)
- [x] **1.4 Canvas control.** Virtualized drawing, zoom, pan, the grid, hover, the status bar and HiDPI. ADR 0015 · [test doc](testing/old/milestone_1/step-1.4-canvas-control.md)
- [x] **1.5 Text mode.** The text tool, caret keys, typing coalesced into undo steps, and a character set per document. ADRs 0016–0018 · [test doc](testing/old/milestone_1/step-1.5-text-mode.md)
- [x] **1.6 Document actions.** New, Resize Canvas, the font picker, and placeholder menus and toolbar. ADRs 0019, 0020 · [test doc](testing/old/milestone_1/step-1.6-document-actions.md)
- [x] **1.7 Saving and opening.** `.asciiart` and `.asciiartz`, atomic saves, format migrations and unsaved-changes prompts. ADR 0021 · [test doc](testing/old/milestone_1/step-1.7-saving-and-opening.md)
- [x] **1.8 Output and macOS windows.** Copy as Text, Export .txt, and the app outliving its window on a Mac. ADRs 0022–0024 · [test doc](testing/old/milestone_1/step-1.8-output-and-macos-windows.md)

## M2: Shell and layers (done 2026-11-13)

- [x] **2.1 Dock shell.** The canvas under see-through windows, the safe area, the settings file, a saved layout and the Window menu. Its last checks moved into 2.5. ADRs 0025–0027, 0029 (all but 0026 since superseded) · [test doc](testing/old/milestone_2/step-2.1-dock-shell.md)
- [x] **2.2 History panel.** Named undo steps; clicking one jumps there. ADR 0028 · [test doc](testing/old/milestone_2/step-2.2-history-panel.md)
- [x] **2.3 Layers panel.** The active layer and every layer operation, with a Layer menu. ADRs 0030, 0031 · [test doc](testing/old/milestone_2/step-2.3-layers-panel.md)
- [x] **2.4 Space option and hard eraser.** Spaces as empty per layer, tool switching, and a 1-cell eraser. ADR 0033 · [test doc](testing/old/milestone_2/step-2.4-space-option-and-eraser.md)
- [x] **2.4b Remembered font files.** Fonts loaded from a file survive a restart. ADR 0034 · [test doc](testing/old/milestone_2/step-2.4b-remembered-fonts.md)
- **2.5 Docking pass.** Our own docking system replaced the Dock library. Spec: `docs/DOCKING.md`, ADR 0035.
  - [x] **2.5a Spec.** `docs/DOCKING.md` and this split. No code.
  - [x] **2.5b Replace Dock.** The `AskiiArt.Docking` project, the default layout, the Window menu, a saved layout, the safe area and click-through. [test doc](testing/old/milestone_2/step-2.5b-replace-dock.md)
  - [x] **2.5c Toolbars.** Floating, dragging with zones and a live preview, Horizontal and Vertical, fades and grips. [test doc](testing/old/milestone_2/step-2.5c-toolbars.md)
  - [x] **2.5d Sub-toolbars.** One at a time, with a Debug sample. [test doc](testing/old/milestone_2/step-2.5d-sub-toolbars.md), [follow-ups](testing/old/milestone_2/step-2.5d-hidden-toolbars.md)
  - [x] **2.5e Widgets and floating windows.** Stacks, title-only windows, hover buttons and menus, dragging, resizing, floating and the edge strip. [test doc](testing/old/milestone_2/step-2.5e-widgets-and-floating-windows.md), [follow-ups](testing/old/milestone_2/step-2.5e-window-follow-ups.md)
  - [x] **2.5f Docks.** Columns, tabs, and converting between docks and widgets. [test doc](testing/old/milestone_2/step-2.5f-docks.md)
  - [x] **2.5g Opening rules and polish.** Where new windows go, the drop marker, too-small windows, and 2.1's last checks. [test doc](testing/old/milestone_2/step-2.5g-opening-rules-and-polish.md), [follow-ups](testing/old/milestone_2/step-2.5g-follow-ups.md)

## M2b: Colour (done 2026-11-16)

Added after 2.3's testing (ADR 0032), before M3 so every tool from then on writes colour. Later milestones keep their numbers.

- [x] **2b.1 Colour model.** Text and background colours per cell, the colour mode and its conversion, backgrounds composited on their own, and file format version 2. ADRs 0036, 0037 · [test doc](testing/old/milestone_2b/step-2b.1-colour-model.md)
- [x] **2b.2 Colour on the canvas.** Each cell's colours drawn in the same batched glyph draw, a Debug colour sample, and box and block characters fitted to the cell so they join without seams. ADRs 0038, 0039 · [test doc](testing/old/milestone_2b/step-2b.2-colour-on-the-canvas.md)
- [x] **2b.2b Frame-rate readout.** Debug → Show Frame Rate: the average, the 1% low and the slowest draw at the right of the status bar. ADR 0040 · [test doc](testing/old/milestone_2b/step-2b.2b-frame-rate-readout.md)
- [x] **2b.3a Hand tool and colours.** A Hand tool (V) that Esc switches to, the New dialog's colour choice, colour modes in the Document menu, the FG and BG colours (X swaps them, D resets them), the text tool writing them, the Brush Options toolbar, and the Color and Swatches windows. ADRs 0041, 0042 · [test doc](testing/old/milestone_2b/step-2b.3a-hand-tool-and-colours.md), [follow-ups](testing/old/milestone_2b/step-2b.3a-follow-ups.md)
- [x] **2b.3b Brush.** A colour Brush (B) with FG and BG brushes (1 and 2) on its sub-toolbar, and Alt+click (Option+click) to pick a colour up from the canvas. ADR 0043 · [test doc](testing/old/milestone_2b/step-2b.3b-brush.md), [follow-ups](testing/old/milestone_2b/step-2b.3b-follow-ups.md)
- [x] **2b.4 ANSI export.** Export as ANSI and Copy as ANSI, with 16-colour or 24-bit escape codes, and Debug test samples sized for a terminal. ADR 0044 · [test doc](testing/old/milestone_2b/step-2b.4-ansi-export.md)

## M3: Ramps and brushes

Glyph density, ramps and their editor and files, a Brush that paints ramps, a Character Brush, the soft eraser, a Tool Options toolbar and a character picker. Steps agreed with the owner on 2026-11-16.

- [x] **3.1 Glyph density and ramps.** The ramp model, the five built-in ramps, and each character's darkness measured per font as the canvas draws it, with a Debug density sample. ADR 0045 · [test doc](testing/step-3.1-glyph-density-and-ramps.md)
- [x] **3.2 Brush sizes and shapes.** Size, square or circle, each either Perceptual (looks square or round on screen, so it's fewer cells tall than wide) or Mathematical (counts cells, so a 10 × 10 square is 10 cells each way), an outline under the pointer, and the Tool Options toolbar: a special toolbar built from the active tool's options as data, so the settings can be exported later. The colour Brush and the hard Eraser get sizes. Built: sizes 1–100 with `[` and `]`, the Tool Options toolbar, Brush Options renamed Colors, and the owner's new default layout. Fixed after testing: a toolbar now grows when its buttons do, and the slider no longer makes Tool Options tall. ADR 0047 · [test doc](testing/step-3.2-brush-sizes-and-shapes.md)
- [~] **3.3 The Brush paints ramps.** Tool groups replace the one-off Brush sub-toolbar and the Debug samples. The Brush group is Brush (ramps), FG and BG, on keys 1–3. Ramp, intensity and softness go on Tool Options, with Blend as the only blend mode for now. A ramp the document's character set can't hold doesn't paint. Built: tool groups with a button each and B bringing back the last brush, a soft blending Brush that writes both colours, the Ramp and Blend mode dropdowns (Replace and Darken greyed out), percentages and a warning on Tool Options. ADR 0049 · [test doc](testing/step-3.3-the-brush-paints-ramps.md)
- [ ] **3.4 Soft eraser.** Soft and hard erasers for text, and a hard eraser for backgrounds, as separate tools in an Eraser group. What a soft background eraser would do is left for M5 (ARCHITECTURE §23).
- [ ] **3.5 Ramp editor and ramp files.** A Ramps window to view, edit, preview, sort by darkness, generate, add, duplicate, rename and delete ramps, and to import and export them as files. Your ramps and brush settings are remembered between runs.
- [ ] **3.6 Character Brush and character picker.** A Character Brush that paints one chosen character, in the Brush group on key 4 (separate from the ramp Brush), and a Characters window that chooses it.
- [ ] **3.7 Pen pressure.** Pressure scales the Brush's intensity. Built and covered by automated tests; checking it on a real tablet waits for M10.

## M4: Selection

Add the selection mask and the rectangle, ellipse, lasso, polygonal lasso and magic wand tools. Support the replace, add, subtract and intersect combine modes, plus select all, select none, invert, grow and shrink. Drawing tools clip to the selection. Add cut, copy and paste with a floating selection, the move tool, and deleting the selection's contents.

## M5: Drawing tools

Line, curve, rectangle, rounded rectangle, ellipse, fill (flood fill, contiguous or global), pickup, table, and big text. Each tool's behavior is specified with the owner before it's built. Also a soft background eraser, once the owner decides what it does (ARCHITECTURE §23).

## M6: Image → ASCII

As described in ARCHITECTURE §17: an import dialog with a live preview, the structural matcher plus a fast ramp mode, dithering, and adjustable parameters. The result goes onto a new layer.

## M7: Export and import

PNG export (font, scale, padding, colors, and a transparent-background option), HTML export, text import, ANSI import, a classic `.ans` export (CP437 with a SAUCE record) for old-school ANSI art viewers, pasting text from the clipboard, and export options (line endings, trimming trailing spaces). PNG and HTML carry the document's colours (M2b).

## M8: UI Pass

Building the real UI with the owner and polishing the app until it looks professional (reshaped from Customization on 2026-11-16, ADR 0046). It opens with fixed steps for what has to exist, such as all the menus the app needs, then turns open-ended: the owner brings the UI and UX they've envisioned, one step at a time. At its start, go through the parked ideas in `docs/TEMPORARY-UI.md` with the owner. Already planned for it: how the user is warned about characters borrowed from another font (ARCHITECTURE §23), and a toolbar customizer: a window for searching tools and dragging them anywhere, adding to toolbars or making new ones. It replaces the earlier "layout builder" idea, since the docking pass already lets everything be placed anywhere. Also our own colour picker in place of Avalonia's, laid out to grow with its window and with control over each part (decided 2026-11-15), a tool palette, a shortcut-rebinding UI, a settings window, light/dark theme switching, layout presets, and frosted glass with a progressive blur for widgets, after a trial to see what they cost (`docs/DOCKING.md` §10).

## M9: Plugins

Added with M2b as M8b (ADR 0032), and given its own number in ADR 0046. Loading plugins, a versioned public API for tools, importers, exporters, actions and panels, a way to manage installed plugins, and a Discord ANSI message plugin built in as the API's first user. How plugins are written, what they're allowed to do and how they're licensed get decided with the owner first (ARCHITECTURE §23).

## M10: Ship-readiness

Autosave and crash recovery, recent files, checking pen pressure (3.7) on a real drawing tablet and a choice of what it controls, a notice when the open file is deleted or changed outside the app (ADR 0024), a file association for the document extension, the app icon, and an About / credits / licenses screen. Packaging for Windows (installer or zip), macOS (an unsigned `.app` bundle, with instructions in the README) and Linux (AppImage or tarball). A release workflow that runs when a tag is pushed.

---

## Parallel track: the owner's UI design

Whenever the owner delivers designs (layout, icons, text), Claude Code adds an integration step at the front of the queue.

**When the work turns mostly to UI design, remind the owner of the parked ideas in `docs/TEMPORARY-UI.md`,** the minimap in particular.

## Reserved, not planned

Per-cell alpha, text attributes (bold, underline and so on), layer opacity, blend modes, animation and wide characters. See ARCHITECTURE §18. The owner doesn't plan opacity or attributes for the base app; they and wide characters could come from plugins.
