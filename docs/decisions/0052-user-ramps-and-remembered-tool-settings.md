<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0052: User ramps and remembered tool settings

Status: Accepted

## Context

Step 3.5 adds the Ramps window: view, edit, preview, sort, generate, add, duplicate, rename and delete ramps, and import and export them as files. The user's ramps and brush settings are remembered between runs. The owner's answers (2026-11-18): built-in ramps are read-only and Duplicate gives an editable copy; remember every Tool Options value, each group's last tool and the FG and BG colours, but still start on the Text tool; edit a name, one line of characters (lightest first) and the lightest level, applied live once valid, with no Undo and a confirmation before Delete; sort and generate in the active document's font; one ramp per `.asciiramp` file; user ramps listed after the built-in ones, newest last; deleting a ramp a tool uses puts that tool back on Standard.

## Decision

- **`RampLibrary` (Core)** holds the user's ramps as `UserRamp(Id, Name, Ramp)`, ids `user.<guid>`, so a tool's choice follows a rename. The built-in names, which the App translates, are reserved. Names are unique ignoring case and surrounding spaces; `Add` numbers a taken one ("Mine 2"), while `Rename` refuses it. Only user ramps change. It raises `Changed`.
- **Choice sources.** A `ChoiceToolOption` can name a source (`MoreChoicesFrom`, an `IToolOptionChoiceSource` registered with `ToolSettings`). `ToolOptionValues` lists its choices after the option's own and accepts them; when a chosen one disappears, the option goes back to its default. The Ramp option uses the user-ramps source, so the options code and the toolbar know nothing about ramps. Tools read the ramp through `RampSource`, which looks the chosen id up when asked, so an edit applies to the next stroke.
- **Live editing.** The window applies a name or characters as soon as they're valid (`Ramp.Check` reports, in order: empty, a space, a character that can't fill a cell, a repeat) and otherwise says why, keeping the last valid ramp. Ramp edits aren't on Undo, since the history belongs to the document.
- **Sort and Generate** measure in the active document's font. Generate drops spaces, repeats, unusable and inkless candidates, sorts by density, and picks `count` of them by dynamic programming, minimising the squared distance from evenly spaced targets and always ending on the darkest. It's O(n²·k) for n candidates, which is instant for a few hundred.
- **Ramp files** are UTF-8 JSON with `\n` endings: `format: "askiiart.ramp"`, `version: 1`, `name`, `lightest` (`empty` or `space`), `characters`. A newer version is refused, as documents are. Import takes several files and lists those it couldn't read, and why.
- **Remembered in settings (§15).** User ramps (`ramps`) are saved whenever they change, since they're the user's work. Tool options (`toolOptions`), each group's last tool (`lastTools`) and the colours (`colors`, written as in documents by `CellColorText`) are saved when the window closes and restored when it's built, as the layout is. Anything saved that no longer applies is ignored: an unknown choice, a number out of range (clamped), a tool that left its group. `ToolSettings` keeps saved values for groups no tool has asked for yet.

## Alternatives

- **Special-casing the Ramp option in the App:** the toolbar would have to know about ramps, and plugins (M9) will want dynamic choices too.
- **Giving each tool its `Ramp` when it's created:** the ramp could be edited while the tool is chosen. A lookup per stroke costs nothing.
- **Saving tool state on every change:** many writes while a slider is dragged. The cost of saving on close is that a crash loses that run's tool changes, but never ramps.
- **A greedy pick for Generate:** it can repeat a character or miss a better spread; the exact pick is small.

## Consequences

- Tool state is only saved when the app closes normally. Stopping it from Rider skips that.
- The ramp chosen for the Brush and Soft Eraser survives renames and edits, and falls back to Standard when deleted.
- `CellColorText` now holds the colour names for documents and settings alike.
