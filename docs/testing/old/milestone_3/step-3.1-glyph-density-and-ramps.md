<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 3.1: Glyph density and ramps, manual test

**Build:** the commit that adds this file
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 15 min

## What changed

Nothing you can draw with yet: this step builds what the Brush will use in 3.3. The app can now measure how dark each character looks in a font (the share of its cell the ink covers), and it has the five ramps we agreed on: Standard, Standard Alphanumerical, Fine, Blocks and Braille. A ramp turns an intensity from 0 (nothing) to 1 (the most ink) into a character. **Debug → Open Density Sample** shows the measurements and the ramps, so you can check by eye that they look right.

## Setup

1. Run the AskiiArt.App run configuration in Rider (or, from the repo root, `dotnet run --project src/AskiiArt.App`). It must be a Debug build, which is what both give you, because the Debug menu only exists there.
2. The sample is about 115 cells wide, so you'll probably need to zoom out or pan to see all of it.

## Tests

### T1: Opening the sample

Checks that the Debug menu item opens the sample in the current document's font, the way the other Debug samples work.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the **Debug** menu | **Open Density Sample** is listed after Open RGB Test Sample |
| 2 | Type a few characters on the canvas, then choose **Debug → Open Density Sample** | You're asked whether to save your changes first, as with the other samples. Choose not to save |
| 3 | Look at the top line of the sample | It reads `Density sample (temporary): JetBrains Mono` (the font a new document uses) |
| 4 | Look at the rest | Lines of text: the characters sorted from lightest to darkest, a list of percentages, the ramps by name, and five long rows of characters between `\|` marks at the bottom |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Characters are sorted from lightest to darkest

Checks the measuring: each character's darkness should match how it looks on screen.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Look at the line under **Printable ASCII, lightest to darkest** | It runs from thin marks such as `` ` `` `.` `-` `'` on the left, through letters and digits, to heavy characters such as `$` `W` `@` on the right. Each character should look about as dark as its neighbours, or darker than the one on its left |
| 2 | Look at the line under **Blocks, lightest to darkest** | It runs from the small quarter blocks (`▖▗▘▝`) and the light shade `░`, through half blocks and `▒`, to three-quarter blocks and `▓`, and ends with the full block `█` |
| 3 | Look at the percentages under **Darkness of each, in percent of the cell** | The numbers only go up, reading left to right and row by row. The quarter blocks and `░` read 25.0, the half blocks and `▒` 50.0, the three-quarter blocks and `▓` 75.0, and `█` 100.0 |
| 4 | Find `@` in the percentages | It's about 25, the same as `░`. That's expected: even the darkest letters and symbols ink only about a quarter of their cell |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Each ramp paints from nothing to its darkest character

Checks how a ramp turns intensity into characters. Each of the five rows at the bottom is a ramp painted across 80 cells, from intensity 0 on the left to 1 on the right, the way a stroke of the Brush will choose its characters in 3.3.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Look at the **Built-in ramps** list | Five ramps, in this order: Standard, Standard Alphanumerical, Fine, Blocks and Braille. Each has a **shipped** line (the ramp as it comes) and a **sorted** line (how this font's measurements would order it) |
| 2 | Compare Standard's two lines | Shipped is `.:-=+*#%@`, and sorted swaps `:` and `-`. This font measures `-` a little lighter than `:` (see the summary) |
| 3 | Look at the five rows at the bottom | Each row starts with a few empty cells (intensity 0 paints nothing), then steps through the ramp's characters from left to right, and ends with its darkest character: `@`, `W`, `W`, `█` and `⣿` |
| 4 | Look at the Standard and Blocks rows | Each character takes a band of about the same width, apart from the first and last bands, which are about half as wide |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Characters the font doesn't have

Checks that characters drawn from another font are measured as they're drawn. None of the bundled fonts has braille, so the app borrows it from a font on your computer, and the canvas flags those cells.

**Also on macOS:** Yes, because each platform has different fonts to borrow from

| # | Do this | Expected result |
|---|---|---|
| 1 | Look at the line under **Drawn from a fallback font** | It lists the eight braille characters `⠁⠃⠇⡇⣇⣧⣷⣿` and nothing else |
| 2 | Look at the braille characters wherever they appear in the sample | Their cells have the orange background that marks a character drawn with another font |
| 3 | Look at the braille line under **Braille, lightest to darkest** | It reads `⠁⠃⠇⡇⣇⣧⣷⣿`: one dot, then two, three, four, and so on up to all eight |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Another font gives other measurements

Checks that the sample measures the document's own font, since each font draws its characters differently.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Choose **Document → Font…**, pick **Roboto Mono**, and close the picker | The sample redraws in Roboto Mono |
| 2 | Choose **Debug → Open Density Sample** again | The top line now ends with `Roboto Mono` |
| 3 | Look at the line under **Drawn from a fallback font** | It now lists the block characters as well as braille, because Roboto Mono doesn't have them |
| 4 | Look at the percentages | The half blocks `▀▄▌▐` still read 50.0 and `█` still reads 100.0, because solid blocks are always stretched to fill the cell. The shades `░▒▓` may be a little off 25, 50 and 75, since their dot patterns come from the borrowed font. The letters' numbers differ from JetBrains Mono's |
| 5 | Repeat steps 1 and 2 with **Fira Code** and **IBM Plex Mono** | The top line names each font in turn, and the fallback line lists only braille |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Tests to revisit

| Step | Test | Where | Why |
|---|---|---|---|
| 2b.3b follow-ups | T1 (menu bar after an Alt+click) | Windows | Not reported yet |
| 2b.3b follow-ups | T2 (Esc while picking) | Windows | Not reported yet |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Open the sample with a font you loaded from a file (Document → Font… → Load from file), especially one with few symbols: more characters should move to the fallback line.
- Zoom in on the gradient rows and check by eye that each step to the right looks a little darker.

## Known limitations (not bugs)

- Nothing paints with ramps yet. The Brush learns to in step 3.3, and the ramp editor arrives in 3.5.
- The sample is temporary, plain text, and goes when the ramp editor can show the same things.
- Braille always comes from a borrowed font, so its cells are always flagged orange, and how dark it measures depends on that font.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 3.1, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
