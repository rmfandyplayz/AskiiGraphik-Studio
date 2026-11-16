<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Step 2.4: Space option and hard eraser, manual test

**Build:** latest `master`
**Test on:** Windows, plus macOS for the tests marked "Also on macOS: Yes" (Linux is covered by CI only)
**Time needed:** about 15 min, plus the revisit list in T7

## What changed

Each layer can now make its spaces see-through. There's a second tool, the Eraser, and you can switch tools from a new Tools menu, the toolbar, or the T and E keys. A file now remembers which layer was active, and several bugs from 2.3's testing are fixed (T6).

## Setup

1. From the repo root, run `dotnet run --project src/AskiiArt.App` (or use the AskiiArt.App run configuration in Rider).
2. Keep Layers and History open. Each layer row now has three checkboxes: **visible**, **locked** and **spaces are see-through**. Hover over one to see which is which.

## Tests

### T1: Spaces are see-through

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | On "Layer 1", type `xxxxxxxx` at the top left. Click New, then click the same top-left cell and type `a  b` (two spaces in the middle) | You see `a  b` followed by `xxxx`: the spaces hide the `x`s under them |
| 2 | Tick the top layer's third checkbox | You see `axxbxxxx`. History shows "Change Space Option" |
| 3 | Press Ctrl+Z (Cmd+Z on a Mac) | The spaces hide the `x`s again, and the box is unticked |
| 4 | Press Ctrl+Y (Cmd+Shift+Z on a Mac), then choose Edit → Copy as Text and paste it into a text editor | The pasted first line is `axxbxxxx` |
| 5 | Untick the box, copy as text again and paste | The pasted first line is `a  bxxxx` |
| 6 | Tick the box on the bottom layer instead, and untick it on the top one | Nothing changes on the canvas: the option only affects its own layer |

- [ ] Pass  - [ ] Fail. Notes:

### T2: Saving the option, and merging

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Set up T1 row 2 again, save, close the document with File → New, then open the file | The picture and the ticked box are as you left them |
| 2 | With the top layer highlighted, click Merge Down | One row is left, and the picture doesn't change |
| 3 | Undo | Both layers are back, with their own boxes |
| 4 | Make three layers. On the bottom one type `bbbb`. On the middle one type `m  m` and tick its box. On the top one, leave the box unticked, click the second cell of that row and type one space | You see `m bm`: the middle layer's second space shows a `b`, and the top layer's space hides what's under it |
| 5 | Highlight the top layer and click Merge Down | You still see `m bm`. The merged layer's box becomes unticked, which is expected: it's what keeps the picture the same |
| 6 | Undo | Three layers again, and the middle layer's box is ticked |

- [ ] Pass  - [ ] Fail. Notes:

### T3: Switching tools

**Also on macOS:** Yes, because the menu's check marks and the letter keys differ there

| # | Do this | Expected result |
|---|---|---|
| 1 | Open the Tools menu, and look at the toolbar | Text and Eraser are listed, with Text checked. The toolbar's Text button is pressed in |
| 2 | Choose Tools → Eraser | Eraser is checked in the menu and pressed in on the toolbar |
| 3 | Click the toolbar's Eraser button again | It stays pressed in |
| 4 | Click the toolbar's Text button, click a cell, and type `hello` | It types normally, including the `e` |
| 5 | Press Esc, then press E | The caret goes away, then the Eraser becomes active |
| 6 | Press T | Text becomes active, with no caret |
| 7 | Press Ctrl+E (Cmd+E on a Mac) | The tool doesn't change |
| 8 | Double-click a layer's name and type `eat` | The name box gets `eat`, and the tool doesn't change |
| 9 | Choose Eraser, then File → New and accept | The new document still has the Eraser chosen |

- [ ] Pass  - [ ] Fail. Notes:

### T4: Erasing

**Also on macOS:** Yes, because pointer capture and trackpads differ there

| # | Do this | Expected result |
|---|---|---|
| 1 | Fill a few rows with text (type long lines). Choose Eraser and click one letter | Only that cell empties |
| 2 | Drag slowly across a row | The cells empty as you go. History shows one "Erase" |
| 3 | Drag very fast, in a zigzag, across several rows | The line has no gaps |
| 4 | Press Ctrl+Z once | The whole stroke comes back in one go |
| 5 | Press Ctrl+Y | It's erased again |
| 6 | Hold the mouse down, drag off the canvas and around, then come back onto it elsewhere and let go | Cells empty only on the canvas, along the path you dragged, and it's still one step |
| 7 | Start dragging, press Esc while still holding the mouse, keep moving, then let go | The stroke disappears as soon as you press Esc. Moving after that erases nothing. History has no new row |
| 8 | Hold Space and drag | The canvas pans, and nothing is erased. The pointer becomes a hand |
| 9 | With the middle button, drag | The canvas pans |
| 10 | Move the pointer around without pressing | Nothing is erased |
| 11 | Alt-Tab (Cmd-Tab on a Mac) away in the middle of a stroke, come back, and move the pointer | Nothing more is erased, and the stroke so far is kept |

- [ ] Pass  - [ ] Fail. Notes:

### T5: Erasing and layers

**Also on macOS:** No

| # | Do this | Expected result |
|---|---|---|
| 1 | Make two layers with different text in the same place, then erase over the top layer's text | The lower layer's text shows through where you erased: erasing leaves empty cells, not spaces |
| 2 | Lock the highlighted layer and drag with the Eraser | Nothing changes. History has no new row |
| 3 | Unlock it, hide it, and drag | Nothing changes |
| 4 | Show it, highlight the other layer, and drag | Only the highlighted layer's cells empty |
| 5 | Erase something, save, undo, and look at the title | The title shows `*`, since you moved away from what was saved |

- [ ] Pass  - [ ] Fail. Notes:

### T6: Fixes from 2.3's testing

**Also on macOS:** Yes, because the crash was seen there and the menu is native

| # | Do this | Expected result |
|---|---|---|
| 1 | Make three layers with text, highlight the middle one, save, then open the same file again | The middle layer is highlighted, and typing goes to it |
| 2 | Highlight another layer, then close the window or start a new document | You're not asked to save just for that: choosing a layer isn't a change |
| 3 | Open a file saved before this update | The top layer is highlighted |
| 4 | Float History by dragging it over the canvas, then open the Window menu | History still has its check mark |
| 5 | Close the floating History from its own close button, and open the Window menu | The check mark is gone |
| 6 | Choose Window → History, then float Layers and dock it back to a window edge | Both stay checked |
| 7 | On a Mac: close the window, leave the app running, then open a new window from the Dock menu and change your displays or let the Mac sleep and wake | The app keeps running. If you can't make it happen, just keep an eye out for the crash during the other tests |

- [ ] Pass  - [ ] Fail. Notes:

### T7: Tests to revisit

The tests left over from earlier steps.

| Step | Test | Where | Why |
|---|---|---|---|
| 2.3 | T3: Rename | Windows | Passed on a Mac, not tried on Windows yet |
| 2.3 | T6: Undo and redo | Windows | Passed on a Mac. You expect it to work; it needs one quick run |
| 2.3 | T8: Wherever the panel is | Windows | Passed on a Mac, not tried on Windows yet |
| 2.1 | The retest list at the top of [its doc](step-2.1-dock-shell.md) | Windows, and a Mac where marked | Those changed parts haven't been reported yet, and 2.1 stays open until they are |

- [ ] Pass  - [ ] Fail. Notes:

## Worth poking at (optional, unscripted)

- Erasing while zoomed far in or far out, and on a big canvas (Debug → Open Sample Document).
- Pressing Ctrl+Z in the middle of a stroke.
- Switching tools with E or T in the middle of a stroke.
- Merge Down across all the combinations of the space box.

## Known limitations (not bugs)

- The Eraser is one cell wide. Sizes arrive with brushes in M3, and so does the soft eraser.
- The pointer doesn't change for the Eraser, apart from the hover highlight.
- The T and E keys aren't shown in the menus, and only work while the canvas has the keyboard and no caret is placed.
- The buttons and checkboxes are placeholders. The toolbar gained two buttons, so it may scroll sideways sooner.
- Colour isn't here yet. It's now on the roadmap as M2b, and so are plugins as M8b.

## Reporting a problem

Paste this into Claude Code:

```
Bug in step 2.4, test T<n>, row <#>
OS: <Windows 11 / macOS version>
Did: ...
Expected: ...
Actually: ...
Log: <if there was an error, open Help → Open Log Folder and paste the last lines>
```
