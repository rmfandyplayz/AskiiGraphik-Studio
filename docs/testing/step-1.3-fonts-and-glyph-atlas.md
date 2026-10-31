<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 1.3: Fonts and glyph atlas, manual test

**Build:** working tree (not pushed yet)
**Test on:** - [ ] Windows  - [ ] macOS (from CI's font sheets)  (Linux is covered by CI)
**Time needed:** about 10 min, plus waiting for CI after the push

## What changed

The app now ships four fonts (JetBrains Mono, Roboto Mono, IBM Plex Mono, Fira Code) and can load any font file or installed font. It works out the grid's cell size from the font, checks whether a font is monospace, and draws characters as small images that it caches. A character the font doesn't have is drawn with another installed font, or as an empty box if no font has it, and is reported so the canvas can flag it later. Nothing shows on screen yet: the canvas arrives in step 1.4. To check the drawing by eye, the tests write a "font sheet" picture for each font.

## Setup

1. Open the solution in Rider.

## Tests

### T1: The tests pass and write the font sheets

| # | Do this | Expected result |
|---|---|---|
| 1 | In Rider, open the **Unit Tests** window and run all tests in the solution | All tests pass (about 300). The new ones are under **AskiiArt.Imaging.Tests → Fonts** and **→ Glyphs**. |
| 2 | In the repo folder, open `artifacts/font-sheets/` | It holds four pictures: `JetBrains-Mono.png`, `Roboto-Mono.png`, `IBM-Plex-Mono.png` and `Fira-Code.png`. |
| 3 | Run the app (`dotnet run --project src/AskiiArt.App`) | It starts and looks exactly as it did after step 0.3. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: The font sheets look right (Windows)

Each sheet shows the same samples twice, at 16 and 32 pixels. The top line names the font, size and cell size (for JetBrains Mono: `cell 10 x 21` and `cell 19 x 42`). Each character sits in its own cell of a light grey grid. Grey cells at the end of a row are just unused. A white cell means the font has that character, **orange** means another installed font drew it, and **red** means no font has it.

| # | Do this | Expected result |
|---|---|---|
| 1 | Open `JetBrains-Mono.png` and look at **Printable ASCII** and **Latin-1** | Every cell is white, every character is sharp and fully inside its cell, and nothing is cut off at the top or bottom (look at `g`, `j`, `y`, `Å`, `Ç`). |
| 2 | Look at **Box drawing and blocks** | Lines and blocks reach the edges of their cells. The shades `░▒▓` look like light, medium and dark texture. |
| 3 | Look at **Fallback check** | The Chinese, Japanese and Korean characters and the emoji are orange and shrunk to fit their cells, not cut in half. The last cell is red with a hollow box in it. |
| 4 | Repeat rows 1 to 3 for the other three sheets | Same results. Some cells under **Arrows and shapes** may be orange for some fonts; that just means the font lacks them. |
| 5 | Compare the 16 px and 32 px halves of any sheet | Same characters, same layout, just bigger. Small text is a little soft at the edges, never blurry or colour-fringed. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: CI is green, and the macOS and Linux sheets look right

| # | Do this | Expected result |
|---|---|---|
| 1 | Commit and push, then open https://github.com/rmfandyplayz/AskiiGraphik-Studio/actions and wait for the run | All three jobs have a green check mark. |
| 2 | Scroll to the bottom of the run's summary page, to **Artifacts** | There are three: `font-sheets-windows-latest`, `font-sheets-macos-latest` and `font-sheets-ubuntu-latest`. |
| 3 | Download `font-sheets-macos-latest`, unzip it, and repeat T2 rows 1 to 3 on its sheets | Same results as on Windows. The edges of the characters can look slightly different (each OS smooths text its own way), but the cell sizes in the top line are identical. |
| 4 | Do the same with `font-sheets-ubuntu-latest` | Same results, except that more cells in **Fallback check** may be red: the Linux machine has few fonts installed. |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Fira Code is known for joining `->` or `==` into one symbol. That can't happen here, because each cell is drawn on its own.
- The cell sizes are worked out from the font files themselves, so a font gives the same grid on every computer.

## Known limitations (not bugs)

- Nothing appears in the app yet; the canvas that uses all this is step 1.4.
- How the canvas flags an orange or red cell (and warns you) is still yours to design. Claude Code will ask about it by step 1.4.
- There's no font picker yet, and no warning for a font that isn't monospace; both come with step 1.6.
- Only the Regular weight is bundled. You can still load a bold or italic font file once the font picker exists.
- The font sheets' colours, grid and layout are only for this check; they're not how the canvas will look.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 1.3, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Attach: the font sheet that looks wrong (or a screenshot of the wrong part)
```
