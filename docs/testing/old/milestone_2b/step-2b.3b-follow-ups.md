<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2b.3b follow-ups, manual test

**Build:** the commit that adds this file
**Test on:** Windows only (Linux is covered by CI only)
**Time needed:** about 5 min

## What changed

From your 2b.3b testing (T4): after an Alt+click with the Brush, the menu bar's letters stayed underlined. Windows repeats a key while it's held, and each repeat of Alt underlined them again, so the canvas now hides them until Alt goes up. Also, the test for Esc while picking now lets go of Alt first, because Alt+Esc belongs to Windows.

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. **File → New**, choose **ANSI (the 16 standard colors)** under **Colors**, and click **OK** (choose **Don't Save** if it asks).
3. Open **Window → Swatches**. With the Text tool, type `HELLO` near the top left of the canvas, then press **Esc** and **B** to choose the Brush.
4. With **FG Brush** chosen, choose red in Swatches and drag across `HELLO`. Press **2**, choose blue, and drag across it again.

## Tests

### T1: The menu bar after an Alt+click

This checks that the menu bar's letters aren't left underlined.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Hold **Alt** on its own for a moment | One letter of each menu name is underlined (File, Edit and so on). |
| 2 | Still holding **Alt**, click a letter of `HELLO`, and keep holding **Alt** for a couple of seconds | The underlines go when you click, and don't come back while you hold Alt. |
| 3 | Let go of **Alt** | No menu opens, and no letters are underlined. |
| 4 | Tap **Alt** on its own | The menu bar opens with its letters underlined. Press **Esc** to close it. |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Esc while picking

This checks that Esc gives back the colour you had before picking. It's T4 row 6 again, done in the right order.

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Press **2** if BG Brush isn't chosen, and choose green in Swatches | The BG square on the Brush Options toolbar is green. |
| 2 | Hold **Alt**, press the mouse button on a letter of `HELLO`, and let go of **Alt** while still holding the button | The BG square turns blue. |
| 3 | Press **Esc**, then let go of the button | The BG square is green again, and nothing is painted. |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Tests to revisit

Nothing else is waiting from earlier steps.

| Step | Test | Where | Why |
|---|---|---|---|
| None | | | |

- [ ] Pass  - [ ] Fail. Notes:

## Known limitations (not bugs)

- On Windows, Esc while picking works only after letting go of Alt, since Alt+Esc is the system's.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2b.3b follow-ups, test T<n>, row <#>
OS: <Windows 11 version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
