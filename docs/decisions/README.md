<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Architecture decision records

How ADRs work (status values, superseding, small corrections) is described in [ADR 0001](0001-record-architecture-decisions.md). Only **Accepted** ADRs describe how the code works now.

| ADR | Title | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-test-stack-and-version-pins.md) | Test stack and version pins | Accepted |
| [0003](0003-slnx-solution-format.md) | Use the .slnx solution format | Accepted |
| [0004](0004-code-style-enforcement.md) | How code style is enforced | Accepted |
| [0005](0005-ci-workflow.md) | CI workflow | Accepted |
| [0006](0006-startup-di-and-logging.md) | Startup, dependency injection and logging | Accepted |
| [0007](0007-actions-menus-and-shortcuts.md) | Actions, menus and shortcuts | Accepted |
| [0008](0008-unexpected-errors.md) | Handling unexpected errors | Accepted |
| [0009](0009-ui-strings-and-theme-tokens.md) | UI strings and theme tokens | Accepted |
| [0010](0010-document-model-and-change-events.md) | Document model, mutation and change events | Accepted |
| [0011](0011-canvas-size-limits-and-resize-anchoring.md) | Canvas size limits and resize anchoring | Accepted |
| [0012](0012-commands-and-undo-history.md) | Commands and undo history | Accepted |
| [0013](0013-fonts-cell-metrics-and-missing-glyphs.md) | Fonts, cell metrics and missing glyphs | Accepted |
| [0014](0014-glyph-rasterizer-and-atlas.md) | Glyph rasterizer and atlas | Accepted |
| [0015](0015-canvas-rendering.md) | Canvas rendering | Accepted |
| [0016](0016-character-set-per-document.md) | A character set per document | Accepted |
| [0017](0017-tools-and-the-text-tool.md) | Tools and the text tool | Accepted |
| [0018](0018-wheel-gestures-and-scroll-momentum.md) | Wheel gestures and scroll momentum | Accepted |
| [0019](0019-document-fonts-and-the-font-catalog.md) | Document fonts and the font catalog | Accepted |
| [0020](0020-active-document-and-document-actions.md) | The active document and the document actions | Accepted |
| [0021](0021-document-files-and-unsaved-changes.md) | Document files and unsaved changes | Accepted |
| [0022](0022-text-output.md) | Text output: Copy as Text and Export as Text | Accepted |
| [0023](0023-macos-window-lifetime.md) | On macOS the app outlives its window | Accepted |
| [0024](0024-open-documents-are-not-locked.md) | Open documents are not locked | Accepted |
| [0025](0025-dock-as-a-see-through-layer.md) | Dock as a see-through layer over the canvas | Superseded by 0035 |
| [0026](0026-settings-file.md) | The settings file | Accepted |
| [0027](0027-top-bar-and-status-bar-outside-dock.md) | The top bar and status bar sit outside Dock | Superseded by 0029 |
| [0028](0028-named-undo-steps.md) | Named undo steps and the History panel | Accepted |
| [0029](0029-menu-strip-and-toolbar-panel.md) | The menu bar is a plain strip, and the toolbar is a Dock panel | Superseded by 0035 |
| [0030](0030-active-layer-and-layer-operations.md) | The active layer and layer operations | Accepted |
| [0031](0031-saved-active-layer.md) | A saved file remembers its active layer | Accepted |
| [0032](0032-colour-and-plugins-are-planned.md) | Colour and plugins are planned, not just reserved | Accepted |
| [0033](0033-space-option-tool-switching-and-eraser.md) | The space option, tool switching and the hard eraser | Accepted |
| [0034](0034-remembered-font-files.md) | Fonts loaded from a file are remembered by path | Accepted |
| [0035](0035-our-own-docking-system.md) | Our own docking system replaces the Dock library | Accepted |
| [0036](0036-the-colour-model.md) | The colour model | Accepted |
| [0037](0037-colour-in-the-file-format.md) | Colour in the file format (version 2) | Accepted |
| [0038](0038-drawing-cell-colours.md) | Drawing cell colours | Accepted |
| [0039](0039-box-drawing-and-block-glyphs-fill-the-cell.md) | Box-drawing and block glyphs fill the cell | Accepted |
| [0040](0040-debug-frame-rate-readout.md) | The Debug frame-rate readout | Accepted |
| [0041](0041-the-hand-tool-and-esc.md) | The Hand tool and Esc | Accepted |
| [0042](0042-the-current-colours.md) | The current colours | Accepted |
| [0043](0043-the-colour-brush.md) | The colour brush | Superseded by 0049 |
| [0044](0044-ansi-export.md) | ANSI export | Accepted |
| [0045](0045-ramps-and-glyph-density.md) | Ramps and glyph density | Accepted |
| [0046](0046-ui-pass-and-renumbered-milestones.md) | The UI Pass, with plugins and ship-readiness renumbered | Accepted |
| [0047](0047-tool-options-and-brush-sizes.md) | Tool options and brush sizes | Accepted |
| [0048](0048-floating-toolbars-keep-their-nearest-corner.md) | Floating toolbars keep their nearest corner | Accepted |
| [0049](0049-tool-groups-and-the-ramp-brush.md) | Tool groups and the ramp Brush | Accepted |
| [0050](0050-the-erasers.md) | The erasers | Accepted |
| [0051](0051-built-with-the-net-11-sdk.md) | Built with the .NET 11 SDK | Accepted |
| [0052](0052-user-ramps-and-remembered-tool-settings.md) | User ramps and remembered tool settings | Accepted |
| [0053](0053-the-character-brush-and-character-sets.md) | The Character Brush and character sets | Accepted |
| [0054](0054-pen-pressure.md) | Pen pressure | Accepted |
| [0055](0055-the-selection-mask-and-selection-undo-steps.md) | The selection mask and selection undo steps | Accepted |
| [0056](0056-text-editing-shortcuts-go-to-a-focused-text-box.md) | Text-editing shortcuts go to a focused text box | Superseded by 0057 |
| [0057](0057-text-editing-actions-act-on-a-focused-text-box.md) | Text-editing actions act on a focused text box | Accepted |
| [0058](0058-text-boxes-group-typing-into-undo-steps.md) | Text boxes group typing into undo steps | Accepted |
| [0059](0059-the-main-window-remembers-where-it-was.md) | The main window remembers where it was | Accepted |
| [0060](0060-selection-combine-modes-and-shape-tools.md) | Selection combine modes and the shape selection tools | Accepted |
| [0061](0061-the-selection-overlay.md) | The selection overlay | Accepted |
| [0062](0062-the-hatching-glides-in-part-pixel-steps.md) | The hatching glides in part-pixel steps | Accepted |
| [0063](0063-the-magic-wand-and-selection-commands.md) | The Magic Wand and the selection commands | Accepted |
| [0064](0064-zebra-hatching-and-brighter-dashes.md) | Zebra hatching and brighter dashes | Accepted |
