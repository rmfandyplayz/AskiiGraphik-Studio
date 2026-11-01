// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Threading;

namespace AskiiArt.App.Shell.Dialogs;

/// <summary>
/// Shows a window as a modal dialog of its owner, and returns only once it has finished closing.
/// Every modal window in the app goes through here.
/// </summary>
/// <remarks>
/// Avalonia finishes <see cref="Window.ShowDialog{TResult}(Window)"/> inside the dialog's Closed event,
/// before it has let go of the owner and re-enabled it. Going straight on from there to a file picker
/// (Save in the unsaved-changes question, then Open) left the next picker unable to appear on Windows.
/// </remarks>
internal static class ModalDialog
{
    public static async Task<TResult> ShowAsync<TResult>(Window dialog, Window owner)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        var result = await dialog.ShowDialog<TResult>(owner);

        // Background runs after the rest of the close, and after the input and activation it causes.
        await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);
        return result;
    }

    public static Task ShowAsync(Window dialog, Window owner) => ShowAsync<object?>(dialog, owner);
}
