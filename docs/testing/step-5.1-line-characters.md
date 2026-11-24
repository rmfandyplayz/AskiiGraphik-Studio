<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 5.1: Line characters, manual test

**Build:** the commit that adds this file
**Test on:** Windows (the sheets use the bundled fonts, so they're the same on a Mac; Linux is covered by CI only)
**Time needed:** about 20 min

## What changed

Nothing changes in the app yet. The code that picks each cell's character for lines and shapes is in, with the three styles you chose: **Standard** (by direction: `- | / \` and `+` at corners), **Precise** (the character whose measured ink best follows the line, such as `_ . - ' "`) and **Single character**. There are two candidates for Standard. The first, called Standard on the sheets, gives each cell the character for the way the line runs there. The second, "Standard by step", gives each cell the character for the step from the cell before it to the cell after, so a shallow line shows each step it takes as a `\` or `/`.

You judge them on picture sheets. Step 5.2 builds the Line tool with your choices: which Standard to keep, and anything you'd change in Precise.

## Setup

1. Pull. The sheets are already in `.local/line-sheets` in the repo folder: two per bundled font, `<Font>-lines.png` and `<Font>-shapes.png`.
2. Each style is also cut out on its own, larger, in `.local/line-sheets/rows`: `-0` is the path, `-1` Standard, `-2` Standard by step, `-3` Precise and `-4` Single character.
3. To make them again, run this from the repo root: `dotnet test --project tests/AskiiArt.Imaging.Tests --filter-class "*LineComparisonSheets" --explicit only`. It takes a few seconds.

## Known limitations (not bugs)

- **No tool yet.** The Line, Rectangle and Ellipse tools come in 5.2 and 5.3. Rectangles' rounded corners and filled shapes aren't drawn here.
- **Precise depends on the font.** The four fonts pick differently in places, for example `,` where another picks `.`.
- **Steep lines in Precise look much like Standard.** Every common character inks the middle of its cell across, so nothing can follow a steep line that runs near a cell's side.
- **Where a curve crosses itself**, as the two waves and the spiral's start do, the cell gets one character from one of the crossing pieces.
- **Placeholders:** the sheets' layout, and `*` as the single character. See `docs/TEMPORARY-UI.md`, "Line characters".

## Tests

### T1: Lines at every angle

**Also on macOS:** No

This checks how each style draws straight lines. Open `.local/line-sheets/JetBrainsMono-lines.png`. Each band is one style, labelled on the left, and the top band shows the true lines in red. The top row of each band holds lines rising at 5°, 10°, 15°, 20°, 25°, 30°, 40°, 50°, 60°, 70°, 80° and 85° as they look on screen. Below them is a star of lines every 15°.

| # | Do this | Expected result |
|---|---|---|
| 1 | Compare the flat lines (5° to 30°) in the Standard and Standard by step bands | Standard draws runs of `-` that step up a row at a time. By step draws the same runs with a `/` at each step |
| 2 | Compare the same lines in the Precise band | Precise climbs within each row through `_ . - ' "` before stepping up, so the line looks smoother |
| 3 | Compare the steep lines (60° to 85°) in every band | Mostly `\|` and `/`, and never a `\` in a rising line. In Precise, a `'` or `"` can sit beside a `/` around 60° in some fonts |
| 4 | Compare the star in each band | Each ray follows its red line. Standard is the same all the way round, mirrored; Precise differs a little between sides, since `'` and `` ` `` aren't mirror images. The Single character band uses `*` everywhere |
| 5 | Open the other three fonts' `-lines.png` sheets | Standard and Single character look the same in every font. Precise differs a little |

### T2: Shapes and curves

**Also on macOS:** No

This checks how each style draws rectangles, ellipses and curves. Open `.local/line-sheets/JetBrainsMono-shapes.png`. Each band holds three rectangles, round and wide ellipses, two waves, a curve and a spiral.

| # | Do this | Expected result |
|---|---|---|
| 1 | Look at the rectangles in each band | Every style but Single character has `+` corners, `-` along the top and bottom and `\|` down the sides |
| 2 | Look at the ellipses in Standard and in Standard by step | Standard puts `\\` and `//` where the outline turns. By step puts `-\` and `/-` there |
| 3 | Look at the ellipses in Precise | The tops and bottoms curve through `_ . - ' "`, like `_.-""-._` |
| 4 | Look at the waves, the curve and the spiral in each band | Each follows its red path above. The cells are the same in every band; only the characters differ |
| 5 | Open the other three fonts' `-shapes.png` sheets | As in step 4, with Precise picking a little differently in each font |

### T3: Your choices

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Tell me which Standard to keep: Standard, or Standard by step | Your pick, for 5.2 |
| 2 | Tell me anything you'd change in Precise, such as a character you'd rather it never used | Your notes, for 5.2 |
| 3 | Tell me whether `*` is a good default for Single character, or what it should be | Your pick. In the tool, the Character Brush's character may be the natural default |
