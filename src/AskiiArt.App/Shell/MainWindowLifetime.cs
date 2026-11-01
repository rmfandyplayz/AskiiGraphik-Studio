// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Documents;
using AskiiArt.Core.Editing;
using Avalonia.Input;
using Avalonia.Threading;

namespace AskiiArt.App.Shell;

/// <summary>
/// Opens the main window, one at a time, and decides what closing it means (ADR 0023). On Windows
/// and Linux, closing it quits the app. On macOS the app keeps running: closing the window closes
/// its document, and the next window starts with a new, empty one.
/// </summary>
internal sealed class MainWindowLifetime(MainWindowBuilder builder, ActiveDocument activeDocument,
    KeyModifiers platformCommandModifiers, bool keepsRunningWithoutWindow)
{
    /// <summary>The open main window, or null while none is (which only happens on macOS).</summary>
    public MainWindow? Window { get; private set; }

    /// <summary>Raised when <see cref="Window"/> changes.</summary>
    public event EventHandler? WindowChanged;

    /// <summary>Opens the main window, or brings the open one to the front.</summary>
    public MainWindow ShowWindow()
    {
        if (Window is { } open)
        {
            open.Activate();
            return open;
        }

        var window = builder.Build(platformCommandModifiers);
        window.Closed += OnWindowClosed;
        Window = window;
        WindowChanged?.Invoke(this, EventArgs.Empty);
        window.Show();
        return window;
    }

    /// <summary>
    /// Runs <paramref name="action"/> in the main window, opening one first if none is open, as the
    /// macOS Dock menu needs. A new window gets to finish opening first, so the action's dialogs
    /// have a window to belong to.
    /// </summary>
    public void RunInWindow(AppAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var hadWindow = Window is not null;
        ShowWindow();
        if (hadWindow)
        {
            Run(action);
            return;
        }

        Dispatcher.UIThread.Post(() => Run(action), DispatcherPriority.Background);
    }

    private static void Run(AppAction action)
    {
        if (action.Command.CanExecute(null))
            action.Command.Execute(null);
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is MainWindow window)
            window.Closed -= OnWindowClosed;

        Window = null;
        WindowChanged?.Invoke(this, EventArgs.Empty);

        // Anything unsaved was asked about before the window closed.
        if (keepsRunningWithoutWindow)
            activeDocument.Replace(new EditingSession(NewDocumentSettings.Default.CreateDocument()));
    }
}
