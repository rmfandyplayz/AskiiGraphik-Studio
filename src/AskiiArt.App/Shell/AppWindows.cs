// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace AskiiArt.App.Shell;

/// <summary>Finds the app's windows, for example to own a dialog.</summary>
internal static class AppWindows
{
    /// <summary>The main window, or null before it opens (and in tests that bring their own).</summary>
    public static Window? Main =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
