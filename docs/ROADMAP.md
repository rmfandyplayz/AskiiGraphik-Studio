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

## M3: Ramps and brushes (done 2026-11-19)

Glyph density, ramps and their editor and files, a Brush that paints ramps, a Character Brush, the soft eraser, a Tool Options toolbar and a character picker. Steps agreed with the owner on 2026-11-16.

- [x] **3.1 Glyph density and ramps.** The ramp model, the five built-in ramps, and each character's darkness measured per font as the canvas draws it, with a Debug density sample. ADR 0045 · [test doc](testing/old/milestone_3/step-3.1-glyph-density-and-ramps.md)
- [x] **3.2 Brush sizes and shapes.** Sizes 1–100 with `[` and `]`, square or circle, Perceptual or Mathematical, an outline under the pointer, and the Tool Options toolbar built from the active tool's options as data, with floating toolbars growing from their nearest corner. ADRs 0047, 0048 · [test doc](testing/old/milestone_3/step-3.2-brush-sizes-and-shapes.md)
- [x] **3.3 The Brush paints ramps.** Tool groups, with the Brush group (Brush, FG, BG) on keys 1–3, and a soft blending Brush with Ramp, Intensity, Softness and Blend mode on Tool Options. ADR 0049 · [test doc](testing/old/milestone_3/step-3.3-the-brush-paints-ramps.md)
- [x] **3.4 Soft eraser.** The Eraser group (Eraser, Soft Eraser, BG Eraser on keys 1–3) sharing one tip, with a Soft Eraser that lightens along the Brush's ramp at least one level per stroke. ADR 0050 · [test doc](testing/old/milestone_3/step-3.4-soft-eraser.md)
- [x] **3.5 Ramp editor and ramp files.** Window → Ramps to view, edit, preview, sort, generate, add, duplicate, rename, delete, import and export `.asciiramp` files, and ramps, tool options and colours remembered between runs. Built with the .NET 11 SDK from here on. ADRs 0051, 0052 · [test doc](testing/old/milestone_3/step-3.5-ramps-window.md)
- [x] **3.6 Character Brush and character picker.** A hard-edged Character Brush on Brush key 4, and Window → Characters with Recent, the user's character sets (`.asciicharset` files) and Unicode blocks. ADR 0053 · [test doc](testing/old/milestone_3/step-3.6-character-brush.md)
- [x] **3.7 Pen pressure.** A pen's pressure scales how strongly the Brush and the Soft Eraser cover each cell, and Debug → Mouse Acts as a Pen at Half Pressure; a real tablet is checked in M10. ADR 0054 · [test doc](testing/old/milestone_3/step-3.7-pen-pressure.md)

## M4: Selection

Add the selection mask and the rectangle, ellipse, lasso, polygonal lasso and magic wand tools. Support the replace, add, subtract and intersect combine modes, plus select all, select none, invert, grow and shrink. Drawing tools clip to the selection. Add cut, copy and paste with a floating selection, the move tool, and deleting the selection's contents. Steps agreed with the owner on 2026-11-19; 4.2b and 4.2c added on 2026-11-20, 4.7 on 2026-11-21, and 4.8, 4.9 and 4.10 on 2026-11-22.

- [x] **4.1 Selection mask, Rectangle Select, Select All and Deselect.** The selection mask, Rectangle Select on M with a running dashed outline, Select All and Deselect, selection undo steps that don't make the document unsaved, the selection moving with a canvas resize, text-box shortcuts and undo, typing over a selection as one undo step, and the main window reopening where it was. ADRs 0055, 0057, 0058, 0059 · [test doc](testing/step-4.1-selection-mask.md) · [follow-ups](testing/step-4.1-follow-ups.md) · [follow-ups 2](testing/step-4.1-follow-ups-2.md)
- [x] **4.2 Ellipse, Lasso, Polygonal Lasso and combine modes.** The rest of the M group on keys 2–4, with replace, add, subtract and intersect picked by Shift, Alt or both at the press or by shared Mode buttons, and Shift and Alt pressed mid-drag making a square or drawing from the middle. ADR 0060 · [test doc](testing/step-4.2-selection-tools.md)
- [x] **4.2b Selection overlay.** Added after 4.2's testing. Faint two-tone hatching over what isn't selected, a live preview of what every selection tool would leave, a red Subtract and a blue Intersect with hatching over the cells they'd deselect, faint outlines that won't stay, a faint closing line on both lassos, and markers on the Polygonal Lasso's corners, the first finishing the shape when clicked. After the owner's visual check: the hatching moves, the grid shows less under it, the old selection's lines fade further, and a shape drawn off the canvas combines like any other. A window closed full screen reopens full screen. ADR 0061 · [test doc](testing/step-4.2b-selection-overlay.md)
- [x] **4.2c Smooth hatching.** Added after 4.2b's testing, for the hatching's flicker at the edge of vision. The hatching glides in eighth-pixel steps with lines twice as thick and far apart, every hatching moves at the dash speed, and an undo or redo during a selection tool's gesture shows its effect at once. An idle selection measured 1 to 1.5% CPU on the owner's i9-14900F. ADR 0062 · [test doc](testing/step-4.2c-smooth-hatching.md)
- [x] **4.3 Magic Wand and selection commands.** The Magic Wand on W and fifth in the M group (contiguous or global, the active layer or all visible layers, matching by glyph, lightness, text colour or background colour, with a tolerance a sideways drag changes), Invert, Grow, Shrink, Select Cells with Glyphs and Select Cells with Background Color. With it, from 4.2c's testing: zebra hatching and brighter Subtract and Intersect dashes. ADRs 0063, 0064 · [test doc](testing/step-4.3-magic-wand-and-selection-commands.md)
- [x] **4.4 Clipping and Delete.** Every tool that changes cells keeps to the selection, the Text tool's caret moving on over cells it doesn't write, and Edit → Delete and the Delete and Backspace keys (unless the Text caret shows) empty the selected cells and keep the selection. With it, from 4.3's testing: thinner, fainter hatching that leaves the hovered cell out. ADR 0065 · [test doc](testing/step-4.4-clipping-and-delete.md)
- [x] **4.5 Floating selection and the Move tool.** The Move tool on V moves the selected cells, or the whole layer, as one floating step across drags and arrow-key nudges, with Esc putting them back and Alt copying. The Hand tool has no letter (Esc picks it) and is the tool at launch. With it, from 4.4's testing: a maximized window moved to another monitor reopens there. ADR 0066 · [test doc](testing/step-4.5-move-tool.md)
- [x] **4.6 Cut, Copy and Paste.** Edit → Cut, Copy and Paste on Ctrl+X, C and V, as text for other apps and with colours for this one, a paste floating as a Move-tool move centred on the pointer or typed in at the Text caret, Copy as Text and Copy as ANSI copying just the selection, and the Move tool's cursor over what it would move. ADR 0067 · [test doc](testing/step-4.6-cut-copy-paste.md)
- [x] **4.7 The PATS brush (perceptually accurate text-based shapes).** The Brush and the Soft Eraser pick each edge cell's glyph by where the tip covers it, with glyphs measured in 4 × 8 parts and `RampShapeMatcher`, which M6 can share. The owner voided its tests on 2026-11-22 after a first look: faint, soft strokes kept backticks along their tops, with a gap below them. Step 4.10 revisits it. ADR 0068 · [test doc (voided)](testing/step-4.7-pats-brush.md)
- [~] **4.8 Move tool follow-ups.** Added after 4.6's testing. With the Move tool, a click that doesn't move to another cell on an unselected cell ends any move and deselects when the button comes up, as Rectangle Select's click does, while a drag from there still moves the selection. During a drag, Shift keeps the move to left and right or up and down (whichever the pointer has gone further), Ctrl (Cmd on a Mac) to those and diagonals that look like 45° on screen, and Ctrl+Shift to those and diagonals of one column per row, measured from where that drag began and changing the moment the keys do; Shift and an arrow still nudge 10 cells. Moving the whole layer selects all of it, as Select All would, when the cells first move, as part of the Move step. In the Layers window's rename box, arrow keys the box can't use do nothing instead of ending the rename. The owner checks clicks, drags with each key and renaming. *Ready for testing:* `MoveConstraint` picks the direction as the drag looks on screen, the canvas tells tools when Ctrl or Cmd goes down or up mid-drag, and every move sets the selection. ADR 0069 · [test doc](testing/step-4.8-move-tool-follow-ups.md)
- [ ] **4.9 Selections past the canvas edge.** Added after 4.6's testing. A move can take the selection past the canvas edge, into a zone 10 columns wide on the left and right and as far on screen above and below (5 rows when cells are twice as tall as wide); arrows and drags stop once the selection's nearest edge reaches the zone's outer edge, and a drag then slides along it. Off the canvas only the outline shows, at half strength, and it no longer closes along the canvas edge. The hatching shows whenever anything is selected. Every selection tool and command works on the canvas only, so in any mode it drops the part off the canvas; while one is in use, that part shows at 15% and the preview closes along the canvas edge. A selection all off the canvas still counts: the whole canvas is hatched, drawing tools paint nothing, Move moves it, and Copy, Cut and Delete are greyed out. When a move ends, the outline stays where it was left. The owner checks moving off each edge, the zone's limit, and each selection tool and mode on a selection partly off the canvas.
- [ ] **4.10 PATS review.** Added after 4.7's first look. Polish the PATS brush, and decide whether shaping edge cells suits the Brush and the Soft Eraser at all, or would serve M5's drawing tools better, where an outline's shape matters more than its lightness. May split into substeps. It starts with side-by-side renders of the same strokes at several intensities and softnesses, with 4.6's brush, 4.7's and a few variants, for the owner to judge by eye before anything is built. The owner checks the renders, then whatever is built.

## M5: Drawing tools

Line, curve, rectangle, rounded rectangle, ellipse, fill (flood fill, contiguous or global), pickup, table, and big text. Each tool's behavior is specified with the owner before it's built. Also a soft background eraser, once the owner decides what it does (ARCHITECTURE §23).

## M6: Image → ASCII

As described in ARCHITECTURE §17: an import dialog with a live preview, the structural matcher plus a fast ramp mode, dithering, and adjustable parameters. The result goes onto a new layer.

## M7: Export and import

PNG export (font, scale, padding, colors, and a transparent-background option), HTML export, text import, ANSI import, a classic `.ans` export (CP437 with a SAUCE record) for old-school ANSI art viewers, pasting text from the clipboard, and export options (line endings, trimming trailing spaces). PNG and HTML carry the document's colours (M2b).

## M8: UI Pass

Building the real UI with the owner and polishing the app until it looks professional (reshaped from Customization on 2026-11-16, ADR 0046). It opens with fixed steps for what has to exist, such as all the menus the app needs, then turns open-ended: the owner brings the UI and UX they've envisioned, one step at a time. At its start, go through the parked ideas in `docs/TEMPORARY-UI.md` with the owner. Already planned for it: how the user is warned about characters borrowed from another font (ARCHITECTURE §23), whether the Brush and the Soft Eraser round to the nearest ramp level or always move at least one (raised after 3.4's testing, `docs/TEMPORARY-UI.md`), and a toolbar customizer: a window for searching tools and dragging them anywhere, adding to toolbars or making new ones. It replaces the earlier "layout builder" idea, since the docking pass already lets everything be placed anywhere. Also our own colour picker in place of Avalonia's, laid out to grow with its window and with control over each part (decided 2026-11-15), a tool palette, a shortcut-rebinding UI, a settings window, light/dark theme switching, layout presets, and frosted glass with a progressive blur for widgets, after a trial to see what they cost (`docs/DOCKING.md` §10).

## M9: Plugins

Added with M2b as M8b (ADR 0032), and given its own number in ADR 0046. Loading plugins, a versioned public API for tools, importers, exporters, actions and panels, a way to manage installed plugins, and a Discord ANSI message plugin built in as the API's first user. How plugins are written, what they're allowed to do and how they're licensed get decided with the owner first (ARCHITECTURE §23).

## M10: Ship-readiness

Autosave and crash recovery, recent files, checking pen pressure (3.7) on a real drawing tablet and a choice of what it controls, a notice when the open file is deleted or changed outside the app (ADR 0024), a file association for the document extension, the app icon, and an About / credits / licenses screen. A profiling pass, where the owner profiles the app on both machines and sends the results (added on 2026-11-20, after 4.2's testing). One lead to start from (added on 2026-11-21, after 4.3's testing): on a busy 500 × 500 sample, a ragged Magic Wand selection of many small pieces kept the slowest frame at about 23.5 ms while idle, probably from walking and dashing every outline edge each frame (ADR 0055), while dragging the wand's tolerance drew in 12 to 15 ms at 3.5 to 4.5% CPU. Another (added on 2026-11-21, after 4.5's testing): dragging a whole, full 500 × 500 layer with the Move tool ran at about 40 fps, its slowest frames 18 to 20 ms, at 2 to 3% GPU and 5% CPU. On a 32-thread CPU, 5% is about one thread kept busy, so the drawing is held up by one thread doing the work rather than by the machine. Packaging for Windows (installer or zip), macOS (an unsigned `.app` bundle, with instructions in the README) and Linux (AppImage or tarball). A release workflow that runs when a tag is pushed.

---

## Parallel track: the owner's UI design

Whenever the owner delivers designs (layout, icons, text), Claude Code adds an integration step at the front of the queue.

**When the work turns mostly to UI design, remind the owner of the parked ideas in `docs/TEMPORARY-UI.md`,** the minimap in particular.

## Reserved, not planned

Per-cell alpha, text attributes (bold, underline and so on), layer opacity, blend modes, animation and wide characters. See ARCHITECTURE §18. The owner doesn't plan opacity or attributes for the base app; they and wide characters could come from plugins.
