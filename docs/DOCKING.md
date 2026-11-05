<!-- SPDX-License-Identifier: GPL-3.0-or-later -->
<!-- Written with Claude Code (Anthropic). -->

# Docking spec

How windows and toolbars behave, from the owner's brief of 2026-11-04 and their answers to the questions on it. This is the target for steps 2.5b to 2.5g, which are building it in stages (`docs/ROADMAP.md`). The decision to replace the Dock library is ADR 0035.

The owner approved all of this on 2026-11-04. All sizes and speeds are theme tokens, tuned in testing. Menu wording is a placeholder.

## 1. Words

| Word | Meaning |
|---|---|
| **Window** | A content panel such as Layers or History. The code says *panel*, because `Window` is an Avalonia class. The whole program is the **app window**. |
| **Widget** | A translucent window in a stack, with gaps around it. The brief's "soft dock". |
| **Dock** | Opaque windows that touch the edge and each other, with tabs. The brief's "hard dock". |
| **Floating window** | A window on its own, above everything, anywhere inside the app window. |
| **Side** | The left or right of the app window. A side holds nothing, widgets or a dock, never a mix. Both sides can be widgets, both docks, or one of each. |
| **Toolbar** | A small bar of tool buttons, anchored or floating. |
| **Anchor** | One of a toolbar's 8 places: 4 corners and 4 edge middles. |
| **Sub-toolbar** | A smaller toolbar showing the rest of a selected tool group. |
| **Canvas region** | What's left once the sides have taken their widths. Toolbars live here. |
| **Edge strip** | The thin zone at the very edge of the app window that's active while dragging a window. |

## 2. The overall layout

```
┌ menu bar strip (the system menu bar on a Mac) ───────────────┐
├───────┬───────────────────────────────────────────────┬──────┤
│       │ [top-left]      [top-middle]      [top-right] │      │
│ left  │ [left                                [right   │ right│
│ side  │  middle]      canvas region          middle]  │ side │
│       │ [bottom-left] [bottom-middle] [bottom-right]  │      │
├───────┴───────────────────────────────────────────────┴──────┤
│ status bar strip                                             │
└──────────────────────────────────────────────────────────────┘
```

Everything is worked out in this order:

1. The menu and status strips are fixed. Nothing goes outside them, as now.
2. Each side takes its width and the full height between the strips.
3. Toolbars fit into the canvas region.
4. The safe area is what remains for centring the canvas (section 8).

The canvas paints everywhere between the strips, so art shows through widgets and is hidden by docks.

**Toolbars never pass a window's height.** A toolbar's top edge is never above the top edge of a dock or widget, and its bottom edge is never below theirs. The default layout today breaks this, with its toolbar row above Layers and History. Toolbars use the same margin as widgets, so they line up exactly. Floating toolbars are held to the same rule by staying inside the canvas region.

**From back to front:** canvas, docks and widgets, toolbars, sub-toolbars, floating windows, then whatever is being dragged. Floating windows may cover toolbars. Among floating windows, the last one clicked is on top.

## 3. Windows

Windows dock left and right only for now, and their contents read top to bottom. A horizontal dock, such as a future animator timeline, may come one day, so the code doesn't assume a side is vertical.

### 3.1 Widgets

- A single widget, or a top-to-bottom stack of at most 4. Never side by side.
- The stack always fills its side's height, less margins. There's a gap between widgets.
- The widgets on a side share one width. Drag the inner edge to change it.
- Drag the gap between two widgets to change their heights. Once one is title-only, the next one gives way.
- A widget squeezed below its minimum height snaps to **title-only**, and can't go smaller. Dragging it back snaps it to its minimum height. In between sizes don't exist.
- At least one widget in a stack always keeps its contents.
- A widget arriving in a stack takes a fair share of the height, from the largest widget first. Title-only widgets stay title-only.
- Translucent for now. Frosted glass waits (section 10).

### 3.2 Title row and hover buttons

Widgets and floating windows have a title at the very top, and no visible buttons until needed:

| State | Menu and close buttons |
|---|---|
| Pointer away from the window | Hidden |
| Pointer over the window | Shown faintly |
| Pointer over a button | That button at full opacity, with a background showing its shape |

```
 Window Title            ≡  ✕
┌───────────────────────────┐
│ contents                  │
└───────────────────────────┘

 Window Title            ≡  ✕      title-only
```

Right-clicking the title opens the same menu as the ≡ button. Dragging the title moves the window.

| Window | Menu | Enabled when |
|---|---|---|
| Widget | Float | Always |
| | Turn into Dock | Always. The whole side converts |
| | Close | Always |
| Floating | Turn into Widget | A side has room, using the opening rules (section 6) |
| | Close | Always |
| Dock tab, on right-click | Float | Always |
| | Turn into Widget | The side has one column, no tabs and at most 4 windows. The whole side converts. Greyed out otherwise |
| | Close | Always |

The Window menu also opens and closes every window.

### 3.3 Docks

Works like Illustrator:

- **Shape:** columns, each a top-to-bottom stack, each of those a group of tabs. No splits inside splits.
- **Look:** opaque, with no margins or gaps. Every dock window has a tab strip, even with one tab, and no hover buttons.
- **Resizing:** drag a border. Title-only applies here too, the same way as widgets.
- **Tabs:** drag a tab to reorder it or take it elsewhere. Too many tabs scroll, with fades like a toolbar's.
- **Converting:** a stack keeps its order and heights when a side converts either way.

### 3.4 Floating windows

- They stay inside the app window, and are single windows with no tabs (the owner's decision).
- They look like widgets, and have the same hover buttons.
- Drag the title to move, and the edges or corners to resize. Title-only works here too.
- Dropping a window on a floating window just floats it on top.

## 4. Toolbars

### 4.1 Anchors

- **At most one toolbar per anchor.**
- **Corners:** the toolbar's origin is at the corner, horizontal or vertical. Right-click chooses.
- **Edge middles:** centred on that edge, with no dead space around it. They follow the edge: horizontal on top and bottom, vertical on left and right.
- **Floating:** anywhere in the canvas region, horizontal or vertical. A floating toolbar may overlap an anchored one, which doesn't move for it.

### 4.2 The toolbar itself

- No title and no close button. Translucent like a widget.
- **Grip:** a small 6-dot grip at both ends, faint while the pointer is over the toolbar and invisible otherwise. Its space is always reserved, so buttons don't shift. Only the grips start a drag.
- **Right-click:** Horizontal and Vertical (corners and floating only), and Hide. The Window menu also shows and hides each toolbar.
- **Too little room:** the icons scroll along the toolbar's direction, with no scrollbar. The background and grips stay put. A fade shows on each side that has more to scroll to.

### 4.3 Sharing space

There are two kinds of collision, and they follow different rules.

**Along the same edge, toolbars fight for length, and the middle one has slight priority.** A toolbar that fits its fair share keeps its natural size, and the rest share what remains. With even gaps, and all of them long, the middle toolbar ends up slightly longer than the corners (25% longer to start with, tuned in testing).

```
 Plenty of room: corners stay in corners, the middle is centred.
 [File]                 [Tools]                  [Edit]

 Not enough: everyone shrinks and scrolls, and the middle gets a bit more.
 [‹ File long ›] [‹ Tools long, a bit longer ›] [‹ Edit long ›]
```

A middle toolbar stays centred until a corner toolbar reaches it, then moves aside. Two middle toolbars long enough to reach the same empty corner don't overlap: the horizontal one gets the corner.

**At a corner, the corner toolbar wins.** A toolbar on the neighbouring edge starts after it. The owner's mockups show this, including a sub-toolbar pushing further:

```
 Top-right horizontal, right-middle vertical:

              [ top-right toolbar ]
              [ its sub-toolbar   ]
                              [ r ]
                              [ i ]      pushed down, and shortened
                              [ g ]      only if it no longer fits
```

Below the smallest size, toolbars hide until there's room again, corners first.

### 4.4 Sub-toolbars

- Only **one at a time**, since only one tool is active. The tool decides it, so it isn't part of the layout and isn't saved.
- It runs the same way as its parent, and sits on the canvas side of it. It's aligned to the corner for a corner toolbar, and centred on the parent otherwise.
- **A floating toolbar's** stays on the side that faced the canvas when the toolbar last left an anchor. Pulled from the top edge, it stays below. It flips to the other side while there's no room, and goes back once there is. Turning the toolbar keeps the side: above goes with left, and below with right.
- At most its parent's length if the parent is fighting for space, otherwise at most 50% longer. It scrolls if that's not enough.
- It pushes the toolbars of the neighbouring edges out of the way, with a short animation. A floating toolbar's pushes nothing, as a floating toolbar pushes nothing. With no room to push anything into, it's left out until there is.
- It can't be dragged, but it goes with its parent. During a drag it follows the toolbar, turned and see-through to match, on the side it would land on. It never moves the canvas (section 8).
- 2.5d tests it with a sample in the Debug menu. Tool groups arrive in M3.

## 5. Dragging

Common to every drag:

- The dragged item follows the pointer.
- The layout previews live what a drop would do, using the same code as the drop, so they can't disagree.
- Movement is lightly animated. Esc cancels, and so does the app losing focus.
- Drop zones are worked out once when the drag starts and don't move during it, which stops zones flickering as the layout shifts under the pointer.

### 5.1 Toolbar drags

Starting a drag shows a zone for each free anchor, and for its own. Occupied anchors show nothing. Dropping anywhere else floats the toolbar there.

Dropping on a middle anchor sets its direction. A corner keeps the direction it had.

**Zones lie among the toolbars like toolbars.** A middle's is a strip that runs between whatever is at each end of its edge, with the usual gap, so a zone never overlaps an anchored toolbar. It's as thick as the dragged toolbar would be there. A strip always gets at least 30% of its edge: the toolbars beside it shrink and scroll for as long as the drag lasts, and get their length back after it. A corner's is a square with room for a strip each way, so its side is the two strips' thicknesses added together. With toolbars the same both ways, that's double. It's never thinner than the strips beside it, unless the region is so small that two squares would meet, where it stops at half the region's shorter side.

**Over a zone, the zone is the preview.** Its blue fades, leaving a dashed outline around the strip or square, and the dragged toolbar goes much more see-through. There's no copy of the toolbar, and nothing else moves until the drop.

**Pushed back, the preview is the toolbar itself.** A floating toolbar that would have to move to stay inside the canvas region shows where it would land as a see-through copy of itself, inside a dashed outline. Any zone that overlaps fades away until it moves off. A floating toolbar that would land right where it's held needs none.

**The outline's dashes run around it** like ants, for as long as it shows.

### 5.2 Window drags

The first match, top to bottom, wins:

| Pointer is over | Dropping does this |
|---|---|
| Edge strip of a side with no windows | The window becomes a widget. A thin placeholder shows at the edge, and toolbars move in |
| Edge strip of a side with widgets | The side becomes a dock. The window is a column at the very edge, and the rest are one column beside it |
| Edge strip of a side with a dock | A new column at the very edge |
| The window's own place | Nothing changes |
| The zone just inside a dock's inner edge, when this is the side's only window | It becomes a widget |
| Upper or lower half of a widget, on a side with room | It joins the stack above or below that widget. The others move apart for a hatched "+" placeholder |
| A dock window's tab strip or middle | It becomes a tab |
| A dock window's top or bottom band | It stacks above or below |
| A dock window's left or right band | It becomes a new column beside that column |
| Anywhere else, or a side already holding 4 widgets | It floats there |

A title-only window that lands somewhere gets a normal size. Dragging the last window off a side empties it, and toolbars move back.

```
 Empty side           Widgets              Dock
 ┌──────────┐         ┌──────────┐         ┌──────────┐
 │         ▌│ strip   │  ┌─────┐ ▌│ strip  │  ┃ A │ B │
 │         ▌│ makes a │  │upper│ ▌│ makes  │  ┃   │   │  bands: stack, column
 │         ▌│ widget  │  │lower│ ▌│ a dock │  ┃   │   │  middle: tab
 └──────────┘         └──────────┘         └──────────┘
```

## 6. Opening and closing

**Reopening** a closed window puts it back where it last was, if that's still possible. Otherwise it's treated as new.

**A new window** takes the first of these that applies:

1. If only one side can take a widget, it goes there. A dock side can't, nor can a side with 4 widgets.
2. If both can, it goes to the side with no toolbar anchored at its middle. Corner toolbars don't count.
3. If that doesn't settle it, the side with fewer windows, then the right.
4. If neither side can take one but a dock exists, it becomes a tab in the dock window with the largest area.
5. If both sides are full of widgets, it floats. Opening a window never converts a side.

It joins the bottom of a widget stack.

Showing a hidden toolbar puts it back on its anchor, or floats it there if the anchor has since been taken.

## 7. Resizing the app window

- Saved sizes are what the user asked for. The solver fits them to the current window without changing them, so shrinking and regrowing restores everything.
- Widgets squeezed below their minimum go title-only for as long as they have to, starting from the bottom of the stack.
- Floating items keep their position, moved just enough to stay inside. Regrowing puts them back.
- If a window is too small for everything: toolbars scroll, then hide, then sides shrink to their minimum widths. The app's existing minimum window size makes this rare.

## 8. The canvas

- **Safe area:** the canvas region, less a band along each edge that has an anchored toolbar lying along it. Centring, zoom to fit and scroll limits use it, as they do today.
- **Not counted:** floating items and sub-toolbars, so picking a tool never moves the view.
- **During a drag,** the safe area stays as it was until the drop, so the art doesn't slide around under the preview.
- **Clicks:** empty space, including the gaps around toolbars, reaches the canvas. A thin handle along a widget's gaps and inner edge catches clicks, because that's how they resize.
- **Typing:** clicking a window or a toolbar hands the keyboard back to the canvas unless a text box has it, as today.

## 9. Menu, default and saving

**The Window menu** lists each window and each toolbar, check-marked, then Reset Layout.

**The default layout** (placeholder toolbars until the toolbar customizer in M8):

| Item | Place |
|---|---|
| Layers above History | Widgets on the right |
| File toolbar: New, Open, Save | Top-left, horizontal |
| Tools toolbar: Text, Eraser | Bottom middle, horizontal |
| Edit toolbar: Undo, Redo, Resize Canvas, Font | Top-right, horizontal |
| View toolbar: Zoom Out, Zoom In, Actual Size, Show Grid | Bottom-right, horizontal |

**Saving:** the layout is saved in settings as before, under a new name, so everyone's layout resets once when this arrives. On load, anything invalid is repaired rather than rejected, and a window the app no longer has is skipped.

## 10. Not in this pass

| Idea | Where it went |
|---|---|
| Frosted glass and progressive blur | M8. Plain translucency until then. Needs a trial first, and keeping windows inside the app window keeps it possible |
| Changing what's inside a toolbar, or adding toolbars | M8's toolbar customizer: a window to search for tools and drag them anywhere, making new toolbars. It replaces the "layout builder" idea |
| Tabs in widgets | Not now. The model is built so that only one rule has to change |
| Several sub-toolbars at once | Not in the owner's design |
| Windows docked along the top or bottom | Later, if ever. The code doesn't rule it out |
| Windows dragged out to a second monitor | Parked. It differs per OS and rules out frosted glass |
| Illustrator's collapse to icons, Unity's Panel and Collapse modes | Parked |
| Minimap | Later. It'll be an ordinary window |

## 11. How it's built

**Why not Dock:** see ADR 0035.

**Projects:** `src/AskiiArt.Docking` has no dependencies, not even on Avalonia, and `tests/AskiiArt.Docking.Tests` tests it. The Avalonia views stay in `src/AskiiArt.App/Shell/Docking`. Windows don't know where they're hosted (ARCHITECTURE §14.4), so `LayersPanelView` and `HistoryPanelView` don't change.

**Four parts, each feeding the next:**

| Part | What it is | Why |
|---|---|---|
| `DockingLayout` | Plain immutable data: each side's kind and columns, each column's tab groups, the floating windows, the toolbar placements, the remembered places | Saving, undoing a drag and testing all become easy |
| `LayoutSolver` | Layout + app window size + measured sizes → a rectangle for everything, plus the safe area. No state, no UI | Every rule in sections 2, 3, 4, 7 and 8 lives in one testable place |
| `LayoutEdits` | Layout + a change → a new layout. Drops, conversions, opening rules, repairs | A preview is `Solve(Edit(layout, drop))`, the same function as the drop |
| `DockingHost` and views | An Avalonia panel with no background that puts views at the solved rectangles and draws zones. A small controller beside it connects the pointer to a drag, which is data too (`ToolbarDrag`) | Empty pixels reach the canvas naturally, which removes today's workaround. Stays thin |

**A widget side is a dock side with stricter rules:** one column, single-tab groups, at most 4. Converting mostly flips a flag, and allowing tabs in widgets later means relaxing one rule. Sides are held by edge, with sizes along and across an axis, rather than by "left and right, vertical".

**Windows and toolbars are registered by id,** with a title, minimum size and a way to make the view. That's what the customizer (M8) and plugins (M8b) need.

**Tests:**

- **Property tests** (CsCheck) try thousands of random layouts and window sizes: nothing anchored overlaps, everything stays inside, toolbars stay within the windows' heights, widgets are title-only or at least their minimum, no window is lost or duplicated by any edit, repair changes nothing the second time, and saving then loading gives the same layout.
- **Example tests** turn each figure in the brief into a test.
- **Headless UI tests** cover click-through, focus, menus and simulated drags.
- **Manual tests** remain the only check of how it feels. Expect several tuning rounds, which is the real cost of owning this.

