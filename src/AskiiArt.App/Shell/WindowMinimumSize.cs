// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;

namespace AskiiArt.App.Shell;

/// <summary>
/// Works out the main window's minimum size. Display scaling can leave a screen with less room than
/// the size we want: 1080p at 150% has about 1280 × 670 logical pixels. Holding to the wanted size
/// there would push the bottom of the window off the screen, so the screen's room wins.
/// </summary>
internal static class WindowMinimumSize
{
    /// <param name="wanted">The minimum size to use when it fits, in logical pixels.</param>
    /// <param name="workingArea">The screen's room without the taskbar or Dock, in device pixels.</param>
    /// <param name="scaling">The screen's display scaling.</param>
    /// <param name="frame">What the title bar and borders add to the window, in logical pixels.</param>
    public static Size Fit(Size wanted, PixelSize workingArea, double scaling, Size frame) => new(
        Math.Min(wanted.Width, workingArea.Width / scaling - frame.Width),
        Math.Min(wanted.Height, workingArea.Height / scaling - frame.Height));
}
