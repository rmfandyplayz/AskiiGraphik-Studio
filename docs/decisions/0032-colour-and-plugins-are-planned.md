<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# 0032: Colour and plugins are planned, not just reserved

Status: Accepted

## Context

ARCHITECTURE §18 reserved colour and plugins as hooks with no plan. After testing 2.3, the owner decided that the app should support colour and ANSI, and that people should be able to write plugins, with a built-in Discord ANSI message plugin among them. Opacity stays out of the plan. The owner left the roadmap changes to Claude Code.

## Decision

- **Colour becomes milestone M2b, right after M2 and before M3.** From M3 on, nearly every milestone adds tools that write cells, and M4 to M7 have features that care about colour (magic wand, fill, image import, exports). Done first, each of those is built with colour from the start. Done last, each needs revisiting. Today only the text tool and the eraser write cells.
- **Plugins become milestone M8b, after M8 and before M9.** A plugin API is a promise to outside code, so it should describe tools, exporters and panels that have settled, and it has to exist before the first release so that packaging can account for it. The built-in Discord plugin is its first user, which proves the API works.
- **Until then, built-ins keep registering the way a plugin would** (ARCHITECTURE §8.1, §16): stable string ids, interfaces and DI registries. This starts with the tool registry in step 2.4.
- **Later milestones keep their numbers.** Many ADRs and test docs mention M3 to M9, and renumbering them would make those records wrong.
- **Layer opacity, blend modes and per-cell alpha stay reserved** with no plan. They could only come from a plugin, and even that needs a design.

## Alternatives

- **Colour after M5 or with M7's exports:** the first thing a user sees sooner, but every tool, the selection work and the renderer would be revisited.
- **Colour inside M3:** ramps and brushes are already a full milestone, and a shared colour model deserves its own testing.
- **Plugins earlier, so tools are written against the API:** the API would be frozen before anything has tested it. Registering built-ins the plugin way gets most of the benefit at no cost.

## Consequences

- Very little has to be redone. `Cell` already has `Foreground`, `Background` and `Attributes` slots, the file format allows optional fields, `DrawAtlas` takes a colour per glyph, and compositing has one implementation. M2b is the "go back" the owner offered to allow for.
- The colour model is the milestone's main design question and is the owner's to weigh in on: Discord's ANSI allows only 8 text and 8 background colours, while terminals reach 16, 256 or full RGB. It's listed in ARCHITECTURE §23.
- A coloured space is visible, so M2b has to revisit the space-as-empty option from step 2.4.
- **Licensing:** a plugin loaded into a GPL-3.0 app is likely covered by the GPL too, so closed-source plugins would be a problem unless the project grants an exception. An exception is easiest to add while the owner holds all the rights, so it should be decided before accepting outside code. Not legal advice; listed in §23.
- **Safety:** a .NET plugin runs with the user's full permissions, because .NET has no sandbox. M8b has to decide whether that's acceptable, or whether plugins use a scripting language instead. Loading .NET plugins also rules out Native AOT and aggressive trimming in M9's packaging.

Updated 2026-11-02: the owner wants a new document to pick monochrome, RGB color or ANSI color, like the character set. The rest of the colour model is still open (ARCHITECTURE §23).
